using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Sec;
using Org.BouncyCastle.Crypto.Parameters;

namespace cloud.charging.open.protocols.WWCP.POI;

public partial class ChargingStationManufacturer
{
    /// <summary>
    /// Reconstruct immutable manufacturer data and public cryptographic identities.
    /// </summary>
    public static ChargingStationManufacturer Parse(JObject json)
        => new(ChargingStationManufacturer_Id.Parse(POIValueJSON.Required(json, "@id")),
               new ImmutableI18NString(POIValueJSON.Text(json, "name")),
               new ImmutableI18NString(POIValueJSON.Text(json, "description")),
               InfrastructureJson.Array(json, "cryptoKeys", token => ImmutableCryptoKeyInfo.Parse(InfrastructureJson.Entry(token))).ToImmutableArray());
}

public partial class ImmutableCryptoKeyInfo
{
    private ImmutableCryptoKeyInfo(JObject json)
    {
        json = (JObject) json.DeepClone();
        json.Remove("ETags");
        InfrastructureJson.ValidateFields(json, "@context", "publicKey", "privateKey", "keyEncoding", "keyUsages", "certificates", "notBefore", "notAfter", "priority");
        PublicKey = POIValueJSON.Required(json, "publicKey");
        KeyType = POIValueJSON.Required(json, "@context");
        KeyEncoding = POIValueJSON.Required(json, "keyEncoding");
        NotBefore = InfrastructureJson.Date(json, "notBefore");
        NotAfter = InfrastructureJson.Date(json, "notAfter");
        if (NotBefore > NotAfter) throw new ArgumentException("Invalid key validity interval.");
        Priority = json["priority"]?.Type == JTokenType.Integer ? json["priority"]!.Value<UInt32>() :
                   throw new ArgumentException("priority: expected an unsigned integer.");
        KeyUsages = InfrastructureJson.Array(json, "keyUsages", token => token.Type == JTokenType.String ? token.Value<String>()! :
                       throw new ArgumentException("keyUsages: expected text identifiers.")).ToImmutableArray();
        if (json["certificates"] is { } certificates && certificates is not JArray)
            throw new ArgumentException("certificates: expected an array.");
        document = JsonSerializer.Deserialize<JsonElement>(json.ToString());
        json.Remove("privateKey");
        publicDocument = JsonSerializer.Deserialize<JsonElement>(json.ToString());
    }

    /// <summary>
    /// Parse a detached key description. Public serialization excludes private key material.
    /// </summary>
    public static ImmutableCryptoKeyInfo Parse(JObject json) => new(json);
}

public partial class ParkingProduct
{
    /// <summary>
    /// Serialize immutable POI metadata and its content identifiers.
    /// </summary>
    public JObject ToJSON() => this.ToJSONWithETags();

    /// <summary>
    /// Reconstruct a parking product with SI duration strings.
    /// </summary>
    public static ParkingProduct Parse(JObject json)
        => new(ParkingProduct_Id.Parse(POIValueJSON.Required(json, "@id")),
               MetrologyJson.ReadDuration(json, "minDuration"), MetrologyJson.ReadDuration(json, "stopParkingAfterTime"));
}

public partial class EVRoamingPartnerInfo
{
    /// <summary>
    /// Serialize immutable POI metadata and its content identifiers.
    /// </summary>
    public JObject ToJSON() => this.ToJSONWithETags();

    /// <summary>
    /// Reconstruct immutable roaming-partner metadata.
    /// </summary>
    public static EVRoamingPartnerInfo Parse(JObject json)
        => new(EMobilityProvider_Id.Parse(POIValueJSON.Required(json, "eMobilityProviderId")),
               POIValueJSON.Text(json, "name"), InfrastructureJson.Date(json, "notBefore")?.UtcDateTime,
               InfrastructureJson.Date(json, "notAfter")?.UtcDateTime, POIValueJSON.Text(json, "comment"));
}

public partial class RootCAInfo
{
    /// <summary>
    /// Serialize immutable POI metadata and its content identifiers.
    /// </summary>
    public JObject ToJSON() => this.ToJSONWithETags();

