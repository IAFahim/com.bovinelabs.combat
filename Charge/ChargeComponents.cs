using Unity.Mathematics;

namespace BovineLabs.Combat.Charge
{
    /// <summary>
    /// Charge behavior component: move at high speed in a straight line toward a target.
    /// When isCharging is true, the agent moves at chargeSpeed * MaxSpeed toward targetPos.
    /// The charge is only valid when the target is beyond minChargeDistance.
    /// </summary>
    public struct ChargeTarget : IComponentData, IEnableableComponent
    {
        /// <summary>Position to charge toward on the XZ plane.</summary>
        public float2 TargetPos;

        /// <summary>Speed multiplier applied to MaxSpeed during charge (e.g., 2.0 = double speed).</summary>
        public float ChargeSpeedMultiplier;

        /// <summary>Minimum distance required to start/continue a charge.</summary>
        public float MinChargeDistance;

        /// <summary>Whether the agent is currently charging.</summary>
        public bool IsCharging;

        public static ChargeTarget Default => new()
        {
            TargetPos = float2.zero,
            ChargeSpeedMultiplier = 2f,
            MinChargeDistance = 5f,
            IsCharging = false,
        };
    }
}
