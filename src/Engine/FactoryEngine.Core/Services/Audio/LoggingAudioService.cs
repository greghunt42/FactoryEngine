using System;
using System.Collections.Generic;
using System.IO;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Audio;

public sealed class LoggingAudioService : IAudioService, IAudioAssetConsumer
{
    private readonly AudioService _inner = new();
    private readonly TextWriter _writer;

    public LoggingAudioService(TextWriter? writer = null)
    {
        _writer = writer ?? Console.Out;
        _inner.SoundPlayed += playback =>
        {
            _writer.WriteLine($"[AudioLog] Played {playback.SoundKey} vol={playback.Parameters.Volume}");
            SoundPlayed?.Invoke(playback);
        };
        _inner.SoundStopped += playback =>
        {
            _writer.WriteLine($"[AudioLog] Stopped {playback.SoundKey}");
            SoundStopped?.Invoke(playback);
        };
    }

    public IReadOnlyList<SoundPlayback> ActiveSounds => _inner.ActiveSounds;

    public event Action<SoundPlayback>? SoundPlayed;
    public event Action<SoundPlayback>? SoundStopped;

    public void PlaySound(string soundId, in AudioParams parameters)
    {
        _inner.PlaySound(soundId, parameters);
    }

    public void Update(float deltaTime)
    {
        _inner.Update(deltaTime);
    }

    public void PlayMusic(string playlistId)
    {
        _writer.WriteLine($"[AudioLog] Music playlist {playlistId} not implemented");
    }

    public void SetGroupVolume(string groupId, float volume)
    {
        _writer.WriteLine($"[AudioLog] Set volume {groupId} -> {volume}");
    }

    public void RegisterSoundBank(SoundBank bank)
    {
        _inner.RegisterSoundBank(bank);
        _writer.WriteLine($"[AudioLog] Registered sound bank {bank.Name}");
    }

    public bool TryResolveSound(string bankName, string soundName, out SoundDefinition definition) =>
        _inner.TryResolveSound(bankName, soundName, out definition);

    public void StopSound(Guid id)
    {
        _inner.StopSound(id);
    }

    public void SetAssetResolver(Func<AssetId, bool>? resolver)
    {
        _inner.SetAssetResolver(resolver);
    }

    public void SetAssetService(IAssetService assets)
    {
        _inner.SetAssetService(assets);
    }
}
