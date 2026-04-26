using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.RoomTraversal
{
    /// <summary>
    /// Singleton component identifying which room the agent is currently in.
    /// </summary>
    public struct RoomGraph : IComponentData
    {
        /// <summary>ID of the room the agent currently occupies.</summary>
        public int CurrentRoomId;

        public static RoomGraph Default => new() { CurrentRoomId = -1 };
    }

    /// <summary>
    /// A connection (doorway) between two rooms.
    /// Stored as a buffer on the room graph entity.
    /// Connections are bidirectional: both (A->B) and (B->A) should be added.
    /// </summary>
    public struct RoomConnection : IBufferElementData
    {
        /// <summary>Source room ID.</summary>
        public int FromRoomId;

        /// <summary>Destination room ID.</summary>
        public int ToRoomId;

        /// <summary>XZ position of the door between rooms.</summary>
        public float2 DoorPosition;

        /// <summary>Width of the doorway for steering purposes.</summary>
        public float DoorWidth;

        public static RoomConnection New(int from, int to, float2 doorPos, float doorWidth) => new()
        {
            FromRoomId = from,
            ToRoomId = to,
            DoorPosition = doorPos,
            DoorWidth = doorWidth,
        };
    }

    /// <summary>
    /// Definition of a room's bounds in the XZ plane.
    /// Stored as a buffer on the room graph entity (one entry per room).
    /// </summary>
    public struct RoomBounds : IBufferElementData
    {
        /// <summary>Unique room identifier.</summary>
        public int RoomId;

        /// <summary>Center of the room on the XZ plane.</summary>
        public float2 Center;

        /// <summary>Half-extents (half-width, half-depth) of the room's AABB.</summary>
        public float2 HalfExtents;

        public static RoomBounds New(int roomId, float2 center, float2 halfExtents) => new()
        {
            RoomId = roomId,
            Center = center,
            HalfExtents = halfExtents,
        };
    }

    /// <summary>
    /// Target room and position for the agent to navigate toward.
    /// When set, the RoomTraversalSystem plans a route through rooms
    /// and steers toward the next door.
    /// </summary>
    public struct RoomTraversalTarget : IComponentData, IEnableableComponent
    {
        /// <summary>ID of the destination room.</summary>
        public int TargetRoomId;

        /// <summary>Final XZ position within the target room.</summary>
        public float2 TargetPosition;

        /// <summary>ID of the next room on the planned route.</summary>
        public int NextRoomId;

        /// <summary>Door position the agent is currently heading toward.</summary>
        public float2 NextDoorPosition;

        /// <summary>Whether a route is currently planned and active.</summary>
        public bool HasRoute;

        public static RoomTraversalTarget Default => new()
        {
            TargetRoomId = -1,
            TargetPosition = float2.zero,
            NextRoomId = -1,
            NextDoorPosition = float2.zero,
            HasRoute = false,
        };
    }
}
