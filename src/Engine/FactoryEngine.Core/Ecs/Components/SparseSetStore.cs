namespace FactoryEngine.Core.Ecs.Components;

using System;
using System.Runtime.InteropServices;

public sealed class SparseSetStore<T> : IComponentStore<T> where T : struct
{
    private readonly List<int> _denseToEntity = new();
    private readonly List<T> _denseData = new();
    private int[] _sparse = Array.Empty<int>();

    public Type ComponentType => typeof(T);

    internal Span<T> Components => CollectionsMarshal.AsSpan(_denseData);
    internal Span<int> EntityIndices => CollectionsMarshal.AsSpan(_denseToEntity);

    public ref T Add(Entity entity)
    {
        EnsureCapacity(entity.Index + 1);
        if (TryGetDenseIndex(entity, out var existing))
        {
            return ref CollectionsMarshal.AsSpan(_denseData)[existing];
        }

        var denseIndex = _denseData.Count;
        _denseData.Add(default);
        _denseToEntity.Add(entity.Index);
        _sparse[entity.Index] = denseIndex;
        return ref CollectionsMarshal.AsSpan(_denseData)[denseIndex];
    }

    public bool Has(Entity entity)
    {
        if (entity.Index < 0 || entity.Index >= _sparse.Length)
        {
            return false;
        }

        var denseIndex = _sparse[entity.Index];
        if (denseIndex < 0 || denseIndex >= _denseData.Count)
        {
            return false;
        }

        return _denseToEntity[denseIndex] == entity.Index;
    }

    public ref T Get(Entity entity)
    {
        var denseIndex = GetDenseIndex(entity);
        return ref CollectionsMarshal.AsSpan(_denseData)[denseIndex];
    }

    public void Remove(Entity entity)
    {
        if (!TryGetDenseIndex(entity, out var denseIndex))
        {
            return;
        }

        var lastIndex = _denseData.Count - 1;
        if (denseIndex != lastIndex)
        {
            CollectionsMarshal.AsSpan(_denseData)[denseIndex] = CollectionsMarshal.AsSpan(_denseData)[lastIndex];
            CollectionsMarshal.AsSpan(_denseToEntity)[denseIndex] = CollectionsMarshal.AsSpan(_denseToEntity)[lastIndex];
            _sparse[_denseToEntity[denseIndex]] = denseIndex;
        }

        _denseData.RemoveAt(lastIndex);
        _denseToEntity.RemoveAt(lastIndex);
        _sparse[entity.Index] = -1;
    }

    private int GetDenseIndex(Entity entity)
    {
        if (!TryGetDenseIndex(entity, out var denseIndex))
        {
            throw new InvalidOperationException("Entity does not own component");
        }

        return denseIndex;
    }

    private bool TryGetDenseIndex(Entity entity, out int denseIndex)
    {
        denseIndex = -1;
        if (entity.Index < 0 || entity.Index >= _sparse.Length)
        {
            return false;
        }

        var candidate = _sparse[entity.Index];
        if (candidate < 0 || candidate >= _denseData.Count)
        {
            return false;
        }

        if (_denseToEntity[candidate] != entity.Index)
        {
            return false;
        }

        denseIndex = candidate;
        return true;
    }

    private void EnsureCapacity(int target)
    {
        if (target <= _sparse.Length)
        {
            return;
        }

        var newSize = Math.Max(target, Math.Max(4, _sparse.Length * 2));
        var newArray = new int[newSize];
        Array.Fill(newArray, -1);
        if (_sparse.Length > 0)
        {
            Array.Copy(_sparse, newArray, _sparse.Length);
        }

        _sparse = newArray;
    }
}
