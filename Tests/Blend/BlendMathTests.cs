using NUnit.Framework;
using BovineLabs.Combat.Blend;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.Blend.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class BlendMathTests
    {
        const float Epsilon = 0.001f;

        #region WeightedBlend

        [Test]
        public void WeightedBlend_SingleForce_ReturnsWeightedForce()
        {
            var forces = new NativeArray<float2>(1, Allocator.Temp);
            var weights = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                forces[0] = new float2(10f, 0f);
                weights[0] = 0.5f;

                var result = BlendMath.WeightedBlend(forces, weights, 1);

                Assert.That(result.x, Is.EqualTo(5f).Within(Epsilon));
                Assert.That(result.y, Is.EqualTo(0f).Within(Epsilon));
            }
            finally
            {
                forces.Dispose();
                weights.Dispose();
            }
        }

        [Test]
        public void WeightedBlend_TwoForces_SumsWeighted()
        {
            var forces = new NativeArray<float2>(2, Allocator.Temp);
            var weights = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                forces[0] = new float2(10f, 0f);
                weights[0] = 1f;
                forces[1] = new float2(0f, 10f);
                weights[1] = 1f;

                var result = BlendMath.WeightedBlend(forces, weights, 2);

                Assert.That(result.x, Is.EqualTo(10f).Within(Epsilon));
                Assert.That(result.y, Is.EqualTo(10f).Within(Epsilon));
            }
            finally
            {
                forces.Dispose();
                weights.Dispose();
            }
        }

        [Test]
        public void WeightedBlend_ZeroWeights_ReturnsZero()
        {
            var forces = new NativeArray<float2>(2, Allocator.Temp);
            var weights = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                forces[0] = new float2(10f, 0f);
                weights[0] = 0f;
                forces[1] = new float2(0f, 10f);
                weights[1] = 0f;

                var result = BlendMath.WeightedBlend(forces, weights, 2);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                forces.Dispose();
                weights.Dispose();
            }
        }

        [Test]
        public void WeightedBlend_ZeroCount_ReturnsZero()
        {
            var forces = new NativeArray<float2>(0, Allocator.Temp);
            var weights = new NativeArray<float>(0, Allocator.Temp);
            try
            {
                var result = BlendMath.WeightedBlend(forces, weights, 0);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                forces.Dispose();
                weights.Dispose();
            }
        }

        [Test]
        public void WeightedBlend_OpposingForces_CancelsOut()
        {
            var forces = new NativeArray<float2>(2, Allocator.Temp);
            var weights = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                forces[0] = new float2(10f, 0f);
                weights[0] = 1f;
                forces[1] = new float2(-10f, 0f);
                weights[1] = 1f;

                var result = BlendMath.WeightedBlend(forces, weights, 2);

                Assert.That(result.x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result.y, Is.EqualTo(0f).Within(Epsilon));
            }
            finally
            {
                forces.Dispose();
                weights.Dispose();
            }
        }

        [Test]
        public void WeightedBlend_DifferentWeights_WeightedSum()
        {
            var forces = new NativeArray<float2>(2, Allocator.Temp);
            var weights = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                forces[0] = new float2(10f, 0f);
                weights[0] = 0.3f;
                forces[1] = new float2(0f, 20f);
                weights[1] = 0.7f;

                var result = BlendMath.WeightedBlend(forces, weights, 2);

                Assert.That(result.x, Is.EqualTo(3f).Within(Epsilon));
                Assert.That(result.y, Is.EqualTo(14f).Within(Epsilon));
            }
            finally
            {
                forces.Dispose();
                weights.Dispose();
            }
        }

        #endregion

        #region PrioritySelect

        [Test]
        public void PrioritySelect_SingleNonZeroForce_ReturnsThatForce()
        {
            var forces = new NativeArray<float2>(1, Allocator.Temp);
            var priorities = new NativeArray<float>(1, Allocator.Temp);
            try
            {
                forces[0] = new float2(5f, 3f);
                priorities[0] = 1f;

                var result = BlendMath.PrioritySelect(forces, priorities, 1);

                Assert.That(result.x, Is.EqualTo(5f).Within(Epsilon));
                Assert.That(result.y, Is.EqualTo(3f).Within(Epsilon));
            }
            finally
            {
                forces.Dispose();
                priorities.Dispose();
            }
        }

        [Test]
        public void PrioritySelect_AllZeroForces_ReturnsZero()
        {
            var forces = new NativeArray<float2>(2, Allocator.Temp);
            var priorities = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                forces[0] = float2.zero;
                priorities[0] = 10f;
                forces[1] = float2.zero;
                priorities[1] = 5f;

                var result = BlendMath.PrioritySelect(forces, priorities, 2);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                forces.Dispose();
                priorities.Dispose();
            }
        }

        [Test]
        public void PrioritySelect_HighestPriorityWins()
        {
            var forces = new NativeArray<float2>(3, Allocator.Temp);
            var priorities = new NativeArray<float>(3, Allocator.Temp);
            try
            {
                forces[0] = new float2(1f, 0f);
                priorities[0] = 1f;
                forces[1] = new float2(0f, 1f);
                priorities[1] = 5f;   // highest
                forces[2] = new float2(0f, 0f);
                priorities[2] = 10f;  // zero force, should be ignored

                var result = BlendMath.PrioritySelect(forces, priorities, 3);

                Assert.That(result.x, Is.EqualTo(0f).Within(Epsilon));
                Assert.That(result.y, Is.EqualTo(1f).Within(Epsilon));
            }
            finally
            {
                forces.Dispose();
                priorities.Dispose();
            }
        }

        [Test]
        public void PrioritySelect_TiedPriorities_AveragesForces()
        {
            var forces = new NativeArray<float2>(2, Allocator.Temp);
            var priorities = new NativeArray<float>(2, Allocator.Temp);
            try
            {
                forces[0] = new float2(10f, 0f);
                priorities[0] = 3f;
                forces[1] = new float2(0f, 10f);
                priorities[1] = 3f;

                var result = BlendMath.PrioritySelect(forces, priorities, 2);

                Assert.That(result.x, Is.EqualTo(5f).Within(Epsilon));
                Assert.That(result.y, Is.EqualTo(5f).Within(Epsilon));
            }
            finally
            {
                forces.Dispose();
                priorities.Dispose();
            }
        }

        [Test]
        public void PrioritySelect_ZeroCount_ReturnsZero()
        {
            var forces = new NativeArray<float2>(0, Allocator.Temp);
            var priorities = new NativeArray<float>(0, Allocator.Temp);
            try
            {
                var result = BlendMath.PrioritySelect(forces, priorities, 0);

                Assert.That(result, Is.EqualTo(float2.zero));
            }
            finally
            {
                forces.Dispose();
                priorities.Dispose();
            }
        }

        #endregion

        #region TruncateToMaxSpeed

        [Test]
        public void TruncateToMaxSpeed_UnderLimit_Unchanged()
        {
            var force = new float2(3f, 4f); // length = 5
            var result = BlendMath.TruncateToMaxSpeed(force, 10f);

            Assert.That(result.x, Is.EqualTo(3f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(4f).Within(Epsilon));
        }

        [Test]
        public void TruncateToMaxSpeed_OverLimit_Truncates()
        {
            var force = new float2(30f, 40f); // length = 50
            var result = BlendMath.TruncateToMaxSpeed(force, 10f);

            Assert.That(math.length(result), Is.EqualTo(10f).Within(Epsilon));
            // Direction preserved
            Assert.That(result.x, Is.EqualTo(6f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(8f).Within(Epsilon));
        }

        [Test]
        public void TruncateToMaxSpeed_ExactlyAtLimit_Unchanged()
        {
            var force = new float2(10f, 0f);
            var result = BlendMath.TruncateToMaxSpeed(force, 10f);

            Assert.That(result.x, Is.EqualTo(10f).Within(Epsilon));
            Assert.That(result.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void TruncateToMaxSpeed_ZeroForce_ReturnsZero()
        {
            var result = BlendMath.TruncateToMaxSpeed(float2.zero, 10f);

            Assert.That(result, Is.EqualTo(float2.zero));
        }

        [Test]
        public void TruncateToMaxSpeed_ZeroMaxSpeed_ReturnsForce()
        {
            // maxSpeed=0, but force isn't zero: it will try normalize*0 = 0
            // Actually sq > maxSq = 0 => true, sq > 0.0001 => true, returns normalize*0 = 0
            var result = BlendMath.TruncateToMaxSpeed(new float2(5f, 0f), 0f);

            Assert.That(result, Is.EqualTo(float2.zero));
        }

        #endregion
    }
}
