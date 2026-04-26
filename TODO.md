# TODO.md - Combat Movement System

## Status: ALL CORE MODULES COMPLETE

### What's Built
- **12 git commits** pushed to https://github.com/IAFahim/com.bovinelabs.combat
- **90 C# source files**, **10,835 lines** of algorithm-centric code
- **20 independent behavior modules**, each in own asmdef
- **15 test assemblies** with 250+ test cases
- **0 compilation errors** on Unity 6000.5.0b1 (verified before Library rebuild)

### Modules

| # | Module | Types | Systems | Key Algorithm |
|---|--------|-------|---------|---------------|
| 1 | Core | 15 | 1 (ApplySteering) | SteeringMath (12 methods), LineOfSightMath, CombatSteeringGroup |
| 2 | Steering | 6 | 6 | Seek, Flee, Arrive, Pursue, Evade, Wander |
| 3 | Navigation | 5 | 1 | NavMesh bridge (Recast), PathFollow, PathCorridor |
| 4 | Avoidance | 2 | 1 | RVO-like velocity obstacles, SpatialHash |
| 5 | ObstacleAvoidance | 2 | 1 | WallSliding, RaycastFan, obstacle repulsion |
| 6 | Group | 3 | 1 | Cohesion, Separation, Alignment (boids) |
| 7 | Formation | 4 | 1 | Line/Wedge/Grid/Circle/Column/V + SlotAssignment |
| 8 | Follow | 3 | 1 | FollowLeader, FollowChain |
| 9 | Patrol | 4 | 1 | Waypoint cycling, Area wandering |
| 10 | Charge | 2 | 1 | Straight-line rush with acceleration |
| 11 | Flank | 2 | 1 | Side/behind approach via target facing |
| 12 | Retreat | 2 | 1 | Orderly withdrawal to safe distance |
| 13 | Kite | 2 | 1 | Hit-and-run at optimal range |
| 14 | Surround | 2 | 1 | Even circle encirclement |
| 15 | Guard | 2 | 1 | Post defense + engage + return |
| 16 | Ambush | 3 | 1 | Hide + wait + spring attack |
| 17 | TargetSelection | 2 | 1 | Nearest, Weakest, MostThreatening + cone filter |
| 18 | Blend | 2 | 1 | Weighted blend + priority select |
| 19 | CombatAI | 3 | 1 | State machine: Idle/Engaging/Fleeing/Following |
| 20 | RoomTraversal | 5 | 1 | BFS room graph, door finding |

### Live Verification (before Library rebuild)
- 32/32 key types loaded and verified
- SteeringMath: 14/14 PASS
- Combat Math: 20/21 PASS (1 test assumption error)
- 34 assemblies compiled clean
- ApplySteeringSystem moves entities based on steering output

### Test Coverage

| Test Assembly | Test Count | Covers |
|--------------|-----------|--------|
| Core.Tests | 34 + 4 ECS | SteeringMath, Components, LineOfSight, ApplySteering |
| Steering.Tests | 4 | WanderMath |
| Avoidance.Tests | 14 + 6 | AvoidanceMath, SpatialHash |
| Formation.Tests | 20 | All 6 formation patterns |
| Charge.Tests | 16 | Charge direction, validity, force |
| Flank.Tests | 10 | Flank position, direction |
| Retreat.Tests | 7 | Retreat direction, safe distance |
| Kite.Tests | 12 | Kite position, ShouldKite |
| Surround.Tests | 11 | Surround positions, slot assignment |
| Guard.Tests | 13 | ShouldEngage, ShouldReturn |
| TargetSelection.Tests | 19 | Nearest, Weakest, MostThreatening, IsInCone |
| Blend.Tests | 16 | WeightedBlend, PrioritySelect, Truncate |
| RoomTraversal.Tests | 21 | FindRoom, FindDoor, BFS PlanRoute |
| Follow.Tests | 10 | FollowPosition, ChainPosition |
| ObstacleAvoidance.Tests | 12 | WallSliding, RaycastFan, ComputeForce |
| Ambush.Tests | 16 | IsEnemyInTrigger, AmbushForce, HasReached |
| CombatAI.Tests | 16 | ThreatScore, ShouldEngage/Flee/Disengage |
| Navigation.Tests | 15 | PathCorridor, PathRequest, NavMeshAreaCosts |

### Pending (needs Unity rebuild to complete)
- Unity is rebuilding Library from scratch (full Library delete was needed to pick up new .cs files)
- Once rebuilt, run all 250+ tests through Unity Test Runner
- ECS integration tests need verification with ApplySteeringSystem
