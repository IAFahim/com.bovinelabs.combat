using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Group
{
    /// <summary>
    /// Group (flocking) behavior system.
    /// Computes cohesion, separation, and alignment forces for agents on the same team.
    /// Gathers neighbor data within GroupNeighborRadius, then sums forces with GroupWeights.
    /// Outputs the combined force to SteeringForce.
    /// 
    /// Uses a two-pass approach:
    ///   1. Collect all group agent positions/velocities/teams into temp arrays.
    ///   2. For each agent, find same-team neighbors within radius and compute flocking forces.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct GroupSystem : ISystem
    {
        private EntityQuery groupAgentQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Query matches the exact set of agents we process, ensuring index alignment
            groupAgentQuery = SystemAPI.QueryBuilder()
                .WithAll<LocalTransform, MovementStats, TeamId, GroupNeighborRadius, GroupWeights>()
                .Build();

            state.RequireForUpdate<GroupNeighborRadius>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var agentCount = groupAgentQuery.CalculateEntityCount();
            if (agentCount == 0)
                return;

            // Pass 1: Gather all group agents into temp arrays
            var positions = new NativeArray<float2>(agentCount, Allocator.Temp);
            var velocities = new NativeArray<float2>(agentCount, Allocator.Temp);
            var agentRadii = new NativeArray<float>(agentCount, Allocator.Temp);
            var teams = new NativeArray<int>(agentCount, Allocator.Temp);

            var idx = 0;
            foreach (var (transform, stats, team) in
                SystemAPI.Query<RefRO<LocalTransform>, RefRO<MovementStats>, RefRO<TeamId>>()
                    .WithAll<GroupNeighborRadius, GroupWeights>())
            {
                positions[idx] = transform.ValueRO.Position.xz;
                velocities[idx] = stats.ValueRO.Velocity;
                agentRadii[idx] = stats.ValueRO.Radius;
                teams[idx] = team.ValueRO.Value;
                idx++;
            }

            // Temp neighbor lists (cleared per agent)
            var neighborPositions = new NativeList<float2>(agentCount, Allocator.Temp);
            var neighborVelocities = new NativeList<float2>(agentCount, Allocator.Temp);
            var neighborRadii = new NativeList<float>(agentCount, Allocator.Temp);

            // Pass 2: For each agent, find neighbors and compute group force
            var agentIndex = 0;
            foreach (var (searchRadius, weights, stats, transform, team, steering) in
                SystemAPI.Query<
                    RefRO<GroupNeighborRadius>,
                    RefRO<GroupWeights>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRO<TeamId>,
                    RefRW<SteeringForce>>())
            {
                neighborPositions.Clear();
                neighborVelocities.Clear();
                neighborRadii.Clear();

                var myPos = transform.ValueRO.Position.xz;
                var myTeam = team.ValueRO.Value;
                var radius = searchRadius.ValueRO.Value;
                var radiusSq = radius * radius;

                // Find same-team neighbors within search radius
                for (int i = 0; i < agentCount; i++)
                {
                    if (i == agentIndex)
                        continue;

                    // Only flock with allies on a real team
                    if (myTeam == 0 || teams[i] != myTeam)
                        continue;

                    var toNeighbor = positions[i] - myPos;
                    var distSq = math.lengthsq(toNeighbor);
                    if (distSq <= radiusSq && distSq > 0.0001f)
                    {
                        neighborPositions.Add(positions[i]);
                        neighborVelocities.Add(velocities[i]);
                        neighborRadii.Add(agentRadii[i]);
                    }
                }

                if (neighborPositions.Length > 0)
                {
                    steering.ValueRW.Linear = GroupMath.ComputeGroupForce(
                        myPos,
                        stats.ValueRO.Velocity,
                        stats.ValueRO.Radius,
                        stats.ValueRO.MaxSpeed,
                        weights.ValueRO.Cohesion,
                        weights.ValueRO.Separation,
                        weights.ValueRO.Alignment,
                        neighborPositions,
                        neighborVelocities,
                        neighborRadii);

                    steering.ValueRW.Priority = 0.8f;
                    steering.ValueRW.Weight = 1f;
                    steering.ValueRW.BehaviorType = SteeringBehaviorType.Cohesion;
                }

                agentIndex++;
            }

            positions.Dispose();
            velocities.Dispose();
            agentRadii.Dispose();
            teams.Dispose();
            neighborPositions.Dispose();
            neighborVelocities.Dispose();
            neighborRadii.Dispose();
        }
    }
}
