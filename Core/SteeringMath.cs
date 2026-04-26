using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Pure math utility functions for steering behaviors.
    /// All operations are on the XZ plane (float2).
    /// Static class - no allocations, Burst-friendly.
    /// </summary>
    public static unsafe class SteeringMath
    {
        /// <summary>
        /// Compute seek steering force: desired velocity toward target.
        /// Returns a force vector whose magnitude is capped at maxSpeed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Seek(float2 currentPos, float2 targetPos, float maxSpeed)
        {
            var desired = targetPos - currentPos;
            var dist = math.length(desired);
            if (dist < 0.0001f)
                return float2.zero;

            return math.normalize(desired) * maxSpeed;
        }

        /// <summary>
        /// Compute flee steering force: desired velocity away from threat.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Flee(float2 currentPos, float2 threatPos, float maxSpeed)
        {
            return Seek(threatPos, currentPos, maxSpeed);
        }

        /// <summary>
        /// Compute arrive steering force with deceleration zones.
        /// slowRadius: distance at which deceleration begins.
        /// arrivalThreshold: distance at which agent stops.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Arrive(float2 currentPos, float2 targetPos, float maxSpeed, float slowRadius, float arrivalThreshold)
        {
            var offset = targetPos - currentPos;
            var dist = math.length(offset);

            if (dist <= arrivalThreshold)
                return float2.zero;

            var desiredSpeed = dist <= slowRadius
                ? maxSpeed * (dist / slowRadius)
                : maxSpeed;

            return math.normalize(offset) * desiredSpeed;
        }

        /// <summary>
        /// Compute pursue steering force: aim at target's predicted future position.
        /// maxPrediction: maximum look-ahead time (seconds).
        /// targetVelocity: current velocity of the target.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Pursue(float2 currentPos, float2 currentVel, float2 targetPos, float2 targetVel, float maxSpeed, float maxPrediction)
        {
            var toTarget = targetPos - currentPos;
            var dist = math.length(toTarget);
            var speed = math.length(currentVel);

            var prediction = speed > 0.0001f
                ? dist / speed
                : maxPrediction;

            prediction = math.min(prediction, maxPrediction);

            var futureTarget = targetPos + targetVel * prediction;
            return Seek(currentPos, futureTarget, maxSpeed);
        }

        /// <summary>
        /// Compute evade steering force: flee from target's predicted future position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Evade(float2 currentPos, float2 currentVel, float2 threatPos, float2 threatVel, float maxSpeed, float maxPrediction)
        {
            var futureThreat = threatPos + threatVel * maxPrediction;
            return Flee(currentPos, futureThreat, maxSpeed);
        }

        /// <summary>
        /// Compute wander steering: a random displacement on a wander circle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Wander(float facingAngle, float wanderRadius, float wanderDistance, float jitter, ref Unity.Mathematics.Random rng, out float newWanderAngle)
        {
            var circleCenter = new float2(
                math.sin(facingAngle),
                math.cos(facingAngle)
            ) * wanderDistance;

            var displacement = new float2(
                math.cos(facingAngle) * wanderRadius,
                math.sin(facingAngle) * wanderRadius
            );

            displacement += new float2(rng.NextFloat(-jitter, jitter), rng.NextFloat(-jitter, jitter));
            displacement = math.normalizesafe(displacement) * wanderRadius;

            newWanderAngle = math.atan2(displacement.y, displacement.x);

            return math.normalizesafe(circleCenter + displacement) * wanderRadius;
        }

        /// <summary>
        /// Limit a vector's magnitude to maxMagnitude.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 LimitMagnitude(float2 v, float maxMagnitude)
        {
            var sq = math.lengthsq(v);
            if (sq > maxMagnitude * maxMagnitude)
            {
                return math.normalize(v) * maxMagnitude;
            }
            return v;
        }

        /// <summary>
        /// Truncate a steering force by subtracting current velocity and limiting by max force.
        /// steering = desired - velocity. Classic Reynolds steering formula.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 Steer(float2 desiredVelocity, float2 currentVelocity, float maxForce)
        {
            var steer = desiredVelocity - currentVelocity;
            return LimitMagnitude(steer, maxForce);
        }

        /// <summary>
        /// Get facing angle from a direction vector on XZ plane.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float FacingAngleFromDirection(float2 direction)
        {
            return math.atan2(direction.x, direction.y);
        }

        /// <summary>
        /// Get forward direction from facing angle.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 DirectionFromFacingAngle(float angle)
        {
            return new float2(math.sin(angle), math.cos(angle));
        }

        /// <summary>
        /// Shortest angular difference from 'from' to 'to' in [-PI, PI].
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float DeltaAngle(float from, float to)
        {
            var delta = to - from;
            while (delta > math.PI) delta -= math.PI * 2f;
            while (delta < -math.PI) delta += math.PI * 2f;
            return delta;
        }

        /// <summary>
        /// Move angle toward target by maxStep. Clamps to exact target if within step.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float MoveAngleToward(float current, float target, float maxStep)
        {
            var delta = DeltaAngle(current, target);
            if (math.abs(delta) <= maxStep)
                return target;
            return current + math.sign(delta) * maxStep;
        }
    }
}
