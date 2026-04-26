using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.Group
{
    /// <summary>
    /// Pure math utility functions for group steering behaviors (flocking).
    /// Implements the three classic Reynolds flocking forces on the XZ plane.
    /// Static class - no allocations, Burst-friendly.
    /// </summary>
    public static unsafe class GroupMath
    {
        /// <summary>
        /// Cohesion: compute a steering force toward the centroid of neighbor positions.
        /// Returns a normalized direction vector toward the group center.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Cohesion(float2 agentPos, NativeList<float2> neighborPositions)
        {
            if (neighborPositions.Length == 0)
                return float2.zero;

            var centroid = float2.zero;
            for (int i = 0; i < neighborPositions.Length; i++)
            {
                centroid += neighborPositions[i];
            }
            centroid /= neighborPositions.Length;

            var toCentroid = centroid - agentPos;
            var dist = math.length(toCentroid);
            if (dist < 0.0001f)
                return float2.zero;

            return math.normalize(toCentroid);
        }

        /// <summary>
        /// Separation: compute a steering force pushing away from close neighbors.
        /// Force strength is inversely proportional to distance.
        /// Uses agent and neighbor radii to determine overlap threshold.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Separation(
            float2 agentPos,
            float agentRadius,
            NativeList<float2> neighborPositions,
            NativeList<float> neighborRadii)
        {
            var separation = float2.zero;
            var count = math.min(neighborPositions.Length, neighborRadii.Length);

            for (int i = 0; i < count; i++)
            {
                var toNeighbor = neighborPositions[i] - agentPos;
                var dist = math.length(toNeighbor);
                var combinedRadius = agentRadius + neighborRadii[i];
                var threshold = combinedRadius * 2.5f;

                if (dist > threshold || dist < 0.0001f)
                    continue;

                // Weight inversely proportional to distance - closer neighbors push harder
                var weight = 1f - (dist / threshold);
                separation -= (toNeighbor / dist) * weight;
            }

            return separation;
        }

        /// <summary>
        /// Alignment: compute a steering force that matches the average velocity of neighbors.
        /// Returns a normalized direction representing the velocity correction needed.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Alignment(float2 agentVel, NativeList<float2> neighborVelocities)
        {
            if (neighborVelocities.Length == 0)
                return float2.zero;

            var avgVel = float2.zero;
            for (int i = 0; i < neighborVelocities.Length; i++)
            {
                avgVel += neighborVelocities[i];
            }
            avgVel /= neighborVelocities.Length;

            // Return the difference between average and current (desired correction)
            var correction = avgVel - agentVel;
            return math.normalizesafe(correction);
        }

        /// <summary>
        /// Combine all three group forces with their weights into a single steering output.
        /// Each force is scaled to maxSpeed, then weighted and summed.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeGroupForce(
            float2 agentPos,
            float2 agentVel,
            float agentRadius,
            float maxSpeed,
            float cohesionWeight,
            float separationWeight,
            float alignmentWeight,
            NativeList<float2> neighborPositions,
            NativeList<float2> neighborVelocities,
            NativeList<float> neighborRadii)
        {
            var cohesion = Cohesion(agentPos, neighborPositions) * maxSpeed;
            var separation = Separation(agentPos, agentRadius, neighborPositions, neighborRadii) * maxSpeed;
            var alignment = Alignment(agentVel, neighborVelocities) * maxSpeed;

            var combined = cohesion * cohesionWeight
                         + separation * separationWeight
                         + alignment * alignmentWeight;

            return BovineLabs.Combat.Core.SteeringMath.LimitMagnitude(combined, maxSpeed);
        }
    }
}
