using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Formation
{
    /// <summary>
    /// Formation system. For each FormationLeader, computes all slot positions based on
    /// the FormationConfig, then assigns FormationSlotAssignment to followers.
    /// 
    /// Followers are entities with FormationSlotAssignment component that share a common
    /// relationship to the leader. The system queries all leaders, computes formation
    /// geometry, then updates each follower's target world position.
    /// 
    /// Uses arrive steering to smoothly move followers toward their assigned slots.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct FormationSystem : ISystem
    {
        private EntityQuery leaderQuery;
        private EntityQuery followerQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            leaderQuery = SystemAPI.QueryBuilder()
                .WithAll<FormationLeader, FormationConfig, LocalTransform>()
                .Build();

            followerQuery = SystemAPI.QueryBuilder()
                .WithAll<FormationSlotAssignment, MovementStats, LocalTransform>()
                .Build();

            state.RequireForUpdate<FormationLeader>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var leaderCount = leaderQuery.CalculateEntityCount();
            if (leaderCount == 0)
                return;

            // Gather leader data
            var leaderEntities = new NativeArray<Entity>(leaderCount, Allocator.Temp);
            var leaderPositions = new NativeArray<float2>(leaderCount, Allocator.Temp);
            var leaderForwards = new NativeArray<float2>(leaderCount, Allocator.Temp);
            var leaderConfigs = new NativeArray<FormationConfig>(leaderCount, Allocator.Temp);
            var leaderOffsets = new NativeArray<float2>(leaderCount, Allocator.Temp);
            var leaderRefs = new NativeArray<Entity>(leaderCount, Allocator.Temp);

            var idx = 0;
            foreach (var (leader, config, transform, entity) in
                SystemAPI.Query<RefRO<FormationLeader>, RefRO<FormationConfig>, RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                leaderEntities[idx] = entity;
                leaderPositions[idx] = transform.ValueRO.Position.xz;
                var fwdTemp = math.forward(transform.ValueRO.Rotation);
                leaderForwards[idx] = math.normalizesafe(new float2(fwdTemp.x, fwdTemp.z));
                leaderConfigs[idx] = config.ValueRO;
                leaderOffsets[idx] = leader.ValueRO.FormationOffset;
                leaderRefs[idx] = leader.ValueRO.ReferenceEntity;
                idx++;
            }

            // Resolve reference entity positions for leaders that point to another entity
            for (int i = 0; i < leaderCount; i++)
            {
                if (leaderRefs[i] != Entity.Null)
                {
                    if (SystemAPI.HasComponent<LocalTransform>(leaderRefs[i]))
                    {
                        var refTransform = SystemAPI.GetComponentRO<LocalTransform>(leaderRefs[i]);
                        leaderPositions[i] = refTransform.ValueRO.Position.xz + leaderOffsets[i];
                    }
                }
                else
                {
                    leaderPositions[i] += leaderOffsets[i];
                }
            }

            // Gather follower data - which leader they belong to
            var followerCount = followerQuery.CalculateEntityCount();

            // Process each leader and update its followers
            foreach (var (leader, config, transform, leaderEntity) in
                SystemAPI.Query<RefRO<FormationLeader>, RefRO<FormationConfig>, RefRO<LocalTransform>>()
                    .WithEntityAccess())
            {
                var leaderPos = transform.ValueRO.Position.xz + leader.ValueRO.FormationOffset;

                // Resolve reference entity
                var refEntity = leader.ValueRO.ReferenceEntity;
                if (refEntity != Entity.Null && SystemAPI.HasComponent<LocalTransform>(refEntity))
                {
                    leaderPos = SystemAPI.GetComponentRO<LocalTransform>(refEntity).ValueRO.Position.xz
                        + leader.ValueRO.FormationOffset;
                }

                // Compute forward direction from rotation
                var fwd = math.forward(transform.ValueRO.Rotation);
                var leaderForward = math.normalizesafe(new float2(fwd.x, fwd.z));
                if (math.lengthsq(leaderForward) < 0.0001f)
                    leaderForward = new float2(0f, 1f);

                // Collect followers for this leader
                var followers = new NativeList<Entity>(Allocator.Temp);
                var followerSlots = new NativeList<int>(Allocator.Temp);

                foreach (var (slotAssignment, followerEntity) in
                    SystemAPI.Query<RefRW<FormationSlotAssignment>>()
                        .WithEntityAccess())
                {
                    // Simple assignment: all FormationSlotAssignment entities follow
                    // In a real scenario you'd filter by a leader reference component
                    followers.Add(followerEntity);
                    followerSlots.Add(slotAssignment.ValueRO.SlotIndex);
                }

                var count = followers.Length;
                if (count == 0)
                {
                    followers.Dispose();
                    followerSlots.Dispose();
                    continue;
                }

                // Compute formation positions based on config type
                NativeArray<float2> slotPositions = default;
                var formationType = config.ValueRO.Type;

                switch (formationType)
                {
                    case Core.FormationType.Line:
                        slotPositions = FormationMath.ComputeLinePositions(
                            leaderPos, leaderForward, config.ValueRO.Spacing, count, Allocator.Temp);
                        break;
                    case Core.FormationType.Wedge:
                        slotPositions = FormationMath.ComputeWedgePositions(
                            leaderPos, leaderForward, config.ValueRO.Spacing, config.ValueRO.Angle, count, Allocator.Temp);
                        break;
                    case Core.FormationType.Grid:
                        slotPositions = FormationMath.ComputeGridPositions(
                            leaderPos, leaderForward, config.ValueRO.Spacing, config.ValueRO.Columns, count, Allocator.Temp);
                        break;
                    case Core.FormationType.Circle:
                        slotPositions = FormationMath.ComputeCirclePositions(
                            leaderPos, config.ValueRO.CircleRadius, count, Allocator.Temp);
                        break;
                    case Core.FormationType.Column:
                        slotPositions = FormationMath.ComputeColumnPositions(
                            leaderPos, leaderForward, config.ValueRO.Spacing, count, Allocator.Temp);
                        break;
                    case Core.FormationType.V:
                        slotPositions = FormationMath.ComputeVPositions(
                            leaderPos, leaderForward, config.ValueRO.Spacing, config.ValueRO.Angle, count, Allocator.Temp);
                        break;
                    default:
                        slotPositions = FormationMath.ComputeLinePositions(
                            leaderPos, leaderForward, config.ValueRO.Spacing, count, Allocator.Temp);
                        break;
                }

                // Assign slot positions to followers and compute steering forces
                for (int i = 0; i < count; i++)
                {
                    var followerEntity = followers[i];
                    if (!SystemAPI.HasComponent<FormationSlotAssignment>(followerEntity))
                        continue;

                    var slotAssignment = SystemAPI.GetComponentRW<FormationSlotAssignment>(followerEntity);
                    slotAssignment.ValueRW.SlotIndex = i;
                    slotAssignment.ValueRW.TargetWorldPos = slotPositions[i];

                    // Apply arrive steering toward the slot position
                    if (SystemAPI.HasComponent<MovementStats>(followerEntity) &&
                        SystemAPI.HasComponent<LocalTransform>(followerEntity) &&
                        SystemAPI.HasComponent<SteeringForce>(followerEntity))
                    {
                        var stats = SystemAPI.GetComponentRO<MovementStats>(followerEntity);
                        var followerTransform = SystemAPI.GetComponentRO<LocalTransform>(followerEntity);
                        var steering = SystemAPI.GetComponentRW<SteeringForce>(followerEntity);

                        var followerPos = followerTransform.ValueRO.Position.xz;
                        var targetPos = slotPositions[i];

                        steering.ValueRW.Linear = SteeringMath.Arrive(
                            followerPos,
                            targetPos,
                            stats.ValueRO.MaxSpeed,
                            stats.ValueRO.MaxSpeed * 0.6f,
                            stats.ValueRO.ArrivalThreshold);
                        steering.ValueRW.Priority = 1.5f;
                        steering.ValueRW.Weight = 1f;
                        steering.ValueRW.BehaviorType = SteeringBehaviorType.Formation;
                    }
                }

                slotPositions.Dispose();
                followers.Dispose();
                followerSlots.Dispose();
            }

            leaderEntities.Dispose();
            leaderPositions.Dispose();
            leaderForwards.Dispose();
            leaderConfigs.Dispose();
            leaderOffsets.Dispose();
            leaderRefs.Dispose();
        }
    }
}
