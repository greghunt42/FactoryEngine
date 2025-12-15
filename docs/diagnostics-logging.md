# Diagnostics & Logging Guidelines

Reliable diagnostics keep the long-lived engine debuggable. This document defines logging structure, metrics collection, and tooling expectations.

## Objectives
- Consistent, structured logging across kernel and modules.
- Minimal overhead in release builds while enabling deep inspection in dev builds.
- Unified metrics pipeline feeding profilers, editor overlays, and telemetry exporters.

## Logging
- Use a lightweight structured logger with categories (e.g., `ECS`, `Audio`, `Module.Platformer`).
- Log levels: `Trace`, `Debug`, `Info`, `Warn`, `Error`, `Critical`.
- Log entries include timestamp, frame index, entity/system IDs (when relevant), and ADR references for complex behaviors.
- Modules register their own categories but follow the same format.
- Provide sinks: console, rolling file, in-memory ring buffer, optional user-defined sinks.

## Diagnostics Events
- `DiagnosticsService` exposes event hooks: `OnSystemStart`, `OnSystemEnd`, `OnEventBusPublish`, `OnAssetLoad`, etc.
- Profiling overlays subscribe to these events to visualize timings.

## Metrics
- Counters: system runtimes, ECS iteration counts, event bus publishes per topic, asset loads, audio channels active.
- Gauges: entity counts, component counts per type, memory usage per service, command buffer sizes.
- Histograms: frame times, allocator latencies, hot reload durations.
- Export formats: Prometheus text, JSON snapshots, and binary capture for editor playback.

## Capture & Replay
- Deterministic sessions can capture input + event bus traffic + random seeds to replay bugs.
- Diagnostics service coordinates capture lifecycle via CLI/UI commands (`start capture`, `stop capture`).
- Captures store references to ADR IDs and doc versions for historical context.

## Error Handling
- Fatal errors produce crash dumps with recent logs and metrics snapshot.
- Non-fatal validation issues (e.g., module manifest mismatch) raise warnings but allow developers to continue in-editor; build/CI treats them as errors.

## Integration
- Editor overlays request diagnostics data through a well-defined API (web socket or IPC) without blocking the engine.
- CLI tooling (`fe-tools`) can attach to running instances to pull stats (future work).

## Configuration
- Logging verbosity configured via data (per build profile) and runtime console commands.
- Metrics sampling rates adjustable per category to manage overhead.

## Future Topics
- ADR for telemetry/export protocols.
- Security/privacy considerations for telemetry in shipped games.
