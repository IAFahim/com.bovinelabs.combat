using Unity.Entities;

namespace BovineLabs.Combat.Timeline.Events
{
    public struct CombatEventClipData : IComponentData
    {
        public int EventId;
        public float Parameter;
    }
}
