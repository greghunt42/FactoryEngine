using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct LevelGoal
{
    public int Bonus;
    public string Message;
    public float ResetDelay;
}

public sealed class LevelGoalDescriptor : IComponentDescriptor<LevelGoal>
{
    public string Name => "LevelGoal";
    public int Version => 1;

    public void Serialize(ref LevelGoal component, IComponentWriter writer)
    {
        writer.WriteInt("bonus", component.Bonus);
        writer.WriteString("message", component.Message);
        writer.WriteFloat("resetDelay", component.ResetDelay);
    }

    public LevelGoal Deserialize(IComponentReader reader)
    {
        return new LevelGoal
        {
            Bonus = reader.ReadInt("bonus", 100),
            Message = reader.ReadString("message", "Level complete!"),
            ResetDelay = reader.ReadFloat("resetDelay", 2f)
        };
    }

    public void Validate(LevelGoal component, ValidationContext context)
    {
        if (component.Bonus < 0)
        {
            context.Error("bonus must be non-negative");
        }

        if (component.ResetDelay < 0)
        {
            context.Error("resetDelay must be non-negative");
        }
    }
}
