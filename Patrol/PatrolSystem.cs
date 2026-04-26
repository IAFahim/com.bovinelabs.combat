using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Patrol
{
    /// <summary>
    /// Patrol behavior system. Handles two patrol modes:
    /// 
    /// 1. Waypoint Patrol: Cycles through PatrolWaypoints in the entity's dynamic buffer.
    ///    The agent seeks toward the current waypoint, waits for the waypoint's WaitTime,
    ///    then advances to the next waypoint. Supports looping and non-looping paths.
    /// 
    /// 2. Area Patrol: Wanders randomly within a rectangular area defined by PatrolArea.
    ///    The agent picks random target points within bounds, moves to them, idles for
    ///    a random duration, then picks a new point. Creates natural wandering behavior.
    /// 
    /// Both modes output a SteeringForce with Patrol behavior type.
    /// Uses arrive steering for smooth waypoint approach.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct PatrolSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PatrolState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;

            // Process Waypoint Patrol entities
            foreach (var (patrolState, stats, transform, steering, waypoints) in
                SystemAPI.Query<
                    RefRW<PatrolState>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>,
                    DynamicBuffer<PatrolWaypoints>>())
            {
                var buffer = waypoints;
                var waypointCount = buffer.Length;

                if (waypointCount == 0)
                {
                    steering.ValueRW = SteeringForce.Zero;
                    continue;
                }

                var ps = patrolState.ValueRW;
                var currentPos = transform.ValueRO.Position.xz;

                // Handle waiting at current waypoint
                if (ps.IsWaiting)
                {
                    ps.WaitTimer += deltaTime;
                    var currentWaypoint = buffer[ps.CurrentWaypointIndex];

                    if (ps.WaitTimer >= currentWaypoint.WaitTime)
                    {
                        // Done waiting, advance to next waypoint
                        ps.WaitTimer = 0f;
                        ps.IsWaiting = false;
                        ps.CurrentWaypointIndex = PatrolMath.NextWaypointIndex(
                            ps.CurrentWaypointIndex, waypointCount, ps.Loop);
                    }
                    else
                    {
                        // Still waiting - output zero force
                        steering.ValueRW = SteeringForce.Zero;
                        steering.ValueRW.BehaviorType = SteeringBehaviorType.Patrol;
                        patrolState.ValueRW = ps;
                        continue;
                    }
                }

                // Clamp index to valid range
                if (ps.CurrentWaypointIndex >= waypointCount)
                    ps.CurrentWaypointIndex = waypointCount - 1;
                if (ps.CurrentWaypointIndex < 0)
                    ps.CurrentWaypointIndex = 0;

                var targetWaypoint = buffer[ps.CurrentWaypointIndex];
                var targetPos = targetWaypoint.Position.xz;

                // Check if we've arrived at the waypoint
                var distSq = math.lengthsq(targetPos - currentPos);
                var arrivalThreshold = stats.ValueRO.ArrivalThreshold;
                var arrivalThresholdSq = arrivalThreshold * arrivalThreshold;

                if (distSq <= arrivalThresholdSq)
                {
                    // Arrived at waypoint - start waiting
                    ps.IsWaiting = true;
                    ps.WaitTimer = 0f;
                    steering.ValueRW.Linear = float2.zero;
                    steering.ValueRW.Priority = 1f;
                    steering.ValueRW.Weight = 1f;
                    steering.ValueRW.BehaviorType = SteeringBehaviorType.Patrol;
                }
                else
                {
                    // Move toward waypoint with arrive steering
                    steering.ValueRW.Linear = SteeringMath.Arrive(
                        currentPos,
                        targetPos,
                        stats.ValueRO.MaxSpeed,
                        stats.ValueRO.MaxSpeed * 0.4f,
                        arrivalThreshold);
                    steering.ValueRW.Priority = 1f;
                    steering.ValueRW.Weight = 1f;
                    steering.ValueRW.BehaviorType = SteeringBehaviorType.Patrol;
                }

                patrolState.ValueRW = ps;
            }

            // Process Area Patrol entities
            foreach (var (patrolArea, areaState, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<PatrolArea>,
                    RefRW<PatrolAreaState>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;
                var area = patrolArea.ValueRO;
                var as_ = areaState.ValueRW;

                // Initialize random if needed
                if (as_.RandomSeed == 0)
                    as_.RandomSeed = (uint)(currentPos.x * 1000f + currentPos.y * 7919f) + 1;

                var rng = new Random(as_.RandomSeed);

                // Handle idle state
                if (as_.IsIdle)
                {
                    as_.IdleTimer += deltaTime;

                    // Random idle duration between min and max
                    var idleDuration = math.lerp(area.MinIdleTime, area.MaxIdleTime, rng.NextFloat());
                    as_.RandomSeed = rng.state;

                    if (as_.IdleTimer >= idleDuration)
                    {
                        // Done idling - pick a new random point
                        as_.IsIdle = false;
                        as_.IdleTimer = 0f;
                        as_.CurrentTarget = PatrolMath.RandomPointInArea(
                            area.Center, area.HalfExtents, ref rng);
                        as_.RandomSeed = rng.state;
                    }
                    else
                    {
                        // Still idling
                        steering.ValueRW = SteeringForce.Zero;
                        steering.ValueRW.BehaviorType = SteeringBehaviorType.Patrol;
                        as_.RandomSeed = rng.state;
                        areaState.ValueRW = as_;
                        continue;
                    }
                }

                // Validate target is inside area - if not, pick a new one
                if (!PatrolMath.IsInsideArea(as_.CurrentTarget, area.Center, area.HalfExtents))
                {
                    as_.CurrentTarget = PatrolMath.RandomPointInArea(
                        area.Center, area.HalfExtents, ref rng);
                    as_.RandomSeed = rng.state;
                }

                var targetPos = as_.CurrentTarget;
                var distSq = math.lengthsq(targetPos - currentPos);
                var arrivalThreshold = stats.ValueRO.ArrivalThreshold;
                var arrivalThresholdSq = arrivalThreshold * arrivalThreshold;

                if (distSq <= arrivalThresholdSq)
                {
                    // Arrived at target - go idle
                    as_.IsIdle = true;
                    as_.IdleTimer = 0f;
                    steering.ValueRW.Linear = float2.zero;
                    steering.ValueRW.Priority = 1f;
                    steering.ValueRW.Weight = 1f;
                    steering.ValueRW.BehaviorType = SteeringBehaviorType.Patrol;
                }
                else
                {
                    // Move toward target with arrive steering
                    steering.ValueRW.Linear = SteeringMath.Arrive(
                        currentPos,
                        targetPos,
                        stats.ValueRO.MaxSpeed,
                        stats.ValueRO.MaxSpeed * 0.4f,
                        arrivalThreshold);
                    steering.ValueRW.Priority = 1f;
                    steering.ValueRW.Weight = 1f;
                    steering.ValueRW.BehaviorType = SteeringBehaviorType.Patrol;
                }

                as_.RandomSeed = rng.state;
                areaState.ValueRW = as_;
            }
        }
    }
}
