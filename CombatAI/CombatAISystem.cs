using System.Runtime.CompilerServices;
using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.CombatAI
{
    /// <summary>
    /// Combat AI state machine system.
    /// Transitions between Idle, Engaging, Fleeing, and Following states
    /// based on threat assessment, health levels, and engagement rules.
    ///
    /// State transitions:
    ///   Idle -> Engaging: enemy within engageRange and health is OK
    ///   Idle -> Fleeing: enemy detected but health below flee threshold
    ///   Engaging -> Fleeing: health drops below flee threshold
    ///   Engaging -> Idle: enemy disengaged (beyond disengageRange) or target dead
    ///   Fleeing -> Idle: no enemies nearby, or health recovered
    ///   Following -> Engaging: enemy within engageRange
    ///   Following -> Idle: leader lost
    ///   Any -> Following: ordered to follow (external trigger)
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct CombatAISystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<EngagementRules>();
            state.RequireForUpdate<CombatAIState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (rules, aiState, health, team, transform) in
                SystemAPI.Query<
                    RefRO<EngagementRules>,
                    RefRW<CombatAIState>,
                    RefRO<CombatHealth>,
                    RefRO<TeamId>,
                    RefRO<LocalTransform>>())
            {
                // Update state timer
                var currentState = aiState.ValueRW;
                currentState.StateTimer += dt;

                var agentPos = transform.ValueRO.Position.xz;
                var agentHealth = health.ValueRO.Current;
                var agentMaxHealth = health.ValueRO.Max;

                // Scan for nearest enemy
                var nearestEnemyDist = float.MaxValue;
                var nearestEnemyPos = float2.zero;
                var nearestEnemyEntity = Entity.Null;

                foreach (var (enemyTeam, enemyHealth, enemyTransform, entity) in
                    SystemAPI.Query<
                        RefRO<TeamId>,
                        RefRO<CombatHealth>,
                        RefRO<LocalTransform>>().WithEntityAccess())
                {
                    if (!enemyHealth.ValueRO.IsAlive)
                        continue;
                    if (!team.ValueRO.IsEnemyTo(enemyTeam.ValueRO))
                        continue;

                    var distSq = math.lengthsq(enemyTransform.ValueRO.Position.xz - agentPos);
                    if (distSq < nearestEnemyDist)
                    {
                        nearestEnemyDist = distSq;
                        nearestEnemyPos = enemyTransform.ValueRO.Position.xz;
                        nearestEnemyEntity = entity;
                    }
                }

                var nearestEnemyDistance = math.sqrt(nearestEnemyDist);
                var hasEnemy = nearestEnemyEntity != Entity.Null;

                // State machine transitions
                switch (currentState.State)
                {
                    case AIState.Idle:
                        if (hasEnemy)
                        {
                            // Check if we should flee first
                            if (CombatAIMath.ShouldFlee(agentHealth, agentMaxHealth, rules.ValueRO.FleeHealthThreshold))
                            {
                                TransitionTo(ref currentState, AIState.Fleeing);
                            }
                            else if (CombatAIMath.ShouldEngage(agentHealth, nearestEnemyDistance, rules.ValueRO.EngageRange))
                            {
                                currentState.EngagementTarget = nearestEnemyEntity;
                                TransitionTo(ref currentState, AIState.Engaging);
                            }
                        }
                        break;

                    case AIState.Engaging:
                        // Check flee condition (overrides engagement)
                        if (CombatAIMath.ShouldFlee(agentHealth, agentMaxHealth, rules.ValueRO.FleeHealthThreshold))
                        {
                            currentState.EngagementTarget = Entity.Null;
                            TransitionTo(ref currentState, AIState.Fleeing);
                        }
                        // Check disengage (target too far or dead)
                        else if (!hasEnemy || CombatAIMath.ShouldDisengage(nearestEnemyDistance, rules.ValueRO.DisengageRange))
                        {
                            currentState.EngagementTarget = Entity.Null;
                            TransitionTo(ref currentState, AIState.Idle);
                        }
                        // Update engagement target to nearest enemy
                        else
                        {
                            currentState.EngagementTarget = nearestEnemyEntity;
                        }
                        break;

                    case AIState.Fleeing:
                        // Stop fleeing when no enemies nearby or health recovered
                        var threatNearby = hasEnemy && nearestEnemyDistance < rules.ValueRO.EngageRange * 1.5f;
                        var healthRecovered = !CombatAIMath.ShouldFlee(agentHealth, agentMaxHealth, rules.ValueRO.FleeHealthThreshold);

                        if (!threatNearby || healthRecovered)
                        {
                            TransitionTo(ref currentState, AIState.Idle);
                        }
                        break;

                    case AIState.Following:
                        // Transition to engaging if enemy is very close
                        if (hasEnemy && CombatAIMath.ShouldEngage(agentHealth, nearestEnemyDistance, rules.ValueRO.EngageRange * 0.5f))
                        {
                            currentState.EngagementTarget = nearestEnemyEntity;
                            TransitionTo(ref currentState, AIState.Engaging);
                        }
                        break;
                }

                aiState.ValueRW = currentState;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void TransitionTo(ref CombatAIState aiState, AIState newState)
        {
            if (aiState.State != newState)
            {
                aiState.State = newState;
                aiState.StateTimer = 0f;
            }
        }
    }
}
