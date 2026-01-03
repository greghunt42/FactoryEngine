using System.IO;
using FactoryEngine.Core.Diagnostics;
using FeTools.Commands;

namespace FeTools.Tests;

[Collection("CLI.Serial")]
public class ValidateAllCommandTests
{
    [Fact]
    public void ValidateAll_RunsConfiguredCommands()
    {
        var tempDir = AssetValidationCommandTests.CreateTempDirectory();
        try
        {
            var sample = AssetValidationCommandTests.CreateSampleCatalog(tempDir, includeHashes: true);
            var configPath = Path.Combine(tempDir, "validate-all.json");
            File.WriteAllText(configPath, $$"""
            {
              "validateAssets": [ "{{sample.CatalogDirectory.Replace("\\", "\\\\")}}" ]
            }
            """);

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ValidateAllCommand.Run(new[] { "--config", configPath }, logger);

            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateAll_StopsOnFailureWhenFlagSet()
    {
        var tempDir = AssetValidationCommandTests.CreateTempDirectory();
        try
        {
            var sample = AssetValidationCommandTests.CreateSampleCatalog(tempDir, includeHashes: false);
            File.WriteAllText(sample.PrefabPath, "{");
            var configPath = Path.Combine(tempDir, "validate-all.json");
            File.WriteAllText(configPath, $$"""
            {
              "validateAssets": [ "{{sample.CatalogDirectory.Replace("\\", "\\\\")}}" ],
              "validateData": [ "--help" ]
            }
            """);

            var writer = new StringWriter();
            var logger = new NdjsonLogger("Test", writer);
            var result = ValidateAllCommand.Run(new[] { "--config", configPath, "--stop-on-failure" }, logger);

            Assert.Equal(1, result);
            Assert.DoesNotContain("validate-data", writer.ToString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }
}
