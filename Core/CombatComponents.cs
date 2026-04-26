using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Team identification. Agents on the same team don't attack each other.
    /// Team 0 = neutral/unassigned.
    /// </summary>
    public struct TeamId : IComponentData
    {
        public int Value;

        public static implicit operator int(TeamId id) => id.Value;
        public static implicit operator TeamId(int v) => new() { Value = v };

        public bool IsEnemyTo(TeamId other) => Value != 0 && other.Value != 0 && Value != other.Value;
        public bool IsAllyTo(TeamId other) => Value != 0 && Value == other.Value;
    }

    /// <summary>
    /// Unique agent identifier for spatial queries and formation slot assignment.
    /// </summary>
    public struct AgentId : IComponentData
    {
        public int Value;

        public static implicit operator int(AgentId id) => id.Value;
        public static implicit operator AgentId(int v) => new() { Value = v };
    }

    /// <summary>
    /// Current target entity for combat behaviors.
    /// When Entity == Entity.Null, agent has no target.
    /// </summary>
    public struct CombatTarget : IComponentData
    {
        public Entity Entity;
        public float2 LastKnownPosition;
        public float LastSeenTime;

        public bool HasTarget => Entity != Entity.Null;

        public static CombatTarget None => new() { Entity = Entity.Null };
    }

    /// <summary>
    /// Combat health component. Used by TargetSelection for threat assessment.
    /// </summary>
    public struct CombatHealth : IComponentData
    {
        public float Current;
        public float Max;

        public float Ratio => Max > 0 ? Current / Max : 0f;
        public bool IsAlive => Current > 0f;
        public bool IsDead => Current <= 0f;
    }

    /// <summary>
    /// Threat score used by CombatAI for engagement decisions.
    /// Higher = more threatening to this agent.
    /// </summary>
    public struct ThreatScore : IComponentData
    {
        public float Value;
        public float FleeThreshold;
        public float FightThreshold;

        public bool ShouldFlee => Value >= FleeThreshold;
        public bool ShouldFight => Value <= FightThreshold;
    }

    /// <summary>
    /// Reference position for Guard behavior - the post to defend.
    /// </summary>
    public struct GuardPost : IComponentData
    {
        public float2 Position;
        public float EngagementRadius;
        public float ReturnRadius;
    }

    /// <summary>
    /// Formation slot assignment. Formation system writes the target position.
    /// </summary>
    public struct FormationSlot : IComponentData
    {
        public int SlotIndex;
        public float2 TargetPosition;
        public FormationType Type;
    }

    /// <summary>
    /// Formation patterns supported by the Formation module.
    /// </summary>
    public enum FormationType : byte
    {
        Line = 0,
        Wedge = 1,
        Grid = 2,
        Circle = 3,
        Column = 4,
        V = 5,
    }
}
