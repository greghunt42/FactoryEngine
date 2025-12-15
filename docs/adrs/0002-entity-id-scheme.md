# 0002 - Entity ID Scheme

Date: 2024-01-01

## Status
Accepted

## Context
- Entities must be lightweight handles that can be copied freely, stored in data, and validated quickly.
- Long-running simulations risk use-after-free bugs if stale IDs remain in systems or serialized data.
- We need room for millions of concurrent entities while staying within typical CPU cache line sizes.

## Decision
Use 64-bit IDs composed of `index` and `generation` segments:
- Lower bits store the dense index into entity metadata arrays.
- Upper bits store a generation counter incremented whenever an index is recycled.
- Comparing an `Entity` to metadata validates whether the handle is still alive.

## Consequences
- **Pros:** O(1) validation, simple storage (arrays indexed by entity index), allocations amortized, safe recycling.
- **Cons:** Increases ID size relative to 32-bit handles, so external serialization must handle 64-bit values explicitly.
- **Follow-ups:** Document serialization format for entity references and ensure tooling can display `index:generation` pairs.

## Alternatives Considered
- **GUIDs:** Globally unique, but large (128 bits) and slow to compare; no generation safety.
- **Pointer-based handles:** Unsafe across serialization boundaries and prone to dangling references.
