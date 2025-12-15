using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;

namespace FactoryEngine.Core.Tests;

public class AudioServiceTests
{
    [Fact]
    public void TryResolveSound_ReturnsDefinition()
    {
        var service = new AudioService();
        var bank = new SoundBank("core");
        bank.Sounds["click"] = new SoundDefinition
        {
            Asset = new AssetId("core", "click"),
            Group = "ui",
            Volume = 0.5f
        };
        service.RegisterSoundBank(bank);

        var success = service.TryResolveSound("core", "click", out var definition);
        Assert.True(success);
        Assert.Equal("core:click", definition.Asset.ToString());
    }
}
