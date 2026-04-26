using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Guard
{
    /// <summary>
    /// Guard behavior system.
    /// Agent stays near its GuardPost position. When an enemy enters EngagementRadius,
    /// the agent seeks toward the enemy. When the enemy leaves range or the agent
    /// exceeds ReturnRadius from the post, the agent returns to the post.
    ///
    /// Uses GuardPost from Core (Position, EngagementRadius, ReturnRadius) and
    /// GuardState to track engagement status.
    ///
    /// Priority = 2.0 to override standard movement but yield to charge/avoidance.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct GuardSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<GuardPost>();
            state.RequireForUpdate<GuardState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (guardPost, guardState, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<GuardPost>,
                    RefRW<GuardState>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;
                var postPos = guardPost.ValueRO.Position;
                var engagementRadius = guardPost.ValueRO.EngagementRadius;
                var returnRadius = guardPost.ValueRO.ReturnRadius;

                float2 desiredTarget;
                bool engaging;

                if (guardState.ValueRO.IsEngaging)
                {
                    var enemyPos = guardState.ValueRO.EngageTarget;

                    // Check if we should return to post (strayed too far)
                    if (GuardMath.ShouldReturn(currentPos, postPos, returnRadius))
                    {
                        // Disengage and return to post
                        guardState.ValueRW.IsEngaging = false;
                        desiredTarget = postPos;
                        engaging = false;
                    }
                    // Check if enemy left engagement radius from the POST (not agent)
                    else if (!GuardMath.ShouldEngage(postPos, enemyPos, engagementRadius))
                    {
                        // Enemy left post's engagement area - return to post
                        guardState.ValueRW.IsEngaging = false;
                        desiredTarget = postPos;
                        engaging = false;
                    }
                    else
                    {
                        // Continue engaging
                        desiredTarget = enemyPos;
                        engaging = true;
                    }
                }
                else
                {
                    // Not engaging - stay near post
                    desiredTarget = postPos;
                    engaging = false;

                    // Check if there's an engagement target to reactivate
                    if (GuardMath.ShouldEngage(currentPos, guardState.ValueRO.EngageTarget, engagementRadius))
                    {
                        guardState.ValueRW.IsEngaging = true;
                        desiredTarget = guardState.ValueRO.EngageTarget;
                        engaging = true;
                    }
                }

                // Compute steering force toward the desired target
                var toTarget = desiredTarget - currentPos;
                var dist = math.length(toTarget);

                if (dist <= stats.ValueRO.ArrivalThreshold)
                    continue;

                // Use arrive behavior for smooth deceleration
                var maxSpeed = stats.ValueRO.MaxSpeed;
                var slowRadius = engaging ? engagementRadius * 0.3f : returnRadius * 0.3f;
                var desiredSpeed = dist <= slowRadius && slowRadius > 0.0001f
                    ? maxSpeed * (dist / slowRadius)
                    : maxSpeed;

                var force = math.normalizesafe(toTarget) * desiredSpeed;

                steering.ValueRW.Linear = force;
                steering.ValueRW.Priority = 2f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Guard;
            }
        }
    }
}
