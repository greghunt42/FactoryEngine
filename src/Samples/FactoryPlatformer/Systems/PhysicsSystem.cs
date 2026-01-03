using System.Collections.Generic;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class PhysicsSystem : SystemBase
{
    private readonly List<Bounds2D> _staticColliders = new();

    public PhysicsSystem()
    {
        DeclareAccess(builder => builder
            .Reads<PhysicsBody>()
            .Reads<Transform2D>()
            .Reads<Collider2D>()
            .Writes<Velocity2D>());
    }

    protected override void OnRun(SystemContext context)
    {
        BuildStaticColliderCache();
        foreach (var entity in World!.Query(builder => builder
                     .All<PhysicsBody>()
                     .All<Transform2D>()
                     .All<Velocity2D>()))
        {
            ref var body = ref World.GetComponent<PhysicsBody>(entity);
            if (!body.Enabled)
            {
                continue;
            }

            ref var velocity = ref World.GetComponent<Velocity2D>(entity);
            ref var transform = ref World.GetComponent<Transform2D>(entity);
            var deltaTime = context.DeltaTime;
            velocity.VY += body.Gravity * deltaTime;
            if (!body.Grounded)
            {
                body.RemainingCoyoteTime = Math.Max(0f, body.RemainingCoyoteTime - deltaTime);
            }
            else
            {
                body.RemainingCoyoteTime = body.CoyoteTime;
            }
            var newY = transform.Y + velocity.VY * context.DeltaTime;
            var grounded = false;

            if (World.HasComponent<Collider2D>(entity))
            {
                ref var collider = ref World.GetComponent<Collider2D>(entity);
                if (collider.Enabled && !collider.IsStatic)
                {
                    var resolved = ResolveVerticalCollisions(
                        transform,
                        collider,
                        transform.Y,
                        newY,
                        ref velocity);
                    if (resolved.HasValue)
                    {
                        newY = resolved.Value;
                        grounded = true;
                    }
                }
            }

            transform.Y = newY;
            var newX = transform.X + velocity.VX * deltaTime;
            transform.X = Math.Clamp(newX, body.MinX, body.MaxX);

            if (!grounded && transform.Y > body.GroundY)
            {
                transform.Y = body.GroundY;
                velocity.VY = 0f;
                grounded = true;
            }

            body.Grounded = grounded;
            if (grounded)
            {
                body.JumpQueued = false;
            }
        }
        _staticColliders.Clear();
    }

    private float? ResolveVerticalCollisions(
        Transform2D transform,
        Collider2D collider,
        float previousY,
        float newY,
        ref Velocity2D velocity)
    {
        var previousBounds = CalculateBounds(transform, collider);
        transform.Y = newY;
        var newBounds = CalculateBounds(transform, collider);
        var halfHeight = collider.Height * 0.5f;

        foreach (var staticBounds in _staticColliders)
        {
            if (!BoundsOverlap(newBounds, staticBounds))
            {
                continue;
            }

            var wasAbove = previousBounds.MaxY <= staticBounds.MinY;
            if (!wasAbove)
            {
                continue;
            }

            var targetY = staticBounds.MinY - halfHeight - collider.OffsetY;
            velocity.VY = 0f;
            return targetY;
        }

        return null;
    }

    private void BuildStaticColliderCache()
    {
        _staticColliders.Clear();
        foreach (var entry in World!.Query<Collider2D, Transform2D>())
        {
            ref var collider = ref World.GetComponent<Collider2D>(entry.Entity);
            if (!collider.Enabled || !collider.IsStatic)
            {
                continue;
            }

            ref var transform = ref World.GetComponent<Transform2D>(entry.Entity);
            _staticColliders.Add(CalculateBounds(transform, collider));
        }
    }

    private static Bounds2D CalculateBounds(Transform2D transform, Collider2D collider)
    {
        var centerX = transform.X + collider.OffsetX;
        var centerY = transform.Y + collider.OffsetY;
        var halfWidth = collider.Width * 0.5f;
        var halfHeight = collider.Height * 0.5f;
        return new Bounds2D(
            centerX - halfWidth,
            centerX + halfWidth,
            centerY - halfHeight,
            centerY + halfHeight);
    }

    private static bool BoundsOverlap(Bounds2D a, Bounds2D b)
    {
        return a.MinX < b.MaxX &&
               a.MaxX > b.MinX &&
               a.MinY < b.MaxY &&
               a.MaxY > b.MinY;
    }

    private readonly record struct Bounds2D(float MinX, float MaxX, float MinY, float MaxY);
}
