using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Systems;

namespace FactoryPlatformer.Systems;

public sealed class AudioSystem : SystemBase
{
    private readonly string _bank;
    private readonly string _sound;
    private bool _played;
    private bool _clipLoaded;

    public AudioSystem(string bank, string sound)
    {
        _bank = bank;
        _sound = sound;
    }

    protected override void OnRun(SystemContext context)
    {
        if (context.Services.Audio.TryResolveSound(_bank, _sound, out var definition))
        {
            EnsureClipLoaded(context, definition.Asset);
            if (!_played)
            {
                var soundId = $"{_bank}:{_sound}";
                context.Services.Audio.PlaySound(soundId, new AudioParams(definition.Volume, 0f, 0.2f));
                _played = true;
            }
        }
    }

    private void EnsureClipLoaded(SystemContext context, AssetId assetId)
    {
        if (_clipLoaded)
        {
            return;
        }

        context.Services.Assets.Load<AudioClipAsset>(assetId);
        _clipLoaded = true;
    }
}
