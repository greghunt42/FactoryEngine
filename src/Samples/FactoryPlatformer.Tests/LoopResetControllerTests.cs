using FactoryPlatformer;

namespace FactoryPlatformer.Tests;

public class LoopResetControllerTests
{
    [Fact]
    public void LoopResetController_TriggersResetAfterDelay()
    {
        var state = new FactoryPlatformerGameState();
        state.RestartLoop(resetScore: false);
        var resetCount = 0;
        var controller = new LoopResetController(state, () => resetCount++);

        state.MarkFailure("ouch", 0.5f);
        controller.Update(0.1f);
        Assert.Equal(0, resetCount);
        controller.Update(0.4f);

        Assert.Equal(1, resetCount);
        Assert.Equal(LevelLoopState.Playing, state.LoopState);
    }

    [Fact]
    public void LoopResetController_ReportsPendingSeconds()
    {
        var state = new FactoryPlatformerGameState();
        state.RestartLoop(resetScore: false);
        var controller = new LoopResetController(state, () => { });

        state.MarkVictory("win", 1.0f);
        controller.Update(0f);
        Assert.Equal(1.0f, controller.PendingResetSeconds!.Value, 3);

        controller.Update(0.4f);
        Assert.InRange(controller.PendingResetSeconds ?? 0f, 0.5f, 0.7f);
    }
}
