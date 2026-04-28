using Unity.Entities;

namespace BovineLabs.Combat.Timeline.Locks
{
    public struct SuperArmorClipData : IComponentData
    {
        public float Duration;
        public float DamageReduction;
    }
}
