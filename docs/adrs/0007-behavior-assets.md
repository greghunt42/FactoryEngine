# 0007 - Behavior Asset Schema

Date: 2024-01-01

## Status
Proposed

## Context
- AI systems require data-driven behavior definitions (behavior trees, utility nodes).
- Schema must be human-editable, diff-friendly, and extensible for module-specific nodes.
- Need stable identifiers to reference actions/conditions defined in code.

## Decision
Adopt a YAML-based behavior graph schema:
- Behavior asset file contains `name`, `version`, `root`, `nodes` map.
- Nodes specify `type` (selector, sequence, action, condition, decorator) and child references by name.
- Parameters stored as scalar/array values; typed at runtime via node descriptors.
- Each node type is registered by modules with metadata (allowed parameters, default values).
- Behavior assets can include other assets via `fragments` for reuse.

## Consequences
- **Pros:** Readable, versionable data; easy to extend with new node types; aligns with other YAML assets.
- **Cons:** Requires runtime validation to ensure references are valid; YAML indentation errors can cause authoring friction (mitigated via tooling).
- **Follow-ups:** Implement loader/validator as part of AI framework; extend schema for utility curves and GOAP later; update doc if format changes.

## Alternatives Considered
- **JSON:** More tooling, but less friendly for large graphs; still supported via conversion if needed.
- **Binary graph format:** Efficient but not inspectable; would require dedicated editor.
