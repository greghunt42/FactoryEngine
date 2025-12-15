using FactoryEngine.Core.Ecs;

namespace FactoryEngine.Core.Tests;

public class EntityManagerTests
{
    [Fact]
    public void Create_ReturnsDistinctEntities()
    {
        var manager = new EntityManager();
        var e1 = manager.Create();
        var e2 = manager.Create();
        Assert.NotEqual(e1, e2);
        Assert.True(manager.IsAlive(e1));
        Assert.True(manager.IsAlive(e2));
    }

    [Fact]
    public void Destroy_QueuesEntityUntilFlush()
    {
        var manager = new EntityManager();
        var entity = manager.Create();
        manager.Destroy(entity);
        Assert.True(manager.IsAlive(entity));
        manager.Flush();
        Assert.False(manager.IsAlive(entity));
    }

    [Fact]
    public void Destroy_IncrementsGenerationOnReuse()
    {
        var manager = new EntityManager();
        var entity = manager.Create();
        manager.Destroy(entity);
        manager.Flush();
        var entity2 = manager.Create();
        Assert.Equal(entity.Index, entity2.Index);
        Assert.NotEqual(entity.Generation, entity2.Generation);
    }
}
