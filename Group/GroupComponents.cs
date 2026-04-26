using Unity.Mathematics;

namespace BovineLabs.Combat.Group
{
    /// <summary>
    /// Search radius for finding group mates.
    /// Only agents within this radius are considered neighbors for group behaviors.
    /// </summary>
    public struct GroupNeighborRadius : IComponentData, IEnableableComponent
    {
        /// <summary>Radius to search for fellow group members.</summary>
        public float Value;

        public static implicit operator float(GroupNeighborRadius r) => r.Value;
        public static implicit operator GroupNeighborRadius(float v) => new() { Value = v };
    }

    /// <summary>
    /// Weights for the three group steering forces: cohesion, separation, alignment.
    /// These control the relative strength of each force in the final summed output.
    /// </summary>
    public struct GroupWeights : IComponentData, IEnableableComponent
    {
        /// <summary>Cohesion weight: pull toward group center.</summary>
        public float Cohesion;

        /// <summary>Separation weight: push away from close neighbors.</summary>
        public float Separation;

        /// <summary>Alignment weight: match neighbor velocities.</summary>
        public float Alignment;

        public static GroupWeights Default => new()
        {
            Cohesion = 1f,
            Separation = 1.5f,
            Alignment = 1f,
        };
    }
}
