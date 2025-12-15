using System;
using FactoryEngine.Core.Engine;

namespace FactoryEngine.Core.Systems;

public interface ISystem
{
    ComponentAccess Access { get; }
    void Run(World world, SystemContext context);
}

public readonly record struct ComponentAccess(
    Type[] ReadComponents,
    Type[] WriteComponents)
{
    public static ComponentAccess None => new(Array.Empty<Type>(), Array.Empty<Type>());
}
