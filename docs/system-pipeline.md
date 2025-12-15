# System Pipeline Specification

The pipeline enforces deterministic ordering across all simulation work while remaining extensible for modules. Each frame flows through well-known phases where systems can register their logic.

## Default Phases
1. **Input** – Collect platform input, convert to abstraction events, update input components.
2. **Simulation** – Core gameplay logic, timers, resource systems, status effects.
3. **Physics** – Collision detection, resolution, kinematics.
4. **AI** – Decision making, tactical planners, behavior trees.
5. **Animation** – Pose updates, blend trees, procedural motion.
6. **RenderPrep** – Build render commands, cull objects, populate GPU buffers.

Phases execute strictly in order, and systems within a phase are ordered by registration priority to keep behavior reproducible.

## Custom Phases
- Modules can request named phases by declaring them in their manifest.
- New phases specify an insertion point (before/after an existing phase) to keep global ordering stable.
- If multiple modules insert the same phase name, the engine reuses it.

## Scheduling Rules
- Systems declare read/write access for each component type they touch.
- Scheduler uses access metadata to detect hazardous ordering (e.g., two writers to the same component). Conflicts require explicit ordering or phase separation.
- Long-running systems may yield work to worker threads, but the pipeline waits for completion before moving to the next phase.

## Frame Flow
```
BeginFrame
  -> Input Phase
  -> Simulation Phase
  -> Physics Phase
  -> AI Phase
  -> Animation Phase
  -> RenderPrep Phase
EndFrame -> Present via MonoGame
```

## Hooks
- `OnFrameStart` / `OnFrameEnd` events allow instrumentation and deterministic capture.
- Modules can register per-phase callbacks for debugging (e.g., validate components after Physics).

## Tuning & Configuration
- Pipeline order is stored in data/config so builds can enable/disable phases for specific platforms or game types.
- Per-phase tick rates are supported (e.g., run Physics at fixed 60 Hz, Simulation at variable frame rate) by scheduling catch-up ticks.

## Future Work
- Investigate task graphs per phase for better core utilization.
- Define replay hooks so deterministic captures can be collected per phase.
