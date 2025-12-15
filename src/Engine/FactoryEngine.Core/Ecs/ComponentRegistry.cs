using FactoryEngine.Core.Ecs.Components;

namespace FactoryEngine.Core.Ecs;

public sealed class ComponentRegistry
{
    private readonly Dictionary<Type, IComponentStore> _stores = new();

    public IComponentStore<T> GetOrCreateStore<T>() where T : struct
    {
        if (_stores.TryGetValue(typeof(T), out var store))
        {
            return (IComponentStore<T>)store;
        }

        var newStore = new SparseSetStore<T>();
        _stores[typeof(T)] = newStore;
        return newStore;
    }

    public bool TryGetStore<T>(out SparseSetStore<T>? store) where T : struct
    {
        if (_stores.TryGetValue(typeof(T), out var existing))
        {
            store = (SparseSetStore<T>)existing;
            return true;
        }

        store = null;
        return false;
    }

    public void RemoveAllComponents(Entity entity)
    {
        foreach (var store in _stores.Values)
        {
            store.Remove(entity);
        }
    }

    internal bool Has(Entity entity, Type componentType)
    {
        if (_stores.TryGetValue(componentType, out var store))
        {
            return store.Has(entity);
        }

        return false;
    }
}
