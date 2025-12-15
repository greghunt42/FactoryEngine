# Design Journal

Use this log to capture explorations, open questions, and decision context before an ADR is written. New entries append to the bottom with the most recent first.

---

**2024-xx-xx — Kickoff**
- Established hybrid ECS direction with sparse-set storage and deterministic pipeline.
- Identified core docs to produce: architecture overview, ECS spec, system pipeline, event bus, ADRs.
- Next: define ADR templates and serialization spec.

**2024-xx-xx — Repository scaffolding**
- Created `FactoryEngine.sln` with core engine, tools, and sample projects.
- Added shared build props and placeholder service/event bus implementations.
- Stubbed sample runner and fe-tools CLI; logging via NDJSON now available for experiments.

**2024-xx-xx — ECS component storage**
- Integrated sparse-set component stores and component registry with World APIs.
- Entities now flush components on destruction; lifecycle covered by unit tests.
- Next focus: query iterators and system scheduler scaffolding.
- Implemented basic component query enumerator returning entity/component refs to unblock system iteration work.
- Added initial system scheduler scaffolding with phase + priority ordering and tied it into `World.Tick`.
- Scheduler now enforces component access conflicts, paving way for deterministic system ordering rules.
- Scheduler now emits per-system timing metrics via diagnostics service; WorldBuilder wiring supports custom diagnostics.
- Added ComponentAccessBuilder + SystemBase helper so systems can declare read/write requirements ergonomically.
- Added query builder/enumerable to support All/Any/None filtering, aligning with planned system ergonomics.
- SystemBase now exposes ForEach helpers to iterate ECS queries, improving ergonomics for system authors.
- Built first pass serialization service with descriptor registration and prefab instantiation backed by in-memory definitions; added unit tests.
- WorldBuilder now wires serialization service, and World exposes `InstantiatePrefab`, enabling modules to spawn data-defined entities via registered descriptors.
- Bootstrapped asset service with catalog registration and basic loading semantics; tests cover namespace lookups and handles.
- Asset service now plugs into WorldBuilder/World so gameplay code can resolve assets from registered catalogs.
- Added initial InputService scaffolding with action map registration, state tracking, and events to support future adapters.
- Input service now plugs into WorldBuilder/World so gameplay systems can access action states via `world.Input`.
- Introduced EngineServices container so worlds expose consolidated access to assets/input/serialization/diagnostics.
- SystemContext now carries EngineServices, giving systems streamlined access to assets/input/serialization/diagnostics during execution.
- FactoryPlatformer sample now has Transform2D/Velocity2D components, descriptors, and a MovementSystem driving entities spawned via prefabs.
- Added MovementSystem unit test to ensure Transform2D updates respond to Velocity2D, validating sample pipeline.
- Implemented initial rendering command buffer with sprite draw commands and tests, prepping for rendering service integration.
- World now wires rendering service alongside other EngineServices, exposing `world.Rendering` for future systems.
- Added Sprite component/descriptor and RenderingSystem to sample; program now enqueues sprite draw commands each tick.
- Added audio service scaffolding with sound bank registration and resolution; integrated NullAudioService updates.
- Audio service now part of EngineServices/world builder; world exposes `Audio` for future sound playback.
- Added AudioSystem in sample that registers a sound bank and plays a sound once; program output now logs sprite draws and prepares for audio hooks.
- Serialization service can now load prefab definitions from JSON files; FactoryPlatformer program consumes `data/prefabs/player.json`.
- Input service loads action maps from JSON; FactoryPlatformer now has input-driven movement scaffolding via InputMovementSystem.
