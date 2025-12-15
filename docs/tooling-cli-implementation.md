# fe-tools Implementation Plan

This plan outlines the steps to build the `fe-tools` CLI described in `docs/tooling-cli.md`.

## Project Setup
1. Create `src/Tools/FeTools/FeTools.csproj` (console app) within the solution.
2. Choose CLI framework (System.CommandLine or Spectre.Cli) for argument parsing.
3. Set up dependency injection container to register engine services/schemas for validation commands.

## Command Implementations
### validate-data
- Load engine + module assemblies to discovery component descriptors.
- Traverse directories, parse YAML/JSON, canonicalize, run schema validation.
- Output human-readable summary and optional JSON report (`--json`).
- Implement caching for descriptors to speed up incremental runs.

### validate-assets
- Parse catalog manifests, ensure namespace uniqueness, verify file existence/hashes.
- Validate asset type has loader registered; warn on unknown types.
- Support `--fix-hashes` to update manifest hashes.

### validate-modules
- Parse module manifest, ensure version range compatibility, validate dependency graph.
- Optionally load module assembly to ensure systems/components exist.

### hash
- Compute canonical hash for given files or directories using same canonicalization as data loader.

## Extensibility
- Command registry reads module-provided tooling descriptors (per `docs/tooling-architecture.md`).
- Modules compile tooling commands into separate assemblies loaded on demand.

## Output & Logging
- Use shared logging infrastructure (NDJSON option) with CLI-friendly formatting by default.
- Provide exit codes: `0` success, `1` validation errors, `2` configuration errors.

## Testing
- Unit tests for canonicalization, manifest parsing, CLI option handling.
- Integration tests using sample module assets.

## Distribution
- Publish CLI as dotnet tool (`dotnet tool install factoryengine.tools`).
- Include version info and commit hash in CLI output for reproducibility.

## Future Enhancements
- Watch mode for live validation during development.
- Editor plugin scaffolding that wraps CLI commands.
- Remote validation (CLI connects to running engine instance for context).
