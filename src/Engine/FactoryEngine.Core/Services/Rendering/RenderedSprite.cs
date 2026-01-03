namespace FactoryEngine.Core.Services.Rendering;

using FactoryEngine.Core.Services.Asset;

public readonly record struct RenderedSprite(SpriteDrawCommand Command, TextureAsset Texture);
