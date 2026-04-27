using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.GridIntelligence
{
    /// <summary>
    /// Local grid-based tactical analysis system.
    /// Reads the DynamicBuffer&lt;SpatialNeighborData&gt; (populated by SpatialIntelligence),
    /// divides the agent's vicinity into a grid, computes per-cell threat density,
    /// and outputs TacticalGridData for tactical decision-making.
    ///
    /// Also produces a steering force toward cover (away from threat cells) when
    /// dangerous cells are detected, enabling agents to autonomously seek safer ground.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct GridIntelligenceSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TacticalGridConfig>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (config, transform, stats, teamId, neighborBuffer, gridData, steering) in
                SystemAPI.Query<
                    RefRO<TacticalGridConfig>,
                    RefRO<Unity.Transforms.LocalTransform>,
                    RefRO<MovementStats>,
                    RefRO<TeamId>,
                    DynamicBuffer<SpatialNeighborData>,
                    RefRW<TacticalGridData>,
                    RefRW<SteeringForce>>())
            {
                var gridRadius = config.ValueRO.GridRadius;
                var resolution = config.ValueRO.GridResolution;
                var cellSize = new float2(gridRadius * 2f / resolution);
                var agentPos = transform.ValueRO.Position.xz;
                var gridOrigin = agentPos - gridRadius;

                // Convert neighbor buffer to NativeArray for the math function
                var neighbors = neighborBuffer.AsNativeArray();

                GridIntelligenceMath.ComputeGridAnalysis(
                    resolution,
                    cellSize,
                    gridOrigin,
                    neighbors,
                    teamId.ValueRO.Value,
                    config.ValueRO.ThreatThreshold,
                    out var result);

                // Write tactical grid data
                gridData.ValueRW = result;

                // Produce steering force toward cover if there are dangerous cells
                if (result.DangerousCellCount > 0 && config.ValueRO.CoverWeight > 0f)
                {
                    var safestDir = result.SafestDirection;
                    if (math.lengthsq(safestDir) > 0.0001f)
                    {
                        steering.ValueRW.Linear = safestDir * stats.ValueRO.MaxSpeed * config.ValueRO.CoverWeight;
                        steering.ValueRW.Priority = 1.5f;
                        steering.ValueRW.Weight = config.ValueRO.CoverWeight;
                        steering.ValueRW.BehaviorType = SteeringBehaviorType.GridTactical;
                    }
                }
            }
        }
    }
}
