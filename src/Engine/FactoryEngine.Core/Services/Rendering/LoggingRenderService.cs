using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Rendering;

public sealed class LoggingRenderService : BasicRenderService
{
    public LoggingRenderService(IAssetService assets, TextWriter? writer = null)
        : base(assets, new ConsoleRenderBackend(writer))
    {
    }
}
