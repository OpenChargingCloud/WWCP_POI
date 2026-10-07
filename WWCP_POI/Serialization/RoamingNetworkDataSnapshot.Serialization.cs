using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkDataSnapshot
{
    /// <summary>
    /// Reconstruct a static snapshot from JSON and validate every declared POI content identifier.
    /// </summary>
    public static RoamingNetworkDataSnapshot Parse(String json)
        => Parse(InfrastructureJson.ReadObject(json));

    /// <summary>
    /// Reconstruct a static snapshot, preserving version metadata and valid optional property presence.
    /// Current statuses are restored during domain validation and excluded from immutable storage.
    /// </summary>
    public static RoamingNetworkDataSnapshot Parse(JObject json)
        => POIRepresentation.ParseJSON(json, document => RoamingNetwork.Parse(document).DataSnapshot);

    /// <summary>
    /// Reconstruct a frozen data snapshot from a CBOR POI hierarchy and check declared ETags.
    /// </summary>
    public static RoamingNetworkDataSnapshot ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, document => RoamingNetwork.Parse(document).DataSnapshot);

    /// <summary>
    /// Try to reconstruct a frozen CBOR snapshot without returning a partial result.
    /// </summary>
    public static Boolean TryParseCBOR(ReadOnlySpan<Byte> data,
                                       [NotNullWhen(true)] out RoamingNetworkDataSnapshot? snapshot,
                                       [NotNullWhen(false)] out String? error)
        => POIRepresentation.TryParseCBOR(data, document => RoamingNetwork.Parse(document).DataSnapshot, out snapshot, out error);
}
