using FactoryPlatformer;
using FactoryPlatformer.Components;

namespace FactoryPlatformer.Tests;

public class PlayerTuningConfigTests
{
    [Fact]
    public void PlayerTuningConfig_AppliesJumpCutAndAirDodge()
    {
        var config = new PlayerTuningConfig
        {
            JumpCut = 0.3f,
            AirDodgeEnabled = false,
            AirDodgeSpeed = 260f,
            AirDodgeCooldown = 0.8f,
            AirDodgeTrail = 0.35f
        };
        config.Normalize();

        var body = new PhysicsBody { JumpCutMultiplier = 0.6f };
        config.ApplyTo(ref body);
        Assert.Equal(0.3f, body.JumpCutMultiplier);

        var dodge = new AirDodge { Enabled = true, Speed = 200f, Cooldown = 1.2f };
        config.ApplyTo(ref dodge);
        Assert.False(dodge.Enabled);
        Assert.Equal(260f, dodge.Speed);
        Assert.Equal(0.8f, dodge.Cooldown);
        Assert.Equal(0.35f, dodge.EffectDuration);
    }
}
