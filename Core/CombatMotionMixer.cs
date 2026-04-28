using BovineLabs.Timeline;
using Unity.Mathematics;

namespace BovineLabs.Combat.Core
{
    public readonly struct CombatMotionMixer : IMixer<CombatMotionData>
    {
        public CombatMotionData Lerp(in CombatMotionData a, in CombatMotionData b, in float s)
        {
            return new CombatMotionData
            {
                Mode = s < 0.5f ? a.Mode : b.Mode,
                DesiredVelocity = math.lerp(a.DesiredVelocity, b.DesiredVelocity, s),
                DesiredDirection = math.lerp(a.DesiredDirection, b.DesiredDirection, s),
                TargetPosition = math.lerp(a.TargetPosition, b.TargetPosition, s),
                SpeedScale = math.lerp(a.SpeedScale, b.SpeedScale, s),
                AccelerationScale = math.lerp(a.AccelerationScale, b.AccelerationScale, s),
                BrakeScale = math.lerp(a.BrakeScale, b.BrakeScale, s),
                ArrivalRadius = math.lerp(a.ArrivalRadius, b.ArrivalRadius, s),
                MaintainDistance = math.lerp(a.MaintainDistance, b.MaintainDistance, s),
                MaxContribution = math.lerp(a.MaxContribution, b.MaxContribution, s),
                Flags = s < 0.5f ? a.Flags : b.Flags,
            };
        }

        public CombatMotionData Add(in CombatMotionData a, in CombatMotionData b)
        {
            var combined = a.DesiredVelocity + b.DesiredVelocity;
            var maxC = math.max(a.MaxContribution, b.MaxContribution);
            var lengthSq = math.lengthsq(combined);

            if (maxC > 0f && lengthSq > maxC * maxC)
            {
                combined = math.normalize(combined) * maxC;
            }

            return new CombatMotionData
            {
                Mode = a.Mode,
                DesiredVelocity = combined,
                DesiredDirection = a.DesiredDirection,
                TargetPosition = a.TargetPosition,
                SpeedScale = math.max(a.SpeedScale, b.SpeedScale),
                AccelerationScale = math.max(a.AccelerationScale, b.AccelerationScale),
                BrakeScale = math.max(a.BrakeScale, b.BrakeScale),
                ArrivalRadius = a.ArrivalRadius,
                MaintainDistance = a.MaintainDistance,
                MaxContribution = maxC,
                Flags = a.Flags,
            };
        }
    }
}
