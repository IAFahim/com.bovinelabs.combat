using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.Formation
{
    /// <summary>
    /// Pure math utility functions for computing formation positions.
    /// All operations on the XZ plane (float2). Static class, Burst-friendly.
    /// </summary>
    public static unsafe class FormationMath
    {
        /// <summary>
        /// Compute line formation positions behind the leader.
        /// Units are placed perpendicular to leaderForward at spacing intervals.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public static NativeArray<float2> ComputeLinePositions(
            float2 leaderPos,
            float2 leaderForward,
            float spacing,
            int count,
            Allocator allocator)
        {
            var positions = new NativeArray<float2>(count, allocator);
            if (count == 0)
                return positions;

            var right = new float2(-leaderForward.y, leaderForward.x);

            for (int i = 0; i < count; i++)
            {
                // Alternate left/right: 0 -> +1, 1 -> -1, 2 -> +2, 3 -> -2, ...
                var side = ((i + 1) / 2) * ((i % 2 == 0) ? 1f : -1f);
                positions[i] = leaderPos + right * side * spacing;
            }

            return positions;
        }

        /// <summary>
        /// Compute wedge formation positions. A V-shape expanding behind the leader.
        /// angle: half-angle of the wedge in radians.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public static NativeArray<float2> ComputeWedgePositions(
            float2 leaderPos,
            float2 leaderForward,
            float spacing,
            float angle,
            int count,
            Allocator allocator)
        {
            var positions = new NativeArray<float2>(count, allocator);
            if (count == 0)
                return positions;

            var backward = -leaderForward;
            var right = new float2(-leaderForward.y, leaderForward.x);

            var leftDir = math.normalize(backward + right * math.sin(angle));
            var rightDir = math.normalize(backward - right * math.sin(angle));

            for (int i = 0; i < count; i++)
            {
                var side = (i % 2 == 0) ? 1 : -1;
                var rank = (i + 1) / 2;
                var dir = side >= 0 ? rightDir : leftDir;
                positions[i] = leaderPos + dir * (rank * spacing);
            }

            return positions;
        }

        /// <summary>
        /// Compute grid formation positions. Rows and columns behind the leader.
        /// columns: number of units per row.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public static NativeArray<float2> ComputeGridPositions(
            float2 leaderPos,
            float2 leaderForward,
            float spacing,
            int columns,
            int count,
            Allocator allocator)
        {
            var positions = new NativeArray<float2>(count, allocator);
            if (count == 0)
                return positions;

            var backward = -leaderForward;
            var right = new float2(-leaderForward.y, leaderForward.x);
            var cols = math.max(1, columns);

            for (int i = 0; i < count; i++)
            {
                var row = i / cols;
                var col = i % cols;

                // Center the row: col offset from center
                var colOffset = col - (cols - 1) * 0.5f;
                var rowOffset = row + 1; // First row behind leader

                positions[i] = leaderPos
                    + backward * (rowOffset * spacing)
                    + right * (colOffset * spacing);
            }

            return positions;
        }

        /// <summary>
        /// Compute circle formation positions. Units evenly distributed around a circle.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public static NativeArray<float2> ComputeCirclePositions(
            float2 centerPos,
            float radius,
            int count,
            Allocator allocator)
        {
            var positions = new NativeArray<float2>(count, allocator);
            if (count == 0)
                return positions;

            for (int i = 0; i < count; i++)
            {
                var angle = (2f * math.PI * i) / count;
                var offset = new float2(math.cos(angle), math.sin(angle)) * radius;
                positions[i] = centerPos + offset;
            }

            return positions;
        }

        /// <summary>
        /// Compute column formation positions. Single file behind the leader.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public static NativeArray<float2> ComputeColumnPositions(
            float2 leaderPos,
            float2 leaderForward,
            float spacing,
            int count,
            Allocator allocator)
        {
            var positions = new NativeArray<float2>(count, allocator);
            if (count == 0)
                return positions;

            var backward = -leaderForward;

            for (int i = 0; i < count; i++)
            {
                positions[i] = leaderPos + backward * ((i + 1) * spacing);
            }

            return positions;
        }

        /// <summary>
        /// Compute V formation positions. Similar to wedge but with a tighter angle.
        /// angle: half-angle of the V in radians.
        /// </summary>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        public static NativeArray<float2> ComputeVPositions(
            float2 leaderPos,
            float2 leaderForward,
            float spacing,
            float angle,
            int count,
            Allocator allocator)
        {
            var positions = new NativeArray<float2>(count, allocator);
            if (count == 0)
                return positions;

            var backward = -leaderForward;
            var right = new float2(-leaderForward.y, leaderForward.x);

            for (int i = 0; i < count; i++)
            {
                var side = (i % 2 == 0) ? 1f : -1f;
                var rank = (i + 1) / 2;

                // V has a steeper angle than wedge
                var backAmount = rank * spacing * math.cos(angle);
                var sideAmount = rank * spacing * math.sin(angle) * side;

                positions[i] = leaderPos + backward * backAmount + right * sideAmount;
            }

            return positions;
        }
    }
}
