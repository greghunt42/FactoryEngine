using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct Hazard
{
    public string Message;
    public float ResetDelay;
    public string Sound;
}

public sealed class HazardDescriptor : IComponentDescriptor<Hazard>
{
    public string Name => "Hazard";
    public int Version => 1;

    public void Serialize(ref Hazard component, IComponentWriter writer)
    {
        writer.WriteString("message", component.Message);
        writer.WriteFloat("resetDelay", component.ResetDelay);
        writer.WriteString("sound", component.Sound ?? string.Empty);
    }

    public Hazard Deserialize(IComponentReader reader)
    {
        return new Hazard
        {
            Message = reader.ReadString("message", "Watch out!"),
            ResetDelay = reader.ReadFloat("resetDelay", 1.5f),
            Sound = reader.ReadString("sound", string.Empty)
        };
    }

    public void Validate(Hazard component, ValidationContext context)
    {
        if (component.ResetDelay < 0)
        {
            context.Error("resetDelay must be non-negative");
        }
    }
}
