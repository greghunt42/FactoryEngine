namespace FactoryEngine.Core.Services.Serialization;

internal sealed class DictionaryComponentReader : IComponentReader
{
    private readonly IReadOnlyDictionary<string, object?> _data;

    public DictionaryComponentReader(IReadOnlyDictionary<string, object?> data)
    {
        _data = data;
    }

    public int ReadInt(string name, int defaultValue = 0)
    {
        if (_data.TryGetValue(name, out var value) && value is not null)
        {
            return Convert.ToInt32(value);
        }

        return defaultValue;
    }

    public float ReadFloat(string name, float defaultValue = 0f)
    {
        if (_data.TryGetValue(name, out var value) && value is not null)
        {
            return Convert.ToSingle(value);
        }

        return defaultValue;
    }

    public bool ReadBool(string name, bool defaultValue = false)
    {
        if (_data.TryGetValue(name, out var value) && value is not null)
        {
            return Convert.ToBoolean(value);
        }

        return defaultValue;
    }

    public string ReadString(string name, string defaultValue = "")
    {
        if (_data.TryGetValue(name, out var value) && value is not null)
        {
            return Convert.ToString(value) ?? defaultValue;
        }

        return defaultValue;
    }
}
