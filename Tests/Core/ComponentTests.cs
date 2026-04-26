using NUnit.Framework;
using BovineLabs.Combat.Core;
using Unity.Mathematics;
using Unity.Entities;

namespace BovineLabs.Combat.Core.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class ComponentTests
    {
        [Test]
        public void SteeringForce_Zero_IsZeroLinear()
        {
            var force = SteeringForce.Zero;
            Assert.That(force.Linear, Is.EqualTo(float2.zero));
            Assert.That(force.Priority, Is.EqualTo(0f));
            Assert.That(force.Weight, Is.EqualTo(0f));
            Assert.That(force.BehaviorType, Is.EqualTo(SteeringBehaviorType.None));
        }

        [Test]
        public void SteeringForce_IsZero_DetectsZeroForce()
        {
            var force = SteeringForce.Zero;
            Assert.That(force.IsZero, Is.True);
        }

        [Test]
        public void SteeringForce_IsZero_DetectsNonZeroForce()
        {
            var force = new SteeringForce { Linear = new float2(1f, 0f) };
            Assert.That(force.IsZero, Is.False);
        }

        [Test]
        public void MovementStats_Default_HasReasonableValues()
        {
            var stats = MovementStats.Default;
            Assert.That(stats.MaxSpeed, Is.EqualTo(5f));
            Assert.That(stats.MaxAcceleration, Is.EqualTo(20f));
            Assert.That(stats.Radius, Is.EqualTo(0.5f));
            Assert.That(stats.ArrivalThreshold, Is.EqualTo(0.1f));
            Assert.That(stats.Velocity, Is.EqualTo(float2.zero));
        }

        [Test]
        public void TeamId_EnemyDetection()
        {
            var team1 = new TeamId { Value = 1 };
            var team2 = new TeamId { Value = 2 };
            var neutral = new TeamId { Value = 0 };

            Assert.That(team1.IsEnemyTo(team2), Is.True);
            Assert.That(team2.IsEnemyTo(team1), Is.True);
            Assert.That(team1.IsEnemyTo(team1), Is.False);
            Assert.That(team1.IsEnemyTo(neutral), Is.False);
            Assert.That(neutral.IsEnemyTo(team1), Is.False);
        }

        [Test]
        public void TeamId_AllyDetection()
        {
            var team1a = new TeamId { Value = 1 };
            var team1b = new TeamId { Value = 1 };
            var team2 = new TeamId { Value = 2 };

            Assert.That(team1a.IsAllyTo(team1b), Is.True);
            Assert.That(team1a.IsAllyTo(team2), Is.False);
        }

        [Test]
        public void TeamId_ImplicitConversions()
        {
            TeamId id = 5;
            Assert.That(id.Value, Is.EqualTo(5));
            int val = id;
            Assert.That(val, Is.EqualTo(5));
        }

        [Test]
        public void CombatTarget_None_HasNoTarget()
        {
            var target = CombatTarget.None;
            Assert.That(target.HasTarget, Is.False);
            Assert.That(target.Entity, Is.EqualTo(Entity.Null));
        }

        [Test]
        public void CombatHealth_Ratio_CalculatesCorrectly()
        {
            var health = new CombatHealth { Current = 75f, Max = 100f };
            Assert.That(health.Ratio, Is.EqualTo(0.75f).Within(0.001f));
            Assert.That(health.IsAlive, Is.True);
            Assert.That(health.IsDead, Is.False);
        }

        [Test]
        public void CombatHealth_ZeroMax_RatioIsZero()
        {
            var health = new CombatHealth { Current = 50f, Max = 0f };
            Assert.That(health.Ratio, Is.EqualTo(0f));
        }

        [Test]
        public void ThreatScore_ThresholdChecks()
        {
            var threat = new ThreatScore
            {
                Value = 0.7f,
                FleeThreshold = 0.8f,
                FightThreshold = 0.5f
            };

            Assert.That(threat.ShouldFlee, Is.False); // 0.7 < 0.8
            Assert.That(threat.ShouldFight, Is.False); // 0.7 > 0.5

            threat.Value = 0.9f;
            Assert.That(threat.ShouldFlee, Is.True);

            threat.Value = 0.3f;
            Assert.That(threat.ShouldFight, Is.True);
        }
    }
}
