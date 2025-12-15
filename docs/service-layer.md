# Service Layer Overview

The service layer bundles cross-cutting abstractions (asset, input, rendering, audio, serialization, diagnostics) that modules consume without touching MonoGame directly. This document defines responsibilities and extension points for each service.

## Guiding Goals
- Stable interfaces that change rarely.
- Pluggable MonoGame adapters so services can be unit-tested and swapped for different platforms.
- Clear lifecycle (initialization, per-frame updates, shutdown).

## Core Services

### Asset Service
- Resolves logical asset IDs per ADR-0005.
- Handles caching, hot-reload notifications, and asset version hashes.
- Supports synchronous load for critical assets and async/prefetch APIs for large data.

### Input Service
- Collects platform events (keyboard, mouse, gamepad, touch) and translates them into abstract actions.
- Provides per-device state queries and event streams for the ECS input components.
- Supports rebinding profiles stored in data files.

### Rendering Service
- Exposes a command buffer API for RenderPrep systems to enqueue draw calls without dealing with MonoGame APIs.
- Manages GPU resources, sprite batches, shader pipelines, and presentation timing.
- Keeps frame stats for diagnostics.

### Audio Service
- Wraps MonoGame audio (SoundEffect, Song) behind logical channel groups.
- Supports positional audio metadata supplied by ECS systems.
- Provides ducking/mixing controls and streaming for large tracks.

### Serialization Service
- Owns component descriptors, prefab/scene loading, and canonicalization per `docs/data-serialization.md`.
- Integrates schema validators and version migration hooks.

### Diagnostics/Logging Service
- Structured logging with component/system scopes.
- Performance counters for ECS iteration, service operations, and event bus timings.
- Hooks for editor overlays or external profilers.

## Lifecycle
1. Kernel constructs services during engine boot using configuration.
2. Modules receive service references when registering.
3. Services expose `BeginFrame/EndFrame` hooks to integrate with the pipeline.
4. On shutdown, services dispose resources deterministically.

## Extensibility
- Custom services can be registered by modules via manifests; they must implement a common `IService` lifecycle interface.
- Platform-specific adapters live in separate assemblies to keep the core platform agnostic.

## Future Work
- Document asset catalog manifests and caching policies.
- Define service provider/locator mechanics (DI container vs manual wiring).
- Add ADRs for rendering and audio command buffer formats once prototypes exist.
