using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using FactoryEngine.Core.Diagnostics;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Services.Serialization;
using FeTools.Metadata;

namespace FeTools.Commands;

public static class AssetValidationCommand
{
    private sealed class Options
    {
        public List<string> Inputs { get; } = new();
        public string? JsonReportPath { get; set; }
        public string? CoverageReportPath { get; set; }
        public string? CoverageNdjsonPath { get; set; }
        public bool StrictMode { get; set; }
        public bool FixHashes { get; set; }
        public List<string> MetadataConfigPaths { get; } = new();
        public bool FailOnUnreferencedClips { get; set; }
        public List<string> RequiredGroups { get; } = new();
        public string? OptionsPath { get; set; }
    }

    private sealed record AssetReport(
        string Catalog,
        string Asset,
        string Type,
        string Path,
        string? DeclaredHash,
        string? ActualHash,
        bool HashMatch,
        IReadOnlyDictionary<string, string>? Metadata,
        IReadOnlyList<AssetIssue> Issues);

    private sealed record AssetIssue(string Message, string Severity)
    {
        public static AssetIssue Error(string message) => new(message, "error");
        public static AssetIssue Warning(string message) => new(message, "warning");
    }

    private sealed record AssetValidationReport(
        DateTimeOffset Timestamp,
        IReadOnlyList<AssetReport> Assets,
        int AssetCount,
        int FailedCount,
        SoundBankCoverageReport? SoundBankCoverage);

    private sealed record SoundBankCoverageReport(
        int SoundBankCount,
        int AudioClipCount,
        IReadOnlyList<GroupCoverage> Groups,
        IReadOnlyList<string> UnreferencedClips);

    private sealed record GroupCoverage(string Group, int SoundCount);

    private sealed record CoverageSummaryMetric(
        string Type,
        DateTimeOffset Timestamp,
        int SoundBankCount,
        int AudioClipCount,
        int GroupCount,
        int UnreferencedClipCount);

    private sealed record CoverageGroupMetric(string Type, string Group, int SoundCount);

    private sealed record CoverageUnreferencedMetric(string Type, string Asset);

    private sealed record CatalogInfo(string ManifestPath, AssetCatalog Catalog, Dictionary<string, string?> ComputedHashes);

    private sealed class OptionsFile
    {
        public List<string>? Inputs { get; set; }
        public string? JsonReportPath { get; set; }
        public string? CoverageJsonPath { get; set; }
        public string? CoverageNdjsonPath { get; set; }
        public bool? StrictMode { get; set; }
        public bool? FixHashes { get; set; }
        public List<string>? MetadataConfigs { get; set; }
        public bool? FailOnUnreferencedClips { get; set; }
        public List<string>? RequiredGroups { get; set; }
    }

    public static int Run(IEnumerable<string> args, NdjsonLogger logger)
    {
        var argArray = args as string[] ?? args.ToArray();
        Options options;
        try
        {
            options = ParseArgs(argArray);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
            return 1;
        }

        var warnings = new List<string>();
        var paths = ResolveManifestPaths(options.Inputs, warnings, logger);
        if (paths.Count == 0)
        {
            Warn("No asset catalog manifests were discovered.", warnings, logger);
            return options.StrictMode && warnings.Count > 0 ? 1 : 0;
        }

        var metadataConfigPaths = new HashSet<string>(options.MetadataConfigPaths, StringComparer.OrdinalIgnoreCase);
        foreach (var path in MetadataConfigLoader.DiscoverWorkspaceConfigs(logger))
        {
            metadataConfigPaths.Add(path);
        }
        foreach (var path in MetadataConfigLoader.DiscoverModuleConfigs(logger))
        {
            metadataConfigPaths.Add(path);
        }
        var metadataRules = MetadataConfigLoader.BuildRules(metadataConfigPaths, warnings, logger);

        var assetService = AssetPipeline.CreateDefaultService();
        var namespaces = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var catalogInfos = new List<CatalogInfo>();
        var errorCount = 0;
        var audioAssets = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        var soundBanks = new List<(string BankAssetId, SoundBank Bank)>();
        foreach (var manifest in paths)
        {
            try
            {
                var catalog = AssetCatalogManifest.LoadFromJson(manifest);
                if (!namespaces.Add(catalog.Namespace))
                {
                    logger.Error($"Duplicate catalog namespace '{catalog.Namespace}' detected.");
                    errorCount++;
                    continue;
                }

                assetService.RegisterCatalog(catalog);
                catalogInfos.Add(new CatalogInfo(manifest, catalog, new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)));
                logger.Info($"Loaded catalog '{catalog.Namespace}' from {manifest}");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load catalog '{manifest}'", ex);
                errorCount++;
            }
        }

