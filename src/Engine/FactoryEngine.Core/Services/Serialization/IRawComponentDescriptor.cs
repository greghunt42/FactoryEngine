namespace FactoryEngine.Core.Services.Serialization;

public interface IRawComponentDescriptor
{
    void ValidateRaw(PrefabComponent component, ValidationContext context);
}
