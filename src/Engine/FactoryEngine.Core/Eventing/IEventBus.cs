namespace FactoryEngine.Core.Eventing;

public interface IEventBus
{
    Subscription Subscribe<T>(Action<T> handler, int priority = 0);
    void Publish<T>(in T payload);
}

public readonly record struct Subscription(Guid Id);
