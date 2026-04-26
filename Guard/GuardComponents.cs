using Unity.Mathematics;

namespace BovineLabs.Combat.Guard
{
    /// <summary>
    /// Guard state component: tracks whether the agent is engaging an enemy
    /// or returning to its guard post.
    /// Works with the GuardPost component from Core (Position, EngagementRadius, ReturnRadius).
    /// </summary>
    public struct GuardState : IComponentData, IEnableableComponent
    {
        /// <summary>Whether the agent is currently engaging an enemy (vs returning to post).</summary>
        public bool IsEngaging;

        /// <summary>Position of the enemy being engaged on the XZ plane.</summary>
        public float2 EngageTarget;

        public static GuardState Default => new()
        {
            IsEngaging = false,
            EngageTarget = float2.zero,
        };
    }
}
