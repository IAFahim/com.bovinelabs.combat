using System.Runtime.CompilerServices;
using BovineLabs.Combat.Core;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.GridIntelligence
{
    /// <summary>
    /// Pure static math utilities for grid-based tactical analysis.
    /// All functions operate on the XZ plane (float2), are Burst-friendly,
    /// and have no ECS dependencies. Fully unit-testable.
    /// </summary>
    public static class GridIntelligenceMath
    {
        /// <summary>
        /// Convert a world position to grid cell coordinates.
        /// </summary>
        /// <param name="worldPos">World XZ position.</param>
        /// <param name="gridOrigin">Bottom-left corner of the grid in world space.</param>
        /// <param name="cellSize">Size of each cell (both X and Y dimensions).</param>
        /// <param name="gridSize">Number of cells per side (clamps result to 0..gridSize-1).</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int2 WorldToCell(float2 worldPos, float2 gridOrigin, float2 cellSize, int2 gridSize)
        {
            var relative = worldPos - gridOrigin;
            var cell = new int2(
                (int)math.floor(relative.x / cellSize.x),
                (int)math.floor(relative.y / cellSize.y));

            // Clamp to grid bounds
            cell.x = math.clamp(cell.x, 0, gridSize.x - 1);
            cell.y = math.clamp(cell.y, 0, gridSize.y - 1);
            return cell;
        }

        /// <summary>
        /// Convert a grid cell coordinate back to world position (cell center).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 CellToWorld(int2 cell, float2 gridOrigin, float2 cellSize)
        {
            return gridOrigin + new float2(cell) * cellSize + cellSize * 0.5f;
        }

        /// <summary>
        /// Compute the flanking direction: perpendicular to the danger direction (90 degrees CW).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 ComputeFlankingDirection(float2 dangerDirection)
        {
            if (math.lengthsq(dangerDirection) < 0.0001f)
                return float2.zero;

            // Rotate 90 degrees clockwise: (x,y) -> (y, -x)
            return math.normalize(new float2(dangerDirection.y, -dangerDirection.x));
        }

        /// <summary>
        /// Compute full grid-based tactical analysis from the neighbor buffer.
        /// Divides the agent's vicinity into a grid, counts enemies per cell,
        /// and computes safest/danger directions and threat metrics.
        /// </summary>
        /// <param name="resolution">Cells per side (e.g., 8 = 8x8 grid).</param>
        /// <param name="cellSize">World-space size of each cell.</param>
        /// <param name="gridOrigin">Bottom-left corner of the grid in world space.</param>
        /// <param name="neighbors">Neighbor data buffer (from SpatialIntelligence).</param>
        /// <param name="myTeam">Agent's team ID.</param>
        /// <param name="threatThreshold">Density above which a cell is 'dangerous'.</param>
        /// <param name="result">Output tactical grid data.</param>
        public static void ComputeGridAnalysis(
            int resolution,
            float2 cellSize,
            float2 gridOrigin,
            NativeArray<SpatialNeighborData> neighbors,
            int myTeam,
            float threatThreshold,
            out TacticalGridData result)
        {
            int totalCells = resolution * resolution;
            float maxDensity = 0f;
            float totalDensity = 0f;
            int dangerousCells = 0;
            int2 maxCell = int2.zero;
            int2 minCell = new int2(resolution / 2, resolution / 2); // start at center
            float minDensity = float.MaxValue;

            // Count enemies per cell using a flat array approach
            // We iterate neighbors and accumulate per cell
            var cellEnemyCounts = new NativeArray<int>(totalCells, Allocator.Temp);
            var cellNeighborCounts = new NativeArray<int>(totalCells, Allocator.Temp);

            try
            {
                var gridSize = new int2(resolution, resolution);

                for (int i = 0; i < neighbors.Length; i++)
                {
                    var neighbor = neighbors[i];
                    bool isEnemy = myTeam != 0 && neighbor.TeamId != 0 && myTeam != neighbor.TeamId;
                    if (!isEnemy) continue;

                    // Reconstruct neighbor world position
                    var neighborPos = new float2(0f); // We don't have absolute position, approximate from grid
                    // Use direction + distance relative to agent (agent is at grid center)
                    // The gridOrigin is agent position - gridRadius, so agent is at grid center
                    // neighborPos relative to gridOrigin:
                    // agent is at gridOrigin + gridRadius
                    // neighbor is at agentPos + direction * distance
                    // So relative to gridOrigin: gridOrigin + gridRadius + dir*dist - gridOrigin = gridRadius + dir*dist
                    // Actually we need to reconstruct from Direction and Distance
                    // gridOrigin is agentPos - gridRadius (float)
                    float gridRadius = gridOrigin.x >= 0 ? gridOrigin.x : -gridOrigin.x; // not used, we just need cell
                    // Simpler: gridOrigin + cellSize*gridSize/2 = agent pos
                    // neighbor relative to agent = Direction * Distance
                    // neighbor world pos = agentPos + Direction * Distance
                    // relative to gridOrigin = Direction * Distance + (gridOrigin + gridRadius) - gridOrigin
                    //                        = Direction * Distance + gridRadius
                    // Wait, gridOrigin is already calculated by the system. We just need:
                    // neighborRelative = Direction * Distance
                    // neighborInGrid = neighborRelative + gridRadius (shift so agent=gridRadius)
                    // cell = floor(neighborInGrid / cellSize)

                    // For this pure math function, compute from direction + distance
                    // relative to grid origin: the neighbor position is at (Direction * Distance)
                    // offset from agent. Agent is at center of grid = gridOrigin + gridRadius
                    // So neighbor world pos = (gridOrigin + some_center) + Direction * Distance
                    // relative to gridOrigin = some_center + Direction * Distance
                    // where some_center = cellSize * resolution * 0.5f as float2

                    // Actually, let's simplify. The caller passes gridOrigin so the agent
                    // is at the center. neighbor offset from agent = Direction * Distance.
                    // So neighbor relative to gridOrigin = Direction * Distance + center offset
                    var centerOffset = cellSize * resolution * 0.5f;
                    var neighborRelative = neighbor.Direction * neighbor.Distance + centerOffset;
                    var cell = new int2(
                        (int)math.floor(neighborRelative.x / cellSize.x),
                        (int)math.floor(neighborRelative.y / cellSize.y));

                    // Clamp to grid
                    cell.x = math.clamp(cell.x, 0, resolution - 1);
                    cell.y = math.clamp(cell.y, 0, resolution - 1);

                    int cellIdx = cell.y * resolution + cell.x;
                    cellEnemyCounts[cellIdx]++;
                }

                // Analyze each cell
                float cellArea = cellSize.x * cellSize.y;
                for (int cy = 0; cy < resolution; cy++)
                {
                    for (int cx = 0; cx < resolution; cx++)
                    {
                        int idx = cy * resolution + cx;
                        float density = cellEnemyCounts[idx] / cellArea;
                        totalDensity += density;

                        if (density > maxDensity)
                        {
                            maxDensity = density;
                            maxCell = new int2(cx, cy);
                        }

                        if (density < minDensity)
                        {
                            minDensity = density;
                            minCell = new int2(cx, cy);
                        }

                        if (density > threatThreshold)
                            dangerousCells++;
                    }
                }

                // Compute directions from agent (center of grid) to min/max cells
                var centerCell = new float2(resolution * 0.5f, resolution * 0.5f);
                var dirToMax = new float2(maxCell.x + 0.5f - centerCell.x, maxCell.y + 0.5f - centerCell.y);
                var dirToMin = new float2(minCell.x + 0.5f - centerCell.x, minCell.y + 0.5f - centerCell.y);

                result.DangerDirection = math.normalizesafe(dirToMax);
                result.SafestDirection = math.normalizesafe(dirToMin);
                result.MaxThreatDensity = maxDensity;
                result.AverageThreatDensity = totalDensity / totalCells;
                result.DangerousCellCount = dangerousCells;
                result.FlankingDirection = ComputeFlankingDirection(result.DangerDirection);
            }
            finally
            {
                cellEnemyCounts.Dispose();
                cellNeighborCounts.Dispose();
            }
        }
    }
}
