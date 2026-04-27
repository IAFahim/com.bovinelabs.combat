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
        private PositionBuilder positionBuilder;
        private EntityQuery spatialQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            spatialQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, MovementStats, TeamId, SpatialNeighborConfig>()
                .Build();

            positionBuilder = new PositionBuilder(ref state, spatialQuery);

            // Spatial map: cell size 16, world size 4096x4096 (-2048 to 2048)
            // Quantized grid = 256x256 cells, ~1MB memory
            spatialMap = new SpatialMap<SpatialPosition>(quantizeStep: 16, size: 4096);

            state.RequireForUpdate(spatialQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Step 1: Gather all agent positions via PositionBuilder and build spatial map
            state.Dependency = positionBuilder.Gather(ref state, state.Dependency, out var positions);
            state.Dependency = spatialMap.Build(positions, state.Dependency);

            // Step 2: Get entities + metadata for index-to-entity mapping
            var entities = spatialQuery.ToEntityListAsync(state.WorldUpdateAllocator, state.Dependency, out var entitiesDep);
            state.Dependency = entitiesDep;

            var teamIds = spatialQuery.ToComponentDataListAsync<TeamId>(state.WorldUpdateAllocator, state.Dependency, out var teamIdsDep);
            state.Dependency = teamIdsDep;

            var stats = spatialQuery.ToComponentDataListAsync<MovementStats>(state.WorldUpdateAllocator, state.Dependency, out var statsDep);
            state.Dependency = statsDep;

            // Step 3: Get read-only spatial map for queries inside the job
            var spatialMapRO = spatialMap.AsReadOnly();

            // Step 4: Schedule the main neighbor-population job
            state.Dependency = new SpatialIntelligenceJob
            {
                Entities = entities.AsDeferredJobArray(),
                Positions = positions,
                TeamIds = teamIds.AsDeferredJobArray(),
                MovementStats = stats.AsDeferredJobArray(),
                SpatialMapRO = spatialMapRO,
            }.ScheduleParallel(spatialQuery, state.Dependency);

            // Dispose temp collections after jobs complete
            entities.Dispose(state.Dependency);
            teamIds.Dispose(state.Dependency);
            stats.Dispose(state.Dependency);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            if (spatialMap.IsCreated)
                spatialMap.Dispose();
        }
    }

    /// <summary>
    /// Burst-compiled IJobEntity that queries the spatial map for each agent,
    /// populates the SpatialNeighborData buffer, and writes SpatialThreatAssessment.
    /// Uses the BovineLabs SpatialMap broadphase to avoid O(n^2) brute force.
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

            // Find self index in the spatial map arrays for skip-self logic
            int myIndex = -1;
            for (int i = 0; i < Entities.Length; i++)
            {
                if (Entities[i] == entity)
                {
                    myIndex = i;
                    break;
                }
            }

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

                        // Team filtering (if enabled, only include enemies)
                        if (filterByTeam)
                        {
                            bool isEnemy = myTeam != 0 && candidateTeam != 0 && myTeam != candidateTeam;
                            if (queryTeamId != 0)
                                isEnemy = candidateTeam == queryTeamId && candidateTeam != myTeam;

                            if (!isEnemy)
                                continue;
                        }

                        // Check buffer capacity
                        if (neighborBuffer.Length >= maxNeighbors)
                            goto done;

                        // Add to neighbor buffer
                        var stats = MovementStats[candidateIdx];
                        neighborBuffer.Add(new SpatialNeighborData
                        {
                            Entity = Entities[candidateIdx],
                            Distance = distance,
                            Direction = direction,
                            TeamId = candidateTeam,
                            Velocity = stats.Velocity,
                            Radius = stats.Radius,
                        });

                        // Accumulate threat assessment data (always counts all, regardless of filter)
                        bool isAllyForThreat = myTeam != 0 && myTeam == candidateTeam;
                        bool isEnemyForThreat = myTeam != 0 && candidateTeam != 0 && myTeam != candidateTeam;

                        if (isEnemyForThreat)
                        {
                            enemyCount++;
                            if (distSq < nearestDistSq)
                            {
                                nearestDistSq = distSq;
                                nearestDir = direction;
                            }
                            enemySum += candidatePos;
                        }
                        else if (isAllyForThreat)
                        {
                            allyCount++;
                        }
                    } while (SpatialMapRO.Map.TryGetNextValue(out candidateIdx, ref it));
                }
            }

        done:
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
