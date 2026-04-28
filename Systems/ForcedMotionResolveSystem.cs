using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using BovineLabs.Combat.Core;

namespace BovineLabs.Combat.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(CombatMotionResolveGroup))]
    [UpdateBefore(typeof(CombatMotionResolveSystem))]
    public partial struct ForcedMotionResolveSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;

            foreach (var (requests, motionState, controlMask) in
                SystemAPI.Query<
                    DynamicBuffer<ForcedMotionRequest>,
                    RefRW<ForcedMotionState>,
                    RefRW<CombatControlMask>>())
            {
                // Step 1: Decrement active state timer, expire if needed
                if (motionState.ValueRO.IsActive)
                {
                    motionState.ValueRW.RemainingTime -= dt;
                    if (motionState.ValueRW.RemainingTime <= 0f)
                    {
                        motionState.ValueRW = ForcedMotionState.Inactive;
                        controlMask.ValueRW.Value = 0;
                    }
                }

                // Step 2: Process new requests - find highest priority (latest non-Freeze wins)
                ForcedMotionRequest? bestRequest = null;
                for (int i = 0; i < requests.Length; i++)
                {
                    var req = requests[i];
                    if (req.Mode != ForcedMotionMode.Freeze || bestRequest == null)
                    {
                        bestRequest = req;
                    }
                }

                requests.Clear();

                // Step 3: Apply the winning request
                if (bestRequest.HasValue)
                {
                    var req = bestRequest.Value;

                    motionState.ValueRW = new ForcedMotionState
                    {
                        ActiveMode = req.Mode,
                        ActiveVector = req.Vector,
                        RemainingTime = req.Duration,
                        Damping = req.Damping,
                        ActiveFlags = req.Flags,
                    };

                    // Map ForcedMotionFlags to CombatControlMask bits
                    byte mask = 0;
                    if ((req.Flags & ForcedMotionFlags.DisableInput) != 0)
                        mask |= 1;
                    if ((req.Flags & ForcedMotionFlags.DisableBrain) != 0)
                        mask |= 2;
                    if ((req.Flags & ForcedMotionFlags.LockFacing) != 0)
                        mask |= 4;
                    if ((req.Flags & ForcedMotionFlags.SuppressNavigation) != 0)
                        mask |= 8;
                    controlMask.ValueRW.Value = mask;
                }

                // Step 4: If active and Freeze mode, ensure resolved motion stops
                // (The actual motion stop is handled downstream by consumers checking ActiveMode == Freeze)
            }
        }
    }
}
