using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Blend
{
    /// <summary>
    /// Weight entry for blending multiple steering behaviors.
    /// Attached as a dynamic buffer to agents that combine multiple forces.
    /// </summary>
    public struct BlendWeights : IBufferElementData
    {
        /// <summary>Which steering behavior this weight applies to.</summary>
        public Core.SteeringBehaviorType BehaviorType;

        /// <summary>Weight multiplier for this behavior (0..1 typical).</summary>
        public float Weight;

        /// <summary>Priority level - higher priority forces override lower ones in priority select mode.</summary>
        public float Priority;

        public static BlendWeights New(Core.SteeringBehaviorType behaviorType, float weight, float priority) => new()
        {
            BehaviorType = behaviorType,
            Weight = weight,
            Priority = priority,
        };
    }

    /// <summary>
    /// Final blended steering force output.
    /// Written by the BlendSystem after combining all active SteeringForce components.
    /// This is the authoritative velocity that movement systems should read.
    /// </summary>
    public struct FinalSteeringForce : IComponentData
    {
        /// <summary>Final blended velocity on the XZ plane.</summary>
        public float2 Velocity;

        public static FinalSteeringForce Zero => new() { Velocity = float2.zero };
    }
}
