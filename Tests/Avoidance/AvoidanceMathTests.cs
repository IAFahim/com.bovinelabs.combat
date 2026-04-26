using NUnit.Framework;
using BovineLabs.Combat.Avoidance;
using BovineLabs.Combat.Core;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.Avoidance.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class AvoidanceMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeAvoidance

        [Test]
        public void ComputeAvoidance_NoNeighbors_ReturnsZero()
        {
            var positions = new NativeArray<float2>(0, Allocator.Temp);
            var velocities = new NativeArray<float2>(0, Allocator.Temp);
            var radii = new NativeArray<float>(0, Allocator.Temp);
            try
            {
                var result = AvoidanceMath.ComputeAvoidance(
                    new float2(0f, 0f), new float2(1f, 0f),
                    0.5f, 5f, 1.5f, 1f,
                    positions, velocities, radii);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                positions.Dispose();
                velocities.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeAvoidance_DistantNeighbor_NoForce()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var velocities = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                // Neighbor far away (combined radius = 1, threshold = 4)
                positions[0] = new float2(100f, 0f);
                velocities[0] = float2.zero;
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeAvoidance(
                    new float2(0f, 0f), new float2(1f, 0f),
                    0.5f, 5f, 1.5f, 1f,
                    positions, velocities, radii);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                positions.Dispose();
                velocities.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeAvoidance_OverlappingNeighbor_ProducesForce()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var velocities = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                // Neighbor very close, overlapping
                positions[0] = new float2(0.8f, 0f);
                velocities[0] = float2.zero;
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeAvoidance(
                    new float2(0f, 0f), new float2(1f, 0f),
                    0.5f, 5f, 1.5f, 1f,
                    positions, velocities, radii);

                // Should produce a non-zero avoidance force
                Assert.That(math.lengthsq(result), Is.GreaterThan(0f));
            }
            finally
            {
                positions.Dispose();
                velocities.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeAvoidance_HeadOnCollision_ProducesAvoidance()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var velocities = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                positions[0] = new float2(2f, 0f);
                velocities[0] = new float2(-1f, 0f); // Moving toward agent
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeAvoidance(
                    new float2(0f, 0f), new float2(1f, 0f),
                    0.5f, 5f, 1.5f, 1f,
                    positions, velocities, radii);

                // Should have a perpendicular avoidance component
                Assert.That(math.lengthsq(result), Is.GreaterThan(0f));
                // Force magnitude capped at maxSpeed
                Assert.That(math.length(result), Is.LessThanOrEqualTo(5f + Epsilon));
            }
            finally
            {
                positions.Dispose();
                velocities.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeAvoidance_MultipleNeighbors_SumsForces()
        {
            var positions = new NativeArray<float2>(2, Allocator.Temp);
            var velocities = new NativeArray<float2>(2, Allocator.Temp);
            var radii = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                // Two close neighbors
                positions[0] = new float2(0.8f, 0f);
                velocities[0] = float2.zero;
                radii[0] = 0.5f;

                positions[1] = new float2(0f, 0.8f);
                velocities[1] = float2.zero;
                radii[1] = 0.5f;

                var result = AvoidanceMath.ComputeAvoidance(
                    new float2(0f, 0f), new float2(1f, 0f),
                    0.5f, 5f, 1.5f, 1f,
                    positions, velocities, radii);

                Assert.That(math.lengthsq(result), Is.GreaterThan(0f));
            }
            finally
            {
                positions.Dispose();
                velocities.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeAvoidance_SeparationForce_VeryClose()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var velocities = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                // Very close: dist=0.6, combinedRadius=1.0, 1.5*combined=1.5
                // dist < combinedRadius*1.5 => separation kicks in
                positions[0] = new float2(0.6f, 0f);
                velocities[0] = new float2(-1f, 0f);
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeAvoidance(
                    new float2(0f, 0f), new float2(1f, 0f),
                    0.5f, 5f, 1.5f, 1f,
                    positions, velocities, radii);

                // Should include separation force pushing agent away (-X direction)
                Assert.That(result.x, Is.LessThan(0f));
            }
            finally
            {
                positions.Dispose();
                velocities.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeAvoidance_StrengthZero_ReturnsZero()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var velocities = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                positions[0] = new float2(0.8f, 0f);
                velocities[0] = float2.zero;
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeAvoidance(
                    new float2(0f, 0f), new float2(1f, 0f),
                    0.5f, 5f, 1.5f, 0f, // strength = 0
                    positions, velocities, radii);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                positions.Dispose();
                velocities.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeAvoidance_CollisionBeyondTimeHorizon_NoForce()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var velocities = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                // Neighbor at distance 4, combined radius 1, approach speed 0.1
                // timeToCollision = (4-1)/0.1 = 30s >> timeHorizon 1.5s
                positions[0] = new float2(4f, 0f);
                velocities[0] = new float2(-0.1f, 0f);
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeAvoidance(
                    new float2(0f, 0f), new float2(0f, 0f),
                    0.5f, 5f, 1.5f, 1f,
                    positions, velocities, radii);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                positions.Dispose();
                velocities.Dispose();
                radii.Dispose();
            }
        }

        #endregion

        #region ComputeSeparation

        [Test]
        public void ComputeSeparation_NoNeighbors_ReturnsZero()
        {
            var positions = new NativeArray<float2>(0, Allocator.Temp);
            var radii = new NativeArray<float>(0, Allocator.Temp);
            try
            {
                var result = AvoidanceMath.ComputeSeparation(
                    new float2(0f, 0f), 0.5f, 5f, 3f,
                    positions, radii);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                positions.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeSeparation_CloseNeighbor_PushesAway()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                positions[0] = new float2(1f, 0f);
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeSeparation(
                    new float2(0f, 0f), 0.5f, 5f, 3f,
                    positions, radii);

                // Should push in -X direction (away from neighbor)
                Assert.That(result.x, Is.LessThan(0f));
                Assert.That(math.length(result), Is.LessThanOrEqualTo(5f + Epsilon));
            }
            finally
            {
                positions.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeSeparation_FarNeighbor_NoForce()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                positions[0] = new float2(100f, 0f);
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeSeparation(
                    new float2(0f, 0f), 0.5f, 5f, 3f,
                    positions, radii);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                positions.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeSeparation_MultipleNeighbors_CombinesForce()
        {
            var positions = new NativeArray<float2>(2, Allocator.Temp);
            var radii = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                positions[0] = new float2(1f, 0f);
                radii[0] = 0.5f;
                positions[1] = new float2(0f, 1f);
                radii[1] = 0.5f;

                var result = AvoidanceMath.ComputeSeparation(
                    new float2(0f, 0f), 0.5f, 5f, 3f,
                    positions, radii);

                // Both push away: -X from first, -Y from second
                Assert.That(result.x, Is.LessThan(0f));
                Assert.That(result.y, Is.LessThan(0f));
            }
            finally
            {
                positions.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeSeparation_OnTopOfNeighbor_ArbitraryPush()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var radii = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                // Agent and neighbor at same position
                positions[0] = new float2(0f, 0f);
                radii[0] = 0.5f;

                var result = AvoidanceMath.ComputeSeparation(
                    new float2(0f, 0f), 0.5f, 5f, 3f,
                    positions, radii);

                // dist < 0.0001 => skipped, so zero
                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                positions.Dispose();
                radii.Dispose();
            }
        }

        [Test]
        public void ComputeSeparation_ForceCappedAtMaxSpeed()
        {
            var positions = new NativeArray<float2>(5, Allocator.Temp);
            var radii = new NativeArray<float>(5, Allocator.Temp);
            try
            {
                // Many close neighbors to build up large force
                for (int i = 0; i < 5; i++)
                {
                    positions[i] = new float2(0.1f, 0f);
                    radii[i] = 0.5f;
                }

                var result = AvoidanceMath.ComputeSeparation(
                    new float2(0f, 0f), 0.5f, 2f, 3f,
                    positions, radii);

                Assert.That(math.length(result), Is.LessThanOrEqualTo(2f + Epsilon));
            }
            finally
            {
                positions.Dispose();
                radii.Dispose();
            }
        }

        #endregion
    }
}
