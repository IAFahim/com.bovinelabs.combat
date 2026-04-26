using NUnit.Framework;
using BovineLabs.Combat.Formation;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.Formation.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class FormationMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeLinePositions

        [Test]
        public void ComputeLinePositions_ZeroCount_ReturnsEmpty()
        {
            var result = FormationMath.ComputeLinePositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 0, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(0));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeLinePositions_SingleUnit_PlacedAtRightSpacing()
        {
            var result = FormationMath.ComputeLinePositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 1, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(1));
                // side = (0+1)/2 * 1 = 1, right = (-1, 0)
                // pos = (0,0) + (-1,0) * 1 * 2 = (-2, 0)
                Assert.That(result[0].x, Is.EqualTo(-2f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(0f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeLinePositions_FourUnits_AlternatesLeftRight()
        {
            var result = FormationMath.ComputeLinePositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 4, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(4));
                // i=0: side = 1/2*1 = 1, right*(-2,0)
                // i=1: side = 2/2*(-1) = -1, right*(2,0)
                // i=2: side = 3/2*1 = 1 (but integer), actually (3/2)=1*1=1... 
                // Let me recalculate:
                // i=0: (0+1)/2 = 0 * (0%2==0?1:-1) = 0*1 = 0 => wait, (1/2)=0 (integer)
                // i=0: side = ((0+1)/2) * (0%2==0?1:-1) = (1/2=0) * 1 = 0
                // i=1: side = ((1+1)/2) * (1%2==0?1:-1) = (2/2=1) * -1 = -1
                // i=2: side = ((2+1)/2) * (2%2==0?1:-1) = (3/2=1) * 1 = 1
                // i=3: side = ((3+1)/2) * (3%2==0?1:-1) = (4/2=2) * -1 = -2
                Assert.That(result[0].x, Is.EqualTo(0f).Within(Epsilon));  // side=0
                Assert.That(result[0].y, Is.EqualTo(0f).Within(Epsilon));

                // result[1]: right*(-1,0)*(-1)*2 = (2,0)
                Assert.That(result[1].x, Is.EqualTo(2f).Within(Epsilon));
                Assert.That(result[1].y, Is.EqualTo(0f).Within(Epsilon));

                // result[2]: right*(-1,0)*(1)*2 = (-2,0)
                Assert.That(result[2].x, Is.EqualTo(-2f).Within(Epsilon));
                Assert.That(result[2].y, Is.EqualTo(0f).Within(Epsilon));

                // result[3]: right*(-1,0)*(-2)*2 = (4,0)
                Assert.That(result[3].x, Is.EqualTo(4f).Within(Epsilon));
                Assert.That(result[3].y, Is.EqualTo(0f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeLinePositions_NonZeroLeader_OffsetCorrectly()
        {
            var result = FormationMath.ComputeLinePositions(
                new float2(10f, 20f), new float2(0f, 1f), 1f, 1, Allocator.Temp);
            try
            {
                // side=0 for i=0
                Assert.That(result[0].x, Is.EqualTo(10f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(20f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        #endregion

        #region ComputeWedgePositions

        [Test]
        public void ComputeWedgePositions_ZeroCount_ReturnsEmpty()
        {
            var result = FormationMath.ComputeWedgePositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 0.5f, 0, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(0));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeWedgePositions_TwoUnits_FormV()
        {
            var result = FormationMath.ComputeWedgePositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, math.PI / 4f, 2, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(2));
                // i=0: side=1, rank=0 => wait (0+1)/2 = 0... rank=0
                // Actually: rank = (0+1)/2 = 0
                // So positions[0] = leaderPos + dir * 0 = leaderPos
                // i=1: side=-1, rank = (1+1)/2 = 1
                // positions[1] = leaderPos + leftDir * spacing

                // First slot at leader pos (rank 0)
                Assert.That(result[0].x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(0f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeWedgePositions_PositionsBehindLeader()
        {
            var result = FormationMath.ComputeWedgePositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, math.PI / 6f, 4, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(4));
                // All positions should be behind the leader (negative Y for forward=(0,1))
                // i=0 rank=0 => at leader pos
                // i=1 rank=1 => behind
                // i=2 rank=1 => behind
                // i=3 rank=2 => further behind
                // rank>0 positions should have y <= 0
                for (int i = 1; i < result.Length; i++)
                {
                    Assert.That(result[i].y, Is.LessThanOrEqualTo(0f + Epsilon),
                        $"Position {i} should be behind leader");
                }
            }
            finally
            {
                result.Dispose();
            }
        }

        #endregion

        #region ComputeGridPositions

        [Test]
        public void ComputeGridPositions_ZeroCount_ReturnsEmpty()
        {
            var result = FormationMath.ComputeGridPositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 3, 0, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(0));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeGridPositions_ThreeColumns_SixUnits_TwoRows()
        {
            var result = FormationMath.ComputeGridPositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 3, 6, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(6));

                // Row 0: i=0,1,2 -> behind leader (backward = (0,-1))
                // Row 1: i=3,4,5 -> further behind
                // backward = (0,-1), right = (-1,0)

                // i=0: row=0, col=0, colOffset = 0-(3-1)*0.5 = -1, rowOffset=1
                // pos = (0,0) + (0,-1)*2 + (-1,0)*(-2) = (2, -2)
                Assert.That(result[0].x, Is.EqualTo(2f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(-2f).Within(Epsilon));

                // i=1: row=0, col=1, colOffset = 1-1 = 0, rowOffset=1
                // pos = (0,0) + (0,-1)*2 + (-1,0)*0 = (0, -2)
                Assert.That(result[1].x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result[1].y, Is.EqualTo(-2f).Within(Epsilon));

                // i=2: row=0, col=2, colOffset = 2-1 = 1, rowOffset=1
                // pos = (0,0) + (0,-1)*2 + (-1,0)*2 = (-2, -2)
                Assert.That(result[2].x, Is.EqualTo(-2f).Within(Epsilon));
                Assert.That(result[2].y, Is.EqualTo(-2f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeGridPositions_ZeroColumns_ClampedToOne()
        {
            var result = FormationMath.ComputeGridPositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 0, 3, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(3));
                // All 3 in a single column behind leader
                // cols=1 (clamped), colOffset = 0 - 0 = 0
                for (int i = 0; i < 3; i++)
                {
                    Assert.That(result[i].x, Is.EqualTo(0f).Within(Epsilon));
                    Assert.That(result[i].y, Is.EqualTo(-(i + 1) * 2f).Within(Epsilon));
                }
            }
            finally
            {
                result.Dispose();
            }
        }

        #endregion

        #region ComputeCirclePositions

        [Test]
        public void ComputeCirclePositions_ZeroCount_ReturnsEmpty()
        {
            var result = FormationMath.ComputeCirclePositions(
                new float2(0f, 0f), 5f, 0, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(0));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeCirclePositions_SingleUnit_AtRadius()
        {
            var result = FormationMath.ComputeCirclePositions(
                new float2(0f, 0f), 5f, 1, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(1));
                // angle = 2*PI*0/1 = 0 => (cos(0)*5, sin(0)*5) = (5,0)
                Assert.That(result[0].x, Is.EqualTo(5f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(0f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeCirclePositions_FourUnits_EvenlySpaced()
        {
            var result = FormationMath.ComputeCirclePositions(
                new float2(0f, 0f), 3f, 4, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(4));
                // i=0: angle=0 => (3,0)
                // i=1: angle=PI/2 => (0,3)
                // i=2: angle=PI => (-3,0)
                // i=3: angle=3PI/2 => (0,-3)
                Assert.That(result[0].x, Is.EqualTo(3f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result[1].x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result[1].y, Is.EqualTo(3f).Within(Epsilon));
                Assert.That(result[2].x, Is.EqualTo(-3f).Within(Epsilon));
                Assert.That(result[2].y, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result[3].x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result[3].y, Is.EqualTo(-3f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeCirclePositions_AllAtDistanceFromCenter()
        {
            var center = new float2(10f, 20f);
            const float radius = 7f;
            var result = FormationMath.ComputeCirclePositions(center, radius, 8, Allocator.Temp);
            try
            {
                for (int i = 0; i < result.Length; i++)
                {
                    var dist = math.length(result[i] - center);
                    Assert.That(dist, Is.EqualTo(radius).Within(Epsilon));
                }
            }
            finally
            {
                result.Dispose();
            }
        }

        #endregion

        #region ComputeColumnPositions

        [Test]
        public void ComputeColumnPositions_ZeroCount_ReturnsEmpty()
        {
            var result = FormationMath.ComputeColumnPositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 0, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(0));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeColumnPositions_ThreeUnits_BehindLeader()
        {
            var result = FormationMath.ComputeColumnPositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, 3, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(3));
                // backward = (0,-1)
                // i=0: (0,0) + (0,-1)*2 = (0,-2)
                Assert.That(result[0].x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(-2f).Within(Epsilon));
                // i=1: (0,0) + (0,-1)*4 = (0,-4)
                Assert.That(result[1].y, Is.EqualTo(-4f).Within(Epsilon));
                // i=2: (0,0) + (0,-1)*6 = (0,-6)
                Assert.That(result[2].y, Is.EqualTo(-6f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeColumnPositions_ForwardAlongX_BehindIsNegX()
        {
            var result = FormationMath.ComputeColumnPositions(
                new float2(0f, 0f), new float2(1f, 0f), 3f, 2, Allocator.Temp);
            try
            {
                // backward = (-1, 0)
                // i=0: (0,0) + (-1,0)*3 = (-3,0)
                Assert.That(result[0].x, Is.EqualTo(-3f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(0f).Within(Epsilon));
                // i=1: (0,0) + (-1,0)*6 = (-6,0)
                Assert.That(result[1].x, Is.EqualTo(-6f).Within(Epsilon));
            }
            finally
            {
                result.Dispose();
            }
        }

        #endregion

        #region ComputeVPositions

        [Test]
        public void ComputeVPositions_ZeroCount_ReturnsEmpty()
        {
            var result = FormationMath.ComputeVPositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, math.PI / 4f, 0, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(0));
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeVPositions_FourUnits_FormVShape()
        {
            var result = FormationMath.ComputeVPositions(
                new float2(0f, 0f), new float2(0f, 1f), 2f, math.PI / 6f, 4, Allocator.Temp);
            try
            {
                Assert.That(result.Length, Is.EqualTo(4));
                // i=0: side=1, rank=0 => pos = leader + backward*0 + right*0 = leader
                Assert.That(result[0].x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result[0].y, Is.EqualTo(0f).Within(Epsilon));

                // i=1: side=-1, rank=1 => back=1*2*cos(PI/6), side=-1*2*sin(PI/6)
                var backAmount = 2f * math.cos(math.PI / 6f);
                var sideAmount = -2f * math.sin(math.PI / 6f);
                Assert.That(result[1].y, Is.EqualTo(-backAmount).Within(Epsilon));
                Assert.That(result[1].x, Is.EqualTo(-sideAmount).Within(Epsilon)); // right = (-1,0), so right * sideAmount
            }
            finally
            {
                result.Dispose();
            }
        }

        [Test]
        public void ComputeVPositions_AllPositionsBehindLeader()
        {
            var result = FormationMath.ComputeVPositions(
                new float2(5f, 5f), new float2(0f, 1f), 3f, math.PI / 4f, 6, Allocator.Temp);
            try
            {
                // All positions except rank 0 should be behind leader (lower Y)
                // forward = (0,1), backward = (0,-1)
                // backAmount = rank*spacing*cos(angle) >= 0
                for (int i = 0; i < result.Length; i++)
                {
                    var rank = (i + 1) / 2;
                    if (rank > 0)
                    {
                        Assert.That(result[i].y, Is.LessThanOrEqualTo(5f + Epsilon));
                    }
                }
            }
            finally
            {
                result.Dispose();
            }
        }

        #endregion
    }
}
