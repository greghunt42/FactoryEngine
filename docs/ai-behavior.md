# AI & Behavior Framework Specification

FactoryEngine targets multiple genres with diverse AI needs (state machines, behavior trees, planners). This document defines a flexible baseline that modules can extend without kernel changes.

## Goals
- Deterministic execution within the AI pipeline phase.
- Data-driven behavior assets (behavior trees, BT fragments, utility curves).
- Extensible runtime so modules can plug in custom evaluators, steering, and decision models.
- Debuggable authoring (visualization hooks, breakpoint support).

## Runtime Architecture
```
Behavior Assets -> Behavior Loader -> BehaviorGraph (runtime) -> AI Systems -> ECS Components
```

- **Behavior Assets:** YAML/JSON describing nodes, conditions, actions, parameters.
- **Behavior Loader:** Validates schema, instantiates runtime node graph, registers with module.
- **AI Components:** Entities reference behavior graphs and store per-entity blackboard data.
- **AI Systems:** Run during the AI phase, ticking behaviors and issuing commands/events.

## Core Concepts
- **Behavior Graph:** Directed tree/graph with nodes implementing `Tick`, `Enter`, `Exit` semantics.
- **Blackboard:** Key-value store (struct-friendly) per entity, optionally namespaced for sharing across entities (squad tactics).
- **Action Library:** Modules register action handlers (C# structs/classes) that operate on components/events.
- **Condition Library:** Query components, events, or blackboard values to gate behavior transitions.

## Data Schema Sketch
```yaml
name: EnemyBasicBT
root: Selector
nodes:
  Selector:
    children: [Flee, Attack]
  Flee:
    type: Sequence
    children: [IsLowHealth, FindCover, MoveTo]
  Attack:
    type: Sequence
    children: [HasLineOfSight, FireWeapon]
```

## Execution Model
1. AI system iterates entities with `BehaviorComponent`.
2. Each behavior node `Tick`s based on last state (Running/Success/Failure).
3. Actions dispatch events (e.g., `RequestMove`) or mutate components.
4. Blackboard data persists across frames; nodes can read/write keys via typed accessors.

## Extensibility
- Modules register new node types (custom sequences, utility nodes) through a descriptor.
- Utility AI or GOAP planners can coexist by implementing the same interface (tick function) and referencing their own data assets.
- AI middleware integration (e.g., GOAP solver) occurs at module level; kernel only provides scheduling/context.

## Debugging Tools
- Behavior visualizer subscribes to AI system events (`NodeStarted`, `NodeEnded`, `ActionTriggered`).
- Breakpoints allow pausing on specific nodes or entities.
- Blackboard inspection APIs expose current values for UI overlays.

## Future Work
- ADR for behavior asset schema and runtime serialization once first implementation stabilizes.
- Shared planner service for tactics modules.
- Integration with nav/physics components for pathing.
