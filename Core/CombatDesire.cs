using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public enum CombatDesireType : byte
    {
        None,
        Engage,
        Attack,
        Flee,
        MaintainDistance,
        Reposition,
        AssistAlly,
        DefendPoint,
        RetreatToAnchor,
    }

    [System.Flags]
    public enum CombatDesireFlags : ushort
    {
        None = 0,
        Urgent = 1 << 0,
        RequiresTarget = 1 << 1,
        RequiresPosition = 1 << 2,
        CancelCurrentAction = 1 << 3,
    }

    public struct CombatDesire : IBufferElementData
    {
        public CombatDesireType Type;
        public Entity Target;
        public float3 Position;
        public float Score;
        public CombatDesireFlags Flags;
    }
}
