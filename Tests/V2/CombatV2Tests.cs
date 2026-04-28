using NUnit.Framework;
using Unity.Entities;
using Unity.Mathematics;
using BovineLabs.Combat.Core;
using BovineLabs.Combat.Systems;

namespace BovineLabs.Combat.Tests
{
    [TestFixture]
    [Category("CombatV2")]
    public class CombatV2Tests
    {
        const float Epsilon = 0.001f;

        // 1. CombatMotionData.None has default scales
        [Test]
        public void CombatMotionData_None_HasDefaultScales()
        {
            var data = CombatMotionData.None;
            Assert.AreEqual(1f, data.SpeedScale, Epsilon);
            Assert.AreEqual(1f, data.AccelerationScale, Epsilon);
            Assert.AreEqual(1f, data.BrakeScale, Epsilon);
            Assert.AreEqual(CombatMotionMode.None, data.Mode);
        }

        // 2. CombatMotionData.None has zero velocity/direction
        [Test]
        public void CombatMotionData_None_HasZeroVelocity()
        {
            var data = CombatMotionData.None;
            Assert.AreEqual(float3.zero, data.DesiredVelocity);
            Assert.AreEqual(float3.zero, data.DesiredDirection);
        }

        // 3. CombatMotionMixer.Lerp blends velocities at t=0.5
        [Test]
        public void CombatMotionMixer_Lerp_BlendsVelocities()
        {
            var a = new CombatMotionData { DesiredVelocity = new float3(0f, 0f, 0f) };
            var b = new CombatMotionData { DesiredVelocity = new float3(10f, 0f, 0f) };
            var mixer = new CombatMotionMixer();
            var result = mixer.Lerp(in a, in b, 0.5f);
            Assert.AreEqual(5f, result.DesiredVelocity.x, Epsilon);
            Assert.AreEqual(0f, result.DesiredVelocity.y, Epsilon);
            Assert.AreEqual(0f, result.DesiredVelocity.z, Epsilon);
        }

        // 4. CombatMotionMixer.Lerp picks mode by threshold (0.5)
        [Test]
        public void CombatMotionMixer_Lerp_PicksModeByThreshold()
        {
            var a = new CombatMotionData { Mode = CombatMotionMode.DesiredVelocity };
            var b = new CombatMotionData { Mode = CombatMotionMode.Stop };
            var mixer = new CombatMotionMixer();

            var lowT = mixer.Lerp(in a, in b, 0.3f);
            Assert.AreEqual(CombatMotionMode.DesiredVelocity, lowT.Mode, "t=0.3 should pick a.Mode");

            var highT = mixer.Lerp(in a, in b, 0.7f);
            Assert.AreEqual(CombatMotionMode.Stop, highT.Mode, "t=0.7 should pick b.Mode");
        }

        // 5. CombatMotionMixer.Add clamps by MaxContribution
        [Test]
        public void CombatMotionMixer_Add_ClampsByMaxContribution()
        {
            var a = new CombatMotionData
            {
                DesiredVelocity = new float3(3f, 0f, 0f),
                MaxContribution = 5f,
            };
            var b = new CombatMotionData
            {
                DesiredVelocity = new float3(4f, 0f, 0f),
                MaxContribution = 5f,
            };
            var mixer = new CombatMotionMixer();
            var result = mixer.Add(in a, in b);

            // Combined is 7 which exceeds maxContribution=5, so it should be clamped to 5
            Assert.AreEqual(5f, result.DesiredVelocity.x, Epsilon);
            Assert.AreEqual(5f, result.MaxContribution, Epsilon);
        }

        // 6. FacingData.None has default scales
        [Test]
        public void FacingData_None_HasDefaultScales()
        {
            var data = FacingData.None;
            Assert.AreEqual(1f, data.TurnSpeedScale, Epsilon);
            Assert.AreEqual(1f, data.AngularDampingScale, Epsilon);
            Assert.AreEqual(FacingMode.None, data.Mode);
        }

        // 7. ForcedMotionState.Inactive has zero time
        [Test]
        public void ForcedMotionState_Inactive_HasZeroTime()
        {
            var state = ForcedMotionState.Inactive;
            Assert.AreEqual(0f, state.RemainingTime, Epsilon);
        }

        // 8. CombatRelationship.IsHostileTo works
        [Test]
        public void CombatRelationship_IsHostileTo_Works()
        {
            var self = new CombatRelationship
            {
                FactionId = 0,
                HostileCategories = 1u << 2, // hostile to faction 2
            };
            var enemy = new CombatRelationship { FactionId = 2 };
            var ally = new CombatRelationship { FactionId = 1 };

            Assert.IsTrue(self.IsHostileTo(in enemy));
            Assert.IsFalse(self.IsHostileTo(in ally));
        }

        // 9. CombatRelationship.IsFriendlyTo works
        [Test]
        public void CombatRelationship_IsFriendlyTo_Works()
        {
            var self = new CombatRelationship
            {
                FactionId = 0,
                FriendlyCategories = 1u << 1, // friendly to faction 1
            };
            var ally = new CombatRelationship { FactionId = 1 };
            var neutral = new CombatRelationship { FactionId = 3 };

            Assert.IsTrue(self.IsFriendlyTo(in ally));
            Assert.IsFalse(self.IsFriendlyTo(in neutral));
        }

        // 10. SensedTarget default has null entity
        [Test]
        public void SensedTarget_Default_HasNullEntity()
        {
            var target = new SensedTarget();
            Assert.AreEqual(Entity.Null, target.Entity);
        }

        // 11. CombatDesire default has None type
        [Test]
        public void CombatDesire_Default_HasNoneType()
        {
            var desire = new CombatDesire();
            Assert.AreEqual(CombatDesireType.None, desire.Type);
        }

        // 12. CombatMotionFlags.IgnoreAvoidance is a valid flag bit
        [Test]
        public void CombatMotionFlags_IgnoreAvoidance_IsBit4()
        {
            Assert.AreNotEqual(CombatMotionFlags.None, CombatMotionFlags.IgnoreAvoidance);
            Assert.IsTrue((CombatMotionFlags.IgnoreAvoidance & CombatMotionFlags.IgnoreAvoidance) != 0);
            // Verify it's bit 2 (1 << 2 = 4)
            Assert.AreEqual(4, (int)CombatMotionFlags.IgnoreAvoidance);
        }
    }
}
