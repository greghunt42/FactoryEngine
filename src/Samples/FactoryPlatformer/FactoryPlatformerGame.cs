using System;
using System.Collections.Generic;
using System.Linq;
using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Services.Rendering;
using FactoryPlatformer.Components;
using FactoryPlatformer.Rendering;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FactoryPlatformer;

public sealed class FactoryPlatformerGame : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private readonly string? _initialScene;
    private FactoryPlatformerRuntime? _runtime;
    private DiagnosticsRenderBackend? _backend;
    private DebugOverlay? _overlay;
    private FactoryPlatformerConfig _config = FactoryPlatformerConfig.Default;
    private List<SceneEntry> _sceneEntries = new();
    private int _sceneIndex;
    private KeyboardState _previousKeyboardState;
    private double _fpsAccumulator;
    private int _fpsFrames;
    private float _fps;
    private readonly RunnerDiagnostics _diagnostics = new();
    private bool _sceneMenuOpen;
    private int _sceneMenuSelection;
    private bool _isLoading;
    private string? _loadingMessage;
    private float _loadingTick;
    private static readonly char[] Spinner = { '|', '/', '-', '\\' };

    public FactoryPlatformerGame(string? initialScene = null)
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = ".";
        IsMouseVisible = true;
        _initialScene = initialScene;
    }

    protected override void LoadContent()
    {
        var spriteBackend = new MonoGameSpriteBatchBackend(GraphicsDevice);
        _backend = new DiagnosticsRenderBackend(spriteBackend, _diagnostics);
        _overlay = new DebugOverlay(GraphicsDevice);
        ReloadConfiguration(preferredScene: _initialScene);
        Console.WriteLine("[Runner] Press PageUp/PageDown to change scenes or F5 to reload game.config.json.");
    }

    protected override void Update(GameTime gameTime)
    {
        if (_runtime is null)
        {
            return;
        }

        var elapsed = gameTime.ElapsedGameTime.TotalSeconds;
        _fpsAccumulator += elapsed;
        _fpsFrames++;
        _loadingTick += (float)elapsed;
        if (_fpsAccumulator >= 1.0)
        {
            _fps = (float)(_fpsFrames / _fpsAccumulator);
            _fpsFrames = 0;
            _fpsAccumulator = 0;
        }

        HandleInput();

        _runtime.RenderService.BeginFrame();
        _runtime.World.Tick((float)elapsed);
        _runtime.LoopReset.Update((float)elapsed);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        if (_runtime is not null)
        {
            _runtime.RenderService.Submit(_runtime.RenderService.GetFrameBuffer());
            if (_overlay is not null)
            {
                var scene = GetCurrentSceneEntry();
                var prefabList = scene.Scene.Prefabs is { Count: > 0 }
                    ? string.Join(", ", scene.Scene.Prefabs)
                    : "<none>";
                var errors = FormatErrors(_diagnostics.GetRenderErrors());
                var gameState = _runtime.GameState;
                var lastEventAge = (float)Math.Max(0, (DateTime.UtcNow - gameState.LastEventTime).TotalSeconds);
                var history = gameState.GetEventHistorySnapshot();
                var sceneNames = _sceneEntries.Select(entry => entry.Name).ToList();
                var spinner = Spinner[(int)(_loadingTick * 8f) % Spinner.Length];
                var pendingResetSeconds = _runtime.PendingResetSeconds;
                var tuningLines = GetMovementTuningLines();
                var data = new DebugOverlayData(
                    scene.Name,
                    prefabList,
                    _fps,
                    errors,
                    gameState.Score,
                    gameState.HighScore,
                    gameState.LastEvent,
                    lastEventAge,
                    history,
                    tuningLines,
                    gameState.LoopState,
                    pendingResetSeconds,
                    _config.SceneSelection.Hint ?? "Press Enter to open scene list",
                    _sceneMenuOpen,
                    sceneNames,
                    _sceneMenuOpen ? _sceneMenuSelection : _sceneIndex,
                    _isLoading ? _loadingMessage ?? "Loading..." : _loadingMessage,
                    spinner);
                _overlay.Draw(data);
            }
        }
        base.Draw(gameTime);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        _backend?.Dispose();
        _overlay?.Dispose();
    }

    private IReadOnlyList<string> GetMovementTuningLines()
    {
        if (_runtime is null)
        {
            return Array.Empty<string>();
        }

        foreach (var entity in _runtime.World.Query(builder => builder
                     .All<PlayerTag>()
                     .All<PhysicsBody>()))
        {
            ref var body = ref _runtime.World.GetComponent<PhysicsBody>(entity);
            return new[]
            {
                $"Run {body.RunSpeed:F0} accel {body.GroundAcceleration:F0}/{body.AirAcceleration:F0}",
                $"Air ctrl {body.AirControlMultiplier:F2} exp {body.AirControlExponent:F2}",
                $"Wall slide {body.WallSlideSpeed:F0}@{body.WallSlideStickTime:F2}s",
                $"Wall jump {body.WallJumpHorizontalSpeed:F0}@{body.WallJumpCooldown:F2}s"
            };
        }

        return Array.Empty<string>();
    }

    private void HandleInput()
    {
        var keyboard = Keyboard.GetState();
        if (IsKeyPressed(keyboard, Keys.Escape))
        {
            if (_sceneMenuOpen)
            {
                _sceneMenuOpen = false;
            }
            else
            {
                Exit();
            }
            _previousKeyboardState = keyboard;
            return;
        }

        if (IsKeyPressed(keyboard, Keys.F5))
        {
            var currentSceneName = GetCurrentSceneName();
            ReloadConfiguration(preferredScene: currentSceneName);
            _previousKeyboardState = keyboard;
            return;
        }

        if (_sceneMenuOpen)
        {
            if (IsKeyPressed(keyboard, Keys.Up))
            {
                _sceneMenuSelection = (_sceneMenuSelection - 1 + _sceneEntries.Count) % _sceneEntries.Count;
            }
            else if (IsKeyPressed(keyboard, Keys.Down))
            {
                _sceneMenuSelection = (_sceneMenuSelection + 1) % _sceneEntries.Count;
            }
            else if (IsKeyPressed(keyboard, Keys.Enter))
            {
                if (_sceneMenuSelection != _sceneIndex)
                {
                    _sceneIndex = _sceneMenuSelection;
                    RebuildRuntime();
                }
                _sceneMenuOpen = false;
            }

            _previousKeyboardState = keyboard;
            return;
        }

        if (_config.SceneSelection.Enabled && IsKeyPressed(keyboard, Keys.Enter))
        {
            _sceneMenuOpen = true;
            _sceneMenuSelection = _sceneIndex;
            _previousKeyboardState = keyboard;
            return;
        }

        if (IsKeyPressed(keyboard, Keys.PageUp))
        {
            CycleScene(-1);
        }
        else if (IsKeyPressed(keyboard, Keys.PageDown))
        {
            CycleScene(1);
        }

        var moveRight = keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right);
        var moveLeft = keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left);
        var jump = keyboard.IsKeyDown(Keys.Space) || keyboard.IsKeyDown(Keys.W) || keyboard.IsKeyDown(Keys.Up);
        _runtime!.World.Input.SetActionState("move_right", new ActionState(moveRight ? 1f : 0f, moveRight));
        _runtime.World.Input.SetActionState("move_left", new ActionState(moveLeft ? 1f : 0f, moveLeft));
        _runtime.World.Input.SetActionState("jump", new ActionState(jump ? 1f : 0f, jump));

        _previousKeyboardState = keyboard;
    }

    private bool IsKeyPressed(KeyboardState current, Keys key) =>
        current.IsKeyDown(key) && !_previousKeyboardState.IsKeyDown(key);

    private void ApplyWindowConfig()
    {
        Window.Title = BuildWindowTitle(GetCurrentSceneName());
        _graphics.PreferredBackBufferWidth = _config.Window.Width;
        _graphics.PreferredBackBufferHeight = _config.Window.Height;
        Window.AllowUserResizing = true;
        _graphics.ApplyChanges();
    }

    private void ReloadConfiguration(string? preferredScene = null)
    {
        _config = FactoryPlatformerConfig.Load("data/config/game.config.json");
        _sceneEntries = _config.GetSceneEntries().ToList();
        if (_sceneEntries.Count == 0)
        {
            _sceneEntries.Add(new SceneEntry("default", SceneConfig.Default));
        }

        var resolved = _config.ResolveScene(preferredScene);
        _sceneIndex = Math.Max(0, _sceneEntries.FindIndex(entry =>
            string.Equals(entry.Name, resolved.Name, StringComparison.OrdinalIgnoreCase)));
        if (_sceneIndex < 0)
        {
            _sceneIndex = 0;
        }
        _sceneMenuSelection = _sceneIndex;

        ApplyWindowConfig();
        RebuildRuntime();
    }

    private void RebuildRuntime()
    {
        if (_backend is null)
        {
            return;
        }

        var scene = GetCurrentSceneEntry();
        _isLoading = true;
        _loadingMessage = $"Loading scene '{scene.Name}'";
        try
        {
            _runtime = FactoryPlatformerBootstrapper.Build(_backend, scene.Scene, _diagnostics, _config.PlayerTuning);
            Window.Title = BuildWindowTitle(scene.Name);
            Console.WriteLine($"[Runner] Loaded scene '{scene.Name}' ({scene.Scene.Prefabs?.Count ?? 0} prefab(s)).");
            _loadingMessage = null;
        }
        catch (Exception ex)
        {
            _loadingMessage = $"Failed to load '{scene.Name}': {ex.Message}";
            _diagnostics.ReportRenderError(_loadingMessage);
        }
        finally
        {
            _isLoading = false;
            _sceneMenuSelection = _sceneIndex;
        }
    }

    private string BuildWindowTitle(string sceneName)
    {
        var baseTitle = _config.Window.Title ?? "FactoryPlatformer";
        if (string.IsNullOrWhiteSpace(sceneName) || string.Equals(sceneName, "default", StringComparison.OrdinalIgnoreCase))
        {
            return baseTitle;
        }

        return $"{baseTitle} [{sceneName}]";
    }

    private string GetCurrentSceneName()
    {
        return GetCurrentSceneEntry().Name;
    }

    private void CycleScene(int delta)
    {
        if (_sceneEntries.Count <= 1)
        {
            return;
        }

        _sceneIndex = (_sceneIndex + delta) % _sceneEntries.Count;
        if (_sceneIndex < 0)
        {
            _sceneIndex = _sceneEntries.Count - 1;
        }
        RebuildRuntime();
        _sceneMenuSelection = _sceneIndex;
    }

    private SceneEntry GetCurrentSceneEntry()
    {
        if (_sceneEntries.Count == 0 || _sceneIndex < 0 || _sceneIndex >= _sceneEntries.Count)
        {
            return new SceneEntry("default", SceneConfig.Default);
        }

        return _sceneEntries[_sceneIndex];
    }

    private static IReadOnlyList<string> FormatErrors(IReadOnlyList<DiagnosticEntry> entries)
    {
        if (entries.Count == 0)
        {
            return Array.Empty<string>();
        }

        var now = DateTime.UtcNow;
        var list = new List<string>(entries.Count);
        foreach (var entry in entries.Take(4))
        {
            var age = Math.Max(0, (now - entry.Timestamp).TotalSeconds);
            var message = entry.Message.Length > 60 ? entry.Message[..57] + "..." : entry.Message;
            list.Add($"{age:F0}s: {message}");
        }
        return list;
    }

}
