using System.Collections.Generic;

namespace FactoryEngine.Core.Services.Asset;

public sealed class AudioClipAsset
{
    public AudioClipAsset(string path, byte[] content, IReadOnlyDictionary<string, string>? metadata = null)
    {
        Path = path;
        Content = content;
        Metadata = metadata;
    }

    public string Path { get; }
    public byte[] Content { get; }
    public IReadOnlyDictionary<string, string>? Metadata { get; }
}
