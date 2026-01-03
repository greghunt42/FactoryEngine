using FactoryEngine.Core.Diagnostics;
using FactoryEngine.Core.Services.Serialization;

namespace FeTools.Descriptors;

internal static class ManifestDescriptorLoader
{
    public static IReadOnlyList<DescriptorInfo> Load(IEnumerable<string> manifestPaths, NdjsonLogger logger)
    {
        var descriptors = new List<DescriptorInfo>();
        foreach (var path in manifestPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (!File.Exists(path))
            {
                logger.Warn($"Descriptor manifest '{path}' not found.");
                continue;
            }

            try
            {
                var manifest = DescriptorManifest.Load(path, logger);
                foreach (var definition in manifest.Components)
                {
                    if (string.IsNullOrWhiteSpace(definition.Name))
                    {
                        logger.Warn($"Manifest '{path}' contains component with no name.");
                        continue;
                    }

                    var descriptor = new ManifestComponentDescriptor(definition);
                    descriptors.Add(new DescriptorInfo(typeof(ManifestComponent), descriptor));
                    logger.Info($"Registered manifest descriptor '{definition.Name}' from {path}.");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load descriptor manifest '{path}'", ex);
            }
        }

        return descriptors;
    }
}
