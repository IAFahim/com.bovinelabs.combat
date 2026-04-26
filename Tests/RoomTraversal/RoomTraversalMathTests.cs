using NUnit.Framework;
using BovineLabs.Combat.RoomTraversal;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.RoomTraversal.Tests
{
    [TestFixture]
    [Category("Combat")]
    public class RoomTraversalMathTests
    {
        const float Epsilon = 0.001f;

        #region FindRoomContaining

        [Test]
        public void FindRoomContaining_PositionInsideRoom_ReturnsRoomId()
        {
            var rooms = new NativeArray<RoomBounds>(1, Allocator.Temp);
            try
            {
                rooms[0] = RoomBounds.New(1, new float2(10f, 10f), new float2(5f, 5f));

                var result = RoomTraversalMath.FindRoomContaining(
                    new float2(10f, 10f), rooms, 1);

                Assert.That(result, Is.EqualTo(1));
            }
            finally
            {
                rooms.Dispose();
            }
        }

        [Test]
        public void FindRoomContaining_PositionOutsideAllRooms_ReturnsMinusOne()
        {
            var rooms = new NativeArray<RoomBounds>(1, Allocator.Temp);
            try
            {
                rooms[0] = RoomBounds.New(1, new float2(10f, 10f), new float2(5f, 5f));

                var result = RoomTraversalMath.FindRoomContaining(
                    new float2(100f, 100f), rooms, 1);

                Assert.That(result, Is.EqualTo(-1));
            }
            finally
            {
                rooms.Dispose();
            }
        }

        [Test]
        public void FindRoomContaining_PositionAtEdge_ReturnsRoomId()
        {
            var rooms = new NativeArray<RoomBounds>(1, Allocator.Temp);
            try
            {
                rooms[0] = RoomBounds.New(1, new float2(10f, 10f), new float2(5f, 5f));

                // min = (5,5), max = (15,15), test at boundary
                var result = RoomTraversalMath.FindRoomContaining(
                    new float2(5f, 10f), rooms, 1);

                Assert.That(result, Is.EqualTo(1));
            }
            finally
            {
                rooms.Dispose();
            }
        }

        [Test]
        public void FindRoomContaining_MultipleRooms_ReturnsFirstContaining()
        {
            var rooms = new NativeArray<RoomBounds>(2, Allocator.Temp);
            try
            {
                rooms[0] = RoomBounds.New(1, new float2(0f, 0f), new float2(5f, 5f));
                rooms[1] = RoomBounds.New(2, new float2(20f, 20f), new float2(5f, 5f));

                var result = RoomTraversalMath.FindRoomContaining(
                    new float2(3f, 3f), rooms, 2);

                Assert.That(result, Is.EqualTo(1));
            }
            finally
            {
                rooms.Dispose();
            }
        }

        [Test]
        public void FindRoomContaining_ZeroRooms_ReturnsMinusOne()
        {
            var rooms = new NativeArray<RoomBounds>(0, Allocator.Temp);
            try
            {
                var result = RoomTraversalMath.FindRoomContaining(
                    new float2(0f, 0f), rooms, 0);

                Assert.That(result, Is.EqualTo(-1));
            }
            finally
            {
                rooms.Dispose();
            }
        }

        #endregion

        #region FindDoorToRoom

        [Test]
        public void FindDoorToRoom_ConnectionExists_ReturnsDoorPosition()
        {
            var connections = new NativeArray<RoomConnection>(1, Allocator.Temp);
            try
            {
                connections[0] = RoomConnection.New(1, 2, new float2(15f, 10f), 2f);

                var door = RoomTraversalMath.FindDoorToRoom(1, 2, connections, 1);

                Assert.That(door.x, Is.EqualTo(15f).Within(Epsilon));
                Assert.That(door.y, Is.EqualTo(10f).Within(Epsilon));
            }
            finally
            {
                connections.Dispose();
            }
        }

        [Test]
        public void FindDoorToRoom_NoConnection_ReturnsZero()
        {
            var connections = new NativeArray<RoomConnection>(1, Allocator.Temp);
            try
            {
                connections[0] = RoomConnection.New(1, 2, new float2(15f, 10f), 2f);

                var door = RoomTraversalMath.FindDoorToRoom(2, 1, connections, 1);

                Assert.That(door, Is.EqualTo(float2.zero));
            }
            finally
            {
                connections.Dispose();
            }
        }

        [Test]
        public void FindDoorToRoom_ZeroConnections_ReturnsZero()
        {
            var connections = new NativeArray<RoomConnection>(0, Allocator.Temp);
            try
            {
                var door = RoomTraversalMath.FindDoorToRoom(1, 2, connections, 0);

                Assert.That(door, Is.EqualTo(float2.zero));
            }
            finally
            {
                connections.Dispose();
            }
        }

        [Test]
        public void FindDoorToRoom_MultipleConnections_FindsCorrectOne()
        {
            var connections = new NativeArray<RoomConnection>(3, Allocator.Temp);
            try
            {
                connections[0] = RoomConnection.New(1, 2, new float2(10f, 0f), 2f);
                connections[1] = RoomConnection.New(2, 3, new float2(20f, 0f), 2f);
                connections[2] = RoomConnection.New(1, 3, new float2(15f, 15f), 2f);

                var door = RoomTraversalMath.FindDoorToRoom(1, 3, connections, 3);

                Assert.That(door.x, Is.EqualTo(15f).Within(Epsilon));
                Assert.That(door.y, Is.EqualTo(15f).Within(Epsilon));
            }
            finally
            {
                connections.Dispose();
            }
        }

        #endregion

        #region PlanRoomRoute

        [Test]
        public void PlanRoomRoute_SameRoom_ReturnsSingleRoom()
        {
            var connections = new NativeArray<RoomConnection>(0, Allocator.Temp);
            try
            {
                var route = RoomTraversalMath.PlanRoomRoute(1, 1, connections, 0, Allocator.Temp);
                try
                {
                    Assert.That(route.Length, Is.EqualTo(1));
                    Assert.That(route[0], Is.EqualTo(1));
                }
                finally
                {
                    route.Dispose();
                }
            }
            finally
            {
                connections.Dispose();
            }
        }

        [Test]
        public void PlanRoomRoute_AdjacentRooms_ReturnsTwoRooms()
        {
            var connections = new NativeArray<RoomConnection>(2, Allocator.Temp);
            try
            {
                connections[0] = RoomConnection.New(0, 1, new float2(5f, 0f), 2f);
                connections[1] = RoomConnection.New(1, 0, new float2(5f, 0f), 2f);

                var route = RoomTraversalMath.PlanRoomRoute(0, 1, connections, 2, Allocator.Temp);
                try
                {
                    Assert.That(route.Length, Is.EqualTo(2));
                    Assert.That(route[0], Is.EqualTo(0));
                    Assert.That(route[1], Is.EqualTo(1));
                }
                finally
                {
                    route.Dispose();
                }
            }
            finally
            {
                connections.Dispose();
            }
        }

        [Test]
        public void PlanRoomRoute_ThreeRoomChain_ReturnsPath()
        {
            var connections = new NativeArray<RoomConnection>(4, Allocator.Temp);
            try
            {
                // 0 -- 1 -- 2
                connections[0] = RoomConnection.New(0, 1, new float2(5f, 0f), 2f);
                connections[1] = RoomConnection.New(1, 0, new float2(5f, 0f), 2f);
                connections[2] = RoomConnection.New(1, 2, new float2(15f, 0f), 2f);
                connections[3] = RoomConnection.New(2, 1, new float2(15f, 0f), 2f);

                var route = RoomTraversalMath.PlanRoomRoute(0, 2, connections, 4, Allocator.Temp);
                try
                {
                    Assert.That(route.Length, Is.EqualTo(3));
                    Assert.That(route[0], Is.EqualTo(0));
                    Assert.That(route[1], Is.EqualTo(1));
                    Assert.That(route[2], Is.EqualTo(2));
                }
                finally
                {
                    route.Dispose();
                }
            }
            finally
            {
                connections.Dispose();
            }
        }

        [Test]
        public void PlanRoomRoute_NoPath_ReturnsEmpty()
        {
            var connections = new NativeArray<RoomConnection>(2, Allocator.Temp);
            try
            {
                // 0 -- 1, no connection to 2
                connections[0] = RoomConnection.New(0, 1, new float2(5f, 0f), 2f);
                connections[1] = RoomConnection.New(1, 0, new float2(5f, 0f), 2f);

                var route = RoomTraversalMath.PlanRoomRoute(0, 2, connections, 2, Allocator.Temp);
                try
                {
                    Assert.That(route.Length, Is.EqualTo(0));
                }
                finally
                {
                    route.Dispose();
                }
            }
            finally
            {
                connections.Dispose();
            }
        }

        [Test]
        public void PlanRoomRoute_BidirectionalBFS_WorksFromEitherEnd()
        {
            var connections = new NativeArray<RoomConnection>(2, Allocator.Temp);
            try
            {
                connections[0] = RoomConnection.New(0, 1, new float2(5f, 0f), 2f);
                connections[1] = RoomConnection.New(1, 0, new float2(5f, 0f), 2f);

                var route = RoomTraversalMath.PlanRoomRoute(1, 0, connections, 2, Allocator.Temp);
                try
                {
                    Assert.That(route.Length, Is.EqualTo(2));
                    Assert.That(route[0], Is.EqualTo(1));
                    Assert.That(route[1], Is.EqualTo(0));
                }
                finally
                {
                    route.Dispose();
                }
            }
            finally
            {
                connections.Dispose();
            }
        }

        [Test]
        public void PlanRoomRoute_DiamondGraph_PicksShortest()
        {
            // Diamond: 0->1->3 and 0->2->3
            var connections = new NativeArray<RoomConnection>(6, Allocator.Temp);
            try
            {
                connections[0] = RoomConnection.New(0, 1, new float2(5f, 5f), 2f);
                connections[1] = RoomConnection.New(1, 0, new float2(5f, 5f), 2f);
                connections[2] = RoomConnection.New(0, 2, new float2(5f, -5f), 2f);
                connections[3] = RoomConnection.New(2, 0, new float2(5f, -5f), 2f);
                connections[4] = RoomConnection.New(1, 3, new float2(15f, 5f), 2f);
                connections[5] = RoomConnection.New(2, 3, new float2(15f, -5f), 2f);

                var route = RoomTraversalMath.PlanRoomRoute(0, 3, connections, 6, Allocator.Temp);
                try
                {
                    Assert.That(route.Length, Is.EqualTo(3));
                    Assert.That(route[0], Is.EqualTo(0));
                    Assert.That(route[2], Is.EqualTo(3));
                }
                finally
                {
                    route.Dispose();
                }
            }
            finally
            {
                connections.Dispose();
            }
        }

        #endregion

        #region HasReachedDoor

        [Test]
        public void HasReachedDoor_AtDoor_ReturnsTrue()
        {
            var result = RoomTraversalMath.HasReachedDoor(
                new float2(5f, 5f), new float2(5f, 5f), 1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasReachedDoor_WithinThreshold_ReturnsTrue()
        {
            var result = RoomTraversalMath.HasReachedDoor(
                new float2(5f, 5f), new float2(5.5f, 5f), 1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasReachedDoor_BeyondThreshold_ReturnsFalse()
        {
            var result = RoomTraversalMath.HasReachedDoor(
                new float2(5f, 5f), new float2(7f, 5f), 1f);

            Assert.That(result, Is.False);
        }

        [Test]
        public void HasReachedDoor_ExactlyAtThreshold_ReturnsTrue()
        {
            var result = RoomTraversalMath.HasReachedDoor(
                new float2(0f, 0f), new float2(1f, 0f), 1f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasReachedDoor_ZeroThreshold_SamePosition_True()
        {
            var result = RoomTraversalMath.HasReachedDoor(
                new float2(5f, 5f), new float2(5f, 5f), 0f);

            Assert.That(result, Is.True);
        }

        [Test]
        public void HasReachedDoor_ZeroThreshold_DifferentPosition_False()
        {
            var result = RoomTraversalMath.HasReachedDoor(
                new float2(0f, 0f), new float2(0.001f, 0f), 0f);

            Assert.That(result, Is.False);
        }

        #endregion
    }
}
