using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Ambush
{
    /// <summary>
    /// Ambush position and trigger parameters.
    /// The agent seeks to hidePosition, waits until hidden, then springs when
    /// an enemy enters triggerRadius.
    /// </summary>
    public struct AmbushPosition : IComponentData, IEnableableComponent
    {
        /// <summary>XZ position where the agent hides.</summary>
        public float2 HidePosition;

        /// <summary>Radius around hidePosition that triggers the spring.</summary>
        public float TriggerRadius;

        /// <summary>Whether the agent has reached the hide position and is concealed.</summary>
        public bool IsHidden;

        /// <summary>Whether the agent is currently springing the ambush.</summary>
        public bool IsSpringing;

        public static AmbushPosition Default => new()
        {
            HidePosition = float2.zero,
            TriggerRadius = 5f,
            IsHidden = false,
            IsSpringing = false,
        };
    }

    /// <summary>
    /// Current state of the ambush behavior.
    /// </summary>
    public struct AmbushState : IComponentData
    {
        /// <summary>Phase of the ambush cycle.</summary>
        public AmbushPhase Phase;

        /// <summary>Target position the agent is springing toward.</summary>
        public float2 SpringTarget;

        public static AmbushState Default => new()
        {
            Phase = AmbushPhase.Hiding,
            SpringTarget = float2.zero,
        };
    }

    /// <summary>
    /// Phases of an ambush behavior cycle.
    /// </summary>
    public enum AmbushPhase : byte
    {
        /// <summary>Moving toward the hide position.</summary>
        Hiding = 0,

        /// <summary>At hide position, waiting for an enemy to enter trigger radius.</summary>
        Waiting = 1,

        /// <summary>Springing from cover toward the enemy.</summary>
        Springing = 2,
    }
}
