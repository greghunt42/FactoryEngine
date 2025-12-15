using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Rendering;

public interface IRenderService
{
    void BeginFrame();
    RenderCommandBuffer GetFrameBuffer();
    MaterialId RegisterMaterial(MaterialDescriptor descriptor);
    void Submit(RenderCommandBuffer buffer);
}

public readonly record struct MaterialId(int Value);

public sealed record MaterialDescriptor(string Name);

public sealed class RenderCommandBuffer
{
    private readonly List<SpriteDrawCommand> _sprites = new();

    public RenderCommandBuffer()
    {
    }

    public IReadOnlyList<SpriteDrawCommand> Sprites => _sprites;

    public void Clear() => _sprites.Clear();

    public void DrawSprite(SpriteDrawCommand command) => _sprites.Add(command);
}

public readonly record struct SpriteDrawCommand(
    AssetId Texture,
    float X,
    float Y,
    float Rotation,
    float ScaleX,
    float ScaleY,
    float Layer);
