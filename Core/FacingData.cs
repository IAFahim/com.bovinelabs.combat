using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public enum FacingMode : byte
    {
        None,
        FaceMovement,
        FaceTarget,
        FaceDirection,
        FacePosition,
        LockCurrent,
    }

    [System.Flags]
    public enum FacingFlags : ushort
    {
        None = 0,
        LockFacing = 1 << 0,
        DisableTurn = 1 << 1,
        UseCurrentTarget = 1 << 2,
    }

    public struct FacingData
    {
        public FacingMode Mode;
        public Entity Target;
        public float3 Direction;
        public float3 Position;
        public float TurnSpeedScale;
        public float AngularDampingScale;
        public FacingFlags Flags;

        public static FacingData None => new()
        {
            Mode = FacingMode.None,
            TurnSpeedScale = 1f,
            AngularDampingScale = 1f,
        };
    }
}
