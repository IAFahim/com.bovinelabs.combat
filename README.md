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
┌──────────────────────────────────────────────────────────────────┐
│                           YOUR GAME                              │
│           (Pick behaviors, combine with Blend)                    │
├──────────────────────────────────────────────────────────────────┤
│  Blend │ CombatAI │ TargetSelection                              │  Decision Layer
├────────┼──────────┼───────────────────────────────────────────────┤
│ Charge │ Flank │ Retreat │ Kite │ Surround │ Guard │ Ambush       │  Combat Behaviors
│ Patrol │ Follow │ Formation                                   │  Tactical Behaviors
├──────────────────────────────────────────────────────────────────┤
│ Steering │ Group │ Avoidance │ ObstacleAvoidance                │  Movement Primitives
├──────────────────────────────────────────────────────────────────┤
│ SpatialIntelligence ── GridIntelligence                          │  Spatial Awareness
├──────────────────────────────────────────────────────────────────┤
│ Navigation │ RoomTraversal                                      │  Path Layer
├──────────────────────────────────────────────────────────────────┤
│                           Core                                    │  Foundation
├──────────────────────────────────────────────────────────────────┤
│            BovineLabs.Recast + BovineLabs.Core                    │  Engine
│            BovineLabs.Core.Spatial (SpatialMap)                   │
└──────────────────────────────────────────────────────────────────┘
```

The Spatial Awareness layer runs BEFORE CombatSteeringGroup each frame:
1. **SpatialIntelligence** builds a `SpatialMap` from all agent positions (O(1) broadphase)
2. Each agent gets a `DynamicBuffer<SpatialNeighborData>` listing nearby entities
3. **GridIntelligence** reads that buffer, divides the agent's vicinity into cells, computes tactical data
4. All downstream modules (Group, Avoidance, CombatAI, etc.) read the neighbor buffer for free

## Modules (22 Independent Assemblies)

### Foundation
| Module | Description |
|--------|-------------|
| **Core** | Shared types: AgentId, TeamId, MovementStats, SteeringForce, SpatialNeighborData, CombatSteeringGroup, SteeringMath |

### Spatial Awareness
| Module | Description |
|--------|-------------|
| **SpatialIntelligence** | O(1) neighbor queries via BovineLabs.Core.SpatialMap. Populates DynamicBuffer\<SpatialNeighborData\> per agent with neighbor entity, distance, direction, team, velocity, radius. Computes SpatialThreatAssessment (enemy/ally counts, nearest enemy, enemy centroid, threat density). Runs before CombatSteeringGroup. |
| **GridIntelligence** | Local grid-based tactical analysis. Divides agent vicinity into configurable grid (default 8x8), computes per-cell enemy density, finds safest/danger/flanking directions. Produces steering toward cover when threats detected. |

### Movement Primitives
| Module | Description |
|--------|-------------|
| **Steering** | Seek, Flee, Arrive, Wander, Pursue, Evade - the 6 fundamental steering forces |
| **Avoidance** | Local agent avoidance using SpatialHash (RVO-inspired) |
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

### Path Layer
| Module | Description |
|--------|-------------|
| **Navigation** | NavMesh pathfinding via Recast, path following, corridor management |
| **RoomTraversal** | Room graph, door finding, room-to-room planning |

## Spatial Awareness: How It Works

The spatial awareness layer eliminates O(n^2) brute-force neighbor searches across ALL modules. Instead of each system (Group, Avoidance, TargetSelection) doing its own neighbor scan, SpatialIntelligence builds the data once per frame and shares it.

### SpatialIntelligence

**What it does:** Builds a `SpatialMap<SpatialPosition>` every frame using BovineLabs.Core.Spatial, then populates a neighbor buffer per agent.

**How it works:**
1. Gather all `LocalTransform` positions into a `SpatialMap` (quantizeStep=16, worldSize=4096)
2. The spatial map uses a quantized grid (256x256 cells, ~1MB) for O(1) broadphase lookups
3. For each agent, iterate the cells overlapping its search radius
4. For each candidate in those cells, check actual distance, team filter, and buffer capacity
5. Write `SpatialNeighborData` buffer entries and `SpatialThreatAssessment` component

**Setup:**
```csharp
// Add these components to any agent that needs spatial awareness
entityManager.AddComponentData(entity, MovementStats.Default);
entityManager.AddComponentData(entity, new TeamId { Value = 1 });
entityManager.AddComponentData(entity, SpatialNeighborConfig.Default);
entityManager.AddBuffer<SpatialNeighborData>(entity);
entityManager.AddComponentData(entity, new SpatialThreatAssessment());
```

**Configuration:**
```csharp
SpatialNeighborConfig.Default // SearchRadius=15, MaxNeighbors=32, no team filter
new SpatialNeighborConfig
{
    SearchRadius = 20f,       // How far to look for neighbors
    MaxNeighbors = 64,        // Buffer cap (prevents memory explosion)
    FilterByTeam = true,      // Only include enemies in buffer
    QueryTeamId = 0,          // 0 = all enemy teams
}
```

**Output per agent:**
- `DynamicBuffer<SpatialNeighborData>` - up to MaxNeighbors entries with Entity, Distance, Direction, TeamId, Velocity, Radius
- `SpatialThreatAssessment` - EnemyCount, AllyCount, NearestEnemyDirection/Distance, CentroidOfEnemies, ThreatDensity

### GridIntelligence

**What it does:** Reads the neighbor buffer, divides the agent's vicinity into a local grid, and computes tactical metrics per cell.

**How it works:**
1. Takes the `SpatialNeighborData` buffer (already populated by SpatialIntelligence)
2. Creates an NxN grid around the agent (default 8x8, covering 40x40 world units)
3. For each enemy neighbor, determines which grid cell it falls into
4. Computes per-cell enemy density, finds danger/safest/flanking directions
5. Writes `TacticalGridData` and optionally steers toward cover

**Setup:**
```csharp
entityManager.AddComponentData(entity, TacticalGridConfig.Default);
entityManager.AddComponentData(entity, new TacticalGridData());
```

**Configuration:**
```csharp
TacticalGridConfig.Default // GridRadius=20, Resolution=8, CoverWeight=0.8
new TacticalGridConfig
{
    GridRadius = 25f,          // Grid covers 50x50 area around agent
    GridResolution = 10,       // 10x10 = 100 cells
    ThreatThreshold = 0.3f,    // Cells above this density are "dangerous"
    CoverWeight = 0.9f,        // How strongly to steer away from threats
}
```

**Output per agent:**
- `TacticalGridData` - SafestDirection, DangerDirection, MaxThreatDensity, AverageThreatDensity, DangerousCellCount, FlankingDirection
- `SteeringForce` - automatically steers toward safest direction when dangerous cells detected (priority 1.5, behavior GridTactical)

### Reading Neighbor Data From Other Modules

Any other module can read the neighbor buffer for free:
```csharp
// In any system that runs after SpatialIntelligenceSystem
foreach (var (neighborBuffer, transform, teamId) in
    SystemAPI.Query<
        DynamicBuffer<SpatialNeighborData>,
        RefRO<LocalTransform>,
        RefRO<TeamId>>())
{
    for (int i = 0; i < neighborBuffer.Length; i++)
    {
        var neighbor = neighborBuffer[i];
        // neighbor.Entity, neighbor.Distance, neighbor.Direction,
        // neighbor.TeamId, neighbor.Velocity, neighbor.Radius
    }
}
```

### Reading Threat Assessment
```csharp
foreach (var threat in SystemAPI.Query<RefRO<SpatialThreatAssessment>>())
{
    if (threat.ValueRO.EnemyCount > 0)
    {
        var nearestEnemy = threat.ValueRO.NearestEnemyDirection * threat.ValueRO.NearestEnemyDistance;
        var density = threat.ValueRO.ThreatDensity;
    }
}
```

## Quick Start

Each module is self-contained. Add the asmdef you need:

```csharp
// Minimal agent with spatial awareness + seek behavior
entityManager.AddComponentData(entity, new MovementStats { MaxSpeed = 5f });
entityManager.AddComponentData(entity, new TeamId { Value = 1 });
entityManager.AddComponentData(entity, new SeekTarget { Position = targetPos });

