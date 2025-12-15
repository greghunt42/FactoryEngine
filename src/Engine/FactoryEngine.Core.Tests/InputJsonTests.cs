using System.IO;
using FactoryEngine.Core.Services.Input;

namespace FactoryEngine.Core.Tests;

public class InputJsonTests
{
    [Fact]
    public void LoadActionMapFromJson_RegistersActions()
    {
        var json = """
        {
          "name": "default",
          "actions": [ { "name": "jump" } ]
        }
        """;
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);

        var service = new InputService();
        service.LoadActionMapFromJson(path);

        Assert.Equal(default, service.GetActionState("jump"));
    }
}
