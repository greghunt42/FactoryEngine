using System.IO;
using System.Linq;
using System.Text.Json;
using FactoryEngine.Core.Diagnostics;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Serialization;
using FeTools.Descriptors;
using FeTools.Metadata;
using FeTools.Modules;

namespace FeTools.Commands;

public static class DataValidationCommand
{
    private sealed record Options(
        List<string> DataPaths,
        List<string> DescriptorAssemblies,
        List<string> DescriptorManifests,
        List<string> CatalogPaths,
        string? JsonReportPath,
        bool StrictMode,
        string? CanonicalOutputDirectory,
        List<string> MetadataConfigPaths);

    public static int Run(string[] args, NdjsonLogger logger)
    {
        Options options;
        try
        {
            options = ParseArgs(args, logger);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            return 1;
        }
        var descriptorAssemblyPaths = DescriptorLoader.ResolveAssemblyPaths(options.DescriptorAssemblies);
        var assemblyDescriptors = DescriptorLoader.Load(descriptorAssemblyPaths, logger);

        var descriptorManifestPaths = ResolveDescriptorManifests(options.DescriptorManifests, logger);
        var manifestDescriptors = ManifestDescriptorLoader.Load(descriptorManifestPaths, logger);

        var descriptors = assemblyDescriptors.Concat(manifestDescriptors).ToList();
        if (descriptors.Count == 0)
        {
            logger.Error("No component descriptors were discovered. Use --descriptor-manifest or --descriptor-assembly to supply descriptors.");
            return 1;
        }

        var serialization = new SerializationService();
        DescriptorLoader.RegisterAll(serialization, descriptors);

        var warnings = new List<string>();
        var metadataConfigPaths = new HashSet<string>(options.MetadataConfigPaths ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
        foreach (var path in MetadataConfigLoader.DiscoverWorkspaceConfigs(logger))
        {
            metadataConfigPaths.Add(path);
        }
        foreach (var path in MetadataConfigLoader.DiscoverModuleConfigs(logger))
        {
            metadataConfigPaths.Add(path);
        }
        var metadataRules = MetadataConfigLoader.BuildRules(metadataConfigPaths, warnings, logger);
        serialization.SetMetadataRules(metadataRules);

        var assets = AssetPipeline.CreateDefaultService();
        var catalogPaths = ResolveCatalogPaths(options.CatalogPaths, logger);
        var catalogs = LoadCatalogs(catalogPaths, assets, logger);
        if (catalogs.Count > 0)
        {
            serialization.SetAssetResolver(AssetCatalogResolver.BuildResolver(catalogs));
        }

        var prefabs = new List<PrefabRecord>();
        prefabs.AddRange(LoadPrefabsFromCatalogs(catalogs, assets, logger));
        prefabs.AddRange(LoadPrefabsFromPaths(options.DataPaths, serialization, logger, warnings));

        if (prefabs.Count == 0)
        {
            Warn("No prefabs found to validate.", warnings, logger);
            return options.StrictMode && warnings.Count > 0 ? 1 : 0;
        }

        var failed = 0;
        var prefabReports = new List<PrefabReport>();
        foreach (var record in prefabs)
        {
            var issues = serialization.ValidatePrefab(record.Prefab);
            if (issues.Count == 0)
            {
                logger.Info($"Validated prefab '{record.Prefab.Id}'.");
            }
            else
            {
                failed++;
                foreach (var issue in issues)
                {
                    var entityLabel = issue.EntityName ?? "<entity>";
                    logger.Error($"Prefab '{issue.PrefabId}' entity '{entityLabel}' component '{issue.ComponentName}': {issue.Message}");
                }
            }

            prefabReports.Add(new PrefabReport(
                record.Prefab.Id,
                record.Source,
                issues.Select(i => new PrefabReportIssue(
                    i.ComponentName,
                    i.EntityName,
                    i.Message,
                    "error")).ToList()));
        }

        if (!string.IsNullOrWhiteSpace(options.CanonicalOutputDirectory))
        {
            WriteCanonicalPrefabs(prefabs, options.CanonicalOutputDirectory!, logger);
        }

        if (!string.IsNullOrWhiteSpace(options.JsonReportPath))
        {
            var report = new DataValidationReport(DateTimeOffset.UtcNow, prefabReports, prefabs.Count, failed);
            WriteJsonReport(report, options.JsonReportPath!, logger);
        }

        if (failed > 0)
        {
            logger.Warn($"Prefab validation failed for {failed} item(s).");
            return 1;
        }

        if (options.StrictMode && warnings.Count > 0)
        {
            logger.Warn("Strict mode enabled and warnings were encountered.");
            return 1;
        }

        logger.Info($"Validated {prefabs.Count} prefab(s) successfully.");
        return 0;
    }

    private static Options ParseArgs(string[] args, NdjsonLogger logger)
    {
        var dataPaths = new List<string>();
        var descriptorAssemblies = new List<string>();
        var descriptorManifests = new List<string>();
        var catalogPaths = new List<string>();
        var metadataConfigPaths = new List<string>();
        string? jsonReportPath = null;
        string? canonicalOutput = null;
        var strictMode = false;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--descriptor-assembly":
                case "-d":
                    if (!TryReadValue(args, ref i, out var descriptorPath))
                    {
                        throw new ArgumentException("Missing value for --descriptor-assembly");
                    }
                    descriptorAssemblies.Add(descriptorPath);
                    break;
                case "--descriptor-manifest":
                case "-m":
                    if (!TryReadValue(args, ref i, out var manifestPath))
                    {
                        throw new ArgumentException("Missing value for --descriptor-manifest");
                    }
                    descriptorManifests.Add(manifestPath);
                    break;
                case "--catalog":
                case "-c":
                    if (!TryReadValue(args, ref i, out var catalogPath))
                    {
                        throw new ArgumentException("Missing value for --catalog");
                    }
                    catalogPaths.Add(catalogPath);
                    break;
                case "--json":
                    if (!TryReadValue(args, ref i, out var reportPath))
                    {
                        throw new ArgumentException("Missing value for --json");
                    }
                    jsonReportPath = reportPath;
                    break;
                case "--metadata-config":
                    if (!TryReadValue(args, ref i, out var configPath))
                    {
                        throw new ArgumentException("Missing value for --metadata-config");
                    }
                    metadataConfigPaths.Add(configPath);
                    break;
                case "--strict":
                    strictMode = true;
                    break;
                case "--out":
                    if (!TryReadValue(args, ref i, out var outputDir))
                    {
                        throw new ArgumentException("Missing value for --out");
                    }
                    canonicalOutput = outputDir;
                    break;
                default:
                    if (arg.StartsWith("-", StringComparison.Ordinal))
                    {
                        throw new ArgumentException($"Unknown option '{arg}'");
                    }
                    dataPaths.Add(arg);
                    break;
            }
        }

