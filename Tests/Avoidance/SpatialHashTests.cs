using NUnit.Framework;
using BovineLabs.Combat.Avoidance;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.Avoidance.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class SpatialHashTests
    {
        const float Epsilon = 0.001f;

        [Test]
        public void Cell_CenterPosition_ReturnsZeroCell()
        {
            var cell = SpatialHash.Cell(new float2(5f, 5f), 10f);
            Assert.That(cell.x, Is.EqualTo(0));
            Assert.That(cell.y, Is.EqualTo(0));
        }

        [Test]
        public void Cell_OffsetPosition_ReturnsCorrectCell()
        {
            var cell = SpatialHash.Cell(new float2(15f, 25f), 10f);
            Assert.That(cell.x, Is.EqualTo(1));
            Assert.That(cell.y, Is.EqualTo(2));
        }

        [Test]
        public void Cell_NegativePosition_ReturnsNegativeCell()
        {
            var cell = SpatialHash.Cell(new float2(-5f, -15f), 10f);
            Assert.That(cell.x, Is.EqualTo(-1));
            Assert.That(cell.y, Is.EqualTo(-2));
        }

        [Test]
        public void Query_OneAgentInRange_ReturnsIt()
        {
            var positions = new NativeArray<float2>(new float2[] { new float2(5f, 0f) }, Allocator.Temp);
            var radii = new NativeArray<float>(new float[] { 0.5f }, Allocator.Temp);
            try
            {
                var results = SpatialHash.Query(float2.zero, 10f, 5f, positions, radii, Allocator.Temp);
                try
                {
                    Assert.That(results.Length, Is.EqualTo(1));
                    Assert.That(results[0], Is.EqualTo(0));
                }
                finally { results.Dispose(); }
            }
            finally { positions.Dispose(); radii.Dispose(); }
        }

        [Test]
        public void Query_AgentOutOfRange_ReturnsEmpty()
        {
            var positions = new NativeArray<float2>(new float2[] { new float2(100f, 0f) }, Allocator.Temp);
            var radii = new NativeArray<float>(new float[] { 0.5f }, Allocator.Temp);
            try
            {
                var results = SpatialHash.Query(float2.zero, 10f, 5f, positions, radii, Allocator.Temp);
                try
                {
                    Assert.That(results.Length, Is.EqualTo(0));
                }
                finally { results.Dispose(); }
            }
            finally { positions.Dispose(); radii.Dispose(); }
        }

        [Test]
        public void FindNearest_ClosestFirst()
        {
            var positions = new NativeArray<float2>(new float2[]
            {
                new float2(3f, 0f),
                new float2(10f, 0f),
                new float2(1f, 0f),
            }, Allocator.Temp);
            var radii = new NativeArray<float>(new float[] { 0.5f, 0.5f, 0.5f }, Allocator.Temp);
            try
            {
                var nearest = SpatialHash.FindNearest(float2.zero, positions, radii, 20f);
                Assert.That(nearest, Is.EqualTo(2)); // index 2 is closest (dist=1)
            }
            finally { positions.Dispose(); radii.Dispose(); }
        }
    }
}
