# FactoryEngine Architecture Overview

## Purpose
FactoryEngine wraps MonoGame with a stable kernel and a set of plug-in style modules. The goal is to support multiple genres over decades without needing to rewrite the core. This document provides a live snapshot of the engine's structure and is the entry point for any new contributor.

## Guiding Principles
- **Minimal kernel, maximal longevity.** Keep the core tiny, well-tested, and resistant to churn so downstream games remain stable.
- **Hybrid ECS foundation.** Internally, data-oriented ECS drives performance; externally, an optional façade keeps authoring ergonomic.
- **Module safety.** New systems are bolt-on modules that never require kernel edits. The core exposes stable APIs and events for extension.
- **Data-driven content.** Prefabs, scenes, and behaviors live in external data so designers can evolve content without recompiling the engine.
- **Deterministic pipelines.** Ordered phases keep systems predictable and simplify debugging, replay, and multiplayer sync.
- **MonoGame abstraction.** Hide raw MonoGame APIs so games remain insulated from backend churn and platform differences.

## High-Level Structure
```
+-------------------+      +-----------------------+
| Game / Module DLL | <--> | Engine Extension APIs |
+-------------------+      +-----------------------+
                                  |
                               Kernel
                                  |
        +---------------------------------------------------+
        | ECS Core | System Pipeline | Event Bus | Services |
        +---------------------------------------------------+
                                  |
                           MonoGame Backend
```

### Kernel Layer
The kernel is responsible for entity/component storage, world lifecycle, deterministic system execution, and shared services (asset, input, rendering, audio, serialization, logging). It does not know about game-specific components, and it treats MonoGame as an implementation detail hidden behind adapters.

### Extension Layer
Modules add struct-only components, systems that operate on ECS queries, optional event handlers, and data schemas. Modules consume services and events from the kernel but never introduce new dependencies that the kernel must honor.

### Game Layer
Games assemble engine modules, supply data, and optionally expose façade helpers. Games should not reach directly into MonoGame unless supplying a new backend implementation.

## Key Subsystems
- **ECS Core:** sparse-set stores per component type, entity IDs with generation counters, struct-only components for performance.
- **System Pipeline:** ordered phases (Input, Simulation, Physics, AI, Animation, RenderPrep) plus configurable hooks for module-defined phases.
- **Event Bus:** lightweight pub/sub linking modules without hard references.
- **Service Abstractions:** asset, input, rendering, audio, serialization, diagnostics.

## Extensibility Model
1. A module registers its components and systems with the world.
2. Systems declare which phase they run in and their query requirements.
3. Modules subscribe to event bus topics or emit their own events.
4. External data describes prefabs, scenes, and behaviors. Modules provide serializers and validation for their schemas.

## Data Flow Overview
1. **Boot:** kernel loads configuration, modules, and data assets.
2. **Frame Start:** input abstraction collects raw platform events and dispatches them into the ECS world.
3. **Simulation:** deterministic phases update entities; systems exchange messages through the event bus.
4. **Render Prep:** systems populate render command buffers and enqueue audio tasks.
5. **Present:** MonoGame backend flushes buffers to the GPU/audio device.

## Upgrade Path
While sparse-set ECS is the initial strategy, the architecture keeps system interfaces abstract enough to swap in an archetype-based storage layer later. The façade layer also allows migration to scripting or hot-reload workflows without rewriting the kernel.

## Next Steps
- Flesh out subsystem specs: `ecs-core.md`, `system-pipeline.md`, `event-bus.md`.
- Capture decision history in ADRs (storage strategy, ID scheme, module contract).
- Create a design journal for explorations that may not yet be committed to ADRs.
