using BovineLabs.Timeline.Data;
using Unity.Entities;
using Unity.Properties;

namespace BovineLabs.Combat.Core
{
    public struct AttackMotionAnimated : IAnimatedComponent<CombatMotionData>
    {
        private CombatMotionData data;

        [CreateProperty]
        public CombatMotionData Value { get => data; set => data = value; }
    }

    public struct LocomotionAnimated : IAnimatedComponent<CombatMotionData>
    {
        private CombatMotionData data;

        [CreateProperty]
        public CombatMotionData Value { get => data; set => data = value; }
    }

    public struct NavigationAnimated : IAnimatedComponent<CombatMotionData>
    {
        private CombatMotionData data;

        [CreateProperty]
        public CombatMotionData Value { get => data; set => data = value; }
    }

    public struct AvoidanceAnimated : IAnimatedComponent<CombatMotionData>
    {
        private CombatMotionData data;

        [CreateProperty]
        public CombatMotionData Value { get => data; set => data = value; }
    }
}
