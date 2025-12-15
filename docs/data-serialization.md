# Data & Serialization Guidelines

FactoryEngine is data-driven: prefabs, scenes, and behaviors live outside the compiled codebase. This document defines how data flows into the engine and what guarantees modules must provide.

## Goals
- Human-editable formats (JSON/YAML) with schema validation.
- Deterministic serialization for reproducible builds and network sync.
- Version-tolerant formats so long-lived games can evolve data safely.

## Component Metadata
- Each component type provides a descriptor containing:
  - Component name and version.
  - Serialization adapter (read/write).
  - Default values and validation rules.
- Descriptors register with the world during module initialization.
- Components use plain data fields only; avoid polymorphism.

## Prefabs & Archetypes
- Prefabs are JSON/YAML documents describing an entity composition:
```yaml
name: Player
components:
  Transform:
    position: [0, 0, 0]
    rotation: 0
  Health:
    max: 100
    current: 100
```
- Prefabs can inherit from other prefabs via `extends` so modules share common setups.
- Prefab baking resolves inheritance and produces deterministic component lists.

## Scenes
- Scenes reference prefabs and add placement data (position, tags, metadata).
- Scene files include references to external assets by stable IDs instead of file paths to improve portability.

## Behavior Scripts/Data
- Modules define their own schema files (e.g., dialog trees, AI state machines) and register validators.
- Behavior data should link to entities via tags or entity references defined by GUID-like stable IDs that are resolved at load time.

## Serialization Flow
1. Loader parses JSON/YAML into intermediate objects.
2. Schema validators ensure required fields and types.
3. Component adapters instantiate struct components and add them to entities.
4. Version adapters migrate old data formats before validation.

## Versioning Strategy
- Each schema includes a `version` field.
- Modules provide upgrade paths for old versions (e.g., migration functions or data rewriting scripts).
- Breaking changes require bumping the major version and documenting migrations in the design journal.

## Determinism & Hashing
- After validation, data is canonicalized (sorted keys, normalized whitespace) and hashed.
- Hashes feed hot-reload checks, build caching, and network sync to detect mismatches.

## Tooling Requirements
- Provide CLI tooling to validate data offline (future work).
- Editor integrations should rely on the same descriptors to maintain parity.

## Open Questions
- Binary blob support for large data assets (e.g., nav meshes) needs a follow-up ADR.
- Need to define standardized identifier format for referencing external assets.
