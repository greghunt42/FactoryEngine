using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct Transform2D
{
    public float X;
    public float Y;
}

public sealed class Transform2DDescriptor : IComponentDescriptor<Transform2D>
{
    public string Name => "Transform2D";
    public int Version => 1;

    public void Serialize(ref Transform2D component, IComponentWriter writer)
    {
        writer.WriteFloat("x", component.X);
        writer.WriteFloat("y", component.Y);
    }

    public Transform2D Deserialize(IComponentReader reader)
    {
        return new Transform2D
        {
            X = reader.ReadFloat("x"),
            Y = reader.ReadFloat("y")
        };
    }

    public void Validate(Transform2D component, ValidationContext context)
    {
    }
}
