using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class RenderingSystem : SystemBase
{
    public RenderingSystem()
    {
        DeclareAccess(builder => builder
            .Reads<Transform2D>()
            .Reads<Sprite>());
    }

    protected override void OnRun(SystemContext context)
    {
        var buffer = context.Services.Rendering.GetFrameBuffer();
        foreach (var entry in World!.Query<Transform2D, Sprite>())
        {
            var transform = World.GetComponent<Transform2D>(entry.Entity);
            var sprite = World.GetComponent<Sprite>(entry.Entity);
            var assetId = new FactoryEngine.Core.Services.Asset.AssetId(sprite.TextureNamespace, sprite.TextureName);
            buffer.DrawSprite(new SpriteDrawCommand(assetId, transform.X, transform.Y, 0f, 1f, 1f, sprite.Layer));
        }
    }
}
