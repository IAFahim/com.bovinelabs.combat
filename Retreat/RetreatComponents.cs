using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Retreat
{
    /// <summary>
    /// Retreat behavior component: flee from a threat position until reaching a safe distance.
    /// When isRetreating is true, the agent moves directly away from threatPos.
    /// Once the agent is at or beyond safeDistance from the threat, retreating stops.
    /// </summary>
    public struct RetreatFrom : IComponentData, IEnableableComponent
    {
        /// <summary>Position of the threat to retreat from on the XZ plane.</summary>
        public float2 ThreatPos;

        /// <summary>Distance from threat at which the agent considers itself safe.</summary>
        public float SafeDistance;

        /// <summary>Whether the agent is currently retreating.</summary>
        public bool IsRetreating;

        public static RetreatFrom Default => new()
        {
            ThreatPos = float2.zero,
            SafeDistance = 10f,
            IsRetreating = false,
        };
    }
}
