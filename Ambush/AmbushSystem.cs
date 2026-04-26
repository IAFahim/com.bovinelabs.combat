using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Ambush
{
    /// <summary>
    /// Ambush behavior system.
    /// Agent seeks to hidePosition. When hidden and an enemy enters triggerRadius,
    /// spring toward the enemy.
    ///
    /// Phase transitions:
    ///   Hiding -> Waiting: when agent reaches hidePosition
    ///   Waiting -> Springing: when enemy enters triggerRadius
    ///   Springing -> Hiding: when agent reaches spring target (reset cycle)
    ///
    /// Runs in CombatSteeringGroup, outputs SteeringForce with BehaviorType.Ambush.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct AmbushSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<AmbushPosition>();
            state.RequireForUpdate<AmbushState>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (ambushPos, ambushState, stats, myTeam, transform, steering) in
                SystemAPI.Query<
                    RefRW<AmbushPosition>,
                    RefRW<AmbushState>,
                    RefRO<MovementStats>,
                    RefRO<TeamId>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;
                var phase = ambushState.ValueRO.Phase;

                // Phase: Hiding - seek hide position
                if (phase == AmbushPhase.Hiding)
                {
                    if (AmbushMath.HasReachedHidePosition(currentPos, ambushPos.ValueRO.HidePosition, stats.ValueRO.ArrivalThreshold))
                    {
                        ambushPos.ValueRW.IsHidden = true;
                        ambushState.ValueRW.Phase = AmbushPhase.Waiting;
                        phase = AmbushPhase.Waiting;
                    }
                }

                // Phase: Waiting - check for enemies in trigger radius
                if (phase == AmbushPhase.Waiting)
                {
                    var triggerRadius = ambushPos.ValueRO.TriggerRadius;
                    var hidePos = ambushPos.ValueRO.HidePosition;
                    var foundEnemy = false;
                    var closestEnemyPos = float2.zero;
                    var closestDistSq = float.MaxValue;

                    // Scan for enemies in trigger radius (different team, alive)
                    foreach (var (enemyTeam, enemyHealth, enemyTransform) in
                        SystemAPI.Query<
                            RefRO<TeamId>,
                            RefRO<CombatHealth>,
                            RefRO<LocalTransform>>())
                    {
                        if (!enemyHealth.ValueRO.IsAlive)
                            continue;

                        if (!myTeam.ValueRO.IsEnemyTo(enemyTeam.ValueRO))
                            continue;

                        var enemyPos = enemyTransform.ValueRO.Position.xz;
                        if (AmbushMath.IsEnemyInTrigger(enemyPos, hidePos, triggerRadius))
                        {
                            var distSq = math.lengthsq(enemyPos - hidePos);
                            if (distSq < closestDistSq)
                            {
                                closestDistSq = distSq;
                                closestEnemyPos = enemyPos;
                                foundEnemy = true;
                            }
                        }
                    }

                    if (foundEnemy)
                    {
                        ambushState.ValueRW.Phase = AmbushPhase.Springing;
                        ambushState.ValueRW.SpringTarget = closestEnemyPos;
                        ambushPos.ValueRW.IsSpringing = true;
                        ambushPos.ValueRW.IsHidden = false;
                        phase = AmbushPhase.Springing;
                    }
                }

                // Phase: Springing - check if reached spring target, then reset
                if (phase == AmbushPhase.Springing)
                {
                    var springTarget = ambushState.ValueRO.SpringTarget;
                    if (AmbushMath.HasReachedHidePosition(currentPos, springTarget, stats.ValueRO.ArrivalThreshold))
                    {
                        ambushState.ValueRW.Phase = AmbushPhase.Hiding;
                        ambushPos.ValueRW.IsSpringing = false;
                        phase = AmbushPhase.Hiding;
                    }
                }

                // Compute steering force for current phase
                var force = AmbushMath.ComputeAmbushForce(
                    currentPos,
                    ambushPos.ValueRO.HidePosition,
                    stats.ValueRO.MaxSpeed,
                    phase,
                    ambushState.ValueRO.SpringTarget);

                steering.ValueRW.Linear = force;
                steering.ValueRW.Priority = 2.5f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Ambush;
            }
        }
    }
}
