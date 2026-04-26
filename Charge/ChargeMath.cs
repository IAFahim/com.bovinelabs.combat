using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Charge
{
    /// <summary>
    /// Pure math utility functions for charge steering behavior.
    /// Charge moves in a straight line at high speed toward a target position.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class ChargeMath
    {
        /// <summary>
        /// Compute the charge direction: straight line from current position to target.
        /// Returns a normalized direction vector. Returns zero if already at target.
        /// </summary>
        /// <param name="currentPos">Agent's current XZ position.</param>
        /// <param name="targetPos">Target XZ position to charge toward.</param>
        /// <returns>Normalized direction toward target, or zero if at target.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeChargeDirection(float2 currentPos, float2 targetPos)
        {
            var toTarget = targetPos - currentPos;
            var dist = math.length(toTarget);
            if (dist < 0.0001f)
                return float2.zero;

            return math.normalize(toTarget);
        }

        /// <summary>
        /// Check if a charge is valid: target must be beyond the minimum charge distance.
        /// </summary>
        /// <param name="currentPos">Agent's current XZ position.</param>
        /// <param name="targetPos">Target XZ position.</param>
        /// <param name="minDistance">Minimum distance required for a valid charge.</param>
        /// <returns>True if the target is far enough to charge toward.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsChargeValid(float2 currentPos, float2 targetPos, float minDistance)
        {
            var distSq = math.lengthsq(targetPos - currentPos);
            return distSq >= minDistance * minDistance;
        }

        /// <summary>
        /// Compute the full charge steering force: direction * chargeSpeed.
        /// The chargeSpeed = baseMaxSpeed * chargeSpeedMultiplier.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeChargeForce(
            float2 currentPos,
            float2 targetPos,
            float baseMaxSpeed,
            float chargeSpeedMultiplier)
        {
            var direction = ComputeChargeDirection(currentPos, targetPos);
            var chargeSpeed = baseMaxSpeed * chargeSpeedMultiplier;
            return direction * chargeSpeed;
        }
    }
}
