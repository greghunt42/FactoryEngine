namespace FactoryEngine.Core.Services.Serialization;

public sealed class ValidationContext
{
    private readonly List<string> _errors = new();

    public void Error(string message) => _errors.Add(message);

    public IReadOnlyList<string> Errors => _errors;

    public bool HasErrors => _errors.Count > 0;
}
