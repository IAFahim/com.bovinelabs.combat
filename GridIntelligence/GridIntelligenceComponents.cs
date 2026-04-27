using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.GridIntelligence
{
    /// <summary>
    /// Configuration for local grid-based tactical analysis.
    /// Enableable: agents without this component skip grid analysis.
    /// The grid is centered on the agent and rebuilt each frame.
    /// </summary>
    public struct TacticalGridConfig : IComponentData, IEnableableComponent
    {
        /// <summary>Total grid extent (grid is GridRadius x GridRadius around agent).</summary>
        public float GridRadius;

        /// <summary>Cells per side (e.g., 8 = 8x8 = 64 cells).</summary>
        public int GridResolution;

        /// <summary>Density above which a cell is considered 'dangerous'.</summary>
        public float ThreatThreshold;

        /// <summary>How strongly to steer away from threat cells (0..1).</summary>
        public float CoverWeight;

        /// <summary>Sensible defaults.</summary>
        public static TacticalGridConfig Default => new()
        {
            GridRadius = 20f,
            GridResolution = 8,
            ThreatThreshold = 0.5f,
            CoverWeight = 0.8f,
        };
    }

    /// <summary>
    /// Per-frame tactical analysis computed from the local grid.
    /// Written by GridIntelligenceSystem after reading the SpatialNeighborData buffer.
    /// Other systems (Flank, Kite, CombatAI) read this for tactical decisions.
    /// </summary>
    public struct TacticalGridData : IComponentData
    {
        /// <summary>Direction (XZ) with lowest threat density - best retreat/cover path.</summary>
        public float2 SafestDirection;

        /// <summary>Direction (XZ) with highest threat density - where enemies are densest.</summary>
        public float2 DangerDirection;

        /// <summary>Peak threat density in any single cell.</summary>
        public float MaxThreatDensity;

        /// <summary>Mean threat density across all cells.</summary>
        public float AverageThreatDensity;

        /// <summary>Number of cells above ThreatThreshold.</summary>
        public int DangerousCellCount;

        /// <summary>
        /// Perpendicular to danger direction - best flanking angle.
        /// Rotated 90 degrees clockwise from DangerDirection.
        /// </summary>
        public float2 FlankingDirection;
    }
}
