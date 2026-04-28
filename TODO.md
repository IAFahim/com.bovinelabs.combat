# TODO.md - Combat Movement System

## Status: ALL MODULES COMPLETE + SPATIAL AWARENESS LAYER

### What's Built
- **18 git commits** pushed to https://github.com/IAFahim/com.bovinelabs.combat
- **100 C# source files**, **12,119 lines** (6,834 runtime + 5,285 test)
- **22 independent behavior modules**, each in own asmdef
- **20 test assemblies** with 318 test methods
- **0 compilation errors** on Unity 6000.5.0b1 (batch mode verified)
- **0 runtime bugs** (code review passed, 5 critical + 2 high + 4 medium fixed)

### Spatial Awareness Layer (NEW)

Two new modules that provide O(1) neighbor queries and grid-based tactical analysis:

| Module | Types | Systems | Key Feature |
|--------|-------|---------|-------------|
| **SpatialIntelligence** | 3 components + math | 1 (IJobEntity) + 1 (IJobChunk gather) | BovineLabs.Core.SpatialMap broadphase, DynamicBuffer\<SpatialNeighborData\>, SpatialThreatAssessment |
| **GridIntelligence** | 2 components + math | 1 (main-thread foreach) | Per-cell enemy density, danger/safest/flanking directions, cover steering |

### All Modules

| # | Module | Types | Systems | Key Algorithm |
|---|--------|-------|---------|---------------|
| 1 | Core | 16 | 1 (ApplySteering) | SteeringMath (12 methods), LineOfSightMath, CombatSteeringGroup, SpatialNeighborData |
| 2 | SpatialIntelligence | 3 | 2 | SpatialMap broadphase, neighbor buffer, threat assessment |
| 3 | GridIntelligence | 2 | 1 | Grid cell analysis, danger/safe/flanking directions |
| 4 | Steering | 6 | 6 | Seek, Flee, Arrive, Pursue, Evade, Wander |
| 5 | Navigation | 5 | 1 | NavMesh bridge (Recast), PathFollow, PathCorridor |
| 6 | Avoidance | 2 | 1 | RVO-like velocity obstacles, SpatialHash |
| 7 | ObstacleAvoidance | 2 | 1 | WallSliding, RaycastFan, obstacle repulsion |
| 8 | Group | 3 | 1 | Cohesion, Separation, Alignment (boids) |
| 9 | Formation | 4 | 1 | Line/Wedge/Grid/Circle/Column/V + SlotAssignment |
| 10 | Follow | 3 | 1 | FollowLeader, FollowChain |
| 11 | Patrol | 4 | 1 | Waypoint cycling, Area wandering |
| 12 | Charge | 2 | 1 | Straight-line rush with acceleration |
| 13 | Flank | 2 | 1 | Side/behind approach via target facing |
| 14 | Retreat | 2 | 1 | Orderly withdrawal to safe distance |
| 15 | Kite | 2 | 1 | Hit-and-run at optimal range |
| 16 | Surround | 2 | 1 | Even circle encirclement |
| 17 | Guard | 2 | 1 | Post defense + engage + return |
| 18 | Ambush | 3 | 1 | Hide + wait + spring attack |
| 19 | TargetSelection | 2 | 1 | Nearest, Weakest, MostThreatening + cone filter |
| 20 | Blend | 2 | 1 | Weighted blend + priority select |
| 21 | CombatAI | 3 | 1 | State machine: Idle/Engaging/Fleeing/Following |
| 22 | RoomTraversal | 5 | 1 | BFS room graph, door finding |

### Test Coverage

| Test Assembly | Test Count | Covers |
|--------------|-----------|--------|
| Core.Tests | 38 | SteeringMath, Components, LineOfSight, ApplySteering |
| SpatialIntelligence.Tests | 10 | ComputeThreatDensity, ComputeCentroid, ComputeThreatAssessment |
| GridIntelligence.Tests | 8 | WorldToCell, CellToWorld, ComputeFlankingDirection, ComputeGridAnalysis |
| Steering.Tests | 4 | WanderMath |
| Avoidance.Tests | 20 | AvoidanceMath, SpatialHash |
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
| Navigation.Tests | 17 | PathCorridor, PathRequest, NavMeshAreaCosts, PathWaypoint |

### Build Verification
- Unity 6000.5.0b1 batch mode: 0 CS errors, 2251 assemblies compiled in ~4 seconds
- SpatialIntelligence.dll, SpatialIntelligence.Tests.dll
- GridIntelligence.dll, GridIntelligence.Tests.dll

### Code Review (passed)
All critical runtime bugs found and fixed:
- PositionBuilder vs IEnableableComponent assertion (replaced with manual IJobChunk)
- Query missing components for IJobEntity (added SpatialThreatAssessment + SpatialNeighborData)
- goto done skipping threat counting when buffer full (replaced with bufferFull flag)
- entityInQueryIndex not supported in Entities 6.x (removed, use Entity comparison)
- IJobEntity must be in own file (split from ISystem file)
- Threat counting blocked by team filter (restructured control flow)
- Unused allocation cellNeighborCounts (removed)
- SteeringForce stale values when threats clear (reset to zero)
