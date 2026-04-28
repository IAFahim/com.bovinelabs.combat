using Unity.Entities;

namespace BovineLabs.Combat.Core
{
    /// <summary>
    /// Runs sensors and brain before motion resolution.
    /// In FixedStepSimulationSystemGroup, before PhysicsSystemGroup.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatPrePhysicsGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Sensor queries run first in the combat pipeline.
    /// </summary>
    [UpdateInGroup(typeof(CombatPrePhysicsGroup))]
    public partial class CombatSpatialGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Brain scoring runs after sensors are populated.
    /// </summary>
    [UpdateInGroup(typeof(CombatPrePhysicsGroup))]
    [UpdateAfter(typeof(CombatSpatialGroup))]
    public partial class CombatBrainGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Motion resolution + physics motor. Must run before Unity PhysicsSystemGroup
    /// so that PhysicsVelocity is written before the physics step.
    /// Contains: ForcedMotionResolve -> CombatMotionResolve -> CombatPhysicsMotor
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatMotionResolveGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Post-physics events: hit detection, collision, damage.
    /// Runs after PhysicsSystemGroup.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatAfterPhysicsGroup : ComponentSystemGroup
    {
    }

    /// <summary>
    /// Cleanup temporary buffers at end of frame.
    /// </summary>
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class CombatCleanupGroup : ComponentSystemGroup
    {
    }
}
