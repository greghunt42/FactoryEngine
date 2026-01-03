using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using FactoryEngine.Core.Diagnostics;
using FeTools.Commands;

namespace FeTools.Tests;

[Collection("CLI.Serial")]
public class AssetValidationCommandTests
{
    [Fact]
    public void ValidateAssets_UsesDefaultDirectoryWhenNoArgs()
    {
        var tempDir = CreateTempDirectory();
        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            CreateSampleCatalog(tempDir);

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(Array.Empty<string>(), logger);

            Assert.Equal(0, result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_LoadsCatalogsFromDirectoryInput()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir);
            var catalogDirectory = sample.CatalogDirectory;

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { catalogDirectory }, logger);

            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_WritesJsonReportWithHashes()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: true);
            var reportPath = Path.Combine(tempDir, "report.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            Assert.True(File.Exists(reportPath));

            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var rootElement = document.RootElement;
            var assets = rootElement.GetProperty("Assets");
            Assert.True(assets.GetArrayLength() >= 1);
            var prefabReport = assets.EnumerateArray().First(a => a.GetProperty("Asset").GetString() == "core:player.prefab");
            Assert.Equal(sample.CatalogNamespace, prefabReport.GetProperty("Catalog").GetString());
            Assert.True(prefabReport.GetProperty("HashMatch").GetBoolean());
            Assert.Equal(0, prefabReport.GetProperty("Issues").GetArrayLength());
            Assert.False(string.IsNullOrWhiteSpace(prefabReport.GetProperty("ActualHash").GetString()));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_DetectsHashMismatch()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir);
            WriteManifest(sample, prefabHash: "deadbeef");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { sample.CatalogDirectory }, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_FixHashesWritesHashesToManifest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false);
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--fix-hashes", sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);

            using var document = JsonDocument.Parse(File.ReadAllText(sample.CatalogPath));
            var assets = document.RootElement.GetProperty("assets");
            Assert.Equal(ComputeFileHash(sample.PrefabPath), assets.GetProperty("player.prefab").GetProperty("hash").GetString());
            Assert.Equal(ComputeFileHash(sample.TexturePath), assets.GetProperty("player").GetProperty("hash").GetString());
            Assert.Equal(ComputeFileHash(sample.AudioPath), assets.GetProperty("step").GetProperty("hash").GetString());
            Assert.Equal(ComputeFileHash(sample.SoundBankPath), assets.GetProperty("core.soundbank").GetProperty("hash").GetString());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_WarnsOnMissingMetadata()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: true, includeAudioMetadata: false);
            var reportPath = Path.Combine(tempDir, "warnings.json");
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            Assert.True(File.Exists(reportPath));

            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var assets = document.RootElement.GetProperty("Assets");
            var audioReport = assets.EnumerateArray().First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:step");
            var warnings = audioReport.GetProperty("Issues")
                .EnumerateArray()
                .Where(issue => string.Equals(issue.GetProperty("Severity").GetString(), "warning", StringComparison.OrdinalIgnoreCase))
                .ToList();
            Assert.NotEmpty(warnings);
            Assert.Contains(warnings, issue => issue.GetProperty("Message").GetString()?.Contains("metadata", StringComparison.OrdinalIgnoreCase) == true);

            var strictLogger = new NdjsonLogger("Test", new StringWriter());
            var strictResult = AssetValidationCommand.Run(new[] { "--strict", sample.CatalogDirectory }, strictLogger);
            Assert.Equal(1, strictResult);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_FailsWhenSoundBankReferencesMissingAsset()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false);
            File.WriteAllText(sample.SoundBankPath, """
            {
              "name": "core",
              "sounds": {
                "step": { "asset": "core:missing", "group": "sfx", "volume": 0.8 }
              }
            }
            """);
            var reportPath = Path.Combine(tempDir, "bank-report.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(1, result);
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var assets = document.RootElement.GetProperty("Assets");
            var bankReport = assets.EnumerateArray().First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:core.soundbank");
            var issues = bankReport.GetProperty("Issues").EnumerateArray().ToList();
            Assert.Contains(issues, issue =>
                string.Equals(issue.GetProperty("Severity").GetString(), "error", StringComparison.OrdinalIgnoreCase) &&
                issue.GetProperty("Message").GetString()?.Contains("missing asset", StringComparison.OrdinalIgnoreCase) == true);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_LoadsDefaultsFromOptionsFile()
    {
        var logger = new NdjsonLogger("Test", new StringWriter());
        var optionsPath = Path.Combine(AppContext.BaseDirectory, "TestData", "asset-options.json");
        var result = AssetValidationCommand.Run(new[] { "--options", optionsPath }, logger);
        Assert.Equal(0, result);
    }

    [Fact]
    public void ValidateAssets_WarnsWhenAudioClipUnreferenced()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false);
            File.WriteAllText(sample.SoundBankPath, """
            {
              "name": "core",
              "sounds": { }
            }
            """);
            var reportPath = Path.Combine(tempDir, "coverage.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var coverage = document.RootElement.GetProperty("SoundBankCoverage");
            Assert.Equal(1, coverage.GetProperty("SoundBankCount").GetInt32());
            Assert.Equal(1, coverage.GetProperty("AudioClipCount").GetInt32());
            var unreferenced = coverage.GetProperty("UnreferencedClips").EnumerateArray().Select(x => x.GetString()).ToList();
            Assert.Contains($"{sample.CatalogNamespace}:step", unreferenced);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_WritesCoverageJson()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false);
            var coveragePath = Path.Combine(tempDir, "coverage.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--coverage-json", coveragePath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            Assert.True(File.Exists(coveragePath));

            using var document = JsonDocument.Parse(File.ReadAllText(coveragePath));
            Assert.Equal(1, document.RootElement.GetProperty("SoundBankCount").GetInt32());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_WritesCoverageNdjson()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false);
            var unusedAudioPath = Path.Combine(sample.DataRoot, "audio", "unused.bin");
            File.WriteAllText(unusedAudioPath, "unused");

            var manifest = JsonNode.Parse(File.ReadAllText(sample.CatalogPath))!.AsObject();
            var assets = manifest["assets"]!.AsObject();
            assets["unused"] = new JsonObject
            {
                ["type"] = "Audio",
                ["path"] = "audio/unused.bin",
                ["metadata"] = new JsonObject { ["group"] = "sfx" }
            };
            File.WriteAllText(sample.CatalogPath, manifest.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

            var coveragePath = Path.Combine(tempDir, "coverage.ndjson");
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--coverage-ndjson", coveragePath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            Assert.True(File.Exists(coveragePath));
            var lines = File.ReadAllLines(coveragePath);
            Assert.True(lines.Length >= 3);

            using var summaryDoc = JsonDocument.Parse(lines[0]);
            Assert.Equal("soundbank.coverage.summary", summaryDoc.RootElement.GetProperty("Type").GetString());
            Assert.Equal(1, summaryDoc.RootElement.GetProperty("SoundBankCount").GetInt32());
            Assert.Equal(2, summaryDoc.RootElement.GetProperty("AudioClipCount").GetInt32());
            Assert.Equal(1, summaryDoc.RootElement.GetProperty("GroupCount").GetInt32());
            Assert.Equal(1, summaryDoc.RootElement.GetProperty("UnreferencedClipCount").GetInt32());

            using var groupDoc = JsonDocument.Parse(lines[1]);
            Assert.Equal("soundbank.coverage.group", groupDoc.RootElement.GetProperty("Type").GetString());
            Assert.Equal("sfx", groupDoc.RootElement.GetProperty("Group").GetString());
            Assert.Equal(1, groupDoc.RootElement.GetProperty("SoundCount").GetInt32());

            using var unrefDoc = JsonDocument.Parse(lines[2]);
            Assert.Equal("soundbank.coverage.unreferenced", unrefDoc.RootElement.GetProperty("Type").GetString());
            Assert.Equal($"{sample.CatalogNamespace}:unused", unrefDoc.RootElement.GetProperty("Asset").GetString());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_FailsWhenPrefabLoaderThrows()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false);
            File.WriteAllText(sample.PrefabPath, "{ invalid json ");

            var writer = new StringWriter();
            var logger = new NdjsonLogger("Test", writer);
            var result = AssetValidationCommand.Run(new[] { sample.CatalogDirectory }, logger);

            Assert.Equal(1, result);
            Assert.Contains("Failed to load asset", writer.ToString());
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_UsesMetadataConfigFile()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(
                tempDir,
                includeHashes: false,
                includeAudioMetadata: true,
                textureFormat: "ktx2",
                audioGroup: "narration",
                soundBankGroup: "narration");
            var configPath = Path.Combine(tempDir, "metadata.json");
            File.WriteAllText(configPath, """
            {
              "textureFormats": [ "ktx2" ],
              "audioGroups": [ "narration" ],
              "defaultAudioGroup": "narration"
            }
            """);
            var reportPath = Path.Combine(tempDir, "config-report.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--metadata-config", configPath, "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var assets = document.RootElement.GetProperty("Assets").EnumerateArray().ToList();
            var textureIssues = assets.First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:player").GetProperty("Issues").EnumerateArray().ToList();
            Assert.DoesNotContain(textureIssues, issue => issue.GetProperty("Message").GetString()?.Contains("Texture format", StringComparison.OrdinalIgnoreCase) == true);
            var audioIssues = assets.First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:step").GetProperty("Issues").EnumerateArray().ToList();
            Assert.DoesNotContain(audioIssues, issue => issue.GetProperty("Message").GetString()?.Contains("Audio group", StringComparison.OrdinalIgnoreCase) == true);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_UsesWorkspaceMetadataConfig()
    {
        var tempDir = CreateTempDirectory();
        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var sample = CreateSampleCatalog(tempDir, includeHashes: false, textureFormat: "ktx2");
            var configPath = Path.Combine(tempDir, "data", "catalogs", "asset-metadata.config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, """
            {
              "textureFormats": [ "ktx2" ]
            }
            """);
            var reportPath = Path.Combine(tempDir, "workspace-report.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var textureIssues = document.RootElement
                .GetProperty("Assets")
                .EnumerateArray()
                .First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:player")
                .GetProperty("Issues")
                .EnumerateArray()
                .ToList();
            Assert.DoesNotContain(textureIssues, issue => issue.GetProperty("Message").GetString()?.Contains("Texture format", StringComparison.OrdinalIgnoreCase) == true);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_UsesModuleMetadataConfig()
    {
        var tempDir = CreateTempDirectory();
        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var sample = CreateSampleCatalog(tempDir, includeHashes: false, audioGroup: "narration", soundBankGroup: "narration");
            var configPath = Path.Combine(tempDir, "data", "metadata", "module.config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            File.WriteAllText(configPath, """
            {
              "audioGroups": [ "narration" ],
              "defaultAudioGroup": "narration"
            }
            """);
            var modulesDir = Path.Combine(tempDir, "data", "modules");
            Directory.CreateDirectory(modulesDir);
            var manifestPath = Path.Combine(modulesDir, "sample.module.json");
            File.WriteAllText(manifestPath, """
            {
              "name": "SampleModule",
              "version": "1.0.0",
              "metadataConfigs": [
                "../metadata/module.config.json"
              ]
            }
            """);
            var reportPath = Path.Combine(tempDir, "module-report.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var audioIssues = document.RootElement
                .GetProperty("Assets")
                .EnumerateArray()
                .First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:step")
                .GetProperty("Issues")
                .EnumerateArray()
                .ToList();
            Assert.DoesNotContain(audioIssues, issue => issue.GetProperty("Message").GetString()?.Contains("Audio group", StringComparison.OrdinalIgnoreCase) == true);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_FailsWhenFailUnreferencedFlagSet()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false);
            File.WriteAllText(sample.SoundBankPath, """
            {
              "name": "core",
              "sounds": { }
            }
            """);

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--fail-unreferenced-clips", sample.CatalogDirectory }, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_FailsWhenRequiredGroupMissing()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false);
            var logger = new NdjsonLogger("Test", new StringWriter());

            var result = AssetValidationCommand.Run(new[] { "--require-groups", "music", sample.CatalogDirectory }, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_WarnsOnUnknownTextureFormat()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(tempDir, includeHashes: false, textureFormat: "custom");
            var reportPath = Path.Combine(tempDir, "texture-format.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var textureReport = document.RootElement
                .GetProperty("Assets")
                .EnumerateArray()
                .First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:player");
            var warnings = textureReport.GetProperty("Issues")
                .EnumerateArray()
                .Where(issue => string.Equals(issue.GetProperty("Severity").GetString(), "warning", StringComparison.OrdinalIgnoreCase))
                .Select(issue => issue.GetProperty("Message").GetString() ?? string.Empty)
                .ToList();
            Assert.Contains(warnings, message => message.Contains("Texture format", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAssets_WarnsOnUnknownAudioGroup()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var sample = CreateSampleCatalog(
                tempDir,
                includeHashes: false,
                includeAudioMetadata: true,
                textureFormat: "bin",
                audioGroup: "narration",
                soundBankGroup: "narration");
            var reportPath = Path.Combine(tempDir, "audio-group.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = AssetValidationCommand.Run(new[] { "--json", reportPath, sample.CatalogDirectory }, logger);

            Assert.Equal(0, result);
            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var assets = document.RootElement.GetProperty("Assets").EnumerateArray().ToList();
            var audioReport = assets.First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:step");
            var audioWarnings = audioReport.GetProperty("Issues").EnumerateArray().Where(issue =>
                string.Equals(issue.GetProperty("Severity").GetString(), "warning", StringComparison.OrdinalIgnoreCase)).Select(issue => issue.GetProperty("Message").GetString() ?? string.Empty).ToList();
            Assert.Contains(audioWarnings, message => message.Contains("Audio group", StringComparison.OrdinalIgnoreCase));

            var bankReport = assets.First(a => a.GetProperty("Asset").GetString() == $"{sample.CatalogNamespace}:core.soundbank");
            var bankWarnings = bankReport.GetProperty("Issues").EnumerateArray().Where(issue =>
                string.Equals(issue.GetProperty("Severity").GetString(), "warning", StringComparison.OrdinalIgnoreCase)).Select(issue => issue.GetProperty("Message").GetString() ?? string.Empty).ToList();
            Assert.Contains(bankWarnings, message => message.Contains("non-standard audio group", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    internal static SampleCatalog CreateSampleCatalog(
        string root,
        bool includeHashes = false,
        bool includeAudioMetadata = true,
        string textureFormat = "bin",
        string audioGroup = "sfx",
        string soundBankGroup = "sfx")
    {
        var dataRoot = Path.Combine(root, "data");
        var prefabsDir = Path.Combine(dataRoot, "prefabs");
        var texturesDir = Path.Combine(dataRoot, "textures");
        var audioDir = Path.Combine(dataRoot, "audio");
        var soundbanksDir = Path.Combine(dataRoot, "soundbanks");
        var catalogsDir = Path.Combine(dataRoot, "catalogs");
        Directory.CreateDirectory(prefabsDir);
        Directory.CreateDirectory(texturesDir);
        Directory.CreateDirectory(audioDir);
        Directory.CreateDirectory(soundbanksDir);
        Directory.CreateDirectory(catalogsDir);

        var prefabFile = Path.Combine(prefabsDir, "player.json");
        File.WriteAllText(prefabFile, """
        {
          "id": "player.prefab",
          "entities": [
            {
              "components": [
                { "name": "Transform2D", "data": { "x": 0, "y": 0 } }
              ]
            }
          ]
        }
        """);
        var textureFile = Path.Combine(texturesDir, "player.bin");
        File.WriteAllText(textureFile, "texture-bytes");
        var audioFile = Path.Combine(audioDir, "step.bin");
        File.WriteAllText(audioFile, "audio-bytes");
        var soundBankFile = Path.Combine(soundbanksDir, "core.soundbank.json");
        File.WriteAllText(soundBankFile, $$"""
        {
          "name": "core",
          "sounds": {
            "step": { "asset": "core:step", "group": "{{soundBankGroup}}", "volume": 0.8 }
          }
        }
        """);

        var catalogPath = Path.Combine(catalogsDir, "core.catalog.json");
        var sample = new SampleCatalog(dataRoot, catalogsDir, catalogPath, "core", prefabFile, textureFile, audioFile, soundBankFile);
        var prefabHash = includeHashes ? ComputeFileHash(prefabFile) : null;
        var textureHash = includeHashes ? ComputeFileHash(textureFile) : null;
        var audioHash = includeHashes ? ComputeFileHash(audioFile) : null;
        var bankHash = includeHashes ? ComputeFileHash(soundBankFile) : null;
        var bytesHash = includeHashes ? textureHash : null;
        WriteManifest(sample, prefabHash, textureHash, audioHash, bytesHash, includeAudioMetadata, bankHash, textureFormat, audioGroup);

        return sample;
    }

    internal static void WriteManifest(
        SampleCatalog sample,
        string? prefabHash = null,
        string? textureHash = null,
        string? audioHash = null,
        string? bytesHash = null,
        bool includeAudioMetadata = true,
        string? bankHash = null,
        string textureFormat = "bin",
        string audioGroup = "sfx")
    {
        var manifest = """
        {
          "namespace": "%NAMESPACE%",
          "root": "..",
          "assets": {
            "player.prefab": { "type": "Prefab", "path": "prefabs/player.json"%PREFAB_HASH% },
            "player": { "type": "Texture", "path": "textures/player.bin"%TEXTURE_HASH%, "metadata": { "format": "%TEXTURE_FORMAT%" } },
            "step": { "type": "Audio", "path": "audio/step.bin"%AUDIO_HASH%%AUDIO_METADATA% },
            "core.soundbank": { "type": "SoundBank", "path": "soundbanks/core.soundbank.json"%BANK_HASH% },
            "player.bytes": { "type": "Bytes", "path": "textures/player.bin"%BYTES_HASH% }
          }
        }
        """;

        string HashFragment(string? hash) => string.IsNullOrWhiteSpace(hash) ? string.Empty : $", \"hash\": \"{hash}\"";
        string AudioMetadataFragment() => includeAudioMetadata ? $", \"metadata\": {{ \"group\": \"{audioGroup}\" }}" : string.Empty;

        manifest = manifest
            .Replace("%NAMESPACE%", sample.CatalogNamespace)
            .Replace("%PREFAB_HASH%", HashFragment(prefabHash))
            .Replace("%TEXTURE_HASH%", HashFragment(textureHash))
            .Replace("%AUDIO_HASH%", HashFragment(audioHash))
            .Replace("%AUDIO_METADATA%", AudioMetadataFragment())
            .Replace("%BANK_HASH%", HashFragment(bankHash))
            .Replace("%BYTES_HASH%", HashFragment(bytesHash))
            .Replace("%TEXTURE_FORMAT%", textureFormat);

        File.WriteAllText(sample.CatalogPath, manifest);
    }

    internal static string ComputeFileHash(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    internal static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    internal sealed record SampleCatalog(
        string DataRoot,
        string CatalogDirectory,
        string CatalogPath,
        string CatalogNamespace,
        string PrefabPath,
        string TexturePath,
        string AudioPath,
        string SoundBankPath);
}
