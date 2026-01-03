namespace FactoryEngine.Core.Services.Rendering;

public sealed class NullRenderBackend : IRenderBackend
{
    public void BeginFrame()
    {
    }

    public void DrawSprite(RenderedSprite sprite)
    {
    }

    public void OnError(string message)
    {
        // Intentionally silent; diagnostics surface errors separately.
    }

    public void EndFrame()
    {
    }
}
