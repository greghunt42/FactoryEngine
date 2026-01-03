# Module Authoring Template

Use this template when designing a new FactoryEngine module. Replace placeholders with module-specific details and commit alongside code changes.

## Module Summary
- **Name:** `ExampleModule`
- **Purpose:** Short description.
- **Dependencies:** Other modules/services required.

## Manifest
```yaml
name: ExampleModule
version: 1.0.0
engineVersion: ">=0.2.0"
dependencies:
  - core
phases:
  - name: Combat
    insertAfter: Simulation
components:
  - Transform
  - Health
systems:
  - CombatSystem (phase: Combat)
descriptorManifests:
  - ../descriptors/core.descriptors.json
metadataConfigs:
  - ../catalogs/asset-metadata.config.json
services:
  requires:
    - AssetService
  optional:
    - AudioService
```
Describe each manifest field:
- `name` / `version`: semantic version for compatibility checks.
- `phases`: optional custom phases with insertion points.
- `components`: list of struct types provided.
- `systems`: type + phase + ordering metadata.
- `descriptorManifests`: JSON files that describe prefab/component schemas; paths may be relative to the manifest.
- `metadataConfigs`: optional JSON files that extend catalog metadata rules (`textureFormats`, `audioGroups`, `defaultAudioGroup`) for the CLI validators.
- `services`: kernel services the module expects to use.

## Components
For each component:
- Type definition summary.
- Serialization schema (fields, defaults, validation rules).
- Related ADR references.

## Systems
For each system:
- Phase, priority, and query signature.
- Inputs (components/events) and outputs (components/events/services).
- Performance considerations.

## Events
- Events published.
- Events subscribed to.
- Event payload structure and delivery mode.

## Data Assets
- Prefabs, scenes, behavior files included with the module.
- Schema references and versioning plan.

## Testing & Validation
- Unit/system tests required.
- Data validation scripts.
- Instrumentation hooks.

## Open Issues
- Known limitations or deferred work.

Link back to ADR IDs where relevant so future updates trace to their rationale.
