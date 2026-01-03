using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace FactoryEngine.Core.Services.Serialization;

internal static class PrefabJsonSerializer
{
    public static PrefabDefinition Read(Stream stream)
    {
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var prefabId = root.GetProperty("id").GetString() ?? throw new InvalidOperationException("Prefab id missing");
        var prefab = new PrefabDefinition(prefabId);
        foreach (var entityElement in root.GetProperty("entities").EnumerateArray())
        {
            var name = entityElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
            var entity = new PrefabEntity { Name = name };
            if (entityElement.TryGetProperty("components", out var components))
            {
                foreach (var compElement in components.EnumerateArray())
                {
                    var compName = compElement.GetProperty("name").GetString() ?? throw new InvalidOperationException("Component name missing");
                    var data = new Dictionary<string, object?>();
                    if (compElement.TryGetProperty("data", out var dataElement))
                    {
                        foreach (var property in dataElement.EnumerateObject())
                        {
                            data[property.Name] = property.Value.ValueKind switch
                            {
                                JsonValueKind.Number => property.Value.TryGetInt64(out var l) ? l : property.Value.GetDouble(),
                                JsonValueKind.String => property.Value.GetString(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Array => property.Value.EnumerateArray().Select(ReadValue).ToList(),
                                JsonValueKind.Object => property.Value.EnumerateObject().ToDictionary(p => p.Name, p => ReadValue(p.Value)),
                                _ => null
                            };
                        }
                    }

                    entity.Components.Add(new PrefabComponent(compName, data));
                }
            }

            prefab.Entities.Add(entity);
        }

        return prefab;
    }

    private static object? ReadValue(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Array => element.EnumerateArray().Select(ReadValue).ToList(),
            JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ReadValue(p.Value)),
            JsonValueKind.Null => null,
            _ => null
        };
    }
}
