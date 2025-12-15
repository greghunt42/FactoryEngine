using System;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Ecs.Components;

namespace FactoryEngine.Core.Systems;

public abstract class SystemBase : ISystem
{
    public ComponentAccess Access { get; private set; } = ComponentAccess.None;

    protected World? World { get; private set; }

    protected void DeclareAccess(Action<ComponentAccessBuilder> configure)
    {
        var builder = new ComponentAccessBuilder();
        configure(builder);
        Access = builder.Build();
    }

    public void Run(World world, SystemContext context)
    {
        World = world;
        OnRun(context);
    }

    protected void ForEach<T>(Action<Entity, T> action) where T : struct
    {
        if (World is null)
        {
            throw new InvalidOperationException("World not assigned.");
        }

        foreach (var entry in World.Query<T>())
        {
            action(entry.Entity, entry.Component);
        }
    }

    protected void ForEach<TA, TB>(Action<Entity, TA, TB> action)
        where TA : struct
        where TB : struct
    {
        if (World is null)
        {
            throw new InvalidOperationException("World not assigned.");
        }

        foreach (var entry in World.Query<TA, TB>())
        {
            action(entry.Entity, entry.A, entry.B);
        }
    }

    protected void ForEach(Action<QueryBuilder> configure, Action<Entity> action)
    {
        if (World is null)
        {
            throw new InvalidOperationException("World not assigned.");
        }

        foreach (var entity in World.Query(configure))
        {
            action(entity);
        }
    }

    protected abstract void OnRun(SystemContext context);
}
