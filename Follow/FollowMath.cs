using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Follow
{
    /// <summary>
    /// Pure math utility functions for follow steering behaviors.
    /// Computes target positions behind leaders or chain entities on the XZ plane.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class FollowMath
    {
        /// <summary>
        /// Compute the follow position behind a leader at the specified distance and offset.
        /// The follow point is directly behind the leader (opposite to leaderForward),
        /// then offset laterally by the given offset vector.
        /// </summary>
        /// <param name="leaderPos">Leader's current XZ position.</param>
        /// <param name="leaderForward">Leader's forward direction (normalized).</param>
        /// <param name="distance">Distance behind the leader.</param>
        /// <param name="offset">Lateral offset from the behind position.</param>
        /// <returns>Target world position on the XZ plane.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeFollowPosition(
            float2 leaderPos,
            float2 leaderForward,
            float distance,
            float2 offset)
        {
            var backward = -leaderForward;
            return leaderPos + backward * distance + offset;
        }

        /// <summary>
        /// Compute the chain follow position behind the entity directly ahead.
        /// Uses the ahead entity's forward direction to determine "behind."
        /// </summary>
        /// <param name="aheadEntityPos">Position of the entity ahead in the chain.</param>
        /// <param name="aheadEntityForward">Forward direction of the entity ahead.</param>
        /// <param name="distance">Distance to maintain behind the ahead entity.</param>
        /// <returns>Target world position on the XZ plane.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeChainPosition(
            float2 aheadEntityPos,
            float2 aheadEntityForward,
            float distance)
        {
            var backward = -aheadEntityForward;
            return aheadEntityPos + backward * distance;
        }
    }
}
