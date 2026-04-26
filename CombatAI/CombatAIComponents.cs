using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.CombatAI
{
    /// <summary>
    /// Engagement rules that govern when the AI transitions between combat states.
    /// </summary>
    public struct EngagementRules : IComponentData
    {
        /// <summary>Distance at which the agent begins engaging enemies.</summary>
        public float EngageRange;

        /// <summary>Distance at which the agent stops pursuing and disengages.</summary>
        public float DisengageRange;

        /// <summary>Health ratio (0..1) below which the agent flees.</summary>
        public float FleeHealthThreshold;

        /// <summary>Range within which the agent calls for help from allies.</summary>
        public float CallForHelpRange;

        public static EngagementRules Default => new()
        {
            EngageRange = 15f,
            DisengageRange = 25f,
            FleeHealthThreshold = 0.25f,
            CallForHelpRange = 10f,
        };
    }

    /// <summary>
    /// Current state of the combat AI state machine.
    /// </summary>
    public struct CombatAIState : IComponentData
    {
        /// <summary>Current AI state.</summary>
        public AIState State;

        /// <summary>Time spent in the current state (seconds).</summary>
        public float StateTimer;

        /// <summary>Entity of the current engagement target (if any).</summary>
        public Entity EngagementTarget;

        public static CombatAIState Default => new()
        {
            State = AIState.Idle,
            StateTimer = 0f,
            EngagementTarget = Entity.Null,
        };
    }

    /// <summary>
    /// States in the combat AI state machine.
    /// </summary>
    public enum AIState : byte
    {
        /// <summary>No combat activity. Scanning for threats.</summary>
        Idle = 0,

        /// <summary>Actively pursuing/attacking a target.</summary>
        Engaging = 1,

        /// <summary>Running away from threats due to low health.</summary>
        Fleeing = 2,

        /// <summary>Following a leader or ally formation.</summary>
        Following = 3,
    }
}
