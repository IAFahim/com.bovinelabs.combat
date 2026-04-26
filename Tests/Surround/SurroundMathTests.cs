using NUnit.Framework;
using BovineLabs.Combat.Surround;
using Unity.Mathematics;

namespace BovineLabs.Combat.Surround.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class SurroundMathTests
    {
        const float Epsilon = 0.001f;

        #region ComputeSurroundPosition

        [Test]
        public void ComputeSurroundPosition_SlotZero_AtAngleZero()
        {
            var pos = SurroundMath.ComputeSurroundPosition(
                new float2(0f, 0f), 5f, 0, 4);

            // angle = 2*PI*0/4 = 0 => (cos(0)*5, sin(0)*5) = (5,0)
            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(0f).Within(Epsilon));
        }

        [Test]
        public void ComputeSurroundPosition_FourSlots_EvenlySpaced()
        {
            var center = new float2(0f, 0f);
            for (int i = 0; i < 4; i++)
            {
                var pos = SurroundMath.ComputeSurroundPosition(center, 3f, i, 4);
                var angle = (2f * math.PI * i) / 4f;
                var expected = center + new float2(math.cos(angle) * 3f, math.sin(angle) * 3f);
                Assert.That(pos.x, Is.EqualTo(expected.x).Within(Epsilon),
                    $"Slot {i} X mismatch");
                Assert.That(pos.y, Is.EqualTo(expected.y).Within(Epsilon),
                    $"Slot {i} Y mismatch");
            }
        }

        [Test]
        public void ComputeSurroundPosition_ZeroSlots_ReturnsCenter()
        {
            var pos = SurroundMath.ComputeSurroundPosition(
                new float2(5f, 10f), 3f, 0, 0);

            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(10f).Within(Epsilon));
        }

        [Test]
        public void ComputeSurroundPosition_NegativeSlots_ReturnsCenter()
        {
            var pos = SurroundMath.ComputeSurroundPosition(
                new float2(5f, 10f), 3f, 0, -1);

            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(10f).Within(Epsilon));
        }

        [Test]
        public void ComputeSurroundPosition_AllSlotsAtRadius()
        {
            var center = new float2(10f, 20f);
            const float radius = 7f;
            const int slots = 6;

            for (int i = 0; i < slots; i++)
            {
                var pos = SurroundMath.ComputeSurroundPosition(center, radius, i, slots);
                var dist = math.length(pos - center);
                Assert.That(dist, Is.EqualTo(radius).Within(Epsilon),
                    $"Slot {i} not at radius");
            }
        }

        [Test]
        public void ComputeSurroundPosition_SingleSlot_AtAngleZero()
        {
            var pos = SurroundMath.ComputeSurroundPosition(
                new float2(0f, 0f), 5f, 0, 1);

            Assert.That(pos.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(pos.y, Is.EqualTo(0f).Within(Epsilon));
        }

        #endregion

        #region ComputeSurroundSlot

        [Test]
        public void ComputeSurroundSlot_AgentNearSlot0_ReturnsSlot0()
        {
            // Slot 0 at (5,0), put agent at (5,0)
            var slot = SurroundMath.ComputeSurroundSlot(
                new float2(5f, 0f), new float2(0f, 0f), 5f, 4);

            Assert.That(slot, Is.EqualTo(0));
        }

        [Test]
        public void ComputeSurroundSlot_AgentNearSlot2_ReturnsSlot2()
        {
            // With 4 slots: slot 2 at angle PI => (-5, 0)
            var slot = SurroundMath.ComputeSurroundSlot(
                new float2(-4.9f, 0f), new float2(0f, 0f), 5f, 4);

            Assert.That(slot, Is.EqualTo(2));
        }

        [Test]
        public void ComputeSurroundSlot_ZeroSlots_ReturnsZero()
        {
            var slot = SurroundMath.ComputeSurroundSlot(
                new float2(5f, 0f), new float2(0f, 0f), 5f, 0);

            Assert.That(slot, Is.EqualTo(0));
        }

        [Test]
        public void ComputeSurroundSlot_AgentBetweenSlots_PicksNearest()
        {
            // 4 slots: 0=(5,0), 1=(0,5), 2=(-5,0), 3=(0,-5)
            // Agent at (3,3) should be nearest to slot 1 (0,5) or slot 0 (5,0)
            // dist to slot0 = sqrt(4+9)=sqrt13≈3.6
            // dist to slot1 = sqrt(9+4)=sqrt13≈3.6
            // Both equal - first found (slot 0) wins
            var slot = SurroundMath.ComputeSurroundSlot(
                new float2(3f, 3f), new float2(0f, 0f), 5f, 4);

            Assert.That(slot, Is.EqualTo(0));
        }

        [Test]
        public void ComputeSurroundSlot_AgentFarAway_PicksNearest()
        {
            // Agent far away but closest to slot 1 (0,5)
            var slot = SurroundMath.ComputeSurroundSlot(
                new float2(0f, 100f), new float2(0f, 0f), 5f, 4);

            Assert.That(slot, Is.EqualTo(1));
        }

        #endregion
    }
}
