using System.Text.Json;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Ecs.Components;

namespace FactoryEngine.Core.Services.Serialization;

public sealed class SerializationService : ISerializationService
{
    private readonly Dictionary<string, IComponentAdapter> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PrefabDefinition> _prefabs = new(StringComparer.OrdinalIgnoreCase);

    public void RegisterDescriptor<T>(IComponentDescriptor<T> descriptor) where T : struct
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _descriptors[descriptor.Name] = new ComponentAdapter<T>(descriptor);
    }

    public void RegisterPrefab(PrefabDefinition prefab)
    {
        ArgumentNullException.ThrowIfNull(prefab);
        _prefabs[prefab.Id] = prefab;
    }

    public PrefabInstance InstantiatePrefab(string prefabId, World world)
    {
        if (!_prefabs.TryGetValue(prefabId, out var definition))
        {
            throw new InvalidOperationException($"Prefab '{prefabId}' not registered.");
        }

        var entities = new List<Entity>();
        foreach (var entityDef in definition.Entities)
        {
            var entity = world.CreateEntity();
            foreach (var component in entityDef.Components)
            {
                if (!_descriptors.TryGetValue(component.ComponentName, out var adapter))
                {
                    throw new InvalidOperationException($"Component descriptor '{component.ComponentName}' not registered.");
                }

                adapter.Apply(world, entity, component);
            }

            entities.Add(entity);
        }

        return new PrefabInstance(world, entities);
    }

    public PrefabDefinition LoadPrefabFromJson(string path)
    {
        using var stream = File.OpenRead(path);
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
                                _ => null
                            };
                        }
                    }

                    entity.Components.Add(new PrefabComponent(compName, data));
                }
            }

            prefab.Entities.Add(entity);
        }

        RegisterPrefab(prefab);
        return prefab;
    }

    private interface IComponentAdapter
    {
        void Apply(World world, Entity entity, PrefabComponent component);
    }

    private sealed class ComponentAdapter<T> : IComponentAdapter where T : struct
    {
        private readonly IComponentDescriptor<T> _descriptor;

        public ComponentAdapter(IComponentDescriptor<T> descriptor)
        {
            _descriptor = descriptor;
        }

        public void Apply(World world, Entity entity, PrefabComponent component)
        {
            var reader = new DictionaryComponentReader(component.Data);
            var value = _descriptor.Deserialize(reader);
            var context = new ValidationContext();
            _descriptor.Validate(value, context);
            if (context.HasErrors)
            {
                throw new InvalidOperationException($"Component '{_descriptor.Name}' failed validation: {string.Join(", ", context.Errors)}");
            }

            ref var storage = ref world.AddComponent<T>(entity);
            storage = value;
        }
    }
}
