using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FactoryEngine.Core.Diagnostics;
using FeTools.Commands;

namespace FeTools.Tests;

[Collection("CLI.Serial")]
public class HashCommandTests
{
    [Fact]
    public void HashCommand_ComputesHashForSingleFile()
    {
        var tempDir = CreateTempDirectory();
        var filePath = Path.Combine(tempDir, "data.bin");
        File.WriteAllText(filePath, "hello world");

        var logger = new NdjsonLogger("Test", new StringWriter());
        var output = new StringWriter();
        var originalOut = Console.Out;
        try
        {
            Console.SetOut(output);
            var result = HashCommand.Run(new[] { filePath }, logger);

            Assert.Equal(0, result);
        }
        finally
        {
            Console.SetOut(originalOut);
            Directory.Delete(tempDir, true);
        }

        var line = output.ToString().Trim();
        var expectedHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("hello world"))).ToLowerInvariant();
        Assert.Contains(expectedHash, line, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith("data.bin", line, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void HashCommand_WritesJsonReportForDirectories()
    {
        var tempDir = CreateTempDirectory();
        try
        {
            var nestedDir = Path.Combine(tempDir, "nested");
            Directory.CreateDirectory(nestedDir);
            var fileA = Path.Combine(tempDir, "a.txt");
            var fileB = Path.Combine(nestedDir, "b.txt");
            File.WriteAllText(fileA, "aaa");
            File.WriteAllText(fileB, "bbb");
            var jsonPath = Path.Combine(tempDir, "report.json");

            var logger = new NdjsonLogger("Test", new StringWriter());
            var result = HashCommand.Run(new[] { "--json", jsonPath, tempDir }, logger);

            Assert.Equal(0, result);
            Assert.True(File.Exists(jsonPath));

            using var document = JsonDocument.Parse(File.ReadAllText(jsonPath));
            var root = document.RootElement;
            Assert.Equal("sha256", root.GetProperty("Algorithm").GetString(), ignoreCase: true);
            var files = root.GetProperty("Files").EnumerateArray().ToList();
            Assert.Equal(2, files.Count);
            Assert.Contains(files, f => f.GetProperty("RelativePath").GetString()?.EndsWith("a.txt") == true);
            Assert.Contains(files, f => f.GetProperty("RelativePath").GetString()?.EndsWith(Path.Combine("nested", "b.txt")) == true);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void HashCommand_ReturnsErrorWhenPathMissing()
    {
        var logger = new NdjsonLogger("Test", new StringWriter());
        var result = HashCommand.Run(Array.Empty<string>(), logger);

        Assert.Equal(1, result);
    }

    private static string CreateTempDirectory()
    {
        var dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(dir);
        return dir;
    }
}
