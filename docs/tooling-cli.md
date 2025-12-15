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
  - `--format json|yaml` (auto-detect default).
  - `--strict` (fail on warnings).
  - `--out canonical/` (write canonicalized files for diffing).

### `fe-tools validate-assets`
- Inputs: asset catalog manifests.
- Workflow:
  - Verify namespace naming rules per ADR-0005.
  - Check referenced files exist and hashes match (if supplied).
  - Ensure asset types have registered loaders.
  - Detect ID collisions across catalogs.

### `fe-tools validate-modules`
- Inputs: module manifests.
- Workflow:
  - Ensure required fields present.
  - Check component/system names map to actual code assemblies (optional reflection step or metadata file).
  - Validate declared services/phases exist.

### `fe-tools hash`
- Utility to compute canonical hashes for arbitrary data/assets, useful for troubleshooting determinism issues.

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
