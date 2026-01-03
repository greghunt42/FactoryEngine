using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Systems;
using FactoryPlatformer;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class LevelGoalSystem : SystemBase
{
    private readonly FactoryPlatformerGameState _state;

    public LevelGoalSystem(FactoryPlatformerGameState state)
    {
        _state = state;
        DeclareAccess(builder => builder
            .Reads<Transform2D>()
            .Reads<Collider2D>()
            .Reads<LevelGoal>()
            .Reads<PlayerTag>());
    }

    protected override void OnRun(SystemContext context)
    {
        if (_state.LoopState != LevelLoopState.Playing)
        {
            return;
        }

        var player = FindPlayer();
        if (!player.IsValid)
        {
            return;
        }

        ref var playerTransform = ref World!.GetComponent<Transform2D>(player);
        ref var playerCollider = ref World.GetComponent<Collider2D>(player);

        foreach (var entity in World.Query(builder => builder
                     .All<LevelGoal>()
                     .All<Transform2D>()
                     .All<Collider2D>()))
        {
            ref var transform = ref World.GetComponent<Transform2D>(entity);
            ref var collider = ref World.GetComponent<Collider2D>(entity);
            if (!Intersects(playerTransform, playerCollider, transform, collider))
            {
                continue;
            }

            ref var goal = ref World.GetComponent<LevelGoal>(entity);
            _state.AddScore(goal.Bonus, goal.Message);
            _state.MarkVictory(goal.Message, goal.ResetDelay);
            World.DestroyEntity(entity);
            break;
        }
    }

    private Entity FindPlayer()
    {
        foreach (var entity in World!.Query(builder => builder
                     .All<PlayerTag>()
                     .All<Transform2D>()
                     .All<Collider2D>()))
        {
            return entity;
        }

        return Entity.Invalid;
    }

    private static bool Intersects(in Transform2D aTransform, in Collider2D aCollider, in Transform2D bTransform, in Collider2D bCollider)
    {
        var aBounds = CalculateBounds(aTransform, aCollider);
        var bBounds = CalculateBounds(bTransform, bCollider);
        return aBounds.MinX < bBounds.MaxX &&
               aBounds.MaxX > bBounds.MinX &&
               aBounds.MinY < bBounds.MaxY &&
               aBounds.MaxY > bBounds.MinY;
    }

    private static Bounds CalculateBounds(in Transform2D transform, in Collider2D collider)
    {
        var centerX = transform.X + collider.OffsetX;
        var centerY = transform.Y + collider.OffsetY;
        var halfWidth = collider.Width * 0.5f;
        var halfHeight = collider.Height * 0.5f;
        return new Bounds(centerX - halfWidth, centerX + halfWidth, centerY - halfHeight, centerY + halfHeight);
    }

    private readonly record struct Bounds(float MinX, float MaxX, float MinY, float MaxY);
}