        if (dataPaths.Count == 0)
        {
            var defaultPrefabs = Path.Combine("data", "prefabs");
            if (Directory.Exists(defaultPrefabs))
            {
                dataPaths.Add(defaultPrefabs);
                logger.Info($"Using default prefab directory '{defaultPrefabs}'.");
            }
        }

        return new Options(dataPaths, descriptorAssemblies, descriptorManifests, catalogPaths, jsonReportPath, strictMode, canonicalOutput, metadataConfigPaths);
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

    private static IReadOnlyList<string> ResolveCatalogPaths(List<string> catalogInputs, NdjsonLogger logger)
    {
        var resolved = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (catalogInputs.Count == 0)
        {
            var defaultDirectory = Path.Combine("data", "catalogs");
            var discovered = AssetCatalogDiscovery.EnumerateCatalogFiles(defaultDirectory);
            foreach (var catalog in discovered)
            {
                resolved.Add(catalog);
            }

            if (resolved.Count > 0)
            {
                logger.Info($"Using catalogs discovered under '{defaultDirectory}'.");
                return resolved.ToList();
            }

            var fallback = Path.Combine(defaultDirectory, "core.catalog.json");
            if (File.Exists(fallback))
            {
                resolved.Add(Path.GetFullPath(fallback));
                logger.Info($"Using fallback catalog '{fallback}'.");
            }

            return resolved.ToList();
        }

        foreach (var input in catalogInputs)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                continue;
            }

