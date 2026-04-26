using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Steering
{
    /// <summary>
    /// Arrive: Move toward target with deceleration zones.
    /// Far away: full speed. Within slowRadius: proportional deceleration.
    /// Within arrivalThreshold: stop completely.
    /// </summary>
    public struct ArriveTarget : IComponentData, IEnableableComponent
    {
        /// <summary>Target position on XZ plane.</summary>
        public float2 Position;

        /// <summary>Distance at which deceleration begins.</summary>
        public float SlowRadius;

        /// <summary>Distance at which agent stops.</summary>
        public float ArrivalThreshold;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct ArriveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (arrive, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<ArriveTarget>,
                    RefRO<MovementStats>,
                    RefRO<Unity.Transforms.LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;
                var targetPos = arrive.ValueRO.Position;

                steering.ValueRW.Linear = SteeringMath.Arrive(
                    currentPos, targetPos,
                    stats.ValueRO.MaxSpeed,
                    arrive.ValueRO.SlowRadius,
                    arrive.ValueRO.ArrivalThreshold);

                steering.ValueRW.Priority = 1f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Arrive;
            }
        }
    }
}
