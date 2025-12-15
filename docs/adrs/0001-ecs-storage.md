# 0001 - Sparse-Set ECS Storage

Date: 2024-01-01

## Status
Accepted

## Context
- Engine must support millions of entities across multiple genres.
- Kernel longevity requires a simple storage strategy that is well understood and battle-tested.
- Archetype ECS offers great cache locality but is complex to implement and maintain in early engine stages.
- Struct-only components should remain contiguous for fast iteration, and iteration order should be deterministic.

## Decision
Adopt sparse-set storage per component type:
- Each component type owns a dense array of component data and a sparse array mapping entity IDs to dense indices.
- Entities can add/remove components in O(1) time, and iteration over a component type is contiguous in memory.
- Systems compose queries by intersecting component sparse sets.

## Consequences
- **Pros:** Predictable performance, easy to reason about, simple serialization story, and minimal per-type bookkeeping.
- **Cons:** Querying multiple components requires intersecting sets manually; no built-in structural grouping of related components; future upgrades may require migrations.
- **Follow-ups:** Keep system APIs abstract so we can swap in archetype storage later. Document serialization requirements per component.

## Alternatives Considered
- **Archetype/Chunk storage:** Better coherence for multi-component iteration but adds significant complexity upfront and makes structural changes more expensive.
- **Dictionary or list-based storage:** Simpler to implement but slower to iterate and more GC pressure, violating performance goals.
