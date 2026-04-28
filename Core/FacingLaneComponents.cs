using BovineLabs.Timeline.Data;
using Unity.Entities;
using Unity.Properties;

namespace BovineLabs.Combat.Core
{
    public struct FacingAnimated : IAnimatedComponent<FacingData>
    {
        private FacingData data;

        [CreateProperty]
        public FacingData Value { get => data; set => data = value; }
    }
}
