using System.Reflection;
using FactoryEngine.Core.Diagnostics;
using FeTools.Commands;

namespace FeTools.Tests;

[Collection("CLI.Serial")]
public class DataValidationCommandTests
{
    [Fact]
    public void ValidateData_SucceedsWithValidPrefab_JsonAndCanonicalOutputs()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var prefabPath = Path.Combine(tempDir, "prefab.json");
            File.WriteAllText(prefabPath, """
            {
              "id": "test",
              "entities": [
                {
                  "components": [
                    { "name": "TestComponent", "data": { "value": 5 } }
                  ]
                }
              ]
            }
            """);

            var manifestPath = CreateDescriptorManifest(tempDir);
            var reportPath = Path.Combine(tempDir, "report.json");
            var canonicalDir = Path.Combine(tempDir, "canonical");
            var catalogPath = CreateEmptyCatalog(tempDir);

            var args = new[]
            {
                "--descriptor-manifest", manifestPath,
                "--catalog", catalogPath,
                "--json", reportPath,
                "--out", canonicalDir,
                tempDir
            };
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = DataValidationCommand.Run(args, logger);

            Assert.Equal(0, result);
            Assert.True(File.Exists(reportPath));
            Assert.True(File.Exists(Path.Combine(canonicalDir, "prefab.json")));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateData_FailsWhenDescriptorValidationErrors()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var manifestPath = CreateDescriptorManifest(tempDir);
            var prefabPath = Path.Combine(tempDir, "prefab.json");
            File.WriteAllText(prefabPath, """
            {
              "id": "test",
              "entities": [
                {
                  "components": [
                    { "name": "TestComponent", "data": { "value": -1 } }
                  ]
                }
              ]
            }
            """);

