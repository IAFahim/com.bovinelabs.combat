using NUnit.Framework;
using BovineLabs.Combat.Core;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class LineOfSightMathTests
    {
        const float Epsilon = 0.001f;

        [Test]
        public void VisionCone_TargetInFront_ReturnsTrue()
        {
            var result = LineOfSightMath.IsInVisionCone(
                float2.zero, new float2(0f, 1f),
                new float2(0f, 5f),
                math.PI / 4f, 10f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void VisionCone_TargetBehind_ReturnsFalse()
        {
            var result = LineOfSightMath.IsInVisionCone(
                float2.zero, new float2(0f, 1f),
                new float2(0f, -5f),
                math.PI / 4f, 10f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void VisionCone_TargetOutOfRange_ReturnsFalse()
        {
            var result = LineOfSightMath.IsInVisionCone(
                float2.zero, new float2(0f, 1f),
                new float2(0f, 15f),
                math.PI / 4f, 10f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void VisionCone_TargetAtEdgeAngle_ReturnsTrue()
        {
            // Target at 44 degrees, cone is 45 degrees
            var target = new float2(math.sin(math.PI / 4f - 0.01f), math.cos(math.PI / 4f - 0.01f)) * 5f;
            var result = LineOfSightMath.IsInVisionCone(
                float2.zero, new float2(0f, 1f),
                target,
                math.PI / 4f, 10f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void RayIntersectsCircle_DirectHit_ReturnsTrue()
        {
            var result = LineOfSightMath.RayIntersectsCircle(
                float2.zero, new float2(1f, 0f),
                new float2(5f, 0f), 1f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void RayIntersectsCircle_Miss_ReturnsFalse()
        {
            var result = LineOfSightMath.RayIntersectsCircle(
                float2.zero, new float2(1f, 0f),
                new float2(5f, 5f), 1f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void RayIntersectsCircle_BehindRay_ReturnsFalse()
        {
            var result = LineOfSightMath.RayIntersectsCircle(
                new float2(10f, 0f), new float2(1f, 0f),
                new float2(5f, 0f), 1f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void RayCircleDistance_DirectHit_ReturnsDistance()
        {
            var dist = LineOfSightMath.RayCircleDistance(
                float2.zero, new float2(1f, 0f),
                new float2(5f, 0f), 1f);
            Assert.That(dist, Is.EqualTo(4f).Within(Epsilon)); // 5 - 1 = 4
        }

        [Test]
        public void RayCircleDistance_Miss_ReturnsMinusOne()
        {
            var dist = LineOfSightMath.RayCircleDistance(
                float2.zero, new float2(1f, 0f),
                new float2(5f, 10f), 1f);
            Assert.That(dist, Is.EqualTo(-1f).Within(Epsilon));
        }

        [Test]
        public void VisionCone_180DegreeCone_SeesAlmostEverything()
        {
            // 180 degree cone (PI radians half-angle)
            var result = LineOfSightMath.IsInVisionCone(
                float2.zero, new float2(0f, 1f),
                new float2(-5f, 0.01f), // slightly to the left but still forward-ish
                math.PI, 10f);
            Assert.That(result, Is.True);
        }
    }
}
