using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class MovementSystem : SystemBase
{
    public MovementSystem()
    {
        DeclareAccess(builder => builder
            .Reads<Velocity2D>()
            .Writes<Transform2D>());
    }

    protected override void OnRun(SystemContext context)
    {
        if (World is null)
        {
            return;
        }

        foreach (var entry in World.Query<Transform2D, Velocity2D>())
        {
            ref var transform = ref World.GetComponent<Transform2D>(entry.Entity);
            ref var velocity = ref World.GetComponent<Velocity2D>(entry.Entity);
            transform.X += velocity.VX * context.DeltaTime;
            transform.Y += velocity.VY * context.DeltaTime;
        }
    }
}
