using FactoryEngine.Core.Services;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Services.Diagnostics;
using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Services.Serialization;

namespace FactoryEngine.Core.Tests;

public class EngineServicesTests
{
    [Fact]
    public void EngineServices_StoresReferences()
    {
        var services = new EngineServices(
            new AssetService(),
            new InputService(),
            new SerializationService(),
            new NullDiagnosticsService(),
            new NullRenderService(),
            new NullAudioService());

        Assert.NotNull(services.Assets);
        Assert.NotNull(services.Input);
        Assert.NotNull(services.Serialization);
        Assert.NotNull(services.Diagnostics);
        Assert.NotNull(services.Rendering);
    }
}
