using FactoryEngine.Core.Services.Input;

namespace FactoryEngine.Core.Tests;

public class InputServiceTests
{
    [Fact]
    public void SetActionState_FiresEvent()
    {
        var service = new InputService();
        service.RegisterActionMap(new ActionMap
        {
            Name = "default",
            Actions = { new ActionBinding { Name = "jump" } }
        });

        ActionEvent? received = null;
        service.OnActionTriggered += evt => received = evt;

        service.SetActionState("jump", new ActionState(1, true));

        Assert.NotNull(received);
        Assert.Equal("jump", received?.ActionName);
        Assert.True(received?.State.IsPressed);
    }
}
