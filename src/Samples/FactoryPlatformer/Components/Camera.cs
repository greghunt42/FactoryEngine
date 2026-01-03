using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct Camera2D
{
    public bool Enabled;
    public float ViewportWidth;
    public float ViewportHeight;
    public float OffsetX;
    public float OffsetY;
    public float MinX;
    public float MaxX;
    public float MinY;
    public float MaxY;
}

public readonly struct CameraTarget;

public sealed class Camera2DDescriptor : IComponentDescriptor<Camera2D>
{
    public string Name => "Camera2D";
    public int Version => 1;

    public void Serialize(ref Camera2D component, IComponentWriter writer)
    {
        writer.WriteBool("enabled", component.Enabled);
        writer.WriteFloat("viewportWidth", component.ViewportWidth);
        writer.WriteFloat("viewportHeight", component.ViewportHeight);
        writer.WriteFloat("minX", component.MinX);
        writer.WriteFloat("maxX", component.MaxX);
        writer.WriteFloat("minY", component.MinY);
        writer.WriteFloat("maxY", component.MaxY);
    }

    public Camera2D Deserialize(IComponentReader reader)
    {
        return new Camera2D
        {
            Enabled = reader.ReadBool("enabled", true),
            ViewportWidth = reader.ReadFloat("viewportWidth", 1280f),
            ViewportHeight = reader.ReadFloat("viewportHeight", 720f),
            MinX = reader.ReadFloat("minX", float.NegativeInfinity),
            MaxX = reader.ReadFloat("maxX", float.PositiveInfinity),
            MinY = reader.ReadFloat("minY", float.NegativeInfinity),
            MaxY = reader.ReadFloat("maxY", float.PositiveInfinity)
        };
    }

    public void Validate(Camera2D component, ValidationContext context)
    {
        if (component.ViewportWidth <= 0 || component.ViewportHeight <= 0)
        {
            context.Error("Camera viewport must be positive.");
        }

        if (component.MinX > component.MaxX)
        {
            context.Error("Camera minX must be <= maxX.");
        }

        if (component.MinY > component.MaxY)
        {
            context.Error("Camera minY must be <= maxY.");
        }
    }
}

public sealed class CameraTargetDescriptor : IComponentDescriptor<CameraTarget>
{
    public string Name => "CameraTarget";
    public int Version => 1;

    public void Serialize(ref CameraTarget component, IComponentWriter writer)
    {
    }

    public CameraTarget Deserialize(IComponentReader reader) => new();

    public void Validate(CameraTarget component, ValidationContext context)
    {
    }
}
