using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// ApplySteeringSystem: The final piece that makes agents actually move.
    /// Reads the FinalSteeringForce (from Blend) or SteeringForce (direct) 
    /// and applies it to the agent's LocalTransform + MovementStats velocity.
    /// 
    /// This system runs AFTER CombatSteeringGroup and after BlendSystem.
    /// It handles:
    /// 1. Acceleration limiting (don't change velocity faster than MaxAcceleration)
    /// 2. Turn speed limiting (don't rotate faster than MaxTurnSpeed)
    /// 3. Speed clamping (don't exceed MaxSpeed)
    /// 4. Writing final position + rotation to LocalTransform
    /// 
    /// Usage: Add to your simulation group AFTER CombatSteeringGroup.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    public partial struct ApplySteeringSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            if (dt <= 0f) return;

            foreach (var (stats, transform, steering) in
                SystemAPI.Query<
                    RefRW<MovementStats>,
                    RefRW<LocalTransform>,
                    RefRO<SteeringForce>>()
                .WithAll<SteeringForce>())
            {
                var force = steering.ValueRO.Linear;
                var currentVel = stats.ValueRO.Velocity;

                // If the force is zero, decelerate
                if (steering.ValueRO.IsZero)
                {
                    var speed = math.length(currentVel);

                    if (speed > 0.01f)
                    {
                        // Decelerate
                        var decel = stats.ValueRO.MaxAcceleration * dt;
                        if (decel >= speed)
                        {
                            stats.ValueRW.Velocity = float2.zero;
                        }
                        else
                        {
                            stats.ValueRW.Velocity = currentVel - math.normalize(currentVel) * decel;
                        }
                    }
                    continue;
                }

                var desiredVel = force;
                var maxSpeed = stats.ValueRO.MaxSpeed;
                var maxAccel = stats.ValueRO.MaxAcceleration;
                var maxTurn = stats.ValueRO.MaxTurnSpeed;

                // Clamp desired velocity to max speed
                desiredVel = SteeringMath.LimitMagnitude(desiredVel, maxSpeed);

                // Apply turn speed limit
                if (maxTurn > 0f && math.length(currentVel) > 0.01f)
                {
                    var currentAngle = SteeringMath.FacingAngleFromDirection(math.normalize(currentVel));
                    var desiredAngle = SteeringMath.FacingAngleFromDirection(math.normalize(desiredVel));
                    var maxStep = maxTurn * dt;
                    var newAngle = SteeringMath.MoveAngleToward(currentAngle, desiredAngle, maxStep);
                    var newDir = SteeringMath.DirectionFromFacingAngle(newAngle);
                    var speed = math.length(desiredVel);
                    desiredVel = newDir * speed;
                }

                // Apply acceleration limit
                if (maxAccel > 0f)
                {
                    var velChange = desiredVel - currentVel;
                    var maxChange = maxAccel * dt;
                    velChange = SteeringMath.LimitMagnitude(velChange, maxChange);
                    desiredVel = currentVel + velChange;
                }

                // Final speed clamp
                desiredVel = SteeringMath.LimitMagnitude(desiredVel, maxSpeed);

                // Update velocity
                stats.ValueRW.Velocity = desiredVel;

                // Update facing angle
                if (math.length(desiredVel) > 0.01f)
                {
                    stats.ValueRW.FacingAngle = SteeringMath.FacingAngleFromDirection(math.normalize(desiredVel));
                }

                // Apply movement to transform (XZ plane)
                var pos = transform.ValueRO.Position;
                pos.x += desiredVel.x * dt;
                pos.z += desiredVel.y * dt;
                transform.ValueRW.Position = pos;

                // Update rotation to face movement direction
                if (math.length(desiredVel) > 0.01f)
                {
                    var angle = stats.ValueRO.FacingAngle;
                    transform.ValueRW.Rotation = quaternion.Euler(0f, angle, 0f);
                }
            }
        }
    }
}
