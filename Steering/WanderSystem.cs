using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Steering
{
    /// <summary>
    /// Wander: Random exploration movement.
    /// Projects a wander circle ahead of the agent and picks a random point on it.
    /// Creates natural-looking, aimless movement patterns.
    /// </summary>
    public struct WanderState : IComponentData, IEnableableComponent
    {
        /// <summary>Current wander angle (radians).</summary>
        public float WanderAngle;

        /// <summary>Radius of the wander circle ahead of the agent.</summary>
        public float WanderRadius;

        /// <summary>Distance ahead of the agent to place the circle center.</summary>
        public float WanderDistance;

        /// <summary>Random jitter magnitude per frame.</summary>
        public float Jitter;

        /// <summary>Random seed. Must be unique per agent.</summary>
        public uint RandomSeed;
    }

    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct WanderSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (wander, stats, transform, steering) in
                SystemAPI.Query<
                    RefRW<WanderState>,
                    RefRO<MovementStats>,
                    RefRO<Unity.Transforms.LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var rng = new Unity.Mathematics.Random(wander.ValueRO.RandomSeed);
                var facingAngle = stats.ValueRO.FacingAngle;

                var force = SteeringMath.Wander(
                    facingAngle,
                    wander.ValueRO.WanderRadius,
                    wander.ValueRO.WanderDistance,
                    wander.ValueRO.Jitter,
                    ref rng,
                    out var newAngle);

                // Update wander angle and random seed for next frame
                wander.ValueRW.WanderAngle = newAngle;
                wander.ValueRW.RandomSeed = rng.state;

                steering.ValueRW.Linear = force;
                steering.ValueRW.Priority = 0.5f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Wander;
            }
        }
    }
}
