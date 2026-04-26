using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.ObstacleAvoidance
{
    /// <summary>
    /// Pure math utility functions for obstacle avoidance steering.
    /// Provides wall-sliding, raycast fan generation, and obstacle force computation.
    /// All operations on the XZ plane (float2). Static class, Burst-friendly.
    /// </summary>
    public static unsafe class ObstacleAvoidanceMath
    {
        /// <summary>
        /// Wall sliding: project desired velocity along the wall surface.
        /// Removes the component of velocity into the wall and scales by slideStrength.
        /// </summary>
        /// <param name="currentPos">Agent's current position.</param>
        /// <param name="desiredVelocity">The velocity the agent wants to move at.</param>
        /// <param name="wallNormal">Outward-facing normal of the wall.</param>
        /// <param name="slideStrength">How much of the sliding force to apply (0..1).</param>
        /// <returns>Velocity adjusted to slide along the wall.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 WallSliding(
            float2 currentPos,
            float2 desiredVelocity,
            float2 wallNormal,
            float slideStrength)
        {
            // Project out the component into the wall
            var dot = math.dot(desiredVelocity, wallNormal);
            if (dot >= 0f)
            {
                // Moving away from wall - no adjustment needed
                return desiredVelocity;
            }

            // Remove wall-penetration component and scale
            var projected = desiredVelocity - wallNormal * dot;
            return projected * slideStrength;
        }

        /// <summary>
        /// Generate a fan of ray directions centered on the agent's facing angle.
        /// Produces evenly spaced rays spanning a semi-circle ahead.
        /// </summary>
        /// <param name="origin">Agent position (for context, not used in direction calc).</param>
        /// <param name="facingAngle">Agent's current facing angle in radians.</param>
        /// <param name="rayCount">Number of rays to generate.</param>
        /// <param name="rayLength">Length of each ray.</param>
        /// <param name="rayAngles">Output: pre-allocated array of ray angles (radians).</param>
        /// <returns>NativeArray of normalized ray directions.</returns>
        public static NativeArray<float2> RaycastFan(
            float2 origin,
            float facingAngle,
            int rayCount,
            float rayLength,
            NativeArray<float> rayAngles)
        {
            var directions = new NativeArray<float2>(rayCount, Allocator.Temp);

            if (rayCount <= 0)
                return directions;

            if (rayCount == 1)
            {
                rayAngles[0] = facingAngle;
                directions[0] = new float2(math.sin(facingAngle), math.cos(facingAngle)) * rayLength;
                return directions;
            }

            // Spread rays across a 180-degree arc (-PI/2 to +PI/2 relative to facing)
            var halfSpread = math.PI * 0.5f;
            var step = (2f * halfSpread) / (rayCount - 1);

            for (int i = 0; i < rayCount; i++)
            {
                var angle = facingAngle - halfSpread + step * i;
                rayAngles[i] = angle;
                directions[i] = new float2(math.sin(angle), math.cos(angle)) * rayLength;
            }

            return directions;
        }

        /// <summary>
        /// Compute the net obstacle avoidance force from multiple raycast hits.
        /// For each hit, generates a repulsion force away from the obstacle surface.
        /// Closer hits produce stronger forces. Also applies wall-sliding.
        /// </summary>
        /// <param name="currentPos">Agent position.</param>
        /// <param name="desiredVelocity">Current desired velocity.</param>
        /// <param name="hitDistances">Distance to each hit (float.MaxValue if no hit).</param>
        /// <param name="hitNormals">Surface normal at each hit point.</param>
        /// <param name="rayDirections">Direction of each ray (normalized * length).</param>
        /// <param name="slideStrength">Wall sliding strength parameter.</param>
        /// <returns>Net avoidance force to add to steering.</returns>
        public static float2 ComputeObstacleForce(
            float2 currentPos,
            float2 desiredVelocity,
            NativeArray<float> hitDistances,
            NativeArray<float2> hitNormals,
            NativeArray<float2> rayDirections,
            float slideStrength)
        {
            var avoidanceForce = float2.zero;
            var count = math.min(hitDistances.Length, math.min(hitNormals.Length, rayDirections.Length));

            for (int i = 0; i < count; i++)
            {
                var hitDist = hitDistances[i];

                // No hit on this ray - skip
                if (hitDist >= 1000000f)
                    continue;

                var rayLength = math.length(rayDirections[i]);
                if (rayLength < 0.0001f)
                    continue;

                var normal = hitNormals[i];

                // Inverse-proportional force: closer obstacles push harder
                var ratio = hitDist / rayLength; // 0 = touching, 1 = at ray tip
                var strength = 1f - ratio;

                // Repulsion away from obstacle surface
                avoidanceForce += normal * strength;

                // Apply wall sliding if we have a desired velocity into the wall
                if (math.lengthsq(desiredVelocity) > 0.0001f)
                {
                    var slid = WallSliding(currentPos, desiredVelocity, normal, slideStrength);
                    avoidanceForce += slid * strength * 0.5f;
                }
            }

            return avoidanceForce;
        }
    }
}
