using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetalLink.Shared;

/// <summary>
/// Custom JSON converter for AccountNumber that handles both string and numeric inputs
/// </summary>
public class AccountNumberConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.String:
                return reader.GetString() ?? string.Empty;
            
            case JsonTokenType.Number:
                if (reader.TryGetInt64(out long longValue))
                {
                    // Format as 8-digit padded string
                    return longValue.ToString("D8");
                }
                else if (reader.TryGetDecimal(out decimal decValue))
                {
                    // Format as 8-digit padded string
                    return ((long)decValue).ToString("D8");
                }
                break;
            
            case JsonTokenType.Null:
                return string.Empty;
        }

        throw new JsonException($"Unable to convert token of type {reader.TokenType} to string for AccountNumber");
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}
