using System.Collections.Generic;
using System.Linq;

namespace FactoryEngine.Core.Systems;

public sealed class ComponentAccessBuilder
{
    private readonly HashSet<Type> _reads = new();
    private readonly HashSet<Type> _writes = new();

    public ComponentAccessBuilder Reads<T>() where T : struct
    {
        _reads.Add(typeof(T));
        return this;
    }

    public ComponentAccessBuilder Writes<T>() where T : struct
    {
        _writes.Add(typeof(T));
        return this;
    }

    public ComponentAccess Build()
    {
        return new ComponentAccess(_reads.ToArray(), _writes.ToArray());
    }
}
