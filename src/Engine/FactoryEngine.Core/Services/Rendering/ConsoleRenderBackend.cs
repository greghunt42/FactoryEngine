using System;
using System.IO;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Rendering;

public sealed class ConsoleRenderBackend : IRenderBackend
{
    private readonly TextWriter _writer;

    public ConsoleRenderBackend(TextWriter? writer = null)
    {
        _writer = writer ?? Console.Out;
    }

    public void BeginFrame()
    {
    }

    public void DrawSprite(RenderedSprite sprite)
    {
        var command = sprite.Command;
        _writer.WriteLine(
            $"Draw sprite {command.Texture} ({sprite.Texture.Path}) at ({command.X:F2}, {command.Y:F2}) layer {command.Layer:F2}");
    }

    public void OnError(string message)
    {
        _writer.WriteLine($"[RenderError] {message}");
    }

    public void EndFrame()
    {
    }
}
