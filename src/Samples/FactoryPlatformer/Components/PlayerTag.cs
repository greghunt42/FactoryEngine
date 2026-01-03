using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public readonly struct PlayerTag;

public sealed class PlayerTagDescriptor : IComponentDescriptor<PlayerTag>
{
    public string Name => "PlayerTag";
    public int Version => 1;

    public void Serialize(ref PlayerTag component, IComponentWriter writer)
    {
    }

    public PlayerTag Deserialize(IComponentReader reader) => new();

    public void Validate(PlayerTag component, ValidationContext context)
    {
    }
}
