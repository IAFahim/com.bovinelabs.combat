using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Steering
{
    /// <summary>
    /// Pursue: Predictive interception of a moving target.
    /// Aims at where the target WILL BE based on its velocity and distance.
    /// Falls back to Seek if target is stationary or prediction is uncertain.
    /// </summary>
    public struct PursueTarget : IComponentData, IEnableableComponent
    {
        /// <summary>Current position of the target.</summary>
        public float2 TargetPosition;

        /// <summary>Current velocity of the target (XZ).</summary>
        public float2 TargetVelocity;

        /// <summary>Maximum look-ahead time in seconds.</summary>
        public float MaxPrediction;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct PursueSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (pursue, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<PursueTarget>,
                    RefRO<MovementStats>,
                    RefRO<Unity.Transforms.LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;

                steering.ValueRW.Linear = SteeringMath.Pursue(
                    currentPos, stats.ValueRO.Velocity,
                    pursue.ValueRO.TargetPosition, pursue.ValueRO.TargetVelocity,
                    stats.ValueRO.MaxSpeed, pursue.ValueRO.MaxPrediction);

                steering.ValueRW.Priority = 1f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Pursue;
            }
        }
    }
}
