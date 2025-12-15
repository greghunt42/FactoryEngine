using System;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Ecs;

namespace FactoryEngine.Core.Ecs.Components;

public readonly ref struct ComponentQuery<T> where T : struct
{
    private readonly EntityManager _entities;
    private readonly Span<T> _components;
    private readonly Span<int> _entityIndices;

    internal ComponentQuery(EntityManager entities, SparseSetStore<T>? store)
    {
        _entities = entities;
        if (store is null)
        {
            _components = Span<T>.Empty;
            _entityIndices = Span<int>.Empty;
        }
        else
        {
            _components = store.Components;
            _entityIndices = store.EntityIndices;
        }
    }

    public Enumerator GetEnumerator() => new Enumerator(_entities, _components, _entityIndices);

    public ref struct Enumerator
    {
        private readonly EntityManager _entities;
        private readonly Span<T> _components;
        private readonly Span<int> _entityIndices;
        private int _index;

        internal Enumerator(EntityManager entities, Span<T> components, Span<int> entityIndices)
        {
            _entities = entities;
            _components = components;
            _entityIndices = entityIndices;
            _index = -1;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _components.Length;
        }

        public ComponentRef<T> Current => new ComponentRef<T>(_entities.ToEntity(_entityIndices[_index]), _components, _index);
    }
}

public readonly ref struct ComponentRef<T> where T : struct
{
    private readonly Entity _entity;
    private readonly Span<T> _components;
    private readonly int _index;

    internal ComponentRef(Entity entity, Span<T> components, int index)
    {
        _entity = entity;
        _components = components;
        _index = index;
    }

    public Entity Entity => _entity;
    public ref T Component => ref _components[_index];
}
