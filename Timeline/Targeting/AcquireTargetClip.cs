using BovineLabs.Combat.Core;
using Unity.Entities;

namespace BovineLabs.Combat.Timeline.Targeting
{
    public struct AcquireTargetClipData : IComponentData
    {
        public int SlotId;
        public float MaxRange;
        public TargetRelation RequiredRelation;
    }
}
