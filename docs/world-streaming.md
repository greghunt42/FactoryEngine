# World Streaming & Scene Management

Large games require streaming multiple worlds/scenes without restarting the engine. This document outlines the architecture for scene management and streaming in FactoryEngine.

## Goals
- Support multiple concurrent worlds (e.g., gameplay world + UI world).
- Enable seamless streaming of scenes/levels with deterministic lifecycle.
- Allow modules to hook into load/unload events for resource management.
- Keep ECS data isolated per world while sharing services.

## Concepts
- **World:** ECS container with its own entity/component stores and system scheduler.
- **Scene:** Data definition referencing prefabs/entities to instantiate in a world.
- **World Stack:** Ordered collection of active worlds (e.g., main game, pause overlay).
- **Streaming Request:** Asynchronous load/unload operation that instantiates scenes, preloads assets, and notifies modules.

## Lifecycle
1. `WorldManager` creates base world(s) during boot via `WorldBuilder`.
2. To load a scene:
   - Asset/serialization services parse scene data and produce entity spawn instructions.
   - Preload required assets (prefabs, textures, audio) via asset service.
   - Instantiate entities in batches, optionally over multiple frames to avoid hitches.
3. To unload:
   - Modules receive `OnSceneUnloading` event to clean up state.
   - Entities associated with the scene are destroyed; asset service decrements references.

## Streaming APIs
```csharp
var handle = worldManager.StreamScene(new StreamSceneRequest
{
    WorldId = mainWorld,
    SceneId = Scenes.Platformer.Level1,
    Mode = StreamMode.Additive,
    Priority = StreamPriority.High
});
handle.Completed += OnSceneLoaded;
```
- `StreamMode` options: `Single` (replace current), `Additive`, `Overlay`.
- World streaming can be asynchronous; completion callback fires when assets/entities ready.

## World References
- Entities are world-scoped; systems must operate within a specific world context.
- For cross-world interactions (e.g., UI referencing gameplay), systems communicate via event bus topics or service APIs that specify world IDs.

## Data Considerations
- Scenes include metadata: world ID, dependencies, streaming hints (priority, chunk size).
- Prefabs can declare streaming tags so loader can filter based on platform/perf budgets.

## Debugging & Diagnostics
- World manager exposes stats: active worlds, entity counts per world, streaming queue depth, load times.
- Diagnostics overlay allows toggling worlds on/off for testing.

## Future Work
- Define ADR for streaming lifecycle once implementation choices are made (threading, load partitioning).
- Integrate with capture/replay to ensure streaming remains deterministic.
- Consider world partitioning for large open-world scenes (stream chunks based on player position).
