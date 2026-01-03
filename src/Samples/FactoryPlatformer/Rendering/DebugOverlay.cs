using System;
using System.Collections.Generic;
using FactoryPlatformer;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FactoryPlatformer.Rendering;

public sealed class DebugOverlay : IDisposable
{
    private readonly SpriteBatch _spriteBatch;
    private readonly Texture2D _pixel;
    private readonly MiniFont _font = new();
    private readonly List<string> _buffer = new();

    public DebugOverlay(GraphicsDevice graphicsDevice)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Draw(in DebugOverlayData data)
    {
        _buffer.Clear();
        _buffer.Add($"Scene: {data.SceneName}");
        _buffer.Add($"Prefabs: {data.PrefabList}");
        _buffer.Add($"FPS: {MathF.Round(data.FramesPerSecond)}");
        _buffer.Add($"Score: {data.Score} (High: {data.HighScore})");
        _buffer.Add($"State: {data.LoopState}");
        if (data.PendingResetSeconds is { } resetSeconds && resetSeconds > 0f)
        {
            _buffer.Add($"Reset in {resetSeconds:F1}s");
        }
        if (!string.IsNullOrWhiteSpace(data.LastEvent))
        {
            _buffer.Add($"Last: {data.LastEvent} ({MathF.Max(0f, data.LastEventAge):F0}s)");
        }
        if (data.EventHistory.Count == 0)
        {
            _buffer.Add("Recent Events: <none>");
        }
        else
        {
            _buffer.Add("Recent Events:");
            foreach (var entry in data.EventHistory)
            {
                var age = MathF.Max(0f, (float)(DateTime.UtcNow - entry.Timestamp).TotalSeconds);
                _buffer.Add($"  {entry.Message} ({age:F0}s)");
            }
        }
        if (data.MovementTuning.Count > 0)
        {
            _buffer.Add("Movement Tuning:");
            foreach (var line in data.MovementTuning)
            {
                _buffer.Add($"  {line}");
            }
        }
        _buffer.Add("Controls: ESC Exit | F5 Reload | PgUp/PgDn Scene | Space Jump");
        if (!string.IsNullOrWhiteSpace(data.SceneHint) && !data.SceneMenuOpen)
        {
            _buffer.Add(data.SceneHint);
        }

        if (!string.IsNullOrWhiteSpace(data.LoadingMessage))
        {
            var prefix = data.LoadingMessage.StartsWith("Failed", StringComparison.OrdinalIgnoreCase)
                ? "!"
                : data.Spinner.ToString();
            _buffer.Add($"Loading: {data.LoadingMessage} {prefix}");
        }

        if (data.SceneMenuOpen && data.SceneNames.Count > 0)
        {
            _buffer.Add("Select Scene:");
            for (var i = 0; i < data.SceneNames.Count; i++)
            {
                var indicator = i == data.SceneSelectionIndex ? "> " : "  ";
                _buffer.Add($"{indicator}{data.SceneNames[i]}");
            }
        }

        if (data.Errors.Count == 0)
        {
            _buffer.Add("Asset Issues: <none>");
        }
        else
        {
            _buffer.Add("Asset Issues:");
            foreach (var error in data.Errors)
            {
                _buffer.Add($"  {error}");
            }
        }

        var width = 0;
        foreach (var line in _buffer)
        {
            width = Math.Max(width, _font.Measure(line));
        }

        var lineHeight = _font.LineHeight + 2;
        var height = _buffer.Count * lineHeight - 2;
        const int padding = 8;
        var background = new Rectangle(10, 10, width + padding * 2, height + padding * 2);

        _spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.NonPremultiplied);
        _spriteBatch.Draw(_pixel, background, new Color(0f, 0f, 0f, 0.65f));

        var cursor = new Vector2(background.Left + padding, background.Top + padding);
        foreach (var line in _buffer)
        {
            _font.Draw(_spriteBatch, _pixel, line, cursor, Color.Lime);
            cursor.Y += lineHeight;
        }

