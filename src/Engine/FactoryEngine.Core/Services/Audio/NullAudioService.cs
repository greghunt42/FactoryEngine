namespace FactoryEngine.Core.Services.Audio;

public sealed class NullAudioService : IAudioService
{
    public void PlaySound(string soundId, in AudioParams parameters)
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
}
