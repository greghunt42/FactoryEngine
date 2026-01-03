using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct SpawnPoint
{
    public float X;
    public float Y;
}

public sealed class SpawnPointDescriptor : IComponentDescriptor<SpawnPoint>
{
    public string Name => "SpawnPoint";
    public int Version => 1;

    public void Serialize(ref SpawnPoint component, IComponentWriter writer)
    {
        writer.WriteFloat("x", component.X);
        writer.WriteFloat("y", component.Y);
    }

    public SpawnPoint Deserialize(IComponentReader reader)
    {
        return new SpawnPoint
        {
            X = reader.ReadFloat("x"),
            Y = reader.ReadFloat("y")
        };
    }

    public void Validate(SpawnPoint component, ValidationContext context)
    {
    }
}
