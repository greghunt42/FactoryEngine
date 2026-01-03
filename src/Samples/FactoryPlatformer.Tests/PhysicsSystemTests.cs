using System;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;
using FactoryPlatformer.Systems;

namespace FactoryPlatformer.Tests;

public class PhysicsSystemTests
{
    [Fact]
    public void PhysicsSystem_StopsBody_OnStaticCollider()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new PhysicsSystem(), SystemPhase.Simulation);

        var ground = world.CreateEntity();
        ref var groundTransform = ref world.AddComponent<Transform2D>(ground);
        groundTransform = new Transform2D { X = 0, Y = 200 };
        ref var groundCollider = ref world.AddComponent<Collider2D>(ground);
        groundCollider = new Collider2D
        {
            Enabled = true,
            IsStatic = true,
            Width = 200,
            Height = 20
        };

        var entity = world.CreateEntity();
        ref var body = ref world.AddComponent<PhysicsBody>(entity);
        body = new PhysicsBody
        {
            Enabled = true,
            Gravity = 9.8f,
            GroundY = float.PositiveInfinity,
            MinX = -1000,
            MaxX = 1000
        };
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D { VX = 0, VY = 0 };
        ref var transform = ref world.AddComponent<Transform2D>(entity);
        transform = new Transform2D { X = 0, Y = 100 };
        ref var collider = ref world.AddComponent<Collider2D>(entity);
        collider = new Collider2D
        {
            Enabled = true,
            IsStatic = false,
            Width = 40,
            Height = 40
        };

        for (var i = 0; i < 10; i++)
        {
            world.Tick(0.5f);
        }

        var halfGround = groundCollider.Height * 0.5f;
        var halfPlayer = collider.Height * 0.5f;
        var expectedY = groundTransform.Y - halfGround - halfPlayer;
        Assert.True(Math.Abs(transform.Y - expectedY) < 0.01f, $"Expected {expectedY}, got {transform.Y}");
        Assert.Equal(0f, world.GetComponent<Velocity2D>(entity).VY);
        Assert.True(world.GetComponent<PhysicsBody>(entity).Grounded);
    }
}
