using System.Runtime.CompilerServices;
using BovineLabs.Combat.Core;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.SpatialIntelligence
{
    /// <summary>
    /// Pure static math utilities for spatial threat assessment.
    /// All functions operate on the XZ plane (float2), are Burst-friendly,
    /// and have no ECS dependencies. Easily unit-testable.
    /// </summary>
    public static class SpatialIntelligenceMath
    {
        /// <summary>
        /// Compute threat density: enemies per unit search area.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ComputeThreatDensity(int enemyCount, float searchRadius)
        {
            if (searchRadius <= 0f)
                return 0f;

            float area = math.PI * searchRadius * searchRadius;
            return enemyCount / area;
        }

        /// <summary>
        /// Compute threat density from a neighbor buffer, counting enemies.
        /// Convenience overload that counts enemies for you.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ComputeThreatDensity(NativeArray<SpatialNeighborData> neighbors, float searchRadius)
        {
            // This overload counts enemies from the buffer - used by tests
            // Note: this can't know "my team" so it counts all non-zero teams as enemies
            int count = 0;
            for (int i = 0; i < neighbors.Length; i++)
            {
                if (neighbors[i].TeamId != 0)
                    count++;
            }
            return ComputeThreatDensity(count, searchRadius);
        }

        /// <summary>
        /// Compute a full threat assessment from the populated neighbor buffer.
        /// Separates allies from enemies using TeamId, finds nearest enemy,
        /// computes enemy centroid, and threat density.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ComputeThreatAssessment(
            float2 agentPos,
            NativeArray<SpatialNeighborData> neighbors,
            int myTeam,
            float searchRadius,
            out SpatialThreatAssessment result)
        {
            int enemyCount = 0;
            int allyCount = 0;
            float nearestDistSq = float.MaxValue;
            float2 nearestDir = float2.zero;
            float2 enemySum = float2.zero;

            for (int i = 0; i < neighbors.Length; i++)
            {
                var neighbor = neighbors[i];
                bool isEnemy = myTeam != 0 && neighbor.TeamId != 0 && myTeam != neighbor.TeamId;
                bool isAlly = myTeam != 0 && myTeam == neighbor.TeamId;

                if (isEnemy)
                {
                    enemyCount++;

                    // Track nearest enemy
                    var distSq = neighbor.Distance * neighbor.Distance;
                    if (distSq < nearestDistSq)
                    {
                        nearestDistSq = distSq;
                        nearestDir = neighbor.Direction;
                    }

                    // Accumulate for centroid: reconstruct position from direction/distance
                    enemySum += agentPos + neighbor.Direction * neighbor.Distance;
                }
                else if (isAlly)
                {
                    allyCount++;
                }
            }

            result.EnemyCount = enemyCount;
            result.AllyCount = allyCount;
            result.NearestEnemyDirection = nearestDir;
            result.NearestEnemyDistance = nearestDistSq < float.MaxValue
                ? math.sqrt(nearestDistSq)
                : float.MaxValue;
            result.CentroidOfEnemies = enemyCount > 0
                ? enemySum / enemyCount
                : float2.zero;
            result.ThreatDensity = ComputeThreatDensity(enemyCount, searchRadius);
        }

        /// <summary>
        /// Compute the centroid (average position) of a list of positions.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeCentroid(NativeList<float2> positions)
        {
            if (positions.Length == 0)
                return float2.zero;

            float2 sum = float2.zero;
            for (int i = 0; i < positions.Length; i++)
            {
                sum += positions[i];
            }

            return sum / positions.Length;
        }
    }
}
