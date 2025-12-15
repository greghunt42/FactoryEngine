using System.Collections.Generic;
using System.IO;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Services.Serialization;
using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;
using FactoryPlatformer.Systems;

var world = new WorldBuilder()
    .WithName("FactoryPlatformer")
    .WithRendering(new LoggingRenderService())
    .Build();

world.Serialization.RegisterDescriptor(new Transform2DDescriptor());
world.Serialization.RegisterDescriptor(new Velocity2DDescriptor());
world.Serialization.RegisterDescriptor(new SpriteDescriptor());

world.Serialization.LoadPrefabFromJson(Path.Combine("data", "prefabs", "player.json"));

var bank = new SoundBank("core");
bank.Sounds["step"] = new SoundDefinition
{
    Asset = new FactoryEngine.Core.Services.Asset.AssetId("core", "step"),
    Group = "sfx",
    Volume = 0.8f
};
world.Audio.RegisterSoundBank(bank);

world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
world.RegisterSystem(new MovementSystem(), SystemPhase.Simulation);
world.RegisterSystem(new RenderingSystem(), SystemPhase.RenderPrep);
world.RegisterSystem(new AudioSystem("core", "step"), SystemPhase.Simulation);

var instance = world.InstantiatePrefab("player");
var playerEntity = instance.Entities[0];

for (var i = 0; i < 5; i++)
{
    world.Rendering.BeginFrame();
    world.Tick(0.016f);
    var transform = world.GetComponent<Transform2D>(playerEntity);
    Console.WriteLine($"Tick {i}: position=({transform.X:F2}, {transform.Y:F2})");
    var sprites = world.Rendering.GetFrameBuffer().Sprites;
    if (sprites.Count > 0)
    {
        Console.WriteLine($"  Draw sprite {sprites[0].Texture} at ({sprites[0].X:F2}, {sprites[0].Y:F2}) layer {sprites[0].Layer}");
    }
    world.Rendering.Submit(world.Rendering.GetFrameBuffer());
}
world.Input.LoadActionMapFromJson(Path.Combine("data", "input", "default-actions.json"));
