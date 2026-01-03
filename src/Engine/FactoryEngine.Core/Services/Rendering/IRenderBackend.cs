namespace FactoryEngine.Core.Services.Rendering;

public interface IRenderBackend
{
    void BeginFrame();
    void DrawSprite(RenderedSprite sprite);
    void OnError(string message);
    void EndFrame();
}
