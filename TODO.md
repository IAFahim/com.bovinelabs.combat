# TODO.md - Combat Movement System Implementation Plan

## Status Legend
- [ ] Not started
- [~] In progress
- [x] Complete

---

## Phase 0: Foundation
- [ ] 00.1 Core asmdef + shared types (AgentId, TeamId, MovementStats, SteeringForce, CombatPosition)
- [ ] 00.2 Core test asmdef + test helpers (WorldFixture, TestData generators)
- [ ] 00.3 Core unit tests (verify all component layouts, sizes, defaults)
- [ ] 00.4 Push Phase 0 to GitHub

## Phase 1: Movement Primitives
- [ ] 01.1 Steering asmdef + Seek (move toward target with desired velocity)
- [ ] 01.2 Steering Seek unit tests (zero target, distant target, arrival threshold, max speed clamp)
- [ ] 01.3 Steering: Flee (opposite of Seek - move away from threat)
- [ ] 01.4 Steering Flee unit tests
- [ ] 01.5 Steering: Arrive (Seek with deceleration zones - slow/stop radius)
- [ ] 01.6 Steering Arrive unit tests (deceleration curve, stop threshold, overshoot)
- [ ] 01.7 Steering: Pursue (predictive interception - aim at target's future position)
- [ ] 01.8 Steering Pursue unit tests (stationary target, fast target, max prediction clamped)
- [ ] 01.9 Steering: Evade (opposite of Pursue - flee from predicted position)
- [ ] 01.10 Steering Evade unit tests
- [ ] 01.11 Steering: Wander (random walk with constrained steering angle)
- [ ] 01.12 Steering Wander unit tests (wander circle, jitter, angle clamping)
- [ ] 01.13 Steering test asmdef + all integration tests
- [ ] 01.14 Push Phase 1 to GitHub

## Phase 2: Navigation
- [ ] 02.1 Navigation asmdef + NavMesh bridge (DtNavMesh wrapper, path request/result)
- [ ] 02.2 Navigation: PathfindingJob (A* via DtNavMeshQuery.FindPath)
- [ ] 02.3 Navigation: PathFollowSystem (follow polyline waypoints, arrive at each)
- [ ] 02.4 Navigation: PathCorridor (sliding window along path for smooth corners)
- [ ] 02.5 Navigation unit tests (path request, corridor advance, waypoint reached)
- [ ] 02.6 Push Phase 2 to GitHub

## Phase 3: Avoidance
- [ ] 03.1 Avoidance asmdef + SpatialMap integration for agent neighbor queries
- [ ] 03.2 Avoidance: RVO-like velocity obstacles (compute avoidance force from nearby agents)
- [ ] 03.3 Avoidance: Agent radius consideration, time-horizon clamping
- [ ] 03.4 Avoidance unit tests (2-agent head-on, perpendicular crossing, following)
- [ ] 03.5 ObstacleAvoidance asmdef + wall raycast (Detour.Raycast for wall detection)
- [ ] 03.6 ObstacleAvoidance: Wall sliding (steer along wall normal)
- [ ] 03.7 ObstacleAvoidance: Pillar avoidance (raycast fan pattern)
- [ ] 03.8 ObstacleAvoidance unit tests (approach wall, wall slide, pillar fan)
- [ ] 03.9 Push Phase 3 to GitHub

## Phase 4: Group Behaviors
- [ ] 04.1 Group asmdef + Cohesion (steer toward group centroid)
- [ ] 04.2 Group: Separation (push away from too-close neighbors)
- [ ] 04.3 Group: Alignment (match average group velocity)
- [ ] 04.4 Group unit tests (cohesion centering, separation distance, alignment convergence)
- [ ] 04.5 Push Phase 4 to GitHub

## Phase 5: Formation
- [ ] 05.1 Formation asmdef + FormationSlot (assign each agent a slot in a pattern)
- [ ] 05.2 Formation: Line formation (agents in a row)
- [ ] 05.3 Formation: Wedge/V formation (V-shape pointing at leader direction)
- [ ] 05.4 Formation: Grid formation (NxM grid behind leader)
- [ ] 05.5 Formation: Circle formation (agents evenly spaced around center)
- [ ] 05.6 Formation: Column formation (single file)
- [ ] 05.7 Formation: Dynamic slot reassignment (when agent dies, reshuffle)
- [ ] 05.8 Formation unit tests (slot positions for each pattern, reassignment on removal)
- [ ] 05.9 Push Phase 5 to GitHub

## Phase 6: Follow
- [ ] 06.1 Follow asmdef + FollowLeader (follow with configurable spacing + offset)
- [ ] 06.2 Follow: Chain following (follow the entity ahead of you, not the leader)
- [ ] 06.3 Follow unit tests (2-agent follow, chain depth 5, spacing enforcement)
- [ ] 06.4 Push Phase 6 to GitHub

## Phase 7: Patrol
- [ ] 07.1 Patrol asmdef + PatrolWaypoint (cycle through ordered waypoint list)
- [ ] 07.2 Patrol: PatrolArea (random wandering within bounded area)
- [ ] 07.3 Patrol: PatrolWithNavMesh (pathfind between waypoints via NavMesh)
- [ ] 07.4 Patrol unit tests (waypoint cycle, area bounds, path integration)
- [ ] 07.5 Push Phase 7 to GitHub

## Phase 8: Combat Behaviors
- [ ] 08.1 Charge asmdef + ChargeSystem (accelerate toward target, straight line rush)
- [ ] 08.2 Charge unit tests (charge initiation, max speed, arrival)
- [ ] 08.3 Flank asmdef + FlankSystem (approach from side/behind using target's facing)
- [ ] 08.4 Flank: Circle flank (wide arc around target to reach behind)
- [ ] 08.5 Flank: Split flank (group splits left/right to attack from both sides)
- [ ] 08.6 Flank unit tests
- [ ] 08.7 Retreat asmdef + RetreatSystem (move away from threat to safe distance)
- [ ] 08.8 Retreat unit tests
- [ ] 08.9 Kite asmdef + KiteSystem (maintain optimal range, attack then reposition)
- [ ] 08.10 Kite unit tests (optimal range enforcement, attack window, reposition arc)
- [ ] 08.11 Surround asmdef + SurroundSystem (agents take positions around target)
- [ ] 08.12 Surround: Slot assignment (distribute evenly around circle)
- [ ] 08.13 Surround unit tests (4-agent surround, 8-agent surround, gap fill on death)
- [ ] 08.14 Guard asmdef + GuardSystem (defend position, engage in range, return to post)
- [ ] 08.15 Guard unit tests (idle at post, engage enemy, return after kill)
- [ ] 08.16 Ambush asmdef + AmbushSystem (hide, wait, spring when enemy enters trigger)
- [ ] 08.17 Ambush unit tests
- [ ] 08.18 Push Phase 8 to GitHub

## Phase 9: Decision Layer
- [ ] 09.1 TargetSelection asmdef + NearestTarget (pick closest enemy)
- [ ] 09.2 TargetSelection: WeakestTarget (pick lowest HP enemy)
- [ ] 09.3 TargetSelection: MostThreatening (pick highest threat score)
- [ ] 09.4 TargetSelection: LineOfSight filter (only targets with clear LOS)
- [ ] 09.5 TargetSelection unit tests
- [ ] 09.6 Blend asmdef + WeightedBlend (combine N steering forces with weights)
- [ ] 09.7 Blend: PriorityArbitrate (pick highest-priority non-zero force)
- [ ] 09.8 Blend: ContextBehavior (context-steering with interest/danger maps)
- [ ] 09.9 Blend unit tests (2-force blend, priority override, context map)
- [ ] 09.10 CombatAI asmdef + EngagementRules (fight/flee/follow thresholds)
- [ ] 09.11 CombatAI: ThreatAssessment (compute threat score from HP/distance/type)
- [ ] 09.12 CombatAI unit tests
- [ ] 09.13 Push Phase 9 to GitHub

## Phase 10: Room Traversal
- [ ] 10.1 RoomTraversal asmdef + RoomGraph (define rooms, doors, connections)
- [ ] 10.2 RoomTraversal: DoorFinder (find nearest door toward target room)
- [ ] 10.3 RoomTraversal: RoomToRoom (plan route through room graph, pathfind each segment)
- [ ] 10.4 RoomTraversal: PursueAcrossRooms (follow target even through doors)
- [ ] 10.5 RoomTraversal unit tests (room graph queries, door finding, multi-room path)
- [ ] 10.6 Push Phase 10 to GitHub

## Phase 11: Polish & Documentation
- [ ] 11.1 Documentation: Architecture overview
- [ ] 11.2 Documentation: Per-module usage guide with code samples
- [ ] 11.3 Documentation: Designer cheat sheet
- [ ] 11.4 Performance benchmarks
- [ ] 11.5 Final push to GitHub

---

## Design Principles

1. **Each module is independent**: Only depends on Core. No cross-dependencies between behavior modules.
2. **Each module is a pure algorithm**: Input components → output SteeringForce. No side effects.
3. **Blend module combines everything**: Add Seek + Avoidance + Formation → single velocity.
4. **Navigation is a special layer**: Provides path waypoints. Steering operates on local goals.
5. **All values are floats on XZ plane**: Y is ignored for steering. PathCorridor handles Y from NavMesh.
6. **Burst-compiled everything**: No managed allocations in hot paths.
7. **Testable without Unity**: Pure math tests with NUnit. ECS tests with ECSTestsFixture.
