# Navigation & Pathfinding Foundations

While not part of the v1 kernel, navigation data and pathfinding services will be essential for many genres. This document captures preliminary goals and interfaces so modules can plan ahead.

## Goals
- Separate nav data generation from runtime path queries.
- Support 2D grid/tile nav first, with path to 3D navmesh later.
- Allow modules to plug in custom solvers (A*, flow fields, steering).
- Integrate with physics/collision data for obstacle awareness.

## Architecture Concept
```
Nav Data Assets (grids, graphs, meshes)
        |
Nav Loader & Catalog Entries
        |
Navigation Service
        |
Modules / Systems (AI, movement)
```

## Data Assets
- `NavGrid`: YAML describing tile walkability, costs, portals.
- `NavMesh`: future format referencing polygons + links.
- `NavGraph`: generic nodes/edges for scripted movement (e.g., patrol routes).
- Assets reference collider data (tilemap, physics shapes) for validation.

## Navigation Service
- APIs:
```csharp
NavQueryHandle query = navService.CreateQuery(NavSpaceId);
var path = query.FindPath(start, goal, options);
navService.RegisterDynamicObstacle(entity, shape);
```
- Manages nav spaces per scene/world; each nav space references one or more nav assets.
- Provides streaming updates when nav data changes (hot reload).

## Pathfinding
- Default solver: A* on grid with heuristics; modules can register custom solvers via interface.
- Supports weighted costs, forbidden areas, and dynamic obstacles (updated from physics or AI systems).

## Integration
- AI behavior nodes request paths via navigation service and store results in blackboard components.
- Physics systems consume paths as steering targets.
- Rendering debug overlays draw nav grids/paths for debugging.

## Future Work
- ADR for nav asset schema once format stabilizes.
- Evaluate navmesh generation pipeline (integration with external tools).
- Determine how nav service interacts with streaming/world partitioning.
