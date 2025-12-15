using FactoryEngine.Core.Engine;

namespace FactoryEngine.Core.Tests;

public class WorldBuilderTests
{
    [Fact]
    public void Build_UsesProvidedName()
    {
        var world = new WorldBuilder().WithName("TestWorld").Build();
        Assert.Equal("TestWorld", world.Name);
    }
}
