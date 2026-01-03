using System;
using System.Collections.Generic;
using System.Linq;
using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Ecs;
using FactoryPlatformer;
using FactoryPlatformer.Components;
using FactoryPlatformer.Rendering;

var options = RunnerOptions.Parse(args);

if (options.Headless)
{
    RunHeadless(options);
}
else
{
    using var game = new FactoryPlatformerGame(options.Scene);
    game.Run();
}

return;

static void RunHeadless(RunnerOptions options)
{
    var config = FactoryPlatformerConfig.Load("data/config/game.config.json");
    var sceneEntry = config.ResolveScene(options.Scene);
    var diagnostics = new RunnerDiagnostics();
    using var backend = new DiagnosticsRenderBackend(new NullRenderBackend(), diagnostics);
    var runtime = FactoryPlatformerBootstrapper.Build(backend, sceneEntry.Scene, diagnostics, config.PlayerTuning);
    Console.WriteLine($"[Headless] Running scene '{sceneEntry.Name}' with {sceneEntry.Scene.Prefabs?.Count ?? 0} prefab(s).");
    runtime.Audio.SoundPlayed += playback =>
        Console.WriteLine($"[Audio] Started {playback.SoundKey} (asset {playback.Asset}) volume={playback.Parameters.Volume:F2}");
    runtime.Audio.SoundStopped += playback =>
        Console.WriteLine($"[Audio] Stopped {playback.SoundKey} after {(DateTime.UtcNow - playback.Timestamp).TotalMilliseconds:F0}ms");

    var world = runtime.World;
    if (runtime.SceneInstances.Count == 0)
    {
        Console.WriteLine("[Scene] No prefabs were instantiated; headless run has nothing to simulate.");
        return;
    }

    var preferredPrefabs = new[] { "player", "headless-player" };
    SceneInstance? playerInstance = null;
    foreach (var prefabId in preferredPrefabs)
    {
        playerInstance = runtime.SceneInstances
            .FirstOrDefault(instance => string.Equals(instance.PrefabId, prefabId, StringComparison.OrdinalIgnoreCase));
        if (playerInstance is not null)
        {
            break;
        }
    }

    playerInstance ??= runtime.SceneInstances[0];
    var trackedInstance = playerInstance;
    if (trackedInstance.Instance.Entities.Count == 0)
    {
        Console.WriteLine($"[Scene] Prefab '{trackedInstance.PrefabId}' spawned no entities; nothing to simulate.");
        return;
    }

    var playerEntity = trackedInstance.Instance.Entities[0];

    if (!string.IsNullOrWhiteSpace(options.ScriptPath))
    {
        RunHeadlessScript(runtime, playerEntity, options.ScriptPath!);
    }
    else
    {
        RunDefaultHeadlessLoop(runtime, playerEntity);
    }

    var errors = diagnostics.GetRenderErrors();
    if (errors.Count > 0)
    {
        Console.WriteLine("[Headless] Render errors detected:");
        foreach (var error in errors)
        {
            Console.WriteLine($"  {error.Timestamp:HH:mm:ss}: {error.Message}");
        }
    }

    Console.WriteLine($"Final score: {runtime.GameState.Score} (State: {runtime.GameState.LoopState}, Last event: {runtime.GameState.LastEvent})");
    Console.WriteLine($"High score: {runtime.GameState.HighScore}");
    var history = runtime.GameState.GetEventHistorySnapshot();
    if (history.Count == 0)
    {
        Console.WriteLine("Event history: <none>");
    }
    else
    {
        Console.WriteLine("Event history:");
        foreach (var entry in history)
        {
            Console.WriteLine($"  {entry.Timestamp:HH:mm:ss}: {entry.Message}");
        }
    }

    ValidateHeadlessExpectations(options, runtime, history);
}

static void ValidateHeadlessExpectations(RunnerOptions options, FactoryPlatformerRuntime runtime, IReadOnlyList<GameEvent> history)
{
    var success = true;
    if (options.MinScore.HasValue && runtime.GameState.Score < options.MinScore.Value)
    {
        Console.WriteLine($"[Headless] Expected score >= {options.MinScore.Value}, but final score was {runtime.GameState.Score}.");
        success = false;
    }

    if (!string.IsNullOrWhiteSpace(options.ExpectEvent))
    {
        var expected = options.ExpectEvent!;
        var found = history.Any(evt => evt.Message.IndexOf(expected, StringComparison.OrdinalIgnoreCase) >= 0);
        if (!found)
        {
            Console.WriteLine($"[Headless] Expected an event containing \"{expected}\", but it was not observed.");
            success = false;
        }
    }

    if (!success)
    {
        Environment.ExitCode = 1;
    }
}

