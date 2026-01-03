using FactoryEngine.Core.Services.Rendering;

namespace FactoryPlatformer.Rendering;

public sealed class DiagnosticsRenderBackend : IRenderBackend, IDisposable
{
    private readonly IRenderBackend _inner;
    private readonly RunnerDiagnostics _diagnostics;

    public DiagnosticsRenderBackend(IRenderBackend inner, RunnerDiagnostics diagnostics)
    {
        _inner = inner;
        _diagnostics = diagnostics;
    }

    public void BeginFrame() => _inner.BeginFrame();

    public void DrawSprite(RenderedSprite sprite) => _inner.DrawSprite(sprite);

    public void OnError(string message)
    {
        _diagnostics.ReportRenderError(message);
        _inner.OnError(message);
    }

    public void EndFrame() => _inner.EndFrame();

    public void Dispose()
    {
        if (_inner is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
