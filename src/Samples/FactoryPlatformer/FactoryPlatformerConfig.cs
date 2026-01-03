using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FactoryPlatformer.Components;

namespace FactoryPlatformer;

public sealed class FactoryPlatformerConfig
{
    public WindowConfig Window { get; set; } = WindowConfig.Default;
    public SceneConfig Scene { get; set; } = SceneConfig.Default;
    public Dictionary<string, SceneConfig>? Scenes { get; set; }
    public string? ActiveScene { get; set; }
    public SceneSelectionConfig SceneSelection { get; set; } = SceneSelectionConfig.Default;
    public PlayerTuningConfig PlayerTuning { get; set; } = PlayerTuningConfig.Default;

    public static FactoryPlatformerConfig Default => new();

    public static FactoryPlatformerConfig Load(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                return Default;
            }

            var json = File.ReadAllText(fullPath);
            var config = JsonSerializer.Deserialize<FactoryPlatformerConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? Default;
            config.Normalize();
            return config;
        }
        catch
        {
            return Default;
        }
    }

    public void Normalize()
    {
        Window ??= WindowConfig.Default;
        Scene ??= SceneConfig.Default;
        SceneSelection ??= SceneSelectionConfig.Default;
        PlayerTuning ??= PlayerTuningConfig.Default;
        Window.Normalize();
        Scene.Normalize();
        SceneSelection.Normalize();
        PlayerTuning.Normalize();
        if (Scenes is not null)
        {
            foreach (var key in Scenes.Keys.ToList())
            {
                var scene = Scenes[key] ?? SceneConfig.Default;
                scene.Normalize();
                Scenes[key] = scene;
            }
        }
    }

    public IReadOnlyList<SceneEntry> GetSceneEntries()
    {
        if (Scenes is null || Scenes.Count == 0)
        {
            return new List<SceneEntry> { new("default", Scene) };
        }

        return Scenes
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new SceneEntry(pair.Key, pair.Value))
            .ToList();
    }

    public SceneEntry ResolveScene(string? preferred = null)
    {
        var entries = GetSceneEntries();
        if (entries.Count == 0)
        {
            return new SceneEntry("default", Scene);
        }

        var match = FindScene(entries, preferred) ??
                    FindScene(entries, ActiveScene);
        return match ?? entries[0];
    }

    private static SceneEntry? FindScene(IReadOnlyList<SceneEntry> entries, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        foreach (var entry in entries)
        {
            if (string.Equals(entry.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }
}

public readonly record struct SceneEntry(string Name, SceneConfig Scene);

public sealed class WindowConfig
{
    public string? Title { get; set; } = "FactoryPlatformer Sample";
    public int Width { get; set; } = 1280;
    public int Height { get; set; } = 720;

    public static WindowConfig Default => new();

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            Title = "FactoryPlatformer Sample";
        }

        if (Width <= 0)
        {
            Width = 1280;
        }

        if (Height <= 0)
        {
            Height = 720;
        }
    }
}

public sealed class SceneConfig
{
    public List<string>? Prefabs { get; set; } = new() { "level", "player" };
    public static SceneConfig Default => new();

    public void Normalize()
    {
        if (Prefabs is null || Prefabs.Count == 0)
        {
            Prefabs = new List<string> { "level", "player" };
            return;
        }

        var normalized = Prefabs
            .Select(prefab => prefab?.Trim())
            .Where(prefab => !string.IsNullOrWhiteSpace(prefab))
            .Select(prefab => prefab!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        Prefabs = normalized.Count == 0 ? new List<string> { "level", "player" } : normalized;
    }
}

public sealed class SceneSelectionConfig
{
    public bool Enabled { get; set; } = true;
    public string? Hint { get; set; } = "Press Enter to open scene list";
    public static SceneSelectionConfig Default => new();

