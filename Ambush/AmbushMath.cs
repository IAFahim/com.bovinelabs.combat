using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Ambush
{
    /// <summary>
    /// Pure math utility functions for ambush steering behavior.
    /// Ambush seeks a hide position during Hiding phase,
    /// then seeks the enemy during Springing phase.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class AmbushMath
    {
        /// <summary>
        /// Compute ambush steering force based on current phase.
        /// Hiding: seek the hide position.
        /// Springing: seek the spring target at high speed.
        /// Waiting: zero force (stay still).
        /// </summary>
        /// <param name="agentPos">Agent's current XZ position.</param>
        /// <param name="hidePos">Position to hide at.</param>
        /// <param name="maxSpeed">Agent's maximum movement speed.</param>
        /// <param name="phase">Current ambush phase.</param>
        /// <param name="springTarget">Target to spring toward (only used when phase == Springing).</param>
        /// <returns>Steering force on the XZ plane.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeAmbushForce(float2 agentPos, float2 hidePos, float maxSpeed, AmbushPhase phase, float2 springTarget)
        {
            switch (phase)
            {
                case AmbushPhase.Hiding:
                    return SeekForce(agentPos, hidePos, maxSpeed);

                case AmbushPhase.Springing:
                    return SeekForce(agentPos, springTarget, maxSpeed * 1.5f);

                case AmbushPhase.Waiting:
                default:
                    return float2.zero;
            }
        }

        /// <summary>
        /// Check if an enemy is within the ambush trigger radius.
        /// </summary>
        /// <param name="enemyPos">Enemy's XZ position.</param>
        /// <param name="ambushPos">Center of the ambush (hide position).</param>
        /// <param name="triggerRadius">Radius to check.</param>
        /// <returns>True if enemy is within trigger radius.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsEnemyInTrigger(float2 enemyPos, float2 ambushPos, float triggerRadius)
        {
            var distSq = math.lengthsq(enemyPos - ambushPos);
            return distSq <= triggerRadius * triggerRadius;
        }

        /// <summary>
        /// Check if the agent has reached the hide position (within arrival threshold).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasReachedHidePosition(float2 agentPos, float2 hidePos, float arrivalThreshold)
        {
            var distSq = math.lengthsq(agentPos - hidePos);
            return distSq <= arrivalThreshold * arrivalThreshold;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 SeekForce(float2 from, float2 to, float speed)
        {
            var desired = to - from;
            var dist = math.length(desired);
            if (dist < 0.0001f)
                return float2.zero;

            return math.normalize(desired) * speed;
        }
    }
}
