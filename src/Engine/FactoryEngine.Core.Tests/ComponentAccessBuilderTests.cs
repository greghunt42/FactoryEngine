using FactoryEngine.Core.Systems;

namespace FactoryEngine.Core.Tests;

public class ComponentAccessBuilderTests
{
    [Fact]
    public void Builder_GeneratesReadWriteSets()
    {
        var builder = new ComponentAccessBuilder();
        builder.Reads<TestComponentA>()
               .Writes<TestComponentB>();

        var access = builder.Build();

        Assert.Contains(typeof(TestComponentA), access.ReadComponents);
        Assert.Contains(typeof(TestComponentB), access.WriteComponents);
    }

    private struct TestComponentA;
    private struct TestComponentB;
}
