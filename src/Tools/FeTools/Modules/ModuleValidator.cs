using FactoryEngine.Core.Systems;
using System.IO;

namespace FeTools.Modules;

internal static class ModuleValidator
{
    private static readonly HashSet<string> KnownServiceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AssetService",
        "AudioService",
        "DiagnosticsService",
        "InputService",
        "RenderService",
        "SerializationService"
    };

    private static readonly HashSet<string> BuiltInPhases = Enum.GetNames<SystemPhase>()
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<ModuleIssue> Validate(ModuleManifest manifest, TypeLookup typeLookup)
    {
        var issues = new List<ModuleIssue>();
        var warnings = new List<ModuleIssue>();
        ValidateRequiredFields(manifest, issues);
        ValidatePhases(manifest, issues);
        ValidateComponents(manifest, typeLookup, issues, warnings);
        ValidateDependencies(manifest, issues, warnings);
        ValidateDescriptorManifests(manifest, issues, warnings);
        ValidateMetadataConfigs(manifest, issues, warnings);
        ValidateSystems(manifest, typeLookup, issues);
        ValidateServices(manifest, issues);
        issues.AddRange(warnings);
        return issues;
    }

    private static void ValidateRequiredFields(ModuleManifest manifest, List<ModuleIssue> issues)
    {
        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            issues.Add(ModuleIssue.Error(manifest.SourcePath, "name", "Module name is required."));
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            issues.Add(ModuleIssue.Error(manifest.SourcePath, "version", "Module version is required."));
        }
        else if (!Version.TryParse(manifest.Version, out _))
        {
            issues.Add(ModuleIssue.Error(manifest.SourcePath, "version", $"Module version '{manifest.Version}' is not a valid version string."));
        }
    }

    private static void ValidatePhases(ModuleManifest manifest, List<ModuleIssue> issues)
    {
        var defined = new HashSet<string>(BuiltInPhases, StringComparer.OrdinalIgnoreCase);
        foreach (var phase in manifest.Phases)
        {
            if (string.IsNullOrWhiteSpace(phase.Name))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "phases", "Custom phase name required."));
                continue;
            }

            if (!defined.Add(phase.Name))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "phases", $"Duplicate phase '{phase.Name}'."));
            }

            if (!string.IsNullOrWhiteSpace(phase.InsertAfter) && !defined.Contains(phase.InsertAfter))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "phases", $"Phase '{phase.Name}' references unknown insertAfter '{phase.InsertAfter}'."));
            }
        }
    }

    private static void ValidateComponents(ModuleManifest manifest, TypeLookup typeLookup, List<ModuleIssue> issues, List<ModuleIssue> warnings)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in manifest.Components)
        {
            if (string.IsNullOrWhiteSpace(component))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "components", "Component name cannot be empty."));
                continue;
            }

            if (!seen.Add(component))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "components", $"Duplicate component '{component}'."));
            }

            if (!typeLookup.TypeExists(component))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "components", $"Component type '{component}' not found in provided assemblies."));
            }
        }

        if (manifest.Components.Count == 0)
        {
            warnings.Add(ModuleIssue.Warning(manifest.SourcePath, "components", "No components declared."));
        }
    }

    private static void ValidateDependencies(ModuleManifest manifest, List<ModuleIssue> issues, List<ModuleIssue> warnings)
    {
        if (string.IsNullOrWhiteSpace(manifest.EngineVersion))
        {
            warnings.Add(ModuleIssue.Warning(manifest.SourcePath, "engineVersion", "engineVersion not specified."));
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var dependency in manifest.Dependencies)
        {
            if (string.IsNullOrWhiteSpace(dependency))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "dependencies", "Dependency name cannot be empty."));
                continue;
            }

            if (!seen.Add(dependency))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "dependencies", $"Duplicate dependency '{dependency}'."));
            }
        }
    }

    private static void ValidateSystems(ModuleManifest manifest, TypeLookup typeLookup, List<ModuleIssue> issues)
    {
        var definedPhases = new HashSet<string>(BuiltInPhases, StringComparer.OrdinalIgnoreCase);
        foreach (var phase in manifest.Phases)
        {
            if (!string.IsNullOrWhiteSpace(phase.Name))
            {
                definedPhases.Add(phase.Name);
            }
        }

        foreach (var system in manifest.Systems)
        {
            if (string.IsNullOrWhiteSpace(system.Name))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "systems", "System name is required."));
            }

            var phaseName = string.IsNullOrWhiteSpace(system.Phase) ? nameof(SystemPhase.Simulation) : system.Phase;
            if (!definedPhases.Contains(phaseName))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "systems", $"System '{system.Name}' references unknown phase '{phaseName}'."));
            }

            if (!typeLookup.TypeExists(system.Type))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "systems", $"System type '{system.Type}' not found in provided assemblies."));
            }
        }
    }

    private static void ValidateDescriptorManifests(ModuleManifest manifest, List<ModuleIssue> issues, List<ModuleIssue> warnings)
    {
        if (manifest.DescriptorManifests.Count == 0)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in manifest.DescriptorManifests)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "descriptorManifests", "Descriptor manifest path cannot be empty."));
                continue;
            }

            if (!seen.Add(path))
            {
                warnings.Add(ModuleIssue.Warning(manifest.SourcePath, "descriptorManifests", $"Duplicate descriptor manifest '{path}'."));
            }

            if (!File.Exists(path))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "descriptorManifests", $"Descriptor manifest '{path}' not found."));
            }
        }
    }

    private static void ValidateMetadataConfigs(ModuleManifest manifest, List<ModuleIssue> issues, List<ModuleIssue> warnings)
    {
        if (manifest.MetadataConfigs.Count == 0)
        {
            return;
        }

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var path in manifest.MetadataConfigs)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "metadataConfigs", "Metadata config path cannot be empty."));
                continue;
            }

            if (!seen.Add(path))
            {
                warnings.Add(ModuleIssue.Warning(manifest.SourcePath, "metadataConfigs", $"Duplicate metadata config '{path}'."));
            }

            if (!File.Exists(path))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "metadataConfigs", $"Metadata config '{path}' not found."));
            }
        }
    }

    private static void ValidateServices(ModuleManifest manifest, List<ModuleIssue> issues)
    {
        foreach (var entry in manifest.RequiredServices)
        {
            if (!KnownServiceNames.Contains(entry))
            {
                issues.Add(ModuleIssue.Error(manifest.SourcePath, "services", $"Unknown required service '{entry}'."));
            }
        }

        foreach (var entry in manifest.OptionalServices)
        {
            if (!KnownServiceNames.Contains(entry))
            {
                issues.Add(ModuleIssue.Warning(manifest.SourcePath, "services", $"Unknown optional service '{entry}'."));
            }
        }
    }
}

internal sealed record ModuleIssue(string Source, string Field, string Message, string Severity)
{
    public static ModuleIssue Error(string source, string field, string message) => new(source, field, message, "error");
    public static ModuleIssue Warning(string source, string field, string message) => new(source, field, message, "warning");
}
