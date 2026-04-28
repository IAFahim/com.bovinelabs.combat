using Unity.Entities;

namespace BovineLabs.Combat.Core
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatPrePhysicsGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(CombatPrePhysicsGroup))]
    public partial class CombatSpatialGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(CombatPrePhysicsGroup))]
    [UpdateAfter(typeof(CombatSpatialGroup))]
    public partial class CombatBrainGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatMotionResolveGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatAfterPhysicsGroup : ComponentSystemGroup
    {
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatCleanupGroup : ComponentSystemGroup
    {
    }
}
