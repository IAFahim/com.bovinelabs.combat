using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Blend
{
    /// <summary>
    /// Blend system that combines multiple steering forces into a final velocity.
    ///
    /// For each agent with a BlendWeights buffer:
    /// 1. Collects all SteeringForce components from active behaviors
    /// 2. Matches them against BlendWeights entries by BehaviorType
    /// 3. Uses priority select for highest-priority force, or weighted blend if priorities are equal
    /// 4. Truncates the result to max speed
    /// 5. Writes to FinalSteeringForce
    ///
    /// This system runs AFTER CombatSteeringGroup so all behavior forces are computed.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(CombatSteeringGroup))]
    public partial struct BlendSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<FinalSteeringForce>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (blendWeights, steeringForce, stats, finalForce) in
                SystemAPI.Query<
                    DynamicBuffer<BlendWeights>,
                    RefRO<SteeringForce>,
                    RefRO<MovementStats>,
                    RefRW<FinalSteeringForce>>())
            {
                var currentForce = steeringForce.ValueRO;

                // If the steering force is zero (no active behavior), output zero
                if (currentForce.IsZero)
                {
                    finalForce.ValueRW.Velocity = float2.zero;
                    continue;
                }

                // Find matching blend weight for the active behavior type
                var matchedWeight = 1f;
                var matchedPriority = currentForce.Priority;

                for (int i = 0; i < blendWeights.Length; i++)
                {
                    if (blendWeights[i].BehaviorType == currentForce.BehaviorType)
                    {
                        matchedWeight = blendWeights[i].Weight;
                        matchedPriority = math.max(matchedPriority, blendWeights[i].Priority);
                        break;
                    }
                }

                // Apply weight
                var blended = currentForce.Linear * matchedWeight;

                // Truncate to max speed
                var result = BlendMath.TruncateToMaxSpeed(blended, stats.ValueRO.MaxSpeed);

                finalForce.ValueRW.Velocity = result;
            }

            // Also handle agents that only have a steering force without blend weights buffer
            // In that case, just truncate the steering force to max speed
            foreach (var (steeringForce, stats, finalForce) in
                SystemAPI.Query<
                    RefRO<SteeringForce>,
                    RefRO<MovementStats>,
                    RefRW<FinalSteeringForce>>()
                .WithNone<BlendWeights>())
            {
                var force = steeringForce.ValueRO.Linear;
                var result = BlendMath.TruncateToMaxSpeed(force, stats.ValueRO.MaxSpeed);
                finalForce.ValueRW.Velocity = result;
            }
        }
    }
}
