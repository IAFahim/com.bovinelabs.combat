using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Guard
{
    /// <summary>
    /// Pure math utility functions for guard steering behavior.
    /// Guard agents stay near a post, engage enemies within engagementRadius,
    /// and return when enemies leave range or agent exceeds returnRadius from post.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class GuardMath
    {
        /// <summary>
        /// Determine whether the agent should start engaging an enemy.
        /// Returns true when the enemy is within engagementRadius of the agent.
        /// </summary>
        /// <param name="agentPos">Agent's current XZ position.</param>
        /// <param name="enemyPos">Enemy position on the XZ plane.</param>
        /// <param name="engagementRadius">Maximum distance at which the agent engages enemies.</param>
        /// <returns>True if the enemy is within engagement range.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldEngage(float2 agentPos, float2 enemyPos, float engagementRadius)
        {
            var distSq = math.lengthsq(enemyPos - agentPos);
            return distSq <= engagementRadius * engagementRadius;
        }

        /// <summary>
        /// Determine whether the agent should return to its guard post.
        /// Returns true when the agent is beyond returnRadius from the post,
        /// indicating it has strayed too far and should disengage.
        /// </summary>
        /// <param name="agentPos">Agent's current XZ position.</param>
        /// <param name="postPos">Guard post position on the XZ plane.</param>
        /// <param name="returnRadius">Maximum distance the agent may stray from the post.</param>
        /// <returns>True if the agent is too far from post and should return.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ShouldReturn(float2 agentPos, float2 postPos, float returnRadius)
        {
            var distSq = math.lengthsq(agentPos - postPos);
            return distSq > returnRadius * returnRadius;
        }
    }
}
