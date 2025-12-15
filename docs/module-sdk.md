# Module SDK Guide

This guide explains how to package, version, test, and distribute FactoryEngine modules.

## Goals
- Provide consistent structure so modules remain plug-and-play.
- Enable third parties to validate modules without kernel access.
- Support semantic versioning and dependency management.

## Module Package Layout
```
MyModule/
  module.manifest.yaml
  src/
    MyModule.csproj
  assets/
    catalogs/
    prefabs/
    behaviors/
  docs/
    README.md
    CHANGELOG.md
  tests/
    MyModule.Tests.csproj
```

### module.manifest.yaml
```yaml
name: MyModule
version: 1.2.0
engineVersion: ">=0.2.0"
dependencies:
  - core
components:
  - MyModule.Components.Health
systems:
  - MyModule.Systems.HealthSystem
phases:
  - name: Combat
    insertAfter: Simulation
services:
  requires: [AssetService]
```
- `engineVersion` uses semver range to ensure compatibility.
- `dependencies` list other modules or asset namespaces required.

## Build & Packaging
1. `dotnet build` the module project targeting `.dll`.
2. Run module unit tests via `dotnet test`.
3. Package assets + manifest + binaries into `.zip` or `.pak` with structure above.
4. Generate metadata file containing module version, git commit, ADR references.

## Validation Workflow
- Run `fe-tools validate-modules module.manifest.yaml`.
- Run `fe-tools validate-data assets/prefabs`.
- Run `fe-tools validate-assets assets/catalogs`.
- Optional: run module-specific CLI commands (registered via tooling doc).

## Distribution
- Recommended to publish via NuGet-style feed or custom registry referencing manifest + asset bundle.
- Include README with installation instructions and changelog with semver entries.
- Modules should not embed MonoGame binaries; rely on engine runtime to supply platform dependencies.

## Versioning & Compatibility
- Follow semantic versioning: bump major for breaking API/schema changes, minor for backward-compatible features, patch for fixes.
- Document required ADR IDs when referencing core decisions.
- Provide upgrade notes for consumers (e.g., component schema changes).

## Testing Requirements
- Unit tests covering module systems/components.
- Integration tests using headless world (no rendering) where possible.
- Optional snapshot tests for data validation.

## Documentation Expectations
- README summarizing module features, dependencies, setup.
- CHANGELOG per semver release.
- API reference for public components/systems.

## Publishing Checklist
- [ ] Tests pass.
- [ ] Validation commands succeed.
- [ ] Manifest `engineVersion` updated.
- [ ] Docs/CHANGELOG updated.
- [ ] Package artifacts signed/hashed for distribution (if required).
