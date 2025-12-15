namespace FactoryEngine.Core.Services.Asset;

public sealed class AssetCatalog
{
    public AssetCatalog(string @namespace)
    {
        Namespace = @namespace;
    }

    public string Namespace { get; }
    public Dictionary<string, AssetRecord> Assets { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed class AssetRecord
{
    public required string Type { get; init; }
    public required string Path { get; init; }
    public string? Hash { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}
