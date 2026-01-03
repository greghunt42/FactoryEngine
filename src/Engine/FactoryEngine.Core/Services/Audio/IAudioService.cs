using System;
using System.Collections.Generic;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Audio;

public interface IAudioService
{
    IReadOnlyList<SoundPlayback> ActiveSounds { get; }
    event Action<SoundPlayback>? SoundPlayed;
    event Action<SoundPlayback>? SoundStopped;

    void PlaySound(string soundId, in AudioParams parameters);
    void Update(float deltaTime);
    void PlayMusic(string playlistId);
    void SetGroupVolume(string groupId, float volume);
    void RegisterSoundBank(SoundBank bank);
    bool TryResolveSound(string bankName, string soundName, out SoundDefinition definition);
    void StopSound(Guid id);
    void SetAssetResolver(Func<AssetId, bool>? resolver);
}

public readonly record struct AudioParams(float Volume = 1f, float Pitch = 0f, float LifetimeSeconds = 0f);
