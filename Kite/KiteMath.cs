using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Kite
{
    /// <summary>
    /// Pure math utility functions for kite steering behavior.
    /// Kiting maintains optimal range by moving to an arc position around the target.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class KiteMath
    {
        /// <summary>
        /// Compute the ideal kite position: a point on a circle of radius optimalRange
        /// around the target, offset by arcAngle from the current agent-to-target direction.
        /// kiteDirection: +1 for CCW, -1 for CW.
        /// </summary>
        /// <param name="agentPos">Agent's current XZ position.</param>
        /// <param name="targetPos">Target position to kite around.</param>
        /// <param name="optimalRange">Desired distance from target.</param>
        /// <param name="arcAngle">Angular offset from the current bearing (radians).</param>
        /// <param name="kiteDirection">Circle direction: +1 CCW, -1 CW.</param>
        /// <returns>The kite position on the circle around the target.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeKitePosition(float2 agentPos, float2 targetPos, float optimalRange, float arcAngle, float kiteDirection)
        {
            var toAgent = agentPos - targetPos;
            var dist = math.length(toAgent);

            // Current bearing angle from target to agent
            float bearingAngle;
            if (dist < 0.0001f)
            {
                // Agent on top of target - pick arbitrary bearing
                bearingAngle = 0f;
            }
            else
            {
                bearingAngle = math.atan2(toAgent.y, toAgent.x);
            }

            // Offset the bearing by arcAngle in the kite direction
            var kiteAngle = bearingAngle + arcAngle * kiteDirection;

            return targetPos + new float2(
                math.cos(kiteAngle) * optimalRange,
                math.sin(kiteAngle) * optimalRange);
        }

        /// <summary>
        /// Determine whether the agent should begin kiting based on distance to target.
        /// Returns true if the agent is within optimalRange +/- tolerance of the target.
        /// </summary>
        /// <param name="agentPos">Agent's current XZ position.</param>
        /// <param name="targetPos">Target position.</param>
        /// <param name="optimalRange">Desired distance from target.</param>
        /// <param name="tolerance">Distance tolerance before engaging kite behavior.</param>
        /// <returns>True if the agent is close enough that kiting is warranted.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldKite(float2 agentPos, float2 targetPos, float optimalRange, float tolerance)
        {
            var distSq = math.lengthsq(agentPos - targetPos);
            var threshold = optimalRange - tolerance;
            // Kite when too close (within optimalRange - tolerance)
            return threshold > 0f && distSq < threshold * threshold;
        }
    }
}
