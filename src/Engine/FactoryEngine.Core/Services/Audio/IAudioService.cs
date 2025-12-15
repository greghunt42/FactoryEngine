namespace FactoryEngine.Core.Services.Audio;

public interface IAudioService
{
    void PlaySound(string soundId, in AudioParams parameters);
    void PlayMusic(string playlistId);
    void SetGroupVolume(string groupId, float volume);
    void RegisterSoundBank(SoundBank bank);
    bool TryResolveSound(string bankName, string soundName, out SoundDefinition definition);
}

public readonly record struct AudioParams(float Volume = 1f, float Pitch = 0f);
