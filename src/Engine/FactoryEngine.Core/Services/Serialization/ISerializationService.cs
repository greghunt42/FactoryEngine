using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Ecs;

namespace FactoryEngine.Core.Services.Serialization;

public interface ISerializationService
{
    void RegisterDescriptor<T>(IComponentDescriptor<T> descriptor) where T : struct;
    void RegisterPrefab(PrefabDefinition prefab);
    PrefabInstance InstantiatePrefab(string prefabId, World world);
    PrefabDefinition LoadPrefabFromJson(string path);
}

public sealed record PrefabInstance(World World, IReadOnlyList<Entity> Entities);
