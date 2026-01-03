# Data Tooling CLI Specification

FactoryEngine needs a command-line tool (`fe-tools`) to validate data, asset catalogs, and module manifests outside the runtime. This document captures requirements for the initial version.

## Goals
- Fast feedback for designers and CI pipelines.
- Deterministic output for integration with build systems.
- Extensible command registry so modules can add custom validators.

## Commands
### `fe-tools validate-data`
- Inputs: directories or files containing prefabs/scenes/behavior data.
- Workflow:
  1. Load component descriptors and schemas from engine + modules.
  2. Parse data files (JSON/YAML) and canonicalize.
  3. Validate required fields, enum values, references to assets/entities.
  4. Emit summary (success/failure) plus detailed diagnostics.
- Flags:
  - `--descriptor-assembly <path>` registers additional component descriptors (defaults to the sample module output).
  - `--descriptor-manifest <path>` loads JSON descriptor manifests so validation can run without compiling module assemblies (defaults to manifests declared by modules under `data/modules`, falling back to `data/descriptors/core.descriptors.json`).
  - `--catalog <path>` loads prefab assets via asset catalogs.
  - `--json <file|->` writes a structured JSON report (stdout when `-` is supplied).
  - `--metadata-config <file>` merges additional metadata rules (same JSON shape as `validate-assets`) so descriptors can enforce module-specific texture/audio constraints.
  - `--strict` treats warnings (e.g., missing data paths, empty results) as errors.
  - `--out <directory>` writes canonicalized prefab JSON for diffing/CI artifacts.
- Current implementation:
  - Discovers descriptors from assemblies (pass `--descriptor-assembly path.dll` or rely on the default FactoryPlatformer build output).
  - Loads descriptor manifests declared in module manifests (sharing the same schema as `validate-modules`) and falls back to `data/descriptors/core.descriptors.json`.
  - Loads prefab assets directly via the asset pipeline when `--catalog <catalog.json>` (file or directory) is supplied; when omitted it automatically discovers catalogs under `data/catalogs`.
  - Component descriptors can call `ValidationContext.RequireAsset` so references (e.g., Sprite textures, audio clips) are validated against the loaded catalogs.
  - Uses the runtime serialization service to deserialize each component and invoke descriptor `Validate` methods, emitting structured errors (and JSON report entries when requested).
  - Applies asset metadata rules from explicit `--metadata-config` files, the workspace-level `data/catalogs/asset-metadata.config.json`, and any module `metadataConfigs`; the resulting `AssetMetadataRules` are handed to descriptors via `ValidationContext.MetadataRules` so audio/texture policies match `validate-assets`.
  - Canonical writer emits deterministic JSON per prefab ID/path so CI can diff generated output.
  - Example: `dotnet run --project src/Tools/FeTools -- validate-data --descriptor-assembly src/Samples/FactoryPlatformer/bin/Debug/net8.0/FactoryPlatformer.dll --catalog data/catalogs/core.catalog.json --json report.json --out canonical data/prefabs`

### `fe-tools validate-assets`
- Inputs: asset catalog manifests.
- Workflow:
  - Load manifests into the shared `AssetService` (via `AssetPipeline`) and register default loaders for prefabs, textures, audio clips, and raw byte blobs.
  - Verify namespace naming rules per ADR-0005 and ensure every referenced file exists relative to the catalog root.
  - Compute SHA-256 hashes for each asset; when the manifest declares `hash`, the command compares it against the computed value and surfaces mismatches.
  - Warn when catalog metadata omits required fields (e.g., texture `format`, audio `group`) so designers can fix descriptors before CI promotes a build.
  - Enforce approved metadata values: texture `format` must be one of `png`, `dds`, `tga`, `bin`, or `placeholder`, and audio `group` must be one of `sfx`, `music`, `ui`, `ambience`, `dialog`, or `voice`. Invalid values surface warnings (and errors under `--strict`) so teams stay aligned with the shared mix schema unless additional formats/groups are supplied via `--metadata-config`.
  - Emit per-asset diagnostics (missing files, failed loaders, hash mismatches) and surface metadata so CI dashboards can visualize catalog health. This now includes `SoundBank` assets (JSON manifests); the command loads each bank, validates referenced clip IDs/groups against the registered catalogs, and produces a coverage summary (per-group counts plus unreferenced audio clips) so audio routing issues are caught alongside prefabs/textures/audio clips.
- Flags:
  - `--json <file|->` writes a structured report (stdout when `-` is supplied) that includes per-asset hashes, metadata, and severity-tagged validation issues.
  - `--coverage-json <file|->` writes just the sound-bank coverage summary (group counts + unreferenced clips) so CI can ingest it separately without the full asset report.
  - `--coverage-ndjson <file|->` emits coverage metrics as NDJSON events (summary, per-group counts, unreferenced clip entries) for streaming into dashboards without parsing large JSON payloads.
  - `--fail-unreferenced-clips` treats any unreferenced audio clips as errors so CI can enforce 100% coverage.
  - `--require-groups <g1,g2,...>` fails when any listed audio group has zero sounds across all banks (case-insensitive), ensuring critical buses always have coverage.
  - `--strict` treats warnings (missing catalog directories, metadata gaps, etc.) as failures.
  - `--options <file>` loads defaults from a JSON file (same property names as the CLI options) so CI/CD pipelines can keep catalog directories, metadata configs, and report paths in one place. CLI arguments always win when both are supplied.
- Options:
  - `--fix-hashes` rewrites manifest files with the freshly computed hashes when they differ from what is declared.
  - `--metadata-config <file>` merges additional metadata values from JSON files so modules can extend the approved texture formats/audio groups without changing the CLI. Each file accepts:
    ```json
    {
      "textureFormats": ["ktx2", "ktx2-basis"],
      "audioGroups": ["narration", "cinematic"],
      "defaultAudioGroup": "narration"
    }
    ```
    When no file is supplied the command automatically loads `data/catalogs/asset-metadata.config.json` (if present) and any `metadataConfigs` declared in module manifests under `data/modules`.
