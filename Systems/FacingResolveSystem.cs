using BovineLabs.Combat.Core;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

namespace BovineLabs.Combat.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(CombatMotionResolveGroup))]
    public partial struct FacingResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (facingAnimated, resolvedFacing, controlMask, resolvedMotion) in
                SystemAPI.Query<
                    RefRO<FacingAnimated>,
                    RefRW<ResolvedFacing>,
                    RefRO<CombatControlMask>,
                    RefRO<ResolvedMotion>>())
            {
                if (controlMask.ValueRO.DisableTurn)
                    continue;

                var data = facingAnimated.ValueRO.Value;

                switch (data.Mode)
                {
                    case FacingMode.None:
                        resolvedFacing.ValueRW = ResolvedFacing.None;
                        break;

                    case FacingMode.FaceMovement:
                        var velocity = resolvedMotion.ValueRO.Motion.DesiredVelocity;
                        if (math.lengthsq(velocity) > 0.0001f)
                        {
                            data.Direction = math.normalize(velocity);
                        }
                        resolvedFacing.ValueRW = new ResolvedFacing { Value = data };
                        break;

                    default:
                        resolvedFacing.ValueRW = new ResolvedFacing { Value = data };
                        break;
                }
            }
        }
    }
}
