using System.Reflection;
using System.Runtime.Loader;
using FactoryEngine.Core.Diagnostics;
using FactoryEngine.Core.Services.Serialization;

namespace FeTools;

internal sealed record DescriptorInfo(Type ComponentType, object Descriptor);

internal static class DescriptorLoader
{
    public static IReadOnlyList<DescriptorInfo> Load(IEnumerable<string> assemblyPaths, NdjsonLogger logger)
    {
        var descriptors = new List<DescriptorInfo>();
        foreach (var path in assemblyPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                logger.Warn($"Descriptor assembly '{path}' not found.");
                continue;
            }

            Assembly assembly;
            try
            {
                assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load descriptor assembly '{path}'", ex);
                continue;
            }

            descriptors.AddRange(DiscoverDescriptors(assembly, logger));
        }

        return descriptors;
    }

    public static IReadOnlyList<string> ResolveAssemblyPaths(IEnumerable<string> userSupplied)
    {
        var resolved = userSupplied.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
        if (resolved.Count > 0)
        {
            return resolved;
        }

        var defaultPath = Path.Combine("src", "Samples", "FactoryPlatformer", "bin", "Debug", "net8.0", "FactoryPlatformer.dll");
        if (File.Exists(defaultPath))
        {
            resolved.Add(defaultPath);
        }

        return resolved;
    }

    public static void RegisterAll(SerializationService service, IReadOnlyList<DescriptorInfo> descriptors)
    {
        if (descriptors.Count == 0)
        {
            return;
        }

        var method = typeof(SerializationService).GetMethod(nameof(SerializationService.RegisterDescriptor));
        if (method == null)
        {
            throw new InvalidOperationException("Unable to find RegisterDescriptor method.");
        }

        foreach (var descriptor in descriptors)
        {
            var generic = method.MakeGenericMethod(descriptor.ComponentType);
            generic.Invoke(service, new[] { descriptor.Descriptor });
        }
    }

    private static IEnumerable<DescriptorInfo> DiscoverDescriptors(Assembly assembly, NdjsonLogger logger)
    {
        foreach (var type in assembly.GetTypes())
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType && iface.GetGenericTypeDefinition() == typeof(IComponentDescriptor<>))
                {
                    var componentType = iface.GenericTypeArguments[0];
                    DescriptorInfo? descriptorInfo = null;
                    try
                    {
                        var instance = Activator.CreateInstance(type);
                        if (instance is not null)
                        {
                            logger.Debug($"Loaded descriptor {type.FullName} for component {componentType.Name}");
                            descriptorInfo = new DescriptorInfo(componentType, instance);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Failed to instantiate descriptor '{type.FullName}'", ex);
                    }

                    if (descriptorInfo is not null)
                    {
                        yield return descriptorInfo;
                    }
                }
            }
        }
    }
}
