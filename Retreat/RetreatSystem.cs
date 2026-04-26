using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Retreat
{
    /// <summary>
    /// Retreat behavior system.
    /// When isRetreating is true, outputs a steering force that moves the agent
    /// directly away from the threat position. Once the agent reaches safeDistance
    /// from the threat, retreating stops and the force goes to zero.
    ///
    /// Priority = 2.0 to override standard movement but yield to charge/avoidance.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct RetreatSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RetreatFrom>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (retreat, stats, transform, steering) in
                SystemAPI.Query<
                    RefRW<RetreatFrom>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                // Only compute force when actively retreating
                if (!retreat.ValueRO.IsRetreating)
                    continue;

                var currentPos = transform.ValueRO.Position.xz;
                var threatPos = retreat.ValueRO.ThreatPos;
                var safeDistance = retreat.ValueRO.SafeDistance;

                var retreatForce = RetreatMath.ComputeRetreatDirection(
                    currentPos,
                    threatPos,
                    safeDistance,
                    stats.ValueRO.MaxSpeed);

                // If force is zero, we've reached safety
                if (math.lengthsq(retreatForce) < 0.0001f)
                {
                    retreat.ValueRW.IsRetreating = false;
                    continue;
                }

                steering.ValueRW.Linear = retreatForce;
                steering.ValueRW.Priority = 2f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Retreat;
            }
        }
    }
}
