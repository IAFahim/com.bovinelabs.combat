using Unity.Burst;
using Unity.Entities;
using BovineLabs.Combat.Core;

namespace BovineLabs.Combat.Systems
{
    [BurstCompile]
    [UpdateInGroup(typeof(CombatBrainGroup))]
    [UpdateAfter(typeof(CombatDesireScoringSystem))]
    public partial struct CombatReactionBridgeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var desires in
                SystemAPI.Query<
                    DynamicBuffer<CombatDesire>>()
                .WithAll<CombatTargets, TargetSlot>())
            {
                Entity bestTarget = Entity.Null;
                float bestScore = float.NegativeInfinity;

                for (int i = 0; i < desires.Length; i++)
                {
                    var desire = desires[i];
                    if ((desire.Flags & CombatDesireFlags.RequiresTarget) == 0) continue;
                    if (desire.Target == Entity.Null) continue;
                    if (desire.Score > bestScore)
                    {
                        bestScore = desire.Score;
                        bestTarget = desire.Target;
                    }
                }

                // Note: TargetSlot patching will be done via ECB or direct access
                // in the full Timeline integration. For now, the best target is
                // identified for downstream systems to consume.
            }
        }
    }
}