    public void Normalize()
    {
        if (string.IsNullOrWhiteSpace(Hint))
        {
            Hint = "Press Enter to open scene list";
        }
    }
}

public sealed class PlayerTuningConfig
{
    public float? RunSpeed { get; set; }
    public float? GroundAcceleration { get; set; }
    public float? AirAcceleration { get; set; }
    public float? AirControl { get; set; }
    public float? AirControlExp { get; set; }
    public float? JumpCut { get; set; }
    public float? WallSlideSpeed { get; set; }
    public float? WallSlideStick { get; set; }
    public float? WallJumpSpeed { get; set; }
    public float? WallJumpCooldown { get; set; }
    public bool? AirDodgeEnabled { get; set; }
    public float? AirDodgeSpeed { get; set; }
    public float? AirDodgeCooldown { get; set; }
    public float? AirDodgeTrail { get; set; }

    public static PlayerTuningConfig Default => new();

    public void Normalize()
    {
        RunSpeed = ClampNonNegative(RunSpeed);
        GroundAcceleration = ClampNonNegative(GroundAcceleration);
        AirAcceleration = ClampNonNegative(AirAcceleration);
        AirControl = ClampNonNegative(AirControl);
        AirControlExp = ClampNonNegative(AirControlExp);
        JumpCut = ClampZeroToOne(JumpCut);
        WallSlideSpeed = ClampNonNegative(WallSlideSpeed);
        WallSlideStick = ClampNonNegative(WallSlideStick);
        WallJumpSpeed = ClampNonNegative(WallJumpSpeed);
        WallJumpCooldown = ClampNonNegative(WallJumpCooldown);
        AirDodgeSpeed = ClampNonNegative(AirDodgeSpeed);
        AirDodgeCooldown = ClampNonNegative(AirDodgeCooldown);
        AirDodgeTrail = ClampNonNegative(AirDodgeTrail);
    }

    public void ApplyTo(ref PhysicsBody body)
    {
        if (RunSpeed.HasValue)
        {
            body.RunSpeed = RunSpeed.Value;
        }

        if (GroundAcceleration.HasValue)
        {
            body.GroundAcceleration = GroundAcceleration.Value;
        }

        if (AirAcceleration.HasValue)
        {
            body.AirAcceleration = AirAcceleration.Value;
        }

        if (AirControl.HasValue)
        {
            body.AirControlMultiplier = AirControl.Value;
        }

        if (AirControlExp.HasValue)
        {
            body.AirControlExponent = AirControlExp.Value;
        }

        if (JumpCut.HasValue)
        {
            body.JumpCutMultiplier = Math.Clamp(JumpCut.Value, 0f, 1f);
        }

        if (WallSlideSpeed.HasValue)
        {
            body.WallSlideSpeed = WallSlideSpeed.Value;
        }

        if (WallSlideStick.HasValue)
        {
            body.WallSlideStickTime = WallSlideStick.Value;
        }

        if (WallJumpSpeed.HasValue)
        {
            body.WallJumpHorizontalSpeed = WallJumpSpeed.Value;
        }

        if (WallJumpCooldown.HasValue)
        {
            body.WallJumpCooldown = WallJumpCooldown.Value;
        }
    }

    public void ApplyTo(ref AirDodge dodge)
    {
        if (AirDodgeEnabled.HasValue)
        {
            dodge.Enabled = AirDodgeEnabled.Value;
        }

        if (AirDodgeSpeed.HasValue)
        {
            dodge.Speed = AirDodgeSpeed.Value;
        }

        if (AirDodgeCooldown.HasValue)
        {
            dodge.Cooldown = AirDodgeCooldown.Value;
        }

        if (AirDodgeTrail.HasValue)
        {
            dodge.EffectDuration = AirDodgeTrail.Value;
        }
    }

    private static float? ClampNonNegative(float? value)
    {
        if (value.HasValue && value.Value < 0f)
        {
            return 0f;
        }

        return value;
    }

    private static float? ClampZeroToOne(float? value)
    {
        if (!value.HasValue)
        {
            return value;
        }

        if (value.Value < 0f)
        {
            return 0f;
        }

        if (value.Value > 1f)
        {
            return 1f;
        }

        return value;
    }
}
