using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Raised when a hit lands on a target.
    /// </summary>
    public struct CombatHitEvent : IComponentData
    {
        public Entity Attacker;
        public Entity Victim;
        public float3 HitPosition;
        public float3 HitDirection;
        public float Damage;
        public int HitboxId;
    }

    /// <summary>
    /// Cleanup event raised when an entity dies.
    /// </summary>
    public struct CombatDeathEvent : IBufferElementData
    {
        public Entity Entity;
        public Entity Killer;
        public float3 DeathPosition;
    }

    /// <summary>
    /// Super armor grants temporary resistance to hit reactions during certain actions.
    /// Can be enabled/disabled via IEnableableComponent.
    /// </summary>
    public struct SuperArmor : IComponentData, IEnableableComponent
    {
        public float RemainingDuration;
        public float DamageReduction; // 0-1 range, 1 = full damage reduction

        public bool IsArmorActive => RemainingDuration > 0f;
    }

    /// <summary>
    /// Tracks which combat-related systems are locked for an entity.
    /// </summary>
    public struct CombatLockState : IComponentData
    {
        public bool InputLocked;
        public bool BrainLocked;
        public bool TurnLocked;
        public bool AvoidanceLocked;

        public bool IsAnyLocked => InputLocked || BrainLocked || TurnLocked || AvoidanceLocked;
    }
}
