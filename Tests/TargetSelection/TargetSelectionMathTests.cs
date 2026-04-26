using NUnit.Framework;
using BovineLabs.Combat.TargetSelection;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.TargetSelection.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class TargetSelectionMathTests
    {
        const float Epsilon = 0.001f;

        #region SelectNearest

        [Test]
        public void SelectNearest_SingleCandidate_ReturnsIndex0()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            try
            {
                positions[0] = new float2(3f, 0f);
                var idx = TargetSelectionMath.SelectNearest(
                    new float2(0f, 0f), positions, 10f, 1);

                Assert.That(idx, Is.EqualTo(0));
            }
            finally
            {
                positions.Dispose();
            }
        }

        [Test]
        public void SelectNearest_MultipleCandidates_ReturnsNearest()
        {
            var positions = new NativeArray<float2>(3, Allocator.Temp);
            try
            {
                positions[0] = new float2(10f, 0f);
                positions[1] = new float2(2f, 0f);  // nearest
                positions[2] = new float2(5f, 0f);

                var idx = TargetSelectionMath.SelectNearest(
                    new float2(0f, 0f), positions, 20f, 3);

                Assert.That(idx, Is.EqualTo(1));
            }
            finally
            {
                positions.Dispose();
            }
        }

        [Test]
        public void SelectNearest_AllBeyondRange_ReturnsMinusOne()
        {
            var positions = new NativeArray<float2>(2, Allocator.Temp);
            try
            {
                positions[0] = new float2(20f, 0f);
                positions[1] = new float2(30f, 0f);

                var idx = TargetSelectionMath.SelectNearest(
                    new float2(0f, 0f), positions, 10f, 2);

                Assert.That(idx, Is.EqualTo(-1));
            }
            finally
            {
                positions.Dispose();
            }
        }

        [Test]
        public void SelectNearest_ZeroCount_ReturnsMinusOne()
        {
            var positions = new NativeArray<float2>(0, Allocator.Temp);
            try
            {
                var idx = TargetSelectionMath.SelectNearest(
                    new float2(0f, 0f), positions, 10f, 0);

                Assert.That(idx, Is.EqualTo(-1));
            }
            finally
            {
                positions.Dispose();
            }
        }

        [Test]
        public void SelectNearest_AtMaxRange_IncludesCandidate()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            try
            {
                positions[0] = new float2(10f, 0f);
                var idx = TargetSelectionMath.SelectNearest(
                    new float2(0f, 0f), positions, 10f, 1);

                Assert.That(idx, Is.EqualTo(0));
            }
            finally
            {
                positions.Dispose();
            }
        }

        #endregion

        #region SelectWeakest

        [Test]
        public void SelectWeakest_ReturnsLowestHP()
        {
            var positions = new NativeArray<float2>(3, Allocator.Temp);
            var healths = new NativeArray<float>(3, Allocator.Temp);
            try
            {
                positions[0] = new float2(1f, 0f);
                positions[1] = new float2(2f, 0f);
                positions[2] = new float2(3f, 0f);
                healths[0] = 100f;
                healths[1] = 50f;   // weakest
                healths[2] = 75f;

                var idx = TargetSelectionMath.SelectWeakest(
                    new float2(0f, 0f), positions, healths, 10f, 3);

                Assert.That(idx, Is.EqualTo(1));
            }
            finally
            {
                positions.Dispose();
                healths.Dispose();
            }
        }

        [Test]
        public void SelectWeakest_AllOutOfRange_ReturnsMinusOne()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var healths = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                positions[0] = new float2(100f, 0f);
                healths[0] = 10f;

                var idx = TargetSelectionMath.SelectWeakest(
                    new float2(0f, 0f), positions, healths, 5f, 1);

                Assert.That(idx, Is.EqualTo(-1));
            }
            finally
            {
                positions.Dispose();
                healths.Dispose();
            }
        }

        [Test]
        public void SelectWeakest_ZeroCount_ReturnsMinusOne()
        {
            var positions = new NativeArray<float2>(0, Allocator.Temp);
            var healths = new NativeArray<float>(0, Allocator.Temp);
            try
            {
                var idx = TargetSelectionMath.SelectWeakest(
                    new float2(0f, 0f), positions, healths, 10f, 0);

                Assert.That(idx, Is.EqualTo(-1));
            }
            finally
            {
                positions.Dispose();
                healths.Dispose();
            }
        }

        [Test]
        public void SelectWeakest_SameHP_ReturnsFirstFound()
        {
            var positions = new NativeArray<float2>(2, Allocator.Temp);
            var healths = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                positions[0] = new float2(1f, 0f);
                positions[1] = new float2(2f, 0f);
                healths[0] = 50f;
                healths[1] = 50f;

                var idx = TargetSelectionMath.SelectWeakest(
                    new float2(0f, 0f), positions, healths, 10f, 2);

                Assert.That(idx, Is.EqualTo(0));
            }
            finally
            {
                positions.Dispose();
                healths.Dispose();
            }
        }

        #endregion

        #region SelectMostThreatening

        [Test]
        public void SelectMostThreatening_ReturnsHighestThreat()
        {
            var positions = new NativeArray<float2>(3, Allocator.Temp);
            var threats = new NativeArray<float>(3, Allocator.Temp);
            try
            {
                positions[0] = new float2(1f, 0f);
                positions[1] = new float2(2f, 0f);
                positions[2] = new float2(3f, 0f);
                threats[0] = 5f;
                threats[1] = 15f;  // most threatening
                threats[2] = 10f;

                var idx = TargetSelectionMath.SelectMostThreatening(
                    new float2(0f, 0f), positions, threats, 10f, 3);

                Assert.That(idx, Is.EqualTo(1));
            }
            finally
            {
                positions.Dispose();
                threats.Dispose();
            }
        }

        [Test]
        public void SelectMostThreatening_AllOutOfRange_ReturnsMinusOne()
        {
            var positions = new NativeArray<float2>(1, Allocator.Temp);
            var threats = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                positions[0] = new float2(100f, 0f);
                threats[0] = 999f;

                var idx = TargetSelectionMath.SelectMostThreatening(
                    new float2(0f, 0f), positions, threats, 5f, 1);

                Assert.That(idx, Is.EqualTo(-1));
            }
            finally
            {
                positions.Dispose();
                threats.Dispose();
            }
        }

        [Test]
        public void SelectMostThreatening_ZeroCount_ReturnsMinusOne()
        {
            var positions = new NativeArray<float2>(0, Allocator.Temp);
            var threats = new NativeArray<float>(0, Allocator.Temp);
            try
            {
                var idx = TargetSelectionMath.SelectMostThreatening(
                    new float2(0f, 0f), positions, threats, 10f, 0);

                Assert.That(idx, Is.EqualTo(-1));
            }
            finally
            {
                positions.Dispose();
                threats.Dispose();
            }
        }

        [Test]
        public void SelectMostThreatening_NegativeThreatScores_ReturnsHighest()
        {
            var positions = new NativeArray<float2>(2, Allocator.Temp);
            var threats = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                positions[0] = new float2(1f, 0f);
                positions[1] = new float2(2f, 0f);
                threats[0] = -10f;
                threats[1] = -5f;   // least negative = highest

                var idx = TargetSelectionMath.SelectMostThreatening(
                    new float2(0f, 0f), positions, threats, 10f, 2);

                Assert.That(idx, Is.EqualTo(1));
            }
            finally
            {
                positions.Dispose();
                threats.Dispose();
            }
        }

        #endregion

        #region IsInCone

        [Test]
        public void IsInCone_TargetInFront_ReturnsTrue()
        {
            var result = TargetSelectionMath.IsInCone(
                new float2(0f, 0f), new float2(0f, 1f),
                new float2(0f, 5f), math.PI / 4f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsInCone_TargetBehind_ReturnsFalse()
        {
            var result = TargetSelectionMath.IsInCone(
                new float2(0f, 0f), new float2(0f, 1f),
                new float2(0f, -5f), math.PI / 4f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void IsInCone_Target90Degrees_HalfAngle45_ReturnsFalse()
        {
            var result = TargetSelectionMath.IsInCone(
                new float2(0f, 0f), new float2(0f, 1f),
                new float2(5f, 0f), math.PI / 4f);

            // angle = 90 deg = PI/2 > PI/4
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsInCone_TargetAtSamePosition_ReturnsTrue()
        {
            var result = TargetSelectionMath.IsInCone(
                new float2(5f, 5f), new float2(0f, 1f),
                new float2(5f, 5f), math.PI / 4f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsInCone_PICone_AcceptsAll()
        {
            var result = TargetSelectionMath.IsInCone(
                new float2(0f, 0f), new float2(0f, 1f),
                new float2(0f, -5f), math.PI);

            Assert.That(result, Is.True);
        }

        [Test]
        public void IsInCone_Target45Degrees_HalfAngle46Degrees_ReturnsTrue()
        {
            var result = TargetSelectionMath.IsInCone(
                new float2(0f, 0f), new float2(0f, 1f),
                new float2(1f, 1f), math.PI / 3f);

            // angle ≈ 45 deg, half-angle = 60 deg => true
            Assert.That(result, Is.True);
        }

        #endregion
    }
}
