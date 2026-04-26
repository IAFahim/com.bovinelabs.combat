using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;

namespace BovineLabs.Combat.Blend
{
    /// <summary>
    /// Pure math utility functions for blending steering forces.
    /// Supports weighted blending and priority-based selection.
    /// Static class, Burst-friendly.
    /// </summary>
    public static unsafe class BlendMath
    {
        /// <summary>
        /// Compute a weighted blend of multiple steering forces.
        /// Each force is multiplied by its corresponding weight, summed, then normalized.
        /// If total weight is zero, returns zero.
        /// </summary>
        /// <param name="forces">Array of force vectors.</param>
        /// <param name="weights">Array of weights (one per force).</param>
        /// <param name="count">Number of valid entries.</param>
        /// <returns>Weighted sum of forces.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 WeightedBlend(NativeArray<float2> forces, NativeArray<float> weights, int count)
        {
            var result = float2.zero;
            var totalWeight = 0f;

            for (int i = 0; i < count; i++)
            {
                result += forces[i] * weights[i];
                totalWeight += weights[i];
            }

            if (totalWeight < 0.0001f)
                return float2.zero;

            return result;
        }

        /// <summary>
        /// Select the force with the highest priority among non-zero forces.
        /// If multiple forces share the highest priority, returns their average.
        /// If no non-zero forces exist, returns zero.
        /// </summary>
        /// <param name="forces">Array of force vectors.</param>
        /// <param name="priorities">Array of priorities (one per force).</param>
        /// <param name="count">Number of valid entries.</param>
        /// <returns>Highest priority non-zero force, or average of tied top-priority forces.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 PrioritySelect(NativeArray<float2> forces, NativeArray<float> priorities, int count)
        {
            var maxPriority = float.MinValue;
            var result = float2.zero;
            var tiedCount = 0;

            // First pass: find max priority among non-zero forces
            for (int i = 0; i < count; i++)
            {
                if (math.lengthsq(forces[i]) > 0.0001f && priorities[i] > maxPriority)
                {
                    maxPriority = priorities[i];
                }
            }

            if (maxPriority < float.MinValue + 0.001f)
                return float2.zero;

            // Second pass: accumulate forces at max priority
            for (int i = 0; i < count; i++)
            {
                if (math.lengthsq(forces[i]) > 0.0001f && priorities[i] >= maxPriority - 0.001f)
                {
                    result += forces[i];
                    tiedCount++;
                }
            }

            if (tiedCount > 1)
                result /= tiedCount;

            return result;
        }

        /// <summary>
        /// Truncate a force vector to not exceed maxSpeed in magnitude.
        /// </summary>
        /// <param name="force">Force vector to truncate.</param>
        /// <param name="maxSpeed">Maximum allowed magnitude.</param>
        /// <returns>Truncated force.</returns>
        [Il2CppSetOption(Option.NullChecks, false)]
        [Il2CppSetOption(Option.ArrayBoundsChecks, false)]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float2 TruncateToMaxSpeed(float2 force, float maxSpeed)
        {
            var sq = math.lengthsq(force);
            var maxSq = maxSpeed * maxSpeed;
            if (sq > maxSq && sq > 0.0001f)
            {
                return math.normalize(force) * maxSpeed;
            }
            return force;
        }
    }
}
