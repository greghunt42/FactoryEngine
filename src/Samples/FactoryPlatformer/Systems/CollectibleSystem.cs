using System.Collections.Generic;
using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Systems;
using FactoryPlatformer;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class CollectibleSystem : SystemBase
{
    private readonly FactoryPlatformerGameState _state;
    private readonly SoundEffectRef? _pickupSound;
    private readonly List<Entity> _collected = new();

    public CollectibleSystem(FactoryPlatformerGameState state, SoundEffectRef? pickupSound = null)
    {
        _state = state;
        _pickupSound = pickupSound;
        DeclareAccess(builder => builder
            .Reads<Transform2D>()
            .Reads<Collider2D>()
            .Reads<PlayerTag>()
            .Reads<Collectible>());
    }

    protected override void OnRun(SystemContext context)
    {
        if (_state.LoopState != LevelLoopState.Playing)
        {
            return;
        }

        var playerEntity = FindPlayer();
        if (!playerEntity.IsValid || !World!.HasComponent<Collider2D>(playerEntity))
        {
            return;
        }

        ref var playerTransform = ref World.GetComponent<Transform2D>(playerEntity);
        ref var playerCollider = ref World.GetComponent<Collider2D>(playerEntity);

        _collected.Clear();
        foreach (var entity in World.Query(builder => builder
                     .All<Collectible>()
                     .All<Transform2D>()
                     .All<Collider2D>()))
        {
            var collectibleEntity = entity;
            ref var collectible = ref World.GetComponent<Collectible>(collectibleEntity);
            ref var transform = ref World.GetComponent<Transform2D>(collectibleEntity);
            ref var collider = ref World.GetComponent<Collider2D>(collectibleEntity);

            if (!Intersects(playerTransform, playerCollider, transform, collider))
            {
                continue;
            }

            _state.AddScore(collectible.Value, collectible.Message);
            PlaySound(context, collectible.Sound);
            _collected.Add(collectibleEntity);
        }

        foreach (var entity in _collected)
        {
            World.DestroyEntity(entity);
        }
    }

    private Entity FindPlayer()
    {
        foreach (var entry in World!.Query(builder => builder
                     .All<PlayerTag>()
                     .All<Transform2D>()
                     .All<Collider2D>()))
        {
            return entry;
        }
        return Entity.Invalid;
    }

    private void PlaySound(SystemContext context, string? overrideSound = null)
    {
        SoundEffectRef sound;
        if (TryParseSoundId(overrideSound, out var overrideRef))
        {
            sound = overrideRef;
        }
        else if (_pickupSound is SoundEffectRef fallback)
        {
            sound = fallback;
        }
        else
        {
            return;
        }

        if (!context.Services.Audio.TryResolveSound(sound.Bank, sound.Name, out var definition))
        {
            return;
        }

        var soundId = $"{sound.Bank}:{sound.Name}";
        context.Services.Audio.PlaySound(soundId, new AudioParams(definition.Volume, 0f, 0.1f));
    }

    private static bool TryParseSoundId(string? value, out SoundEffectRef sound)
    {
        sound = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var separator = value.IndexOf(':');
        if (separator <= 0 || separator >= value.Length - 1)
        {
            return false;
        }

        var bank = value[..separator];
        var name = value[(separator + 1)..];
        sound = new SoundEffectRef(bank, name);
        return true;
    }

    private static bool Intersects(in Transform2D aTransform, in Collider2D aCollider, in Transform2D bTransform, in Collider2D bCollider)
    {
        var aBounds = CalculateBounds(aTransform, aCollider);
        var bBounds = CalculateBounds(bTransform, bCollider);
        return aBounds.MinX < bBounds.MaxX &&
               aBounds.MaxX > bBounds.MinX &&
               aBounds.MinY < bBounds.MaxY &&
               aBounds.MaxY > bBounds.MinY;
    }

    private static Bounds CalculateBounds(in Transform2D transform, in Collider2D collider)
    {
        var centerX = transform.X + collider.OffsetX;
        var centerY = transform.Y + collider.OffsetY;
        var halfWidth = collider.Width * 0.5f;
        var halfHeight = collider.Height * 0.5f;
        return new Bounds(centerX - halfWidth, centerX + halfWidth, centerY - halfHeight, centerY + halfHeight);
    }

    private readonly record struct Bounds(float MinX, float MaxX, float MinY, float MaxY);
}
