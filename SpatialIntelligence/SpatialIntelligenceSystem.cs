using BovineLabs.Combat.Core;
using BovineLabs.Core.Spatial;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.SpatialIntelligence
{
    /// <summary>
    /// Builds a SpatialMap each frame and populates DynamicBuffer&lt;SpatialNeighborData&gt;
    /// per agent using O(1) broadphase grid queries. Also writes SpatialThreatAssessment
    /// for downstream combat AI decisions.
    ///
    /// Uses BovineLabs.Core.SpatialMap for frame-rebuild spatial hashing:
    ///   - quantizeStep=16, size=4096 => 256x256 cells, ~1MB memory
    ///   - Broadphase reduces O(n^2) neighbor search to O(n * k) where k = cell density
    ///
    /// Runs in FixedStepSimulationSystemGroup BEFORE CombatSteeringGroup so all steering
    /// behaviors have access to up-to-date neighbor data.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(CombatSteeringGroup))]
    public partial struct SpatialIntelligenceSystem : ISystem
    {
        private SpatialMap<SpatialPosition> spatialMap;
        private EntityQuery spatialQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Single query for all spatial intelligence agents.
            // Includes all components the job reads/writes.
            spatialQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, MovementStats, TeamId, SpatialNeighborConfig,
                         SpatialThreatAssessment>()
                .WithAllRW<SpatialNeighborData>()
                .Build();

            spatialMap = new SpatialMap<SpatialPosition>(quantizeStep: 16, size: 4096);

            state.RequireForUpdate(spatialQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var entityCount = spatialQuery.CalculateEntityCount();
            if (entityCount == 0)
                return;

            // Allocate temp arrays for gather
            var positions = new NativeArray<SpatialPosition>(entityCount, Allocator.TempJob);
            var entities = new NativeArray<Entity>(entityCount, Allocator.TempJob);
            var teamIds = new NativeArray<TeamId>(entityCount, Allocator.TempJob);
            var movementStats = new NativeArray<MovementStats>(entityCount, Allocator.TempJob);

            // Get base entity indices for correct multi-chunk writes
            var baseEntityIndices = spatialQuery.CalculateBaseEntityIndexArrayAsync(
                state.WorldUpdateAllocator, state.Dependency, out var baseIndexDep);
            state.Dependency = baseIndexDep;

            // Step 1: Gather positions and metadata manually
            var gatherJob = new GatherJob
            {
                LocalTransformHandle = state.GetComponentTypeHandle<LocalTransform>(true),
                TeamIdHandle = state.GetComponentTypeHandle<TeamId>(true),
                MovementStatsHandle = state.GetComponentTypeHandle<MovementStats>(true),
                EntityTypeHandle = state.GetEntityTypeHandle(),
                FirstEntityIndices = baseEntityIndices,
                Positions = positions,
                Entities = entities,
                TeamIds = teamIds,
                MovementStats = movementStats,
            }.ScheduleParallel(spatialQuery, state.Dependency);

            // Step 2: Build spatial map from gathered positions
            state.Dependency = spatialMap.Build(positions, gatherJob);

            // Step 3: Schedule the main neighbor-population job
            var spatialMapRO = spatialMap.AsReadOnly();

            state.Dependency = new SpatialIntelligenceJob
            {
                Entities = entities,
                Positions = positions,
                TeamIds = teamIds,
                MovementStats = movementStats,
                SpatialMapRO = spatialMapRO,
            }.ScheduleParallel(spatialQuery, state.Dependency);

            // Dispose all temp collections after jobs complete
            positions.Dispose(state.Dependency);
            entities.Dispose(state.Dependency);
            teamIds.Dispose(state.Dependency);
            movementStats.Dispose(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (spatialMap.IsCreated)
                spatialMap.Dispose();
        }

        /// <summary>
        /// Gathers LocalTransform positions + metadata into parallel arrays.
        /// Avoids PositionBuilder which asserts against enableable components.
        /// Uses FirstEntityIndices for correct multi-chunk index computation.
        /// </summary>
        [BurstCompile]
        private struct GatherJob : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<LocalTransform> LocalTransformHandle;
            [ReadOnly] public ComponentTypeHandle<TeamId> TeamIdHandle;
            [ReadOnly] public ComponentTypeHandle<MovementStats> MovementStatsHandle;
            [ReadOnly] public EntityTypeHandle EntityTypeHandle;
            [ReadOnly] public NativeArray<int> FirstEntityIndices;

            [NativeDisableParallelForRestriction]
            public NativeArray<SpatialPosition> Positions;
            [NativeDisableParallelForRestriction]
            public NativeArray<Entity> Entities;
            [NativeDisableParallelForRestriction]
            public NativeArray<TeamId> TeamIds;
            [NativeDisableParallelForRestriction]
            public NativeArray<MovementStats> MovementStats;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in Unity.Burst.Intrinsics.v128 chunkEnabledMask)
            {
                var transforms = chunk.GetNativeArray(ref LocalTransformHandle);
                var teams = chunk.GetNativeArray(ref TeamIdHandle);
                var stats = chunk.GetNativeArray(ref MovementStatsHandle);
                var chunkEntities = chunk.GetNativeArray(EntityTypeHandle);
                var baseIndex = FirstEntityIndices[unfilteredChunkIndex];

                for (int i = 0; i < chunk.Count; i++)
                {
                    Positions[baseIndex + i] = new SpatialPosition { Position = transforms[i].Position };
                    Entities[baseIndex + i] = chunkEntities[i];
                    TeamIds[baseIndex + i] = teams[i];
                    MovementStats[baseIndex + i] = stats[i];
                }
            }
        }
    }
}
