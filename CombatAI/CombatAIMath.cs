using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.CombatAI
{
    /// <summary>
    /// Pure math utility functions for combat AI decision-making.
    /// Threat assessment and engagement threshold checks.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class CombatAIMath
    {
        /// <summary>
        /// Compute a threat score for an enemy based on its attributes and distance.
        /// Higher score = more threatening.
        /// </summary>
        /// <param name="enemyHealth">Enemy's current health.</param>
        /// <param name="distance">Distance to the enemy.</param>
        /// <param name="enemySpeed">Enemy's movement speed.</param>
        /// <param name="agentHealth">Agent's current health (low health amplifies threat).</param>
        /// <returns>Threat score (higher = more dangerous).</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float ComputeThreatScore(float enemyHealth, float distance, float enemySpeed, float agentHealth)
        {
            // Base threat from enemy power (health * speed)
            var powerThreat = enemyHealth * enemySpeed * 0.1f;

            // Proximity threat (closer = more threatening, minimum distance of 1 to avoid division by zero)
            var proximityThreat = 1f / math.max(distance, 1f);

            // Vulnerability modifier: lower agent health amplifies perceived threat
            var vulnerability = 1f;
            if (agentHealth > 0f && agentHealth < 100f)
            {
                vulnerability = 1f + (1f - agentHealth / 100f);
            }

            return (powerThreat + proximityThreat * 10f) * vulnerability;
        }

        /// <summary>
        /// Determine if the agent should engage a target based on distance and engage range.
        /// </summary>
        /// <param name="agentHealth">Agent's current health. Must be alive.</param>
        /// <param name="enemyDistance">Distance to the enemy.</param>
        /// <param name="engageRange">Maximum engagement range.</param>
        /// <returns>True if the agent should engage.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldEngage(float agentHealth, float enemyDistance, float engageRange)
        {
            return agentHealth > 0f && enemyDistance <= engageRange;
        }

        /// <summary>
        /// Determine if the agent should flee based on health ratio.
        /// </summary>
        /// <param name="agentHealth">Agent's current health.</param>
        /// <param name="maxHealth">Agent's maximum health.</param>
        /// <param name="fleeThreshold">Health ratio (0..1) below which agent flees.</param>
        /// <returns>True if the agent should flee.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldFlee(float agentHealth, float maxHealth, float fleeThreshold)
        {
            if (maxHealth <= 0f)
                return true;

            var ratio = agentHealth / maxHealth;
            return ratio <= fleeThreshold;
        }

        /// <summary>
        /// Determine if the agent should disengage from combat (enemy too far away).
        /// </summary>
        /// <param name="enemyDistance">Distance to the enemy.</param>
        /// <param name="disengageRange">Distance at which to give up pursuit.</param>
        /// <returns>True if the agent should disengage.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldDisengage(float enemyDistance, float disengageRange)
        {
            return enemyDistance > disengageRange;
        }
    }
}
