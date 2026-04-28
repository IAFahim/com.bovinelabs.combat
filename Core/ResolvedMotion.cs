using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Identifies which source produced the resolved combat motion.
    /// Used for debugging and priority arbitration.
    /// </summary>
    public enum CombatMotionSource : byte
    {
        None = 0,
        Forced = 1,
        Attack = 2,
        Locomotion = 3,
        Navigation = 4,
        Idle = 5,
    }

    /// <summary>
    /// Final resolved motion after all combat motion lanes have been blended.
    /// Written by the motion resolve system; consumed by animation / movement drivers.
    /// </summary>
    public struct ResolvedMotion : IComponentData
    {
        /// <summary>The blended combat motion data.</summary>
        public CombatMotionData Motion;

        /// <summary>Which source won priority arbitration.</summary>
        public CombatMotionSource Source;
    }
}
