using System;
using System.Collections.Generic;
using System.IO;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Audio;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Services.Serialization;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;
using FactoryPlatformer.Systems;

namespace FactoryPlatformer;

public static class FactoryPlatformerBootstrapper
{
    public static FactoryPlatformerRuntime Build(IRenderBackend backend, SceneConfig? scene = null, RunnerDiagnostics? diagnostics = null, PlayerTuningConfig? playerTuning = null)
    {
        var serialization = new SerializationService();
        serialization.RegisterDescriptor(new Transform2DDescriptor());
        serialization.RegisterDescriptor(new Velocity2DDescriptor());
        serialization.RegisterDescriptor(new SpriteDescriptor());
        serialization.RegisterDescriptor(new PhysicsBodyDescriptor());
        serialization.RegisterDescriptor(new Collider2DDescriptor());
        serialization.RegisterDescriptor(new Camera2DDescriptor());
        serialization.RegisterDescriptor(new CameraTargetDescriptor());
        serialization.RegisterDescriptor(new PlayerTagDescriptor());
        serialization.RegisterDescriptor(new SpawnPointDescriptor());
        serialization.RegisterDescriptor(new CollectibleDescriptor());
        serialization.RegisterDescriptor(new HazardDescriptor());
        serialization.RegisterDescriptor(new LevelGoalDescriptor());
        serialization.RegisterDescriptor(new AirDodgeDescriptor());
        serialization.RegisterDescriptor(new HazardPatrolDescriptor());

        var assets = AssetPipeline.CreateDefaultService();
        var catalogs = LoadCatalogs(assets);
        var assetResolver = AssetCatalogResolver.BuildResolver(catalogs);
        serialization.SetAssetResolver(assetResolver);

        var audio = new AudioService();
        audio.SetAssetResolver(assetResolver);
        audio.SetAssetService(assets);

        foreach (var catalog in catalogs)
        {
            foreach (var (assetName, record) in catalog.Assets)
            {
                if (!string.Equals(record.Type, AssetTypes.SoundBank, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var bankHandle = assets.Load<SoundBank>(new AssetId(catalog.Namespace, assetName));
                if (bankHandle.Value is not null)
                {
                    audio.RegisterSoundBank(bankHandle.Value);
                }
            }
        }

        var renderService = new BasicRenderService(assets, backend);
        RegisterPrefabs(serialization, assets, catalogs);
        var gameState = new FactoryPlatformerGameState();
        var highScorePath = Path.Combine("data", "config", "highscore.json");
        var storedHighScore = HighScoreStorage.Load(highScorePath);
        gameState.InitializeHighScore(storedHighScore);
        gameState.HighScoreChanged += (_, score) => HighScoreStorage.Save(highScorePath, score);
        gameState.RestartLoop("Ready!", resetScore: false);

        var world = new WorldBuilder()
            .WithName("FactoryPlatformer")
            .WithSerialization(serialization)
            .WithAssets(assets)
            .WithRendering(renderService)
            .WithAudio(audio)
            .Build();

        var pickupSound = new SoundEffectRef("core", "pickup");
        var hazardSound = new SoundEffectRef("core", "hazard");
        var wallSlideSound = new SoundEffectRef("core", "wall-slide");
        var airDodgeSound = new SoundEffectRef("core", "air-dodge");

        world.RegisterSystem(new InputMovementSystem(), SystemPhase.Input);
        world.RegisterSystem(new PhysicsSystem(), SystemPhase.Simulation);
        world.RegisterSystem(new HazardPatrolSystem(), SystemPhase.Simulation);
        world.RegisterSystem(new CameraFollowSystem(), SystemPhase.Simulation);
        world.RegisterSystem(new CollectibleSystem(gameState, pickupSound), SystemPhase.Animation);
        world.RegisterSystem(new HazardSystem(gameState, hazardSound), SystemPhase.Animation);
        world.RegisterSystem(new PlayerMovementFeedbackSystem(gameState, wallSlideSound, airDodgeSound), SystemPhase.Animation);
        world.RegisterSystem(new LevelGoalSystem(gameState), SystemPhase.Animation);
        world.RegisterSystem(new RenderingSystem(), SystemPhase.RenderPrep);
        world.RegisterSystem(new AudioSystem("core", "step"), SystemPhase.Simulation);
        world.Input.LoadActionMapFromJson(Path.Combine("data", "input", "default-actions.json"));

        scene ??= SceneConfig.Default;
        scene.Normalize();

        var sceneInstances = LoadScene(world, scene);
        ApplyPlayerTuning(world, playerTuning);
        return new FactoryPlatformerRuntime(world, serialization, assets, renderService, audio, sceneInstances, diagnostics, gameState, scene, playerTuning);
    }

    private static IReadOnlyList<AssetCatalog> LoadCatalogs(IAssetService assets)
    {
        var catalogDirectory = Path.Combine("data", "catalogs");
        var catalogPaths = AssetCatalogDiscovery.EnumerateCatalogFiles(catalogDirectory);
        if (catalogPaths.Count == 0)
        {
            throw new InvalidOperationException($"No asset catalogs found under '{catalogDirectory}'.");
        }

        var loaded = new List<AssetCatalog>();
        foreach (var manifestPath in catalogPaths)
        {
            var catalog = AssetCatalogManifest.LoadFromJson(manifestPath);
            assets.RegisterCatalog(catalog);
            loaded.Add(catalog);
        }
        return loaded;
    }

    private static void RegisterPrefabs(SerializationService serialization, IAssetService assets, IEnumerable<AssetCatalog> catalogs)
    {
        foreach (var catalog in catalogs)
        {
            foreach (var (assetName, record) in catalog.Assets)
            {
                if (!string.Equals(record.Type, AssetTypes.Prefab, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var assetId = new AssetId(catalog.Namespace, assetName);
                try
                {
                    var handle = assets.Load<PrefabDefinition>(assetId);
                    if (handle.Value is not null)
                    {
                        serialization.RegisterPrefab(handle.Value);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Prefabs] Failed to load '{assetId}': {ex.Message}");
                }
            }
        }
    }
    internal static void ApplyPlayerTuning(World world, PlayerTuningConfig? tuning)
    {
        if (tuning is null)
        {
            return;
        }

        foreach (var entity in world.Query(builder => builder
                     .All<PlayerTag>()
                     .All<PhysicsBody>()))
        {
            ref var body = ref world.GetComponent<PhysicsBody>(entity);
            tuning.ApplyTo(ref body);
            if (world.HasComponent<AirDodge>(entity))
            {
                ref var dodge = ref world.GetComponent<AirDodge>(entity);
                tuning.ApplyTo(ref dodge);
            }
        }
    }

    public static List<SceneInstance> LoadScene(World world, SceneConfig? scene)
    {
        ArgumentNullException.ThrowIfNull(world);
        scene?.Normalize();
        var prefabs = scene?.Prefabs;
        if (prefabs is null || prefabs.Count == 0)
        {
            prefabs = new List<string> { "level", "player" };
        }

        var instances = new List<SceneInstance>();
        foreach (var prefabId in prefabs)
        {
            if (string.IsNullOrWhiteSpace(prefabId))
            {
                continue;
            }

            try
            {
                var instance = world.InstantiatePrefab(prefabId);
                instances.Add(new SceneInstance(prefabId, instance));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Scene] Failed to instantiate prefab '{prefabId}': {ex.Message}");
            }
        }

        return instances;
    }
}

public sealed record SceneInstance(string PrefabId, PrefabInstance Instance);

public sealed class FactoryPlatformerRuntime
{
    private readonly LoopResetController _loopReset;

    public FactoryPlatformerRuntime(
        World world,
        SerializationService serialization,
        IAssetService assets,
        BasicRenderService renderService,
        IAudioService audio,
        List<SceneInstance> sceneInstances,
        RunnerDiagnostics? diagnostics,
        FactoryPlatformerGameState gameState,
        SceneConfig scene,
        PlayerTuningConfig? appliedTuning)
    {
        World = world;
        Serialization = serialization;
        Assets = assets;
        RenderService = renderService;
        Audio = audio;
        SceneInstances = sceneInstances;
        Diagnostics = diagnostics;
        GameState = gameState;
        Scene = scene;
        PlayerTuning = appliedTuning;
        _loopReset = new LoopResetController(gameState, () => ResetScene());
    }

    public World World { get; }
    public SerializationService Serialization { get; }
    public IAssetService Assets { get; }
    public BasicRenderService RenderService { get; }
    public IAudioService Audio { get; }
    public List<SceneInstance> SceneInstances { get; }
    public RunnerDiagnostics? Diagnostics { get; }
    public FactoryPlatformerGameState GameState { get; }
    public SceneConfig Scene { get; }
    public PlayerTuningConfig? PlayerTuning { get; }
    public LoopResetController LoopReset => _loopReset;
    public float? PendingResetSeconds => _loopReset.PendingResetSeconds;

    public void ResetScene(SceneConfig? overrideScene = null)
    {
        var nextScene = overrideScene ?? Scene;
        nextScene.Normalize();
        DestroySceneEntities();
        var instances = FactoryPlatformerBootstrapper.LoadScene(World, nextScene);
        SceneInstances.Clear();
        SceneInstances.AddRange(instances);
        if (PlayerTuning is not null)
        {
            FactoryPlatformerBootstrapper.ApplyPlayerTuning(World, PlayerTuning);
        }
    }

    private void DestroySceneEntities()
    {
        foreach (var instance in SceneInstances)
        {
            foreach (var entity in instance.Instance.Entities)
            {
                if (World.IsAlive(entity))
                {
                    World.DestroyEntity(entity);
                }
            }
        }

        World.FlushDestroyedEntities();
    }
}