// Add spatial awareness (optional but recommended)
entityManager.AddComponentData(entity, SpatialNeighborConfig.Default);
entityManager.AddBuffer<SpatialNeighborData>(entity);
entityManager.AddComponentData(entity, new SpatialThreatAssessment());
```

## Performance

SpatialIntelligence uses BovineLabs.Core.SpatialMap which is designed for per-frame rebuild:
- SpatialMap is rebuilt from scratch every frame (extremely fast to build)
- Quantized grid: 256x256 cells (~1MB) for a 4096x4096 world
- Broadphase reduces neighbor search from O(n^2) to O(n * k) where k = average cell density
- All neighbor data computed once, shared across all modules
- GridIntelligence uses stack-allocated cell arrays (Allocator.Temp, reclaimed per frame)

Memory per agent:
- SpatialNeighborData buffer: 32 entries * 44 bytes = ~1.4 KB (configurable via MaxNeighbors)
- SpatialThreatAssessment: 32 bytes (fixed)
- TacticalGridData: 36 bytes (fixed)

## Testing

22 test assemblies with 318 test methods. Every module has its own test asmdef.
Run with Unity Test Runner or:

```bash
unity-cli exec < test_script.cs
```

## Dependencies

- Unity 6000.5+ with Entities 6.5+
- BovineLabs.Core 1.6.1+ (provides SpatialMap, SpatialPosition, PositionBuilder)
- BovineLabs.Recast 1.0.6+ (for Navigation module)

## License

MIT
