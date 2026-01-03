using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Tests;

public class AssetCatalogManifestTests
{
    [Fact]
    public void LoadFromJson_ReadsCatalog()
    {
        var root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        var catalogDir = Path.Combine(root, "catalogs");
        Directory.CreateDirectory(catalogDir);
        var manifestPath = Path.Combine(catalogDir, "core.json");
        File.WriteAllText(manifestPath, """
        {
          "namespace": "core",
          "root": "..",
          "assets": {
            "player_prefab": {
              "type": "Prefab",
              "path": "prefabs/player.json"
            },
            "step_audio": {
              "type": "Audio",
              "path": "audio/step.wav",
              "metadata": {
                "loop": "false"
              }
            }
          }
        }
        """);

        try
        {
            var catalog = AssetCatalogManifest.LoadFromJson(manifestPath);
            Assert.Equal("core", catalog.Namespace);
            Assert.Equal(Path.GetFullPath(root), catalog.RootPath);
            Assert.Equal(2, catalog.Assets.Count);
            Assert.Equal("Prefab", catalog.Assets["player_prefab"].Type);
            Assert.Equal("audio/step.wav", catalog.Assets["step_audio"].Path);
            Assert.Equal("false", catalog.Assets["step_audio"].Metadata?["loop"]);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }
}
