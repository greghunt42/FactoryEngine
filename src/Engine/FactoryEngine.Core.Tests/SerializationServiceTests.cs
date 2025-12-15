using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Serialization;

namespace FactoryEngine.Core.Tests;

public class SerializationServiceTests
{
    private struct Position
    {
        public float X;
        public float Y;
    }

    private sealed class PositionDescriptor : IComponentDescriptor<Position>
    {
        public string Name => "Position";
        public int Version => 1;

        public void Serialize(ref Position component, IComponentWriter writer)
        {
            writer.WriteFloat("x", component.X);
            writer.WriteFloat("y", component.Y);
        }

        public Position Deserialize(IComponentReader reader)
        {
            return new Position
            {
                X = reader.ReadFloat("x"),
                Y = reader.ReadFloat("y")
            };
        }

        public void Validate(Position component, ValidationContext context)
        {
            if (component.X < -1000 || component.X > 1000)
            {
                context.Error("Position.X out of range");
            }
        }
    }

    [Fact]
    public void InstantiatePrefab_AddsComponents()
    {
        var service = new SerializationService();
        service.RegisterDescriptor(new PositionDescriptor());

        var prefab = new PrefabDefinition("player");
        var entityDef = new PrefabEntity();
        entityDef.Components.Add(new PrefabComponent("Position", new Dictionary<string, object?>
        {
            ["x"] = 5,
            ["y"] = 10
        }));
        prefab.Entities.Add(entityDef);
        service.RegisterPrefab(prefab);

        var world = new WorldBuilder().Build();
        var instance = service.InstantiatePrefab("player", world);

        Assert.Single(instance.Entities);
        var entity = instance.Entities[0];
        var position = world.GetComponent<Position>(entity);
        Assert.Equal(5, position.X);
        Assert.Equal(10, position.Y);
    }
}
