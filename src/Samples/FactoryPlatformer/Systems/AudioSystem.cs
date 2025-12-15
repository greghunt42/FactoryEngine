using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Systems;

namespace FactoryPlatformer.Systems;

public sealed class AudioSystem : SystemBase
{
    private readonly string _bank;
    private readonly string _sound;
    private bool _played;

    public AudioSystem(string bank, string sound)
    {
        _bank = bank;
        _sound = sound;
    }

    protected override void OnRun(SystemContext context)
    {
        if (!_played && context.Services.Audio.TryResolveSound(_bank, _sound, out var definition))
        {
            context.Services.Audio.PlaySound(_sound, new AudioParams(definition.Volume, 0f));
            _played = true;
        }
    }
}
