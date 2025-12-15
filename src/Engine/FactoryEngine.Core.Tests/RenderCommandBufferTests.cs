using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Rendering;

namespace FactoryEngine.Core.Tests;

public class RenderCommandBufferTests
{
    [Fact]
    public void DrawSprite_AppendsCommand()
    {
        var buffer = new RenderCommandBuffer();
        buffer.DrawSprite(new SpriteDrawCommand(new AssetId("core", "player"), 1, 2, 0, 1, 1, 0));
        Assert.Single(buffer.Sprites);
        Assert.Equal("core:player", buffer.Sprites[0].Texture.ToString());
    }

    [Fact]
    public void Clear_RemovesCommands()
    {
        var buffer = new RenderCommandBuffer();
        buffer.DrawSprite(new SpriteDrawCommand(new AssetId("core", "player"), 1, 2, 0, 1, 1, 0));
        buffer.Clear();
        Assert.Empty(buffer.Sprites);
    }
}