    /// <summary>
    /// Reconstruct root CA metadata and an SEC or NIST named elliptic-curve public key.
    /// </summary>
    public static RootCAInfo Parse(JObject json)
    {
        var algorithm = POIValueJSON.Required(json, "algorithm");
        var curve = NistNamedCurves.GetByName(algorithm) ?? SecNamedCurves.GetByName(algorithm) ??
                    throw new ArgumentException("algorithm: unknown named elliptic curve.");
        var parameters = new ECDomainParameters(curve.Curve, curve.G, curve.N, curve.H, curve.GetSeed());
        var point = curve.Curve.DecodePoint(Convert.FromHexString(POIValueJSON.Required(json, "publicKey")));
        return new(POIValueJSON.Text(json, "name"), RootCAProtocol.Parse(POIValueJSON.Required(json, "protocol")),
                   new ECPublicKeyParameters(point, parameters),
                   (InfrastructureJson.Date(json, "notBefore") ?? throw new ArgumentException("notBefore: required.")).UtcDateTime,
                   (InfrastructureJson.Date(json, "notAfter") ?? throw new ArgumentException("notAfter: required.")).UtcDateTime,
                   algorithm, POIValueJSON.Text(json, "comment"));
    }
}

public partial class PublicKey
{
    /// <summary>
    /// Reconstruct an asymmetric public key without guessing its algorithm or encoding.
    /// </summary>
    public static PublicKey Parse(JObject json)
    {
        var encoding = InfrastructureJson.Text(json, "encoding") is { } format ? CryptoEncoding.Parse(format) : CryptoEncoding.BASE64;
        var text = POIValueJSON.Required(json, "value");
        Byte[] bytes;
        if (encoding == CryptoEncoding.HEX) bytes = Convert.FromHexString(text);
        else if (encoding == CryptoEncoding.BASE32)
        {
            if (!text.TryParseBASE32(out var decoded, out var error)) throw new ArgumentException(error);
            bytes = decoded;
        }
        else if (encoding == CryptoEncoding.BASE64 || encoding == CryptoEncoding.NONE) bytes = Convert.FromBase64String(text);
        else throw new ArgumentException("encoding: unsupported public key encoding.");
        return new(bytes,
                   InfrastructureJson.Text(json, "algorithm") is { } algorithm ? CryptoAlgorithm.Parse(algorithm) : null,
                   InfrastructureJson.Text(json, "serialization") is { } serialization ? CryptoSerialization.Parse(serialization) : CryptoSerialization.RAW,
                   encoding,
                   json["customData"] is JObject custom ? WWCP.CustomData.Parse(custom) : null);
    }

    /// <summary>
    /// Try to reconstruct a detached public key.
    /// </summary>
    public static Boolean TryParse(JObject json, [NotNullWhen(true)] out PublicKey? value, [NotNullWhen(false)] out String? error)
    {
        try { value = Parse(json); error = null; return true; }
        catch (Exception exception) { value = null; error = exception.Message; return false; }
    }
}

internal static class POIValueJSON
{
    internal static String Required(JObject json, String field)
        => InfrastructureJson.Text(json, field) is { Length: > 0 } text ? text : throw new ArgumentException($"{field}: required.");

    internal static I18NString Text(JObject json, String field)
        => InfrastructureJson.Name(json, field) ?? I18NString.Empty;
}

public partial class AuthenticationModes
{
    /// <summary>
    /// Create an immutable authentication mode from a type label using the domain parser.
    /// </summary>
    public static AuthenticationModes FromType(String type)
        => Parse(new JObject(new JProperty("type", type)));

    /// <summary>
    /// Parse an immutable authentication mode from CBOR.
    /// </summary>
    public static AuthenticationModes ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => Parse(json));
}

public partial class ECCPublicKey
{
    /// <summary>
    /// Parse an elliptic-curve public key from CBOR and retain its parsed curve point.
    /// </summary>
    public new static ECCPublicKey ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => Parse(json));
}
