using NUnit.Framework;
using BovineLabs.Combat.Flank;
using Unity.Mathematics;

namespace BovineLabs.Combat.Flank.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class FlankMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeFlankPosition

        [Test]
        public void ComputeFlankPosition_ZeroAngle_BehindTarget()
        {
            // Target at origin, facing +Y (angle=0), offset=0
            // worldAngle = 0 + PI + 0 = PI
            // offset = (sin(PI)*dist, cos(PI)*dist) = (0, -dist)
            var pos = FlankMath.ComputeFlankPosition(
                new float2(0f, 0f), 0f, 0f, 5f);

            Assert.That(pos.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(-5f).Within(Epsilon));
        }

        [Test]
        public void ComputeFlankPosition_RightFlank()
        {
            // Target facing +Y (angle=0), offset = PI/2 (right flank)
            // worldAngle = 0 + PI + PI/2 = 3PI/2
            // offset = (sin(3PI/2)*dist, cos(3PI/2)*dist) = (-dist, 0)
            var pos = FlankMath.ComputeFlankPosition(
                new float2(0f, 0f), 0f, math.PI / 2f, 5f);

            Assert.That(pos.x, Is.EqualTo(-5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeFlankPosition_LeftFlank()
        {
            // Target facing +Y (angle=0), offset = -PI/2 (left flank)
            // worldAngle = 0 + PI - PI/2 = PI/2
            // offset = (sin(PI/2)*dist, cos(PI/2)*dist) = (dist, 0)
            var pos = FlankMath.ComputeFlankPosition(
                new float2(0f, 0f), 0f, -math.PI / 2f, 5f);

            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeFlankPosition_NonZeroTarget_OffsetFromTarget()
        {
            var pos = FlankMath.ComputeFlankPosition(
                new float2(10f, 20f), 0f, 0f, 3f);

            // Should be offset from target, not origin
            Assert.That(pos.x, Is.EqualTo(10f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(17f).Within(Epsilon)); // 20 - 3
        }

        [Test]
        public void ComputeFlankPosition_ZeroDistance_AtTarget()
        {
            var pos = FlankMath.ComputeFlankPosition(
                new float2(5f, 5f), 0f, 0f, 0f);

            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(5f).Within(Epsilon));
        }

        #endregion

        #region ComputeFlankDirection

        [Test]
        public void ComputeFlankDirection_TowardFlankPosition_ReturnsScaledDirection()
        {
            var dir = FlankMath.ComputeFlankDirection(
                new float2(0f, 0f), new float2(10f, 0f), 5f);

            Assert.That(dir.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeFlankDirection_AtFlankPosition_ReturnsZero()
        {
            var dir = FlankMath.ComputeFlankDirection(
                new float2(5f, 5f), new float2(5f, 5f), 5f);

            Assert.That(dir, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeFlankDirection_VeryCloseToTarget_ReturnsZero()
        {
            var dir = FlankMath.ComputeFlankDirection(
                new float2(5f, 5f), new float2(5.00005f, 5f), 5f);

            Assert.That(dir, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeFlankDirection_Diagonal()
        {
            var dir = FlankMath.ComputeFlankDirection(
                new float2(0f, 0f), new float2(3f, 4f), 10f);

            // normalize(3,4) = (0.6, 0.8) * 10 = (6, 8)
            Assert.That(dir.x, Is.EqualTo(6f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(8f).Within(Epsilon));
        }

        [Test]
        public void ComputeFlankDirection_ZeroMaxSpeed_ReturnsZero()
        {
            var dir = FlankMath.ComputeFlankDirection(
                new float2(0f, 0f), new float2(10f, 0f), 0f);

            Assert.That(dir, Is.EqualTo(float2.zero));
        }

        #endregion
    }
}
