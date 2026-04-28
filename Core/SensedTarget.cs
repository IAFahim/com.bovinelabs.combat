using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public enum TargetRelation : byte
    {
        Unknown,
        Hostile,
        Friendly,
        Neutral,
        Self,
    }

    [System.Flags]
    public enum SensedTargetFlags : ushort
    {
        None = 0,
        InLineOfSight = 1 << 0,
        InAttackRange = 1 << 1,
        InFleeRange = 1 << 2,
        RecentlyDamagedMe = 1 << 3,
        IsBoss = 1 << 4,
        IsLowHealth = 1 << 5,
    }

    public struct SensedTarget : IBufferElementData
    {
        public Entity Entity;
        public float3 Position;
        public float DistanceSq;
        public float ThreatScore;
        public TargetRelation Relation;
        public SensedTargetFlags Flags;
    }
}
