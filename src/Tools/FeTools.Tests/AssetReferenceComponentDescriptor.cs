using FactoryEngine.Core.Services.Serialization;

namespace FeTools.Tests;

public struct AssetReferenceComponent
{
    public string Namespace;
    public string Name;
}

public sealed class AssetReferenceComponentDescriptor : IComponentDescriptor<AssetReferenceComponent>
{
    public string Name => nameof(AssetReferenceComponent);
    public int Version => 1;

    public void Serialize(ref AssetReferenceComponent component, IComponentWriter writer)
    {
        writer.WriteString("ns", component.Namespace);
        writer.WriteString("name", component.Name);
    }

    public AssetReferenceComponent Deserialize(IComponentReader reader)
    {
        return new AssetReferenceComponent
        {
            Namespace = reader.ReadString("ns", "core"),
            Name = reader.ReadString("name", string.Empty)
        };
    }

    public void Validate(AssetReferenceComponent component, ValidationContext context)
    {
        if (string.IsNullOrWhiteSpace(component.Name))
        {
            context.Error("Asset name required.");
            return;
        }

        var ns = string.IsNullOrWhiteSpace(component.Namespace) ? string.Empty : component.Namespace;
        context.RequireAsset(ns, component.Name);
    }
}
