# Service Interface Sketches

These sketches provide initial interface definitions for core services to help guide implementation.

## Asset Service
```csharp
public interface IAssetService
{
    void RegisterCatalog(AssetCatalog catalog);
    void RegisterLoader<T>(IAssetLoader<T> loader) where T : class;
    AssetHandle<T> Load<T>(AssetId assetId) where T : class;
    ValueTask<AssetHandle<T>> LoadAsync<T>(AssetId assetId);
    void Release<T>(AssetHandle<T> handle) where T : class;
    event Action<AssetId>? AssetReloaded;
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
    IReadOnlyList<SoundPlayback> ActiveSounds { get; }
    event Action<SoundPlayback>? SoundPlayed;
    event Action<SoundPlayback>? SoundStopped;

    void PlaySound(string soundId, AudioParams parameters = default);
    void Update(float deltaTime);
    void PlayMusicPlaylist(string playlistId);
    void SetGroupVolume(string groupId, float value);
    void RegisterSoundBank(SoundBank bank);
    bool TryResolveSound(string bankName, string soundName, out SoundDefinition definition);
    void StopSound(Guid id);
    void SetAssetResolver(Func<AssetId, bool>? resolver);
}

public readonly record struct AudioParams(float Volume = 1f, float Pitch = 0f, float LifetimeSeconds = 0f);
```
The resolver hook lets the audio service validate `SoundBank` asset references against the same catalogs that drive serialization/descriptors.

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
