# Asset Catalog Specification

Asset catalogs bridge logical asset IDs (`namespace:asset_name`) to physical resources. This document defines catalog structure, lifecycle, and integration with the asset service.

## Goals
- Stable, human-readable manifests for every asset namespace.
- Deterministic resolution regardless of packaging (loose files, archives, bundles).
- Support hot reload and build-time validation.

## Catalog Manifest
Each namespace ships a manifest file (YAML or JSON) describing the assets it exports:
```yaml
namespace: platformer
version: 1
root: ../assets
assets:
  hero_idle:
    type: Texture2D
    path: art/hero_idle.png
    tags: [character, animation]
    hash: f3c1b4...
  hero_theme:
    type: AudioTrack
    path: audio/hero_theme.ogg
    streaming: true
```

### Fields
- `namespace`: unique identifier, matching ADR-0005 rules (ASCII, lowercase).
- `version`: schema version for migrations.
- `root`: root directory (relative to the manifest) used to resolve `path` fields. Defaults to the manifest's directory.
- `assets`: map from logical name to metadata.
  - `type`: logical asset type (Texture2D, SpriteFont, Prefab, Scene, BehaviorGraph, etc.).
  - `path`: relative path within the catalog root or bundle.
  - `hash`: optional SHA-256 content hash for validation/caching (lowercase hex). `fe-tools validate-assets` recomputes hashes to catch drift.
  - `tags`: optional array for tooling queries.
  - Type-specific fields (e.g., `streaming`, `compression`).
- Texture metadata must declare a `format` chosen from the approved list (`png`, `dds`, `tga`, `bin`, `placeholder`), and audio clips should declare a `group` from the shared mix schema (`sfx`, `music`, `ui`, `ambience`, `dialog`, `voice`). Catalog validation surfaces warnings for anything outside these sets (treatable as errors with `--strict`) so teams stay aligned. Modules can extend the approved values without modifying the CLI by passing `--metadata-config path/to/formats.json` to `fe-tools validate-assets` or `fe-tools validate-data`, and by dropping configs under `data/catalogs/asset-metadata.config.json` or listing them in module `metadataConfigs`; both commands auto-discover these files. Each config file uses a JSON shape with `textureFormats`, `audioGroups`, and optional `defaultAudioGroup`.
- `SoundBank` entries are regular catalog assets (`type: "SoundBank"`) that point at JSON manifests under `data/soundbanks`. The engine loads them through `SoundBankJsonLoader` and hands the resulting `SoundBank` to `IAudioService`.

## Resolution Flow
1. Asset service loads all catalog manifests at boot (from disk, bundles, or remote sources).
2. When code requests `platformer:hero_idle`, the service locates the catalog entry and delegates to the registered type loader.
3. If multiple catalogs export the same ID, the engine fails fast unless explicitly overridden via configuration.

## Hot Reload
- Catalogs watch their manifest files and underlying assets for changes.
- When a file changes, the catalog recomputes hashes and notifies the asset service, which emits hot-reload events.

## Packaging
- Build pipeline can bake catalogs into binary bundles, but the logical manifest still exists (possibly embedded as JSON) for deterministic lookups.
- Catalogs may include `includes` to compose large namespaces from smaller fragments.

## Validation
- CLI tool reads manifests, ensures all `path` references exist, validates hashes, and enforces naming rules.
- Metadata requirements (e.g., texture `format`, audio `group`) are validated; missing fields raise warnings that become errors under `--strict`.
- Scenes/prefabs referencing assets must validate against loaded catalogs during data import.
- Structured CLI reports include computed hashes + manifest metadata so CI dashboards can diff catalog state over time.

## Extensibility
- Modules can register custom asset types with loaders and validators.
- Catalog manifest schema evolution is tracked via ADRs; new fields must be backward compatible or include migration scripts.

## Engine Integration
- `AssetCatalogManifest.LoadFromJson` converts the YAML/JSON manifest into runtime `AssetCatalog` instances (JSON is currently implemented).
- Default loaders (`Prefab`, `Texture`, `Audio`, `Bytes`) are registered via `AssetPipeline.RegisterDefaultLoaders`.
- A reference manifest lives under `data/catalogs` (with optional nested namespaces). `AssetCatalogDiscovery` recursively scans this directory so FactoryPlatformer and `fe-tools` can auto-register every available catalog without additional CLI flags.
