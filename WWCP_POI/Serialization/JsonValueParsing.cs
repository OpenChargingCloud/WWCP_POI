using System.Globalization;
using Newtonsoft.Json.Linq;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>Reads scalar JSON values without converting JSON numbers through the current culture.</summary>
internal static class JsonValueParsing
{
    internal delegate bool ScalarParser<T>(string text, out T value) where T : struct;

    internal static bool TryReadOptional<T>(JObject json, string name, ScalarParser<T> parser,
                                            out T? value, out string? error) where T : struct
    {
        value = null;
        error = null;
        var token = json[name];
        if (token is null || token.Type == JTokenType.Null)
            return true;

        var text = token.Type switch
        {
            JTokenType.String => token.Value<string>(),
            JTokenType.Integer or JTokenType.Float => Convert.ToString(((JValue) token).Value, CultureInfo.InvariantCulture),
            _ => null
        };

        if (text is not null && parser(text, out var parsed))
        {
            value = parsed;
            return true;
        }

        error = $"Invalid '{name}': expected a valid scalar value.";
        return false;
    }
}
