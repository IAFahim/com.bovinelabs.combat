using NUnit.Framework;
using BovineLabs.Combat.Guard;
using Unity.Mathematics;

namespace BovineLabs.Combat.Guard.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class GuardMathTests
    {
        const float Epsilon = 0.001f;

        #region ShouldEngage

        [Test]
        public void ShouldEngage_EnemyWithinRadius_ReturnsTrue()
        {
            var result = GuardMath.ShouldEngage(
                new float2(0f, 0f), new float2(3f, 0f), 5f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldEngage_EnemyAtExactRadius_ReturnsTrue()
        {
            var result = GuardMath.ShouldEngage(
                new float2(0f, 0f), new float2(5f, 0f), 5f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldEngage_EnemyBeyondRadius_ReturnsFalse()
        {
            var result = GuardMath.ShouldEngage(
                new float2(0f, 0f), new float2(6f, 0f), 5f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldEngage_SamePosition_ReturnsTrue()
        {
            var result = GuardMath.ShouldEngage(
                new float2(5f, 5f), new float2(5f, 5f), 5f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldEngage_ZeroRadius_ReturnsFalseForDistantEnemy()
        {
            var result = GuardMath.ShouldEngage(
                new float2(0f, 0f), new float2(1f, 0f), 0f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldEngage_ZeroRadius_SamePosition_ReturnsTrue()
        {
            var result = GuardMath.ShouldEngage(
                new float2(3f, 4f), new float2(3f, 4f), 0f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldEngage_DiagonalDistance()
        {
            // dist = sqrt(9+16) = 5, radius = 5 => true (exact boundary)
            var result = GuardMath.ShouldEngage(
                new float2(0f, 0f), new float2(3f, 4f), 5f);

            Assert.That(result, Is.True);
        }

        #endregion

        #region ShouldReturn

        [Test]
        public void ShouldReturn_BeyondReturnRadius_ReturnsTrue()
        {
            var result = GuardMath.ShouldReturn(
                new float2(11f, 0f), new float2(0f, 0f), 10f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldReturn_AtReturnRadius_ReturnsFalse()
        {
            var result = GuardMath.ShouldReturn(
                new float2(10f, 0f), new float2(0f, 0f), 10f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldReturn_WithinReturnRadius_ReturnsFalse()
        {
            var result = GuardMath.ShouldReturn(
                new float2(5f, 0f), new float2(0f, 0f), 10f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldReturn_AtPost_ReturnsFalse()
        {
            var result = GuardMath.ShouldReturn(
                new float2(3f, 3f), new float2(3f, 3f), 10f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldReturn_ZeroRadius_OnlyTrueWhenExactlyAtPost()
        {
            // dist > 0^2 => true for any non-zero distance
            var result = GuardMath.ShouldReturn(
                new float2(0.001f, 0f), new float2(0f, 0f), 0f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldReturn_DiagonalDistance()
        {
            // dist = sqrt(64+36) = 10, radius = 10 => false (not beyond)
            var result = GuardMath.ShouldReturn(
                new float2(8f, 6f), new float2(0f, 0f), 10f);

            Assert.That(result, Is.False);
        }

        #endregion
    }
}
