# 0003 - Module Contract

Date: 2024-01-01

## Status
Accepted

## Context
- Engine must support genre-specific extensions without modifying kernel code.
- Modules need a predictable way to register components, systems, and data schemas with the world.
- Core services (asset, input, rendering, etc.) should be accessible without exposing MonoGame directly.

## Decision
Define a formal module contract:
- Modules declare a manifest describing required services, provided components, systems, and custom pipeline phases.
- Registration occurs at world boot; modules receive handles to the world, event bus, and service interfaces.
- Modules may only interact with the kernel through published APIs/events; no internal type access.
- Data schemas must be registered so the serialization system can validate external content.

## Consequences
- **Pros:** Keeps kernel decoupled, simplifies module onboarding, enables selective inclusion per game.
- **Cons:** Requires careful versioning of module interfaces and manifests; adds upfront boilerplate for module authors.
- **Follow-ups:** Provide module template docs and tooling to validate manifests.

## Alternatives Considered
- **Ad-hoc static registration:** Quick to start but encourages tight coupling and kernel edits.
- **Friend assemblies:** Easier short-term but violates long-lived stability requirement by exposing internals.
