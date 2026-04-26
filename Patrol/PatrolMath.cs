using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Patrol
{
    /// <summary>
    /// Pure math utility functions for patrol behaviors.
    /// Handles waypoint cycling, area wandering, and containment checks.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class PatrolMath
    {
        /// <summary>
        /// Compute the next waypoint index with wrap-around support.
        /// When looping, wraps from last to first. When not looping, stays at the last index.
        /// </summary>
        /// <param name="current">Current waypoint index.</param>
        /// <param name="total">Total number of waypoints.</param>
        /// <param name="loop">Whether the patrol loops back to the start.</param>
        /// <returns>Next waypoint index.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextWaypointIndex(int current, int total, bool loop)
        {
            if (total <= 0)
                return 0;

            var next = current + 1;
            if (next >= total)
            {
                return loop ? 0 : current; // Stay at last if not looping
            }

            return next;
        }

        /// <summary>
        /// Generate a random point within a rectangular area defined by center and half-extents.
        /// </summary>
        /// <param name="center">Center of the area on the XZ plane.</param>
        /// <param name="halfExtents">Half-extents (half-width, half-depth).</param>
        /// <param name="rng">Reference to a Random instance (will be consumed).</param>
        /// <returns>Random point within the area bounds.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 RandomPointInArea(float2 center, float2 halfExtents, ref Random rng)
        {
            var offsetX = rng.NextFloat(-halfExtents.x, halfExtents.x);
            var offsetY = rng.NextFloat(-halfExtents.y, halfExtents.y);
            return center + new float2(offsetX, offsetY);
        }

        /// <summary>
        /// Check if a position is inside a rectangular area.
        /// Uses AABB test: point must be within center +/- halfExtents on each axis.
        /// </summary>
        /// <param name="pos">Position to test.</param>
        /// <param name="center">Area center.</param>
        /// <param name="halfExtents">Area half-extents.</param>
        /// <returns>True if the position is inside the area.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInsideArea(float2 pos, float2 center, float2 halfExtents)
        {
            var diff = pos - center;
            return math.abs(diff.x) <= halfExtents.x && math.abs(diff.y) <= halfExtents.y;
        }
    }
}
