using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Navigation
{
    /// <summary>
    /// PathFollowSystem: Advances along a polyline path corridor.
    /// For each agent with a PathCorridor + PathWaypoint buffer:
    ///   1. Check if current waypoint is reached
    ///   2. If so, advance to next waypoint
    ///   3. Output a SteeringForce toward the current waypoint
    ///   4. Mark IsComplete when final waypoint is reached
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(CombatSteeringGroup))]
    public partial struct PathFollowSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (corridor, waypoints, stats, transform, steering) in
                SystemAPI.Query<
                    RefRW<PathCorridor>,
                    DynamicBuffer<PathWaypoint>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>()
                .WithAll<PathRequest>())
            {
                if (corridor.ValueRO.IsComplete || waypoints.IsEmpty)
                {
                    corridor.ValueRW.IsComplete = true;
                    steering.ValueRW = SteeringForce.Zero;
                    continue;
                }

                var currentPos = transform.ValueRO.Position.xz;
                var idx = corridor.ValueRO.CurrentWaypointIndex;

                // Clamp index to valid range
                if (idx >= waypoints.Length)
                {
                    corridor.ValueRW.IsComplete = true;
                    steering.ValueRW = SteeringForce.Zero;
                    continue;
                }

                var targetWaypoint = waypoints[idx].Position.xz;
                var distToWaypoint = math.distance(currentPos, targetWaypoint);

                // Advance waypoints if we're close enough
                while (distToWaypoint < corridor.ValueRO.WaypointArrivalThreshold && idx < waypoints.Length - 1)
                {
                    idx++;
                    targetWaypoint = waypoints[idx].Position.xz;
                    distToWaypoint = math.distance(currentPos, targetWaypoint);
                }

                corridor.ValueRW.CurrentWaypointIndex = idx;

                // Check if we've reached the final waypoint
                if (idx >= waypoints.Length - 1 && distToWaypoint < corridor.ValueRO.WaypointArrivalThreshold)
                {
                    corridor.ValueRW.IsComplete = true;
                    steering.ValueRW = SteeringForce.Zero;
                    continue;
                }

                // Arrive at current waypoint (decelerate near end of path)
                var isFinalWaypoint = idx >= waypoints.Length - 1;
                var slowRadius = isFinalWaypoint ? stats.ValueRO.MaxSpeed * 0.5f : 0f;

                var desired = isFinalWaypoint
                    ? SteeringMath.Arrive(currentPos, targetWaypoint, stats.ValueRO.MaxSpeed, slowRadius, corridor.ValueRO.WaypointArrivalThreshold)
                    : SteeringMath.Seek(currentPos, targetWaypoint, stats.ValueRO.MaxSpeed);

                steering.ValueRW.Linear = desired;
                steering.ValueRW.Priority = 1f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.PathFollow;
            }
        }
    }
}
