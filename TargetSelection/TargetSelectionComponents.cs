using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.TargetSelection
{
    /// <summary>
    /// Parameters controlling how an agent selects its combat target.
    /// </summary>
    public struct TargetSelectionParams : IComponentData
    {
        /// <summary>Maximum range at which a target can be selected.</summary>
        public float MaxRange;

        /// <summary>Maximum angle (radians, half-cone) for frontal cone selection. PI = omnidirectional.</summary>
        public float MaxAngle;

        /// <summary>Strategy used to pick the best target.</summary>
        public SelectionStrategy Strategy;

        public static TargetSelectionParams Default => new()
        {
            MaxRange = 20f,
            MaxAngle = math.PI,
            Strategy = SelectionStrategy.Nearest,
        };
    }

    /// <summary>
    /// Strategies for selecting a combat target from multiple candidates.
    /// </summary>
    public enum SelectionStrategy : byte
    {
        /// <summary>Pick the closest enemy within range.</summary>
        Nearest = 0,

        /// <summary>Pick the enemy with the lowest current health.</summary>
        Weakest = 1,

        /// <summary>Pick the enemy with the highest threat score.</summary>
        MostThreatening = 2,
    }
}
