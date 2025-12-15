using System;
using System.Collections.Generic;

namespace FactoryEngine.Core.Ecs.Components;

public readonly ref struct QueryEnumerable
{
    private readonly EntityManager _entities;
    private readonly ComponentRegistry _registry;
    private readonly Type[] _all;
    private readonly Type[] _any;
    private readonly Type[] _none;

    internal QueryEnumerable(EntityManager entities, ComponentRegistry registry, Type[] all, Type[] any, Type[] none)
    {
        _entities = entities;
        _registry = registry;
        _all = all;
        _any = any;
        _none = none;
    }

    public Enumerator GetEnumerator() => new Enumerator(_entities, _registry, _all, _any, _none);

    public ref struct Enumerator
    {
        private readonly EntityManager _entities;
        private readonly ComponentRegistry _registry;
        private readonly Type[] _all;
        private readonly Type[] _any;
        private readonly Type[] _none;
        private int _entityIndex;

        internal Enumerator(EntityManager entities, ComponentRegistry registry, Type[] all, Type[] any, Type[] none)
        {
            _entities = entities;
            _registry = registry;
            _all = all;
            _any = any;
            _none = none;
            _entityIndex = -1;
        }

        public bool MoveNext()
        {
            while (true)
            {
                _entityIndex++;
                if (_entityIndex >= _entities.Capacity)
                {
                    return false;
                }

                var entity = _entities.ToEntity(_entityIndex);
                if (!_entities.IsAlive(entity))
                {
                    continue;
                }

                if (!MatchesAll(entity) || !MatchesAny(entity) || MatchesNone(entity))
                {
                    continue;
                }

                Current = entity;
                return true;
            }
        }

        public Entity Current { get; private set; }

        private bool MatchesAll(Entity entity)
        {
            foreach (var type in _all)
            {
                if (!_registry.Has(entity, type))
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesAny(Entity entity)
        {
            if (_any.Length == 0)
            {
                return true;
            }

            foreach (var type in _any)
            {
                if (_registry.Has(entity, type))
                {
                    return true;
                }
            }

            return false;
        }

        private bool MatchesNone(Entity entity)
        {
            foreach (var type in _none)
            {
                if (_registry.Has(entity, type))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
