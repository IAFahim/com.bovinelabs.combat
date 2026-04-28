using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public struct CombatBrainProfile : IComponentData
    {
        public float EngageRange;
        public float AttackRange;
        public float FleeHealthThreshold;
        public float MaintainDistanceRange;
        public float Aggression;
        public float ThreatSensitivity;
        public float UpdateInterval;
        public float TimeSinceLastUpdate;

        public static CombatBrainProfile Default => new CombatBrainProfile
        {
            EngageRange = 10f,
            AttackRange = 2f,
            FleeHealthThreshold = 0.25f,
            MaintainDistanceRange = 5f,
            Aggression = 0.5f,
            ThreatSensitivity = 1f,
            UpdateInterval = 0.5f,
            TimeSinceLastUpdate = 0f,
        };
    }
}
