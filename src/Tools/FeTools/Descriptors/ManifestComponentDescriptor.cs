using System;
using FactoryEngine.Core.Services.Serialization;

namespace FeTools.Descriptors;

internal sealed class ManifestComponentDescriptor : IComponentDescriptor<ManifestComponent>, IRawComponentDescriptor
{
    private readonly ComponentDescriptorDefinition _definition;

    public ManifestComponentDescriptor(ComponentDescriptorDefinition definition)
    {
        _definition = definition;
    }

    public string Name => _definition.Name;
    public int Version => _definition.Version;

    public void Serialize(ref ManifestComponent component, IComponentWriter writer)
    {
        throw new NotSupportedException("Manifest descriptors do not serialize components.");
    }

    public ManifestComponent Deserialize(IComponentReader reader)
    {
        throw new NotSupportedException("Manifest descriptors are only used for validation.");
    }

    public void Validate(ManifestComponent component, ValidationContext context)
    {
        // Validation handled via IRawComponentDescriptor
    }

    public void ValidateRaw(PrefabComponent component, ValidationContext context)
    {
        foreach (var field in _definition.Fields)
        {
            if (!component.Data.TryGetValue(field.Name, out var value) || value is null)
            {
                if (field.Required)
                {
                    context.Error($"Field '{field.Name}' is required.");
                }
                continue;
            }

            if (!IsTypeMatch(value, field.Type))
            {
                context.Error($"Field '{field.Name}' expected type '{field.Type}' but received '{value.GetType().Name}'.");
                continue;
            }

            if (field.AllowedValues.Count > 0 && value is string str && !field.AllowedValues.Contains(str))
            {
                context.Error($"Field '{field.Name}' value '{str}' not in allowed values ({string.Join(", ", field.AllowedValues)}).");
            }

            if ((field.Min.HasValue || field.Max.HasValue) && TryGetNumericValue(value, out var numeric))
            {
                if (field.Min.HasValue && numeric < field.Min.Value)
                {
                    context.Error($"Field '{field.Name}' value {numeric} is less than minimum {field.Min.Value}.");
                }

                if (field.Max.HasValue && numeric > field.Max.Value)
                {
                    context.Error($"Field '{field.Name}' value {numeric} exceeds maximum {field.Max.Value}.");
                }
            }
        }
    }

    private static bool IsTypeMatch(object value, string type)
    {
        return type switch
        {
            "int" => value is int or long or short or byte || (value is double d && Math.Abs(d - Math.Round(d)) < 0.0001),
            "float" => value is float or double or decimal or int or long,
            "bool" => value is bool,
            "string" => value is string,
            _ => true
        };
    }

    private static bool TryGetNumericValue(object value, out double numeric)
    {
        switch (value)
        {
            case int i:
                numeric = i;
                return true;
            case long l:
                numeric = l;
                return true;
            case float f:
                numeric = f;
                return true;
            case double d:
                numeric = d;
                return true;
            case decimal m:
                numeric = (double)m;
                return true;
            default:
                numeric = 0;
                return false;
        }
    }
}
