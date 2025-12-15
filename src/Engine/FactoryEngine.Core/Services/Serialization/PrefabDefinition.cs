namespace FactoryEngine.Core.Services.Serialization;

public sealed class PrefabDefinition
{
    public PrefabDefinition(string id)
    {
        Id = id;
    }

    public string Id { get; }
    public List<PrefabEntity> Entities { get; } = new();
}

public sealed class PrefabEntity
{
    public string? Name { get; init; }
    public List<PrefabComponent> Components { get; } = new();
}

public sealed class PrefabComponent
{
    public PrefabComponent(string componentName, Dictionary<string, object?> data)
    {
        ComponentName = componentName;
        Data = data;
    }

    public string ComponentName { get; }
    public Dictionary<string, object?> Data { get; }
}
