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
- `assets`: map from logical name to metadata.
  - `type`: logical asset type (Texture2D, SpriteFont, Prefab, Scene, BehaviorGraph, etc.).
  - `path`: relative path within the catalog root or bundle.
  - `hash`: optional precomputed content hash for validation/caching.
  - `tags`: optional array for tooling queries.
  - Type-specific fields (e.g., `streaming`, `compression`).

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
- Scenes/prefabs referencing assets must validate against loaded catalogs during data import.

## Extensibility
- Modules can register custom asset types with loaders and validators.
- Catalog manifest schema evolution is tracked via ADRs; new fields must be backward compatible or include migration scripts.
