using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using BovineLabs.Combat.Core;

namespace BovineLabs.Combat.Systems
{
    /// <summary>
    /// Populates SensedTarget buffers for all CombatAgent entities.
    /// Uses simple distance-based O(n^2) spatial queries.
    /// Future: integrate with BovineLabs.Core.SpatialMap for O(1) lookups.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSpatialGroup))]
    public partial struct SensorQuerySystem : ISystem
    {
        private struct AgentData
        {
            public Entity Entity;
            public float3 Position;
            public CombatRelationship Relationship;
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Step 1: Build NativeArray of all CombatAgent entities
            var agentQuery = SystemAPI.QueryBuilder()
                .WithAll<CombatAgent, LocalTransform>()
                .Build();

            var agentEntities = agentQuery.ToEntityArray(Allocator.Temp);
            var agentTransforms = agentQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            var relationshipLookup = SystemAPI.GetComponentLookup<CombatRelationship>(true);

            var agents = new NativeArray<AgentData>(agentEntities.Length, Allocator.Temp);
            for (int i = 0; i < agentEntities.Length; i++)
            {
                agents[i] = new AgentData
                {
                    Entity = agentEntities[i],
                    Position = agentTransforms[i].Position,
                    Relationship = relationshipLookup.HasComponent(agentEntities[i])
                        ? relationshipLookup[agentEntities[i]]
                        : default,
                };
            }

            try
            {
                // Step 2: For each sensing entity
                foreach (var (profile, transform, memory, entity) in
                    SystemAPI.Query<RefRO<CombatAgentProfile>, RefRO<LocalTransform>, RefRW<TargetMemory>>()
                        .WithAll<CombatAgent>()
                        .WithEntityAccess())
                {
                    var sensorRadius = profile.ValueRO.SensorRadius;
                    var maxTargets = profile.ValueRO.MaxSensedTargets;
                    var sensorRadiusSq = sensorRadius * sensorRadius;
                    var myPos = transform.ValueRO.Position;
                    var myRelationship = relationshipLookup.HasComponent(entity)
                        ? relationshipLookup[entity]
                        : default;

                    // Clear sensed target buffer
                    if (!SystemAPI.HasBuffer<SensedTarget>(entity))
                        continue;

                    var sensedBuffer = SystemAPI.GetBuffer<SensedTarget>(entity);
                    sensedBuffer.Clear();

                    // Collect candidates within sensor radius
                    var candidates = new NativeList<SensedTarget>(Allocator.Temp);
                    try
                    {
                        for (int j = 0; j < agents.Length; j++)
                        {
                            if (agents[j].Entity == entity)
                                continue;

                            var diff = agents[j].Position - myPos;
                            var distanceSq = math.lengthsq(diff);

                            if (distanceSq > sensorRadiusSq)
                                continue;

                            var relation = DetermineRelation(myRelationship, agents[j].Relationship);
                            var flags = SensedTargetFlags.InLineOfSight;
                            float threatScore = ComputeThreatScore(relation, distanceSq, sensorRadiusSq);

                            candidates.Add(new SensedTarget
                            {
                                Entity = agents[j].Entity,
                                Position = agents[j].Position,
                                DistanceSq = distanceSq,
                                ThreatScore = threatScore,
                                Relation = relation,
                                Flags = flags,
                            });
                        }

                        // Sort by ThreatScore descending, keep top MaxSensedTargets
                        candidates.Sort(new ThreatScoreComparer());

                        var targetCount = math.min(candidates.Length, maxTargets);
                        for (int k = 0; k < targetCount; k++)
                        {
                            sensedBuffer.Add(candidates[k]);
                        }

                        // Update TargetMemory for best hostile
                        Entity bestHostile = Entity.Null;
                        float3 bestPosition = float3.zero;
                        float bestThreat = float.NegativeInfinity;

                        for (int k = 0; k < candidates.Length; k++)
                        {
                            if (candidates[k].Relation == TargetRelation.Hostile &&
                                candidates[k].ThreatScore > bestThreat)
                            {
                                bestThreat = candidates[k].ThreatScore;
                                bestHostile = candidates[k].Entity;
                                bestPosition = candidates[k].Position;
                            }
                        }

                        if (bestHostile != Entity.Null)
                        {
                            memory.ValueRW.LastTarget = bestHostile;
                            memory.ValueRW.LastKnownPosition = bestPosition;
                            memory.ValueRW.LastSeenTime = (float)SystemAPI.Time.ElapsedTime;
                        }
                    }
                    finally
                    {
                        candidates.Dispose();
                    }
                }
            }
            finally
            {
                agents.Dispose();
                agentEntities.Dispose();
                agentTransforms.Dispose();
            }
        }

        private static TargetRelation DetermineRelation(CombatRelationship self, CombatRelationship other)
        {
            if (self.FactionId == other.FactionId && self.FactionId != 0)
                return TargetRelation.Friendly;

            if (self.IsHostileTo(other))
                return TargetRelation.Hostile;

            return TargetRelation.Neutral;
        }

        private static float ComputeThreatScore(TargetRelation relation, float distanceSq, float sensorRadiusSq)
        {
            float baseScore = relation switch
            {
                TargetRelation.Hostile => 100f,
                TargetRelation.Neutral => 50f,
                TargetRelation.Friendly => 10f,
                _ => 0f,
            };

            float distanceFactor = 1f - math.saturate(distanceSq / sensorRadiusSq);
            return baseScore + distanceFactor * 50f;
        }

        private struct ThreatScoreComparer : IComparer<SensedTarget>
        {
            public int Compare(SensedTarget a, SensedTarget b)
            {
                return b.ThreatScore.CompareTo(a.ThreatScore);
            }
        }
    }
}
