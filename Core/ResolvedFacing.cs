using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public struct ResolvedFacing : IComponentData
    {
        public FacingData Value;

        public static ResolvedFacing None => new() { Value = FacingData.None };
    }
}
