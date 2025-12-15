using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Services.Diagnostics;
using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Services.Serialization;

namespace FactoryEngine.Core.Services;

public sealed class EngineServices
{
    public EngineServices(
        IAssetService assets,
        IInputService input,
        ISerializationService serialization,
        IDiagnosticsService diagnostics,
        IRenderService rendering,
        IAudioService audio)
    {
        Assets = assets;
        Input = input;
        Serialization = serialization;
        Diagnostics = diagnostics;
        Rendering = rendering;
        Audio = audio;
    }

    public IAssetService Assets { get; }
    public IInputService Input { get; }
    public ISerializationService Serialization { get; }
    public IDiagnosticsService Diagnostics { get; }
    public IRenderService Rendering { get; }
    public IAudioService Audio { get; }
}
