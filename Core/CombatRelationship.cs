using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public struct CombatRelationship : IComponentData
    {
        public int FactionId;
        public uint HostileCategories;  // bitmask
        public uint FriendlyCategories; // bitmask

        public bool IsHostileTo(in CombatRelationship other) =>
            (HostileCategories & (1u << other.FactionId)) != 0;

        public bool IsFriendlyTo(in CombatRelationship other) =>
            (FriendlyCategories & (1u << other.FactionId)) != 0;
    }
}
