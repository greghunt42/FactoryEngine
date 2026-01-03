using System.Collections.Generic;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Tests;

public class AssetLoaderTests
{
    [Fact]
    public void FileAssetLoader_LoadsBytes()
    {
        var temp = Path.GetTempFileName();
        try
        {
            File.WriteAllText(temp, "hello");
            var loader = new FileAssetLoader();
            var record = new AssetRecord { Path = Path.GetFileName(temp), Type = "Bytes" };
            var root = Path.GetDirectoryName(temp)!;
            var bytes = loader.Load(record, root);
            Assert.Equal("hello", System.Text.Encoding.UTF8.GetString(bytes));
        }
        finally
        {
            File.Delete(temp);
        }
    }

    [Fact]
    public void PrefabJsonAssetLoader_LoadsPrefabDefinition()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "player.json");
        File.WriteAllText(path, """
        {
          "id": "player",
          "entities": [
            {
              "name": "Player",
              "components": [
                { "name": "Transform2D", "data": { "x": 0, "y": 0 } }
              ]
            }
          ]
        }
        """);
        var loader = new PrefabJsonAssetLoader();
        var record = new AssetRecord { Path = "player.json", Type = AssetTypes.Prefab };
        var prefab = loader.Load(record, tempDir);
        Assert.Equal("player", prefab.Id);
        Assert.Single(prefab.Entities);
    }

    [Fact]
    public void TextureFileLoader_ReturnsContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "player.tex");
        File.WriteAllText(path, "texture-bytes");
        var loader = new TextureFileLoader();
        var record = new AssetRecord { Path = "player.tex", Type = AssetTypes.Texture, Metadata = new Dictionary<string, string> { ["format"] = "txt" } };
        var texture = loader.Load(record, tempDir);
        Assert.Equal(path, texture.Path);
        Assert.Equal("texture-bytes", System.Text.Encoding.UTF8.GetString(texture.Content));
        Assert.Equal("txt", texture.Metadata?["format"]);
    }

    [Fact]
    public void AudioFileLoader_ReturnsContent()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "step.snd");
        File.WriteAllText(path, "audio-bytes");
        var loader = new AudioFileLoader();
        var record = new AssetRecord { Path = "step.snd", Type = AssetTypes.Audio };
        var clip = loader.Load(record, tempDir);
        Assert.Equal(path, clip.Path);
        Assert.Equal("audio-bytes", System.Text.Encoding.UTF8.GetString(clip.Content));
    }

    [Fact]
    public void SoundBankJsonLoader_LoadsBank()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var path = Path.Combine(tempDir, "core.soundbank.json");
        File.WriteAllText(path, """
        {
          "name": "core",
          "sounds": {
            "step": { "asset": "core:step", "group": "sfx", "volume": 0.75 }
          }
        }
        """);
        var loader = new SoundBankJsonLoader();
        var record = new AssetRecord { Path = "core.soundbank.json", Type = AssetTypes.SoundBank };
        var bank = loader.Load(record, tempDir);
        Assert.Equal("core", bank.Name);
        Assert.True(bank.Sounds.TryGetValue("step", out var definition));
        Assert.Equal("core:step", definition.Asset.ToString());
        Assert.Equal("sfx", definition.Group);
        Assert.Equal(0.75f, definition.Volume);
    }
}
