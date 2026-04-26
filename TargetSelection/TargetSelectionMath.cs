using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.TargetSelection
{
    /// <summary>
    /// Pure math utility functions for target selection.
    /// All methods are Burst-friendly, static, and allocation-free
    /// (except PlanRoomRoute which returns a NativeList).
    /// </summary>
    public static unsafe class TargetSelectionMath
    {
        /// <summary>
        /// Select the nearest candidate within maxRange.
        /// </summary>
        /// <param name="agentPos">Agent's XZ position.</param>
        /// <param name="candidatePositions">Positions of all candidates.</param>
        /// <param name="maxRange">Maximum selection range.</param>
        /// <param name="count">Number of valid entries in candidatePositions.</param>
        /// <returns>Index of nearest candidate, or -1 if none within range.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SelectNearest(float2 agentPos, NativeArray<float2> candidatePositions, float maxRange, int count)
        {
            var bestIndex = -1;
            var bestDistSq = maxRange * maxRange;

            for (int i = 0; i < count; i++)
            {
                var distSq = math.lengthsq(candidatePositions[i] - agentPos);
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Select the weakest (lowest current HP) candidate within maxRange.
        /// </summary>
        /// <param name="agentPos">Agent's XZ position.</param>
        /// <param name="candidatePositions">Positions of all candidates.</param>
        /// <param name="healths">Current health values of candidates.</param>
        /// <param name="maxRange">Maximum selection range.</param>
        /// <param name="count">Number of valid entries.</param>
        /// <returns>Index of weakest candidate, or -1 if none within range.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SelectWeakest(float2 agentPos, NativeArray<float2> candidatePositions, NativeArray<float> healths, float maxRange, int count)
        {
            var bestIndex = -1;
            var bestHealth = float.MaxValue;
            var rangeSq = maxRange * maxRange;

            for (int i = 0; i < count; i++)
            {
                var distSq = math.lengthsq(candidatePositions[i] - agentPos);
                if (distSq <= rangeSq && healths[i] < bestHealth)
                {
                    bestHealth = healths[i];
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Select the most threatening candidate within maxRange based on threat scores.
        /// </summary>
        /// <param name="agentPos">Agent's XZ position.</param>
        /// <param name="candidatePositions">Positions of all candidates.</param>
        /// <param name="threatScores">Threat scores of candidates.</param>
        /// <param name="maxRange">Maximum selection range.</param>
        /// <param name="count">Number of valid entries.</param>
        /// <returns>Index of most threatening candidate, or -1 if none within range.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SelectMostThreatening(float2 agentPos, NativeArray<float2> candidatePositions, NativeArray<float> threatScores, float maxRange, int count)
        {
            var bestIndex = -1;
            var bestThreat = float.MinValue;
            var rangeSq = maxRange * maxRange;

            for (int i = 0; i < count; i++)
            {
                var distSq = math.lengthsq(candidatePositions[i] - agentPos);
                if (distSq <= rangeSq && threatScores[i] > bestThreat)
                {
                    bestThreat = threatScores[i];
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Check if a target position falls within a forward-facing cone from the agent.
        /// </summary>
        /// <param name="agentPos">Agent's XZ position.</param>
        /// <param name="agentForward">Agent's normalized forward direction on XZ plane.</param>
        /// <param name="targetPos">Target's XZ position.</param>
        /// <param name="maxAngle">Half-angle of the cone in radians. PI = no restriction.</param>
        /// <returns>True if target is within the cone.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInCone(float2 agentPos, float2 agentForward, float2 targetPos, float maxAngle)
        {
            var toTarget = targetPos - agentPos;
            var dist = math.length(toTarget);
            if (dist < 0.0001f)
                return true;

            var dir = toTarget / dist;
            var dot = math.dot(agentForward, dir);
            var angle = math.acos(math.clamp(dot, -1f, 1f));
            return angle <= maxAngle;
        }
    }
}
