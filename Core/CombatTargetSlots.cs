using Unity.Entities;

namespace BovineLabs.Combat.Core
{
    public struct TargetSlot : IBufferElementData
    {
        public Entity Entity;
        public int SlotId; // 0=primary, 1=secondary, 2=lastHitter, etc.
    }

    public struct CombatTargets : IComponentData { }
}
