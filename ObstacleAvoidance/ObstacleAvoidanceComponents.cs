using Unity.Mathematics;

namespace BovineLabs.Combat.ObstacleAvoidance
{
    /// <summary>
    /// Parameters for obstacle avoidance steering behavior.
    /// Casts a fan of rays ahead of the agent and computes avoidance forces
    /// from detected obstacle hits.
    /// </summary>
    public struct ObstacleAvoidanceParams : IComponentData, IEnableableComponent
    {
        /// <summary>Radius around the agent to detect obstacles.</summary>
        public float DetectionRadius;

        /// <summary>Number of rays in the forward-facing fan.</summary>
        public int RaycastCount;

        /// <summary>Length of each raycast ray.</summary>
        public float RaycastLength;

        /// <summary>
        /// Strength of wall-sliding force. Higher = agent slides along walls faster
        /// rather than bouncing off.
        /// </summary>
        public float WallSlideStrength;

        public static ObstacleAvoidanceParams Default => new()
        {
            DetectionRadius = 3f,
            RaycastCount = 5,
            RaycastLength = 2f,
            WallSlideStrength = 0.8f,
        };
    }
}
