# Roadmap & Backlog

This document tracks near-term priorities and provides hand-off friendly tasks. Update frequently as architecture evolves.

## Roadmap Themes
1. **Kernel Foundations:** ECS implementation, pipeline scheduler, event bus runtime.
2. **Service Layer:** Asset, input, rendering, audio abstractions.
3. **Data Tooling:** Serialization pipeline, prefab/scene authoring tools, validation CLI.
4. **Module Ecosystem:** Core gameplay modules (platformer, RPG), module SDK tooling.

## Backlog (High Priority)
- [P0-1] Implement ECS sparse-set storage per ADR-0001 (EntityManager, SparseSetStore, tests, benchmarks).
- [P0-2] Build system pipeline scheduler with phase registration + conflict detection.
- [P0-3] Implement event bus runtime aligned with the spec and diagnostics.
- [P0-4] Define serialization component descriptors and hook into loader.
- [P0-5] Asset service MVP loading catalogs, resolving IDs, emitting hot reload events.

## Backlog (Next)
- [P1-1] Input service: action map loader, desktop adapter, Input system integration.
- [P1-2] Rendering facade: command buffer + MonoGame backend skeleton.
- [P1-3] Audio service: sound bank loader, play/stop commands.
- [P1-4] `fe-tools validate-data` and `validate-assets` commands (stub -> functional).
- [P1-5] Sample module components/systems for movement/physics/rendering.

## Documentation Tasks
- ADR for data serialization format (binary assets vs text) once requirements solidify. ✅ (ADR-0004)
- Module SDK guide detailing packaging, testing, and distribution. _(todo)_
- Tooling architecture doc for CLI/editor integration. _(todo)_
- Behavior asset schema ADR after AI framework prototype. _(todo)_

## Handoff Guidance
Each task should specify:
- Related docs/ADRs (e.g., ADR-0001, ecs-core spec).
- Definition of done (tests, docs, sample data).
- Validation plan (benchmarks, sample modules, data fixtures).
