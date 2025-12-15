# Capture & Replay Specification

FactoryEngine requires deterministic capture/replay for debugging, regression testing, and multiplayer verification. This document outlines requirements and architecture.

## Goals
- Capture input, random seeds, and relevant events to reproduce a session deterministically.
- Replay captures offline or against live builds for regression tests.
- Minimal runtime overhead when capture is disabled.

## Capture Data
- **Metadata:** engine version, module versions, ADR IDs, date/time, user tags.
- **Input Stream:** ordered list of input events with timestamps/frame indices.
- **Random Seeds:** initial seeds per world/system; optional reseeds for deterministic RNGs.
- **Event Bus Stream (optional):** selected topics for debugging.
- **State Snapshots (optional):** periodic world hash or component subset for validation.

## Architecture
```
CaptureController
  - StartCapture(config)
  - RecordInput(event)
  - RecordEventBus(topic, payload)
  - StopCapture()
ReplayController
  - LoadCapture(file)
  - FeedInputs(world)
  - Verify(StateHash)
```

- Controllers integrate with diagnostics service for status and logging.
- Capture files stored as binary with header + chunked streams (consider using MessagePack or custom TLV format).

## Workflow
1. Developer enables capture (CLI command, console, or API).
2. Engine records data to file while running normally.
3. Replay mode loads capture, feeds inputs into world each frame, blocks live input.
4. Optional verifications compare world hash per frame; mismatches logged for debugging.

## Tooling Integration
- `fe-tools replay --capture file.cap` runs headless replay and reports pass/fail.
- Editor can visualize capture timeline and scrub through frames.

## Determinism Requirements
- Systems must avoid nondeterministic APIs (wall clock, random without seeded RNG).
- Diagnostics service logs determinism violations (e.g., event order mismatch).
- Capture file includes determinism metadata for future reference.

## Future Work
- ADR for capture file format once prototype exists.
- Network sync integration (use capture to drive lockstep simulations).
- Selective component recording for targeted debugging.
