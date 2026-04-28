using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using BovineLabs.Combat.Core;

namespace BovineLabs.Combat.Systems
{
    /// <summary>
    /// The ONLY system that writes PhysicsVelocity from resolved combat motion.
    /// Reads <see cref="ResolvedMotion"/> after priority arbitration and applies
    /// the resulting movement to <see cref="PhysicsVelocity"/>.
    /// Angular velocity is intentionally left untouched (handled by facing system).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatMotionResolveGroup))]
    public partial struct CombatPhysicsMotorSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            if (dt <= 0f)
                return;

            foreach (var (motionRO, velocityRW, profileRO) in
                SystemAPI.Query<RefRO<ResolvedMotion>, RefRW<PhysicsVelocity>, RefRO<CombatAgentProfile>>())
            {
                var motion = motionRO.ValueRO.Motion;
                var profile = profileRO.ValueRO;
                var currentVelocity = velocityRW.ValueRO;

                // Work in xz plane
                var currentXZ = new float2(currentVelocity.Linear.x, currentVelocity.Linear.z);

                float2 targetXZ;
                float accelScale;
                float brakeScale;

                switch (motion.Mode)
                {
                    case CombatMotionMode.Stop:
                    case CombatMotionMode.HoldPosition:
                        // Brake toward zero velocity
                        targetXZ = float2.zero;
                        accelScale = motion.AccelerationScale;
                        brakeScale = motion.BrakeScale;
                        break;

                    case CombatMotionMode.DesiredVelocity:
                    case CombatMotionMode.DesiredDirection:
                    case CombatMotionMode.ArriveAtPosition:
                    case CombatMotionMode.MaintainDistance:
                        // Resolve system has already computed the desired velocity;
                        // apply SpeedScale to get the effective target.
                        targetXZ = new float2(motion.DesiredVelocity.x, motion.DesiredVelocity.z) * motion.SpeedScale;
                        accelScale = motion.AccelerationScale;
                        brakeScale = motion.BrakeScale;
                        break;

                    default:
                        // CombatMotionMode.None — leave velocity unchanged
                        continue;
                }

                // Compute velocity change
                var desiredChange = targetXZ - currentXZ;
                var maxAcceleration = profile.DefaultMaxAcceleration;

                // Use braking scale when decelerating (velocity moving away from target)
                // Use acceleration scale when accelerating (velocity moving toward target)
                float effectiveMaxAccel;
                if (math.lengthsq(targetXZ) < math.lengthsq(currentXZ))
                {
                    // Decelerating — use brake scale
                    effectiveMaxAccel = maxAcceleration * brakeScale;
                }
                else
                {
                    // Accelerating — use acceleration scale
                    effectiveMaxAccel = maxAcceleration * accelScale;
                }

                var maxDelta = effectiveMaxAccel * dt;
                var changeMag = math.length(desiredChange);

                float2 newXZ;
                if (changeMag > maxDelta && changeMag > 0f)
                {
                    // Limit acceleration
                    newXZ = currentXZ + (desiredChange / changeMag) * maxDelta;
                }
                else
                {
                    newXZ = targetXZ;
                }

                // Clamp final speed by max speed * speed scale
                var maxSpeed = profile.DefaultMaxSpeed * motion.SpeedScale;
                var speed = math.length(newXZ);
                if (speed > maxSpeed && speed > 0f)
                {
                    newXZ = newXZ * (maxSpeed / speed);
                }

                // Write back, preserving Y component of linear velocity; do NOT touch angular
                velocityRW.ValueRW.Linear = new float3(newXZ.x, currentVelocity.Linear.y, newXZ.y);
            }
        }
    }
}
