using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public struct TargetMemory : IComponentData
    {
        public Entity LastTarget;
        public float3 LastKnownPosition;
        public float LastSeenTime;
        public float MemoryDuration;

        public bool HasValidTarget => LastTarget != Entity.Null;

        public bool IsExpired(float currentTime) => HasValidTarget && (currentTime - LastSeenTime) > MemoryDuration;

        public static TargetMemory Default => new TargetMemory { MemoryDuration = 3f };
    }
}