static void RunDefaultHeadlessLoop(FactoryPlatformerRuntime runtime, Entity playerEntity)
{
    for (var i = 0; i < 5; i++)
    {
        var pressed = i % 2 == 0;
        runtime.World.Input.SetActionState("move_right", new ActionState(pressed ? 1f : 0f, pressed));
        runtime.World.Input.SetActionState("move_left", new ActionState(pressed ? 0f : 1f, !pressed));
        var jump = i == 1;
        runtime.World.Input.SetActionState("jump", new ActionState(jump ? 1f : 0f, jump));
        SimulateFrame(runtime, playerEntity, 0.016f, i, logFrame: true);
    }
}

static void RunHeadlessScript(FactoryPlatformerRuntime runtime, Entity playerEntity, string scriptPath)
{
    var script = HeadlessScript.Load(scriptPath);
    Console.WriteLine($"[Headless] Loaded script '{scriptPath}' with {script.Steps.Count} step(s).");
    var actionStates = new Dictionary<string, ActionState>(StringComparer.OrdinalIgnoreCase);
    var tickIndex = 0;
    foreach (var step in script.Steps)
    {
        var duration = Math.Max(0.0f, step.Duration);
        if (step.Actions is not null && step.Actions.Count > 0)
        {
            foreach (var (action, value) in step.Actions)
            {
                var pressed = value > 0.5f;
                actionStates[action] = new ActionState(value, pressed);
            }
            Console.WriteLine($"[Headless] Step actions: {string.Join(", ", step.Actions.Select(pair => $"{pair.Key}={(pair.Value > 0.5f ? "on" : "off")}"))} for {duration:F2}s");
        }
        else
        {
            Console.WriteLine($"[Headless] Step with duration {duration:F2}s (no action changes).");
        }

        var remaining = duration;
        while (remaining > 0f)
        {
            var dt = Math.Min(0.016f, remaining);
            foreach (var (action, state) in actionStates)
            {
                runtime.World.Input.SetActionState(action, state);
            }

            SimulateFrame(runtime, playerEntity, dt, tickIndex, logFrame: tickIndex % 15 == 0);
            remaining -= dt;
            tickIndex++;
        }
    }
}

static void SimulateFrame(FactoryPlatformerRuntime runtime, Entity playerEntity, float deltaTime, int tickIndex, bool logFrame)
{
    runtime.RenderService.BeginFrame();
    runtime.World.Tick(deltaTime);
    runtime.LoopReset.Update(deltaTime);
    if (logFrame && runtime.World.IsAlive(playerEntity))
    {
        var transform = runtime.World.GetComponent<Transform2D>(playerEntity);
        Console.WriteLine($"Tick {tickIndex}: position=({transform.X:F2}, {transform.Y:F2})");
        var sprites = runtime.RenderService.GetFrameBuffer().Sprites;
        if (sprites.Count > 0)
        {
            Console.WriteLine($"  Draw sprite {sprites[0].Texture} at ({sprites[0].X:F2}, {sprites[0].Y:F2}) layer {sprites[0].Layer}");
        }
    }
    runtime.RenderService.Submit(runtime.RenderService.GetFrameBuffer());
}

readonly record struct RunnerOptions(bool Headless, string? Scene, int? MinScore, string? ExpectEvent, string? ScriptPath)
{
    public static RunnerOptions Parse(string[] args)
    {
        var headless = false;
        string? scene = null;
        int? minScore = null;
        string? expectEvent = null;
        string? script = null;
        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg)
            {
                case "--headless":
                    headless = true;
                    break;
                case "--scene":
                    if (i + 1 < args.Length)
                    {
                        scene = args[++i];
                    }
                    break;
                case "--min-score":
                    if (i + 1 >= args.Length || !int.TryParse(args[++i], out var parsedScore))
                    {
                        throw new ArgumentException("Missing or invalid value for --min-score");
                    }
                    minScore = parsedScore;
                    break;
                case "--expect-event":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --expect-event");
                    }
                    expectEvent = args[++i];
                    break;
                case "--script":
                    if (i + 1 >= args.Length)
                    {
                        throw new ArgumentException("Missing value for --script");
                    }
                    script = args[++i];
                    break;
            }
        }

        return new RunnerOptions(headless, scene, minScore, expectEvent, script);
    }
}
