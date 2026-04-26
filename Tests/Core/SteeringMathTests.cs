using NUnit.Framework;
using BovineLabs.Combat.Core;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class SteeringMathTests
    {
        const float Epsilon = 0.001f;

        #region Seek

        [Test]
        public void Seek_TargetAtOrigin_ReturnsDirectionToOrigin()
        {
            var force = SteeringMath.Seek(new float2(10f, 0f), float2.zero, 5f);

            Assert.That(math.length(force), Is.EqualTo(5f).Within(Epsilon));
            Assert.That(force.x, Is.EqualTo(-5f).Within(Epsilon)); // pointing -X toward origin
            Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void Seek_SamePosition_ReturnsZero()
        {
            var force = SteeringMath.Seek(float2.zero, float2.zero, 5f);
            Assert.That(force, Is.EqualTo(float2.zero));
        }

        [Test]
        public void Seek_DiagonalTarget_Normalized()
        {
            var force = SteeringMath.Seek(float2.zero, new float2(1f, 1f), 10f);

            var expected = math.normalize(new float2(1f, 1f)) * 10f;
            Assert.That(force.x, Is.EqualTo(expected.x).Within(Epsilon));
            Assert.That(force.y, Is.EqualTo(expected.y).Within(Epsilon));
        }

        [Test]
        public void Seek_ZeroMaxSpeed_ReturnsZero()
        {
            var force = SteeringMath.Seek(float2.zero, new float2(10f, 10f), 0f);
            // maxSpeed = 0, normalize * 0 = 0 (but normalize of zero is zero, which is fine)
            Assert.That(math.length(force), Is.EqualTo(0f).Within(Epsilon));
        }

        #endregion

        #region Flee

        [Test]
        public void Flee_ThreatAtOrigin_ReturnsDirectionAway()
        {
            var force = SteeringMath.Flee(new float2(1f, 0f), float2.zero, 5f);

            Assert.That(force.x, Is.EqualTo(5f).Within(Epsilon)); // pointing +X away from origin
            Assert.That(force.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void Flee_IsInverseOfSeek()
        {
            var pos = new float2(3f, 7f);
            var target = new float2(11f, 2f);
            var speed = 8f;

            var seek = SteeringMath.Seek(pos, target, speed);
            var flee = SteeringMath.Flee(pos, target, speed);

            // Flee(pos, target) == Seek(target, pos)
            var expected = SteeringMath.Seek(target, pos, speed);
            Assert.That(flee.x, Is.EqualTo(expected.x).Within(Epsilon));
            Assert.That(flee.y, Is.EqualTo(expected.y).Within(Epsilon));
        }

        #endregion

        #region Arrive

        [Test]
        public void Arrive_FarAway_FullSpeed()
        {
            var force = SteeringMath.Arrive(float2.zero, new float2(100f, 0f), 5f, 10f, 0.1f);

            Assert.That(math.length(force), Is.EqualTo(5f).Within(Epsilon));
        }

        [Test]
        public void Arrive_WithinSlowRadius_Decelerates()
        {
            var force = SteeringMath.Arrive(float2.zero, new float2(5f, 0f), 10f, 10f, 0.1f);

            // dist=5, slowRadius=10 => speed = 10 * (5/10) = 5
            Assert.That(math.length(force), Is.EqualTo(5f).Within(Epsilon));
        }

        [Test]
        public void Arrive_AtTarget_Stops()
        {
            var force = SteeringMath.Arrive(float2.zero, new float2(0.05f, 0f), 10f, 10f, 0.1f);

            Assert.That(force, Is.EqualTo(float2.zero));
        }

        [Test]
        public void Arrive_ExactlyAtTarget_Zero()
        {
            var force = SteeringMath.Arrive(new float2(5f, 5f), new float2(5f, 5f), 10f, 10f, 0.1f);

            Assert.That(force, Is.EqualTo(float2.zero));
        }

        [Test]
        public void Arrive_HalfSlowRadius_HalfSpeed()
        {
            var force = SteeringMath.Arrive(float2.zero, new float2(5f, 0f), 10f, 10f, 0.01f);

            // dist=5, slowRadius=10 => desiredSpeed = 10 * 5/10 = 5
            Assert.That(math.length(force), Is.EqualTo(5f).Within(Epsilon));
        }

        #endregion

        #region Pursue

        [Test]
        public void Pursue_StationaryTarget_EqualsSeek()
        {
            var currentPos = new float2(0f, 0f);
            var targetPos = new float2(10f, 0f);
            var speed = 5f;

            var pursue = SteeringMath.Pursue(currentPos, float2.zero, targetPos, float2.zero, speed, 2f);
            var seek = SteeringMath.Seek(currentPos, targetPos, speed);

            Assert.That(pursue.x, Is.EqualTo(seek.x).Within(Epsilon));
            Assert.That(pursue.y, Is.EqualTo(seek.y).Within(Epsilon));
        }

        [Test]
        public void Pursue_MovingTarget_LeadsTarget()
        {
            var currentPos = new float2(0f, 0f);
            var currentVel = new float2(0f, 0f);
            var targetPos = new float2(10f, 0f);
            var targetVel = new float2(0f, 5f); // target moving in +Y
            var maxSpeed = 10f;

            var force = SteeringMath.Pursue(currentPos, currentVel, targetPos, targetVel, maxSpeed, 2f);

            // Should aim ahead of target (in +Y direction)
            Assert.That(force.y, Is.GreaterThan(0f));
        }

        [Test]
        public void Pursue_FastTarget_ClampsPrediction()
        {
            var currentPos = new float2(0f, 0f);
            var currentVel = new float2(0f, 0f);
            var targetPos = new float2(100f, 0f);
            var targetVel = new float2(0f, 1000f);
            var maxPrediction = 0.5f;

            var force = SteeringMath.Pursue(currentPos, currentVel, targetPos, targetVel, 10f, maxPrediction);

            // Should not aim too far ahead
            Assert.That(math.length(force), Is.EqualTo(10f).Within(Epsilon));
        }

        #endregion

        #region Evade

        [Test]
        public void Evade_IsInverseOfPursue()
        {
            var pos = new float2(0f, 0f);
            var vel = new float2(2f, 1f);
            var threatPos = new float2(10f, 5f);
            var threatVel = new float2(-1f, 3f);

            var pursue = SteeringMath.Pursue(pos, vel, threatPos, threatVel, 10f, 1f);
            var evade = SteeringMath.Evade(pos, vel, threatPos, threatVel, 10f, 1f);

            // Evade should point roughly opposite to pursue
            var dot = math.dot(math.normalize(pursue), math.normalize(evade));
            Assert.That(dot, Is.LessThan(0f));
        }

        #endregion

        #region LimitMagnitude

        [Test]
        public void LimitMagnitude_UnderLimit_Unchanged()
        {
            var v = new float2(3f, 4f); // length = 5
            var result = SteeringMath.LimitMagnitude(v, 10f);

            Assert.That(result.x, Is.EqualTo(3f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(4f).Within(Epsilon));
        }

        [Test]
        public void LimitMagnitude_OverLimit_Clamped()
        {
            var v = new float2(30f, 40f); // length = 50
            var result = SteeringMath.LimitMagnitude(v, 10f);

            Assert.That(math.length(result), Is.EqualTo(10f).Within(Epsilon));
        }

        #endregion

        #region Steer

        [Test]
        public void Steer_SubtractsVelocity()
        {
            var desired = new float2(10f, 0f);
            var current = new float2(5f, 0f);

            var result = SteeringMath.Steer(desired, current, 100f);

            Assert.That(result.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(0f).Within(Epsilon));
        }

        #endregion

        #region Angle Utilities

        [Test]
        [TestCase(0f, 0f, 0f)]
        [TestCase(0f, math.PI, math.PI)]
        [TestCase(math.PI, 0f, -math.PI)]
        [TestCase(0f, math.PI / 2f, math.PI / 2f)]
        public void DeltaAngle_Correct(float from, float to, float expected)
        {
            var result = SteeringMath.DeltaAngle(from, to);
            Assert.That(result, Is.EqualTo(expected).Within(Epsilon));
        }

        [Test]
        public void MoveAngleToward_WithinStep_ReachesTarget()
        {
            var result = SteeringMath.MoveAngleToward(0f, 0.5f, 1f);
            Assert.That(result, Is.EqualTo(0.5f).Within(Epsilon));
        }

        [Test]
        public void MoveAngleToward_ExceedsStep_ClampsToStep()
        {
            var result = SteeringMath.MoveAngleToward(0f, 2f, 1f);
            Assert.That(result, Is.EqualTo(1f).Within(Epsilon));
        }

        [Test]
        public void FacingAngleFromDirection_Forward_IsZero()
        {
            var dir = new float2(0f, 1f); // +Y = forward
            var angle = SteeringMath.FacingAngleFromDirection(dir);
            Assert.That(angle, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void DirectionFromFacingAngle_Zero_IsForward()
        {
            var dir = SteeringMath.DirectionFromFacingAngle(0f);
            Assert.That(dir.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(dir.y, Is.EqualTo(1f).Within(Epsilon));
        }

        #endregion
    }
}
