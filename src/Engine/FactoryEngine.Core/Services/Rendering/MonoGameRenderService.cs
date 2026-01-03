#if MONOGAME
using FactoryEngine.Core.Services.Asset;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FactoryEngine.Core.Services.Rendering;

public sealed class MonoGameRenderService : IRenderService, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly RenderCommandBuffer _buffer = new();
    private readonly Dictionary<Asset.AssetId, Texture2D> _textureCache = new();
    private readonly Func<Asset.AssetId, Texture2D> _textureLoader;

    public MonoGameRenderService(GraphicsDevice graphicsDevice, Func<Asset.AssetId, Texture2D> textureLoader)
    {
        _graphicsDevice = graphicsDevice;
        _spriteBatch = new SpriteBatch(_graphicsDevice);
        _textureLoader = textureLoader;
    }

    public void BeginFrame()
    {
        _buffer.Clear();
    }

    public RenderCommandBuffer GetFrameBuffer() => _buffer;

    public MaterialId RegisterMaterial(MaterialDescriptor descriptor)
    {
        return new MaterialId(0);
    }

    public void Submit(RenderCommandBuffer buffer)
    {
        _spriteBatch.Begin();
        foreach (var sprite in buffer.Sprites)
        {
            var texture = LoadTexture(sprite.Texture);
            _spriteBatch.Draw(texture, new Vector2(sprite.X, sprite.Y), Color.White);
        }
        _spriteBatch.End();
    }

    private Texture2D LoadTexture(Asset.AssetId assetId)
    {
        if (_textureCache.TryGetValue(assetId, out var cached))
        {
            return cached;
        }

        var texture = _textureLoader(assetId);
        _textureCache[assetId] = texture;
        return texture;
    }

    public void Dispose()
    {
        foreach (var texture in _textureCache.Values)
        {
            texture.Dispose();
        }
        _spriteBatch.Dispose();
    }
}

#else
namespace FactoryEngine.Core.Services.Rendering;

public sealed class MonoGameRenderService
{
    public MonoGameRenderService()
    {
        throw new NotSupportedException("MonoGame support not enabled. Define MONOGAME symbol to compile this adapter.");
    }
}
#endif
