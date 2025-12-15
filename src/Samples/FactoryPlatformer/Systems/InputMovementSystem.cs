using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Systems;

public sealed class InputMovementSystem : SystemBase
{
    public InputMovementSystem()
    {
        DeclareAccess(builder => builder
            .Reads<Transform2D>()
            .Writes<Velocity2D>());
    }

    protected override void OnRun(SystemContext context)
    {
        var input = context.Services.Input;
        var state = input.GetActionState("move_right");
        foreach (var entry in World!.Query<Transform2D, Velocity2D>())
        {
            ref var velocity = ref World.GetComponent<Velocity2D>(entry.Entity);
            velocity.VX = state.IsPressed ? 2f : 0f;
        }
    }
}
