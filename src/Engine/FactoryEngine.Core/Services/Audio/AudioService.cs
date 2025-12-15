namespace FactoryEngine.Core.Services.Audio;

public sealed class AudioService : IAudioService
{
    private readonly Dictionary<string, SoundBank> _banks = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterSoundBank(SoundBank bank)
    {
        _banks[bank.Name] = bank;
    }

    public void PlaySound(string soundId, in AudioParams parameters)
    {
    }

    public void PlayMusic(string playlistId)
    {
    }

    public void SetGroupVolume(string groupId, float volume)
    {
    }

    public bool TryResolveSound(string bankName, string soundName, out SoundDefinition definition)
    {
        definition = null!;
        if (_banks.TryGetValue(bankName, out var bank) && bank.Sounds.TryGetValue(soundName, out var def))
        {
            definition = def;
            return true;
        }

        return false;
    }
}
