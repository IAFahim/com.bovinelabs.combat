using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Patrol
{
    /// <summary>
    /// Dynamic buffer of patrol waypoints. Each waypoint has a position and wait time.
    /// The patrol system cycles through these waypoints in order.
    /// </summary>
    public struct PatrolWaypoints : IBufferElementData
    {
        /// <summary>Waypoint position in world space (Y is typically 0 for XZ plane patrol).</summary>
        public float3 Position;

        /// <summary>Time in seconds to wait at this waypoint before moving to the next.</summary>
        public float WaitTime;

        public static implicit operator PatrolWaypoints(float3 pos) => new() { Position = pos, WaitTime = 0f };

        public PatrolWaypoints(float3 position, float waitTime = 0f)
        {
            Position = position;
            WaitTime = waitTime;
        }
    }

    /// <summary>
    /// Current state for waypoint-based patrol. Tracks which waypoint the agent is
    /// heading toward, whether it is currently waiting, and the wait timer.
    /// </summary>
    public struct PatrolState : IComponentData, IEnableableComponent
    {
        /// <summary>Index of the current waypoint we are moving toward or waiting at.</summary>
        public int CurrentWaypointIndex;

        /// <summary>Timer for waiting at the current waypoint. Counts up from 0.</summary>
        public float WaitTimer;

        /// <summary>Whether the agent is currently waiting at a waypoint.</summary>
        public bool IsWaiting;

        /// <summary>Whether the patrol loops (true) or stops at the last waypoint (false).</summary>
        public bool Loop;

        public static PatrolState Default => new()
        {
            CurrentWaypointIndex = 0,
            WaitTimer = 0f,
            IsWaiting = false,
            Loop = true,
        };
    }

    /// <summary>
    /// Defines a rectangular patrol area for area-based random wandering.
    /// The agent picks random points within the area and moves between them.
    /// </summary>
    public struct PatrolArea : IComponentData, IEnableableComponent
    {
        /// <summary>Center of the patrol area on the XZ plane.</summary>
        public float2 Center;

        /// <summary>Half-extents of the patrol area (width/2, depth/2).</summary>
        public float2 HalfExtents;

        /// <summary>Minimum idle time between reaching a point and picking a new one.</summary>
        public float MinIdleTime;

        /// <summary>Maximum idle time between reaching a point and picking a new one.</summary>
        public float MaxIdleTime;

        public static PatrolArea Default => new()
        {
            Center = float2.zero,
            HalfExtents = new float2(10f, 10f),
            MinIdleTime = 1f,
            MaxIdleTime = 3f,
        };
    }

    /// <summary>
    /// Current state for area-based patrol. Tracks the current random target point
    /// and idle timer between movements.
    /// </summary>
    public struct PatrolAreaState : IComponentData, IEnableableComponent
    {
        /// <summary>Current target position on the XZ plane.</summary>
        public float2 CurrentTarget;

        /// <summary>Timer for idle time between reaching a point and picking a new one.</summary>
        public float IdleTimer;

        /// <summary>Whether the agent is currently idle (waiting at a reached point).</summary>
        public bool IsIdle;

        /// <summary>Random seed for generating patrol points. Initialize with unique seed.</summary>
        public uint RandomSeed;

        public static PatrolAreaState Default => new()
        {
            CurrentTarget = float2.zero,
            IdleTimer = 0f,
            IsIdle = false,
            RandomSeed = 1,
        };
    }
}
