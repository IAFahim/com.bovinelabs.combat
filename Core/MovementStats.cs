using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Core movement statistics for any combat agent.
    /// Attached to every entity that can move.
    /// </summary>
    public struct MovementStats : IComponentData
    {
        /// <summary>Maximum movement speed (units/sec).</summary>
        public float MaxSpeed;

        /// <summary>Maximum acceleration (units/sec^2). 0 = instant.</summary>
        public float MaxAcceleration;

        /// <summary>Maximum turning speed (radians/sec). 0 = instant.</summary>
        public float MaxTurnSpeed;

        /// <summary>Agent radius for collision/avoidance.</summary>
        public float Radius;

        /// <summary>Distance at which agent considers itself "at" a point.</summary>
        public float ArrivalThreshold;

        /// <summary>Current velocity on XZ plane.</summary>
        public float2 Velocity;

        /// <summary>Current facing angle (radians, 0 = +Z).</summary>
        public float FacingAngle;

        public static MovementStats Default => new()
        {
            MaxSpeed = 5f,
            MaxAcceleration = 20f,
            MaxTurnSpeed = math.PI * 2f,
            Radius = 0.5f,
            ArrivalThreshold = 0.1f,
            Velocity = float2.zero,
            FacingAngle = 0f,
        };
    }
}
