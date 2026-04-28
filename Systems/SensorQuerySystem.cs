using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using BovineLabs.Combat.Core;

namespace BovineLabs.Combat.Systems
{
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
        public void OnCreate(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            // Step 1: Build NativeArray of all CombatAgent entities with LocalTransform + CombatRelationship
            var agentQuery = SystemAPI.QueryBuilder()
                .WithAll<CombatAgent, LocalTransform>()
                .Build();

            var agentEntities = agentQuery.ToEntityArray(Allocator.Temp);
            var agentTransforms = agentQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

            // Build agent data array
            var agents = new NativeArray<AgentData>(agentEntities.Length, Allocator.Temp);
            for (int i = 0; i < agentEntities.Length; i++)
            {
                agents[i] = new AgentData
                {
                    Entity = agentEntities[i],
                    Position = agentTransforms[i].Position,
                    Relationship = SystemAPI.GetComponentLookup<CombatRelationship>().HasComponent(agentEntities[i])
                        ? SystemAPI.GetComponentLookup<CombatRelationship>()[agentEntities[i]]
                        : default
                };
            }

            // Step 2: For each sensing entity (has CombatAgentProfile)
            foreach (var (profile, transform, memory, entity) in
                SystemAPI.Query<RefRO<CombatAgentProfile>, RefRO<LocalTransform>, RefRW<TargetMemory>>()
                    .WithAll<CombatAgent>()
                    .WithEntityAccess())
            {
                var sensorRadius = profile.ValueRO.SensorRadius;
                var maxTargets = profile.ValueRO.MaxSensedTargets;
                var sensorRadiusSq = sensorRadius * sensorRadius;

                // a. Clear SensedTarget buffer
                var sensedBuffer = commandBuffer.SetBuffer<SensedTarget>(entity);
                // If buffer already exists, get it and clear
                if (SystemAPI.HasBuffer<SensedTarget>(entity))
                {
                    SystemAPI.GetBuffer<SensedTarget>(entity).Clear();
                }

                // Collect candidates within sensor radius
                var candidates = new NativeList<SensedTarget>(Allocator.Temp);

                // b. Check all other agents within SensorRadius
                for (int j = 0; j < agents.Length; j++)
                {
                    if (agents[j].Entity == entity)
                        continue;

                    var diff = agents[j].Position - transform.ValueRO.Position;
                    var distanceSq = math.lengthsq(diff);

                    if (distanceSq > sensorRadiusSq)
                        continue;

                    // c. Compute DistanceSq, determine TargetRelation via CombatRelationship
                    var relation = DetermineRelation(
                        SystemAPI.GetComponentLookup<CombatRelationship>().HasComponent(entity)
                            ? SystemAPI.GetComponentLookup<CombatRelationship>()[entity]
                            : default,
                        agents[j].Relationship);

                    // d. Set SensedTargetFlags based on distance
                    var flags = SensedTargetFlags.None;
                    if (distanceSq < sensorRadiusSq)
                        flags |= SensedTargetFlags.InLineOfSight;

                    // Compute threat score based on relation and distance
                    float threatScore = ComputeThreatScore(relation, distanceSq, sensorRadiusSq);

                    var sensedTarget = new SensedTarget
                    {
                        Entity = agents[j].Entity,
                        Position = agents[j].Position,
                        DistanceSq = distanceSq,
                        ThreatScore = threatScore,
                        Relation = relation,
                        Flags = flags
                    };

                    candidates.Add(sensedTarget);
                }

                // e. Sort by ThreatScore descending and keep only top MaxSensedTargets
                candidates.Sort(new ThreatScoreComparer());

                var targetCount = math.min(candidates.Length, maxTargets);
                for (int k = 0; k < targetCount; k++)
                {
                    SystemAPI.GetBuffer<SensedTarget>(entity).Add(candidates[k]);
                }

                // f. Update TargetMemory for best hostile target
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
                    var mem = memory.ValueRW;
                    mem.LastTarget = bestHostile;
                    mem.LastKnownPosition = bestPosition;
                    mem.LastSeenTime = (float)SystemAPI.Time.ElapsedTime;
                    memory.ValueRW = mem;
                }

                candidates.Dispose();
            }

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();

            agents.Dispose();
            agentEntities.Dispose();
            agentTransforms.Dispose();
        }

        private static TargetRelation DetermineRelation(CombatRelationship self, CombatRelationship other)
        {
            if (self.FactionId == other.FactionId)
                return TargetRelation.Friendly;

            if (self.IsHostileTo(other))
                return TargetRelation.Hostile;

            return TargetRelation.Neutral;
        }

        private static float ComputeThreatScore(TargetRelation relation, float distanceSq, float sensorRadiusSq)
        {
            // Hostile targets are highest priority, closer = more threatening
            float baseScore = relation switch
            {
                TargetRelation.Hostile => 100f,
                TargetRelation.Neutral => 50f,
                TargetRelation.Friendly => 10f,
                _ => 0f
            };

            // Invert distance so closer targets score higher
            float distanceFactor = 1f - math.saturate(distanceSq / sensorRadiusSq);
            return baseScore + distanceFactor * 50f;
        }

        private struct ThreatScoreComparer : IComparer<SensedTarget>
        {
            public int Compare(SensedTarget a, SensedTarget b)
            {
                // Descending order by ThreatScore
                return b.ThreatScore.CompareTo(a.ThreatScore);
            }
        }
    }
}
