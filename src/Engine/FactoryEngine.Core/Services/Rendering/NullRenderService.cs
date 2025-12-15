namespace FactoryEngine.Core.Services.Rendering;

public sealed class NullRenderService : IRenderService
{
    private readonly RenderCommandBuffer _buffer = new();
    private int _nextMaterialId = 1;

    public void BeginFrame()
    {
        _buffer.Clear();
    }

    public RenderCommandBuffer GetFrameBuffer() => _buffer;

    public MaterialId RegisterMaterial(MaterialDescriptor descriptor) => new MaterialId(_nextMaterialId++);

    public void Submit(RenderCommandBuffer buffer)
    {
        // Intentionally empty until real rendering backend exists.
    }
}
