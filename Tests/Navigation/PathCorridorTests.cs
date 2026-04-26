using NUnit.Framework;
using Unity.Mathematics;

namespace BovineLabs.Combat.Navigation.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class PathCorridorTests
    {
        const float Epsilon = 0.001f;

        [Test]
        public void PathCorridor_Default_NotComplete()
        {
            var corridor = PathCorridor.Default;
            Assert.That(corridor.IsComplete, Is.False);
            Assert.That(corridor.CurrentWaypointIndex, Is.EqualTo(0));
            Assert.That(corridor.WaypointArrivalThreshold, Is.EqualTo(0.5f));
        }

        [Test]
        public void PathRequest_Default_NeedsUpdate()
        {
            var request = PathRequest.Default;
            Assert.That(request.NeedsUpdate, Is.True);
            Assert.That(request.MaxPathLength, Is.EqualTo(256));
        }

        [Test]
        public void NavMeshAreaCosts_Default_IncludeAll()
        {
            var costs = NavMeshAreaCosts.Default;
            Assert.That(costs.IncludeFlags, Is.EqualTo((ushort)0xffff));
            Assert.That(costs.ExcludeFlags, Is.EqualTo((ushort)0));
        }

        [Test]
        public void PathWaypoint_ConstructsWithPosition()
        {
            var wp = new PathWaypoint(new float3(1f, 0f, 2f));
            Assert.That(wp.Position.x, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(wp.Position.z, Is.EqualTo(2f).Within(Epsilon));
        }
    }
}