            var catalogPath = CreateEmptyCatalog(tempDir);
            var args = new[]
            {
                "--descriptor-manifest", manifestPath,
                "--catalog", catalogPath,
                prefabPath
            };
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = DataValidationCommand.Run(args, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateData_StrictFailsWhenNoDataFound()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var manifestPath = CreateDescriptorManifest(tempDir);
            var catalogPath = CreateEmptyCatalog(tempDir);
            var args = new[]
            {
                "--descriptor-manifest", manifestPath,
                "--catalog", catalogPath,
                "--strict",
                Path.Combine("nonexistent", "path")
            };
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = DataValidationCommand.Run(args, logger);
            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateData_AutoDiscoversDescriptorManifestFromModules()
    {
        var tempDir = CreateTempDirectory();
        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var prefabsDir = Path.Combine("data", "prefabs");
            var modulesDir = Path.Combine("data", "modules");
            var descriptorsDir = Path.Combine("data", "descriptors");
            Directory.CreateDirectory(prefabsDir);
            Directory.CreateDirectory(modulesDir);
            Directory.CreateDirectory(descriptorsDir);

            var prefabPath = Path.Combine(prefabsDir, "prefab.json");
            File.WriteAllText(prefabPath, """
            {
              "id": "auto",
              "entities": [
                {
                  "components": [
                    { "name": "TestComponent", "data": { "value": 1 } }
                  ]
                }
              ]
            }
            """);

            var descriptorManifestPath = CreateDescriptorManifest(descriptorsDir);
            var relativeDescriptorPath = Path.Combine("..", "descriptors", Path.GetFileName(descriptorManifestPath)).Replace("\\", "\\\\");
            var moduleManifestPath = Path.Combine(modulesDir, "sample.module.json");
            File.WriteAllText(moduleManifestPath, $$"""
            {
              "name": "SampleModule",
              "version": "1.0.0",
              "descriptorManifests": [
                "{{relativeDescriptorPath}}"
              ],
              "components": []
            }
            """);

            var args = new[]
            {
                prefabsDir
            };
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = DataValidationCommand.Run(args, logger);

            Assert.Equal(0, result);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateData_FailsWhenAssetReferenceMissing()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var prefabPath = Path.Combine(tempDir, "prefab.json");
            File.WriteAllText(prefabPath, """
            {
              "id": "asset_ref",
              "entities": [
                {
                  "components": [
                    { "name": "AssetReferenceComponent", "data": { "ns": "core", "name": "missing" } }
                  ]
                }
              ]
            }
            """);

            var catalogPath = CreateEmptyCatalog(tempDir);
            var args = new[]
            {
                "--descriptor-assembly", Assembly.GetExecutingAssembly().Location,
                "--catalog", catalogPath,
                prefabPath
            };
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = DataValidationCommand.Run(args, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateData_UsesMetadataConfigFlag()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var prefabPath = Path.Combine(tempDir, "prefab.json");
            File.WriteAllText(prefabPath, """
            {
              "id": "audio_prefab",
              "entities": [
                {
                  "components": [
                    { "name": "AudioGroupComponent", "data": { "group": "narration" } }
                  ]
                }
              ]
            }
            """);
            var catalogPath = CreateEmptyCatalog(tempDir);
            var metadataConfigPath = Path.Combine(tempDir, "metadata.json");
            File.WriteAllText(metadataConfigPath, """
            {
              "audioGroups": [ "narration" ]
            }
            """);

            var failureArgs = new[]
            {
                "--descriptor-assembly", Assembly.GetExecutingAssembly().Location,
                "--catalog", catalogPath,
                prefabPath
            };
            var failureLogger = new NdjsonLogger("Test", new StringWriter());
            var failureResult = DataValidationCommand.Run(failureArgs, failureLogger);
            Assert.Equal(1, failureResult);

            var successArgs = new[]
            {
                "--descriptor-assembly", Assembly.GetExecutingAssembly().Location,
                "--catalog", catalogPath,
                "--metadata-config", metadataConfigPath,
                prefabPath
            };
            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = DataValidationCommand.Run(successArgs, logger);

            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateData_UsesWorkspaceAndModuleMetadataConfigs()
    {
        var tempDir = CreateTempDirectory();
        var originalDirectory = Directory.GetCurrentDirectory();
        try
        {
            Directory.SetCurrentDirectory(tempDir);
            var dataDir = Path.Combine(tempDir, "data");
            var prefabsDir = Path.Combine(dataDir, "prefabs");
            Directory.CreateDirectory(prefabsDir);

            var workspacePrefab = Path.Combine(prefabsDir, "workspace.prefab.json");
            File.WriteAllText(workspacePrefab, """
            {
              "id": "workspace_prefab",
              "entities": [
                {
                  "components": [
                    { "name": "AudioGroupComponent", "data": { "group": "workspace" } }
                  ]
                }
              ]
            }
            """);

            var modulePrefab = Path.Combine(prefabsDir, "module.prefab.json");
            File.WriteAllText(modulePrefab, """
            {
              "id": "module_prefab",
              "entities": [
                {
                  "components": [
                    { "name": "AudioGroupComponent", "data": { "group": "module" } }
                  ]
                }
              ]
            }
            """);

            var catalogPath = CreateEmptyCatalog(tempDir);
            var args = new[]
            {
                "--descriptor-assembly", Assembly.GetExecutingAssembly().Location,
                "--catalog", catalogPath
            };

            var failureLogger = new NdjsonLogger("Test", new StringWriter());
            var failureResult = DataValidationCommand.Run(args, failureLogger);
            Assert.Equal(1, failureResult);

            var workspaceConfigPath = Path.Combine(dataDir, "catalogs", "asset-metadata.config.json");
            Directory.CreateDirectory(Path.GetDirectoryName(workspaceConfigPath)!);
            File.WriteAllText(workspaceConfigPath, """
            {
              "audioGroups": [ "workspace" ]
            }
            """);

            var metadataDir = Path.Combine(dataDir, "metadata");
            Directory.CreateDirectory(metadataDir);
            var moduleConfigPath = Path.Combine(metadataDir, "module.config.json");
            File.WriteAllText(moduleConfigPath, """
            {
              "audioGroups": [ "module" ]
            }
            """);

            var modulesDir = Path.Combine(dataDir, "modules");
            Directory.CreateDirectory(modulesDir);
            var manifestPath = Path.Combine(modulesDir, "auto.module.json");
            File.WriteAllText(manifestPath, """
            {
              "name": "AutoModule",
              "version": "1.0.0",
              "metadataConfigs": [
                "../metadata/module.config.json"
              ]
            }
            """);

            var successLogger = new NdjsonLogger("Test", new StringWriter());
            var successResult = DataValidationCommand.Run(args, successLogger);
            Assert.Equal(0, successResult);
        }
        finally
        {
            Directory.SetCurrentDirectory(originalDirectory);
            Directory.Delete(tempDir, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string CreateDescriptorManifest(string directory)
    {
        var manifestPath = Path.Combine(directory, "descriptors.json");
        File.WriteAllText(manifestPath, """
        {
          "components": [
            {
              "name": "TestComponent",
              "version": 1,
              "fields": [
                { "name": "value", "type": "int", "required": true, "min": 0 }
              ]
            }
          ]
        }
        """);
        return manifestPath;
    }

    private static string CreateEmptyCatalog(string directory)
    {
        var catalogPath = Path.Combine(directory, "catalog.json");
        File.WriteAllText(catalogPath, """
        {
          "namespace": "test",
          "root": ".",
          "assets": {}
        }
        """);
        return catalogPath;
    }
}
