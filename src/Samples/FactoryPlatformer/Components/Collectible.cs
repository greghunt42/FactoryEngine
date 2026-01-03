using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct Collectible
{
    public int Value;
    public string Message;
    public string Sound;
}

public sealed class CollectibleDescriptor : IComponentDescriptor<Collectible>
{
    public string Name => "Collectible";
    public int Version => 1;

    public void Serialize(ref Collectible component, IComponentWriter writer)
    {
        writer.WriteInt("value", component.Value);
        writer.WriteString("message", component.Message);
        writer.WriteString("sound", component.Sound ?? string.Empty);
    }

    public Collectible Deserialize(IComponentReader reader)
    {
        return new Collectible
        {
            Value = reader.ReadInt("value"),
            Message = reader.ReadString("message", string.Empty),
            Sound = reader.ReadString("sound", string.Empty)
        };
    }

    public void Validate(Collectible component, ValidationContext context)
    {
        if (component.Value <= 0)
        {
            context.Error("Collectible value must be positive.");
        }
    }
}
