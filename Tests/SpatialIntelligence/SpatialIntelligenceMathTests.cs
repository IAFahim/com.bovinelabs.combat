using NUnit.Framework;
using BovineLabs.Combat.SpatialIntelligence;
using BovineLabs.Combat.Core;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.SpatialIntelligence.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class SpatialIntelligenceMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeThreatDensity

        [Test]
        public void ComputeThreatDensity_ReturnsCorrectDensity()
        {
            // 4 enemies in radius 10: density = 4 / (PI * 100)
            float density = SpatialIntelligenceMath.ComputeThreatDensity(4, 10f);
            float expected = 4f / (math.PI * 100f);
            Assert.That(density, Is.EqualTo(expected).Within(Epsilon));
        }

        [Test]
        public void ComputeThreatDensity_ZeroRadius_ReturnsZero()
        {
            float density = SpatialIntelligenceMath.ComputeThreatDensity(5, 0f);
            Assert.That(density, Is.EqualTo(0f));
        }

        [Test]
        public void ComputeThreatDensity_ZeroEnemies_ReturnsZero()
        {
            float density = SpatialIntelligenceMath.ComputeThreatDensity(0, 10f);
            Assert.That(density, Is.EqualTo(0f));
        }

        [Test]
        public void ComputeThreatDensity_FromBuffer_CountsCorrectly()
        {
            var neighbors = new NativeArray<SpatialNeighborData>(4, Allocator.Temp);
            try
            {
                for (int i = 0; i < 4; i++)
                {
                    neighbors[i] = new SpatialNeighborData
                    {
                        Entity = default,
                        Distance = 5f,
                        Direction = math.normalize(new float2(i, i + 1)),
                        TeamId = 2,
                        Velocity = float2.zero,
                        Radius = 0.5f
                    };
                }

                float density = SpatialIntelligenceMath.ComputeThreatDensity(neighbors, 10f);
                float expected = 4f / (math.PI * 100f);
                Assert.That(density, Is.EqualTo(expected).Within(Epsilon));
            }
            finally
            {
                neighbors.Dispose();
            }
        }

        #endregion

        #region ComputeCentroid

        [Test]
        public void ComputeCentroid_SinglePosition_ReturnsThatPosition()
        {
            var positions = new NativeList<float2>(Allocator.Temp);
            try
            {
                positions.Add(new float2(3f, 7f));

                float2 centroid = SpatialIntelligenceMath.ComputeCentroid(positions);

                Assert.That(centroid.x, Is.EqualTo(3f).Within(Epsilon));
                Assert.That(centroid.y, Is.EqualTo(7f).Within(Epsilon));
            }
            finally
            {
                positions.Dispose();
            }
        }

        [Test]
        public void ComputeCentroid_MultiplePositions_ReturnsAverage()
        {
            var positions = new NativeList<float2>(Allocator.Temp);
            try
            {
                positions.Add(new float2(0f, 0f));
                positions.Add(new float2(10f, 0f));
                positions.Add(new float2(0f, 10f));
                positions.Add(new float2(10f, 10f));

                float2 centroid = SpatialIntelligenceMath.ComputeCentroid(positions);

                Assert.That(centroid.x, Is.EqualTo(5f).Within(Epsilon));
                Assert.That(centroid.y, Is.EqualTo(5f).Within(Epsilon));
            }
            finally
            {
                positions.Dispose();
            }
        }

        [Test]
        public void ComputeCentroid_EmptyList_ReturnsZero()
        {
            var positions = new NativeList<float2>(Allocator.Temp);
            try
            {
                float2 centroid = SpatialIntelligenceMath.ComputeCentroid(positions);
                Assert.That(centroid.x, Is.EqualTo(0f));
                Assert.That(centroid.y, Is.EqualTo(0f));
            }
            finally
            {
                positions.Dispose();
            }
        }

        #endregion

        #region ComputeThreatAssessment

        [Test]
        public void ComputeThreatAssessment_NoNeighbors_ReturnsZeroCounts()
        {
            var neighbors = new NativeArray<SpatialNeighborData>(0, Allocator.Temp);
            try
            {
                SpatialIntelligenceMath.ComputeThreatAssessment(
                    float2.zero, neighbors, 1, 10f, out var result);

                Assert.That(result.EnemyCount, Is.EqualTo(0));
                Assert.That(result.AllyCount, Is.EqualTo(0));
                Assert.That(result.NearestEnemyDistance, Is.EqualTo(float.MaxValue));
                Assert.That(result.ThreatDensity, Is.EqualTo(0f));
            }
            finally
            {
                neighbors.Dispose();
            }
        }

        [Test]
        public void ComputeThreatAssessment_MixedTeams_CountsCorrectly()
        {
            var neighbors = new NativeArray<SpatialNeighborData>(5, Allocator.Temp);
            try
            {
                // 3 enemies (team 2), 2 friendlies (team 1), myTeam = 1
                neighbors[0] = new SpatialNeighborData
                {
                    Entity = default, Distance = 3f, Direction = new float2(1f, 0f),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };
                neighbors[1] = new SpatialNeighborData
                {
                    Entity = default, Distance = 5f, Direction = new float2(0f, 1f),
                    TeamId = 1, Velocity = float2.zero, Radius = 0.5f
                };
                neighbors[2] = new SpatialNeighborData
                {
                    Entity = default, Distance = 7f, Direction = new float2(-1f, 0f),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };
                neighbors[3] = new SpatialNeighborData
                {
                    Entity = default, Distance = 2f, Direction = new float2(0f, -1f),
                    TeamId = 1, Velocity = float2.zero, Radius = 0.5f
                };
                neighbors[4] = new SpatialNeighborData
                {
                    Entity = default, Distance = 9f, Direction = new float2(1f, 1f),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };

                SpatialIntelligenceMath.ComputeThreatAssessment(
                    float2.zero, neighbors, 1, 10f, out var result);

                Assert.That(result.EnemyCount, Is.EqualTo(3));
                Assert.That(result.AllyCount, Is.EqualTo(2));
            }
            finally
            {
                neighbors.Dispose();
            }
        }

        [Test]
        public void ComputeThreatAssessment_FindsNearestEnemy()
        {
            var neighbors = new NativeArray<SpatialNeighborData>(3, Allocator.Temp);
            try
            {
                // Nearest enemy at distance 2
                neighbors[0] = new SpatialNeighborData
                {
                    Entity = default, Distance = 8f, Direction = new float2(1f, 0f),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };
                neighbors[1] = new SpatialNeighborData
                {
                    Entity = default, Distance = 2f, Direction = new float2(0f, 1f),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };
                neighbors[2] = new SpatialNeighborData
                {
                    Entity = default, Distance = 5f, Direction = new float2(-1f, 0f),
                    TeamId = 1, Velocity = float2.zero, Radius = 0.5f
                };

                SpatialIntelligenceMath.ComputeThreatAssessment(
                    float2.zero, neighbors, 1, 10f, out var result);

                Assert.That(result.NearestEnemyDistance, Is.EqualTo(2f).Within(Epsilon));
                // Nearest enemy direction should point toward the closest one
                Assert.That(result.NearestEnemyDirection.y, Is.GreaterThan(0.9f));
            }
            finally
            {
                neighbors.Dispose();
            }
        }

        #endregion
    }
}
