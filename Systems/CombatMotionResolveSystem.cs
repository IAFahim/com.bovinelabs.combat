using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Systems
{
    /// <summary>
    /// Lane gate resolver that picks the active motion lane and writes ResolvedMotion.
    ///
    /// Priority order (first active wins):
    ///   1. AttackMotionAnimated  -> Source = Attack
    ///   2. LocomotionAnimated    -> Source = Locomotion
    ///   3. NavigationAnimated    -> Source = Navigation
    ///   4. None active           -> Mode = Stop, Source = Idle
    ///
    /// After resolving the primary lane, avoidance is applied additively
    /// (unless the resolved motion has the IgnoreAvoidance flag).
    /// The avoidance contribution is clamped by its MaxContribution field.
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatMotionResolveGroup))]
    public partial struct CombatMotionResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (attack, locomotion, navigation, avoidance, resolved) in
                SystemAPI.Query<
                    RefRO<AttackMotionAnimated>,
                    RefRO<LocomotionAnimated>,
                    RefRO<NavigationAnimated>,
                    RefRO<AvoidanceAnimated>,
                    RefRW<ResolvedMotion>>())
            {
                var attackData = attack.ValueRO.Value;
                var locomotionData = locomotion.ValueRO.Value;
                var navigationData = navigation.ValueRO.Value;
                var avoidanceData = avoidance.ValueRO.Value;

                // Step 1: Resolve primary lane by priority
                CombatMotionData motion;
                CombatMotionSource source;

                if (attackData.Mode != CombatMotionMode.None)
                {
                    motion = attackData;
                    source = CombatMotionSource.Attack;
                }
                else if (locomotionData.Mode != CombatMotionMode.None)
                {
                    motion = locomotionData;
                    source = CombatMotionSource.Locomotion;
                }
                else if (navigationData.Mode != CombatMotionMode.None)
                {
                    motion = navigationData;
                    source = CombatMotionSource.Navigation;
                }
                else
                {
                    motion = new CombatMotionData
                    {
                        Mode = CombatMotionMode.Stop,
                        SpeedScale = 1f,
                        AccelerationScale = 1f,
                        BrakeScale = 1f,
                    };
                    source = CombatMotionSource.Idle;
                }

                // Step 2: Additive avoidance blending
                if (avoidanceData.Mode != CombatMotionMode.None &&
                    (motion.Flags & CombatMotionFlags.IgnoreAvoidance) == 0)
                {
                    var combined = motion.DesiredVelocity + avoidanceData.DesiredVelocity;
                    var maxC = avoidanceData.MaxContribution;

                    if (maxC > 0f)
                    {
                        var lengthSq = math.lengthsq(combined);
                        if (lengthSq > maxC * maxC)
                        {
                            combined = math.normalize(combined) * maxC;
                        }
                    }

                    motion.DesiredVelocity = combined;
                }

                // Step 3: Write resolved output
                resolved.ValueRW.Motion = motion;
                resolved.ValueRW.Source = source;
            }
        }
    }
}
