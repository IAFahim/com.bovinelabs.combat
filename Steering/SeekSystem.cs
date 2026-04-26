using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Steering
{
    /// <summary>
    /// Seek: Move toward a fixed target position.
    /// Produces a SteeringForce with desired velocity = direction to target * maxSpeed.
    /// Classic Reynolds seek - no deceleration, arrives at full speed (use Arrive for that).
    /// </summary>
    public struct SeekTarget : IComponentData, IEnableableComponent
    {
        /// <summary>Target position on XZ plane.</summary>
        public float2 Position;
    }

    /// <summary>
    /// Seek behavior system.
    /// For each entity with SeekTarget + MovementStats + LocalTransform,
    /// computes a steering force toward the target.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct SeekSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (seek, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<SeekTarget>,
                    RefRO<MovementStats>,
                    RefRO<Unity.Transforms.LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;
                var targetPos = seek.ValueRO.Position;

                steering.ValueRW.Linear = SteeringMath.Seek(currentPos, targetPos, stats.ValueRO.MaxSpeed);
                steering.ValueRW.Priority = 1f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Seek;
            }
        }
    }
}
