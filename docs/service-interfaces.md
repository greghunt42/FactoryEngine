# Service Interface Sketches

These sketches provide initial interface definitions for core services to help guide implementation.

## Asset Service
```csharp
public interface IAssetService
{
    AssetHandle<T> Load<T>(AssetId assetId) where T : class;
    ValueTask<AssetHandle<T>> LoadAsync<T>(AssetId assetId);
    void Release(AssetHandle handle);
    event Action<AssetId> AssetReloaded;
}
```
- `AssetHandle` wraps reference counting and exposes `Value`/`Hash`.

## Serialization Service
```csharp
public interface ISerializationService
{
    void RegisterDescriptor<T>(IComponentDescriptor<T> descriptor) where T : struct;
    EntityBuilder InstantiatePrefab(PrefabId prefabId, World world);
    PrefabData LoadPrefab(Stream stream);
}
```
- `EntityBuilder` allows batched creation of entities/components.

## Input Service
```csharp
public interface IInputService
{
    void LoadActionMap(ActionMap map);
    void EnableContext(string contextName);
    ActionState GetActionState(Entity entity, string actionName);
    event Action<InputEvent> OnActionTriggered;
}
```

## Rendering Service
```csharp
public interface IRenderService
{
    RenderCommandBuffer GetFrameBuffer(World world);
    MaterialId RegisterMaterial(MaterialDescriptor descriptor);
    void Submit(RenderCommandBuffer buffer);
}
```

## Audio Service
```csharp
public interface IAudioService
{
    void PlaySound(SoundId id, AudioParams parameters = default);
    void PlayMusicPlaylist(string playlistId);
    void SetGroupVolume(string groupId, float value);
}
```

## Diagnostics Service
```csharp
public interface IDiagnosticsService
{
    ILogger CreateLogger(string categoryName);
    void RecordMetric(string name, double value, MetricType type, ReadOnlySpan<KeyValuePair<string,string>> labels = default);
    CaptureHandle StartCapture(CaptureOptions options);
}
```

## Event Bus
```csharp
public interface IEventBus
{
    Subscription Subscribe<T>(Action<T> handler, int priority = 0);
    void Publish<T>(in T evt);
}
```

These interfaces will evolve during implementation, but the sketches provide a shared vocabulary for initial coding efforts.
