using System.Collections.Generic;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class RenderingSystem : SystemBase
{
    private readonly Dictionary<AssetId, bool> _loadedTextures = new();
    private static readonly AssetId WallSlideEffect = new("core", "wall-slide-effect");
    private static readonly AssetId AirDodgeTrail = new("core", "air-dodge-trail");

    public RenderingSystem()
    {
        DeclareAccess(builder => builder
            .Reads<Transform2D>()
            .Reads<Sprite>()
            .Reads<PhysicsBody>()
            .Reads<PlayerTag>());
    }

    protected override void OnRun(SystemContext context)
    {
        var buffer = context.Services.Rendering.GetFrameBuffer();
        var cameraOffset = GetCameraOffset();
        foreach (var entry in World!.Query<Transform2D, Sprite>())
        {
            var transform = World.GetComponent<Transform2D>(entry.Entity);
            var sprite = World.GetComponent<Sprite>(entry.Entity);
            var assetId = new AssetId(sprite.TextureNamespace, sprite.TextureName);
            EnsureTextureLoaded(assetId, context);
            buffer.DrawSprite(new SpriteDrawCommand(
                assetId,
                transform.X - cameraOffset.X,
                transform.Y - cameraOffset.Y,
                0f,
                1f,
                1f,
                sprite.Layer));

            if (World.HasComponent<PlayerTag>(entry.Entity) &&
                World.HasComponent<PhysicsBody>(entry.Entity))
            {
                ref var body = ref World.GetComponent<PhysicsBody>(entry.Entity);
                if (body.IsWallSliding)
                {
                    EnsureTextureLoaded(WallSlideEffect, context);
                    var direction = body.WallSlideSide < 0 ? -1f : 1f;
                    var effectX = transform.X - cameraOffset.X + (direction < 0 ? -20f : 20f);
                    var effectY = transform.Y - cameraOffset.Y + 18f;
                    buffer.DrawSprite(new SpriteDrawCommand(
                        WallSlideEffect,
                        effectX,
                        effectY,
                        0f,
                        direction,
                        1f,
                        sprite.Layer + 0.01f));
                }

                if (World.HasComponent<AirDodge>(entry.Entity))
                {
                    ref var dodge = ref World.GetComponent<AirDodge>(entry.Entity);
                    if (dodge.EffectTimer > 0f && dodge.EffectDuration > 0f)
                    {
                        EnsureTextureLoaded(AirDodgeTrail, context);
                        var dir = dodge.LastDirection >= 0f ? 1f : -1f;
                        var effectX = transform.X - cameraOffset.X - dir * 28f;
                        var effectY = transform.Y - cameraOffset.Y + 16f;
                        buffer.DrawSprite(new SpriteDrawCommand(
                            AirDodgeTrail,
                            effectX,
                            effectY,
                            0f,
                            dir,
                            1f,
                            sprite.Layer + 0.02f));
                    }
                }
            }
        }
    }

    private (float X, float Y) GetCameraOffset()
    {
        foreach (var cameraRef in World!.Query<Camera2D>())
        {
            var camera = cameraRef.Component;
            if (!camera.Enabled)
            {
                continue;
            }
            return (camera.OffsetX, camera.OffsetY);
        }
        return (0f, 0f);
    }

    private void EnsureTextureLoaded(AssetId assetId, SystemContext context)
    {
        if (_loadedTextures.ContainsKey(assetId))
        {
            return;
        }

        context.Services.Assets.Load<TextureAsset>(assetId);
        _loadedTextures[assetId] = true;
    }
}
