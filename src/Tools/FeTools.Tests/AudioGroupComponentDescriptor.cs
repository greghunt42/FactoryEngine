using FactoryEngine.Core.Services.Serialization;

namespace FeTools.Tests;

public struct AudioGroupComponent
{
    public string Group;
}

public sealed class AudioGroupComponentDescriptor : IComponentDescriptor<AudioGroupComponent>
{
    public string Name => nameof(AudioGroupComponent);
    public int Version => 1;

    public void Serialize(ref AudioGroupComponent component, IComponentWriter writer)
    {
        writer.WriteString("group", component.Group);
    }

    public AudioGroupComponent Deserialize(IComponentReader reader)
    {
        return new AudioGroupComponent
        {
            Group = reader.ReadString("group", string.Empty)
        };
    }

    public void Validate(AudioGroupComponent component, ValidationContext context)
    {
        var rules = context.MetadataRules;
        if (rules is null)
        {
            context.Error("Metadata rules not configured.");
            return;
        }

        if (string.IsNullOrWhiteSpace(component.Group))
        {
            context.Error("Audio group required.");
            return;
        }

        if (!rules.IsAudioGroupAllowed(component.Group))
        {
            context.Error($"Audio group '{component.Group}' not allowed.");
        }
    }
}
