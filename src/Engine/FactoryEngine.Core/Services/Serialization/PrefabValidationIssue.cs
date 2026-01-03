namespace FactoryEngine.Core.Services.Serialization;

public sealed record PrefabValidationIssue(string PrefabId, string? EntityName, string ComponentName, string Message);
