using System.IO;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Serialization;

namespace FactoryEngine.Core.Tests;

public class SerializationJsonTests
{
    private struct Dummy
    {
        public int Value;
    }

    private sealed class DummyDescriptor : IComponentDescriptor<Dummy>
    {
        public string Name => "Dummy";
        public int Version => 1;
        public void Serialize(ref Dummy component, IComponentWriter writer) => writer.WriteInt("value", component.Value);
        public Dummy Deserialize(IComponentReader reader) => new() { Value = reader.ReadInt("value") };
        public void Validate(Dummy component, ValidationContext context)
        {
        }
    }

    [Fact]
    public void LoadPrefabFromJson_RegistersPrefab()
    {
        var json = """
        {
          "id": "dummy",
          "entities": [
            {
              "components": [
                { "name": "Dummy", "data": { "value": 5 } }
              ]
            }
          ]
        }
        """;
        var path = Path.GetTempFileName();
        File.WriteAllText(path, json);

        var service = new SerializationService();
        service.RegisterDescriptor(new DummyDescriptor());
        service.LoadPrefabFromJson(path);

        var world = new WorldBuilder().WithSerialization(service).Build();
        var instance = world.InstantiatePrefab("dummy");

        Assert.Single(instance.Entities);
    }
}
