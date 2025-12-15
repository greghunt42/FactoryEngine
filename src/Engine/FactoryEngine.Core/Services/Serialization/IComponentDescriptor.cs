namespace FactoryEngine.Core.Services.Serialization;

public interface IComponentDescriptor<T> where T : struct
{
    string Name { get; }
    int Version { get; }

    void Serialize(ref T component, IComponentWriter writer);
    T Deserialize(IComponentReader reader);
    void Validate(T component, ValidationContext context);
}
