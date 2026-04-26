using Unity.Entities;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Update group for all steering behavior systems.
    /// Systems in this group run in parallel - each writes to its own SteeringForce component.
    /// The Blend module (separate asmdef) combines forces after this group.
    /// Defined in Core so all modules can reference it without cross-module dependencies.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatSteeringGroup : ComponentSystemGroup
    {
    }
}
