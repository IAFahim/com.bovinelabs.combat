using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Retreat
{
    /// <summary>
    /// Pure math utility functions for retreat steering behavior.
    /// Retreat is flee with a safe-distance stop condition: move away from
    /// the threat until the agent is beyond safeDistance, then output zero force.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class RetreatMath
    {
        /// <summary>
        /// Compute retreat direction: flee from threat, but only while within safeDistance.
        /// Returns a direction vector scaled by maxSpeed while retreating, or zero if safe.
        /// </summary>
        /// <param name="agentPos">Agent's current XZ position.</param>
        /// <param name="threatPos">Threat position to flee from.</param>
        /// <param name="safeDistance">Distance at which retreating stops.</param>
        /// <param name="maxSpeed">Agent's maximum movement speed.</param>
        /// <returns>Steering force: away from threat at maxSpeed, or zero if safe.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeRetreatDirection(float2 agentPos, float2 threatPos, float safeDistance, float maxSpeed)
        {
            var away = agentPos - threatPos;
            var dist = math.length(away);

            // Already at safe distance - stop retreating
            if (dist >= safeDistance)
                return float2.zero;

            // On top of threat - pick arbitrary direction
            if (dist < 0.0001f)
                return new float2(0f, 1f) * maxSpeed;

            return math.normalize(away) * maxSpeed;
        }
    }
}
