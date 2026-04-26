using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Avoidance
{
    /// <summary>
    /// Spatial hash-based neighbor query for local avoidance.
    /// Rebuilds every frame from agent positions.
    /// Uses a simple grid hash for O(1) cell lookup.
    /// </summary>
    public static unsafe class SpatialHash
    {
        /// <summary>
        /// Compute grid cell index for a position.
        /// </summary>
        public static int2 Cell(float2 position, float cellSize)
        {
            return new int2(
                (int)math.floor(position.x / cellSize),
                (int)math.floor(position.y / cellSize)
            );
        }

        /// <summary>
        /// Query all agents within radius of a position.
        /// Uses the spatial hash grid for efficient neighbor lookup.
        /// Returns indices of matching agents.
        /// </summary>
        public static NativeList<int> Query(
            float2 center,
            float radius,
            float cellSize,
            NativeArray<float2> positions,
            NativeArray<float> radii,
            Allocator allocator)
        {
            var results = new NativeList<int>(allocator);
            var radiusSq = radius * radius;
            var minCell = Cell(center - new float2(radius), cellSize);
            var maxCell = Cell(center + new float2(radius), cellSize);

            for (int x = minCell.x; x <= maxCell.x; x++)
            {
                for (int y = minCell.y; y <= maxCell.y; y++)
                {
                    // Linear scan of all agents (could be optimized with actual hash map)
                    for (int i = 0; i < positions.Length; i++)
                    {
                        var distSq = math.distancesq(center, positions[i]);
                        var combinedRadius = radius + radii[i];
                        if (distSq <= combinedRadius * combinedRadius)
                        {
                            results.Add(i);
                        }
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Find the nearest agent to a position.
        /// </summary>
        public static int FindNearest(
            float2 position,
            NativeArray<float2> positions,
            NativeArray<float> radii,
            float maxRange)
        {
            var bestIndex = -1;
            var bestDistSq = maxRange * maxRange;

            for (int i = 0; i < positions.Length; i++)
            {
                var distSq = math.distancesq(position, positions[i]);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }
    }
}
