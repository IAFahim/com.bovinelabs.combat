using NUnit.Framework;
using Unity.Mathematics;

namespace BovineLabs.Combat.Navigation.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class NavComponentTests
    {
        const float Epsilon = 0.001f;

        #region PathCorridor

        [Test]
        public void PathCorridor_Default_IndexIsZero()
        {
            var corridor = PathCorridor.Default;
            Assert.That(corridor.CurrentWaypointIndex, Is.EqualTo(0));
        }

        [Test]
        public void PathCorridor_Default_ThresholdIsHalf()
        {
            var corridor = PathCorridor.Default;
            Assert.That(corridor.WaypointArrivalThreshold, Is.EqualTo(0.5f));
        }

        [Test]
        public void PathCorridor_Default_NotComplete()
        {
            var corridor = PathCorridor.Default;
            Assert.That(corridor.IsComplete, Is.False);
        }

        [Test]
        public void PathCorridor_Modified_Complete()
        {
            var corridor = new PathCorridor
            {
                CurrentWaypointIndex = 5,
                WaypointArrivalThreshold = 1.0f,
                IsComplete = true,
            };

            Assert.That(corridor.CurrentWaypointIndex, Is.EqualTo(5));
            Assert.That(corridor.WaypointArrivalThreshold, Is.EqualTo(1.0f));
            Assert.That(corridor.IsComplete, Is.True);
        }

        #endregion

        #region PathRequest

        [Test]
        public void PathRequest_Default_NeedsUpdate()
        {
            var request = PathRequest.Default;
            Assert.That(request.NeedsUpdate, Is.True);
        }

        [Test]
        public void PathRequest_Default_MaxPathLength256()
        {
            var request = PathRequest.Default;
            Assert.That(request.MaxPathLength, Is.EqualTo(256));
        }

        [Test]
        public void PathRequest_Default_HalfExtents()
        {
            var request = PathRequest.Default;
            Assert.That(request.HalfExtents.x, Is.EqualTo(2f).Within(Epsilon));
            Assert.That(request.HalfExtents.y, Is.EqualTo(4f).Within(Epsilon));
            Assert.That(request.HalfExtents.z, Is.EqualTo(2f).Within(Epsilon));
        }

        [Test]
        public void PathRequest_Modified_StartEnd()
        {
            var request = new PathRequest
            {
                Start = new float2(1f, 2f),
                EndPosition = new float3(10f, 0f, 20f),
                HalfExtents = new float3(1f, 1f, 1f),
                MaxPathLength = 128,
                NeedsUpdate = false,
            };

            Assert.That(request.Start.x, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(request.Start.y, Is.EqualTo(2f).Within(Epsilon));
            Assert.That(request.EndPosition.x, Is.EqualTo(10f).Within(Epsilon));
            Assert.That(request.EndPosition.z, Is.EqualTo(20f).Within(Epsilon));
            Assert.That(request.MaxPathLength, Is.EqualTo(128));
            Assert.That(request.NeedsUpdate, Is.False);
        }

        #endregion

        #region NavMeshAreaCosts

        [Test]
        public void NavMeshAreaCosts_Default_IncludeAll()
        {
            var costs = NavMeshAreaCosts.Default;
            Assert.That(costs.IncludeFlags, Is.EqualTo((ushort)0xffff));
        }

        [Test]
        public void NavMeshAreaCosts_Default_ExcludeNone()
        {
            var costs = NavMeshAreaCosts.Default;
            Assert.That(costs.ExcludeFlags, Is.EqualTo((ushort)0));
        }

        [Test]
        public void NavMeshAreaCosts_Default_CostsAreZero()
        {
            var costs = NavMeshAreaCosts.Default;
            // Fixed array defaults to 0
            for (int i = 0; i < 64; i++)
            {
                Assert.That(costs.Costs[i], Is.EqualTo(0f));
            }
        }

        [Test]
        public void NavMeshAreaCosts_Modified_FlagsSet()
        {
            var costs = new NavMeshAreaCosts
            {
                IncludeFlags = 0x0001,
                ExcludeFlags = 0x00ff,
            };

            Assert.That(costs.IncludeFlags, Is.EqualTo((ushort)0x0001));
            Assert.That(costs.ExcludeFlags, Is.EqualTo((ushort)0x00ff));
        }

        #endregion

        #region PathWaypoint

        [Test]
        public void PathWaypoint_ConstructsWithPosition()
        {
            var wp = new PathWaypoint(new float3(1f, 2f, 3f));
            Assert.That(wp.Position.x, Is.EqualTo(1f).Within(Epsilon));
            Assert.That(wp.Position.y, Is.EqualTo(2f).Within(Epsilon));
            Assert.That(wp.Position.z, Is.EqualTo(3f).Within(Epsilon));
        }

        [Test]
        public void PathWaypoint_DefaultPolygon_IsDefault()
        {
            var wp = new PathWaypoint(new float3(1f, 2f, 3f));
            Assert.That(wp.Polygon.Equals(default), Is.True);
        }

        [Test]
        public void PathWaypoint_ConstructsWithPositionAndPolygon()
        {
            DtPolyRef poly = 42; // implicit operator from int
            var wp = new PathWaypoint(new float3(5f, 0f, 10f), poly);
            Assert.That(wp.Position.x, Is.EqualTo(5f).Within(Epsilon));
            Assert.That(wp.Position.z, Is.EqualTo(10f).Within(Epsilon));
            Assert.That(wp.Polygon.Equals(poly), Is.True);
        }

        [Test]
        public void PathWaypoint_ZeroPosition()
        {
            var wp = new PathWaypoint(float3.zero);
            Assert.That(wp.Position.x, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(wp.Position.y, Is.EqualTo(0f).Within(Epsilon));
            Assert.That(wp.Position.z, Is.EqualTo(0f).Within(Epsilon));
        }

        #endregion
    }
}
