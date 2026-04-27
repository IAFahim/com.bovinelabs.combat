using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.SpatialIntelligence
{
    /// <summary>
    /// Configuration for spatial neighbor queries per agent.
    /// Enableable: agents without this component are skipped by SpatialIntelligenceSystem.
    /// </summary>
    public struct SpatialNeighborConfig : IComponentData, IEnableableComponent
    {
        /// <summary>Radius to search for neighbors (XZ plane).</summary>
        public float SearchRadius;

        /// <summary>Maximum number of neighbors stored in the buffer to prevent explosion.</summary>
        public int MaxNeighbors;

        /// <summary>
        /// If true, only include enemies (different team) in the neighbor buffer.
        /// If false, include all agents regardless of team.
        /// </summary>
        public bool FilterByTeam;

        /// <summary>
        /// Which team ID to consider as 'enemy' for filtering.
        /// 0 = consider all non-zero teams as enemies.
        /// </summary>
        public int QueryTeamId;

        /// <summary>Sensible defaults for a medium-range combat agent.</summary>
        public static SpatialNeighborConfig Default => new()
        {
            SearchRadius = 15f,
            MaxNeighbors = 32,
            FilterByTeam = false,
            QueryTeamId = 0,
        };
    }

    /// <summary>
    /// Per-frame threat assessment computed from the spatial neighbor buffer.
    /// Written by SpatialIntelligenceSystem after populating SpatialNeighborData.
    /// Other systems (CombatAI, Flee, Kite, etc.) read this for decision-making.
    /// </summary>
    public struct SpatialThreatAssessment : IComponentData
    {
        /// <summary>Number of enemies within SearchRadius.</summary>
        public int EnemyCount;

        /// <summary>Number of allies within SearchRadius.</summary>
        public int AllyCount;

        /// <summary>
        /// Normalized direction (XZ) from agent to the nearest enemy.
        /// Zero if no enemies in range.
        /// </summary>
        public float2 NearestEnemyDirection;

        /// <summary>Distance to the nearest enemy. float.MaxValue if none.</summary>
        public float NearestEnemyDistance;

        /// <summary>
        /// Average position (centroid) of all enemies in range (XZ plane).
        /// Zero if no enemies in range.
        /// </summary>
        public float2 CentroidOfEnemies;

        /// <summary>
        /// Enemy density: enemies per unit area within the search radius.
        /// enemyCount / (PI * searchRadius^2).
        /// </summary>
        public float ThreatDensity;
    }
}
