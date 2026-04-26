using NUnit.Framework;
using BovineLabs.Combat.Kite;
using Unity.Mathematics;

namespace BovineLabs.Combat.Kite.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class KiteMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeKitePosition

        [Test]
        public void ComputeKitePosition_AgentEastOfTarget_CCWArcMovesNorth()
        {
            // Agent at (5,0), target at (0,0), bearing = 0 (atan2(0,5) = 0)
            // kiteAngle = 0 + PI/4 * 1 = PI/4
            // pos = (0,0) + (cos(PI/4)*range, sin(PI/4)*range)
            var pos = KiteMath.ComputeKitePosition(
                new float2(5f, 0f), new float2(0f, 0f), 5f, math.PI / 4f, 1f);

            Assert.That(pos.x, Is.EqualTo(5f * math.cos(math.PI / 4f)).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(5f * math.sin(math.PI / 4f)).Within(Epsilon));
        }

        [Test]
        public void ComputeKitePosition_ZeroArcAngle_StaysOnBearing()
        {
            var pos = KiteMath.ComputeKitePosition(
                new float2(5f, 0f), new float2(0f, 0f), 3f, 0f, 1f);

            // kiteAngle = bearing(0) + 0 = 0
            Assert.That(pos.x, Is.EqualTo(3f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeKitePosition_CWDirection_NegativeOffset()
        {
            var pos = KiteMath.ComputeKitePosition(
                new float2(5f, 0f), new float2(0f, 0f), 5f, math.PI / 4f, -1f);

            // kiteAngle = 0 + PI/4 * (-1) = -PI/4
            var expected = new float2(
                math.cos(-math.PI / 4f) * 5f,
                math.sin(-math.PI / 4f) * 5f);
            Assert.That(pos.x, Is.EqualTo(expected.x).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(expected.y).Within(Epsilon));
        }

        [Test]
        public void ComputeKitePosition_AgentOnTarget_UsesBearingZero()
        {
            var pos = KiteMath.ComputeKitePosition(
                new float2(0f, 0f), new float2(0f, 0f), 5f, 0f, 1f);

            // bearing = 0 (arbitrary), kiteAngle = 0
            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeKitePosition_NonZeroTarget_OffsetPositioned()
        {
            var pos = KiteMath.ComputeKitePosition(
                new float2(15f, 10f), new float2(10f, 10f), 5f, 0f, 1f);

            // Agent at (15,10), target at (10,10), toAgent = (5,0), bearing = atan2(0,5) = 0
            // kiteAngle = 0, pos = (10,10) + (cos(0)*5, sin(0)*5) = (15,10)
            Assert.That(pos.x, Is.EqualTo(15f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(10f).Within(Epsilon));
        }

        [Test]
        public void ComputeKitePosition_FullCircle_ArcAnglePI()
        {
            var pos = KiteMath.ComputeKitePosition(
                new float2(5f, 0f), new float2(0f, 0f), 5f, math.PI, 1f);

            // kiteAngle = 0 + PI = PI
            Assert.That(pos.x, Is.EqualTo(-5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(0f).Within(Epsilon));
        }

        #endregion

        #region ShouldKite

        [Test]
        public void ShouldKite_TooClose_ReturnsTrue()
        {
            // Agent at dist=2, optimalRange=5, tolerance=1, threshold=4
            // distSq=4 < threshold^2=16
            var result = KiteMath.ShouldKite(
                new float2(2f, 0f), new float2(0f, 0f), 5f, 1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldKite_AtOptimalRange_ReturnsFalse()
        {
            // dist=5, optimalRange=5, tolerance=1, threshold=4
            // distSq=25 > threshold^2=16 => false
            var result = KiteMath.ShouldKite(
                new float2(5f, 0f), new float2(0f, 0f), 5f, 1f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldKite_FarAway_ReturnsFalse()
        {
            var result = KiteMath.ShouldKite(
                new float2(100f, 0f), new float2(0f, 0f), 5f, 1f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldKite_OnTarget_ReturnsTrue()
        {
            var result = KiteMath.ShouldKite(
                new float2(0f, 0f), new float2(0f, 0f), 5f, 1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldKite_LargeTolerance_MakesThresholdNegative_ReturnsFalse()
        {
            // tolerance > optimalRange => threshold = optimalRange - tolerance < 0 => threshold > 0 is false
            var result = KiteMath.ShouldKite(
                new float2(0f, 0f), new float2(0f, 0f), 3f, 5f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldKite_ExactlyAtThreshold_ReturnsFalse()
        {
            // threshold = 5 - 1 = 4, dist = 4, distSq = 16
            // distSq < threshold^2 => 16 < 16 is false
            var result = KiteMath.ShouldKite(
                new float2(4f, 0f), new float2(0f, 0f), 5f, 1f);

            Assert.That(result, Is.False);
        }

        #endregion
    }
}
