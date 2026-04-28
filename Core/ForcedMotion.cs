using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public enum ForcedMotionMode : byte
    {
        Impulse,
        VelocityOverride,
        PullToPosition,
        Freeze,
        Grabbed,
    }

    [System.Flags]
    public enum ForcedMotionFlags : ushort
    {
        None = 0,
        ZeroCurrentVelocity = 1 << 0,
        Additive = 1 << 1,
        SuppressAttackMotion = 1 << 2,
        SuppressLocomotion = 1 << 3,
        SuppressNavigation = 1 << 4,
        LockFacing = 1 << 5,
        DisableInput = 1 << 6,
        DisableBrain = 1 << 7,
    }

    public struct ForcedMotionRequest : IBufferElementData
    {
        public ForcedMotionMode Mode;
        public float3 Vector;
        public float3 TargetPosition;
        public float Duration;
        public float Damping;
        public float Strength;
        public ForcedMotionFlags Flags;
    }

    public struct ForcedMotionState : IComponentData, IEnableableComponent
    {
        public ForcedMotionMode ActiveMode;
        public float3 ActiveVector;
        public float RemainingTime;
        public float Damping;
        public ForcedMotionFlags ActiveFlags;

        public bool IsActive => RemainingTime > 0f;

        public static ForcedMotionState Inactive => new() { RemainingTime = 0f };
    }
}
