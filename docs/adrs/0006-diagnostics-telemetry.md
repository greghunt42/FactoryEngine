# 0006 - Diagnostics & Telemetry Export Format

Date: 2024-01-01

## Status
Accepted

## Context
- Engine requires consistent diagnostics for long-term maintenance.
- Metrics must feed editor overlays, CLI tools, and potentially production telemetry without duplicating pipelines.
- Need a compact, structured format readable by both humans and machines.
- Integration with external tools (Prometheus, JSON log aggregators) should be straightforward.

## Decision
Adopt a dual-format export strategy:
1. **Structured Log Events:** NDJSON (newline-delimited JSON) with standardized fields (`timestamp`, `frame`, `category`, `level`, `message`, `properties`). Suitable for log ingestion pipelines and human inspection.
2. **Metrics Stream:** Prometheus text exposition format served via diagnostics API/endpoint for pull-based tooling; binary capture files store snapshot series for replay.

- Diagnostics service emits NDJSON to configured sinks (console/file/socket) and retains a ring buffer in memory for crash dumps.
- Metrics backend maintains labeled counters/gauges/histograms and exposes them via HTTP or IPC.

## Consequences
- **Pros:** Widely supported formats, easy to parse, incremental adoption, can be inspected with existing tools.
- **Cons:** JSON logs are larger than binary; Prometheus text requires translation for push-based systems.
- **Follow-ups:** Provide adapters for other telemetry backends (e.g., ETW, OpenTelemetry) as optional modules.

## Alternatives Considered
- **Custom binary trace format:** Efficient but would require bespoke tooling and limit community adoption.
- **Plain text logs:** Simple but lose structured metadata critical for long-term maintenance.
