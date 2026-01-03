using System;
using System.IO;
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

    [Fact]
    public void PlaySound_TracksActivePlaybackAndRaisesEvent()
    {
        var service = new AudioService();
        var bank = new SoundBank("core");
        bank.Sounds["step"] = new SoundDefinition { Asset = new AssetId("core", "step") };
        service.RegisterSoundBank(bank);
        SoundPlayback? played = null;
        service.SoundPlayed += playback => played = playback;

        service.PlaySound("core:step", new AudioParams(0.5f, 0f));

        Assert.True(played.HasValue);
        Assert.Single(service.ActiveSounds);
        Assert.Equal("core:step", service.ActiveSounds[0].SoundKey);
    }

    [Fact]
    public void Update_AutoStopsSoundsAfterLifetime()
    {
        var service = new AudioService();
        var bank = new SoundBank("core");
        bank.Sounds["step"] = new SoundDefinition { Asset = new AssetId("core", "step") };
        service.RegisterSoundBank(bank);
        SoundPlayback? stopped = null;
        service.SoundStopped += playback => stopped = playback;

        service.PlaySound("core:step", new AudioParams(1f, 0f, 0.1f));

        service.Update(0.05f);
        Assert.Single(service.ActiveSounds);

        service.Update(0.1f);
        Assert.True(stopped.HasValue);
        Assert.Empty(service.ActiveSounds);
    }

    [Fact]
    public void RegisterSoundBank_ThrowsWhenResolverMissingAsset()
    {
        var service = new AudioService();
        service.SetAssetResolver(asset => asset == new AssetId("core", "step"));

        var bank = new SoundBank("core");
        bank.Sounds["missing"] = new SoundDefinition { Asset = new AssetId("core", "missing") };

        var ex = Assert.Throws<InvalidOperationException>(() => service.RegisterSoundBank(bank));
        Assert.Contains("core:missing", ex.Message);
    }

    [Fact]
    public void PlaySound_LoadsClipWhenAssetServiceConfigured()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var audioDir = Path.Combine(tempDir, "audio");
            Directory.CreateDirectory(audioDir);
            var clipPath = Path.Combine(audioDir, "step.aud");
            File.WriteAllText(clipPath, "clip-bytes");
            var manifestPath = Path.Combine(tempDir, "core.catalog.json");
            File.WriteAllText(manifestPath, """
            {
              "namespace": "core",
              "root": ".",
              "assets": {
                "step": { "type": "Audio", "path": "audio/step.aud", "metadata": { "group": "sfx" } }
              }
            }
            """);

            var assets = AssetPipeline.CreateDefaultService();
            assets.RegisterCatalog(AssetCatalogManifest.LoadFromJson(manifestPath));

            var service = new AudioService();
            service.SetAssetService(assets);
            var bank = new SoundBank("core");
            bank.Sounds["step"] = new SoundDefinition { Asset = new AssetId("core", "step") };
            service.RegisterSoundBank(bank);

            service.PlaySound("core:step", new AudioParams(1f, 0f));

            Assert.True(service.TryGetLoadedClip(new AssetId("core", "step"), out var clip));
            Assert.NotNull(clip);
            Assert.Equal(
                Path.GetFullPath(clipPath),
                Path.GetFullPath(clip!.Path));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
