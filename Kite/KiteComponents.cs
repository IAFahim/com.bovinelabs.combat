using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Kite
{
    /// <summary>
    /// Kite behavior component: maintain optimal range from a target while circling.
    /// When isKiting is true, the agent stays at optimalRange from the target,
    /// moving along an arc defined by kiteArcAngle at kiteSpeed.
    /// </summary>
    public struct KiteTarget : IComponentData, IEnableableComponent
    {
        /// <summary>Target position to kite around on the XZ plane.</summary>
        public float2 TargetPos;

        /// <summary>Desired distance from the target while kiting.</summary>
        public float OptimalRange;

        /// <summary>Angular increment (radians) per kite cycle to circle around the target.</summary>
        public float KiteArcAngle;

        /// <summary>Speed while kiting (units/sec).</summary>
        public float KiteSpeed;

        /// <summary>Whether the agent is currently kiting.</summary>
        public bool IsKiting;

        /// <summary>Current kite direction: 1 = counter-clockwise, -1 = clockwise.</summary>
        public float KiteDirection;

        public static KiteTarget Default => new()
        {
            TargetPos = float2.zero,
            OptimalRange = 8f,
            KiteArcAngle = math.PI / 4f,
            KiteSpeed = 4f,
            IsKiting = false,
            KiteDirection = 1f,
        };
    }
}
