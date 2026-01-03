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
- Content slices now include “Ancient Relics” collectibles and “Turret Traps” hazards to exercise prefab/audio/texture loaders beyond the base level set.
- Scenes: small test map defined via YAML.
- Behavior assets: patrol BT, chase BT.
- Sound banks: jump, coin pickup, basic music loop.

## Testbed Application
- MonoGame desktop runner loading the engine + module + sample scene.
- Debug overlay displaying ECS stats, logs, command buffer contents.
- Hot reload demo (modify prefab, see change live).
- Persist telemetry (high score and recent events) so the runner + headless mode can surface gameplay health over time.

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

## Controls & Movement Tuning
- `PhysicsBody` exposes `runSpeed`, `groundAccel`, and `airAccel` so prefabs can control move targets and acceleration ramps independently. Defaults (35 u/s run, 320 ground accel, 180 air accel) deliver responsive ground motion with softer air adjustments.
- Wall-slide prototyping uses `wallSlideSpeed` and `wallSlideStick` on `PhysicsBody`; when the player presses into a scene boundary while airborne, downward velocity clamps to the configured slide speed for the stick duration. Entering a slide emits a short audio cue and renders an overlaid effect so the state is visually obvious even during headless captures, and pressing jump during the slide performs a wall jump using `wallJumpSpeed` to push away from the wall. Wall jumps respect `wallJumpCooldown` so mashing jump doesn’t spam impulses, while `airControlExp` shapes how aerial steering ramps up.
- The sample’s `data/config/game.config.json` file now includes a `playerTuning` section so designers can tweak these numbers without rebuilding prefabs; the MonoGame overlay lists the active values every frame for easy iteration.
- Jump cutting remains available via `jumpCut`, and `AirDodge` continues to handle horizontal bursts; future experiments can extend these components without touching systems.

## Telemetry & Headless Validation
- High score data is stored at `data/config/highscore.json` and loaded at bootstrap time so both the runner overlay and headless logs can display cumulative progress. The file is rewritten whenever the score increases; failures are swallowed so gameplay keeps running in restricted environments.
- `FactoryPlatformerGameState` records an event history ring buffer. The overlay lists recent events with age stamps, while headless runs print the same log before exiting. This history makes CI/headless runs debuggable without a renderer.
- Headless CLI flags:
  - `--scene <id>` selects the scene defined in `data/config/game.config.json`.
  - `--min-score <value>` exits with code `1` when the final score is below the threshold (useful for ensuring collectibles/goals were reached).
  - `--expect-event "<text>"` fails when no event message contains the supplied substring (case-insensitive), allowing CI to assert on narrative beats like “Goal reached!”.
  - Combine with `--headless` to script validations: `dotnet run --project src/Samples/FactoryPlatformer -- --headless --scene headless-demo --script data/scripts/headless-victory.json --min-score 50 --expect-event victory` runs the deterministic headless scene that picks up the shard and touches the goal for CI.
- GitHub Actions runs the headless command after `dotnet test`; add additional assertions there as the sample slice gains more gameplay coverage.
