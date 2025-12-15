namespace FactoryEngine.Core.Services.Asset;

public readonly struct AssetHandle<T> where T : class
{
    public AssetHandle(T? value, string hash)
    {
        Value = value;
        Hash = hash;
    }

    public T? Value { get; }
    public string Hash { get; }

    public bool IsValid => Value is not null;
}