        _spriteBatch.End();
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _spriteBatch.Dispose();
    }
}

public readonly record struct DebugOverlayData(
    string SceneName,
    string PrefabList,
    float FramesPerSecond,
    IReadOnlyList<string> Errors,
    int Score,
    int HighScore,
    string LastEvent,
    float LastEventAge,
    IReadOnlyList<GameEvent> EventHistory,
    IReadOnlyList<string> MovementTuning,
    LevelLoopState LoopState,
    float? PendingResetSeconds,
    string SceneHint,
    bool SceneMenuOpen,
    IReadOnlyList<string> SceneNames,
    int SceneSelectionIndex,
    string? LoadingMessage,
    char Spinner);

internal sealed class MiniFont
{
    private const int GlyphWidth = 4;
    private const int GlyphHeight = 5;
    private const int PixelSize = 2;
    private const int GlyphSpacing = 1;

    private static readonly Dictionary<char, string[]> Glyphs = InitializeGlyphs();

    public int LineHeight => GlyphHeight * PixelSize;

    public int Measure(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        var length = text.Length;
        return (length * (GlyphWidth + GlyphSpacing) - GlyphSpacing) * PixelSize;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, string text, Vector2 position, Color color)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var x = position.X;
        var y = position.Y;
        foreach (var raw in text)
        {
            var ch = Normalize(raw);
            if (!Glyphs.TryGetValue(ch, out var glyph))
            {
                glyph = Glyphs['?'];
            }

            DrawGlyph(spriteBatch, pixel, glyph, x, y, color);
            x += (GlyphWidth + GlyphSpacing) * PixelSize;
        }
    }

    private static char Normalize(char ch)
    {
        if (char.IsLetter(ch))
        {
            return char.ToUpperInvariant(ch);
        }

        return ch;
    }

    private static void DrawGlyph(SpriteBatch spriteBatch, Texture2D pixel, string[] glyph, float startX, float startY, Color color)
    {
        for (var row = 0; row < GlyphHeight; row++)
        {
            var pattern = glyph[row];
            for (var col = 0; col < GlyphWidth && col < pattern.Length; col++)
            {
                if (pattern[col] != '#')
                {
                    continue;
                }

                var rect = new Rectangle(
                    (int)startX + col * PixelSize,
                    (int)startY + row * PixelSize,
                    PixelSize,
                    PixelSize);
                spriteBatch.Draw(pixel, rect, color);
            }
        }
    }

    private static Dictionary<char, string[]> InitializeGlyphs()
    {
        return new Dictionary<char, string[]>
        {
            [' '] = new[]
            {
                "....",
                "....",
                "....",
                "....",
                "...."
            },
            ['A'] = new[]
            {
                ".##.",
                "#..#",
                "####",
                "#..#",
                "#..#"
            },
            ['B'] = new[]
            {
                "###.",
                "#..#",
                "###.",
                "#..#",
                "###."
            },
            ['C'] = new[]
            {
                ".###",
                "#...",
                "#...",
                "#...",
                ".###"
            },
            ['D'] = new[]
            {
                "###.",
                "#..#",
                "#..#",
                "#..#",
                "###."
            },
            ['E'] = new[]
            {
                "####",
                "#...",
                "###.",
                "#...",
                "####"
            },
            ['F'] = new[]
            {
                "####",
                "#...",
                "###.",
                "#...",
                "#..."
            },
            ['G'] = new[]
            {
                ".###",
                "#...",
                "#.##",
                "#..#",
                ".###"
            },
            ['H'] = new[]
            {
                "#..#",
                "#..#",
                "####",
                "#..#",
                "#..#"
            },
            ['I'] = new[]
            {
                ".##.",
                "..#.",
                "..#.",
                "..#.",
                ".##."
            },
            ['J'] = new[]
            {
                "..##",
                "...#",
                "...#",
                "#..#",
                ".##."
            },
            ['K'] = new[]
            {
                "#..#",
                "#.#.",
                "##..",
                "#.#.",
                "#..#"
            },
            ['L'] = new[]
            {
                "#...",
                "#...",
                "#...",
                "#...",
                "####"
            },
            ['M'] = new[]
            {
                "#..#",
                "####",
                "#..#",
                "#..#",
                "#..#"
            },
            ['N'] = new[]
            {
                "#..#",
                "##.#",
                "#.##",
                "#..#",
                "#..#"
            },
            ['O'] = new[]
            {
                ".##.",
                "#..#",
                "#..#",
                "#..#",
                ".##."
            },
            ['P'] = new[]
            {
                "###.",
                "#..#",
                "###.",
                "#...",
                "#..."
            },
            ['Q'] = new[]
            {
                ".##.",
                "#..#",
                "#..#",
                "#.##",
                ".###"
            },
            ['R'] = new[]
            {
                "###.",
                "#..#",
                "###.",
                "#.#.",
                "#..#"
            },
            ['S'] = new[]
            {
                ".###",
                "#...",
                ".##.",
                "...#",
                "###."
            },
            ['T'] = new[]
            {
                "####",
                ".#..",
                ".#..",
                ".#..",
                ".#.."
            },
            ['U'] = new[]
            {
                "#..#",
                "#..#",
                "#..#",
                "#..#",
                ".##."
            },
            ['V'] = new[]
            {
                "#..#",
                "#..#",
                "#..#",
                ".##.",
                ".##."
            },
            ['W'] = new[]
            {
                "#..#",
                "#..#",
                "####",
                "####",
                "#..#"
            },
            ['X'] = new[]
            {
                "#..#",
                ".##.",
                ".##.",
                ".##.",
                "#..#"
            },
            ['Y'] = new[]
            {
                "#..#",
                "#..#",
                ".##.",
                ".#..",
                ".#.."
            },
            ['Z'] = new[]
            {
                "####",
                "..#.",
                ".#..",
                "#...",
                "####"
            },
            ['0'] = new[]
            {
                ".##.",
                "#..#",
                "#..#",
                "#..#",
                ".##."
            },
            ['1'] = new[]
            {
                "..#.",
                ".##.",
                "..#.",
                "..#.",
                ".###"
            },
            ['2'] = new[]
            {
                ".##.",
                "...#",
                ".##.",
                "#...",
                "####"
            },
            ['3'] = new[]
            {
                "###.",
                "...#",
                ".##.",
                "...#",
                "###."
            },
            ['4'] = new[]
            {
                "#..#",
                "#..#",
                "####",
                "...#",
                "...#"
            },
            ['5'] = new[]
            {
                "####",
                "#...",
                "###.",
                "...#",
                "###."
            },
            ['6'] = new[]
            {
                ".##.",
                "#...",
                "###.",
                "#..#",
                ".##."
            },
            ['7'] = new[]
            {
                "####",
                "...#",
                "..#.",
                ".#..",
                ".#.."
            },
            ['8'] = new[]
            {
                ".##.",
                "#..#",
                ".##.",
                "#..#",
                ".##."
            },
            ['9'] = new[]
            {
                ".##.",
                "#..#",
                ".###",
                "...#",
                ".##."
            },
            [':'] = new[]
            {
                "..#.",
                "..#.",
                "....",
                "..#.",
                "..#."
            },
            ['.'] = new[]
            {
                "....",
                "....",
                "....",
                "..#.",
                "..#."
            },
            [','] = new[]
            {
                "....",
                "....",
                "....",
                "..#.",
                ".#.."
            },
            ['-'] = new[]
            {
                "....",
                "....",
                "####",
                "....",
                "...."
            },
            ['|'] = new[]
            {
                "..#.",
                "..#.",
                "..#.",
                "..#.",
                "..#."
            },
            ['/'] = new[]
            {
                "...#",
                "..#.",
                ".#..",
                "#...",
                "#..."
            },
            ['?'] = new[]
            {
                ".##.",
                "...#",
                ".#..",
                "....",
                ".#.."
            }
        };
    }
}
