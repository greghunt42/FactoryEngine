using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Ecs;
using FactoryEngine.Core.Ecs.Components;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Serialization;

public sealed class SerializationService : ISerializationService
{
    private readonly Dictionary<string, IComponentAdapter> _descriptors = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, PrefabDefinition> _prefabs = new(StringComparer.OrdinalIgnoreCase);
    private Func<AssetId, bool>? _assetResolver;
    private AssetMetadataRules? _metadataRules;

    public void RegisterDescriptor<T>(IComponentDescriptor<T> descriptor) where T : struct
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _descriptors[descriptor.Name] = new ComponentAdapter<T>(descriptor, this);
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
        var prefab = PrefabJsonSerializer.Read(stream);
        RegisterPrefab(prefab);
        return prefab;
    }

    public IReadOnlyList<PrefabValidationIssue> ValidatePrefab(PrefabDefinition prefab)
    {
        ArgumentNullException.ThrowIfNull(prefab);
        var issues = new List<PrefabValidationIssue>();
        foreach (var entity in prefab.Entities)
        {
            foreach (var component in entity.Components)
            {
                if (!_descriptors.TryGetValue(component.ComponentName, out var adapter))
                {
                    issues.Add(new PrefabValidationIssue(prefab.Id, entity.Name, component.ComponentName, $"Component descriptor '{component.ComponentName}' not registered."));
                    continue;
                }

                try
                {
                    var validationErrors = adapter.Validate(component);
                    foreach (var error in validationErrors)
                    {
                        issues.Add(new PrefabValidationIssue(prefab.Id, entity.Name, component.ComponentName, error));
                    }
                }
                catch (Exception ex)
                {
                    issues.Add(new PrefabValidationIssue(prefab.Id, entity.Name, component.ComponentName, $"Exception while validating component: {ex.Message}"));
                }
            }
        }

        return issues;
    }

    public void SetAssetResolver(Func<AssetId, bool>? resolver)
    {
        _assetResolver = resolver;
    }

    public void SetMetadataRules(AssetMetadataRules? rules)
    {
        _metadataRules = rules;
    }

    private interface IComponentAdapter
    {
        void Apply(World world, Entity entity, PrefabComponent component);
        IReadOnlyList<string> Validate(PrefabComponent component);
    }

    private sealed class ComponentAdapter<T> : IComponentAdapter where T : struct
    {
        private readonly IComponentDescriptor<T> _descriptor;
        private readonly SerializationService _owner;

        public ComponentAdapter(IComponentDescriptor<T> descriptor, SerializationService owner)
        {
            _descriptor = descriptor;
            _owner = owner;
        }

        public void Apply(World world, Entity entity, PrefabComponent component)
        {
            var reader = new DictionaryComponentReader(component.Data);
            var value = _descriptor.Deserialize(reader);
            var context = new ValidationContext();
            context.SetAssetResolver(_owner._assetResolver);
            context.SetMetadataRules(_owner._metadataRules);
            _descriptor.Validate(value, context);
            if (context.HasErrors)
            {
                throw new InvalidOperationException($"Component '{_descriptor.Name}' failed validation: {string.Join(", ", context.Errors)}");
            }

            ref var storage = ref world.AddComponent<T>(entity);
            storage = value;
        }

        public IReadOnlyList<string> Validate(PrefabComponent component)
        {
            var context = new ValidationContext();
            context.SetAssetResolver(_owner._assetResolver);
            context.SetMetadataRules(_owner._metadataRules);
            if (_descriptor is IRawComponentDescriptor rawDescriptor)
            {
                rawDescriptor.ValidateRaw(component, context);
                return context.Errors;
            }

            var reader = new DictionaryComponentReader(component.Data);
            var value = _descriptor.Deserialize(reader);
            _descriptor.Validate(value, context);
            return context.Errors;
        }
    }
}
