using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FactoryPlatformer;

public sealed class HeadlessScript
{
    public List<HeadlessScriptStep> Steps { get; set; } = new();

    public static HeadlessScript Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        var fullPath = Path.GetFullPath(path);
        var json = File.ReadAllText(fullPath);
        var script = JsonSerializer.Deserialize<HeadlessScript>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });
        if (script is null || script.Steps.Count == 0)
        {
            throw new InvalidOperationException($"Headless script '{path}' is empty.");
        }
        return script;
    }
}

public sealed class HeadlessScriptStep
{
    public float Duration { get; set; } = 0.016f;
    public Dictionary<string, float> Actions { get; set; } = new();
}
