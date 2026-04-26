using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.RoomTraversal
{
    /// <summary>
    /// Pure math utility functions for room-based navigation.
    /// Room finding, door lookups, and BFS pathfinding through room graphs.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class RoomTraversalMath
    {
        /// <summary>
        /// Find which room contains a given position.
        /// A position is inside a room if it falls within the room's AABB
        /// (center +/- halfExtents on each axis).
        /// </summary>
        /// <param name="pos">XZ position to test.</param>
        /// <param name="roomBounds">Buffer of all room bounds.</param>
        /// <param name="count">Number of valid entries in roomBounds.</param>
        /// <returns>RoomId containing the position, or -1 if none.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindRoomContaining(float2 pos, NativeArray<RoomBounds> roomBounds, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var min = roomBounds[i].Center - roomBounds[i].HalfExtents;
                var max = roomBounds[i].Center + roomBounds[i].HalfExtents;

                if (pos.x >= min.x && pos.x <= max.x && pos.y >= min.y && pos.y <= max.y)
                {
                    return roomBounds[i].RoomId;
                }
            }

            return -1;
        }

        /// <summary>
        /// Find the door position connecting two adjacent rooms.
        /// Searches for a connection from fromRoom to toRoom.
        /// </summary>
        /// <param name="fromRoom">Source room ID.</param>
        /// <param name="toRoom">Destination room ID.</param>
        /// <param name="connections">Buffer of all room connections.</param>
        /// <param name="count">Number of valid entries in connections.</param>
        /// <returns>Door position between the two rooms, or zero if no direct connection.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 FindDoorToRoom(int fromRoom, int toRoom, NativeArray<RoomConnection> connections, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (connections[i].FromRoomId == fromRoom && connections[i].ToRoomId == toRoom)
                {
                    return connections[i].DoorPosition;
                }
            }

            return float2.zero;
        }

        /// <summary>
        /// Plan a route from one room to another using BFS.
        /// Returns a list of room IDs forming the path, including start and end rooms.
        /// Caller is responsible for disposing the returned NativeList.
        /// </summary>
        /// <param name="fromRoom">Starting room ID.</param>
        /// <param name="toRoom">Destination room ID.</param>
        /// <param name="connections">Buffer of all room connections (bidirectional).</param>
        /// <param name="count">Number of valid entries in connections.</param>
        /// <param name="allocator">Allocator for the returned NativeList.</param>
        /// <returns>NativeList of room IDs from start to end. Empty if no path found.</returns>
        public static NativeList<int> PlanRoomRoute(int fromRoom, int toRoom, NativeArray<RoomConnection> connections, int count, Allocator allocator)
        {
            var route = new NativeList<int>(allocator);

            if (fromRoom == toRoom)
            {
                route.Add(fromRoom);
                return route;
            }

            // BFS to find shortest path through rooms
            // visited: room ID -> parent room ID (-1 = unvisited)
            var maxRoomId = 0;
            for (int i = 0; i < count; i++)
            {
                maxRoomId = math.max(maxRoomId, math.max(connections[i].FromRoomId, connections[i].ToRoomId));
            }

            var visitedSize = maxRoomId + 1;
            var parent = new NativeArray<int>(visitedSize, Allocator.Temp);
            for (int i = 0; i < visitedSize; i++)
                parent[i] = -1;

            var queue = new NativeQueue<int>(Allocator.Temp);

            // Start BFS
            parent[fromRoom] = fromRoom;
            queue.Enqueue(fromRoom);

            var found = false;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();

                if (current == toRoom)
                {
                    found = true;
                    break;
                }

                // Explore neighbors
                for (int i = 0; i < count; i++)
                {
                    // Forward connection
                    if (connections[i].FromRoomId == current)
                    {
                        var neighbor = connections[i].ToRoomId;
                        if (neighbor >= 0 && neighbor < visitedSize && parent[neighbor] == -1)
                        {
                            parent[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
                    }
                    // Reverse connection (bidirectional)
                    else if (connections[i].ToRoomId == current)
                    {
                        var neighbor = connections[i].FromRoomId;
                        if (neighbor >= 0 && neighbor < visitedSize && parent[neighbor] == -1)
                        {
                            parent[neighbor] = current;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }

            // Reconstruct path
            if (found)
            {
                // Trace back from toRoom to fromRoom
                var path = new NativeList<int>(Allocator.Temp);
                var node = toRoom;
                while (node != fromRoom)
                {
                    path.Add(node);
                    node = parent[node];
                    if (node == -1)
                        break;
                }

                if (node == fromRoom)
                {
                    path.Add(fromRoom);

                    // Reverse to get start-to-end order
                    for (int i = path.Length - 1; i >= 0; i--)
                    {
                        route.Add(path[i]);
                    }
                }
                else
                {
                    // Path broken - shouldn't happen if found is true
                    route.Clear();
                }

                path.Dispose();
            }

            parent.Dispose();
            queue.Dispose();

            return route;
        }

        /// <summary>
        /// Check if the agent has reached a door position (within arrival threshold).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool HasReachedDoor(float2 agentPos, float2 doorPos, float arrivalThreshold)
        {
            var distSq = math.lengthsq(agentPos - doorPos);
            return distSq <= arrivalThreshold * arrivalThreshold;
        }
    }
}
