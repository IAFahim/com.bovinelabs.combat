using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using BovineLabs.Combat.Core;

namespace BovineLabs.Combat.Systems
{
    /// <summary>
    /// Evaluates sensed hostile targets and produces scored CombatDesire entries.
    /// Runs at the interval specified by CombatBrainProfile.UpdateInterval.
    /// Top 4 desires (by score) are written to the CombatDesire buffer.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatBrainGroup))]
    public partial struct CombatDesireScoringSystem : ISystem
    {
        private const int MaxDesires = 4;

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            if (dt <= 0f)
                return;

            foreach (var (brain, sensedTargets, desires) in
                SystemAPI.Query<
                    RefRW<CombatBrainProfile>,
                    DynamicBuffer<SensedTarget>,
                    DynamicBuffer<CombatDesire>>())
            {
                // Throttle brain updates to the configured interval
                brain.ValueRW.TimeSinceLastUpdate += dt;
                if (brain.ValueRW.TimeSinceLastUpdate < brain.ValueRO.UpdateInterval)
                    continue;

                brain.ValueRW.TimeSinceLastUpdate = 0f;

                var engageRangeSq = brain.ValueRO.EngageRange * brain.ValueRO.EngageRange;
                var attackRangeSq = brain.ValueRO.AttackRange * brain.ValueRO.AttackRange;
                var aggression = brain.ValueRO.Aggression;
                var threatSensitivity = brain.ValueRO.ThreatSensitivity;
                var fleeThreshold = brain.ValueRO.FleeHealthThreshold;

                // Collect candidate desires
                var candidates = new NativeList<CombatDesire>(Allocator.Temp);

                for (int i = 0; i < sensedTargets.Length; i++)
                {
                    var target = sensedTargets[i];

                    if (target.Relation != TargetRelation.Hostile)
                        continue;

                    var distSq = target.DistanceSq;
                    var threat = target.ThreatScore;

                    // Attack desire: within attack range
                    if (distSq <= attackRangeSq)
                    {
                        var invDist = math.rsqrt(math.max(distSq, 0.001f));
                        var score = invDist + threat * aggression;
                        candidates.Add(new CombatDesire
                        {
                            Type = CombatDesireType.Attack,
                            Target = target.Entity,
                            Position = target.Position,
                            Score = score,
                            Flags = CombatDesireFlags.RequiresTarget | CombatDesireFlags.Urgent,
                        });
                    }

                    // Engage desire: within engage range (but not already in attack range scored above)
                    if (distSq <= engageRangeSq && distSq > attackRangeSq)
                    {
                        var score = threat * aggression;
                        candidates.Add(new CombatDesire
                        {
                            Type = CombatDesireType.Engage,
                            Target = target.Entity,
                            Position = target.Position,
                            Score = score,
                            Flags = CombatDesireFlags.RequiresTarget,
                        });
                    }

                    // Flee desire: always considered, scored by threat sensitivity
                    // High threat + high sensitivity => strong flee desire
                    // The actual flee decision is modulated by FleeHealthThreshold applied externally
                    {
                        var fleeScore = threat * threatSensitivity;
                        candidates.Add(new CombatDesire
                        {
                            Type = CombatDesireType.Flee,
                            Target = target.Entity,
                            Position = target.Position,
                            Score = fleeScore,
                            Flags = CombatDesireFlags.RequiresPosition,
                        });
                    }
                }

                // Sort by score descending (simple insertion sort, N is small)
                for (int i = 1; i < candidates.Length; i++)
                {
                    var key = candidates[i];
                    var j = i - 1;
                    while (j >= 0 && candidates[j].Score < key.Score)
                    {
                        candidates[j + 1] = candidates[j];
                        j--;
                    }
                    candidates[j + 1] = key;
                }

                // Write top desires to buffer
                desires.Clear();
                var count = math.min(candidates.Length, MaxDesires);
                for (int i = 0; i < count; i++)
                {
                    desires.Add(candidates[i]);
                }

                candidates.Dispose();
            }
        }
    }
}
