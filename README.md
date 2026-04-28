# BovineLabs Combat Movement

S-tier combat movement system for Unity DOTS/ECS. Timeline-native architecture with structural motion lanes, physics-authoritative motor, shared sensor pipeline, and combat brain with desire scoring.

## Architecture

```
Stats / Essence / Intrinsics
        |
Spatial / Sensors / Target Memory
        |
Reaction / Combat Brain
        |
Timeline Directors
        |
Timeline Motion Lanes (Forced > Attack > Locomotion > Navigation > Idle + Avoidance)
        |
Thin Lane Gate -> ResolvedMotion + ResolvedFacing
        |
Physics Motor (ONLY writer to PhysicsVelocity)
        |
Unity.Physics
```

## Core Rules

1. **Only CombatPhysicsMotorSystem writes PhysicsVelocity** -- no other system touches physics
2. **Structural lane authority** -- not priority numbers. Forced > Attack > Locomotion > Navigation > Idle
3. **Forced motion bypasses desire but goes through physics motor**
4. **Facing is separate from movement** -- completely independent lanes
5. **Shared SensedTarget buffer** -- no per-behavior enemy scanning
6. **Timeline blending inside lanes** -- not flat float3 additive
7. **CombatMotionMixer clamps additive velocity** -- fixes double-speed bug from additive blending

## Assemblies

| Assembly | Description |
|----------|-------------|
| BovineLabs.Combat.Core | Core types: CombatMotionData, lane components, forced motion, sensors, brain, groups |
| BovineLabs.Combat.Systems | 8 systems: motion resolve, physics motor, facing, forced motion, sensors, brain |
| BovineLabs.Combat.Timeline | 15 clip types for Timeline integration |
| BovineLabs.Combat.Steering | 6 steering behaviors: Seek, Flee, Arrive, Pursue, Evade, Wander |
| BovineLabs.Combat.Navigation | NavMesh bridge (Recast), PathFollow |
| BovineLabs.Combat.Avoidance | RVO-like velocity obstacles |
| + 19 more legacy modules | Charge, Flank, Retreat, Kite, Surround, Guard, Ambush, Formation, etc. |

## Usage

### Quick Start: Combat Agent Setup

```csharp
// An entity needs these components to participate in v2 combat:
entityManager.AddComponent<CombatAgent>(entity);
entityManager.AddComponentData(entity, new CombatAgentProfile { DefaultMaxSpeed = 5f });
entityManager.AddComponent<ResolvedMotion>(entity);
entityManager.AddComponent<ResolvedFacing>(entity);
entityManager.AddComponent<ForcedMotionState>(entity);
entityManager.AddComponent<CombatControlMask>(entity);
entityManager.AddComponentData(entity, CombatBrainProfile.Default);
entityManager.AddComponent<TargetMemory>(entity);
entityManager.AddComponent<CombatTargets>(entity);
entityManager.AddBuffer<TargetSlot>(entity);
entityManager.AddBuffer<SensedTarget>(entity);
entityManager.AddBuffer<CombatDesire>(entity);

// Lane components (populated by Timeline):
entityManager.AddComponent<AttackMotionAnimated>(entity);
entityManager.AddComponent<LocomotionAnimated>(entity);
entityManager.AddComponent<NavigationAnimated>(entity);
entityManager.AddComponent<AvoidanceAnimated>(entity);
entityManager.AddComponent<FacingAnimated>(entity);
```

### Custom Behavior Module

To create a behavior that feeds into the v2 lane system:

```csharp
[BurstCompile]
[UpdateInGroup(typeof(CombatPrePhysicsGroup))]
public partial struct MyCustomFleeSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (locomotion, target, transform) in
            SystemAPI.Query<RefRW<LocomotionAnimated>, RefRO<CombatTarget>, RefRO<LocalTransform>>())
        {
            if (target.ValueRO.HasTarget)
            {
                var targetPos = target.ValueRO.LastKnownPosition;
                var myPos = new float2(transform.ValueRO.Position.x, transform.ValueRO.Position.y);
                var dir = myPos - targetPos;
                var fleeDir = math.normalizesafe(dir);

                locomotion.ValueRW.Value = new CombatMotionData
                {
                    Mode = CombatMotionMode.DesiredVelocity,
                    DesiredVelocity = new float3(fleeDir.x, 0f, fleeDir.y) * 8f,
                    SpeedScale = 1.35f,
                    AccelerationScale = 1.2f,
                    BrakeScale = 1f,
                    Flags = CombatMotionFlags.UseCurrentTarget,
                };
            }
            else
            {
                // MUST reset to None when not active!
                locomotion.ValueRW.Value = CombatMotionData.None;
            }
        }
    }
}
```

### Knockback (Forced Motion)

```csharp
var requests = entityManager.GetBuffer<ForcedMotionRequest>(entity);
requests.Add(new ForcedMotionRequest
{
    Mode = ForcedMotionMode.VelocityOverride,
    Vector = new float3(-5f, 2f, 0f), // knockback + up
    Duration = 0.3f,
    Damping = 5f,
    Strength = 1f,
    Flags = ForcedMotionFlags.ZeroCurrentVelocity | ForcedMotionFlags.SuppressAttackMotion,
});
```

## Pipeline Order

```
CombatPrePhysicsGroup (FixedStepSimulationSystemGroup)
  -> CombatSpatialGroup: SensorQuerySystem, TargetPatchSystem
  -> CombatBrainGroup: CombatDesireScoringSystem, CombatReactionBridgeSystem

CombatMotionResolveGroup (FixedStepSimulationSystemGroup, before PhysicsSystemGroup)
  -> ForcedMotionResolveSystem: processes knockback/stun requests
  -> CombatMotionResolveSystem: forced > attack > locomotion > navigation > idle + avoidance
  -> CombatPhysicsMotorSystem: writes PhysicsVelocity
  -> FacingResolveSystem: writes ResolvedFacing

PhysicsSystemGroup (Unity.Physics)
  -> Physics steps using our PhysicsVelocity

CombatAfterPhysicsGroup
  -> Hit detection, collision events, damage

CombatCleanupGroup
  -> Clear temporary buffers
```

## Dependencies

- Unity 6000.0+ (Entities 6.x, Physics)
- BovineLabs.Core (Spatial, Essence, Reaction, Timeline)
- BovineLabs.Recast (Navigation)
