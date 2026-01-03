using System.IO;
using System.Linq;
using System.Text.Json;
using FeTools.Modules;
using FactoryEngine.Core.Diagnostics;

namespace FeTools.Commands;

public static class ModuleValidationCommand
{
    private sealed record Options(
        List<string> Inputs,
        List<string> AssemblyPaths,
        string? JsonReportPath,
        bool StrictMode);

    private sealed record ModuleReport(string Manifest, string? Name, IReadOnlyList<ModuleIssue> Issues);

    private sealed record ModuleValidationReport(
        IReadOnlyList<ModuleReport> Modules,
        ModuleGraphReport Graph);

    private sealed record ModuleGraphReport(
        IReadOnlyList<ModuleGraphNode> Nodes,
        IReadOnlyList<IReadOnlyList<string>> Cycles);

    private sealed record ModuleGraphNode(
        string Name,
        string Manifest,
        IReadOnlyList<string> Dependencies,
        IReadOnlyList<string> MissingDependencies);

    private sealed class ModuleReportBuilder
    {
        public ModuleManifest Manifest { get; }
        public List<ModuleIssue> Issues { get; }

        public ModuleReportBuilder(ModuleManifest manifest, List<ModuleIssue> issues)
        {
            Manifest = manifest;
            Issues = issues;
        }
    }

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

        var warnings = new List<string>();
        var manifestPaths = ResolveManifestPaths(options.Inputs, warnings, logger);
        if (manifestPaths.Count == 0)
        {
            Warn("No module manifests found.", warnings, logger);
            return options.StrictMode && warnings.Count > 0 ? 1 : 0;
        }

        var typeLookup = new TypeLookup(options.AssemblyPaths, logger);
        var manifestByPath = new Dictionary<string, ModuleManifest>(StringComparer.OrdinalIgnoreCase);
        var reportBuilders = new Dictionary<string, ModuleReportBuilder>(StringComparer.OrdinalIgnoreCase);
        var manifests = new List<ModuleManifest>();
        var errorCount = 0;

        foreach (var path in manifestPaths)
        {
            if (!File.Exists(path))
            {
                Warn($"Manifest '{path}' not found.", warnings, logger);
                continue;
            }

            ModuleManifest manifest;
            try
            {
                manifest = ModuleManifest.Load(path);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to parse manifest '{path}'", ex);
                errorCount++;
                continue;
            }

            manifestByPath[path] = manifest;
            manifests.Add(manifest);
            var issues = ModuleValidator.Validate(manifest, typeLookup).ToList();
            reportBuilders[path] = new ModuleReportBuilder(manifest, issues);

            foreach (var issue in issues)
            {
                if (issue.Severity == "error")
                {
                    logger.Error($"{Path.GetFileName(path)} ({issue.Field}): {issue.Message}");
                    errorCount++;
                }
                else
                {
                    Warn($"{Path.GetFileName(path)} ({issue.Field}): {issue.Message}", warnings, logger);
                }
            }

            if (issues.Count == 0)
            {
                logger.Info($"Validated module '{manifest.Name}' ({path}).");
            }
        }

        ModuleGraphReport graphReport = new(
            Array.Empty<ModuleGraphNode>(),
            Array.Empty<IReadOnlyList<string>>());

        if (manifests.Count > 0)
        {
            var graph = new ModuleDependencyGraph(manifests);
            var graphIssues = graph.Validate();
            foreach (var issue in graphIssues)
            {
                if (issue.Severity == "error")
                {
                    logger.Error($"{Path.GetFileName(issue.Source)} ({issue.Field}): {issue.Message}");
                    errorCount++;
                }
                else
                {
                    Warn($"{Path.GetFileName(issue.Source)} ({issue.Field}): {issue.Message}", warnings, logger);
                }

                if (reportBuilders.TryGetValue(issue.Source, out var builder))
                {
                    builder.Issues.Add(issue);
                }
                else if (manifestByPath.TryGetValue(issue.Source, out var manifest))
                {
                    reportBuilders[issue.Source] = new ModuleReportBuilder(manifest, new List<ModuleIssue> { issue });
                }
            }

            graphReport = BuildGraphReport(graph);
        }

        var reports = reportBuilders.Values
            .Select(builder => new ModuleReport(builder.Manifest.SourcePath, builder.Manifest.Name, builder.Issues.ToList()))
            .ToList();

        if (!string.IsNullOrWhiteSpace(options.JsonReportPath))
        {
            var output = new ModuleValidationReport(reports, graphReport);
            WriteJsonReport(output, options.JsonReportPath!, logger);
        }

        if (errorCount > 0)
        {
            return 1;
        }

        if (options.StrictMode && warnings.Count > 0)
        {
            logger.Warn("Strict mode enabled and warnings encountered.");
            return 1;
        }

        logger.Info($"Validated {reports.Count} module manifest(s).");
        return 0;
    }

    private static Options ParseArgs(string[] args)
    {
        var inputs = new List<string>();
        var assemblies = new List<string>();
        string? json = null;
        var strict = false;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--assembly":
                case "-a":
                    if (!TryReadValue(args, ref i, out var assembly))
                    {
                        throw new ArgumentException("Missing value for --assembly");
                    }
                    assemblies.Add(assembly);
                    break;
                case "--json":
                    if (!TryReadValue(args, ref i, out var report))
                    {
                        throw new ArgumentException("Missing value for --json");
                    }
                    json = report;
                    break;
                case "--strict":
                    strict = true;
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

        if (inputs.Count == 0)
        {
            inputs.Add(Path.Combine("data", "modules"));
        }

        return new Options(inputs, assemblies, json, strict);
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

    private static List<string> ResolveManifestPaths(IEnumerable<string> inputs, List<string> warnings, NdjsonLogger logger)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in inputs)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (File.Exists(input))
            {
                paths.Add(Path.GetFullPath(input));
            }
            else if (Directory.Exists(input))
            {
                var files = ModuleManifestDiscovery.EnumerateManifestFiles(Path.GetFullPath(input));
                if (files.Count == 0)
                {
                    Warn($"No manifests found in directory '{input}'.", warnings, logger);
                }
                else
                {
                    foreach (var file in files)
                    {
                        paths.Add(file);
                    }
                }
            }
            else
            {
                Warn($"Manifest input '{input}' not found.", warnings, logger);
            }
        }

        return paths.ToList();
    }

    private static ModuleGraphReport BuildGraphReport(ModuleDependencyGraph graph)
    {
        var nodes = graph.Nodes
            .Select(node => new ModuleGraphNode(
                node.Name,
                node.SourcePath,
                node.Dependencies.ToList(),
                node.MissingDependencies.ToList()))
            .ToList();
        var cycles = graph.Cycles.Select(cycle => (IReadOnlyList<string>)cycle.ToList()).ToList();
        return new ModuleGraphReport(nodes, cycles);
    }

    private static void WriteJsonReport(ModuleValidationReport report, string destination, NdjsonLogger logger)
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
            logger.Info("Wrote module validation report.");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to write module validation report", ex);
        }
    }

    private static void Warn(string message, List<string> warnings, NdjsonLogger logger)
    {
        warnings.Add(message);
        logger.Warn(message);
    }
}
