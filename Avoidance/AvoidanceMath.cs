using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Avoidance
{
    /// <summary>
    /// Local avoidance using simplified RVO (Reciprocal Velocity Obstacles).
    /// Each agent computes an avoidance force based on nearby agents' positions and velocities.
    /// Uses a neighbor query radius to limit computations.
    /// </summary>
    public struct AvoidanceParams : IComponentData
    {
        /// <summary>Radius to search for nearby agents.</summary>
        public float NeighborRadius;

        /// <summary>Time horizon - how far ahead to predict collisions (seconds).</summary>
        public float TimeHorizon;

        /// <summary>Maximum number of neighbors to consider.</summary>
        public int MaxNeighbors;

        /// <summary>Strength of avoidance force (0..1 blends with other forces).</summary>
        public float AvoidanceStrength;

        public static AvoidanceParams Default => new()
        {
            NeighborRadius = 5f,
            TimeHorizon = 1.5f,
            MaxNeighbors = 10,
            AvoidanceStrength = 1f,
        };
    }

    /// <summary>
    /// Pure math for RVO-like local avoidance.
    /// Given an agent and a list of nearby agents, computes an avoidance velocity.
    /// </summary>
    public static unsafe class AvoidanceMath
    {
        /// <summary>
        /// Compute avoidance force for a single agent.
        /// Uses the ORCA (Optimal Reciprocal Collision Avoidance) principle:
        /// for each neighbor, compute a half-plane of permitted velocities,
        /// then find the closest velocity to the preferred velocity that satisfies all constraints.
        /// 
        /// Simplified version: sum up repulsive forces from agents within collision time.
        /// </summary>
        public static float2 ComputeAvoidance(
            float2 agentPos,
            float2 agentVel,
            float agentRadius,
            float maxSpeed,
            float timeHorizon,
            float avoidanceStrength,
            NativeArray<float2> neighborPositions,
            NativeArray<float2> neighborVelocities,
            NativeArray<float> neighborRadii)
        {
            var avoidanceForce = float2.zero;
            var count = math.min(neighborPositions.Length, neighborVelocities.Length);
            count = math.min(count, neighborRadii.Length);

            for (int i = 0; i < count; i++)
            {
                var toNeighbor = neighborPositions[i] - agentPos;
                var distSq = math.lengthsq(toNeighbor);
                var combinedRadius = agentRadius + neighborRadii[i];

                // Skip if too far away
                if (distSq > combinedRadius * combinedRadius * 4f)
                    continue;

                var dist = math.sqrt(distSq);
                if (dist < 0.0001f)
                    continue;

                var direction = toNeighbor / dist;

                // Relative velocity
                var relativeVel = agentVel - neighborVelocities[i];

                // Time to closest approach
                var approachSpeed = -math.dot(relativeVel, direction);
                var timeToCollision = approachSpeed > 0.0001f
                    ? (dist - combinedRadius) / approachSpeed
                    : float.MaxValue;

                // Only avoid if collision is imminent
                if (timeToCollision > timeHorizon)
                    continue;

                // Compute avoidance strength based on urgency
                var urgency = timeToCollision < 0f
                    ? 1f  // Already overlapping - full strength
                    : 1f - (timeToCollision / timeHorizon);

                // Perpendicular avoidance direction (turn away from neighbor)
                var avoidDir = new float2(-direction.y, direction.x);

                // Choose the side that requires less velocity change
                var side = math.dot(relativeVel, avoidDir);
                if (side < 0f)
                    avoidDir = -avoidDir;

                avoidanceForce += avoidDir * urgency * avoidanceStrength;

                // Add separation force for very close agents
                if (dist < combinedRadius * 1.5f)
                {
                    var separationForce = -direction * (1f - dist / (combinedRadius * 1.5f));
                    avoidanceForce += separationForce * avoidanceStrength;
                }
            }

            return SteeringMath.LimitMagnitude(avoidanceForce, maxSpeed);
        }

        /// <summary>
        /// Simple separation force: push away from nearby agents.
        /// Cheaper than full RVO - good for large crowds.
        /// </summary>
        public static float2 ComputeSeparation(
            float2 agentPos,
            float agentRadius,
            float maxSpeed,
            float separationRadius,
            NativeArray<float2> neighborPositions,
            NativeArray<float> neighborRadii)
        {
            var separation = float2.zero;
            var count = math.min(neighborPositions.Length, neighborRadii.Length);

            for (int i = 0; i < count; i++)
            {
                var toNeighbor = neighborPositions[i] - agentPos;
                var dist = math.length(toNeighbor);

                var combinedRadius = agentRadius + neighborRadii[i];
                var threshold = math.max(separationRadius, combinedRadius * 2f);

                if (dist > threshold || dist < 0.0001f)
                    continue;

                // Weight inversely proportional to distance
                var weight = 1f - (dist / threshold);
                separation -= (toNeighbor / dist) * weight;
            }

            return SteeringMath.LimitMagnitude(separation, maxSpeed);
        }
    }
}
