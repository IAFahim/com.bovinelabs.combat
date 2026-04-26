using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Formation
{
    /// <summary>
    /// Formation leader component. Attached to the entity that serves as the formation center.
    /// The formation is positioned relative to the reference entity (usually the leader itself).
    /// </summary>
    public struct FormationLeader : IComponentData, IEnableableComponent
    {
        /// <summary>Reference entity for the formation center. Typically Entity.Null means self.</summary>
        public Entity ReferenceEntity;

        /// <summary>Offset from the reference entity's position on the XZ plane.</summary>
        public float2 FormationOffset;

        public static FormationLeader Default => new()
        {
            ReferenceEntity = Entity.Null,
            FormationOffset = float2.zero,
        };
    }

    /// <summary>
    /// Formation configuration. Defines the shape and parameters of the formation.
    /// </summary>
    public struct FormationConfig : IComponentData
    {
        /// <summary>Type of formation pattern (Line, Wedge, Grid, Circle, Column, V).</summary>
        public Core.FormationType Type;

        /// <summary>Spacing between units in the formation.</summary>
        public float Spacing;

        /// <summary>Number of columns for grid formation.</summary>
        public int Columns;

        /// <summary>Wedge/V formation half-angle in radians.</summary>
        public float Angle;

        /// <summary>Circle formation radius multiplier (1.0 = spacing-based).</summary>
        public float CircleRadius;

        public static FormationConfig Default => new()
        {
            Type = Core.FormationType.Line,
            Spacing = 2f,
            Columns = 4,
            Angle = math.PI / 6f,
            CircleRadius = 5f,
        };
    }

    /// <summary>
    /// Formation slot assignment. Written by FormationSystem for each follower entity.
    /// Stores the computed target world position for the assigned slot.
    /// </summary>
    public struct FormationSlotAssignment : IComponentData, IEnableableComponent
    {
        /// <summary>Index of the assigned slot in the formation.</summary>
        public int SlotIndex;

        /// <summary>Computed target world position on XZ plane.</summary>
        public float2 TargetWorldPos;

        public static FormationSlotAssignment Default => new()
        {
            SlotIndex = -1,
            TargetWorldPos = float2.zero,
        };
    }
}
