using NUnit.Framework;
using BovineLabs.Combat.Charge;
using Unity.Mathematics;

namespace BovineLabs.Combat.Charge.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class ChargeMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeChargeDirection

        [Test]
        public void ComputeChargeDirection_TargetToRight_ReturnsNormalizedRight()
        {
            var dir = ChargeMath.ComputeChargeDirection(
                new float2(0f, 0f), new float2(10f, 0f));

            Assert.That(dir.x, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeChargeDirection_SamePosition_ReturnsZero()
        {
            var dir = ChargeMath.ComputeChargeDirection(
                new float2(5f, 5f), new float2(5f, 5f));

            Assert.That(dir, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeChargeDirection_VeryClose_ReturnsZero()
        {
            var dir = ChargeMath.ComputeChargeDirection(
                new float2(0f, 0f), new float2(0.00005f, 0f));

            Assert.That(dir, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeChargeDirection_Diagonal_Normalized()
        {
            var dir = ChargeMath.ComputeChargeDirection(
                new float2(0f, 0f), new float2(3f, 4f));

            Assert.That(math.length(dir), Is.EqualTo(1f).Within(Epsilon));
            Assert.That(dir.x, Is.EqualTo(0.6f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(0.8f).Within(Epsilon));
        }

        [Test]
        public void ComputeChargeDirection_Backward_ReturnsBackward()
        {
            var dir = ChargeMath.ComputeChargeDirection(
                new float2(10f, 0f), new float2(0f, 0f));

            Assert.That(dir.x, Is.EqualTo(-1f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(0f).Within(Epsilon));
        }

        #endregion

        #region IsChargeValid

        [Test]
        public void IsChargeValid_BeyondMinDistance_ReturnsTrue()
        {
            var result = ChargeMath.IsChargeValid(
                new float2(0f, 0f), new float2(15f, 0f), 10f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsChargeValid_AtMinDistance_ReturnsTrue()
        {
            var result = ChargeMath.IsChargeValid(
                new float2(0f, 0f), new float2(10f, 0f), 10f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsChargeValid_BelowMinDistance_ReturnsFalse()
        {
            var result = ChargeMath.IsChargeValid(
                new float2(0f, 0f), new float2(5f, 0f), 10f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsChargeValid_SamePosition_ReturnsFalse()
        {
            var result = ChargeMath.IsChargeValid(
                new float2(3f, 3f), new float2(3f, 3f), 1f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsChargeValid_ZeroMinDistance_ReturnsTrue()
        {
            var result = ChargeMath.IsChargeValid(
                new float2(0f, 0f), new float2(0f, 0f), 0f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsChargeValid_DiagonalDistance()
        {
            var result = ChargeMath.IsChargeValid(
                new float2(0f, 0f), new float2(6f, 8f), 9f);

            // dist = 10, min = 9 => valid
            Assert.That(result, Is.True);
        }

        #endregion

        #region ComputeChargeForce

        [Test]
        public void ComputeChargeForce_BasicCase_ReturnsScaledDirection()
        {
            var force = ChargeMath.ComputeChargeForce(
                new float2(0f, 0f), new float2(10f, 0f), 5f, 2f);

            // direction = (1,0), chargeSpeed = 5*2 = 10
            Assert.That(force.x, Is.EqualTo(10f).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeChargeForce_AtTarget_ReturnsZero()
        {
            var force = ChargeMath.ComputeChargeForce(
                new float2(5f, 5f), new float2(5f, 5f), 5f, 2f);

            Assert.That(force, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeChargeForce_ZeroSpeed_ReturnsZero()
        {
            var force = ChargeMath.ComputeChargeForce(
                new float2(0f, 0f), new float2(10f, 0f), 0f, 2f);

            Assert.That(force, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeChargeForce_ZeroMultiplier_ReturnsZero()
        {
            var force = ChargeMath.ComputeChargeForce(
                new float2(0f, 0f), new float2(10f, 0f), 5f, 0f);

            Assert.That(force, Is.EqualTo(float2.zero));
        }

        [Test]
        public void ComputeChargeForce_DiagonalTarget()
        {
            var force = ChargeMath.ComputeChargeForce(
                new float2(0f, 0f), new float2(3f, 4f), 2f, 1f);

            // direction normalized = (0.6, 0.8), speed = 2*1 = 2
            Assert.That(force.x, Is.EqualTo(1.2f).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(1.6f).Within(Epsilon));
        }

        #endregion
    }
}
