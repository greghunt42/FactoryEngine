# Networking & Multiplayer Considerations

While networking is out of scope for the initial kernel release, architectural choices should keep multiplayer support feasible. This document captures assumptions and early guidance.

## Goals
- Enable future deterministic lockstep or client/server models without core rewrites.
- Keep systems deterministic and deterministic-capture friendly.
- Isolate networking concerns via services/modules so kernel stays clean.

## High-Level Models
1. **Deterministic Lockstep:** Clients run identical simulations, exchanging input frames. Requires strict determinism and capture/replay infrastructure (already planned).
2. **Client/Server:** Authoritative server runs simulation; clients receive state snapshots/deltas.
3. **Hybrid:** Server + prediction/rollback.

## Architectural Hooks
- **Determinism:** ECS systems must avoid nondeterministic APIs; random usage must go through seeded RNG services.
- **Capture/Replay:** Use capture files for debugging desyncs.
- **Networking Service:** Future module providing transport abstraction (UDP/TCP/RPC) and synchronization helpers.
- **Event Bus Integration:** Networking module can subscribe to events or produce ones (e.g., `NetworkCommandReceived`).

## State Serialization
- Components must support deterministic serialization/deserialization; consider adding `INetSerializable` metadata for components requiring network sync.
- Snapshot pipeline should leverage existing serialization descriptors to avoid duplication.

## Latency & Prediction
- Engine should allow systems to run at different tick rates (already possible via pipeline scheduler).
- Prediction modules may require rewinding components; plan for component history buffers or copy-on-write snapshots.

## Security
- Validate external inputs through networking module before mutating ECS state.
- Provide hooks for authoritative validation (e.g., server verifying client commands).

## Future Work
- ADR for networking architecture when scope is defined.
- Design netcode module interfaces (input buffers, snapshot delta compression).
- Evaluate open-source netcode libs for integration or adapter layer.
