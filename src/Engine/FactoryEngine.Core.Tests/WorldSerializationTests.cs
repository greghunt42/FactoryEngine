using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Serialization;

namespace FactoryEngine.Core.Tests;

public class WorldSerializationTests
{
    private struct Flag { public int Value; }

    private sealed class FlagDescriptor : IComponentDescriptor<Flag>
    {
        public string Name => "Flag";
        public int Version => 1;
        public void Serialize(ref Flag component, IComponentWriter writer) => writer.WriteInt("value", component.Value);
        public Flag Deserialize(IComponentReader reader) => new() { Value = reader.ReadInt("value") };
        public void Validate(Flag component, ValidationContext context)
        {
            if (component.Value < 0)
            {
                context.Error("Value negative");
            }
        }
    }

    [Fact]
    public void World_InstantiatePrefab_UsesSerializationService()
    {
        var serialization = new SerializationService();
        serialization.RegisterDescriptor(new FlagDescriptor());
        var prefab = new PrefabDefinition("flag");
        var entity = new PrefabEntity();
        entity.Components.Add(new PrefabComponent("Flag", new Dictionary<string, object?> { ["value"] = 42 }));
        prefab.Entities.Add(entity);
        serialization.RegisterPrefab(prefab);

        var world = new WorldBuilder()
            .WithSerialization(serialization)
            .Build();

        var instance = world.InstantiatePrefab("flag");
        Assert.Single(instance.Entities);
        var value = world.GetComponent<Flag>(instance.Entities[0]);
        Assert.Equal(42, value.Value);
    }
}
