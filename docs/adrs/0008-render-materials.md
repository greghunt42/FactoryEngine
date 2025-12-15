# 0008 - Rendering Materials & Shader Abstraction

Date: 2024-01-01

## Status
Proposed

## Context
- Rendering facade needs a stable way to reference shaders/blend states without exposing MonoGame internals.
- Modules should be able to define custom materials (e.g., lit sprites, outlines) while keeping command buffer simple.
- Need to support future backend swaps.

## Decision
Introduce a logical `MaterialId` referencing a material descriptor:
- Material descriptor includes shader asset ID, render state (blend, depth), texture bindings, and parameter layout.
- Rendering service manages material registry; modules register descriptors at load time.
- Render commands reference `MaterialId` instead of raw `Effect` instances.
- Material parameters supplied via structured `MaterialParams` objects (strongly typed, minimal boxing).

## Consequences
- **Pros:** Clean abstraction for modules, easier to batch commands by material, backend-agnostic.
- **Cons:** Requires descriptor registration boilerplate; more upfront work for modules wanting custom shaders.
- **Follow-ups:** Implement material registry in rendering service; define serialization format for material descriptors; extend ADR if 3D pipeline introduces more complex states.

## Alternatives Considered
- **Direct `Effect` references:** Simpler but couples modules to MonoGame and prevents backend swaps.
- **Hard-coded material enums:** Fast but inflexible for user-defined materials.
