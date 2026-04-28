using Unity.Burst;
using Unity.Entities;
using BovineLabs.Combat.Core;

namespace BovineLabs.Combat.Systems
{
    /// <summary>
    /// Bridges the top-scored CombatDesire to the TargetSlot system.
    /// Finds the highest-score desire that requires a target and writes
    /// it to the primary TargetSlot (SlotId 0).
    ///
    /// Future integration: will trigger Timeline directors based on desire type
    /// (e.g. Attack -> attack timeline, Flee -> flee timeline).
    /// </summary>
    [BurstCompile]
    [UpdateInGroup(typeof(CombatBrainGroup))]
    [UpdateAfter(typeof(CombatDesireScoringSystem))]
    public partial struct CombatReactionBridgeSystem : ISystem
    {
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var (desires, entity) in
                SystemAPI.Query<
                    DynamicBuffer<CombatDesire>>()
                .WithAll<CombatTargets>()
                .WithEntityAccess())
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

                // Write to primary slot (SlotId 0)
                if (bestTarget != Entity.Null)
                {
                    var targetSlots = SystemAPI.GetBuffer<TargetSlot>(entity);
                    if (targetSlots.Length > 0)
                    {
                        targetSlots[0] = new TargetSlot { Entity = bestTarget, SlotId = 0 };
                    }
                }
            }
        }
    }
}
