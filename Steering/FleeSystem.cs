using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Steering
{
    /// <summary>
    /// Flee: Move away from a fixed threat position.
    /// Inverse of Seek - desired velocity points away from the threat.
    /// </summary>
    public struct FleeFrom : IComponentData, IEnableableComponent
    {
        /// <summary>Position of the threat to flee from.</summary>
        public float2 Position;

        /// <summary>Panic radius - only flee if within this distance.</summary>
        public float PanicRadius;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct FleeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (flee, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<FleeFrom>,
                    RefRO<MovementStats>,
                    RefRO<Unity.Transforms.LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;
                var threatPos = flee.ValueRO.Position;
                var dist = math.distance(currentPos, threatPos);

                // Only flee if within panic radius (or panic radius = 0 = always flee)
                if (flee.ValueRO.PanicRadius > 0f && dist > flee.ValueRO.PanicRadius)
                {
                    steering.ValueRW = SteeringForce.Zero;
                    continue;
                }

                steering.ValueRW.Linear = SteeringMath.Flee(currentPos, threatPos, stats.ValueRO.MaxSpeed);
                steering.ValueRW.Priority = 2f; // Flee is higher priority than Seek
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Flee;
            }
        }
    }
}
