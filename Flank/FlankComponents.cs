using Unity.Mathematics;

namespace BovineLabs.Combat.Flank
{
    /// <summary>
    /// Flank behavior component: seek to a position beside/behind a target.
    /// Computes a flank position offset from the target's facing direction,
    /// then steers toward that position.
    /// </summary>
    public struct FlankTarget : IComponentData, IEnableableComponent
    {
        /// <summary>Current position of the target to flank.</summary>
        public float2 TargetPos;

        /// <summary>Direction the target is facing (radians, 0 = +Z).</summary>
        public float TargetFacingAngle;

        /// <summary>
        /// Angle offset from the target's facing direction for the flank position.
        /// Positive = clockwise offset, negative = counter-clockwise.
        /// Example: PI/2 = directly to the right, PI = directly behind.
        /// </summary>
        public float FlankAngleOffset;

        /// <summary>Distance from the target to maintain while flanking.</summary>
        public float FlankDistance;

        public static FlankTarget Default => new()
        {
            TargetPos = float2.zero,
            TargetFacingAngle = 0f,
            FlankAngleOffset = math.PI * 0.75f, // ~135 degrees - behind and to the side
            FlankDistance = 3f,
        };
    }
}
