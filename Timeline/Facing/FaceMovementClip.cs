using BovineLabs.Timeline.Data;
using BovineLabs.Combat.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Properties;

namespace BovineLabs.Combat.Timeline.Facing
{
    public struct FaceMovementClipAnimated : IAnimatedComponent<FacingData>
    {
        private FacingData data;

        [CreateProperty]
        public FacingData Value { get => data; set => data = value; }
    }
}
