namespace FactoryEngine.Core.Ecs;

public sealed class EntityManager
{
    private struct EntityMeta
    {
        public uint Generation;
        public bool Alive;
    }

    private readonly List<EntityMeta> _metadata = new();
    private readonly Queue<int> _freeList = new();
    private readonly List<int> _pendingDestroy = new();

    public Entity Create()
    {
        int index;
        if (_freeList.Count > 0)
        {
            index = _freeList.Dequeue();
        }
        else
        {
            index = _metadata.Count;
            _metadata.Add(default);
        }

        var meta = _metadata[index];
        meta.Alive = true;
        _metadata[index] = meta;
        return new Entity(index, meta.Generation);
    }

    public void Destroy(Entity entity)
    {
        if (!IsAlive(entity))
        {
            return;
        }

        _pendingDestroy.Add(entity.Index);
    }

    public bool IsAlive(Entity entity)
    {
        if (entity.Index < 0 || entity.Index >= _metadata.Count)
        {
            return false;
        }

        var meta = _metadata[entity.Index];
        return meta.Alive && meta.Generation == entity.Generation;
    }

    public void Flush(Action<Entity>? onDestroyed = null)
    {
        foreach (var index in _pendingDestroy)
        {
            var meta = _metadata[index];
            if (!meta.Alive)
            {
                continue;
            }

            var entity = new Entity(index, meta.Generation);
            onDestroyed?.Invoke(entity);
            meta.Alive = false;
            meta.Generation++;
            _metadata[index] = meta;
            _freeList.Enqueue(index);
        }

        _pendingDestroy.Clear();
    }

    public int AliveCount
    {
        get
        {
            var count = 0;
            foreach (var meta in _metadata)
            {
                if (meta.Alive)
                {
                    count++;
                }
            }

            return count;
        }
    }

    internal Entity ToEntity(int index)
    {
        var meta = _metadata[index];
        return new Entity(index, meta.Generation);
    }

    internal int Capacity => _metadata.Count;
}
