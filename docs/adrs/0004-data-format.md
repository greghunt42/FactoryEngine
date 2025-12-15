# 0004 - Text-Based Data Format

Date: 2024-01-01

## Status
Accepted

## Context
- Designers and technical artists must author prefabs, scenes, and behaviors without compiling the engine.
- Files need to diff cleanly in source control and support code review.
- Deterministic builds and cross-platform tooling require consistent parsing.
- Some assets (nav meshes, baked lighting) may eventually require binary formats, but most gameplay data is lightweight text.

## Decision
Standardize on JSON/YAML for gameplay data:
- Prefabs, scenes, behavior graphs, and module manifests are stored as text (YAML preferred for readability; JSON for tooling interoperability).
- The serialization pipeline canonicalizes the parsed data (sorted keys, normalized whitespace) before hashing/loading to keep determinism.
- Binary payloads are allowed only as referenced blobs with explicit adapters; they cannot be embedded directly in gameplay data files.

## Consequences
- **Pros:** Easy diffing/merging, straightforward tooling, humans can read/edit files.
- **Cons:** Larger file sizes versus binary; parsing overhead must be mitigated with caching/baking.
- **Follow-ups:** Define CLI tooling for validation and canonicalization; create ADR for binary asset pipeline once requirements harden.

## Alternatives Considered
- **Binary custom format:** Smaller and faster but hard to debug; requires specialized tooling.
- **Database-backed data:** Overkill for most games and complicates mod/community workflows.
