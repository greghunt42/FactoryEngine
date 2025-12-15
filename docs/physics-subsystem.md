# Physics Subsystem Specification

This document outlines the physics architecture targeted for the FactoryEngine kernel and modules.

## Goals
- Deterministic results across platforms/run sessions.
- Modular design: kernel provides interfaces, modules can plug in different solvers (simple AABB, rigid body, tilemap collisions, future 3D).
- Data-driven colliders and materials.
- Integration with ECS, event bus, and rendering for debug visualization.

## Architecture Overview
```
Physics Components + Collision Shapes
        |
Physics Systems (module-provided solvers)
        |
Physics Service (broad-phase, manifolds, integration helpers)
        |
ECS / Event Bus / Rendering (debug)
```

## Core Components
- `PhysicsBody`: mass, velocity, damping, gravity scale, flags (static/kinematic/dynamic).
- `Collider2D`: shape (AABB, circle, polygon), size, offset, material ID.
- `CollisionState`: contacts, normals, impulses (generated per frame).
- `PhysicsSettings`: per-world tunables (gravity vector, iterations, fixed timestep).

## Execution Model
1. **Integration:** Update velocities from forces/inputs.
2. **Broad Phase:** Spatial partition (grid or sweep & prune) to find candidate pairs.
3. **Narrow Phase:** Compute contacts/manifolds per collider shape.
4. **Resolution:** Apply impulses/positional corrections.
5. **Events:** Emit `CollisionStarted`, `CollisionEnded`, `TriggerEntered`, etc., via event bus.

The kernel provides basic data structures (spatial hash, manifolds). Modules may supply specialized solvers (platformer vs. top-down shooter) by plugging systems into the Physics phase.

## Fixed Timestep
- Physics can run at a fixed rate (e.g., 60 Hz). Pipeline scheduler handles catch-up ticks: run multiple physics steps per frame if `deltaTime` accumulates.
- Input and simulation phases write forces/commands; physics reads them during its tick(s).

## Data Definitions
- Collider data stored in prefab/scene files:
```yaml
Collider2D:
  shape: box
  size: [1, 2]
  offset: [0, -0.5]
  material: ice
```
- Materials define friction, restitution, density.

## Debugging
- Physics service registers debug draw commands (collider outlines, contact normals) emitted during RenderPrep when debug mode is enabled.
- Diagnostics include collision pair counts, solver iteration times, and penetration errors.

## Extensibility
- Provide `IPhysicsSolver` interface so modules can supply full solver implementations (e.g., ragdoll module).
- Allow modules to register custom collider shapes and manifold calculators.
- Integrate nav/pathfinding as separate modules referencing physics collision data.

## Future Work
- ADR for tilemap collision baking/workflow.
- Determine 3D support timeline/pipeline integration.
- Evaluate third-party physics libs for potential adapter.
