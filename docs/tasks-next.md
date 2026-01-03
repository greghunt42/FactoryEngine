# Next Tasks

Runner polish v2 landed (scene selector, loading spinner, improved diagnostics) and the asset pipeline now drives both FactoryPlatformer and `fe-tools validate-all`. The remaining work for this milestone splits into two tracks.

## Phase 3 – Gameplay Expansion
- (Done) Finish a basic gameplay loop: the shared LoopResetController now restarts scenes for both the MonoGame runner and headless pipeline, so the goal prefab + respawn flow broadcast win/loss strings back into the overlay/event log automatically.
- Broaden content coverage: author at least one new collectible tier and a hazard/enemy prefab, wire audio cues for pickups/deaths, and add textures/prefabs so AssetService exercises more loader types. (Legendary cache, ancient relics, sentinel + turret hazards now cover multiple catalogs; lava pits + crusher gates + Charged Core pickups + plasma fields + guardian orbs + the new frost biome keep cranking on variety—more still welcome.)
- Tighten physics + controls: expose jump-shortening logic to input, add air-dodge or wall-slide experiments, and cover the new behavior in `InputMovementSystemTests`/`PhysicsSystemTests`. (Acceleration ramps + boundary wall-slides + tests landed; jump-cut + air-dodge tuning + feedback trail/SFX are now configurable/tested.)
- (Done) Expand telemetry/UI: high score persistence + event history now feed the overlay/headless runner, and CI can assert scene success via the new headless flags.

## Phase 2 Follow-Up – Tooling + CI
- (Done) Documented the `validate-all` workflow (`docs/tooling-cli.md`) with the checked-in sample config plus CI guidance, including wrapper scripts and the GitHub Actions step that runs `./build/validate-all.sh`.
- (Done) Repo-level scripts live under `build/validate-all.sh|ps1`, and CI now invokes them before the headless smoke test so catalog/prefab/module failures block builds consistently.
- (Done) CLI backlog audit recorded in `docs/tooling-status.md`; future CLI enhancements will track under later roadmap phases.
