using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Steering
{
    /// <summary>
    /// Evade: Flee from a moving threat's predicted future position.
    /// Opposite of Pursue - runs away from where the threat will be.
    /// </summary>
    public struct EvadeFrom : IComponentData, IEnableableComponent
    {
        /// <summary>Current position of the threat.</summary>
        public float2 ThreatPosition;

        /// <summary>Current velocity of the threat (XZ).</summary>
        public float2 ThreatVelocity;

        /// <summary>Maximum look-ahead time in seconds.</summary>
        public float MaxPrediction;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct EvadeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (evade, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<EvadeFrom>,
                    RefRO<MovementStats>,
                    RefRO<Unity.Transforms.LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;

                steering.ValueRW.Linear = SteeringMath.Evade(
                    currentPos, stats.ValueRO.Velocity,
                    evade.ValueRO.ThreatPosition, evade.ValueRO.ThreatVelocity,
                    stats.ValueRO.MaxSpeed, evade.ValueRO.MaxPrediction);

                steering.ValueRW.Priority = 2f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Evade;
            }
        }
    }
}
