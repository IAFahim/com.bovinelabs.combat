using NUnit.Framework;
using BovineLabs.Combat.CombatAI;
using Unity.Mathematics;

namespace BovineLabs.Combat.CombatAI.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class CombatAIMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeThreatScore

        [Test]
        public void ComputeThreatScore_BasicValues()
        {
            // enemyHealth=100, distance=10, enemySpeed=5, agentHealth=100
            // powerThreat = 100 * 5 * 0.1 = 50
            // proximityThreat = 1 / 10 = 0.1
            // vulnerability = 1 (agentHealth >= 100 => no modifier)
            // score = (50 + 0.1 * 10) * 1 = 51
            var score = CombatAIMath.ComputeThreatScore(100f, 10f, 5f, 100f);

            Assert.That(score, Is.EqualTo(51f).Within(Epsilon));
        }

        [Test]
        public void ComputeThreatScore_CloseEnemy_HigherScore()
        {
            var farScore = CombatAIMath.ComputeThreatScore(100f, 20f, 5f, 100f);
            var closeScore = CombatAIMath.ComputeThreatScore(100f, 5f, 5f, 100f);

            Assert.That(closeScore, Is.GreaterThan(farScore));
        }

        [Test]
        public void ComputeThreatScore_FastEnemy_HigherScore()
        {
            var slowScore = CombatAIMath.ComputeThreatScore(100f, 10f, 2f, 100f);
            var fastScore = CombatAIMath.ComputeThreatScore(100f, 10f, 10f, 100f);

            Assert.That(fastScore, Is.GreaterThan(slowScore));
        }

        [Test]
        public void ComputeThreatScore_LowAgentHealth_AmplifiesThreat()
        {
            var fullHealth = CombatAIMath.ComputeThreatScore(100f, 10f, 5f, 100f);
            var lowHealth = CombatAIMath.ComputeThreatScore(100f, 10f, 5f, 25f);

            // vulnerability at 25 HP: 1 + (1 - 25/100) = 1 + 0.75 = 1.75
            Assert.That(lowHealth, Is.GreaterThan(fullHealth));
        }

        [Test]
        public void ComputeThreatScore_ZeroAgentHealth_NoSpecialModifier()
        {
            // agentHealth <= 0 => vulnerability stays at 1
            var score = CombatAIMath.ComputeThreatScore(100f, 10f, 5f, 0f);

            // Same as full health case: vulnerability = 1
            Assert.That(score, Is.EqualTo(51f).Within(Epsilon));
        }

        [Test]
        public void ComputeThreatScore_VeryCloseDistance_Clamped()
        {
            // Distance < 1 => clamped to 1
            var score0 = CombatAIMath.ComputeThreatScore(100f, 0.5f, 5f, 100f);
            var score1 = CombatAIMath.ComputeThreatScore(100f, 1f, 5f, 100f);

            // Both should produce the same proximity threat since clamped to 1
            Assert.That(score0, Is.EqualTo(score1).Within(Epsilon));
        }

        [Test]
        public void ComputeThreatScore_50PercentHealth_DoublesVulnerability()
        {
            // agentHealth = 50 => vulnerability = 1 + (1 - 50/100) = 1.5
            var score = CombatAIMath.ComputeThreatScore(100f, 10f, 5f, 50f);

            // (50 + 1) * 1.5 = 76.5
            Assert.That(score, Is.EqualTo(76.5f).Within(Epsilon));
        }

        #endregion

        #region ShouldEngage

        [Test]
        public void ShouldEngage_WithinRangeAndAlive_ReturnsTrue()
        {
            var result = CombatAIMath.ShouldEngage(50f, 8f, 10f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldEngage_OutsideRange_ReturnsFalse()
        {
            var result = CombatAIMath.ShouldEngage(50f, 12f, 10f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldEngage_DeadAgent_ReturnsFalse()
        {
            var result = CombatAIMath.ShouldEngage(0f, 5f, 10f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldEngage_ExactRange_ReturnsTrue()
        {
            var result = CombatAIMath.ShouldEngage(50f, 10f, 10f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldEngage_NegativeHealth_ReturnsFalse()
        {
            var result = CombatAIMath.ShouldEngage(-10f, 5f, 10f);
            Assert.That(result, Is.False);
        }

        #endregion

        #region ShouldFlee

        [Test]
        public void ShouldFlee_BelowThreshold_ReturnsTrue()
        {
            // health=20, maxHealth=100, threshold=0.3 => ratio=0.2 <= 0.3
            var result = CombatAIMath.ShouldFlee(20f, 100f, 0.3f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldFlee_AboveThreshold_ReturnsFalse()
        {
            // health=50, maxHealth=100, threshold=0.3 => ratio=0.5 > 0.3
            var result = CombatAIMath.ShouldFlee(50f, 100f, 0.3f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldFlee_ExactThreshold_ReturnsTrue()
        {
            // health=30, maxHealth=100, threshold=0.3 => ratio=0.3 <= 0.3
            var result = CombatAIMath.ShouldFlee(30f, 100f, 0.3f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldFlee_ZeroMaxHealth_ReturnsTrue()
        {
            var result = CombatAIMath.ShouldFlee(10f, 0f, 0.5f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldFlee_FullHealth_ReturnsFalse()
        {
            var result = CombatAIMath.ShouldFlee(100f, 100f, 0.3f);
            Assert.That(result, Is.False);
        }

        #endregion

        #region ShouldDisengage

        [Test]
        public void ShouldDisengage_EnemyFar_ReturnsTrue()
        {
            var result = CombatAIMath.ShouldDisengage(15f, 10f);
            Assert.That(result, Is.True);
        }

        [Test]
        public void ShouldDisengage_EnemyClose_ReturnsFalse()
        {
            var result = CombatAIMath.ShouldDisengage(5f, 10f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldDisengage_ExactRange_ReturnsFalse()
        {
            // distance == disengageRange => not strictly greater
            var result = CombatAIMath.ShouldDisengage(10f, 10f);
            Assert.That(result, Is.False);
        }

        [Test]
        public void ShouldDisengage_ZeroDistance_ReturnsFalse()
        {
            var result = CombatAIMath.ShouldDisengage(0f, 10f);
            Assert.That(result, Is.False);
        }

        #endregion
    }
}
