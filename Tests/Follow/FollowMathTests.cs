using NUnit.Framework;
using BovineLabs.Combat.Follow;
using Unity.Mathematics;

namespace BovineLabs.Combat.Follow.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class FollowMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeFollowPosition

        [Test]
        public void ComputeFollowPosition_BehindLeaderAtDistance()
        {
            // Leader at origin, facing +Y, distance 5, no offset
            var pos = FollowMath.ComputeFollowPosition(
                new float2(0f, 0f),
                new float2(0f, 1f),
                5f,
                float2.zero);

            Assert.That(pos.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(-5f).Within(Epsilon));
        }

        [Test]
        public void ComputeFollowPosition_WithOffset()
        {
            // Leader at origin, facing +Y, distance 5, lateral offset (3, 0)
            var pos = FollowMath.ComputeFollowPosition(
                new float2(0f, 0f),
                new float2(0f, 1f),
                5f,
                new float2(3f, 0f));

            Assert.That(pos.x, Is.EqualTo(3f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(-5f).Within(Epsilon));
        }

        [Test]
        public void ComputeFollowPosition_ZeroDistance_AtLeader()
        {
            var pos = FollowMath.ComputeFollowPosition(
                new float2(5f, 5f),
                new float2(1f, 0f),
                0f,
                float2.zero);

            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(5f).Within(Epsilon));
        }

        [Test]
        public void ComputeFollowPosition_NonZeroOrigin_FacingX()
        {
            // Leader at (10, 10), facing +X, distance 3, no offset
            var pos = FollowMath.ComputeFollowPosition(
                new float2(10f, 10f),
                new float2(1f, 0f),
                3f,
                float2.zero);

            Assert.That(pos.x, Is.EqualTo(7f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(10f).Within(Epsilon));
        }

        [Test]
        public void ComputeFollowPosition_NegativeOffset()
        {
            // Leader at origin, facing +Y, distance 5, offset (-2, 0)
            var pos = FollowMath.ComputeFollowPosition(
                new float2(0f, 0f),
                new float2(0f, 1f),
                5f,
                new float2(-2f, 0f));

            Assert.That(pos.x, Is.EqualTo(-2f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(-5f).Within(Epsilon));
        }

        [Test]
        public void ComputeFollowPosition_DiagonalForward()
        {
            // Leader at origin, facing (1,1) normalized, distance sqrt(2)
            var forward = math.normalize(new float2(1f, 1f));
            var pos = FollowMath.ComputeFollowPosition(
                new float2(0f, 0f),
                forward,
                math.sqrt(2f),
                float2.zero);

            // backward = (-1/sqrt2, -1/sqrt2), * sqrt2 = (-1, -1)
            Assert.That(pos.x, Is.EqualTo(-1f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(-1f).Within(Epsilon));
        }

        #endregion

        #region ComputeChainPosition

        [Test]
        public void ComputeChainPosition_BehindEntityAtDistance()
        {
            // Ahead entity at origin, facing +Y, distance 4
            var pos = FollowMath.ComputeChainPosition(
                new float2(0f, 0f),
                new float2(0f, 1f),
                4f);

            Assert.That(pos.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(-4f).Within(Epsilon));
        }

        [Test]
        public void ComputeChainPosition_ZeroDistance_AtEntity()
        {
            var pos = FollowMath.ComputeChainPosition(
                new float2(5f, 5f),
                new float2(0f, 1f),
                0f);

            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(5f).Within(Epsilon));
        }

        [Test]
        public void ComputeChainPosition_FacingX()
        {
            var pos = FollowMath.ComputeChainPosition(
                new float2(10f, 10f),
                new float2(1f, 0f),
                3f);

            Assert.That(pos.x, Is.EqualTo(7f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(10f).Within(Epsilon));
        }

        [Test]
        public void ComputeChainPosition_DiagonalForward()
        {
            var forward = math.normalize(new float2(1f, 1f));
            var pos = FollowMath.ComputeChainPosition(
                new float2(0f, 0f),
                forward,
                math.sqrt(2f));

            Assert.That(pos.x, Is.EqualTo(-1f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(-1f).Within(Epsilon));
        }

        #endregion
    }
}
