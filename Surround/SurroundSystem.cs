using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Surround
{
    /// <summary>
    /// Surround behavior system.
    /// Each agent seeks its assigned position around the target.
    /// Total slots = number of agents with SurroundTarget currently surrounding.
    /// Agents are evenly distributed on a circle at surroundRadius from the target.
    ///
    /// Priority = 2.0 to override standard movement but yield to charge/avoidance.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatSteeringGroup))]
    public partial struct SurroundSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<SurroundTarget>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // Count total surrounding agents to determine slot count
            var surroundQuery = SystemAPI.QueryBuilder().WithAll<SurroundTarget, SurroundAssignment>().Build();
            var totalSlots = surroundQuery.CalculateEntityCount();

            if (totalSlots <= 0)
                return;

            // Assign slot indices and compute assigned positions
            var slotCounter = 0;

            foreach (var (surround, assignment, stats, transform, steering) in
                SystemAPI.Query<
                    RefRW<SurroundTarget>,
                    RefRW<SurroundAssignment>,
                    RefRO<MovementStats>,
                    RefRO<LocalTransform>,
                    RefRW<SteeringForce>>())
            {
                var currentPos = transform.ValueRO.Position.xz;
                var targetPos = surround.ValueRO.TargetPos;
                var radius = surround.ValueRO.SurroundRadius;

                // Update total slots
                surround.ValueRW.TotalSlots = totalSlots;

                // Assign this agent's slot index (simple sequential assignment)
                surround.ValueRW.SlotIndex = slotCounter;

                // Compute the assigned surround position for this slot
                var assignedPos = SurroundMath.ComputeSurroundPosition(
                    targetPos,
                    radius,
                    slotCounter,
                    totalSlots);

                assignment.ValueRW.AssignedPosition = assignedPos;

                // Seek toward the assigned position with arrive behavior
                var toAssigned = assignedPos - currentPos;
                var dist = math.length(toAssigned);

                if (dist <= stats.ValueRO.ArrivalThreshold)
                {
                    // Already at assigned position
                    slotCounter++;
                    continue;
                }

                // Decelerate when close to slot
                var maxSpeed = stats.ValueRO.MaxSpeed;
                var slowRadius = radius * 0.5f;
                var desiredSpeed = dist <= slowRadius
                    ? maxSpeed * (dist / slowRadius)
                    : maxSpeed;

                var force = math.normalizesafe(toAssigned) * desiredSpeed;

                steering.ValueRW.Linear = force;
                steering.ValueRW.Priority = 2f;
                steering.ValueRW.Weight = 1f;
                steering.ValueRW.BehaviorType = SteeringBehaviorType.Surround;

                slotCounter++;
            }
        }
    }
}
