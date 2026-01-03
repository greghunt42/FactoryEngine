using System.Linq;

namespace FeTools.Modules;

internal sealed class ModuleDependencyGraph
{
    private readonly Dictionary<string, ModuleManifest> _manifests = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> _edges = new(StringComparer.OrdinalIgnoreCase);
    private List<ModuleDependencyNode>? _nodes;
    private List<IReadOnlyList<string>>? _cycles;

    public ModuleDependencyGraph(IEnumerable<ModuleManifest> manifests)
    {
        foreach (var manifest in manifests)
        {
            _manifests[manifest.Name] = manifest;
            _edges[manifest.Name] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        foreach (var manifest in manifests)
        {
            foreach (var dependency in manifest.Dependencies)
            {
                _edges[manifest.Name].Add(dependency);
            }
        }
    }

    public IReadOnlyList<ModuleDependencyNode> Nodes => _nodes ??= BuildNodes();

    public IReadOnlyList<IReadOnlyList<string>> Cycles => _cycles ??= BuildCycles();

    public IReadOnlyList<ModuleIssue> Validate()
    {
        var issues = new List<ModuleIssue>();
        foreach (var node in Nodes)
        {
            foreach (var missing in node.MissingDependencies)
            {
                issues.Add(ModuleIssue.Error(node.SourcePath, "dependencies", $"Module '{node.Name}' depends on '{missing}' which was not provided."));
            }
        }

        foreach (var cycle in Cycles)
        {
            if (cycle.Count == 0)
            {
                continue;
            }

            var sourceName = cycle[0];
            var sourcePath = _manifests.TryGetValue(sourceName, out var manifest)
                ? manifest.SourcePath
                : _manifests.Values.FirstOrDefault()?.SourcePath ?? string.Empty;
            issues.Add(ModuleIssue.Error(sourcePath, "dependencies", $"Dependency cycle detected: {string.Join(" -> ", cycle)}"));
        }

        return issues;
    }

    private List<ModuleDependencyNode> BuildNodes()
    {
        var nodes = new List<ModuleDependencyNode>();
        foreach (var manifest in _manifests.Values.OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase))
        {
            var dependencies = _edges.TryGetValue(manifest.Name, out var deps)
                ? deps.OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList()
                : new List<string>();
            var missing = dependencies.Where(dep => !_manifests.ContainsKey(dep)).ToList();
            nodes.Add(new ModuleDependencyNode(manifest.Name, manifest.SourcePath, dependencies, missing));
        }

        return nodes;
    }

    private List<IReadOnlyList<string>> BuildCycles()
    {
        var cycles = new List<IReadOnlyList<string>>();
        var cycleKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var path = new List<string>();

        foreach (var module in _manifests.Keys.OrderBy(m => m, StringComparer.OrdinalIgnoreCase))
        {
            if (!visited.Contains(module))
            {
                DetectCycles(module, visited, stack, path, cycles, cycleKeys);
            }
        }

        return cycles;
    }

    private void DetectCycles(
        string module,
        HashSet<string> visited,
        HashSet<string> stack,
        List<string> path,
        List<IReadOnlyList<string>> cycles,
        HashSet<string> cycleKeys)
    {
        visited.Add(module);
        stack.Add(module);
        path.Add(module);

        foreach (var dependency in _edges[module])
        {
            if (!_manifests.ContainsKey(dependency))
            {
                continue;
            }

            if (!visited.Contains(dependency))
            {
                DetectCycles(dependency, visited, stack, path, cycles, cycleKeys);
            }
            else if (stack.Contains(dependency))
            {
                var cycle = ExtractCycle(path, dependency);
                var key = string.Join("->", cycle.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
                if (cycle.Count > 0 && cycleKeys.Add(key))
                {
                    cycles.Add(cycle);
                }
            }
        }

        stack.Remove(module);
        path.RemoveAt(path.Count - 1);
    }

    private static List<string> ExtractCycle(List<string> path, string start)
    {
        var index = path.FindIndex(p => string.Equals(p, start, StringComparison.OrdinalIgnoreCase));
        if (index >= 0)
        {
            return path.Skip(index).ToList();
        }

        return new List<string>(path);
    }
}

internal sealed record ModuleDependencyNode(
    string Name,
    string SourcePath,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> MissingDependencies);
