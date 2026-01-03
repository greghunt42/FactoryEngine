using System;
using System.Collections.Generic;

namespace FactoryEngine.Core.Services.Asset;

public static class AssetCatalogResolver
{
    public static Func<AssetId, bool> BuildResolver(IEnumerable<AssetCatalog> catalogs)
    {
        ArgumentNullException.ThrowIfNull(catalogs);
        var lookup = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var catalog in catalogs)
        {
            foreach (var asset in catalog.Assets.Keys)
            {
                lookup.Add($"{catalog.Namespace}:{asset}");
            }
        }

        return assetId =>
        {
            if (string.IsNullOrWhiteSpace(assetId.Name))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(assetId.Namespace))
            {
                return lookup.Contains(assetId.Name);
            }

            var key = $"{assetId.Namespace}:{assetId.Name}";
            return lookup.Contains(key);
        };
    }
}
