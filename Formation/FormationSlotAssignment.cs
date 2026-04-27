using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.Formation
{
    /// <summary>
    /// Dynamic slot assignment for formations.
    /// Handles:
    /// - Assigning new agents to the nearest available slot
    /// - Reassigning when an agent dies (gap fill)
    /// - Maintaining formation coherence
    /// </summary>
    public static unsafe class FormationSlotHelper
    {
        /// <summary>
        /// Assign slots to agents by finding each agent's nearest unoccupied slot.
        /// Uses greedy nearest-first assignment.
        /// </summary>
        public static NativeArray<int> AssignSlotsGreedy(
            NativeArray<float2> agentPositions,
            NativeArray<float2> slotPositions,
            Allocator allocator)
        {
            var assignments = new NativeArray<int>(agentPositions.Length, allocator);
            var occupied = new NativeArray<bool>(slotPositions.Length, Allocator.Temp);

            try
            {
                // Initialize all assignments to -1 (unassigned)
                for (int i = 0; i < assignments.Length; i++)
                    assignments[i] = -1;

                // For each agent, find nearest unoccupied slot
                for (int i = 0; i < agentPositions.Length; i++)
                {
                    var bestSlot = -1;
                    var bestDist = float.MaxValue;

                    for (int s = 0; s < slotPositions.Length; s++)
                    {
                        if (occupied[s]) continue;

                        var dist = math.distancesq(agentPositions[i], slotPositions[s]);
                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            bestSlot = s;
                        }
                    }

                    if (bestSlot >= 0)
                    {
                        assignments[i] = bestSlot;
                        occupied[bestSlot] = true;
                    }
                }

                return assignments;
            }
            finally
            {
                occupied.Dispose();
            }
        }

        /// <summary>
        /// Reassign slots after an agent is removed.
        /// Shifts assignments down to fill the gap, maintaining formation shape.
        /// </summary>
        public static NativeArray<int> ReassignAfterRemoval(
            NativeArray<int> currentAssignments,
            int removedAgentIndex,
            int totalSlots,
            Allocator allocator)
        {
            var newAssignments = new NativeArray<int>(currentAssignments.Length - 1, allocator);

            int writeIdx = 0;
            for (int i = 0; i < currentAssignments.Length; i++)
            {
                if (i == removedAgentIndex) continue;

                var slot = currentAssignments[i];
                // Shift slot indices down if they were after the removed agent's slot
                var removedSlot = currentAssignments[removedAgentIndex];
                if (slot > removedSlot && slot < totalSlots)
                    slot--;

                newAssignments[writeIdx++] = slot;
            }

            return newAssignments;
        }

        /// <summary>
        /// Compute the formation coherence score (0..1).
        /// 1 = all agents at their assigned slot positions.
        /// 0 = maximum deviation.
        /// </summary>
        public static float ComputeCoherence(
            NativeArray<float2> agentPositions,
            NativeArray<float2> slotPositions,
            NativeArray<int> assignments)
        {
            if (agentPositions.Length == 0) return 1f;

            var totalDeviation = 0f;
            var maxDeviation = 0f;

            // Find max slot distance for normalization
            for (int i = 0; i < slotPositions.Length; i++)
            {
                for (int j = i + 1; j < slotPositions.Length; j++)
                {
                    var d = math.distance(slotPositions[i], slotPositions[j]);
                    if (d > maxDeviation) maxDeviation = d;
                }
            }

            if (maxDeviation < 0.0001f) maxDeviation = 1f;

            for (int i = 0; i < agentPositions.Length; i++)
            {
                if (i < assignments.Length && assignments[i] >= 0 && assignments[i] < slotPositions.Length)
                {
                    var deviation = math.distance(agentPositions[i], slotPositions[assignments[i]]);
                    totalDeviation += deviation / maxDeviation;
                }
            }

            var avgDeviation = totalDeviation / agentPositions.Length;
            return math.clamp(1f - avgDeviation, 0f, 1f);
        }
    }
}
