using System.Collections.Generic;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Rendering;

public class BasicRenderService : IRenderService
{
    private readonly IAssetService _assets;
    private readonly IRenderBackend _backend;
    private readonly RenderCommandBuffer _buffer = new();
    private readonly List<RenderedSprite> _lastFrame = new();

    public BasicRenderService(IAssetService assets, IRenderBackend? backend = null)
    {
        _assets = assets ?? throw new ArgumentNullException(nameof(assets));
        _backend = backend ?? new ConsoleRenderBackend();
    }

    public IReadOnlyList<RenderedSprite> LastFrame => _lastFrame;

    public void BeginFrame()
    {
        _buffer.Clear();
        _backend.BeginFrame();
    }

    public RenderCommandBuffer GetFrameBuffer() => _buffer;

    public MaterialId RegisterMaterial(MaterialDescriptor descriptor) => new(0);

    public void Submit(RenderCommandBuffer buffer)
    {
        ArgumentNullException.ThrowIfNull(buffer);

        _lastFrame.Clear();
        foreach (var command in buffer.Sprites)
        {
            TextureAsset? texture = null;
            try
            {
                var handle = _assets.Load<TextureAsset>(command.Texture);
                texture = handle.Value ?? throw new InvalidOperationException("Texture asset returned null.");
            }
            catch (Exception ex)
            {
                _backend.OnError($"Failed to load texture '{command.Texture}': {ex.Message}");
                continue;
            }

            var rendered = new RenderedSprite(command, texture);
            _lastFrame.Add(rendered);
            _backend.DrawSprite(rendered);
        }

        _backend.EndFrame();
    }
}
