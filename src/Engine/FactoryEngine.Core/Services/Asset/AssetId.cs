namespace FactoryEngine.Core.Services.Asset;

public readonly record struct AssetId(string Namespace, string Name)
{
    public override string ToString() => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}:{Name}";

    public static AssetId Parse(string value)
    {
        var parts = value.Split(':');
        return parts.Length == 2 ? new AssetId(parts[0], parts[1]) : new AssetId(string.Empty, value);
    }
}
