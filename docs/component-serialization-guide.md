# Component Serialization Guide

This guide shows how to make ECS components data-driven through descriptors and serialization hooks.

## Goals
- Ensure every component can be instantiated from JSON/YAML data.
- Provide consistent validation and defaulting behavior.
- Keep serialization logic close to component definitions for maintainability.

## Component Definition Example
```csharp
public struct Health
{
    public int Max;
    public int Current;
    public float RegenRate;
}
```

## Descriptor Registration
```csharp
public sealed class HealthComponentDescriptor : IComponentDescriptor<Health>
{
    public string Name => "Health";
    public int Version => 1;

    public void Serialize(ref Health component, IComponentWriter writer)
    {
        writer.WriteInt("max", component.Max);
        writer.WriteInt("current", component.Current);
        writer.WriteFloat("regenRate", component.RegenRate);
    }

    public Health Deserialize(IComponentReader reader)
    {
        return new Health
        {
            Max = reader.ReadInt("max", defaultValue: 100),
            Current = reader.ReadInt("current", defaultValue: 100),
            RegenRate = reader.ReadFloat("regenRate", defaultValue: 0f)
        };
    }

    public void Validate(Health component, ValidationContext ctx)
    {
        if (component.Max <= 0)
            ctx.Error("Health.Max must be > 0");
        if (component.Current < 0 || component.Current > component.Max)
            ctx.Error("Health.Current out of range");
    }
}
```

## Registration Flow
1. Module registers descriptor during initialization:
```csharp
serializationService.RegisterDescriptor(new HealthComponentDescriptor());
```
2. Prefab loader uses descriptors to instantiate components when parsing data files.

## Prefab Snippet
```yaml
components:
  Health:
    max: 120
    current: 75
    regenRate: 1.5
```

## Versioning
- Increment descriptor `Version` when serialization format changes.
- Provide migration hooks to convert older data:
```csharp
public void Upgrade(DataObject data, int fromVersion)
{
    if (fromVersion < 2)
    {
        data["regenRate"] = 0f;
    }
}
```

## Validation & Tooling
- `fe-tools validate-data` invokes `Validate` to catch issues before runtime.
- Descriptor metadata can export schemas for editor tooling (JSON Schema, etc.).

## Best Practices
- Keep components POD-like (no references to services or MonoGame types).
- Prefer explicit fields over dictionaries for clarity/performance.
- Use shared enums/IDs defined in data (asset IDs, action names) to keep data portable.
- Document component schema alongside descriptor for designers.
