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
    /// Burst-compiled IJobEntity that queries the spatial map for each agent,
    /// populates the SpatialNeighborData buffer, and writes SpatialThreatAssessment.
    /// Uses the BovineLabs SpatialMap broadphase to avoid O(n^2) brute force.
    ///
    /// entityInQueryIndex maps directly into the parallel arrays (Entities, Positions,
    /// TeamIds, MovementStats) since they were gathered from the same query.
    ///
    /// MUST be in its own file - Unity's IJobEntity source generator requires
    /// the partial struct to be the only top-level type in the file.
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
