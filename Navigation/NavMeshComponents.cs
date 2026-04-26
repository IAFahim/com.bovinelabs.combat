using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using BovineLabs.Recast;

namespace BovineLabs.Combat.Navigation
{
    /// <summary>
    /// Reference to a NavMesh surface. Add to a singleton entity.
    /// The DtNavMesh* is a native pointer owned externally (typically by a bake-time system).
    /// </summary>
    public struct NavMeshReference : IComponentData
    {
        /// <summary>Pointer to the DtNavMesh. Must remain valid for the lifetime of this component.</summary>
        public unsafe DtNavMesh* NavMesh;

        /// <summary>Whether the navmesh is valid and usable.</summary>
        public bool IsValid
        {
            get
            {
                unsafe { return NavMesh != null; }
            }
        }
    }

    /// <summary>
    /// A pathfinding request. Add this component to an agent to trigger path computation.
    /// The NavigationSystem processes it and fills in the PathResult buffer.
    /// </summary>
    public struct PathRequest : IComponentData, IEnableableComponent
    {
        /// <summary>Start position on XZ plane (Y derived from NavMesh).</summary>
        public float2 Start;

        /// <summary>End position on XZ plane.</summary>
        public float3 EndPosition;

        /// <summary>Search half-extents for finding nearest poly.</summary>
        public float3 HalfExtents;

        /// <summary>Maximum path length (number of polygon refs).</summary>
        public int MaxPathLength;

        /// <summary>Whether the request needs processing.</summary>
        public bool NeedsUpdate;

        public static PathRequest Default => new()
        {
            HalfExtents = new float3(2f, 4f, 2f),
            MaxPathLength = 256,
            NeedsUpdate = true,
        };
    }

    /// <summary>
    /// Path result: a polyline of waypoints the agent should follow.
    /// Stored as a DynamicBuffer on the agent entity.
    /// </summary>
    [InternalBufferCapacity(32)]
    public struct PathWaypoint : IBufferElementData
    {
        public float3 Position;
        public DtPolyRef Polygon;

        public PathWaypoint(float3 position, DtPolyRef polygon = default)
        {
            Position = position;
            Polygon = polygon;
        }
    }

    /// <summary>
    /// Path corridor state. Tracks which waypoint the agent is currently heading toward.
    /// </summary>
    public struct PathCorridor : IComponentData
    {
        /// <summary>Index into PathWaypoint buffer of current target waypoint.</summary>
        public int CurrentWaypointIndex;

        /// <summary>Distance at which agent advances to next waypoint.</summary>
        public float WaypointArrivalThreshold;

        /// <summary>Whether the path is complete (agent reached final waypoint).</summary>
        public bool IsComplete;

        public static PathCorridor Default => new()
        {
            CurrentWaypointIndex = 0,
            WaypointArrivalThreshold = 0.5f,
            IsComplete = false,
        };
    }

    /// <summary>
    /// NavMesh area costs for pathfinding queries.
    /// 64 area types, matching Detour's max areas.
    /// </summary>
    public unsafe struct NavMeshAreaCosts : IComponentData
    {
        public fixed float Costs[64];
        public ushort IncludeFlags;
        public ushort ExcludeFlags;

        public static NavMeshAreaCosts Default => new()
        {
            IncludeFlags = 0xffff,
            ExcludeFlags = 0,
        };
    }

    /// <summary>
    /// Static utility for NavMesh pathfinding operations.
    /// Wraps DtNavMeshQuery into a friendlier API.
    /// </summary>
    public static unsafe class NavMeshPathfinder
    {
        /// <summary>
        /// Find a path from start to end on the NavMesh.
        /// Returns path waypoints as a NativeList.
        /// </summary>
        public static bool FindPath(
            DtNavMesh* navMesh,
            float3 start,
            float3 end,
            float3 halfExtents,
            ref DtQueryFilter filter,
            int maxPath,
            NativeList<float3> outPath,
            Allocator allocator)
        {
            var query = DtNavMeshQuery.Create(navMesh, 4096, allocator);
            try
            {
                // Find nearest polygons to start/end
                var startRef = default(DtPolyRef);
                var endRef = default(DtPolyRef);
                var startPt = start;
                var endPt = end;

                var status = query->FindNearestPoly(start, halfExtents, ref filter, ref startRef, ref startPt);
                if (!Detour.StatusSucceed(status)) return false;

                status = query->FindNearestPoly(end, halfExtents, ref filter, ref endRef, ref endPt);
                if (!Detour.StatusSucceed(status)) return false;

                if (startRef.Equals(default) || endRef.Equals(default))
                    return false;

                // Find polygon path
                var polyPath = new NativeArray<DtPolyRef>(maxPath, allocator, NativeArrayOptions.UninitializedMemory);
                int pathCount = 0;
                try
                {
                    status = query->FindPath(startRef, endRef, startPt, endPt, ref filter,
                        (DtPolyRef*)polyPath.GetUnsafePtr(), out pathCount, maxPath);

                    if (!Detour.StatusSucceed(status) || pathCount == 0)
                        return false;

                    // Get straight path (waypoints)
                    var straightPath = new NativeArray<float3>(pathCount, allocator, NativeArrayOptions.UninitializedMemory);
                    var straightFlags = new NativeArray<DtStraightPathFlags>(pathCount, allocator, NativeArrayOptions.UninitializedMemory);
                    var straightRefs = new NativeArray<DtPolyRef>(pathCount, allocator, NativeArrayOptions.UninitializedMemory);
                    try
                    {
                        int straightCount = 0;
                        status = query->FindStraightPath(
                            startPt, endPt,
                            (DtPolyRef*)polyPath.GetUnsafePtr(), pathCount,
                            (float3*)straightPath.GetUnsafePtr(),
                            (DtStraightPathFlags*)straightFlags.GetUnsafePtr(),
                            (DtPolyRef*)straightRefs.GetUnsafePtr(),
                            out straightCount, pathCount);

                        if (!Detour.StatusSucceed(status)) return false;

                        outPath.Clear();
                        for (int i = 0; i < straightCount; i++)
                        {
                            outPath.Add(straightPath[i]);
                        }

                        return outPath.Length > 0;
                    }
                    finally
                    {
                        straightPath.Dispose();
                        straightFlags.Dispose();
                        straightRefs.Dispose();
                    }
                }
                finally
                {
                    polyPath.Dispose();
                }
            }
            finally
            {
                DtNavMeshQuery.Free(query);
            }
        }

        /// <summary>
        /// Create a DtQueryFilter from NavMeshAreaCosts.
        /// Caller is responsible for the returned filter's lifetime.
        /// </summary>
        public static DtQueryFilter CreateQueryFilter(NavMeshAreaCosts costs)
        {
            var filter = DtQueryFilter.CreateDefault();
            filter.SetIncludeFlags(costs.IncludeFlags);
            filter.SetExcludeFlags(costs.ExcludeFlags);
            for (int i = 0; i < 64; i++)
            {
                unsafe { filter.SetAreaCost(i, costs.Costs[i]); }
            }
            return filter;
        }
    }
}
