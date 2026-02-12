using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetalLink.Shared.Json;

/// <summary>
/// Serializes account numbers as 8-digit zero-padded strings (e.g. 00000001)
/// while allowing deserialization from either string or number.
/// </summary>
public sealed class PaddedAccountNumberLongConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
            return null;

        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt64();

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString();
            if (string.IsNullOrWhiteSpace(s))
                return null;

            // Allow both padded (00000001) and plain (1)
            if (long.TryParse(s, out var value))
                return value;
        }

        throw new JsonException("Invalid account number value.");
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (!value.HasValue)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString("D8"));
    }
}
