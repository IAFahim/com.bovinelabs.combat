using NUnit.Framework;
using BovineLabs.Combat.Core;
using Unity.Mathematics;

namespace BovineLabs.Combat.Steering.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class WanderMathTests
    {
        const float Epsilon = 0.01f;

        [Test]
        public void Wander_ProducesNonZeroForce()
        {
            var rng = new Unity.Mathematics.Random(12345);
            var force = SteeringMath.Wander(0f, 2f, 3f, 1f, ref rng, out var newAngle);

            Assert.That(math.length(force), Is.GreaterThan(0f));
        }

        [Test]
        public void Wander_UpdatesAngle()
        {
            var rng = new Unity.Mathematics.Random(42);
            var initialAngle = 0f;

            SteeringMath.Wander(initialAngle, 2f, 3f, 1f, ref rng, out var newAngle);

            // Angle should have changed (jitter > 0)
            Assert.That(newAngle, Is.Not.EqualTo(initialAngle).Within(Epsilon));
        }

        [Test]
        public void Wander_ZeroJitter_NoDisplacement()
        {
            var rng = new Unity.Mathematics.Random(99);
            var force = SteeringMath.Wander(0f, 2f, 3f, 0f, ref rng, out var newAngle);

            // With zero jitter, displacement stays on the circle edge at facing angle
            Assert.That(math.length(force), Is.EqualTo(2f).Within(Epsilon));
        }

        [Test]
        public void Wander_DifferentSeeds_DifferentResults()
        {
            var rng1 = new Unity.Mathematics.Random(111);
            var rng2 = new Unity.Mathematics.Random(222);

            var f1 = SteeringMath.Wander(0f, 2f, 3f, 1f, ref rng1, out _);
            var f2 = SteeringMath.Wander(0f, 2f, 3f, 1f, ref rng2, out _);

            // Different seeds should produce different results
            Assert.That(math.length(f1 - f2), Is.GreaterThan(0.01f));
        }
    }
}
