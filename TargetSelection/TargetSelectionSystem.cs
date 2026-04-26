using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.TargetSelection
{
    /// <summary>
    /// Target selection system.
    /// Queries all enemies (different TeamId), applies the configured selection strategy,
    /// and writes the best target to the CombatTarget component.
    ///
    /// Supports three strategies: Nearest, Weakest, MostThreatening.
    /// Optionally filters by a forward-facing cone using MaxAngle.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct TargetSelectionSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<TargetSelectionParams>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Collect all enemies upfront (alive, different team)
            var enemyCount = 0;
            foreach (var (team, health, transform, entity) in
                SystemAPI.Query<
                    RefRO<TeamId>,
                    RefRO<CombatHealth>,
                    RefRO<LocalTransform>>().WithEntityAccess())
            {
                if (health.ValueRO.IsAlive)
                    enemyCount++;
            }

            if (enemyCount == 0)
                return;

            // Allocate temporary collections
            var enemyPositions = new NativeArray<float2>(enemyCount, Allocator.Temp);
            var enemyHealths = new NativeArray<float>(enemyCount, Allocator.Temp);
            var enemyThreats = new NativeArray<float>(enemyCount, Allocator.Temp);
            var enemyEntities = new NativeArray<Entity>(enemyCount, Allocator.Temp);
            var enemyTeams = new NativeArray<int>(enemyCount, Allocator.Temp);

            var idx = 0;
            foreach (var (team, health, transform, threat, entity) in
                SystemAPI.Query<
                    RefRO<TeamId>,
                    RefRO<CombatHealth>,
                    RefRO<LocalTransform>,
                    RefRO<ThreatScore>>().WithEntityAccess())
            {
                if (!health.ValueRO.IsAlive)
                    continue;

                enemyPositions[idx] = transform.ValueRO.Position.xz;
                enemyHealths[idx] = health.ValueRO.Current;
                enemyThreats[idx] = threat.ValueRO.Value;
                enemyEntities[idx] = entity;
                enemyTeams[idx] = team.ValueRO.Value;
                idx++;
            }

            var actualCount = idx;

            // For each agent with selection params, find the best enemy target
            foreach (var (selParams, myTeam, myTransform, combatTarget) in
                SystemAPI.Query<
                    RefRO<TargetSelectionParams>,
                    RefRO<TeamId>,
                    RefRO<LocalTransform>,
                    RefRW<CombatTarget>>())
            {
                var agentPos = myTransform.ValueRO.Position.xz;
                var agentFwd = math.forward(myTransform.ValueRO.Rotation).xz;

                // Filter candidates by team (enemies only) and cone
                var candidatePositions = new NativeArray<float2>(actualCount, Allocator.Temp);
                var candidateHealths = new NativeArray<float>(actualCount, Allocator.Temp);
                var candidateThreats = new NativeArray<float>(actualCount, Allocator.Temp);
                var candidateEntities = new NativeArray<Entity>(actualCount, Allocator.Temp);
                var candidateCount = 0;

                for (int i = 0; i < actualCount; i++)
                {
                    // Skip same team or neutral
                    var enemyTeam = new TeamId { Value = enemyTeams[i] };
                    if (!myTeam.ValueRO.IsEnemyTo(enemyTeam))
                        continue;

                    // Optional cone check (PI = omnidirectional, skip check)
                    if (selParams.ValueRO.MaxAngle < math.PI)
                    {
                        if (!TargetSelectionMath.IsInCone(agentPos, agentFwd, enemyPositions[i], selParams.ValueRO.MaxAngle))
                            continue;
                    }

                    candidatePositions[candidateCount] = enemyPositions[i];
                    candidateHealths[candidateCount] = enemyHealths[i];
                    candidateThreats[candidateCount] = enemyThreats[i];
                    candidateEntities[candidateCount] = enemyEntities[i];
                    candidateCount++;
                }

                if (candidateCount == 0)
                {
                    combatTarget.ValueRW = CombatTarget.None;
                    candidatePositions.Dispose();
                    candidateHealths.Dispose();
                    candidateThreats.Dispose();
                    candidateEntities.Dispose();
                    continue;
                }

                // Apply selection strategy
                int bestIndex = selParams.ValueRO.Strategy switch
                {
                    SelectionStrategy.Weakest => TargetSelectionMath.SelectWeakest(
                        agentPos, candidatePositions, candidateHealths, selParams.ValueRO.MaxRange, candidateCount),
                    SelectionStrategy.MostThreatening => TargetSelectionMath.SelectMostThreatening(
                        agentPos, candidatePositions, candidateThreats, selParams.ValueRO.MaxRange, candidateCount),
                    _ => TargetSelectionMath.SelectNearest(
                        agentPos, candidatePositions, selParams.ValueRO.MaxRange, candidateCount),
                };

                if (bestIndex >= 0)
                {
                    combatTarget.ValueRW.Entity = candidateEntities[bestIndex];
                    combatTarget.ValueRW.LastKnownPosition = candidatePositions[bestIndex];
                    combatTarget.ValueRW.LastSeenTime = (float)SystemAPI.Time.ElapsedTime;
                }
                else
                {
                    combatTarget.ValueRW = CombatTarget.None;
                }

                candidatePositions.Dispose();
                candidateHealths.Dispose();
                candidateThreats.Dispose();
                candidateEntities.Dispose();
            }

            enemyPositions.Dispose();
            enemyHealths.Dispose();
            enemyThreats.Dispose();
            enemyEntities.Dispose();
            enemyTeams.Dispose();
        }
    }
}
