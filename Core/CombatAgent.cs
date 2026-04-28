using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public struct CombatAgent : IComponentData
    {
    }

    public struct CombatAgentProfile : IComponentData
    {
        public float DefaultMaxSpeed;
        public float DefaultMaxAcceleration;
        public float DefaultTurnSpeed;
        public float DefaultRadius;
        public float SensorRadius;
        public int MaxSensedTargets;

        public static CombatAgentProfile Default => new()
        {
            DefaultMaxSpeed = 5f,
            DefaultMaxAcceleration = 20f,
            DefaultTurnSpeed = math.PI * 2f,
            DefaultRadius = 0.5f,
            SensorRadius = 15f,
            MaxSensedTargets = 8,
        };
    }
}
