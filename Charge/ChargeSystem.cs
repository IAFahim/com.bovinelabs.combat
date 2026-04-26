using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Charge
{
    /// <summary>
    /// Charge behavior system.
    /// When isCharging is true and the target is beyond minChargeDistance,
    /// outputs a high-priority steering force in a straight line toward the target
    /// at chargeSpeedMultiplier * MaxSpeed.
    /// 
    /// Charge has high priority (3.0) to override other steering behaviors,
    /// ensuring the agent commits to the charge direction.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct ChargeSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<ChargeTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (charge, stats, transform, steering) in
                SystemAPI.Query<
                    RefRW<ChargeTarget>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                // Only compute force when actively charging
                if (!charge.ValueRO.IsCharging)
                    continue;

                var currentPos = transform.ValueRO.Position.xz;
                var targetPos = charge.ValueRO.TargetPos;
                var minDistance = charge.ValueRO.MinChargeDistance;

                // Validate charge distance
                if (!ChargeMath.IsChargeValid(currentPos, targetPos, minDistance))
                {
                    // Too close - stop charging
                    charge.ValueRW.IsCharging = false;
                    continue;
                }

                var chargeForce = ChargeMath.ComputeChargeForce(
                    currentPos,
                    targetPos,
                    stats.ValueRO.MaxSpeed,
                    charge.ValueRO.ChargeSpeedMultiplier);

                steering.ValueRW.Linear = chargeForce;
                steering.ValueRW.Priority = 3f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Charge;
            }
        }
    }
}
