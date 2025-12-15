using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Audio;

public sealed class SoundBank
{
    public SoundBank(string name)
    {
        Name = name;
    }

    public string Name { get; }
    public Dictionary<string, SoundDefinition> Sounds { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class SoundDefinition
{
    public required AssetId Asset { get; init; }
    public string Group { get; init; } = "sfx";
    public float Volume { get; init; } = 1f;
}
