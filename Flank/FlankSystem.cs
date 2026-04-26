using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Flank
{
    /// <summary>
    /// Flank behavior system.
    /// Computes a position beside/behind the target (based on target's facing direction)
    /// and seeks toward it. Uses the FlankMath utilities to calculate the ideal flank
    /// position and generate a steering force toward it.
    /// 
    /// Priority is moderate (1.5) - higher than basic seek but lower than charge,
    /// since flanking is a tactical positioning behavior.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct FlankSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FlankTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (flank, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<FlankTarget>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;

                // Compute where we want to be: beside/behind the target
                var flankPos = FlankMath.ComputeFlankPosition(
                    flank.ValueRO.TargetPos,
                    flank.ValueRO.TargetFacingAngle,
                    flank.ValueRO.FlankAngleOffset,
                    flank.ValueRO.FlankDistance);

                // Steer toward the flank position
                var flankForce = FlankMath.ComputeFlankDirection(
                    currentPos,
                    flankPos,
                    stats.ValueRO.MaxSpeed);

                steering.ValueRW.Linear = flankForce;
                steering.ValueRW.Priority = 1.5f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Flank;
            }
        }
    }
}
