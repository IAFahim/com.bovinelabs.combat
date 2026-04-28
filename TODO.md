# TODO.md - Combat v2: Timeline-Native Architecture

## Status: COMPILED CLEAN

### What's Built
- **0 compilation errors** on Unity 6000.5.0b1 (batch mode verified)
- **43 new C# files** across 3 new assemblies
- **45 total combat assemblies** compiled (22 old modules + 3 new v2 assemblies + 20 test assemblies)
- Old modules remain fully functional alongside new v2 architecture

### New Assemblies

| Assembly | Files | Purpose |
|----------|-------|---------|
| BovineLabs.Combat.Systems | 7 systems | Motion resolve, physics motor, facing, forced motion, sensors, brain |
| BovineLabs.Combat.Timeline | 15 clips | Motion/Facing/Targeting/Lock/Event timeline clips |
| BovineLabs.Combat.V2.Tests | 12 tests | Pure math tests for v2 types |

### Architecture

```
Stats/Essence -> Spatial/Sensors -> Brain/Reaction -> Timeline Directors
    -> Motion Lanes (Forced > Attack > Locomotion > Navigation > Idle)
    -> Lane Gate -> ResolvedMotion + ResolvedFacing
    -> Physics Motor (ONLY PhysicsVelocity writer)
    -> Unity.Physics
```

### Core Types (in BovineLabs.Combat.Core)
- CombatMotionData + CombatMotionMode + CombatMotionFlags
- CombatMotionMixer (IMixer<CombatMotionData>)
- AttackMotionAnimated, LocomotionAnimated, NavigationAnimated, AvoidanceAnimated
- ResolvedMotion, ResolvedFacing
- FacingData + FacingMode + FacingFlags
- ForcedMotionRequest + ForcedMotionState + ForcedMotionFlags
- CombatControlMask
- CombatAgent + CombatAgentProfile
- CombatGroups (6 system groups)
- SensedTarget + TargetRelation + SensedTargetFlags
- TargetMemory, TargetSlot, CombatTargets, CombatRelationship
- CombatDesire + CombatDesireType + CombatDesireFlags
- CombatBrainProfile
- CombatHitEvent, CombatDeathEvent, SuperArmor, CombatLockState

### Systems (in BovineLabs.Combat.Systems)
- CombatMotionResolveSystem - Lane gate resolver
- CombatPhysicsMotorSystem - Only writer to PhysicsVelocity
- FacingResolveSystem - Facing lane to ResolvedFacing
- ForcedMotionResolveSystem - Knockback/stun/grab processing
- SensorQuerySystem - Distance-based target sensing
- TargetPatchSystem - Target memory updates
- CombatDesireScoringSystem - AI desire scoring
- CombatReactionBridgeSystem - Desire-to-target bridging

### Performance Rules Enforced
1. Only one system writes PhysicsVelocity
2. No behavior scans all enemies (shared SensedTarget buffer)
3. Sensor query runs once per entity
4. Top-K sensed targets (max 8)
5. No per-frame allocations in hot paths
6. No giant shared motion intent buffer
7. No designer-authored priority numbers
8. Timeline clips bake data; runtime clips are simple
9. Default lane value is always None
10. Forced motion is separate from desired motion
11. Facing is separate from movement
