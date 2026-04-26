# Combat Movement - Architecture Overview

## Assembly Dependency Graph

```
BovineLabs.Combat.Core  ←  Everything references this
  ├── SteeringForce (output of all behaviors)
  ├── MovementStats (agent capabilities)
  ├── SteeringMath (pure math: Seek, Flee, Arrive, Pursue, Evade, Wander, etc.)
  ├── TeamId, CombatTarget, CombatHealth (identity + state)
  └── CombatSteeringGroup (system update group)

Each module below depends ONLY on Core:
  BovineLabs.Combat.Steering     → Seek, Flee, Arrive, Pursue, Evade, Wander
  BovineLabs.Combat.Navigation   → NavMesh bridge, PathFollow (depends also on Recast)
  BovineLabs.Combat.Avoidance    → RVO-like local agent avoidance
  BovineLabs.Combat.ObstacleAvoidance → Wall/pillar raycast avoidance
  BovineLabs.Combat.Group        → Cohesion, Separation, Alignment
  BovineLabs.Combat.Formation    → Line, Wedge, Grid, Circle, Column, V
  BovineLabs.Combat.Follow       → FollowLeader, FollowChain
  BovineLabs.Combat.Patrol       → Waypoint cycling, Area wandering
  BovineLabs.Combat.Charge       → Straight-line rush attack
  BovineLabs.Combat.Flank        → Side/behind approach
  BovineLabs.Combat.Retreat      → Orderly withdrawal
  BovineLabs.Combat.Kite         → Hit-and-run at optimal range
  BovineLabs.Combat.Surround     → Encircle target from all angles
  BovineLabs.Combat.Guard        → Defend post, engage, return
  BovineLabs.Combat.Ambush       → Hide, wait, spring attack
  BovineLabs.Combat.TargetSelection → Nearest, Weakest, MostThreatening
  BovineLabs.Combat.Blend        → WeightedBlend, PrioritySelect
  BovineLabs.Combat.CombatAI     → State machine: Idle/Engaging/Fleeing/Following
  BovineLabs.Combat.RoomTraversal → Room graph BFS, door-to-door pathing
```

## Data Flow

```
  Designer sets up components:
    MovementStats, TeamId, SeekTarget, AvoidanceParams, etc.
         │
         ▼
  ┌──────────────────────────────────────┐
  │       CombatSteeringGroup            │  (runs every fixed step)
  │                                      │
  │  SeekSystem ──────┐                  │
  │  AvoidanceSystem ─┤                  │
  │  FormationSystem ─┤──→ SteeringForce │  (each writes to SteeringForce)
  │  FollowSystem ────┤                  │
  │  ChargeSystem ────┘                  │
  └──────────────┬───────────────────────┘
                 │
                 ▼
  ┌──────────────────────────────────────┐
  │       BlendSystem                    │  (runs after CombatSteeringGroup)
  │                                      │
  │  Reads all active SteeringForces     │
  │  Applies weights / priority select   │
  │  Outputs FinalSteeringForce          │
  └──────────────┬───────────────────────┘
                 │
                 ▼
  Your MovementSystem reads FinalSteeringForce
  and applies to LocalTransform / Velocity
```

## Quick Setup Examples

### Basic Seek (move toward target)
```csharp
entityManager.AddComponentData(entity, new MovementStats { MaxSpeed = 5f });
entityManager.AddComponentData(entity, new SeekTarget { Position = new float2(10f, 10f) });
entityManager.AddComponentData(entity, new SteeringForce());
```

### Guard Post (defend a location)
```csharp
entityManager.AddComponentData(entity, new MovementStats { MaxSpeed = 4f, Radius = 0.5f });
entityManager.AddComponentData(entity, new GuardPost {
    Position = new float2(5f, 5f),
    EngagementRadius = 8f,
    ReturnRadius = 12f
});
entityManager.AddComponentData(entity, new GuardState());
entityManager.AddComponentData(entity, new TeamId { Value = 1 });
```

### Formation (5 agents in wedge)
```csharp
// Leader
entityManager.AddComponentData(leader, new FormationLeader { FormationOffset = float2.zero });
entityManager.AddComponentData(leader, new FormationConfig {
    Type = FormationType.Wedge,
    Spacing = 2f
});

// Followers
for (int i = 0; i < 4; i++) {
    entityManager.AddComponentData(followers[i], new FormationSlotAssignment { SlotIndex = i });
}
```

### Full Combat Agent (seek + avoid + select target)
```csharp
entityManager.AddComponentData(entity, new MovementStats { MaxSpeed = 5f, Radius = 0.5f });
entityManager.AddComponentData(entity, new SteeringForce());
entityManager.AddComponentData(entity, new TeamId { Value = 1 });
entityManager.AddComponentData(entity, new CombatTarget());
entityManager.AddComponentData(entity, new TargetSelectionParams {
    MaxRange = 15f,
    Strategy = SelectionStrategy.Nearest
});
entityManager.AddComponentData(entity, new AvoidanceParams());
entityManager.AddComponentData(entity, new SeekTarget());
```
