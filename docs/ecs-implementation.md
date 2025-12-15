# ECS Implementation Blueprint

This document translates the ECS core specification into concrete implementation steps and structures for FactoryEngine.

## Project Structure
```
src/
  Engine/
    ECS/
      Entity.cs
      EntityManager.cs
      ComponentStorage/
        IComponentStore.cs
        SparseSetStore.cs
      Query/
        QueryBuilder.cs
        QueryIterator.cs
      Systems/
        ISystem.cs
        SystemScheduler.cs
    Services/
      ...
```

## Entity Manager
- Maintains arrays of entity metadata (`State`, `Generation`, `ComponentMask`).
- Free list of destroyed entity indices for reuse.
- API:
  - `Entity CreateEntity()`
  - `void DestroyEntity(Entity entity)` (marks pending destruction)
  - `bool IsAlive(Entity entity)`
- End-of-frame compaction removes pending entities and increments generations.

## Component Storage
- `SparseSetStore<T>` implements `IComponentStore`.
- Data members:
  - `List<T> denseData`
  - `List<Entity> denseEntities`
  - `int[] sparse` (maps entity index -> dense index or `InvalidIndex`).
- Operations:
  - `bool Has(Entity e)`
  - `ref T Add(Entity e)` (adds default-initialized struct, returns reference)
  - `void Remove(Entity e)` (swap-remove from dense array)
  - `ref T Get(Entity e)` (throws if missing)
  - `Span<T> DenseSpan` for iteration.
- Versioning: optional `uint[] componentVersion` to track writes.

## Component Registry
- Maps `ComponentTypeId` to store instances.
- Handles registration from modules (component descriptors supply type info, serialization metadata, optional construction hooks).

## Queries
- `QueryBuilder` composes `All<T>`, `Any<T>`, `None<T>` filters.
- Query execution obtains intersected entity lists by iterating the smallest component set first.
- Provide `foreach`-style API:
```csharp
foreach (var (entity, transform, velocity) in world.Query<Transform, Velocity>())
{
    // update
}
```
- Under the hood, iterators return references to components to avoid copying.

## Systems & Scheduler
- Systems implement `void Run(World world, in SystemContext ctx)`.
- `SystemContext` exposes services, delta time, event bus, and phase info.
- Scheduler groups systems per pipeline phase, sorts by priority, enforces read/write constraints:
  - Each system declares `ComponentAccess` (Read/Write sets).
  - Scheduler detects conflicts; if unsatisfied, engine logs/throws during registration.

## World Lifecycle
1. `WorldBuilder` registers modules, components, and systems.
2. `World` holds entity manager, component registry, scheduler, event bus reference.
3. `World.Tick(deltaTime)` executes phases sequentially:
   - Process queued entity/component additions/removals as needed.
   - Run systems per phase via scheduler.
   - Flush destruction queue at end of frame.

## Threading
- V1 runs systems sequentially for determinism.
- Prepare for future parallelization by isolating mutable state per system and keeping scheduler metadata ready for dependency graphs.

## Diagnostics Hooks
- `ComponentStats` (count, capacity) exposed for tooling.
- Scheduler reports per-system duration.
- Entity manager exposes counts (total, alive, pending destruction).

## Testing Strategy
- Unit tests for entity lifecycle, component add/remove, query correctness.
- Microbenchmarks for add/remove/query hot paths.
- Integration tests with sample systems verifying deterministic ordering.

## Future Work
- Archetype backend adapter once performance profiling demands it.
- Job system integration w/ per-phase task graphs.
- Chunk-based shared component filters.
