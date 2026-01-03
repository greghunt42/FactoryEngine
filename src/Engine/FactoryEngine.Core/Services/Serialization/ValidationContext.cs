using System;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Services.Serialization;

public sealed class ValidationContext
{
    private readonly List<string> _errors = new();
    private Func<AssetId, bool>? _assetResolver;
    private AssetMetadataRules? _metadataRules;

    public void Error(string message) => _errors.Add(message);

    public IReadOnlyList<string> Errors => _errors;

    public bool HasErrors => _errors.Count > 0;

    internal void SetAssetResolver(Func<AssetId, bool>? resolver)
    {
        _assetResolver = resolver;
    }

    internal void SetMetadataRules(AssetMetadataRules? rules)
    {
        _metadataRules = rules;
    }

    public AssetMetadataRules? MetadataRules => _metadataRules;

    public bool RequireAsset(string assetNamespace, string assetName, string? message = null)
    {
        if (_assetResolver is null)
        {
            return true;
        }

        var id = new AssetId(assetNamespace ?? string.Empty, assetName ?? string.Empty);
        if (_assetResolver(id))
        {
            return true;
        }

        Error(message ?? $"Asset '{id}' not registered in loaded catalogs.");
        return false;
    }
}
