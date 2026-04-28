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
    [UpdateAfter(typeof(SensorQuerySystem))]
    public partial struct TargetPatchSystem : ISystem
    {
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
            var currentTime = (float)SystemAPI.Time.ElapsedTime;

            foreach (var (sensedTargets, memory) in
                SystemAPI.Query<DynamicBuffer<SensedTarget>, RefRW<TargetMemory>>())
            {
                // Step 1: Find best hostile target in SensedTarget buffer
                Entity bestHostile = Entity.Null;
                float3 bestPosition = float3.zero;
                float bestThreat = float.NegativeInfinity;

                for (int i = 0; i < sensedTargets.Length; i++)
                {
                    var target = sensedTargets[i];
                    if (target.Relation == TargetRelation.Hostile &&
                        target.ThreatScore > bestThreat)
                    {
                        bestThreat = target.ThreatScore;
                        bestHostile = target.Entity;
                        bestPosition = target.Position;
                    }
                }

                if (bestHostile == Entity.Null)
                    continue;

                // Step 2: Update TargetMemory.LastKnownPosition from target Position
                var mem = memory.ValueRW;
                mem.LastTarget = bestHostile;
                mem.LastKnownPosition = bestPosition;

                // Step 3: Update TargetMemory.LastSeenTime from current time
                mem.LastSeenTime = currentTime;

                memory.ValueRW = mem;
            }
        }
    }
}
