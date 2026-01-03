using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Services.Diagnostics;
using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Services.Serialization;

namespace FactoryEngine.Core.Engine;

public sealed class WorldBuilder
{
    private string _name = "World";
    private IDiagnosticsService _diagnostics = new NullDiagnosticsService();
    private ISerializationService _serialization = new SerializationService();
    private IAssetService _assets = new AssetService();
    private IInputService _input = new InputService();
    private IRenderService _render = new NullRenderService();
    private IAudioService _audio = new AudioService();

    public WorldBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public WorldBuilder WithDiagnostics(IDiagnosticsService diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        return this;
    }

    public WorldBuilder WithSerialization(ISerializationService serialization)
    {
        _serialization = serialization ?? throw new ArgumentNullException(nameof(serialization));
        return this;
    }

    public WorldBuilder WithAssets(IAssetService assets)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        return this;
    }

    public WorldBuilder WithInput(IInputService input)
    {
        _input = input ?? throw new ArgumentNullException(nameof(input));
        return this;
    }

    public WorldBuilder WithRendering(IRenderService render)
    {
        _render = render ?? throw new ArgumentNullException(nameof(render));
        return this;
    }

    public WorldBuilder WithAudio(IAudioService audio)
    {
        _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        return this;
    }

    public World Build()
    {
        if (_audio is IAudioAssetConsumer consumer)
        {
            consumer.SetAssetService(_assets);
        }

        return new World(_name, _diagnostics, _serialization, _assets, _input, _render, _audio);
    }
}
