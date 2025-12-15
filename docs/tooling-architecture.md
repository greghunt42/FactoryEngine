# Tooling Architecture Overview

This doc explains how CLI tools, editor integrations, and runtime diagnostics interoperate.

## Components
- **fe-tools CLI:** Command-line utility for validation, hashing, project automation (`docs/tooling-cli.md`).
- **Editor Integrations:** IDE/level editor plugins that invoke CLI commands or attach to the engine for live data.
- **Runtime Diagnostics API:** Exposes metrics/logs (per ADR-0006) via IPC/HTTP for tooling consumption.
- **Module Tooling Hooks:** Modules register additional CLI commands or editor panels.

## Data Flow
```
          fe-tools CLI (offline)
           /          |          \
    Data Validation   |    Module Packaging
          |           |          |
Editor Plugins <---- Runtime Diagnostics API ----> Engine Instance
```

## CLI Integration
- Editor plugins call `fe-tools` for on-save validation (prefabs, catalogs) and parse JSON output.
- CI pipelines run the same commands in headless mode to ensure parity.
- CLI config file stores paths to engine assemblies, module directories, and output locations.

## Runtime Diagnostics API
- Engine hosts a lightweight IPC or HTTP endpoint exposing:
  - NDJSON log stream.
  - Prometheus metrics endpoint.
  - Command buffer snapshots via request/response.
- Tooling connects to visualize stats, behavior graphs, physics debug data.

## Module Hooks
- Modules expose additional CLI commands via manifest entry:
```yaml
tooling:
  cli:
    - name: export-navmesh
      assembly: MyModule.Tools.dll
      type: MyModule.Tools.ExportNavmeshCommand
```
- Editors discover module tooling descriptors and render custom panels (defined via JSON schema or UI plugin).

## Security & Isolation
- Tooling processes run out-of-process; they communicate via IPC to avoid coupling.
- Authentication (e.g., token) required for remote diagnostics connections in production builds.

## Future Work
- Define standard protocol (WebSocket/HTTP) for runtime diagnostics streaming.
- Create template for editor plugin referencing CLI + diagnostics.
- Add ADR covering tooling extensibility once concrete implementations exist.
