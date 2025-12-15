using System;
using FactoryEngine.Core.Ecs;

namespace FactoryEngine.Core.Ecs.Components;

public readonly ref struct ComponentQuery<TA, TB>
    where TA : struct
    where TB : struct
{
    private readonly EntityManager _entities;
    private readonly SparseSetStore<TA>? _storeA;
    private readonly SparseSetStore<TB>? _storeB;

    internal ComponentQuery(EntityManager entities, SparseSetStore<TA>? storeA, SparseSetStore<TB>? storeB)
    {
        _entities = entities;
        _storeA = storeA;
        _storeB = storeB;
    }

    public Enumerator GetEnumerator() => new Enumerator(_entities, _storeA, _storeB);

    public ref struct Enumerator
    {
        private readonly EntityManager _entities;
        private readonly SparseSetStore<TA>? _storeA;
        private readonly SparseSetStore<TB>? _storeB;
        private int _index;

        internal Enumerator(EntityManager entities, SparseSetStore<TA>? storeA, SparseSetStore<TB>? storeB)
        {
            _entities = entities;
            _storeA = storeA;
            _storeB = storeB;
            _index = -1;
        }

        public bool MoveNext()
        {
            if (_storeA is null || _storeB is null)
            {
                return false;
            }

            _index++;
            while (_index < _storeA.Components.Length)
            {
                var entityIndex = _storeA.EntityIndices[_index];
                if (_storeB.Has(_entities.ToEntity(entityIndex)))
                {
                    return true;
                }

                _index++;
            }

            return false;
        }

        public ComponentPairRef<TA, TB> Current => new ComponentPairRef<TA, TB>(
            _entities.ToEntity(_storeA!.EntityIndices[_index]),
            _storeA!,
            _storeB!);
    }
}

public readonly ref struct ComponentPairRef<TA, TB>
    where TA : struct
    where TB : struct
{
    private readonly Entity _entity;
    private readonly SparseSetStore<TA> _storeA;
    private readonly SparseSetStore<TB> _storeB;

    internal ComponentPairRef(Entity entity, SparseSetStore<TA> storeA, SparseSetStore<TB> storeB)
    {
        _entity = entity;
        _storeA = storeA;
        _storeB = storeB;
    }

    public Entity Entity => _entity;
    public ref TA A => ref _storeA.Get(_entity);
    public ref TB B => ref _storeB.Get(_entity);
}
