using System;
using System.Collections.Generic;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Audio;

public sealed class NullAudioService : IAudioService
{
    public IReadOnlyList<SoundPlayback> ActiveSounds { get; } = Array.Empty<SoundPlayback>();

    public event Action<SoundPlayback>? SoundPlayed
    {
        add { }
        remove { }
    }

    public event Action<SoundPlayback>? SoundStopped
    {
        add { }
        remove { }
    }

    public void PlaySound(string soundId, in AudioParams parameters)
    {
    }

    public void Update(float deltaTime)
    {
    }

    public void PlayMusic(string playlistId)
    {
    }

    public void SetGroupVolume(string groupId, float volume)
    {
    }

    public void RegisterSoundBank(SoundBank bank)
    {
    }

    public bool TryResolveSound(string bankName, string soundName, out SoundDefinition definition)
    {
        definition = null!;
        return false;
    }

    public void StopSound(Guid id)
    {
    }

    public void SetAssetResolver(Func<AssetId, bool>? resolver)
    {
    }
}
