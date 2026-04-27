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

    /// <summary>
    /// Burst-compiled IJobEntity that queries the spatial map for each agent,
    /// populates the SpatialNeighborData buffer, and writes SpatialThreatAssessment.
    /// Uses the BovineLabs SpatialMap broadphase to avoid O(n^2) brute force.
    ///
    /// entityInQueryIndex maps directly into the parallel arrays (Entities, Positions,
    /// TeamIds, MovementStats) since they were gathered from the same query.
    /// </summary>
    [BurstCompile]
    public partial struct SpatialIntelligenceJob : IJobEntity
    {
        [ReadOnly] public NativeArray<Entity> Entities;
        [ReadOnly] public NativeArray<SpatialPosition> Positions;
        [ReadOnly] public NativeArray<TeamId> TeamIds;
        [ReadOnly] public NativeArray<MovementStats> MovementStats;
        [ReadOnly] public SpatialMap.ReadOnly SpatialMapRO;

        public void Execute(
            Entity entity,
            int entityInQueryIndex,
            in LocalTransform transform,
            in TeamId teamId,
            in SpatialNeighborConfig config,
            ref SpatialThreatAssessment threatAssessment,
            ref DynamicBuffer<SpatialNeighborData> neighborBuffer)
        {
            var agentPos = transform.Position.xz;
            var myTeam = teamId.Value;
            var searchRadius = config.SearchRadius;
            var maxNeighbors = config.MaxNeighbors;
            var filterByTeam = config.FilterByTeam;
            var queryTeamId = config.QueryTeamId;

            // Clear the buffer for this frame
            neighborBuffer.Clear();

            // entityInQueryIndex IS our index into the parallel arrays
            int myIndex = entityInQueryIndex;

            // Query the spatial map: iterate all cells overlapping the search radius
            var min = SpatialMapRO.Quantized(agentPos - searchRadius);
            var max = SpatialMapRO.Quantized(agentPos + searchRadius);
            var radiusSq = searchRadius * searchRadius;

            // Accumulate threat data inline (no extra allocation)
            int enemyCount = 0;
            int allyCount = 0;
            float nearestDistSq = float.MaxValue;
            float2 nearestDir = float2.zero;
            float2 enemySum = float2.zero;
            bool bufferFull = false;

            for (int j = min.y; j <= max.y; j++)
            {
                for (int i = min.x; i <= max.x; i++)
                {
                    var hash = SpatialMapRO.Hash(new int2(i, j));

                    if (!SpatialMapRO.Map.TryGetFirstValue(hash, out int candidateIdx, out var it))
                        continue;

                    do
                    {
                        // Skip self
                        if (candidateIdx == myIndex)
                            continue;

                        // Bounds check
                        if (candidateIdx < 0 || candidateIdx >= Positions.Length)
                            continue;

                        var candidatePos = Positions[candidateIdx].Position.xz;
                        var diff = candidatePos - agentPos;
                        var distSq = math.lengthsq(diff);

                        if (distSq > radiusSq || distSq < 0.0001f)
                            continue;

                        var distance = math.sqrt(distSq);
                        var direction = diff / distance;
                        var candidateTeam = TeamIds[candidateIdx].Value;

                        // Always accumulate threat assessment (regardless of buffer filter)
                        bool isEnemy = myTeam != 0 && candidateTeam != 0 && myTeam != candidateTeam;
                        bool isAlly = myTeam != 0 && myTeam == candidateTeam;

                        if (isEnemy)
                        {
                            enemyCount++;
                            if (distSq < nearestDistSq)
                            {
                                nearestDistSq = distSq;
                                nearestDir = direction;
                            }
                            enemySum += candidatePos;
                        }
                        else if (isAlly)
                        {
                            allyCount++;
                        }

                        // Team filtering for buffer (separate from threat counting)
                        if (filterByTeam)
                        {
                            bool passesFilter = isEnemy;
                            if (queryTeamId != 0)
                                passesFilter = candidateTeam == queryTeamId && candidateTeam != myTeam;
                            if (!passesFilter)
                                continue;
                        }

                        // Add to neighbor buffer (if not full)
                        if (!bufferFull)
                        {
                            var candidateStats = MovementStats[candidateIdx];
                            neighborBuffer.Add(new SpatialNeighborData
                            {
                                Entity = Entities[candidateIdx],
                                Distance = distance,
                                Direction = direction,
                                TeamId = candidateTeam,
                                Velocity = candidateStats.Velocity,
                                Radius = candidateStats.Radius,
                            });

                            if (neighborBuffer.Length >= maxNeighbors)
                                bufferFull = true;
                        }
                    } while (SpatialMapRO.Map.TryGetNextValue(out candidateIdx, ref it));
                }
            }

            // Write threat assessment
            threatAssessment.EnemyCount = enemyCount;
            threatAssessment.AllyCount = allyCount;
            threatAssessment.NearestEnemyDirection = nearestDir;
            threatAssessment.NearestEnemyDistance = nearestDistSq < float.MaxValue
                ? math.sqrt(nearestDistSq)
                : float.MaxValue;
            threatAssessment.CentroidOfEnemies = enemyCount > 0
                ? enemySum / enemyCount
                : float2.zero;
            threatAssessment.ThreatDensity = SpatialIntelligenceMath.ComputeThreatDensity(enemyCount, searchRadius);
        }
    }
}
