using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;
using FactoryPlatformer.Systems;

namespace FactoryPlatformer.Tests;

public class InputMovementSystemTests
{
    [Fact]
    public void InputMovementSystem_AppliesJumpImpulse()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "test" };
        map.Actions.Add(new ActionBinding { Name = "jump" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        ref var transform = ref world.AddComponent<Transform2D>(entity);
        transform = new Transform2D { X = 0, Y = 0 };
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D();
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody { Enabled = true, Grounded = true, JumpSpeed = 150f, AirControlMultiplier = 0.5f, CoyoteTime = 0.1f, RemainingCoyoteTime = 0.1f };

        world.Input.SetActionState("jump", new ActionState(1f, true));
        world.Tick(0.016f);

        Assert.Equal(-150f, world.GetComponent<Velocity2D>(entity).VY);
        Assert.False(world.GetComponent<PhysicsBody>(entity).Grounded);
    }

    [Fact]
    public void InputMovementSystem_UsesConfiguredRunSpeed()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "movement" };
        map.Actions.Add(new ActionBinding { Name = "move_right" });
        map.Actions.Add(new ActionBinding { Name = "move_left" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        world.AddComponent<Transform2D>(entity);
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D();
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody { Enabled = true, Grounded = true, RunSpeed = 12f, AirControlMultiplier = 0.5f };

        world.Input.SetActionState("move_right", new ActionState(1f, true));
        world.Input.SetActionState("move_left", new ActionState(0f, false));
        world.Tick(0.016f);

        Assert.Equal(12f, world.GetComponent<Velocity2D>(entity).VX);
    }

    [Fact]
    public void InputMovementSystem_RespectsAcceleration()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "movement" };
        map.Actions.Add(new ActionBinding { Name = "move_right" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        world.AddComponent<Transform2D>(entity);
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D();
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody
        {
            Enabled = true,
            Grounded = true,
            RunSpeed = 20f,
            GroundAcceleration = 10f
        };

        world.Input.SetActionState("move_right", new ActionState(1f, true));
        world.Tick(0.5f);

        Assert.Equal(5f, world.GetComponent<Velocity2D>(entity).VX, 3);
    }

    [Fact]
    public void InputMovementSystem_TriggersWallSlide()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "movement" };
        map.Actions.Add(new ActionBinding { Name = "move_left" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        ref var transform = ref world.AddComponent<Transform2D>(entity);
        transform = new Transform2D { X = 0, Y = 0 };
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D { VX = 0, VY = 200 };
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody
        {
            Enabled = true,
            Grounded = false,
            MinX = 0,
            MaxX = 100,
            WallSlideSpeed = 50f,
            WallSlideStickTime = 0.2f
        };

        world.Input.SetActionState("move_left", new ActionState(1f, true));
        world.Tick(0.016f);

        var physics = world.GetComponent<PhysicsBody>(entity);
        Assert.Equal(50f, world.GetComponent<Velocity2D>(entity).VY, 3);
        Assert.True(physics.WallSlideTimer > 0f);
        Assert.True(physics.IsWallSliding);
    }

    [Fact]
    public void InputMovementSystem_PerformsWallJump()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "movement" };
        map.Actions.Add(new ActionBinding { Name = "jump" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        ref var transform = ref world.AddComponent<Transform2D>(entity);
        transform = new Transform2D { X = 0, Y = 0 };
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D { VX = 0, VY = 0 };
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody
        {
            Enabled = true,
            Grounded = false,
            WallSlideSide = -1,
            IsWallSliding = true,
            WallJumpHorizontalSpeed = 120f,
            JumpSpeed = 200f
        };

        world.Input.SetActionState("jump", new ActionState(1f, true));
        world.Tick(0.016f);

        var newVelocity = world.GetComponent<Velocity2D>(entity);
        Assert.Equal(120f, newVelocity.VX);
        Assert.Equal(-200f, newVelocity.VY);
        Assert.False(world.GetComponent<PhysicsBody>(entity).IsWallSliding);
    }

    [Fact]
    public void InputMovementSystem_WallJumpHonorsCooldown()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "movement" };
        map.Actions.Add(new ActionBinding { Name = "jump" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        world.AddComponent<Transform2D>(entity);
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D();
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody
        {
            Enabled = true,
            Grounded = false,
            IsWallSliding = true,
            WallSlideSide = 1,
            WallJumpHorizontalSpeed = 100f,
            JumpSpeed = 150f,
            WallJumpCooldown = 0.5f,
            WallJumpCooldownRemaining = 0.25f
        };

        world.Input.SetActionState("jump", new ActionState(1f, true));
        world.Tick(0.016f);

        var afterPhysics = world.GetComponent<PhysicsBody>(entity);
        var afterVelocity = world.GetComponent<Velocity2D>(entity);
        Assert.Equal(0f, afterVelocity.VX);
        Assert.Equal(0f, afterVelocity.VY);
        Assert.True(afterPhysics.WallJumpCooldownRemaining > 0f);
    }

    [Fact]
    public void InputMovementSystem_AppliesJumpCutOnRelease()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "movement" };
        map.Actions.Add(new ActionBinding { Name = "jump" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        world.AddComponent<Transform2D>(entity);
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D();
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody
        {
            Enabled = true,
            Grounded = true,
            JumpSpeed = 200f,
            JumpCutMultiplier = 0.5f,
            RemainingCoyoteTime = 0.2f
        };

        world.Input.SetActionState("jump", new ActionState(1f, true));
        world.Tick(0.016f);
        world.Input.SetActionState("jump", new ActionState(0f, false));
        world.Tick(0.016f);

        Assert.Equal(-100f, world.GetComponent<Velocity2D>(entity).VY, 3);
    }

    [Fact]
    public void InputMovementSystem_TriggersAirDodge()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "movement" };
        map.Actions.Add(new ActionBinding { Name = "move_right" });
        map.Actions.Add(new ActionBinding { Name = "air_dodge" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        world.AddComponent<Transform2D>(entity);
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D { VX = 5f, VY = -10f };
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody
        {
            Enabled = true,
            Grounded = false,
            RunSpeed = 20f
        };
        ref var dodge = ref world.AddComponent<AirDodge>(entity);
        dodge = new AirDodge
        {
            Enabled = true,
            Speed = 180f,
            Cooldown = 1f,
            EffectDuration = 0.2f
        };

        world.Input.SetActionState("move_right", new ActionState(1f, true));
        world.Input.SetActionState("air_dodge", new ActionState(1f, true));
        world.Tick(0.016f);

        var updatedVelocity = world.GetComponent<Velocity2D>(entity);
        var updatedDodge = world.GetComponent<AirDodge>(entity);
        Assert.Equal(180f, updatedVelocity.VX);
        Assert.True(updatedDodge.CooldownRemaining > 0f);
        Assert.True(updatedDodge.EffectTimer > 0f);
        Assert.Equal(1f, updatedDodge.LastDirection);
    }

    [Fact]
    public void InputMovementSystem_DampensAirDodgeEffectOverTime()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        var map = new ActionMap { Name = "movement" };
        map.Actions.Add(new ActionBinding { Name = "move_left" });
        map.Actions.Add(new ActionBinding { Name = "air_dodge" });
        world.Input.RegisterActionMap(map);

        var entity = world.CreateEntity();
        world.AddComponent<Transform2D>(entity);
        world.AddComponent<Velocity2D>(entity);
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody { Enabled = true, Grounded = false };
        ref var dodge = ref world.AddComponent<AirDodge>(entity);
        dodge = new AirDodge { Enabled = true, Speed = 150f, Cooldown = 0.5f, EffectDuration = 0.2f };

        world.Input.SetActionState("move_left", new ActionState(1f, true));
        world.Input.SetActionState("air_dodge", new ActionState(1f, true));
        world.Tick(0.016f);
        var afterTrigger = world.GetComponent<AirDodge>(entity);
        Assert.True(afterTrigger.EffectTimer > 0f);
        world.Input.SetActionState("air_dodge", new ActionState(0f, false));
        world.Tick(0.5f);
        Assert.Equal(0f, world.GetComponent<AirDodge>(entity).EffectTimer, 3);
    }
}
