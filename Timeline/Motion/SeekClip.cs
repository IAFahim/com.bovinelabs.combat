using BovineLabs.Timeline.Data;
using BovineLabs.Combat.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;

namespace BovineLabs.Combat.Timeline.Motion
{
    public struct SeekClipAnimated : IAnimatedComponent<CombatMotionData>
    {
        private CombatMotionData data;

        [CreateProperty]
        public CombatMotionData Value { get => data; set => data = value; }
    }
}
