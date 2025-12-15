namespace FactoryEngine.Core.Ecs.Components;

public interface IComponentStore
{
    Type ComponentType { get; }
    bool Has(Entity entity);
    void Remove(Entity entity);
}

public interface IComponentStore<T> : IComponentStore where T : struct
{
    ref T Add(Entity entity);
    ref T Get(Entity entity);
}
