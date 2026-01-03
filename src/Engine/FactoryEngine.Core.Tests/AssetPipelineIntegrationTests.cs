using System.Text;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Services.Serialization;

namespace FactoryEngine.Core.Tests;

public class AssetPipelineIntegrationTests
{
    [Fact]
    public void AssetPipeline_LoadsPrefabTextureAudioAndSoundBank()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        try
        {
            var prefabsDir = Path.Combine(tempDir, "prefabs");
            var texturesDir = Path.Combine(tempDir, "textures");
            var audioDir = Path.Combine(tempDir, "audio");
            var soundbanksDir = Path.Combine(tempDir, "soundbanks");
            Directory.CreateDirectory(prefabsDir);
            Directory.CreateDirectory(texturesDir);
            Directory.CreateDirectory(audioDir);
            Directory.CreateDirectory(soundbanksDir);

            var prefabPath = Path.Combine(prefabsDir, "player.json");
            File.WriteAllText(prefabPath, """
            {
              "id": "player",
              "entities": [
                {
                  "components": [
                    { "name": "Transform2D", "data": { "x": 0, "y": 0 } }
                  ]
                }
              ]
            }
            """);
            var texturePath = Path.Combine(texturesDir, "player.tex");
            File.WriteAllText(texturePath, "texture-bytes");
            var audioPath = Path.Combine(audioDir, "step.aud");
            File.WriteAllText(audioPath, "audio-bytes");
            var soundBankPath = Path.Combine(soundbanksDir, "core.soundbank.json");
            File.WriteAllText(soundBankPath, """
            {
              "name": "core",
              "sounds": {
                "step": { "asset": "core:step", "group": "sfx", "volume": 0.8 }
              }
            }
            """);

            var manifestPath = Path.Combine(tempDir, "core.catalog.json");
            File.WriteAllText(manifestPath, """
            {
              "namespace": "core",
              "root": ".",
              "assets": {
                "player.prefab": { "type": "Prefab", "path": "prefabs/player.json" },
                "player": { "type": "Texture", "path": "textures/player.tex", "metadata": { "format": "placeholder" } },
                "step": { "type": "Audio", "path": "audio/step.aud", "metadata": { "group": "sfx" } },
                "core.soundbank": { "type": "SoundBank", "path": "soundbanks/core.soundbank.json" }
              }
            }
            """);

            var service = AssetPipeline.CreateDefaultService();
            var catalog = AssetCatalogManifest.LoadFromJson(manifestPath);
            service.RegisterCatalog(catalog);

            var prefabHandle = service.Load<PrefabDefinition>(new AssetId("core", "player.prefab"));
            Assert.True(prefabHandle.IsValid);
            Assert.Equal("player", prefabHandle.Value!.Id);

            var textureHandle = service.Load<TextureAsset>(new AssetId("core", "player"));
            Assert.Equal(
                Path.GetFullPath(texturePath),
                Path.GetFullPath(textureHandle.Value!.Path));
            Assert.Equal("texture-bytes", Encoding.UTF8.GetString(textureHandle.Value.Content));

            var audioHandle = service.Load<AudioClipAsset>(new AssetId("core", "step"));
            Assert.Equal(
                Path.GetFullPath(audioPath),
                Path.GetFullPath(audioHandle.Value!.Path));
            Assert.Equal("audio-bytes", Encoding.UTF8.GetString(audioHandle.Value.Content));

            var bankHandle = service.Load<SoundBank>(new AssetId("core", "core.soundbank"));
            Assert.Equal("core", bankHandle.Value!.Name);
            Assert.True(bankHandle.Value.Sounds.ContainsKey("step"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
