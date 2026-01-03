using System;
using System.Collections.Generic;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Audio;

internal interface IAudioAssetConsumer
{
    void SetAssetService(IAssetService assets);
}

public sealed class AudioService : IAudioService, IAudioAssetConsumer
{
    private sealed class SoundInstance
    {
        public required SoundPlayback Playback { get; init; }
        public float RemainingLifetime { get; set; }
        public bool AutoStop { get; init; }
    }

    private readonly Dictionary<string, SoundBank> _banks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, SoundDefinition> _soundLookup = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<AssetId, AudioClipAsset> _clipCache = new();
    private readonly List<SoundInstance> _instances = new();
    private readonly List<SoundPlayback> _activeSounds = new();
    private Func<AssetId, bool>? _assetResolver;
    private IAssetService? _assetService;

    public IReadOnlyList<SoundPlayback> ActiveSounds => _activeSounds;

    public event Action<SoundPlayback>? SoundPlayed;
    public event Action<SoundPlayback>? SoundStopped;

    public void RegisterSoundBank(SoundBank bank)
    {
        ArgumentNullException.ThrowIfNull(bank);
        ValidateSoundBank(bank);
        _banks[bank.Name] = bank;
        foreach (var sound in bank.Sounds)
        {
            _soundLookup[$"{bank.Name}:{sound.Key}"] = sound.Value;
        }
    }

    public void PlaySound(string soundId, in AudioParams parameters)
    {
        if (!_soundLookup.TryGetValue(soundId, out var definition))
        {
            return;
        }

        if (_assetService is not null && EnsureClipLoaded(definition.Asset) is null)
        {
            return;
        }

        var playback = new SoundPlayback(Guid.NewGuid(), soundId, definition.Asset, parameters, DateTime.UtcNow);
        var lifetime = MathF.Max(0f, parameters.LifetimeSeconds);
        var autoStop = lifetime > 0f;
        var instance = new SoundInstance
        {
            Playback = playback,
            RemainingLifetime = lifetime,
            AutoStop = autoStop
        };
        _instances.Add(instance);
        _activeSounds.Add(playback);
        SoundPlayed?.Invoke(playback);
    }

    public void Update(float deltaTime)
    {
        if (deltaTime <= 0f)
        {
            return;
        }

        for (var i = _instances.Count - 1; i >= 0; i--)
        {
            var instance = _instances[i];
            if (!instance.AutoStop)
            {
                continue;
            }

            instance.RemainingLifetime -= deltaTime;
            if (instance.RemainingLifetime <= 0f)
            {
                CompleteInstanceAt(i);
            }
        }
    }

    public void PlayMusic(string playlistId)
    {
    }

    public void SetGroupVolume(string groupId, float volume)
    {
    }

    public bool TryResolveSound(string bankName, string soundName, out SoundDefinition definition)
    {
        var key = $"{bankName}:{soundName}";
        if (_soundLookup.TryGetValue(key, out var def))
        {
            definition = def;
            return true;
        }

        definition = null!;
        return false;
    }

    public void StopSound(Guid id)
    {
        for (var i = _instances.Count - 1; i >= 0; i--)
        {
            if (_instances[i].Playback.Id == id)
            {
                CompleteInstanceAt(i);
                break;
            }
        }
    }

    public void SetAssetResolver(Func<AssetId, bool>? resolver)
    {
        _assetResolver = resolver;
    }

    public void SetAssetService(IAssetService assets)
    {
        _assetService = assets ?? throw new ArgumentNullException(nameof(assets));
    }

    public bool TryGetLoadedClip(AssetId assetId, out AudioClipAsset? clip) =>
        _clipCache.TryGetValue(assetId, out clip);

    private void CompleteInstanceAt(int index)
    {
        var instance = _instances[index];
        _instances.RemoveAt(index);
        RemoveActivePlayback(instance.Playback.Id);
        SoundStopped?.Invoke(instance.Playback);
    }

    private void RemoveActivePlayback(Guid id)
    {
        for (var i = 0; i < _activeSounds.Count; i++)
        {
            if (_activeSounds[i].Id == id)
            {
                _activeSounds.RemoveAt(i);
                break;
            }
        }
    }

    private void ValidateSoundBank(SoundBank bank)
    {
        if (_assetResolver is null)
        {
            return;
        }

        foreach (var (soundName, definition) in bank.Sounds)
        {
            if (definition.Asset == default || string.IsNullOrWhiteSpace(definition.Asset.Name))
            {
                throw new InvalidOperationException($"Sound '{bank.Name}:{soundName}' is missing an asset reference.");
            }

            if (!_assetResolver(definition.Asset))
            {
                throw new InvalidOperationException($"Sound '{bank.Name}:{soundName}' references missing asset '{definition.Asset}'.");
            }
        }
    }

    private AudioClipAsset? EnsureClipLoaded(AssetId asset)
    {
        if (_clipCache.TryGetValue(asset, out var clip))
        {
            return clip;
        }

        if (_assetService is null)
        {
            return null;
        }

        try
        {
            var handle = _assetService.Load<AudioClipAsset>(asset);
            if (handle.Value is null)
            {
                return null;
            }

            _clipCache[asset] = handle.Value;
            return handle.Value;
        }
        catch
        {
            return null;
        }
    }
}
