using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Steering force output. Each behavior system writes its desired velocity here.
    /// The Blend module combines multiple forces into a single final velocity.
    /// All movement is on the XZ plane (Y=0).
    /// </summary>
    public struct SteeringForce : IComponentData, IEnableableComponent
    {
        /// <summary>Desired velocity on XZ plane. Magnitude = desired speed.</summary>
        public float2 Linear;

        /// <summary>Priority of this force (0 = none, higher = more important).</summary>
        public float Priority;

        /// <summary>Weight for blending (0..1 normalized by Blend system).</summary>
        public float Weight;

        /// <summary>Which behavior produced this force (for debugging).</summary>
        public SteeringBehaviorType BehaviorType;

        public static SteeringForce Zero => new()
        {
            Linear = float2.zero,
            Priority = 0f,
            Weight = 0f,
            BehaviorType = SteeringBehaviorType.None,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsZero => math.lengthsq(Linear) < 0.0001f;
    }

    /// <summary>
    /// Identifies which behavior produced a steering force.
    /// Used for debugging and priority arbitration.
    /// </summary>
    public enum SteeringBehaviorType : byte
    {
        None = 0,
        Seek = 1,
        Flee = 2,
        Arrive = 3,
        Pursue = 4,
        Evade = 5,
        Wander = 6,
        Avoidance = 7,
        ObstacleAvoidance = 8,
        Cohesion = 9,
        Separation = 10,
        Alignment = 11,
        Formation = 12,
        Follow = 13,
        Patrol = 14,
        Charge = 15,
        Flank = 16,
        Retreat = 17,
        Kite = 18,
        Surround = 19,
        Guard = 20,
        Ambush = 21,
        PathFollow = 22,
    }
}
