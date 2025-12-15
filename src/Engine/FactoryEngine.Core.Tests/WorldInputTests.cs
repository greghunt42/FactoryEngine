using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Input;

namespace FactoryEngine.Core.Tests;

public class WorldInputTests
{
    [Fact]
    public void World_ExposesInputService()
    {
        var input = new InputService();
        input.RegisterActionMap(new ActionMap { Name = "default", Actions = { new ActionBinding { Name = "jump" } } });

        var world = new WorldBuilder().WithInput(input).Build();
        world.Input.SetActionState("jump", new ActionState(1, true));

        var state = world.Input.GetActionState("jump");
        Assert.True(state.IsPressed);
    }
}
