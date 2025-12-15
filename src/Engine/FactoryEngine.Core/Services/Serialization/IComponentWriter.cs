namespace FactoryEngine.Core.Services.Serialization;

public interface IComponentWriter
{
    void WriteInt(string name, int value);
    void WriteFloat(string name, float value);
    void WriteBool(string name, bool value);
    void WriteString(string name, string value);
}
