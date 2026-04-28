using Unity.Entities;

namespace BovineLabs.Combat.Core
{
    public struct CombatControlMask : IComponentData
    {
        public byte Value;

        public bool DisableInput => (Value & 1) != 0;
        public bool DisableBrain => (Value & 2) != 0;
        public bool DisableTurn => (Value & 4) != 0;
        public bool DisableAvoidance => (Value & 8) != 0;
        public bool HasAny => Value != 0;
    }
}
