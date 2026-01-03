using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Ecs.Components;
using FactoryEngine.Core.Services;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Services.Diagnostics;
using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Services.Serialization;
using FactoryEngine.Core.Systems;

namespace FactoryEngine.Core.Engine;

/// <summary>
/// Represents an ECS world. Manages entity lifecycle and will later host component/system state.
/// </summary>
public sealed class World
{
    private readonly EntityManager _entityManager = new();
    private readonly ComponentRegistry _components = new();
    private readonly SystemScheduler _scheduler;
    private readonly EngineServices _services;
    private readonly string _name;

    internal World(string name, IDiagnosticsService diagnostics, ISerializationService serialization, IAssetService assets, IInputService input, IRenderService render, IAudioService audio)
    {
        _name = name;
        _scheduler = new SystemScheduler(diagnostics);
        _services = new EngineServices(assets, input, serialization, diagnostics, render, audio);
    }

    public string Name => _name;

    public Entity CreateEntity()
    {
        return _entityManager.Create();
    }

    public void DestroyEntity(Entity entity)
    {
        _entityManager.Destroy(entity);
    }

    public bool IsAlive(Entity entity) => _entityManager.IsAlive(entity);

    public ref T AddComponent<T>(Entity entity) where T : struct
    {
        return ref _components.GetOrCreateStore<T>().Add(entity);
    }

    public bool HasComponent<T>(Entity entity) where T : struct
    {
        return _components.TryGetStore<T>(out var store) && store is not null && store.Has(entity);
    }

    public ref T GetComponent<T>(Entity entity) where T : struct
    {
        return ref _components.GetOrCreateStore<T>().Get(entity);
    }

    public void RemoveComponent<T>(Entity entity) where T : struct
    {
        if (_components.TryGetStore<T>(out var store) && store is not null)
        {
            store.Remove(entity);
        }
    }

    public ComponentQuery<T> Query<T>() where T : struct
    {
        _components.TryGetStore<T>(out var store);
        return new ComponentQuery<T>(_entityManager, store);
    }

    public ComponentQuery<TA, TB> Query<TA, TB>()
        where TA : struct
        where TB : struct
    {
        _components.TryGetStore<TA>(out var storeA);
        _components.TryGetStore<TB>(out var storeB);
        return new ComponentQuery<TA, TB>(_entityManager, storeA, storeB);
    }

    public QueryEnumerable Query(Action<QueryBuilder> configure)
    {
        var builder = new QueryBuilder();
        configure(builder);
        return new QueryEnumerable(_entityManager, _components, builder.AllTypes, builder.AnyTypes, builder.NoneTypes);
    }

    public void Tick(float deltaTime)
    {
        _services.Audio.Update(deltaTime);
        _entityManager.Flush(entity => _components.RemoveAllComponents(entity));
        _scheduler.Run(this, deltaTime);
    }

    public void FlushDestroyedEntities()
    {
        _entityManager.Flush(entity => _components.RemoveAllComponents(entity));
    }

    internal EntityManager EntityManager => _entityManager;

    public void RegisterSystem(ISystem system, SystemPhase phase, int priority = 0)
    {
        _scheduler.Register(system, phase, priority);
    }

    public PrefabInstance InstantiatePrefab(string prefabId)
    {
        return _services.Serialization.InstantiatePrefab(prefabId, this);
    }

    public ISerializationService Serialization => _services.Serialization;
    public IAssetService Assets => _services.Assets;
    public IInputService Input => _services.Input;
    public IDiagnosticsService Diagnostics => _services.Diagnostics;
    public IRenderService Rendering => _services.Rendering;
    public IAudioService Audio => _services.Audio;
    public EngineServices Services => _services;
}
