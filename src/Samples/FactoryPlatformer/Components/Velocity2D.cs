using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct Velocity2D
{
    public float VX;
    public float VY;
}

public sealed class Velocity2DDescriptor : IComponentDescriptor<Velocity2D>
{
    public string Name => "Velocity2D";
    public int Version => 1;

    public void Serialize(ref Velocity2D component, IComponentWriter writer)
    {
        writer.WriteFloat("vx", component.VX);
        writer.WriteFloat("vy", component.VY);
    }

    public Velocity2D Deserialize(IComponentReader reader)
    {
        return new Velocity2D
        {
            VX = reader.ReadFloat("vx"),
            VY = reader.ReadFloat("vy")
        };
    }

    public void Validate(Velocity2D component, ValidationContext context)
    {
    }
}
