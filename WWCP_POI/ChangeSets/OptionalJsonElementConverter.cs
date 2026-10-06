using System.Text.Json;
using System.Text.Json.Serialization;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Distinguishes an omitted operation value from an explicit JSON null.
/// Omitted values remain nullable null; a present null is a defined JsonElement.
/// </summary>
internal sealed class OptionalJsonElementConverter : JsonConverter<JsonElement?>
{
    public override bool HandleNull => true;

    public override JsonElement? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.Clone();
    }

    public override void Write(Utf8JsonWriter writer, JsonElement? value, JsonSerializerOptions options)
    {
        if (value.HasValue)
            value.Value.WriteTo(writer);
        else
            writer.WriteNullValue();
    }
}
