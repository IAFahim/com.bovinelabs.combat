using System.Runtime.CompilerServices;
using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.RoomTraversal
{
    /// <summary>
    /// Room traversal navigation system.
    /// When the target is in a different room, plans a route through rooms using BFS,
    /// then steers the agent toward the next door on the route.
    ///
    /// Flow:
    /// 1. Determine which room the agent is currently in
    /// 2. If target room is different, plan route through connected rooms
    /// 3. Seek toward the next door position
    /// 4. When door is reached, update current room and advance to next step
    /// 5. When in target room, seek directly to target position
    ///
    /// Outputs a SteeringForce with BehaviorType.PathFollow.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct RoomTraversalSystem : ISystem
    {
        private EntityQuery roomGraphQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RoomGraph>();
            state.RequireForUpdate<RoomTraversalTarget>();

            roomGraphQuery = state.GetEntityQuery(
                ComponentType.ReadWrite<RoomGraph>(),
                ComponentType.ReadOnly<RoomConnection>(),
                ComponentType.ReadOnly<RoomBounds>());
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (roomGraphQuery.IsEmpty)
                return;

            // Get the singleton room graph with its buffers
            var graphEntity = roomGraphQuery.GetSingletonEntity();
            var connections = state.EntityManager.GetBuffer<RoomConnection>(graphEntity);
            var roomBounds = state.EntityManager.GetBuffer<RoomBounds>(graphEntity);
            var graph = roomGraphQuery.GetSingleton<RoomGraph>();

            // Copy buffers to NativeArrays for math functions
            var connArray = connections.ToNativeArray(Allocator.Temp);
            var boundsArray = roomBounds.ToNativeArray(Allocator.Temp);

            foreach (var (traversalTarget, stats, transform, steering) in
                SystemAPI.Query<
                    RefRW<RoomTraversalTarget>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var agentPos = transform.ValueRO.Position.xz;
                var target = traversalTarget.ValueRO;

                // If no route or target, output zero force
                if (target.TargetRoomId < 0)
                {
                    steering.ValueRW = SteeringForce.Zero;
                    continue;
                }

                // Determine current room
                var currentRoom = RoomTraversalMath.FindRoomContaining(agentPos, boundsArray, boundsArray.Length);
                if (currentRoom < 0)
                    currentRoom = graph.CurrentRoomId;

                // If already in target room, seek directly to target position
                if (currentRoom == target.TargetRoomId)
                {
                    var force = SeekForce(agentPos, target.TargetPosition, stats.ValueRO.MaxSpeed);
                    steering.ValueRW.Linear = force;
                    steering.ValueRW.Priority = 1.5f;
                    steering.ValueRW.Weight = 1f;
                    steering.ValueRW.BehaviorType = SteeringBehaviorType.PathFollow;

                    traversalTarget.ValueRW.HasRoute = true;
                    traversalTarget.ValueRW.NextRoomId = target.TargetRoomId;
                    continue;
                }

                // Plan route if needed (no route or route is stale)
                if (!target.HasRoute || target.NextRoomId != currentRoom && target.NextRoomId >= 0)
                {
                    // Check if we need to replan
                    var needsReplan = !target.HasRoute;

                    if (!needsReplan && target.NextRoomId >= 0)
                    {
                        // Check if the next door was reached - advance along route
                        if (RoomTraversalMath.HasReachedDoor(agentPos, target.NextDoorPosition, stats.ValueRO.ArrivalThreshold))
                        {
                            // We reached the next room's door - update current room
                            currentRoom = target.NextRoomId;
                            graph.CurrentRoomId = currentRoom;

                            if (currentRoom == target.TargetRoomId)
                            {
                                // Arrived at target room
                                var force = SeekForce(agentPos, target.TargetPosition, stats.ValueRO.MaxSpeed);
                                steering.ValueRW.Linear = force;
                                steering.ValueRW.Priority = 1.5f;
                                steering.ValueRW.Weight = 1f;
                                steering.ValueRW.BehaviorType = SteeringBehaviorType.PathFollow;

                                traversalTarget.ValueRW.HasRoute = true;
                                traversalTarget.ValueRW.NextRoomId = target.TargetRoomId;
                                continue;
                            }

                            needsReplan = true;
                        }
                    }

                    if (needsReplan)
                    {
                        var route = RoomTraversalMath.PlanRoomRoute(
                            currentRoom,
                            target.TargetRoomId,
                            connArray,
                            connArray.Length,
                            Allocator.Temp);

                        if (route.Length >= 2)
                        {
                            var nextRoom = route[1];
                            var doorPos = RoomTraversalMath.FindDoorToRoom(currentRoom, nextRoom, connArray, connArray.Length);

                            traversalTarget.ValueRW.NextRoomId = nextRoom;
                            traversalTarget.ValueRW.NextDoorPosition = doorPos;
                            traversalTarget.ValueRW.HasRoute = true;
                        }
                        else
                        {
                            // No path found - clear route
                            traversalTarget.ValueRW.HasRoute = false;
                            steering.ValueRW = SteeringForce.Zero;
                            route.Dispose();
                            continue;
                        }

                        route.Dispose();
                    }
                }

                // Seek toward the next door
                var seekTarget = traversalTarget.ValueRO.NextDoorPosition;
                var seekForce = SeekForce(agentPos, seekTarget, stats.ValueRO.MaxSpeed);

                steering.ValueRW.Linear = seekForce;
                steering.ValueRW.Priority = 1.5f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.PathFollow;
            }

            connArray.Dispose();
            boundsArray.Dispose();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float2 SeekForce(float2 from, float2 to, float maxSpeed)
        {
            var desired = to - from;
            var dist = math.length(desired);
            if (dist < 0.0001f)
                return float2.zero;

            return math.normalize(desired) * maxSpeed;
        }
    }
}
