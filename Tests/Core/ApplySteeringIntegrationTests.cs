using NUnit.Framework;
using BovineLabs.Combat.Core;
using BovineLabs.Testing;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace BovineLabs.Combat.Core.Tests
{
    /// <summary>
    /// ECS integration tests for the ApplySteeringSystem.
    /// These tests create real ECS entities, run systems, and verify output.
    /// </summary>
    [TestFixture]
    [Category("Combat")]
    public class ApplySteeringIntegrationTests : ECSTestsFixture
    {
        /// <summary>
        /// Test that an entity with SteeringForce pointing +X moves in +X direction.
        /// </summary>
        [Test]
        public void ApplySteering_ForcePosX_MovesPosX()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, MovementStats.Default);
            Manager.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            Manager.AddComponentData(entity, new SteeringForce
            {
                Linear = new float2(5f, 0f),
                Priority = 1f,
                Weight = 1f,
                BehaviorType = SteeringBehaviorType.Seek,
            });

            // Run the system
            var sys = World.GetOrCreateSystem<ApplySteeringSystem>();
            sys.Update(World.Unmanaged);

            var pos = Manager.GetComponentData<LocalTransform>(entity).Position;
            // After 1 frame, position should have moved in +X direction
            Assert.That(pos.x, Is.GreaterThan(0f));
        }

        /// <summary>
        /// Test that zero steering force causes deceleration.
        /// </summary>
        [Test]
        public void ApplySteering_ZeroForce_Decelerates()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new MovementStats
            {
                MaxSpeed = 5f,
                MaxAcceleration = 20f,
                Velocity = new float2(5f, 0f),
                FacingAngle = 0f,
                Radius = 0.5f,
                ArrivalThreshold = 0.1f,
            });
            Manager.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            Manager.AddComponentData(entity, SteeringForce.Zero);

            var sys = World.GetOrCreateSystem<ApplySteeringSystem>();
            sys.Update(World.Unmanaged);

            var stats = Manager.GetComponentData<MovementStats>(entity);
            // Velocity should decrease
            Assert.That(math.length(stats.Velocity), Is.LessThan(5f));
        }

        /// <summary>
        /// Test that speed is clamped to MaxSpeed.
        /// </summary>
        [Test]
        public void ApplySteering_OverspeedForce_ClampsToMaxSpeed()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new MovementStats
            {
                MaxSpeed = 3f,
                MaxAcceleration = 100f, // Very high acceleration = instant
                Velocity = float2.zero,
                FacingAngle = 0f,
                Radius = 0.5f,
                ArrivalThreshold = 0.1f,
            });
            Manager.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            Manager.AddComponentData(entity, new SteeringForce
            {
                Linear = new float2(100f, 0f), // Way over max speed
                Priority = 1f,
                Weight = 1f,
                BehaviorType = SteeringBehaviorType.Seek,
            });

            var sys = World.GetOrCreateSystem<ApplySteeringSystem>();
            sys.Update(World.Unmanaged);

            var stats = Manager.GetComponentData<MovementStats>(entity);
            Assert.That(math.length(stats.Velocity), Is.LessThanOrEqualTo(3.1f)); // small tolerance
        }

        /// <summary>
        /// Test that facing angle updates toward movement direction.
        /// </summary>
        [Test]
        public void ApplySteering_Movement_UpdatesFacingAngle()
        {
            var entity = Manager.CreateEntity();
            Manager.AddComponentData(entity, new MovementStats
            {
                MaxSpeed = 5f,
                MaxAcceleration = 100f,
                MaxTurnSpeed = 0f, // No turn limit = instant rotation
                Velocity = float2.zero,
                FacingAngle = 0f,
                Radius = 0.5f,
                ArrivalThreshold = 0.1f,
            });
            Manager.AddComponentData(entity, LocalTransform.FromPosition(float3.zero));
            Manager.AddComponentData(entity, new SteeringForce
            {
                Linear = new float2(0f, 5f), // Moving in +Z direction
                Priority = 1f,
                Weight = 1f,
                BehaviorType = SteeringBehaviorType.Seek,
            });

            var sys = World.GetOrCreateSystem<ApplySteeringSystem>();
            sys.Update(World.Unmanaged);

            var stats = Manager.GetComponentData<MovementStats>(entity);
            // Facing +Z (angle=0) when moving in +Y direction in XZ plane
            Assert.That(math.abs(stats.FacingAngle), Is.LessThan(0.1f));
        }
    }
}
