using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct Collider2D
{
    public bool Enabled;
    public bool IsStatic;
    public float Width;
    public float Height;
    public float OffsetX;
    public float OffsetY;
}

public sealed class Collider2DDescriptor : IComponentDescriptor<Collider2D>
{
    public string Name => "Collider2D";
    public int Version => 1;

    public void Serialize(ref Collider2D component, IComponentWriter writer)
    {
        writer.WriteBool("enabled", component.Enabled);
        writer.WriteBool("static", component.IsStatic);
        writer.WriteFloat("width", component.Width);
        writer.WriteFloat("height", component.Height);
        writer.WriteFloat("offsetX", component.OffsetX);
        writer.WriteFloat("offsetY", component.OffsetY);
    }

    public Collider2D Deserialize(IComponentReader reader)
    {
        return new Collider2D
        {
            Enabled = reader.ReadBool("enabled", true),
            IsStatic = reader.ReadBool("static", false),
            Width = reader.ReadFloat("width", 32f),
            Height = reader.ReadFloat("height", 32f),
            OffsetX = reader.ReadFloat("offsetX"),
            OffsetY = reader.ReadFloat("offsetY")
        };
    }

    public void Validate(Collider2D component, ValidationContext context)
    {
        if (component.Width <= 0 || component.Height <= 0)
        {
            context.Error("Collider width/height must be positive.");
        }
    }
}
