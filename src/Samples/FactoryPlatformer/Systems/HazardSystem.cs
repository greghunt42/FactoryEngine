using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Systems;
using FactoryPlatformer;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class HazardSystem : SystemBase
{
    private readonly FactoryPlatformerGameState _state;
    private readonly SoundEffectRef? _hazardSound;

    public HazardSystem(FactoryPlatformerGameState state, SoundEffectRef? hazardSound = null)
    {
        _state = state;
        _hazardSound = hazardSound;
        DeclareAccess(builder => builder
            .Reads<Transform2D>()
            .Reads<Collider2D>()
            .Reads<Hazard>()
            .Reads<PlayerTag>()
            .Reads<SpawnPoint>()
            .Writes<Velocity2D>()
            .Writes<PhysicsBody>());
    }

    protected override void OnRun(SystemContext context)
    {
        if (_state.LoopState != LevelLoopState.Playing)
        {
            return;
        }

        var playerEntity = FindPlayer();
        if (!playerEntity.IsValid)
        {
            return;
        }

        ref var playerTransform = ref World!.GetComponent<Transform2D>(playerEntity);
        ref var playerCollider = ref World.GetComponent<Collider2D>(playerEntity);

        foreach (var entity in World.Query(builder => builder
                     .All<Hazard>()
                     .All<Transform2D>()
                     .All<Collider2D>()))
        {
            ref var hazardTransform = ref World.GetComponent<Transform2D>(entity);
            ref var hazardCollider = ref World.GetComponent<Collider2D>(entity);
            if (Intersects(playerTransform, playerCollider, hazardTransform, hazardCollider))
            {
                HandleHazard(context, playerEntity, entity, ref playerTransform);
                break;
            }
        }
    }

    private void HandleHazard(SystemContext context, Entity playerEntity, Entity hazardEntity, ref Transform2D playerTransform)
    {
        ref var hazard = ref World!.GetComponent<Hazard>(hazardEntity);
        ref var velocity = ref World.GetComponent<Velocity2D>(playerEntity);
        ref var body = ref World.GetComponent<PhysicsBody>(playerEntity);
        if (World.HasComponent<SpawnPoint>(playerEntity))
        {
            ref var spawn = ref World.GetComponent<SpawnPoint>(playerEntity);
            playerTransform.X = spawn.X;
            playerTransform.Y = spawn.Y;
        }
        velocity.VX = 0f;
        velocity.VY = 0f;
        body.Grounded = false;
        body.RemainingCoyoteTime = body.CoyoteTime;
        _state.MarkFailure(hazard.Message, hazard.ResetDelay);
        PlaySound(context, hazard.Sound);
    }

    private void PlaySound(SystemContext context, string? overrideSound)
    {
        SoundEffectRef sound;
        if (TryParseSoundId(overrideSound, out var overrideRef))
        {
            sound = overrideRef;
        }
        else if (_hazardSound is SoundEffectRef fallback)
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
        context.Services.Audio.PlaySound(soundId, new AudioParams(definition.Volume, 0f, 0.05f));
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

    private Entity FindPlayer()
    {
        foreach (var entity in World!.Query(builder => builder
                     .All<PlayerTag>()
                     .All<Transform2D>()
                     .All<Collider2D>()
                     .All<Velocity2D>()
                     .All<PhysicsBody>()))
        {
            return entity;
        }
        return Entity.Invalid;
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
