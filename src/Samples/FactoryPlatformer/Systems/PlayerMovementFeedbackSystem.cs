using System.Collections.Generic;
using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Systems;
using FactoryPlatformer;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class PlayerMovementFeedbackSystem : SystemBase
{
    private readonly FactoryPlatformerGameState _state;
    private readonly SoundEffectRef _slideSound;
    private readonly SoundEffectRef _airDodgeSound;
    private readonly Dictionary<Entity, bool> _wallSlideState = new();
    private readonly Dictionary<Entity, bool> _airDodgeState = new();

    public PlayerMovementFeedbackSystem(FactoryPlatformerGameState state, SoundEffectRef slideSound, SoundEffectRef airDodgeSound)
    {
        _state = state;
        _slideSound = slideSound;
        _airDodgeSound = airDodgeSound;
        DeclareAccess(builder => builder
            .Reads<PlayerTag>()
            .Reads<PhysicsBody>()
            .Reads<AirDodge>());
    }

    protected override void OnRun(SystemContext context)
    {
        foreach (var entity in World!.Query(builder => builder
                     .All<PlayerTag>()
                     .All<PhysicsBody>()))
        {
            ref var body = ref World.GetComponent<PhysicsBody>(entity);
            var sliding = body.IsWallSliding;
            _wallSlideState.TryGetValue(entity, out var wasSliding);
            if (sliding && !wasSliding)
            {
                _state.SetEvent("Wall slide!");
                PlaySound(context, _slideSound, 0.1f);
            }
            _wallSlideState[entity] = sliding;

            if (World.HasComponent<AirDodge>(entity))
            {
                ref var dodge = ref World.GetComponent<AirDodge>(entity);
                var dodging = dodge.EffectTimer > 0f;
                _airDodgeState.TryGetValue(entity, out var wasDodging);
                if (dodging && !wasDodging)
                {
                    _state.SetEvent("Air dodge!");
                    PlaySound(context, _airDodgeSound, 0.05f);
                }
                _airDodgeState[entity] = dodging;
            }
        }
    }

    private void PlaySound(SystemContext context, SoundEffectRef effect, float pitchVariance)
    {
        if (string.IsNullOrWhiteSpace(effect.Bank) || string.IsNullOrWhiteSpace(effect.Name))
        {
            return;
        }

        if (!context.Services.Audio.TryResolveSound(effect.Bank, effect.Name, out var definition))
        {
            return;
        }

        var soundId = $"{effect.Bank}:{effect.Name}";
        context.Services.Audio.PlaySound(soundId, new AudioParams(definition.Volume, 0f, pitchVariance));
    }
}
