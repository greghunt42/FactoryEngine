using System.Collections.Generic;

namespace FactoryEngine.Core.Services.Asset;

public sealed class AssetMetadataRules
{
    private readonly HashSet<string> _textureFormats;
    private readonly HashSet<string> _audioGroups;
    private readonly List<string> _textureFormatList;
    private readonly List<string> _audioGroupList;

    public AssetMetadataRules(IEnumerable<string> textureFormats, IEnumerable<string> audioGroups, string defaultAudioGroup)
    {
        if (textureFormats is null)
        {
            throw new ArgumentNullException(nameof(textureFormats));
        }

        if (audioGroups is null)
        {
            throw new ArgumentNullException(nameof(audioGroups));
        }

        _textureFormatList = Normalize(textureFormats);
        _audioGroupList = Normalize(audioGroups);
        _textureFormats = new HashSet<string>(_textureFormatList, StringComparer.OrdinalIgnoreCase);
        _audioGroups = new HashSet<string>(_audioGroupList, StringComparer.OrdinalIgnoreCase);

        DefaultAudioGroup = string.IsNullOrWhiteSpace(defaultAudioGroup)
            ? (_audioGroupList.Count > 0 ? _audioGroupList[0] : "sfx")
            : defaultAudioGroup;

        if (!_audioGroups.Contains(DefaultAudioGroup))
        {
            _audioGroups.Add(DefaultAudioGroup);
            _audioGroupList.Add(DefaultAudioGroup);
        }
    }

    public IReadOnlyList<string> TextureFormats => _textureFormatList;
    public IReadOnlyList<string> AudioGroups => _audioGroupList;
    public string DefaultAudioGroup { get; }

    public bool IsTextureFormatAllowed(string value) =>
        !string.IsNullOrWhiteSpace(value) && _textureFormats.Contains(value);

    public bool IsAudioGroupAllowed(string value) =>
        !string.IsNullOrWhiteSpace(value) && _audioGroups.Contains(value);

    private static List<string> Normalize(IEnumerable<string> values)
    {
        var list = new List<string>();
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var value in values)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                continue;
            }

            if (set.Add(trimmed))
            {
                list.Add(trimmed);
            }
        }

        return list;
    }
}
