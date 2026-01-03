using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public enum PatrolAxis
{
    Horizontal,
    Vertical
}

public struct HazardPatrol
{
    public float Range;
    public float Speed;
    public PatrolAxis Axis;
    public float OriginX;
    public float OriginY;
    public float Offset;
    public float Direction;
    public bool Initialized;
}

public sealed class HazardPatrolDescriptor : IComponentDescriptor<HazardPatrol>
{
    public string Name => "HazardPatrol";
    public int Version => 1;

    public void Serialize(ref HazardPatrol component, IComponentWriter writer)
    {
        writer.WriteFloat("range", component.Range);
        writer.WriteFloat("speed", component.Speed);
        writer.WriteString("axis", component.Axis.ToString());
        writer.WriteFloat("originX", component.OriginX);
        writer.WriteFloat("originY", component.OriginY);
    }

    public HazardPatrol Deserialize(IComponentReader reader)
    {
        var axisValue = reader.ReadString("axis", "Horizontal");
        Enum.TryParse<PatrolAxis>(axisValue, true, out var axis);
        return new HazardPatrol
        {
            Range = reader.ReadFloat("range", 50f),
            Speed = reader.ReadFloat("speed", 30f),
            Axis = axis,
            OriginX = reader.ReadFloat("originX", 0f),
            OriginY = reader.ReadFloat("originY", 0f),
            Direction = 1f
        };
    }

    public void Validate(HazardPatrol component, ValidationContext context)
    {
        if (component.Range <= 0f)
        {
            context.Error("range must be positive");
        }

        if (component.Speed <= 0f)
        {
            context.Error("speed must be positive");
        }
    }
}
