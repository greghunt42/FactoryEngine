using System.Linq;
using System.Reflection;
using System.Text.Json;
using FeTools.Commands;
using FactoryEngine.Core.Diagnostics;

namespace FeTools.Tests
{
public class ModuleValidationCommandTests
{
    [Fact]
    public void ValidateModules_SucceedsWithValidManifest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var manifestPath = Path.Combine(tempDir, "module.module.json");
            File.WriteAllText(manifestPath, """
            {
              "name": "Sample",
              "version": "1.0.0",
              "components": [
                "FeTools.Tests.ModuleTypes.SampleComponent"
              ],
              "systems": [
                { "name": "FeTools.Tests.ModuleTypes.SampleSystem", "phase": "Simulation" }
              ],
              "services": {
                "requires": ["AssetService"],
                "optional": ["AudioService"]
              }
            }
            """);

            var args = new[]
            {
                "--assembly", Assembly.GetExecutingAssembly().Location,
                manifestPath
            };

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ModuleValidationCommand.Run(args, logger);

            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateModules_FailsWhenTypeMissing()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var manifestPath = Path.Combine(tempDir, "module.module.json");
            File.WriteAllText(manifestPath, """
            {
              "name": "Sample",
              "version": "1.0.0",
              "components": [
                "Missing.Component"
              ],
              "systems": []
            }
            """);

            var args = new[]
            {
                "--assembly", Assembly.GetExecutingAssembly().Location,
                manifestPath
            };

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ModuleValidationCommand.Run(args, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateModules_StrictFailsOnWarnings()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var args = new[]
            {
                "--strict",
                tempDir
            };

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ModuleValidationCommand.Run(args, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateModules_LoadsYamlManifest()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var manifestPath = Path.Combine(tempDir, "module.module.yaml");
            File.WriteAllText(manifestPath, """
            name: Sample
            version: 1.0.0
            components:
              - FeTools.Tests.ModuleTypes.SampleComponent
            systems:
              - name: FeTools.Tests.ModuleTypes.SampleSystem
                phase: Simulation
            dependencies: []
            services:
              requires: [AssetService]
            """);

            var args = new[]
            {
                "--assembly", Assembly.GetExecutingAssembly().Location,
                manifestPath
            };

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ModuleValidationCommand.Run(args, logger);

            Assert.Equal(0, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateModules_DetectsMissingDependency()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var manifestPath = Path.Combine(tempDir, "module.module.json");
            File.WriteAllText(manifestPath, """
            {
              "name": "Sample",
              "version": "1.0.0",
              "dependencies": ["Missing"],
              "components": [],
              "systems": []
            }
            """);

            var args = new[]
            {
                manifestPath
            };

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ModuleValidationCommand.Run(args, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateModules_DetectsDependencyCycle()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var moduleA = Path.Combine(tempDir, "moduleA.module.yaml");
            var moduleB = Path.Combine(tempDir, "moduleB.module.yaml");
            File.WriteAllText(moduleA, """
            name: ModuleA
            version: 1.0.0
            dependencies:
              - ModuleB
            """);
            File.WriteAllText(moduleB, """
            name: ModuleB
            version: 1.0.0
            dependencies:
              - ModuleA
            """);

            var args = new[]
            {
                moduleA,
                moduleB
            };

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ModuleValidationCommand.Run(args, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateModules_FailsWhenDescriptorManifestMissing()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var manifestPath = Path.Combine(tempDir, "module.module.json");
            File.WriteAllText(manifestPath, """
            {
              "name": "Sample",
              "version": "1.0.0",
              "descriptorManifests": ["missing/descriptors.json"],
              "components": [],
              "systems": []
            }
            """);

            var args = new[]
            {
                manifestPath
            };

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ModuleValidationCommand.Run(args, logger);

            Assert.Equal(1, result);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void ValidateModules_WritesGraphDetailsToJsonReport()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var moduleA = Path.Combine(tempDir, "moduleA.module.json");
            var moduleB = Path.Combine(tempDir, "moduleB.module.json");
            var moduleC = Path.Combine(tempDir, "moduleC.module.json");
            File.WriteAllText(moduleA, """
            {
              "name": "ModuleA",
              "version": "1.0.0",
              "dependencies": ["ModuleB"]
            }
            """);
            File.WriteAllText(moduleB, """
            {
              "name": "ModuleB",
              "version": "1.0.0",
              "dependencies": ["ModuleA"]
            }
            """);
            File.WriteAllText(moduleC, """
            {
              "name": "ModuleC",
              "version": "1.0.0",
              "dependencies": ["MissingModule"]
            }
            """);

            var reportPath = Path.Combine(tempDir, "report.json");
            var args = new[]
            {
                "--json", reportPath,
                moduleA,
                moduleB,
                moduleC
            };

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = ModuleValidationCommand.Run(args, logger);

            Assert.Equal(1, result);
            Assert.True(File.Exists(reportPath));

            using var document = JsonDocument.Parse(File.ReadAllText(reportPath));
            var root = document.RootElement;
            Assert.Equal(JsonValueKind.Object, root.ValueKind);
            var modules = root.GetProperty("Modules");
            Assert.Equal(3, modules.GetArrayLength());

            var graph = root.GetProperty("Graph");
            var nodes = graph.GetProperty("Nodes").EnumerateArray().ToList();
            var nodeA = nodes.First(n => n.GetProperty("Name").GetString() == "ModuleA");
            Assert.Contains("ModuleB", nodeA.GetProperty("Dependencies").EnumerateArray().Select(v => v.GetString()));
            var nodeC = nodes.First(n => n.GetProperty("Name").GetString() == "ModuleC");
            Assert.Contains("MissingModule", nodeC.GetProperty("MissingDependencies").EnumerateArray().Select(v => v.GetString()));

            var cycles = graph.GetProperty("Cycles").EnumerateArray().ToList();
            Assert.True(cycles.Any(cycle =>
            {
                var names = cycle.EnumerateArray().Select(v => v.GetString()).Where(v => v is not null).ToList();
                return names.Count == 2 && names.Contains("ModuleA") && names.Contains("ModuleB");
            }), "Cycle report did not include ModuleA/ModuleB.");
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }
}
}
