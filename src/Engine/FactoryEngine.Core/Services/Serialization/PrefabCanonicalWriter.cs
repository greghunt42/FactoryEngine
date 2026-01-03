using System.Text.Json;
using System.Linq;

namespace FactoryEngine.Core.Services.Serialization;

public static class PrefabCanonicalWriter
{
    public static void Write(PrefabDefinition prefab, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        using var stream = File.Create(path);
        using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true });
        WritePrefab(prefab, writer);
    }

    private static void WritePrefab(PrefabDefinition prefab, Utf8JsonWriter writer)
    {
        writer.WriteStartObject();
        writer.WriteString("id", prefab.Id);
        writer.WritePropertyName("entities");
        writer.WriteStartArray();
        foreach (var entity in prefab.Entities)
        {
            writer.WriteStartObject();
            if (!string.IsNullOrWhiteSpace(entity.Name))
            {
                writer.WriteString("name", entity.Name);
            }

            writer.WritePropertyName("components");
            writer.WriteStartArray();
            foreach (var component in entity.Components)
            {
                writer.WriteStartObject();
                writer.WriteString("name", component.ComponentName);
                writer.WritePropertyName("data");
                WriteDataObject(writer, component.Data);
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        writer.WriteEndArray();
        writer.WriteEndObject();
    }

    private static void WriteDataObject(Utf8JsonWriter writer, Dictionary<string, object?> data)
    {
        writer.WriteStartObject();
        foreach (var entry in data.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            WriteValue(writer, entry.Key, entry.Value);
        }
        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, string name, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNull(name);
                break;
            case string s:
                writer.WriteString(name, s);
                break;
            case bool b:
                writer.WriteBoolean(name, b);
                break;
            case int i:
                writer.WriteNumber(name, i);
                break;
            case long l:
                writer.WriteNumber(name, l);
                break;
            case float f:
                writer.WriteNumber(name, f);
                break;
            case double d:
                writer.WriteNumber(name, d);
                break;
            case IEnumerable<object?> list:
                writer.WritePropertyName(name);
                writer.WriteStartArray();
                foreach (var item in list)
                {
                    WriteArrayValue(writer, item);
                }
                writer.WriteEndArray();
                break;
            case Dictionary<string, object?> nested:
                writer.WritePropertyName(name);
                WriteDataObject(writer, nested);
                break;
            default:
                writer.WriteString(name, value.ToString());
                break;
        }
    }

    private static void WriteArrayValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case string s:
                writer.WriteStringValue(s);
                break;
            case bool b:
                writer.WriteBooleanValue(b);
                break;
            case int i:
                writer.WriteNumberValue(i);
                break;
            case long l:
                writer.WriteNumberValue(l);
                break;
            case float f:
                writer.WriteNumberValue(f);
                break;
            case double d:
                writer.WriteNumberValue(d);
                break;
            case IEnumerable<object?> list:
                writer.WriteStartArray();
                foreach (var nested in list)
                {
                    WriteArrayValue(writer, nested);
                }
                writer.WriteEndArray();
                break;
            case Dictionary<string, object?> nestedObject:
                writer.WriteStartObject();
                foreach (var entry in nestedObject.OrderBy(k => k.Key, StringComparer.Ordinal))
                {
                    WriteValue(writer, entry.Key, entry.Value);
                }
                writer.WriteEndObject();
                break;
            default:
                writer.WriteStringValue(value.ToString());
                break;
        }
    }
}
