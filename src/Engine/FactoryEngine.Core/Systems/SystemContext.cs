using FactoryEngine.Core.Services;

namespace FactoryEngine.Core.Systems;

public readonly record struct SystemContext(SystemPhase Phase, float DeltaTime, EngineServices Services);
