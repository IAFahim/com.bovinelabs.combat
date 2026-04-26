using NUnit.Framework;
using BovineLabs.Combat.Retreat;
using Unity.Mathematics;

namespace BovineLabs.Combat.Retreat.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class RetreatMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeRetreatDirection

        [Test]
        public void ComputeRetreatDirection_WithinSafeDistance_ReturnsAwayForce()
        {
            var dir = RetreatMath.ComputeRetreatDirection(
                new float2(2f, 0f), new float2(0f, 0f), 5f, 3f);

            // away = (2,0), dist=2 < safeDistance=5, normalized*(1,0)*3
            Assert.That(dir.x, Is.EqualTo(3f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeRetreatDirection_AtSafeDistance_ReturnsZero()
        {
            var dir = RetreatMath.ComputeRetreatDirection(
                new float2(5f, 0f), new float2(0f, 0f), 5f, 3f);

            // dist = 5 >= safeDistance = 5 => zero
            Assert.That(dir, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeRetreatDirection_BeyondSafeDistance_ReturnsZero()
        {
            var dir = RetreatMath.ComputeRetreatDirection(
                new float2(10f, 0f), new float2(0f, 0f), 5f, 3f);

            Assert.That(dir, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeRetreatDirection_OnTopOfThreat_ArbitraryDirection()
        {
            var dir = RetreatMath.ComputeRetreatDirection(
                new float2(0f, 0f), new float2(0f, 0f), 5f, 3f);

            // dist < 0.0001 => return (0,1) * maxSpeed
            Assert.That(dir.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(3f).Within(Epsilon));
        }

        [Test]
        public void ComputeRetreatDirection_DiagonalAway()
        {
            var dir = RetreatMath.ComputeRetreatDirection(
                new float2(3f, 4f), new float2(0f, 0f), 10f, 5f);

            // away = (3,4), dist=5 < 10, normalize*(0.6,0.8)*5 = (3,4)
            Assert.That(dir.x, Is.EqualTo(3f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(4f).Within(Epsilon));
        }

        [Test]
        public void ComputeRetreatDirection_ZeroMaxSpeed_ReturnsZero()
        {
            var dir = RetreatMath.ComputeRetreatDirection(
                new float2(2f, 0f), new float2(0f, 0f), 5f, 0f);

            Assert.That(dir, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeRetreatDirection_ThreatToRight_MovesLeft()
        {
            var dir = RetreatMath.ComputeRetreatDirection(
                new float2(0f, 0f), new float2(5f, 0f), 10f, 3f);

            // away = (0,0) - (5,0) = (-5,0), normalized*(-1,0)*3
            Assert.That(dir.x, Is.EqualTo(-3f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(0f).Within(Epsilon));
        }

        #endregion
    }
}