- Example `--options` file:
  ```json
  {
    "inputs": ["data/catalogs"],
    "jsonReportPath": "out/assets-report.json",
    "strictMode": false
  }
  ```
  Invoking `fe-tools validate-assets --options build/asset-options.json` keeps CI commands short while still letting engineers override any value with explicit CLI flags.
- Inputs may be individual manifest files or directories; when no input is supplied the command scans `data/catalogs` recursively so the sample data works out-of-the-box.
- Usage today: `fe-tools validate-assets data/catalogs`.

### `fe-tools validate-all`
- Batch driver that runs `validate-assets`, `validate-data`, and `validate-modules` using a single JSON configuration so CI pipelines can keep arguments centralized.
- Usage: `fe-tools validate-all --config build/validate-all.json [--stop-on-failure]`. The config file is JSON with optional arrays for each command:
  ```json
  {
    "validateAssets": ["--options", "build/assets.json"],
    "validateData": ["--descriptor-manifest", "data/descriptors/core.descriptors.json"],
    "validateModules": ["data/modules"],
    "stopOnFirstFailure": false
  }
  ```
- Entries are passed verbatim to the respective commands (so the example above runs assets with an options file, data with a descriptor manifest, and modules over `data/modules`). When `--stop-on-failure` (or `stopOnFirstFailure` in the config) is set, the driver stops after the first failing command.
- Sample configuration: the repo ships `build/validate-all.json` so contributors can read/edit the exact payload CI uses. Right now it validates the shipped catalogs and prefabs:
  ```json
  {
    "validateAssets": [
      "data/catalogs"
    ],
    "validateData": [
      "--descriptor-manifest",
      "data/descriptors/core.descriptors.json",
      "data/prefabs"
    ],
    "stopOnFirstFailure": true
  }
  ```
  Add `validateModules` entries (and additional flags like `--json`) as new modules land; keeping the defaults in this file ensures desktops and CI never drift.

#### CI integration
- The repo ships `build/validate-all.json`, which runs asset validation over `data/catalogs` and prefab validation (with `core.descriptors.json`) over `data/prefabs`, with `stopOnFirstFailure` enabled so CI halts immediately when any validator fails. Add or adjust the `validateModules` payload once module manifests begin landing under `data/modules`.
- CI/headless usage mirrors local runs: `dotnet run --project src/Tools/FeTools -- validate-all --config build/validate-all.json --stop-on-failure`. For convenience, call `./build/validate-all.sh` (macOS/Linux) or `build\validate-all.ps1` (Windows/PowerShell) so both developers and CI agents share the exact command + config. The CLI still preserves per-command exits/reporting semantics, so existing JSON report flags continue to work when included in the config array.
- Keep the config (and wrapper scripts) checked in so desktops and build agents share flag drift; prefer editing the config over inlining long command arguments in pipelines. When longer flag sets are needed, `--options` and descriptor/metadata manifests slot naturally into the `validateAssets`/`validateData` arrays without changing the driver invocation.
- GitHub Actions example (from `.github/workflows/ci.yml`):
  ```yaml
  - name: Validate Assets/Data/Modules
    run: ./build/validate-all.sh
  ```
  Windows agents can call `build\validate-all.ps1` instead. Because both wrappers simply forward CLI arguments, CI can add extra flags (e.g., `--json artifacts/validate.json`) without duplicating logic.

### `fe-tools validate-modules`
- Inputs: module manifests.
- Workflow:
  - Ensure required fields present.
  - Check component/system names map to actual code assemblies (optional reflection step or metadata file).
  - Validate declared services/phases exist.
- Current implementation:
  - Accepts manifest files or directories (defaults to `data/modules`); supports both JSON and YAML manifests shaped like `docs/module-template.md`, including optional `descriptorManifests` entries that point to prefab schema files.
  - `--assembly <path>` option loads assemblies so component/system type names can be resolved via reflection.
  - Validates dependency graphs: missing module references and cycles surface as errors before runtime.
  - `--json <file|->` writes a validation report that now includes `graph.nodes` (per-module dependencies + missing references) and `graph.cycles` ready for CI dashboards, and `--strict` treats warnings (missing manifests, optional service typos, etc.) as failures.
  - Example: `dotnet run --project src/Tools/FeTools -- validate-modules --assembly src/Samples/FactoryPlatformer/bin/Debug/net8.0/FactoryPlatformer.dll modules/sample.module.json`

### `fe-tools hash`
- Utility to compute canonical hashes for arbitrary data/assets, useful for troubleshooting determinism issues.
- Flags:
  - `--algo <sha256|sha1|md5>` chooses the hashing algorithm (default `sha256`).
  - `--json <file|->` writes a structured report that includes the algorithm, timestamp, and every hashed file with relative paths/sizes.
- Inputs can be individual files or directories; directories are scanned recursively and results are printed in the format `<hash>  <relative-path>` for easy piping into diff tools.

## Extensibility
- Modules can register additional commands via discovery (e.g., `.dll` exports or script hooks).
- Command metadata includes help text, option schema, and dependencies.

## Output
- Default human-readable text.
- `--json` flag emits machine-readable reports (per-file status, errors, warnings) for CI integration.

## Performance
- Parallelize validation across CPU cores when safe (per file).
- Cache schema metadata between runs (watch for invalidation when assemblies change).

## Roadmap
1. Implement `validate-data` + `validate-assets` to unblock content workflows.
2. Add canonicalization writer and hash diffing.
3. Integrate with IDE/editor plugins for on-save validation.
