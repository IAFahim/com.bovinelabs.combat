using NUnit.Framework;
using BovineLabs.Combat.GridIntelligence;
using BovineLabs.Combat.Core;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.GridIntelligence.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class GridIntelligenceMathTests
    {
        const float Epsilon = 0.001f;

        #region WorldToCell

        [Test]
        public void WorldToCell_Center_ReturnsMiddleCell()
        {
            // Grid: 10x10 cells, cellSize=2, origin at (0,0)
            // World (10,10) => relative (10,10) / cellSize(2,2) = cell (5,5)
            int2 cell = GridIntelligenceMath.WorldToCell(
                new float2(10f, 10f),
                new float2(0f, 0f),
                new float2(2f, 2f),
                new int2(10, 10));

            Assert.That(cell.x, Is.EqualTo(5));
            Assert.That(cell.y, Is.EqualTo(5));
        }

        [Test]
        public void WorldToCell_OutOfBounds_ReturnsEdgeClamped()
        {
            // Grid: 5x5, cellSize=1, origin at (0,0)
            // World (100, -50) => clamp to (4, 0)
            int2 cell = GridIntelligenceMath.WorldToCell(
                new float2(100f, -50f),
                new float2(0f, 0f),
                new float2(1f, 1f),
                new int2(5, 5));

            Assert.That(cell.x, Is.EqualTo(4));
            Assert.That(cell.y, Is.EqualTo(0));
        }

        #endregion

        #region CellToWorld

        [Test]
        public void CellToWorld_MiddleCell_ReturnsCenter()
        {
            // Cell (5,5), origin (0,0), cellSize (2,2)
            // world = origin + cell * cellSize + cellSize * 0.5 = 0 + 10 + 1 = 11
            float2 world = GridIntelligenceMath.CellToWorld(
                new int2(5, 5),
                new float2(0f, 0f),
                new float2(2f, 2f));

            Assert.That(world.x, Is.EqualTo(11f).Within(Epsilon));
            Assert.That(world.y, Is.EqualTo(11f).Within(Epsilon));
        }

        #endregion

        #region ComputeFlankingDirection

        [Test]
        public void ComputeFlankingDirection_FromNorth_ReturnsEast()
        {
            // Danger direction (0,1) = North => flank = perpendicular CW = (1,0) = East
            float2 flank = GridIntelligenceMath.ComputeFlankingDirection(
                new float2(0f, 1f));

            Assert.That(math.length(flank), Is.EqualTo(1f).Within(Epsilon));
            Assert.That(math.dot(flank, new float2(0f, 1f)), Is.EqualTo(0f).Within(Epsilon));
            Assert.That(flank.x, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(flank.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeFlankingDirection_Zero_ReturnsZero()
        {
            float2 flank = GridIntelligenceMath.ComputeFlankingDirection(float2.zero);

            Assert.That(flank.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(flank.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeFlankingDirection_FromEast_ReturnsSouth()
        {
            // Danger direction (1,0) = East => flank = (0,-1) = South (CW rotation)
            float2 flank = GridIntelligenceMath.ComputeFlankingDirection(
                new float2(1f, 0f));

            Assert.That(flank.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(flank.y, Is.EqualTo(-1f).Within(Epsilon));
        }

        #endregion

        #region ComputeGridAnalysis

        [Test]
        public void ComputeGridAnalysis_NoNeighbors_AllZeros()
        {
            var neighbors = new NativeArray<SpatialNeighborData>(0, Allocator.Temp);
            try
            {
                int resolution = 8;
                var cellSize = new float2(5f, 5f);

                GridIntelligenceMath.ComputeGridAnalysis(
                    resolution, cellSize,
                    neighbors, 1, 0.5f, out var result);

                Assert.That(result.MaxThreatDensity, Is.EqualTo(0f));
                Assert.That(result.DangerousCellCount, Is.EqualTo(0));
                Assert.That(result.AverageThreatDensity, Is.EqualTo(0f));
            }
            finally
            {
                neighbors.Dispose();
            }
        }

        [Test]
        public void ComputeGridAnalysis_EnemiesOnOneSide_DangerDirectionPointsThere()
        {
            var neighbors = new NativeArray<SpatialNeighborData>(3, Allocator.Temp);
            try
            {
                // 3 enemies to the right (+X direction) from agent
                neighbors[0] = new SpatialNeighborData
                {
                    Entity = default, Distance = 5f, Direction = new float2(1f, 0f),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };
                neighbors[1] = new SpatialNeighborData
                {
                    Entity = default, Distance = 6f, Direction = math.normalize(new float2(1f, 0.5f)),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };
                neighbors[2] = new SpatialNeighborData
                {
                    Entity = default, Distance = 4f, Direction = math.normalize(new float2(1f, -0.3f)),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };

                int resolution = 8;
                var cellSize = new float2(5f, 5f);

                GridIntelligenceMath.ComputeGridAnalysis(
                    resolution, cellSize,
                    neighbors, 1, 0.01f, out var result);

                // Danger direction should point toward enemies (positive X component)
                Assert.That(result.DangerDirection.x, Is.GreaterThan(0f));
                Assert.That(result.MaxThreatDensity, Is.GreaterThan(0f));
                Assert.That(result.DangerousCellCount, Is.GreaterThan(0));
            }
            finally
            {
                neighbors.Dispose();
            }
        }

        [Test]
        public void ComputeGridAnalysis_FlankingIsPerpendicularToDanger()
        {
            var neighbors = new NativeArray<SpatialNeighborData>(1, Allocator.Temp);
            try
            {
                neighbors[0] = new SpatialNeighborData
                {
                    Entity = default, Distance = 5f, Direction = new float2(0f, 1f),
                    TeamId = 2, Velocity = float2.zero, Radius = 0.5f
                };

                int resolution = 4;
                var cellSize = new float2(10f, 10f);

                GridIntelligenceMath.ComputeGridAnalysis(
                    resolution, cellSize,
                    neighbors, 1, 0.0f, out var result);

                // Flanking should be perpendicular to danger direction
                if (math.lengthsq(result.DangerDirection) > 0.001f)
                {
                    float dot = math.dot(result.FlankingDirection, result.DangerDirection);
                    Assert.That(math.abs(dot), Is.LessThan(Epsilon));
                }
            }
            finally
            {
                neighbors.Dispose();
            }
        }

        #endregion
    }
}
