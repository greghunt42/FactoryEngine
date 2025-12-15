namespace FactoryEngine.Core.Eventing;

public sealed class SimpleEventBus : IEventBus
{
    private readonly Dictionary<Type, List<Handler>> _handlers = new();

    public Subscription Subscribe<T>(Action<T> handler, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(handler);
        var key = typeof(T);
        if (!_handlers.TryGetValue(key, out var list))
        {
            list = new List<Handler>();
            _handlers[key] = list;
        }

        var subscription = new Subscription(Guid.NewGuid());
        list.Add(new Handler(priority, subscription, obj => handler((T)obj)));
        list.Sort(static (a, b) => b.Priority.CompareTo(a.Priority));
        return subscription;
    }

    public void Publish<T>(in T payload)
    {
        if (_handlers.TryGetValue(typeof(T), out var list))
        {
            foreach (var handler in list)
            {
                handler.Callback(payload!);
            }
        }
    }

    private sealed record Handler(int Priority, Subscription Subscription, Action<object> Callback);
}
