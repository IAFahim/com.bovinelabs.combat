using Unity.Entities;

namespace BovineLabs.Combat.Timeline.Events
{
    public struct HitboxWindowClipData : IComponentData
    {
        public int HitboxId;
        public bool IsOpen;
    }
}
