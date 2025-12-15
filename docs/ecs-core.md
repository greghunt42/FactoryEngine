# ECS Core Specification

This document codifies the expectations for FactoryEngine's entity-component-system kernel. It should inform both implementation and module authorship.

## Objectives
- Provide deterministic, cache-friendly storage for struct-only components.
- Support millions of entities with predictable allocation patterns.
- Maintain stable APIs so modules remain source compatible for years.
- Enable safe extension without kernel modifications.

## Entities
- Entities are 64-bit values composed of `index` and `generation` segments.
- Generation increments whenever an entity at a given index is destroyed, preventing stale handles.
- `Entity` is a lightweight value type; no heap allocations when issuing or copying IDs.
- Entity creation occurs via `World.CreateEntity()`; destruction via `World.DestroyEntity()` (queued until end of frame to keep system iteration stable).

## Components
- Components are C# `struct`s with no virtual members and no references to MonoGame types.
- Each component type owns a sparse-set storage (dense array + sparse index) that maps entity IDs to component data.
- Adding/removing components can be batched; mutations are versioned so systems can detect changes if needed.
- Serialization metadata lives alongside the component type (attributes or registration object) so data-driven pipelines can instantiate components from JSON/YAML.

## Systems
- Systems are stateless objects (or static functions) that operate over queries defined by component requirements and optional filters.
- Systems declare the pipeline phase they run in and are executed in deterministic order per phase.
- Queries expose iterators over matched entities with direct references to component data.
- Write conflicts are prevented by scheduling rules (e.g., exclusive component access) or by splitting systems into compatible phases.

## World
- The world orchestrates entity lifecycle, component storage, and system scheduling.
- Worlds can be nested or instanced to support level streaming or deterministic simulations (e.g., server vs. client).
- Module registration occurs at world creation: modules contribute component descriptors, system factories, and serialization hooks.

## Queries & Filters
- Primary query primitive matches `All<T1, T2,...>` and optional `None<Tx>` / `Any<Ty>` sets.
- Filters can include tags, shared data slots, or chunk-level metadata for future archetype migration.
- Iteration order is by dense array index to maximize cache locality.

## Events & Messaging
- The ECS core exposes lifecycle events (entity created/destroyed, component added/removed) via the event bus.
- Systems remain decoupled by publishing gameplay events instead of mutating distant components directly.

## Diagnostics
- Each component type tracks counts, add/remove rates, and memory usage for tooling.
- Worlds expose instrumentation hooks so modules can integrate with profilers or debug UIs.

## Future Considerations
- Hybrid storage: allow certain high-frequency components to opt into SoA layouts.
- Burst-style job graphs: once MonoGame threading constraints are mapped, systems could fan out across worker threads while respecting determinism.
