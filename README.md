# BovineLabs Combat Movement

S-tier combat movement system for Unity DOTS/ECS RPG games.

## Philosophy

- **Algorithm-centric**: Every behavior is a pure, testable algorithm with clear inputs and outputs
- **Designer-friendly**: Simple authoring components, intuitive parameters, visual debugging
- **Fully modular**: Each behavior is an independent asmdef. Pick what you need, ignore the rest
- **Data-Oriented**: Pure ECS - IComponentData, Burst-compiled jobs, zero GC allocations
- **Flat XZ World**: Designed for top-down/isometric RPG with walls, pillars, rooms, and corridors

## Architecture

```
┌─────────────────────────────────────────────────────┐
│                    YOUR GAME                         │
│        (Pick behaviors, combine with Blend)          │
├─────────────────────────────────────────────────────┤
│  Blend │ CombatAI │ TargetSelection                  │  ← Decision Layer
├────────┼──────────┼──────────────────────────────────┤
│ Charge │ Flank │ Retreat │ Kite │ Surround │ Guard   │  ← Combat Behaviors
│ Patrol │ Ambush │ Follow │ Formation                 │  ← Tactical Behaviors
├─────────────────────────────────────────────────────┤
│ Steering │ Group │ Avoidance │ ObstacleAvoidance     │  ← Movement Primitives
├─────────────────────────────────────────────────────┤
│  Navigation │ RoomTraversal                          │  ← Path Layer
├─────────────────────────────────────────────────────┤
│                    Core                              │  ← Foundation
├─────────────────────────────────────────────────────┤
│           BovineLabs.Recast + BovineLabs.Core        │  ← Engine
└─────────────────────────────────────────────────────┘
```

## Modules (24 Independent Behaviors)

### Foundation
| Module | Description |
|--------|-------------|
| **Core** | Shared types: AgentId, TeamId, MovementStats, SteeringForce, CombatPosition |

### Navigation
| Module | Description |
|--------|-------------|
| **Navigation** | NavMesh pathfinding via Recast, path following, corridor management |
| **RoomTraversal** | Room graph, door finding, room-to-room planning |

### Movement Primitives
| Module | Description |
|--------|-------------|
| **Steering** | Seek, Flee, Arrive, Wander, Pursue, Evade - the 6 fundamental steering forces |
| **Avoidance** | Local agent avoidance using SpatialMap (RVO-inspired) |
| **ObstacleAvoidance** | Wall/pillar avoidance via raycasting |
| **Group** | Cohesion, Separation, Alignment (boids-like group forces) |

### Tactical Behaviors
| Module | Description |
|--------|-------------|
| **Formation** | Line, Wedge, Grid, Circle, Column, V-formation slot assignment |
| **Follow** | Follow-the-leader with configurable spacing and chain depth |
| **Patrol** | Waypoint cycling and area-bounded random patrol |

### Combat Behaviors
| Module | Description |
|--------|-------------|
| **Charge** | Rush toward target in a straight line with acceleration |
| **Flank** | Approach target from the side or behind |
| **Retreat** | Orderly withdrawal to safe distance |
| **Kite** | Hit-and-run: maintain optimal distance, attack then reposition |
| **Surround** | Multiple agents encircle a target from all angles |
| **Guard** | Stay near a post, engage enemies that enter range |
| **Ambush** | Hide at position, wait for enemy to enter trigger zone |

### Decision Layer
| Module | Description |
|--------|-------------|
| **TargetSelection** | Best-target algorithms: nearest, weakest, most threatening |
| **Blend** | Weighted blending and priority-based steering arbitration |
| **CombatAI** | Engagement rules: when to fight, flee, call for help |

## Quick Start

Each module is self-contained. Add the asmdef you need:

```csharp
// Seek behavior - move toward a target
entityManager.AddComponentData(entity, new SeekTarget { Position = targetPos });
entityManager.AddComponentData(entity, new MovementStats { MaxSpeed = 5f });
```

## Testing

Every module has its own test assembly. Run with Unity Test Runner or:

```bash
unity-cli exec < test_script.cs
```

## License

MIT
