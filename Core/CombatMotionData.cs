using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public enum CombatMotionMode : byte
    {
        None,
        DesiredVelocity,
        DesiredDirection,
        ArriveAtPosition,
        MaintainDistance,
        Stop,
        HoldPosition,
    }

    [System.Flags]
    public enum CombatMotionFlags : ushort
    {
        None = 0,
        PreserveYVelocity = 1 << 0,
        IgnoreNavigation = 1 << 1,
        IgnoreAvoidance = 1 << 2,
        UseCurrentTarget = 1 << 3,
        AllowSlide = 1 << 4,
        HardBrakeOnEnd = 1 << 5,
    }

    public struct CombatMotionData
    {
        public CombatMotionMode Mode;

        public float3 DesiredVelocity;
        public float3 DesiredDirection;
        public float3 TargetPosition;

        public float SpeedScale;
        public float AccelerationScale;
        public float BrakeScale;

        public float ArrivalRadius;
        public float MaintainDistance;
        public float MaxContribution;

        public CombatMotionFlags Flags;

        public static CombatMotionData None => new CombatMotionData
        {
            Mode = CombatMotionMode.None,
            SpeedScale = 1f,
            AccelerationScale = 1f,
            BrakeScale = 1f,
        };
    }
}
