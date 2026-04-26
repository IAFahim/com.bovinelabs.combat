using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Follow
{
    /// <summary>
    /// Follow behavior system. Handles two follow modes:
    /// 
    /// 1. FollowLeader: Entity seeks to a position behind a designated leader at
    ///    the specified distance and lateral offset. Uses arrive steering for smooth approach.
    /// 
    /// 2. FollowChain: Entity follows behind the entity directly ahead of it in a chain.
    ///    Chain followers use the ahead entity's facing direction to determine "behind."
    ///    This creates a snake-like trailing formation.
    /// 
    /// Both modes output a SteeringForce with Follow behavior type.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct FollowSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FollowLeader>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Process FollowLeader entities
            foreach (var (follow, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<FollowLeader>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var leaderEntity = follow.ValueRO.Leader;
                if (leaderEntity == Entity.Null)
                    continue;

                if (!SystemAPI.HasComponent<LocalTransform>(leaderEntity))
                    continue;

                var leaderTransform = SystemAPI.GetComponentRO<LocalTransform>(leaderEntity);
                var leaderPos = leaderTransform.ValueRO.Position.xz;

                // Compute leader forward direction from rotation
                var fwd = math.forward(leaderTransform.ValueRO.Rotation);
                var leaderForward = math.normalizesafe(new float2(fwd.x, fwd.z));
                if (math.lengthsq(leaderForward) < 0.0001f)
                    leaderForward = new float2(0f, 1f);

                var targetPos = FollowMath.ComputeFollowPosition(
                    leaderPos,
                    leaderForward,
                    follow.ValueRO.FollowDistance,
                    follow.ValueRO.FollowOffset);

                var currentPos = transform.ValueRO.Position.xz;

                steering.ValueRW.Linear = SteeringMath.Arrive(
                    currentPos,
                    targetPos,
                    stats.ValueRO.MaxSpeed,
                    stats.ValueRO.MaxSpeed * 0.5f,
                    stats.ValueRO.ArrivalThreshold);
                steering.ValueRW.Priority = 1.2f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Follow;
            }

            // Process FollowChain entities
            foreach (var (chain, stats, transform, steering) in
                SystemAPI.Query<
                    RefRO<FollowChain>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var aheadEntity = chain.ValueRO.AheadOfMe;
                if (aheadEntity == Entity.Null)
                    continue;

                if (!SystemAPI.HasComponent<LocalTransform>(aheadEntity))
                    continue;

                var aheadTransform = SystemAPI.GetComponentRO<LocalTransform>(aheadEntity);
                var aheadPos = aheadTransform.ValueRO.Position.xz;

                // Compute ahead entity's forward direction
                var fwd = math.forward(aheadTransform.ValueRO.Rotation);
                var aheadForward = math.normalizesafe(new float2(fwd.x, fwd.z));
                if (math.lengthsq(aheadForward) < 0.0001f)
                    aheadForward = new float2(0f, 1f);

                var targetPos = FollowMath.ComputeChainPosition(
                    aheadPos,
                    aheadForward,
                    chain.ValueRO.FollowDistance);

                var currentPos = transform.ValueRO.Position.xz;

                steering.ValueRW.Linear = SteeringMath.Arrive(
                    currentPos,
                    targetPos,
                    stats.ValueRO.MaxSpeed,
                    stats.ValueRO.MaxSpeed * 0.5f,
                    stats.ValueRO.ArrivalThreshold);
                steering.ValueRW.Priority = 1.2f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Follow;
            }
        }
    }
}
