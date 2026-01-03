using System;
using System.Collections.Generic;
using System.IO;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FactoryPlatformer.Rendering;

public sealed class MonoGameSpriteBatchBackend : IRenderBackend, IDisposable
{
    private readonly GraphicsDevice _graphicsDevice;
    private readonly SpriteBatch _spriteBatch;
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);

    public MonoGameSpriteBatchBackend(GraphicsDevice graphicsDevice)
    {
        _graphicsDevice = graphicsDevice ?? throw new ArgumentNullException(nameof(graphicsDevice));
        _spriteBatch = new SpriteBatch(_graphicsDevice);
    }

    public void BeginFrame()
    {
        _spriteBatch.Begin(SpriteSortMode.BackToFront, BlendState.AlphaBlend);
    }

    public void DrawSprite(RenderedSprite sprite)
    {
        var texture = ResolveTexture(sprite.Texture);
        var command = sprite.Command;
        _spriteBatch.Draw(
            texture,
            new Vector2(command.X, command.Y),
            null,
            Color.White,
            command.Rotation,
            Vector2.Zero,
            new Vector2(command.ScaleX, command.ScaleY),
            SpriteEffects.None,
            command.Layer);
    }

    public void OnError(string message)
    {
        Console.WriteLine($"[RenderError] {message}");
    }

    public void EndFrame()
    {
        _spriteBatch.End();
    }

    private Texture2D ResolveTexture(TextureAsset asset)
    {
        var key = asset.Path ?? Guid.NewGuid().ToString("N");
        if (_textureCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        Texture2D texture;
        try
        {
            if (asset.Content.Length > 0)
            {
                using var stream = new MemoryStream(asset.Content);
                texture = Texture2D.FromStream(_graphicsDevice, stream);
            }
            else if (!string.IsNullOrWhiteSpace(asset.Path) && File.Exists(asset.Path))
            {
                using var stream = File.OpenRead(asset.Path);
                texture = Texture2D.FromStream(_graphicsDevice, stream);
            }
            else
            {
                texture = CreateFallbackTexture();
            }
        }
        catch
        {
            texture = CreateFallbackTexture();
        }

        _textureCache[key] = texture;
        return texture;
    }

    private Texture2D CreateFallbackTexture()
    {
        var tex = new Texture2D(_graphicsDevice, 1, 1);
        tex.SetData(new[] { Color.White });
        return tex;
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
