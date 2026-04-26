using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Surround
{
    /// <summary>
    /// Surround target component: defines the target to surround and the agent's slot.
    /// Agents distribute evenly around the target at surroundRadius.
    /// </summary>
    public struct SurroundTarget : IComponentData, IEnableableComponent
    {
        /// <summary>Center position of the target to surround on the XZ plane.</summary>
        public float2 TargetPos;

        /// <summary>Radius of the surround circle around the target.</summary>
        public float SurroundRadius;

        /// <summary>This agent's assigned slot index in the surround formation.</summary>
        public int SlotIndex;

        /// <summary>Total number of surround slots (set by system based on agent count).</summary>
        public int TotalSlots;

        public static SurroundTarget Default => new()
        {
            TargetPos = float2.zero,
            SurroundRadius = 5f,
            SlotIndex = 0,
            TotalSlots = 1,
        };
    }

    /// <summary>
    /// Surround slot assignment: written by the system to tell each agent
    /// its assigned world position in the surround formation.
    /// </summary>
    public struct SurroundAssignment : IComponentData
    {
        /// <summary>Agent identifier for slot coordination.</summary>
        public int AgentId;

        /// <summary>The assigned world position this agent should seek to.</summary>
        public float2 AssignedPosition;

        public static SurroundAssignment Default => new()
        {
            AgentId = 0,
            AssignedPosition = float2.zero,
        };
    }
}
