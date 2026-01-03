using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct PhysicsBody
{
    public bool Enabled;
    public float Gravity;
    public float GroundY;
    public float MinX;
    public float MaxX;
    public float JumpSpeed;
    public float RunSpeed;
    public bool Grounded;
    public float CoyoteTime;
    public float AirControlMultiplier;
    public float RemainingCoyoteTime;
    public bool JumpQueued;
    public float JumpCutMultiplier;
    public float GroundAcceleration;
    public float AirAcceleration;
    public float AirControlExponent;
    public float WallSlideSpeed;
    public float WallSlideStickTime;
    public float WallSlideTimer;
    public bool IsWallSliding;
    public int WallSlideSide;
    public float WallJumpHorizontalSpeed;
    public float WallJumpCooldown;
    public float WallJumpCooldownRemaining;
}

public sealed class PhysicsBodyDescriptor : IComponentDescriptor<PhysicsBody>
{
    public string Name => "PhysicsBody";
    public int Version => 1;

    public void Serialize(ref PhysicsBody component, IComponentWriter writer)
    {
        writer.WriteBool("enabled", component.Enabled);
        writer.WriteFloat("gravity", component.Gravity);
        writer.WriteFloat("ground", component.GroundY);
        writer.WriteFloat("minX", component.MinX);
        writer.WriteFloat("maxX", component.MaxX);
        writer.WriteFloat("jumpSpeed", component.JumpSpeed);
        writer.WriteFloat("runSpeed", component.RunSpeed);
        writer.WriteFloat("coyoteTime", component.CoyoteTime);
        writer.WriteFloat("airControl", component.AirControlMultiplier);
        writer.WriteFloat("airControlExp", component.AirControlExponent);
        writer.WriteFloat("jumpCut", component.JumpCutMultiplier);
        writer.WriteBool("queueJump", component.JumpQueued);
        writer.WriteFloat("groundAccel", component.GroundAcceleration);
        writer.WriteFloat("airAccel", component.AirAcceleration);
        writer.WriteFloat("wallSlideSpeed", component.WallSlideSpeed);
        writer.WriteFloat("wallSlideStick", component.WallSlideStickTime);
        writer.WriteFloat("wallJumpSpeed", component.WallJumpHorizontalSpeed);
        writer.WriteFloat("wallJumpCooldown", component.WallJumpCooldown);
    }

    public PhysicsBody Deserialize(IComponentReader reader)
    {
        return new PhysicsBody
        {
            Enabled = reader.ReadBool("enabled", true),
            Gravity = reader.ReadFloat("gravity", 9.8f),
            GroundY = reader.ReadFloat("ground", float.PositiveInfinity),
            MinX = reader.ReadFloat("minX", float.NegativeInfinity),
            MaxX = reader.ReadFloat("maxX", float.PositiveInfinity),
            JumpSpeed = reader.ReadFloat("jumpSpeed", 0f),
            RunSpeed = reader.ReadFloat("runSpeed", 4f),
            CoyoteTime = reader.ReadFloat("coyoteTime", 0.1f),
            AirControlMultiplier = reader.ReadFloat("airControl", 1f),
            AirControlExponent = reader.ReadFloat("airControlExp", 1f),
            JumpCutMultiplier = reader.ReadFloat("jumpCut", 0.5f),
            JumpQueued = reader.ReadBool("queueJump", false),
            GroundAcceleration = reader.ReadFloat("groundAccel", 200f),
            AirAcceleration = reader.ReadFloat("airAccel", 100f),
            WallSlideSpeed = reader.ReadFloat("wallSlideSpeed", 80f),
            WallSlideStickTime = reader.ReadFloat("wallSlideStick", 0.15f),
            WallJumpHorizontalSpeed = reader.ReadFloat("wallJumpSpeed", 140f),
            WallJumpCooldown = reader.ReadFloat("wallJumpCooldown", 0.2f),
            WallSlideTimer = 0f,
            IsWallSliding = false,
            WallSlideSide = 0,
            WallJumpCooldownRemaining = 0f
        };
    }

    public void Validate(PhysicsBody component, ValidationContext context)
    {
        if (component.Gravity < 0f)
        {
            context.Error("Gravity must be non-negative.");
        }

        if (component.MinX > component.MaxX)
        {
            context.Error("PhysicsBody minX must be <= maxX.");
        }

        if (component.RunSpeed < 0f)
        {
            context.Error("Run speed must be non-negative.");
        }

        if (component.JumpSpeed < 0f)
        {
            context.Error("Jump speed must be non-negative.");
        }

        if (component.CoyoteTime < 0f)
        {
            context.Error("Coyote time must be non-negative.");
        }

        if (component.AirControlMultiplier < 0f)
        {
            context.Error("Air control multiplier must be non-negative.");
        }

        if (component.AirControlExponent < 0f)
        {
            context.Error("Air control exponent must be non-negative.");
        }

        if (component.JumpCutMultiplier < 0f || component.JumpCutMultiplier > 1f)
        {
            context.Error("Jump cut multiplier must be between 0 and 1.");
        }

        if (component.GroundAcceleration < 0f)
        {
            context.Error("Ground acceleration must be non-negative.");
        }

        if (component.AirAcceleration < 0f)
        {
            context.Error("Air acceleration must be non-negative.");
        }

        if (component.WallSlideSpeed < 0f)
        {
            context.Error("Wall slide speed must be non-negative.");
        }

        if (component.WallSlideStickTime < 0f)
        {
            context.Error("Wall slide stick time must be non-negative.");
        }

        if (component.WallJumpHorizontalSpeed < 0f)
        {
            context.Error("Wall jump speed must be non-negative.");
        }

        if (component.WallJumpCooldown < 0f)
        {
            context.Error("Wall jump cooldown must be non-negative.");
        }
    }
}
