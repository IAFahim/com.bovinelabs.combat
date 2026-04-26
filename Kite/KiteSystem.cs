using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Kite
{
    /// <summary>
    /// Kite behavior system.
    /// When isKiting is true, the agent maintains optimal distance from the target.
    /// If too close, it moves to an arc position around the target at optimalRange,
    /// incrementally circling at kiteArcAngle per cycle.
    ///
    /// Priority = 2.0 to override standard movement but yield to charge/avoidance.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct KiteSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<KiteTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (kite, stats, transform, steering) in
                SystemAPI.Query<
                    RefRW<KiteTarget>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                // Only compute force when actively kiting
                if (!kite.ValueRO.IsKiting)
                    continue;

                var currentPos = transform.ValueRO.Position.xz;
                var targetPos = kite.ValueRO.TargetPos;
                var optimalRange = kite.ValueRO.OptimalRange;
                var arcAngle = kite.ValueRO.KiteArcAngle;
                var kiteSpeed = kite.ValueRO.KiteSpeed;
                var kiteDirection = kite.ValueRO.KiteDirection;

                // Compute the desired kite position on the circle
                var kitePos = KiteMath.ComputeKitePosition(
                    currentPos,
                    targetPos,
                    optimalRange,
                    arcAngle,
                    kiteDirection);

                // Seek toward the kite position using arrive-like behavior
                var toKitePos = kitePos - currentPos;
                var distToKitePos = math.length(toKitePos);

                if (distToKitePos < stats.ValueRO.ArrivalThreshold)
                {
                    // Already at kite position - no force needed
                    continue;
                }

                // Scale speed: use kiteSpeed while circling, capped by maxSpeed
                var desiredSpeed = math.min(kiteSpeed, stats.ValueRO.MaxSpeed);
                var force = math.normalizesafe(toKitePos) * desiredSpeed;

                steering.ValueRW.Linear = force;
                steering.ValueRW.Priority = 2f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Kite;
            }
        }
    }
}
