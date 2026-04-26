using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Follow
{
    /// <summary>
    /// Follow leader component. Entity follows behind a designated leader at a given distance/offset.
    /// Uses arrive steering to smoothly move to the follow position.
    /// </summary>
    public struct FollowLeader : IComponentData, IEnableableComponent
    {
        /// <summary>The entity to follow.</summary>
        public Entity Leader;

        /// <summary>Distance to maintain behind the leader.</summary>
        public float FollowDistance;

        /// <summary>Lateral offset from directly behind the leader (positive = right).</summary>
        public float2 FollowOffset;

        public static FollowLeader Default => new()
        {
            Leader = Entity.Null,
            FollowDistance = 3f,
            FollowOffset = float2.zero,
        };
    }

    /// <summary>
    /// Follow chain component. Entity follows behind a specific entity in a chain formation.
    /// chainDepth indicates position in the chain (0 = directly behind the leader).
    /// </summary>
    public struct FollowChain : IComponentData, IEnableableComponent
    {
        /// <summary>The entity directly ahead of this one in the chain.</summary>
        public Entity AheadOfMe;

        /// <summary>Depth in the chain (0 = first follower behind leader).</summary>
        public int ChainDepth;

        /// <summary>Distance to maintain behind the entity ahead.</summary>
        public float FollowDistance;

        public static FollowChain Default => new()
        {
            AheadOfMe = Entity.Null,
            ChainDepth = 0,
            FollowDistance = 2f,
        };
    }
}
