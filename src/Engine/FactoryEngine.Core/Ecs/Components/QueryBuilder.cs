using System.Collections.Generic;

namespace FactoryEngine.Core.Ecs.Components;

public sealed class QueryBuilder
{
    private readonly List<Type> _all = new();
    private readonly List<Type> _any = new();
    private readonly List<Type> _none = new();

    public QueryBuilder All<T>() where T : struct
    {
        _all.Add(typeof(T));
        return this;
    }

    public QueryBuilder Any<T>() where T : struct
    {
        _any.Add(typeof(T));
        return this;
    }

    public QueryBuilder None<T>() where T : struct
    {
        _none.Add(typeof(T));
        return this;
    }

    internal Type[] AllTypes => _all.ToArray();
    internal Type[] AnyTypes => _any.ToArray();
    internal Type[] NoneTypes => _none.ToArray();
}
