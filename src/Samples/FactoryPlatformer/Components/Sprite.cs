using FactoryEngine.Core.Services.Serialization;

namespace FactoryPlatformer.Components;

public struct Sprite
{
    public string TextureNamespace;
    public string TextureName;
    public float Layer;
}

public sealed class SpriteDescriptor : IComponentDescriptor<Sprite>
{
    public string Name => "Sprite";
    public int Version => 1;

    public void Serialize(ref Sprite component, IComponentWriter writer)
    {
        writer.WriteString("ns", component.TextureNamespace);
        writer.WriteString("name", component.TextureName);
        writer.WriteFloat("layer", component.Layer);
    }

    public Sprite Deserialize(IComponentReader reader)
    {
        return new Sprite
        {
            TextureNamespace = reader.ReadString("ns", "core"),
            TextureName = reader.ReadString("name", string.Empty),
            Layer = reader.ReadFloat("layer")
        };
    }

    public void Validate(Sprite component, ValidationContext context)
    {
        if (string.IsNullOrEmpty(component.TextureName))
        {
            context.Error("Sprite texture name required");
            return;
        }

        var ns = string.IsNullOrWhiteSpace(component.TextureNamespace) ? string.Empty : component.TextureNamespace;
        context.RequireAsset(ns, component.TextureName);
    }
}
