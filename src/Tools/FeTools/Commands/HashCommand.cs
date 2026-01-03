using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using FactoryEngine.Core.Diagnostics;

namespace FeTools.Commands;

public static class HashCommand
{
    private sealed record Options(
        List<string> Inputs,
        string Algorithm,
        string? JsonOutput);

    private sealed record HashEntry(string Path, string RelativePath, string Hash, long Size);

    private sealed record HashReport(DateTimeOffset Timestamp, string Algorithm, IReadOnlyList<HashEntry> Files);

    public static int Run(string[] args, NdjsonLogger logger)
    {
        Options options;
        try
        {
            options = ParseArgs(args);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            return 1;
        }

        if (options.Inputs.Count == 0)
        {
            logger.Error("No files or directories supplied. Provide one or more paths to hash.");
            return 1;
        }

        var files = ResolveFiles(options.Inputs, logger);
        if (files.Count == 0)
        {
            logger.Warn("No files discovered for hashing.");
            return 1;
        }

        var entries = new List<HashEntry>();
        var failures = 0;
        foreach (var file in files)
        {
            try
            {
                var hash = ComputeHash(file, options.Algorithm);
                var relative = Path.GetRelativePath(Environment.CurrentDirectory, file);
                var size = new FileInfo(file).Length;
                entries.Add(new HashEntry(file, relative, hash, size));
                Console.WriteLine($"{hash}  {relative}");
            }
            catch (Exception ex)
            {
                failures++;
                logger.Error($"Failed to hash '{file}': {ex.Message}");
            }
        }

        if (!string.IsNullOrWhiteSpace(options.JsonOutput))
        {
            WriteJsonReport(new HashReport(DateTimeOffset.UtcNow, options.Algorithm, entries), options.JsonOutput!, logger);
        }

        return failures > 0 ? 1 : 0;
    }

    private static Options ParseArgs(string[] args)
    {
        var inputs = new List<string>();
        string algorithm = "sha256";
        string? json = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--algo":
                case "--algorithm":
                    if (!TryReadValue(args, ref i, out var algoValue))
                    {
                        throw new ArgumentException("Missing value for --algo");
                    }
                    algorithm = algoValue;
                    break;
                case "--json":
                    if (!TryReadValue(args, ref i, out var jsonValue))
                    {
                        throw new ArgumentException("Missing value for --json");
                    }
                    json = jsonValue;
                    break;
                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown option '{arg}'");
                    }
                    inputs.Add(arg);
                    break;
            }
        }

        return new Options(inputs, NormalizeAlgorithm(algorithm), json);
    }

    private static List<string> ResolveFiles(IEnumerable<string> inputs, NdjsonLogger logger)
    {
        var files = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (File.Exists(input))
            {
                files.Add(Path.GetFullPath(input));
                continue;
            }

            if (Directory.Exists(input))
            {
                var found = false;
                foreach (var file in Directory.EnumerateFiles(input, "*", SearchOption.AllDirectories))
                {
                    files.Add(Path.GetFullPath(file));
                    found = true;
                }

                if (!found)
                {
                    logger.Warn($"Directory '{input}' does not contain any files.");
                }
                continue;
            }

            logger.Warn($"Path '{input}' not found.");
        }

        return files.ToList();
    }

    private static string ComputeHash(string path, string algorithm)
    {
        using var stream = File.OpenRead(path);
        return algorithm switch
        {
            "sha1" => Convert.ToHexString(SHA1.HashData(stream)).ToLowerInvariant(),
            "md5" => Convert.ToHexString(MD5.HashData(stream)).ToLowerInvariant(),
            _ => Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant()
        };
    }

    private static void WriteJsonReport(HashReport report, string destination, NdjsonLogger logger)
    {
        try
        {
            var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
            if (destination == "-")
            {
                Console.WriteLine(json);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination)) ?? ".");
                File.WriteAllText(destination, json);
            }
            logger.Info("Wrote hash report.");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to write hash report", ex);
        }
    }

    private static string NormalizeAlgorithm(string algo)
    {
        if (string.IsNullOrWhiteSpace(algo))
        {
            return "sha256";
        }

        return algo.Trim().ToLowerInvariant() switch
        {
            "sha1" => "sha1",
            "md5" => "md5",
            _ => "sha256"
        };
    }

    private static bool TryReadValue(string[] args, ref int index, out string value)
    {
        index++;
        if (index >= args.Length)
        {
            value = string.Empty;
            return false;
        }

        value = args[index];
        return true;
    }
}