            if (File.Exists(input))
            {
                resolved.Add(Path.GetFullPath(input));
            }
            else if (Directory.Exists(input))
            {
                var files = AssetCatalogDiscovery.EnumerateCatalogFiles(input);
                if (files.Count == 0)
                {
                    logger.Warn($"No catalog manifests found in '{input}'.");
                }
                else
                {
                    foreach (var file in files)
                    {
                        resolved.Add(file);
                    }
                }
            }
            else
            {
                logger.Warn($"Catalog path '{input}' not found.");
            }
        }

        return resolved.ToList();
    }

    private static IReadOnlyList<string> ResolveDescriptorManifests(List<string> manifestPaths, NdjsonLogger logger)
    {
        if (manifestPaths.Count > 0)
        {
            return manifestPaths;
        }

        var moduleDescriptors = DiscoverModuleDescriptorManifests(logger);
        if (moduleDescriptors.Count > 0)
        {
            return moduleDescriptors;
        }

        var defaultManifest = Path.Combine("data", "descriptors", "core.descriptors.json");
        if (File.Exists(defaultManifest))
        {
            return new[] { defaultManifest };
        }

        return Array.Empty<string>();
    }

    private static IReadOnlyList<string> DiscoverModuleDescriptorManifests(NdjsonLogger logger)
    {
        var modulesDirectory = Path.Combine("data", "modules");
        if (!Directory.Exists(modulesDirectory))
        {
            return Array.Empty<string>();
        }

        var manifestFiles = ModuleManifestDiscovery.EnumerateManifestFiles(modulesDirectory);
        if (manifestFiles.Count == 0)
        {
            return Array.Empty<string>();
        }

        var descriptors = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var manifestPath in manifestFiles)
        {
            try
            {
                var manifest = ModuleManifest.Load(manifestPath);
                foreach (var descriptorPath in manifest.DescriptorManifests)
                {
                    if (descriptors.Add(descriptorPath))
                    {
                        logger.Info($"Discovered descriptor manifest '{descriptorPath}' from module '{manifest.Name}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to parse module manifest '{manifestPath}' for descriptor discovery: {ex.Message}");
            }
        }

        return descriptors.ToList();
    }

    private static IReadOnlyList<AssetCatalog> LoadCatalogs(IReadOnlyList<string> catalogPaths, IAssetService assets, NdjsonLogger logger)
    {
        var catalogs = new List<AssetCatalog>();
        foreach (var path in catalogPaths)
        {
            try
            {
                var catalog = AssetCatalogManifest.LoadFromJson(path);
                assets.RegisterCatalog(catalog);
                catalogs.Add(catalog);
                logger.Info($"Loaded catalog '{catalog.Namespace}' from {path}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load catalog '{path}'", ex);
            }
        }

        return catalogs;
    }

    private static List<PrefabRecord> LoadPrefabsFromCatalogs(IEnumerable<AssetCatalog> catalogs, IAssetService assets, NdjsonLogger logger)
    {
        var prefabs = new List<PrefabRecord>();
        foreach (var catalog in catalogs)
        {
            foreach (var entry in catalog.Assets)
            {
                if (!string.Equals(entry.Value.Type, AssetTypes.Prefab, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var assetId = new AssetId(catalog.Namespace, entry.Key);
                PrefabDefinition? prefab = null;
                try
                {
                    var prefabHandle = assets.Load<PrefabDefinition>(assetId);
                    if (prefabHandle.Value is not null)
                    {
                        logger.Debug($"Loaded prefab asset {assetId}");
                        prefab = prefabHandle.Value;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error($"Failed to load prefab asset '{assetId}'", ex);
                }

                if (prefab is not null)
                {
                    var relative = Path.Combine(catalog.Namespace, $"{entry.Key}.json");
                    prefabs.Add(new PrefabRecord(prefab, $"asset:{assetId}", relative));
                }
            }
        }

        return prefabs;
    }

    private static List<PrefabRecord> LoadPrefabsFromPaths(IEnumerable<string> dataPaths, SerializationService serialization, NdjsonLogger logger, List<string> warnings)
    {
        var prefabs = new List<PrefabRecord>();
        foreach (var path in dataPaths)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.json", SearchOption.AllDirectories))
                {
                    if (IsPrefabFile(file) && !IsDescriptorManifestPath(file))
                    {
                        var prefab = TryLoadPrefab(serialization, file, logger);
                        if (prefab is not null)
                        {
                            var relative = Path.ChangeExtension(Path.GetRelativePath(path, file), ".json");
                            prefabs.Add(new PrefabRecord(prefab, $"file:{file}", NormalizeRelativePath(relative)));
                        }
                    }
                }
            }
            else if (File.Exists(path))
            {
                if (IsDescriptorManifestPath(path))
                {
                    continue;
                }

                var prefab = TryLoadPrefab(serialization, path, logger);
                if (prefab is not null)
                {
                    var fileName = Path.GetFileName(path);
                    prefabs.Add(new PrefabRecord(prefab, $"file:{path}", NormalizeRelativePath(fileName)));
                }
            }
            else if (!string.IsNullOrWhiteSpace(path))
            {
                Warn($"Data path '{path}' not found.", warnings, logger);
            }
        }

        return prefabs;
    }

    private static PrefabDefinition? TryLoadPrefab(SerializationService serialization, string file, NdjsonLogger logger)
    {
        try
        {
            var prefab = serialization.LoadPrefabFromJson(file);
            logger.Debug($"Loaded prefab '{prefab.Id}' from {file}");
            return prefab;
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load prefab '{file}'", ex);
            return null;
        }
    }

    private static bool IsPrefabFile(string path)
    {
        var extension = Path.GetExtension(path);
        return string.Equals(extension, ".json", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(extension, ".prefab", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDescriptorManifestPath(string path)
    {
        var fileName = Path.GetFileName(path);
        if (fileName.Contains("descriptor", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("manifest", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var segments = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries);
        return segments.Any(segment => segment.Equals("descriptors", StringComparison.OrdinalIgnoreCase));
    }

    private static void WriteCanonicalPrefabs(IEnumerable<PrefabRecord> prefabs, string outputDirectory, NdjsonLogger logger)
    {
        foreach (var record in prefabs)
        {
            var relative = record.RelativePath ?? $"{record.Prefab.Id}.json";
            var path = Path.Combine(outputDirectory, relative);
            try
            {
                PrefabCanonicalWriter.Write(record.Prefab, path);
                logger.Info($"Wrote canonical prefab '{record.Prefab.Id}' to {path}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to write canonical prefab '{record.Prefab.Id}'", ex);
            }
        }
    }

    private static void WriteJsonReport(DataValidationReport report, string destination, NdjsonLogger logger)
    {
        try
        {
            if (destination == "-")
            {
                var json = JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine(json);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination)) ?? ".");
                File.WriteAllText(destination, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
            }
            logger.Info("Wrote JSON validation report.");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to write JSON report", ex);
        }
    }

    private static string NormalizeRelativePath(string relativePath)
    {
        var sanitized = relativePath.Replace('\\', '/');
        sanitized = sanitized.Replace("..", "__");
        sanitized = sanitized.TrimStart('/');
        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") + ".json" : sanitized;
    }

    private static void Warn(string message, List<string> warnings, NdjsonLogger logger)
    {
        warnings.Add(message);
        logger.Warn(message);
    }

    private sealed record PrefabRecord(PrefabDefinition Prefab, string Source, string? RelativePath);

    private sealed record PrefabReport(string PrefabId, string Source, IReadOnlyList<PrefabReportIssue> Issues);

    private sealed record PrefabReportIssue(string ComponentName, string? EntityName, string Message, string Severity);

    private sealed record DataValidationReport(DateTimeOffset Timestamp, IReadOnlyList<PrefabReport> Prefabs, int PrefabCount, int FailedCount);
}
