namespace FactoryEngine.Core.Services.Serialization;

public interface IComponentReader
{
    int ReadInt(string name, int defaultValue = 0);
    float ReadFloat(string name, float defaultValue = 0f);
    bool ReadBool(string name, bool defaultValue = false);
    string ReadString(string name, string defaultValue = "");
}
