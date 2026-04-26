using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Flank
{
    /// <summary>
    /// Pure math utility functions for flank steering behavior.
    /// Flank positions agents beside or behind a target, using the target's
    /// facing direction to determine optimal positioning.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class FlankMath
    {
        /// <summary>
        /// Compute the desired flank position relative to the target.
        /// The position is offset from the target by flankAngleOffset (relative to target facing)
        /// at flankDistance units away.
        /// </summary>
        /// <param name="targetPos">Position of the target to flank.</param>
        /// <param name="targetFacingAngle">Direction the target is facing (radians).</param>
        /// <param name="flankAngleOffset">Angle offset from target's facing direction.</param>
        /// <param name="flankDistance">Distance from target to flank position.</param>
        /// <returns>The desired XZ position for the flanking agent.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeFlankPosition(
            float2 targetPos,
            float targetFacingAngle,
            float flankAngleOffset,
            float flankDistance)
        {
            // The flank angle is relative to the target's facing direction.
            // We go to the side/behind: the actual world angle is facingAngle + PI (behind) + offset
            var worldAngle = targetFacingAngle + math.PI + flankAngleOffset;

            var offset = new float2(
                math.sin(worldAngle) * flankDistance,
                math.cos(worldAngle) * flankDistance);

            return targetPos + offset;
        }

        /// <summary>
        /// Compute the steering direction toward the flank position.
        /// Returns a normalized direction scaled by maxSpeed.
        /// </summary>
        /// <param name="currentPos">Agent's current XZ position.</param>
        /// <param name="flankTargetPos">The computed flank position to seek toward.</param>
        /// <param name="maxSpeed">Agent's maximum movement speed.</param>
        /// <returns>Steering force toward the flank position.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeFlankDirection(
            float2 currentPos,
            float2 flankTargetPos,
            float maxSpeed)
        {
            var toFlank = flankTargetPos - currentPos;
            var dist = math.length(toFlank);
            if (dist < 0.0001f)
                return float2.zero;

            return math.normalize(toFlank) * maxSpeed;
        }
    }
}
