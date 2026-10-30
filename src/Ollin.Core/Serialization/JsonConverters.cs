using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ollin.Core.Serialization;

/// <summary>Serializa Vector2 como [x, y] para que los escenarios sean legibles.</summary>
public sealed class Vector2JsonConverter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();
            reader.Read();
            return new Vector2(x, y);
        }

        // Compatibilidad: acepta también { "x": 1, "y": 2 }.
        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        return new Vector2(
            root.TryGetProperty("x", out var px) ? px.GetSingle() : 0f,
            root.TryGetProperty("y", out var py) ? py.GetSingle() : 0f);
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteEndArray();
    }
}

/// <summary>Serializa Vector4 (color) como [r, g, b, a].</summary>
public sealed class Vector4JsonConverter : JsonConverter<Vector4>
{
    public override Vector4 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read();
            var x = reader.GetSingle();
            reader.Read();
            var y = reader.GetSingle();
            reader.Read();
            var z = reader.GetSingle();
            reader.Read();
            var w = reader.GetSingle();
            reader.Read();
            return new Vector4(x, y, z, w);
        }

        using var document = JsonDocument.ParseValue(ref reader);
        var root = document.RootElement;
        return new Vector4(
            root.TryGetProperty("x", out var px) ? px.GetSingle() : 1f,
            root.TryGetProperty("y", out var py) ? py.GetSingle() : 1f,
            root.TryGetProperty("z", out var pz) ? pz.GetSingle() : 1f,
            root.TryGetProperty("w", out var pw) ? pw.GetSingle() : 1f);
    }

    public override void Write(Utf8JsonWriter writer, Vector4 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteNumberValue(value.W);
        writer.WriteEndArray();
    }
}
