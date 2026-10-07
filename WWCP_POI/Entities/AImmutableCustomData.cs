using System.Text.Json;
using cloud.charging.open.protocols.WWCP;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Custom data with detached input and output, including when accessed through a base reference.
/// </summary>
public abstract class AImmutableCustomData
{
    private readonly JsonElement? customData;
    private readonly String canonicalCustomData;

    protected AImmutableCustomData(CustomData? CustomData = null)
    {
        if (CustomData is not null)
            customData = JsonSerializer.Deserialize<JsonElement>(CustomData.ToJSON().ToString());
        canonicalCustomData = CustomData is null ? "null" : org.GraphDefined.Vanaheimr.Illias.CanonicalJSON.Serialize(CustomData.ToJSON());
    }

    /// <summary>
    /// A detached copy of the custom data.
    /// </summary>
    public CustomData? CustomData
        => customData is { } value ? WWCP.CustomData.Parse((Newtonsoft.Json.Linq.JObject) InfrastructureJson.ReadToken(value.GetRawText())) : null;

    public override Boolean Equals(Object? other)
        => other is AImmutableCustomData value &&
           String.Equals(canonicalCustomData, value.canonicalCustomData, StringComparison.Ordinal);

    public override Int32 GetHashCode()
        => canonicalCustomData.GetHashCode(StringComparison.Ordinal);
}
