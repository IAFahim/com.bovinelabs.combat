using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// DynamicBuffer element storing a single neighbor entity and cached spatial data.
    /// Populated by SpatialIntelligence systems each frame using the BovineLabs SpatialMap.
    /// Other modules (Group, Avoidance, TargetSelection, etc.) read this buffer
    /// instead of doing their own brute-force O(n^2) neighbor searches.
    /// </summary>
    public struct SpatialNeighborData : IBufferElementData
    {
        /// <summary>The neighbor entity.</summary>
        public Entity Entity;

        /// <summary>Distance from owner to this neighbor (XZ plane).</summary>
        public float Distance;

        /// <summary>Normalized direction from owner to this neighbor (XZ plane).</summary>
        public float2 Direction;

        /// <summary>Neighbor's team ID. Cached to avoid component lookup.</summary>
        public int TeamId;

        /// <summary>Neighbor's current velocity (XZ). Cached for alignment behaviors.</summary>
        public float2 Velocity;

        /// <summary>Neighbor's radius. Cached for separation behaviors.</summary>
        public float Radius;
    }
}
