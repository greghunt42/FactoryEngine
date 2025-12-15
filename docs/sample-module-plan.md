# Sample Module & Testbed Plan

To validate the engine architecture early, we will build a lightweight "FactoryPlatformer" sample module and testbed project. This document outlines goals, scope, and tasks.

## Objectives
- Exercise ECS, system pipeline, event bus, rendering, audio, and input services end-to-end.
- Provide reference implementation for module authors (companion to `docs/module-template.md`).
- Supply automated tests and scenarios for regression coverage.

## Components & Systems
- `Transform2D`, `Velocity2D`, `SpriteAnimator`, `InputState`, `PhysicsBody2D`, `Health`.
- Systems:
  - Input mapping -> `InputState` components (Input phase).
  - Movement & gravity (Simulation).
  - Simple AABB physics/collision (Physics).
  - AI: patrolling enemies using behavior framework (AI phase).
  - Animation state machine projecting frames (Animation).
  - RenderPrep building sprite draw commands.

## Data Assets
- Prefabs: player, enemy, platforms, collectibles.
- Scenes: small test map defined via YAML.
- Behavior assets: patrol BT, chase BT.
- Sound banks: jump, coin pickup, basic music loop.

## Testbed Application
- MonoGame desktop runner loading the engine + module + sample scene.
- Debug overlay displaying ECS stats, logs, command buffer contents.
- Hot reload demo (modify prefab, see change live).

## Validation Tasks
1. Write integration tests verifying pipeline phase order and system side effects.
2. Use `fe-tools validate-data` on sample assets in CI.
3. Create scripted scenario to capture deterministic replay (inputs + events) and ensure results match.

## Timeline (rough)
1. **Milestone A:** Core ECS/services implemented, sample module skeleton loads (entities spawn, no gameplay).
2. **Milestone B:** Movement + physics + rendering working; sample scene playable.
3. **Milestone C:** Audio + AI behaviors integrated; hot reload demo.
4. **Milestone D:** Documentation + tests polished; package sample as reference.

## Future Expansion
- Additional sample modules (tactics, shooter) to validate extensibility.
- Use sample modules as automated benchmarks.
