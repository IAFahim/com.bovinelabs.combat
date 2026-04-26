using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Pure math utility for line-of-sight checks on XZ plane.
    /// Uses simple ray-circle intersection for agent visibility.
    /// For full wall/obstacle LOS, combine with Recast raycasting.
    /// </summary>
    public static unsafe class LineOfSightMath
    {
        /// <summary>
        /// Check if target is within a cone of vision from the agent.
        /// agentPos: agent position
        /// agentForward: agent facing direction (normalized)
        /// targetPos: target position
        /// coneHalfAngle: half-angle of the vision cone (radians)
        /// maxRange: maximum sight distance
        /// </summary>
        public static bool IsInVisionCone(
            float2 agentPos,
            float2 agentForward,
            float2 targetPos,
            float coneHalfAngle,
            float maxRange)
        {
            var toTarget = targetPos - agentPos;
            var dist = math.length(toTarget);

            if (dist > maxRange || dist < 0.0001f)
                return false;

            var dirToTarget = toTarget / dist;
            var dot = math.dot(agentForward, dirToTarget);
            var angle = math.acos(math.clamp(dot, -1f, 1f));

            return angle <= coneHalfAngle;
        }

        /// <summary>
        /// Check if a ray from origin in direction intersects a circle.
        /// Used for simple obstacle/agent LOS blocking.
        /// circleCenter: center of the blocking circle
        /// circleRadius: radius of the blocking circle
        /// Returns true if the ray intersects the circle.
        /// </summary>
        public static bool RayIntersectsCircle(
            float2 rayOrigin,
            float2 rayDir,
            float2 circleCenter,
            float circleRadius)
        {
            var oc = rayOrigin - circleCenter;
            var a = math.dot(rayDir, rayDir);
            var b = 2f * math.dot(oc, rayDir);
            var c = math.dot(oc, oc) - circleRadius * circleRadius;
            var discriminant = b * b - 4f * a * c;

            if (discriminant < 0f)
                return false;

            var sqrtD = math.sqrt(discriminant);
            var t1 = (-b - sqrtD) / (2f * a);
            var t2 = (-b + sqrtD) / (2f * a);

            // Hit if either intersection is in front (t > 0)
            return t1 > 0f || t2 > 0f;
        }

        /// <summary>
        /// Compute distance along a ray to a circle intersection.
        /// Returns -1 if no intersection.
        /// </summary>
        public static float RayCircleDistance(
            float2 rayOrigin,
            float2 rayDir,
            float2 circleCenter,
            float circleRadius)
        {
            var oc = rayOrigin - circleCenter;
            var a = math.dot(rayDir, rayDir);
            var b = 2f * math.dot(oc, rayDir);
            var c = math.dot(oc, oc) - circleRadius * circleRadius;
            var discriminant = b * b - 4f * a * c;

            if (discriminant < 0f)
                return -1f;

            var sqrtD = math.sqrt(discriminant);
            var t1 = (-b - sqrtD) / (2f * a);

            if (t1 > 0f)
                return t1;

            var t2 = (-b + sqrtD) / (2f * a);
            if (t2 > 0f)
                return t2;

            return -1f;
        }
    }
}
