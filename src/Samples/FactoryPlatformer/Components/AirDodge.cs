using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct AirDodge
{
    public bool Enabled;
    public float Speed;
    public float Cooldown;
    public float CooldownRemaining;
    public float EffectTimer;
    public float EffectDuration;
    public float LastDirection;
}

public sealed class AirDodgeDescriptor : IComponentDescriptor<AirDodge>
{
    public string Name => "AirDodge";
    public int Version => 1;

    public void Serialize(ref AirDodge component, IComponentWriter writer)
    {
        writer.WriteBool("enabled", component.Enabled);
        writer.WriteFloat("speed", component.Speed);
        writer.WriteFloat("cooldown", component.Cooldown);
        writer.WriteFloat("cooldownRemaining", component.CooldownRemaining);
        writer.WriteFloat("effectTimer", component.EffectTimer);
        writer.WriteFloat("effectDuration", component.EffectDuration);
        writer.WriteFloat("lastDirection", component.LastDirection);
    }

    public AirDodge Deserialize(IComponentReader reader)
    {
        return new AirDodge
        {
            Enabled = reader.ReadBool("enabled", true),
            Speed = reader.ReadFloat("speed", 200f),
            Cooldown = reader.ReadFloat("cooldown", 1f),
            CooldownRemaining = reader.ReadFloat("cooldownRemaining", 0f),
            EffectTimer = reader.ReadFloat("effectTimer", 0f),
            EffectDuration = reader.ReadFloat("effectDuration", 0.2f),
            LastDirection = reader.ReadFloat("lastDirection", 1f)
        };
    }

    public void Validate(AirDodge component, ValidationContext context)
    {
        if (component.Speed <= 0f)
        {
            context.Error("AirDodge speed must be positive.");
        }

        if (component.Cooldown < 0f)
        {
            context.Error("AirDodge cooldown must be non-negative.");
        }

        if (component.CooldownRemaining < 0f)
        {
            context.Error("Cooldown remaining cannot be negative.");
        }

        if (component.EffectTimer < 0f)
        {
            context.Error("Effect timer must be non-negative.");
        }

        if (component.EffectDuration < 0f)
        {
            context.Error("Effect duration must be non-negative.");
        }
}
}
