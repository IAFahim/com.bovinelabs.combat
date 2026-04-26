using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Surround
{
    /// <summary>
    /// Pure math utility functions for surround steering behavior.
    /// Surround distributes agents evenly around a target in a circle.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class SurroundMath
    {
        /// <summary>
        /// Compute the world position for a given surround slot.
        /// Agents are evenly spaced around a circle of the given radius.
        /// Slot 0 is at angle 0 (+X direction from center).
        /// </summary>
        /// <param name="center">Center of the surround circle.</param>
        /// <param name="radius">Radius of the surround circle.</param>
        /// <param name="slotIndex">This agent's slot index (0-based).</param>
        /// <param name="totalSlots">Total number of slots in the surround.</param>
        /// <returns>World position for this slot.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeSurroundPosition(float2 center, float radius, int slotIndex, int totalSlots)
        {
            if (totalSlots <= 0)
                return center;

            // Evenly distribute around full circle
            var angle = (math.PI * 2f * slotIndex) / totalSlots;

            return center + new float2(
                math.cos(angle) * radius,
                math.sin(angle) * radius);
        }

        /// <summary>
        /// Determine the nearest available surround slot for an agent based on its current position.
        /// Returns the slot index that minimizes distance from the agent to the slot position.
        /// </summary>
        /// <param name="agentPos">Agent's current XZ position.</param>
        /// <param name="center">Center of the surround circle.</param>
        /// <param name="radius">Radius of the surround circle.</param>
        /// <param name="totalSlots">Total number of slots in the surround.</param>
        /// <returns>The index of the nearest slot.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ComputeSurroundSlot(float2 agentPos, float2 center, float radius, int totalSlots)
        {
            if (totalSlots <= 0)
                return 0;

            var bestSlot = 0;
            var bestDistSq = float.MaxValue;

            for (int i = 0; i < totalSlots; i++)
            {
                var slotPos = ComputeSurroundPosition(center, radius, i, totalSlots);
                var distSq = math.lengthsq(agentPos - slotPos);

                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestSlot = i;
                }
            }

            return bestSlot;
        }
    }
}