        var assetReports = new List<AssetReport>();
        var assetResolver = catalogInfos.Count > 0
            ? AssetCatalogResolver.BuildResolver(catalogInfos.Select(info => info.Catalog))
            : null;
        var errors = new List<string>();
        foreach (var info in catalogInfos)
        {
            foreach (var entry in info.Catalog.Assets)
            {
                var assetId = new AssetId(info.Catalog.Namespace, entry.Key);
                var (actualHash, hashMatch, issues, fullPath, soundBank) = ValidateAssetRecord(assetService, info.Catalog, assetId, entry.Value, assetResolver, metadataRules, errors, warnings, logger);
                info.ComputedHashes[entry.Key] = actualHash;
                assetReports.Add(new AssetReport(
                    info.Catalog.Namespace,
                    assetId.ToString(),
                    entry.Value.Type,
                    fullPath,
                    entry.Value.Hash,
                    actualHash,
                    hashMatch,
                    entry.Value.Metadata,
                    issues));
                if (string.Equals(entry.Value.Type, AssetTypes.Audio, StringComparison.OrdinalIgnoreCase))
                {
                    var group = entry.Value.Metadata is not null &&
                                entry.Value.Metadata.TryGetValue("group", out var g)
                        ? g
                        : null;
                    audioAssets[assetId.ToString()] = group;
                }
                if (soundBank is not null)
                {
                    soundBanks.Add((assetId.ToString(), soundBank));
                }
            }
        }

        var coverage = BuildSoundBankCoverage(audioAssets, soundBanks, metadataRules, warnings, logger);

        foreach (var error in errors)
        {
            logger.Error(error);
        }

        var failed = assetReports.Count(report => report.Issues.Any(issue => issue.Severity == "error"));
        if (options.FixHashes)
        {
            FixCatalogHashes(catalogInfos, logger);
        }

        var totalAssets = assetReports.Count;

        if (!string.IsNullOrWhiteSpace(options.JsonReportPath))
        {
            var report = new AssetValidationReport(DateTimeOffset.UtcNow, assetReports, totalAssets, failed, coverage);
            WriteJsonReport(report, options.JsonReportPath!, logger);
        }

        var coverageFailed = EvaluateCoverageThresholds(coverage, options, logger);

        if (!string.IsNullOrWhiteSpace(options.CoverageReportPath) && coverage is not null)
        {
            WriteCoverageReport(coverage, options.CoverageReportPath!, logger);
        }
        else if (!string.IsNullOrWhiteSpace(options.CoverageReportPath))
        {
            logger.Warn("Sound bank coverage report requested but no audio clips or sound banks were discovered.");
        }

        if (!string.IsNullOrWhiteSpace(options.CoverageNdjsonPath))
        {
            if (coverage is not null)
            {
                WriteCoverageNdjson(coverage, options.CoverageNdjsonPath!, logger);
            }
            else
            {
                logger.Warn("Sound bank coverage NDJSON report requested but no audio clips or sound banks were discovered.");
            }
        }

        if (failed > 0 || errorCount > 0 || coverageFailed)
        {
            logger.Warn("Asset validation failed.");
            return 1;
        }

        if (options.StrictMode && warnings.Count > 0)
        {
            logger.Warn("Strict mode enabled and warnings encountered.");
            return 1;
        }

