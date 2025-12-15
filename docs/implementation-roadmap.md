# Implementation Roadmap

This document translates high-level goals into concrete engineering milestones with suggested sequencing.

## Phase 0 – Scaffolding & Tooling
**Goals:** Prepare repo structure, basic tooling, logging, and service interfaces.

**Tasks**
1. Create `src/FactoryEngine.sln` with engine + tests + sample projects.
2. Set up CI skeleton (GitHub Actions or equivalent) running lint/tests.
3. Implement logging infrastructure per ADR-0006 (NDJSON writer, console/file sinks).
4. Define service interfaces (`IAssetService`, `IInputService`, `IRenderService`, `IAudioService`, `ISerializationService`, `IDiagnosticsService`).
5. Add placeholder implementations wired into a basic engine bootstrap (no ECS yet).
6. Update `docs/design-journal.md` with kickoff notes.

**Acceptance Criteria**
- Solution builds/tests pass in CI.
- Logging emits structured NDJSON to console.
- Services can be constructed and disposed without MonoGame dependencies.

## Phase 1 – ECS Kernel
**Goals:** Implement entity/component storage, queries, scheduler, and event bus.

**Tasks**
1. Implement `Entity`, `EntityManager`, pending destruction queue, unit tests.
2. Implement `SparseSetStore<T>` with add/remove/get tests + benchmarks.
3. Build component registry and descriptor registration logic.
4. Implement query API (`QueryBuilder`, iterators) with integration tests.
5. Implement `SystemScheduler` with phases, priorities, access conflict detection.
6. Wire in event bus runtime and expose lifecycle events.
7. Instrument ECS via diagnostics hooks (per docs/diagnostics-logging).
8. Add benchmarks comparing component iteration vs baseline.

**Acceptance Criteria**
- Unit/integration tests cover entity lifecycle, queries, scheduling.
- Benchmarks demonstrate expected O(1) add/remove and contiguous iteration.
- Documentation updated with any deviations, design journal entry added.

## Phase 2 – Service Layer Foundations
**Goals:** Make asset, serialization, input, rendering, and audio services functional enough for the sample module to run basic loops.

**Tasks**
1. Asset service: load catalog manifests (`docs/asset-catalog.md`), resolve IDs, emit hot reload events.
2. Serialization pipeline: implement component descriptors, prefab/scene loader, integrate with ECS/world builder.
3. Input service: implement action map loader (`docs/input-abstraction.md`), desktop adapter, update `InputComponent` system.
4. Rendering facade: implement command buffer (`docs/rendering-facade.md`), MonoGame backend stub.
5. Audio service: load sound banks, play/stop basic sounds, integrate with event bus.
6. Add unit/integration tests for each service.
7. Expand `fe-tools` CLI skeleton with `validate-data` + `validate-assets` commands stubs.

**Acceptance Criteria**
- Engine can boot, create a world, and tick through empty phases with all services wired.
- Sample data (prefab/action map) loads and surfaces minimal behavior (e.g., logging, no visuals yet).
- CLI commands run and report stubbed status.

## Phase 3 – Sample Module Alpha
**Goals:** FactoryPlatformer module exercises ECS + services end-to-end.

**Tasks**
1. Implement core components (`Transform2D`, `Velocity2D`, etc.) and register via module manifest.
2. Implement Input phase system translating action maps to movement intents.
3. Implement Simulation phase systems (movement/gravity).
4. Implement Physics phase AABB solver + collision events.
5. Implement Animation system updating sprite frames.
6. Implement RenderPrep commands for sprite drawing using rendering facade.
7. Author initial prefabs/scenes/behavior assets per `docs/sample-module-plan.md`.
8. Create MonoGame runner to load engine + module + scene.
9. Add integration tests verifying pipeline order and sample scene determinism.

**Acceptance Criteria**
- Sample runner displays interactive scene with movement/collisions/animation.
- Hot reload demo (modify prefab, see change live) works for at least one component.
- `fe-tools validate-data` passes on sample assets.

## Phase 4 – Expanded Systems
**Goals:** Deepen systems and add diagnostics overlay.

**Tasks**
1. Physics service helpers (spatial hash, collision events, debug draw).
2. Audio positional playback + playlists per `docs/audio-subsystem.md`.
3. AI behavior runtime MVP with sample enemy behavior.
4. Diagnostics overlay showing ECS stats, logs, command buffer debug view.
5. Rendering improvements (materials, clipping, camera support).
6. Input rebinding UI demo.

**Acceptance Criteria**
- Sample module demonstrates physics debug view, AI enemy behaviors, positional audio.
- Diagnostics overlay can toggle metrics/logs.
- Documentation/design journal updated with new learnings.

## Phase 5 – Tooling & Hardening
**Goals:** Solidify validation, capture/replay, and performance.

**Tasks**
1. Complete `fe-tools` commands (`validate-data`, `validate-assets`, `validate-modules`, `hash`).
2. Deterministic capture/replay implementation (input/event recording).
3. Performance benchmarking harness (measure ECS, rendering, physics loops).
4. Error handling upgrade: crash dumps with log snapshots.
5. Expand unit/integration test coverage across services.

**Acceptance Criteria**
- CLI catches invalid data/assets; integrated into CI.
- Capture/replay replicates sample scene deterministically.
- Benchmarks produce repeatable metrics logged via diagnostics.

## Phase 6 – Additional Modules & Extensibility
**Goals:** Prove modular architecture with additional content and platform support.

**Tasks**
1. Build second sample module (e.g., tactics) reusing kernel/services.
2. Create module SDK packaging docs/tools (distribution, versioning).
3. Add additional service adapters (console/mobile input, rendering tweaks).
4. Document module interoperability patterns and risk mitigation.

**Acceptance Criteria**
- Second module runs alongside FactoryPlatformer without kernel changes.
- Module SDK docs/tooling enable third parties to create modules.
- Platform adapters validated on at least one additional target (e.g., mobile input stub).
