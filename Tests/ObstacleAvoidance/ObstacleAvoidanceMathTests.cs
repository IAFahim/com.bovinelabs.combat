using NUnit.Framework;
using BovineLabs.Combat.ObstacleAvoidance;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.ObstacleAvoidance.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class ObstacleAvoidanceMathTests
    {
        const float Epsilon = 0.001f;

        #region WallSliding

        [Test]
        public void WallSliding_PerpendicularVelocity_RemovesIntoWall()
        {
            // Velocity going into wall (wall normal points away from wall, i.e. toward agent)
            // Agent moving right (1,0), wall normal pointing left (-1,0)
            var result = ObstacleAvoidanceMath.WallSliding(
                new float2(0f, 0f),
                new float2(1f, 0f),
                new float2(-1f, 0f),
                1f);

            // dot = dot((1,0), (-1,0)) = -1
            // projected = (1,0) - (-1,0)*(-1) = (1,0) - (1,0) = (0,0)
            Assert.That(result.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void WallSliding_ParallelVelocity_NoChange()
        {
            // Velocity parallel to wall - moving away, no adjustment
            // dot >= 0 means moving away from wall
            var result = ObstacleAvoidanceMath.WallSliding(
                new float2(0f, 0f),
                new float2(1f, 0f),
                new float2(1f, 0f),
                1f);

            // dot = dot((1,0), (1,0)) = 1 >= 0, return desiredVelocity unchanged
            Assert.That(result.x, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void WallSliding_DiagonalVelocity_SlidesAlongWall()
        {
            // Velocity going diagonally into wall
            // desiredVelocity = (1, 1), wallNormal = (-1, 0)
            var result = ObstacleAvoidanceMath.WallSliding(
                new float2(0f, 0f),
                new float2(1f, 1f),
                new float2(-1f, 0f),
                1f);

            // dot = dot((1,1), (-1,0)) = -1
            // projected = (1,1) - (-1,0)*(-1) = (1,1) - (1,0) = (0,1)
            // result = (0,1) * 1 = (0,1)
            Assert.That(result.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(1f).Within(Epsilon));
        }

        [Test]
        public void WallSliding_SlideStrength_ScalesOutput()
        {
            var result = ObstacleAvoidanceMath.WallSliding(
                new float2(0f, 0f),
                new float2(1f, 1f),
                new float2(-1f, 0f),
                0.5f);

            // projected = (0,1) as above, scaled by 0.5
            Assert.That(result.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(0.5f).Within(Epsilon));
        }

        [Test]
        public void WallSliding_ZeroVelocity_ReturnsZero()
        {
            var result = ObstacleAvoidanceMath.WallSliding(
                new float2(0f, 0f),
                float2.zero,
                new float2(-1f, 0f),
                1f);

            Assert.That(result.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(0f).Within(Epsilon));
        }

        #endregion

        #region RaycastFan

        [Test]
        public void RaycastFan_SingleRay_MatchesFacingAngle()
        {
            var angles = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                var dirs = ObstacleAvoidanceMath.RaycastFan(
                    float2.zero, 0f, 1, 5f, angles);

                try
                {
                    Assert.That(angles[0], Is.EqualTo(0f).Within(Epsilon));
                    // direction = (sin(0), cos(0)) * 5 = (0, 5)
                    Assert.That(dirs[0].x, Is.EqualTo(0f).Within(Epsilon));
                    Assert.That(dirs[0].y, Is.EqualTo(5f).Within(Epsilon));
                }
                finally
                {
                    dirs.Dispose();
                }
            }
            finally
            {
                angles.Dispose();
            }
        }

        [Test]
        public void RaycastFan_ThreeRays_CorrectSpread()
        {
            var angles = new NativeArray<float>(3, Allocator.Temp);
            try
            {
                var dirs = ObstacleAvoidanceMath.RaycastFan(
                    float2.zero, 0f, 3, 10f, angles);

                try
                {
                    // 3 rays: spread from -PI/2 to +PI/2
                    // angles: -PI/2, 0, +PI/2
                    Assert.That(angles[0], Is.EqualTo(-math.PI / 2f).Within(Epsilon));
                    Assert.That(angles[1], Is.EqualTo(0f).Within(Epsilon));
                    Assert.That(angles[2], Is.EqualTo(math.PI / 2f).Within(Epsilon));

                    // All directions should have length 10
                    Assert.That(math.length(dirs[0]), Is.EqualTo(10f).Within(Epsilon));
                    Assert.That(math.length(dirs[1]), Is.EqualTo(10f).Within(Epsilon));
                    Assert.That(math.length(dirs[2]), Is.EqualTo(10f).Within(Epsilon));
                }
                finally
                {
                    dirs.Dispose();
                }
            }
            finally
            {
                angles.Dispose();
            }
        }

        [Test]
        public void RaycastFan_FiveRays_CorrectCount()
        {
            var angles = new NativeArray<float>(5, Allocator.Temp);
            try
            {
                var dirs = ObstacleAvoidanceMath.RaycastFan(
                    float2.zero, math.PI / 4f, 5, 1f, angles);

                try
                {
                    Assert.That(dirs.Length, Is.EqualTo(5));

                    // Step = PI / (5-1) = PI/4
                    // Angles: facingAngle - PI/2 + step*i
                    // = PI/4 - PI/2 + i*PI/4 = -PI/4 + i*PI/4
                    // = -PI/4, 0, PI/4, PI/2, 3PI/4
                    Assert.That(angles[0], Is.EqualTo(-math.PI / 4f).Within(Epsilon));
                    Assert.That(angles[1], Is.EqualTo(0f).Within(Epsilon));
                    Assert.That(angles[2], Is.EqualTo(math.PI / 4f).Within(Epsilon));
                    Assert.That(angles[3], Is.EqualTo(math.PI / 2f).Within(Epsilon));
                    Assert.That(angles[4], Is.EqualTo(3f * math.PI / 4f).Within(Epsilon));
                }
                finally
                {
                    dirs.Dispose();
                }
            }
            finally
            {
                angles.Dispose();
            }
        }

        [Test]
        public void RaycastFan_ZeroRays_ReturnsEmpty()
        {
            var angles = new NativeArray<float>(0, Allocator.Temp);
            try
            {
                var dirs = ObstacleAvoidanceMath.RaycastFan(
                    float2.zero, 0f, 0, 1f, angles);

                try
                {
                    Assert.That(dirs.Length, Is.EqualTo(0));
                }
                finally
                {
                    dirs.Dispose();
                }
            }
            finally
            {
                angles.Dispose();
            }
        }

        [Test]
        public void RaycastFan_DirectionsAreNormalizedTimesLength()
        {
            var angles = new NativeArray<float>(3, Allocator.Temp);
            try
            {
                var dirs = ObstacleAvoidanceMath.RaycastFan(
                    float2.zero, 0f, 3, 7f, angles);

                try
                {
                    for (int i = 0; i < 3; i++)
                    {
                        Assert.That(math.length(dirs[i]), Is.EqualTo(7f).Within(Epsilon));
                    }
                }
                finally
                {
                    dirs.Dispose();
                }
            }
            finally
            {
                angles.Dispose();
            }
        }

        #endregion

        #region ComputeObstacleForce

        [Test]
        public void ComputeObstacleForce_NoHits_ZeroForce()
        {
            var hitDistances = new NativeArray<float>(3, Allocator.Temp);
            var hitNormals = new NativeArray<float2>(3, Allocator.Temp);
            var rayDirections = new NativeArray<float2>(3, Allocator.Temp);
            try
            {
                // All distances at float.MaxValue => no hits
                for (int i = 0; i < 3; i++)
                {
                    hitDistances[i] = 1000000f;
                    hitNormals[i] = float2.zero;
                    rayDirections[i] = new float2(0f, 1f);
                }

                var force = ObstacleAvoidanceMath.ComputeObstacleForce(
                    new float2(0f, 0f),
                    new float2(1f, 0f),
                    hitDistances,
                    hitNormals,
                    rayDirections,
                    1f);

                Assert.That(force.x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
            }
            finally
            {
                hitDistances.Dispose();
                hitNormals.Dispose();
                rayDirections.Dispose();
            }
        }

        [Test]
        public void ComputeObstacleForce_WallHit_ProducesRepulsionForce()
        {
            var hitDistances = new NativeArray<float>(1, Allocator.Temp);
            var hitNormals = new NativeArray<float2>(1, Allocator.Temp);
            var rayDirections = new NativeArray<float2>(1, Allocator.Temp);
            try
            {
                // One ray, hit at distance 0.5 (ray length = 1.0)
                hitDistances[0] = 0.5f;
                hitNormals[0] = new float2(1f, 0f); // normal pointing right (away from wall)
                rayDirections[0] = new float2(-1f, 0f); // ray pointing left, length 1

                var force = ObstacleAvoidanceMath.ComputeObstacleForce(
                    new float2(0f, 0f),
                    new float2(-1f, 0f),
                    hitDistances,
                    hitNormals,
                    rayDirections,
                    1f);

                // ratio = 0.5 / 1.0 = 0.5
                // strength = 1 - 0.5 = 0.5
                // repulsion = normal * strength = (1, 0) * 0.5 = (0.5, 0)
                // desiredVelocity into wall => wall sliding applied
                // dot = dot((-1,0), (1,0)) = -1 < 0
                // slid = (-1,0) - (1,0)*(-1) = (-1,0)+(1,0) = (0,0)
                // avoidanceForce += (0,0) * 0.5 * 0.5 = (0,0)
                // total = (0.5, 0)
                Assert.That(force.x, Is.EqualTo(0.5f).Within(Epsilon));
                Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
            }
            finally
            {
                hitDistances.Dispose();
                hitNormals.Dispose();
                rayDirections.Dispose();
            }
        }

        [Test]
        public void ComputeObstacleForce_CloseHit_StrongerForce()
        {
            var hitDistances = new NativeArray<float>(1, Allocator.Temp);
            var hitNormals = new NativeArray<float2>(1, Allocator.Temp);
            var rayDirections = new NativeArray<float2>(1, Allocator.Temp);
            try
            {
                // Hit at distance 0.1 (very close), ray length 1.0
                hitDistances[0] = 0.1f;
                hitNormals[0] = new float2(0f, 1f);
                rayDirections[0] = new float2(0f, -1f);

                // Use zero desired velocity to isolate repulsion
                var force = ObstacleAvoidanceMath.ComputeObstacleForce(
                    new float2(0f, 0f),
                    float2.zero,
                    hitDistances,
                    hitNormals,
                    rayDirections,
                    1f);

                // ratio = 0.1, strength = 0.9
                // repulsion = (0, 1) * 0.9 = (0, 0.9)
                // zero velocity => no wall sliding contribution
                Assert.That(force.x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(force.y, Is.EqualTo(0.9f).Within(Epsilon));
            }
            finally
            {
                hitDistances.Dispose();
                hitNormals.Dispose();
                rayDirections.Dispose();
            }
        }

        [Test]
        public void ComputeObstacleForce_MultipleHits_AccumulatesForce()
        {
            var hitDistances = new NativeArray<float>(2, Allocator.Temp);
            var hitNormals = new NativeArray<float2>(2, Allocator.Temp);
            var rayDirections = new NativeArray<float2>(2, Allocator.Temp);
            try
            {
                // Two rays, each with a hit at half their ray length
                hitDistances[0] = 0.5f;
                hitNormals[0] = new float2(1f, 0f);
                rayDirections[0] = new float2(-1f, 0f);

                hitDistances[1] = 0.5f;
                hitNormals[1] = new float2(0f, 1f);
                rayDirections[1] = new float2(0f, -1f);

                var force = ObstacleAvoidanceMath.ComputeObstacleForce(
                    new float2(0f, 0f),
                    float2.zero,
                    hitDistances,
                    hitNormals,
                    rayDirections,
                    1f);

                // Each contributes strength 0.5 from repulsion alone
                // Ray 0: (0.5, 0), Ray 1: (0, 0.5)
                Assert.That(force.x, Is.EqualTo(0.5f).Within(Epsilon));
                Assert.That(force.y, Is.EqualTo(0.5f).Within(Epsilon));
            }
            finally
            {
                hitDistances.Dispose();
                hitNormals.Dispose();
                rayDirections.Dispose();
            }
        }

        #endregion
    }
}