        logger.Info($"Validated {totalAssets} assets across {catalogInfos.Count} catalogs.");
        if (coverage is not null)
        {
            logger.Info($"Sound banks: {coverage.SoundBankCount}, audio clips: {coverage.AudioClipCount}.");
        }
        return 0;
    }

    private static (string? ActualHash, bool HashMatch, List<AssetIssue> Issues, string FullPath, SoundBank? SoundBank) ValidateAssetRecord(
        IAssetService assetService,
        AssetCatalog catalog,
        AssetId assetId,
        AssetRecord record,
        Func<AssetId, bool>? assetResolver,
        AssetMetadataRules metadataRules,
        List<string> errors,
        List<string> warnings,
        NdjsonLogger logger)
    {
        var issues = new List<AssetIssue>();
        var fullPath = Path.Combine(catalog.RootPath, record.Path);
        string? actualHash = null;
        var hashMatch = string.IsNullOrWhiteSpace(record.Hash);

        if (!File.Exists(fullPath))
        {
            var message = $"Asset file '{fullPath}' not found.";
            issues.Add(AssetIssue.Error(message));
            errors.Add($"{assetId}: {message}");
        }
        else
        {
            actualHash = ComputeFileHash(fullPath);
            if (!string.IsNullOrWhiteSpace(record.Hash))
            {
                hashMatch = string.Equals(record.Hash, actualHash, StringComparison.OrdinalIgnoreCase);
                if (!hashMatch)
                {
                    var message = $"Asset '{assetId}' hash mismatch. Declared {record.Hash} actual {actualHash}.";
                    issues.Add(AssetIssue.Error(message));
                    errors.Add(message);
                }
            }
        }

        SoundBank? soundBank = null;
        try
        {
            var loaded = LoadAsset(assetService, assetId, record.Type);
            soundBank = loaded as SoundBank;
        }
        catch (Exception ex)
        {
            var message = $"Failed to load asset '{assetId}': {ex.Message}";
            issues.Add(AssetIssue.Error(message));
            errors.Add(message);
        }

        ValidateMetadata(assetId, record, metadataRules, issues, warnings, logger);
        if (soundBank is not null)
        {
            ValidateSoundBank(soundBank, assetId, assetResolver, metadataRules, issues, errors, warnings, logger);
        }

        return (actualHash, hashMatch, issues, fullPath, soundBank);
    }

    private static string ComputeFileHash(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static Options ParseArgs(string[] args)
    {
        var options = new Options();
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--json":
                    if (!TryReadValue(args, ref i, out var report))
                    {
                        throw new ArgumentException("Missing value for --json");
                    }
                    options.JsonReportPath = report;
                    break;
                case "--coverage-json":
                    if (!TryReadValue(args, ref i, out var coveragePath))
                    {
                        throw new ArgumentException("Missing value for --coverage-json");
                    }
                    options.CoverageReportPath = coveragePath;
                    break;
                case "--coverage-ndjson":
                    if (!TryReadValue(args, ref i, out var coverageNdjsonPath))
                    {
                        throw new ArgumentException("Missing value for --coverage-ndjson");
                    }
                    options.CoverageNdjsonPath = coverageNdjsonPath;
                    break;
                case "--strict":
                    options.StrictMode = true;
                    break;
                case "--fix-hashes":
                    options.FixHashes = true;
                    break;
                case "--metadata-config":
                    if (!TryReadValue(args, ref i, out var configPath))
                    {
                        throw new ArgumentException("Missing value for --metadata-config");
                    }
                    options.MetadataConfigPaths.Add(configPath);
                    break;
                case "--fail-unreferenced-clips":
                    options.FailOnUnreferencedClips = true;
                    break;
                case "--require-groups":
                    if (!TryReadValue(args, ref i, out var groupList))
                    {
                        throw new ArgumentException("Missing value for --require-groups");
                    }
                    foreach (var group in groupList.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    {
                        options.RequiredGroups.Add(group);
                    }
                    break;
                case "--options":
                    if (!TryReadValue(args, ref i, out var optionsPath))
                    {
                        throw new ArgumentException("Missing value for --options");
                    }
                    options.OptionsPath = optionsPath;
                    break;
                default:
                    options.Inputs.Add(arg);
                    break;
            }
        }

        return options;
    }

    private static Options ApplyOptionsFile(Options options, NdjsonLogger logger)
    {
        if (string.IsNullOrWhiteSpace(options.OptionsPath))
        {
            return options;
        }

        try
        {
            var fullPath = Path.GetFullPath(options.OptionsPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException($"Options file '{fullPath}' not found.");
            }

            var json = File.ReadAllText(fullPath);
            var config = JsonSerializer.Deserialize<OptionsFile>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (config is null)
            {
                throw new InvalidOperationException("Options file was empty.");
            }

            MergeLists(options.Inputs, config.Inputs);
            MergeLists(options.MetadataConfigPaths, config.MetadataConfigs);
            MergeLists(options.RequiredGroups, config.RequiredGroups);

            options.JsonReportPath ??= config.JsonReportPath;
            options.CoverageReportPath ??= config.CoverageJsonPath;
            options.CoverageNdjsonPath ??= config.CoverageNdjsonPath;
            options.StrictMode = options.StrictMode || config.StrictMode.GetValueOrDefault();
            options.FixHashes = options.FixHashes || config.FixHashes.GetValueOrDefault();
            options.FailOnUnreferencedClips = options.FailOnUnreferencedClips || config.FailOnUnreferencedClips.GetValueOrDefault();

            return options;
        }
        catch (Exception ex)
        {
            logger.Error("Failed to load options file.", ex);
            throw;
        }

        static void MergeLists(List<string> target, IReadOnlyList<string>? source)
        {
            if (source is null || source.Count == 0)
            {
                return;
            }

            if (target.Count > 0)
            {
                return;
            }

            foreach (var value in source)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    target.Add(value);
                }
            }
        }
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
        var manifestInputs = inputs?.ToList() ?? new List<string>();
        if (manifestInputs.Count == 0)
        {
            var defaultDir = Path.Combine("data", "catalogs");
            manifestInputs.Add(defaultDir);
            logger.Info($"Using default catalog directory '{defaultDir}'.");
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var input in manifestInputs)
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
                var files = AssetCatalogDiscovery.EnumerateCatalogFiles(input);
                if (files.Count == 0)
                {
                    Warn($"No catalog manifests found in directory '{input}'.", warnings, logger);
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
                Warn($"Catalog input '{input}' not found.", warnings, logger);
            }
        }

        return paths.ToList();
    }

    private static object? LoadAsset(IAssetService assets, AssetId assetId, string type) =>
        type switch
        {
            AssetTypes.Prefab => assets.Load<PrefabDefinition>(assetId).Value,
            AssetTypes.Texture => assets.Load<TextureAsset>(assetId).Value,
            AssetTypes.Audio => assets.Load<AudioClipAsset>(assetId).Value,
            AssetTypes.SoundBank => assets.Load<SoundBank>(assetId).Value,
            AssetTypes.Bytes => assets.Load<byte[]>(assetId).Value,
            _ => throw new InvalidOperationException($"Unknown asset type '{type}'")
        };

    private static void FixCatalogHashes(IEnumerable<CatalogInfo> catalogs, NdjsonLogger logger)
    {
        foreach (var info in catalogs)
        {
            try
            {
                if (!File.Exists(info.ManifestPath))
                {
                    logger.Warn($"Cannot fix hashes; manifest '{info.ManifestPath}' not found.");
                    continue;
                }

                var json = JsonNode.Parse(File.ReadAllText(info.ManifestPath)) as JsonObject;
                if (json is null || json["assets"] is not JsonObject assetsNode)
                {
                    logger.Warn($"Cannot fix hashes; manifest '{info.ManifestPath}' has unexpected format.");
                    continue;
                }

                var modified = false;
                foreach (var (assetName, hash) in info.ComputedHashes)
                {
                    if (string.IsNullOrWhiteSpace(hash))
                    {
                        continue;
                    }

                    if (assetsNode[assetName] is JsonObject assetNode)
                    {
                        var current = assetNode["hash"]?.GetValue<string>();
                        if (!string.Equals(current, hash, StringComparison.OrdinalIgnoreCase))
                        {
                            assetNode["hash"] = hash;
                            modified = true;
                        }
                    }
                }

                if (modified)
                {
                    var options = new JsonSerializerOptions { WriteIndented = true };
                    File.WriteAllText(info.ManifestPath, json.ToJsonString(options));
                    logger.Info($"Updated asset hashes in '{info.ManifestPath}'.");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to update manifest '{info.ManifestPath}'", ex);
            }
        }
    }

    private static void WriteJsonReport(AssetValidationReport report, string destination, NdjsonLogger logger)
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
            logger.Info("Wrote asset validation report.");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to write asset validation report", ex);
        }
    }

    private static void WriteCoverageReport(SoundBankCoverageReport coverage, string destination, NdjsonLogger logger)
    {
        try
        {
            var json = JsonSerializer.Serialize(coverage, new JsonSerializerOptions { WriteIndented = true });
            if (destination == "-")
            {
                Console.WriteLine(json);
            }
            else
            {
                Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(destination)) ?? ".");
                File.WriteAllText(destination, json);
            }
            logger.Info("Wrote sound bank coverage report.");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to write sound bank coverage report", ex);
        }
    }

    private static void WriteCoverageNdjson(SoundBankCoverageReport coverage, string destination, NdjsonLogger logger)
    {
        try
        {
            var events = new List<object>
            {
                new CoverageSummaryMetric(
                    "soundbank.coverage.summary",
                    DateTimeOffset.UtcNow,
                    coverage.SoundBankCount,
                    coverage.AudioClipCount,
                    coverage.Groups.Count,
                    coverage.UnreferencedClips.Count)
            };

            foreach (var group in coverage.Groups)
            {
                events.Add(new CoverageGroupMetric("soundbank.coverage.group", group.Group, group.SoundCount));
            }

            foreach (var clip in coverage.UnreferencedClips)
            {
                events.Add(new CoverageUnreferencedMetric("soundbank.coverage.unreferenced", clip));
            }

            if (destination == "-")
            {
                foreach (var evt in events)
                {
                    Console.WriteLine(JsonSerializer.Serialize(evt));
                }
            }
            else
            {
                var fullPath = Path.GetFullPath(destination);
                Directory.CreateDirectory(Path.GetDirectoryName(fullPath) ?? ".");
                using var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using var writer = new StreamWriter(stream);
                foreach (var evt in events)
                {
                    writer.WriteLine(JsonSerializer.Serialize(evt));
                }
            }

            logger.Info("Wrote sound bank coverage NDJSON report.");
        }
        catch (Exception ex)
        {
            logger.Error("Failed to write sound bank coverage NDJSON report", ex);
        }
    }

    private static void Warn(string message, List<string> warnings, NdjsonLogger logger)
    {
        warnings.Add(message);
        logger.Warn(message);
    }

    private static void ValidateMetadata(
        AssetId assetId,
        AssetRecord record,
        AssetMetadataRules metadataRules,
        List<AssetIssue> issues,
        List<string> warnings,
        NdjsonLogger logger)
    {
        switch (record.Type)
        {
            case AssetTypes.Texture:
                if (RequireMetadata(assetId, record.Metadata, "format", "Texture asset", issues, warnings, logger, out var formatValue))
                {
                    ValidateMetadataValue(
                        assetId,
                        "Texture format",
                        formatValue!,
                        metadataRules.IsTextureFormatAllowed,
                        metadataRules.TextureFormats,
                        issues,
                        warnings,
                        logger);
                }
                break;
            case AssetTypes.Audio:
                if (RequireMetadata(assetId, record.Metadata, "group", "Audio asset", issues, warnings, logger, out var groupValue))
                {
                    ValidateMetadataValue(
                        assetId,
                        "Audio group",
                        groupValue!,
                        metadataRules.IsAudioGroupAllowed,
                        metadataRules.AudioGroups,
                        issues,
                        warnings,
                        logger);
                }
                break;
        }
    }

    private static bool RequireMetadata(
        AssetId assetId,
        IReadOnlyDictionary<string, string>? metadata,
        string key,
        string prefix,
        List<AssetIssue> issues,
        List<string> warnings,
        NdjsonLogger logger,
        out string? value)
    {
        if (metadata is not null &&
            metadata.TryGetValue(key, out var raw) &&
            !string.IsNullOrWhiteSpace(raw))
        {
            value = raw;
            return true;
        }

        value = null;
        var message = $"{prefix} '{assetId}' metadata missing '{key}'.";
        issues.Add(AssetIssue.Warning(message));
        Warn(message, warnings, logger);
        return false;
    }

    private static void ValidateMetadataValue(
        AssetId assetId,
        string description,
        string value,
        Func<string, bool> isAllowed,
        IReadOnlyList<string> allowedValues,
        List<AssetIssue> issues,
        List<string> warnings,
        NdjsonLogger logger)
    {
        if (isAllowed(value))
        {
            return;
        }

        var allowedText = string.Join(", ", allowedValues);
        var message = $"{description} '{value}' on asset '{assetId}' is not in the approved list ({allowedText}).";
        issues.Add(AssetIssue.Warning(message));
        Warn(message, warnings, logger);
    }

    private static bool EvaluateCoverageThresholds(
        SoundBankCoverageReport? coverage,
        Options options,
        NdjsonLogger logger)
    {
        var failed = false;
        var requireGroups = options.RequiredGroups;
        if (!options.FailOnUnreferencedClips && (requireGroups == null || requireGroups.Count == 0))
        {
            return false;
        }

        if (coverage is null)
        {
            if (options.FailOnUnreferencedClips)
            {
                logger.Error("Sound bank coverage thresholds enabled but no coverage data was produced (no audio clips or banks).");
                failed = true;
            }

            if (requireGroups is not null && requireGroups.Count > 0)
            {
                foreach (var group in requireGroups)
                {
                    logger.Error($"Required audio group '{group}' could not be validated because no coverage data was produced.");
                }
                failed = true;
            }

            return failed;
        }

        if (options.FailOnUnreferencedClips && coverage.UnreferencedClips.Count > 0)
        {
            logger.Error($"Found {coverage.UnreferencedClips.Count} audio asset(s) not referenced by any sound bank.");
            failed = true;
        }

        if (requireGroups is not null && requireGroups.Count > 0)
        {
            var coverageLookup = coverage.Groups.ToDictionary(g => g.Group, g => g.SoundCount, StringComparer.OrdinalIgnoreCase);
            foreach (var group in requireGroups)
            {
                if (string.IsNullOrWhiteSpace(group))
                {
                    continue;
                }

                if (!coverageLookup.TryGetValue(group, out var count) || count <= 0)
                {
                    logger.Error($"Required audio group '{group}' has no registered sounds in any sound bank.");
                    failed = true;
                }
            }
        }

        return failed;
    }

    private static SoundBankCoverageReport? BuildSoundBankCoverage(
        IReadOnlyDictionary<string, string?> audioClips,
        IReadOnlyList<(string BankAssetId, SoundBank Bank)> soundBanks,
        AssetMetadataRules metadataRules,
        List<string> warnings,
        NdjsonLogger logger)
    {
        if (audioClips.Count == 0 && soundBanks.Count == 0)
        {
            return null;
        }

        var clipUsage = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        var groupCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, bank) in soundBanks)
        {
            foreach (var (soundName, definition) in bank.Sounds)
            {
                var group = string.IsNullOrWhiteSpace(definition.Group) ? metadataRules.DefaultAudioGroup : definition.Group;
                groupCounts[group] = groupCounts.TryGetValue(group, out var count) ? count + 1 : 1;

                var clipKey = definition.Asset.ToString();
                if (!clipUsage.TryGetValue(clipKey, out var usage))
                {
                    usage = new List<string>();
                    clipUsage[clipKey] = usage;
                }
                usage.Add($"{bank.Name}:{soundName}");
            }
        }

        var unreferenced = new List<string>();
        foreach (var clip in audioClips.Keys)
        {
            if (clipUsage.ContainsKey(clip))
            {
                continue;
            }

            var message = $"Audio asset '{clip}' is not referenced by any sound bank.";
            Warn(message, warnings, logger);
            unreferenced.Add(clip);
        }

        var groups = groupCounts
            .OrderByDescending(pair => pair.Value)
            .ThenBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new GroupCoverage(pair.Key, pair.Value))
            .ToList();

        return new SoundBankCoverageReport(soundBanks.Count, audioClips.Count, groups, unreferenced);
    }

    private static void ValidateSoundBank(
        SoundBank bank,
        AssetId assetId,
        Func<AssetId, bool>? resolver,
        AssetMetadataRules metadataRules,
        List<AssetIssue> issues,
        List<string> errors,
        List<string> warnings,
        NdjsonLogger logger)
    {
        foreach (var (soundName, definition) in bank.Sounds)
        {
            if (definition.Asset == default || string.IsNullOrWhiteSpace(definition.Asset.Name))
            {
                var message = $"Sound bank '{assetId}' sound '{soundName}' is missing an asset reference.";
                issues.Add(AssetIssue.Error(message));
                errors.Add(message);
                continue;
            }

            if (resolver is not null && !resolver(definition.Asset))
            {
                var message = $"Sound bank '{assetId}' sound '{soundName}' references missing asset '{definition.Asset}'.";
                issues.Add(AssetIssue.Error(message));
                errors.Add(message);
            }

            if (string.IsNullOrWhiteSpace(definition.Group))
            {
                var warning = $"Sound bank '{assetId}' sound '{soundName}' is missing an audio group.";
                issues.Add(AssetIssue.Warning(warning));
                Warn(warning, warnings, logger);
            }
            else if (!metadataRules.IsAudioGroupAllowed(definition.Group))
            {
                var allowedText = string.Join(", ", metadataRules.AudioGroups);
                var warning = $"Sound bank '{assetId}' sound '{soundName}' uses non-standard audio group '{definition.Group}'. Expected one of: {allowedText}.";
                issues.Add(AssetIssue.Warning(warning));
                Warn(warning, warnings, logger);
            }
        }
    }
}
