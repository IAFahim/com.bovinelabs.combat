using NUnit.Framework;
using BovineLabs.Combat.Ambush;
using Unity.Mathematics;

namespace BovineLabs.Combat.Ambush.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class AmbushMathTests
    {
        const float Epsilon = 0.001f;

        #region IsEnemyInTrigger

        [Test]
        public void IsEnemyInTrigger_InsideRadius_ReturnsTrue()
        {
            // Enemy at (3, 0), ambush at origin, radius 5
            var result = AmbushMath.IsEnemyInTrigger(
                new float2(3f, 0f),
                new float2(0f, 0f),
                5f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEnemyInTrigger_OutsideRadius_ReturnsFalse()
        {
            // Enemy at (6, 0), ambush at origin, radius 5
            var result = AmbushMath.IsEnemyInTrigger(
                new float2(6f, 0f),
                new float2(0f, 0f),
                5f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsEnemyInTrigger_AtBoundary_ReturnsTrue()
        {
            // Enemy exactly at radius = 5
            var result = AmbushMath.IsEnemyInTrigger(
                new float2(5f, 0f),
                new float2(0f, 0f),
                5f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEnemyInTrigger_SamePosition_ReturnsTrue()
        {
            var result = AmbushMath.IsEnemyInTrigger(
                new float2(3f, 4f),
                new float2(3f, 4f),
                1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEnemyInTrigger_DiagonalDistance()
        {
            // Distance = sqrt(9 + 16) = 5, radius 5
            var result = AmbushMath.IsEnemyInTrigger(
                new float2(3f, 4f),
                new float2(0f, 0f),
                5f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsEnemyInTrigger_JustOutsideDiagonal()
        {
            // Distance = sqrt(9 + 16) = 5, radius 4.9
            var result = AmbushMath.IsEnemyInTrigger(
                new float2(3f, 4f),
                new float2(0f, 0f),
                4.9f);

            Assert.That(result, Is.False);
        }

        #endregion

        #region ComputeAmbushForce

        [Test]
        public void ComputeAmbushForce_Hiding_SeeksHidePosition()
        {
            // Agent at origin, hide at (10, 0), speed 5
            var force = AmbushMath.ComputeAmbushForce(
                new float2(0f, 0f),
                new float2(10f, 0f),
                5f,
                AmbushPhase.Hiding,
                float2.zero);

            // Seek toward (10,0) => normalize = (1,0) * 5 = (5, 0)
            Assert.That(force.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeAmbushForce_Springing_SeeksSpringTarget()
        {
            // Agent at origin, spring target at (0, 10), speed 4
            var force = AmbushMath.ComputeAmbushForce(
                new float2(0f, 0f),
                new float2(100f, 0f), // hide pos ignored during springing
                4f,
                AmbushPhase.Springing,
                new float2(0f, 10f));

            // Seek toward (0,10) at speed * 1.5 = 4 * 1.5 = 6
            // normalize(0,10) = (0,1) * 6 = (0, 6)
            Assert.That(force.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(6f).Within(Epsilon));
        }

        [Test]
        public void ComputeAmbushForce_Waiting_ZeroForce()
        {
            var force = AmbushMath.ComputeAmbushForce(
                new float2(0f, 0f),
                new float2(10f, 0f),
                5f,
                AmbushPhase.Waiting,
                float2.zero);

            Assert.That(force.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeAmbushForce_Springing_UsesFasterSpeed()
        {
            // Agent at origin, spring target at (10, 0), speed 10
            var force = AmbushMath.ComputeAmbushForce(
                new float2(0f, 0f),
                float2.zero,
                10f,
                AmbushPhase.Springing,
                new float2(10f, 0f));

            // Spring speed = 10 * 1.5 = 15
            Assert.That(force.x, Is.EqualTo(15f).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeAmbushForce_Hiding_AtHidePos_ReturnsZero()
        {
            // Agent already at hide position
            var force = AmbushMath.ComputeAmbushForce(
                new float2(5f, 5f),
                new float2(5f, 5f),
                5f,
                AmbushPhase.Hiding,
                float2.zero);

            Assert.That(force.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeAmbushForce_Hiding_Diagonal()
        {
            // Agent at (0,0), hide at (3,4), speed 10
            var force = AmbushMath.ComputeAmbushForce(
                new float2(0f, 0f),
                new float2(3f, 4f),
                10f,
                AmbushPhase.Hiding,
                float2.zero);

            // normalize(3,4) = (0.6, 0.8) * 10 = (6, 8)
            Assert.That(force.x, Is.EqualTo(6f).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(8f).Within(Epsilon));
        }

        #endregion

        #region HasReachedHidePosition

        [Test]
        public void HasReachedHidePosition_AtPosition_ReturnsTrue()
        {
            var result = AmbushMath.HasReachedHidePosition(
                new float2(5f, 5f),
                new float2(5f, 5f),
                1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasReachedHidePosition_NearPosition_ReturnsTrue()
        {
            // Agent 0.5 away, threshold 1.0
            var result = AmbushMath.HasReachedHidePosition(
                new float2(5.5f, 5f),
                new float2(5f, 5f),
                1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasReachedHidePosition_FarFromPosition_ReturnsFalse()
        {
            // Agent 2.0 away, threshold 1.0
            var result = AmbushMath.HasReachedHidePosition(
                new float2(7f, 5f),
                new float2(5f, 5f),
                1f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasReachedHidePosition_ExactlyAtThreshold_ReturnsTrue()
        {
            var result = AmbushMath.HasReachedHidePosition(
                new float2(6f, 5f),
                new float2(5f, 5f),
                1f);

            // Distance = 1, threshold = 1, should be true (<=)
            Assert.That(result, Is.True);
        }

        [Test]
        public void HasReachedHidePosition_JustBeyondThreshold_ReturnsFalse()
        {
            var result = AmbushMath.HasReachedHidePosition(
                new float2(6.01f, 5f),
                new float2(5f, 5f),
                1f);

            Assert.That(result, Is.False);
        }

        #endregion
    }
}
