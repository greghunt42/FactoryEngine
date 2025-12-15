using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Rendering;

public sealed class LoggingRenderService : IRenderService
{
    private readonly RenderCommandBuffer _buffer = new();
    private readonly TextWriter _writer;

    public LoggingRenderService(TextWriter? writer = null)
    {
        _writer = writer ?? Console.Out;
    }

    public void BeginFrame()
    {
        _buffer.Clear();
    }

    public RenderCommandBuffer GetFrameBuffer() => _buffer;

    public MaterialId RegisterMaterial(MaterialDescriptor descriptor) => new MaterialId(0);

    public void Submit(RenderCommandBuffer buffer)
    {
        foreach (var sprite in buffer.Sprites)
        {
            _writer.WriteLine($"Draw sprite {sprite.Texture} at ({sprite.X}, {sprite.Y}) layer {sprite.Layer}");
        }
    }
}
