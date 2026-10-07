using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Canonical POI representations and content identifiers shared by JSON and CBOR.
/// </summary>
public static class POIRepresentation
{
    [ThreadStatic]
    private static Int32 untaggedSerializationDepth;

    /// <summary>
    /// Compute both identifiers from the complete immutable POI content.
    /// </summary>
    public static ImmutableArray<ETag> GetETags(IImmutablePOI value)
    {
        var json = Content(value);
        return Tags(json, Kind(value));
    }

    /// <summary>
    /// Serialize immutable POI content with its two content identifiers.
    /// Select HEX or Base64 digest text; independently include current statuses and revision metadata.
    /// </summary>
    public static JObject ToJSONWithETags(this IImmutablePOI value, Boolean IncludeRuntime = false,
                                          ETagDigestEncoding DigestEncoding = ETagDigestEncoding.HEX,
                                          Boolean IncludeVersionMetadata = false)
    {
        if (IncludeRuntime && value is RoamingNetworkDataSnapshot)
            throw new ArgumentException("A static DataSnapshot has no runtime state. Export a RoamingNetwork to include current statuses.", nameof(IncludeRuntime));
        var kind = Kind(value);
        var json = Document(value);
        Prepare(json, kind, IncludeRuntime, IncludeVersionMetadata);
        Visit(json, kind, "", (document, type, path) =>
        {
            if (path.Length > 0 && !TaggedKinds.Contains(type)) return;
            var staticContent = (JObject) document.DeepClone();
            Prepare(staticContent, type, false);
            document["ETags"] = TagsJSON(Tags(staticContent, type), DigestEncoding);
        });
        return json;
    }

    /// <summary>
    /// Return the exact canonical JSON bytes used for the JSON ETag, without ETags or runtime data.
    /// </summary>
    public static Byte[] ToCanonicalJSON(this IImmutablePOI value)
        => CanonicalJSON.ToUTF8Bytes(Content(value));

    /// <summary>
    /// Return the exact deterministic CBOR bytes used for the CBOR ETag, without ETags or runtime data.
    /// </summary>
    public static Byte[] ToCanonicalCBOR(this IImmutablePOI value)
        => Encode(Content(value), Kind(value));

    /// <summary>
    /// Serialize a POI document as deterministic CBOR, including its ETag array.
    /// Measurements use Styx metrological tag 44252; runtime and revision metadata are independent options.
    /// </summary>
    public static Byte[] ToCBOR(this IImmutablePOI value, Boolean IncludeRuntime = false,
                                Boolean IncludeVersionMetadata = false)
        => Encode(value.ToJSONWithETags(IncludeRuntime, IncludeVersionMetadata: IncludeVersionMetadata), Kind(value));

    /// <summary>
    /// Decode and reconstruct a POI value with the same parser and parent context as JSON.
    /// Supplied ETags must match the reconstructed immutable content.
    /// </summary>
    public static T ParseCBOR<T>(ReadOnlySpan<Byte> data, Func<JObject, T> parser)
        where T : IImmutablePOI
    {
        ArgumentNullException.ThrowIfNull(parser);
        var json = CBORJSON.ToJSON(ConvertETagCBOR(CBORValue.Parse(data), Kind(typeof(T)), false)) as JObject ??
                   throw new ArgumentException("A POI CBOR document must be a map with text property names.", nameof(data));
        return ParseJSON(json, parser);
    }

    /// <summary>
    /// Reconstruct a complete POI JSON representation and check its declared ETags.
    /// </summary>
    public static T ParseJSON<T>(JObject json, Func<JObject, T> parser) where T : IImmutablePOI
    {
        ArgumentNullException.ThrowIfNull(parser);
        json = (JObject) json.DeepClone();
        var kind = Kind(typeof(T));
        var declarations = ReadDeclarations(json, kind);
        RemoveETags(json, kind);
        var value = parser(json);
        ValidateDeclarations(declarations, Document(value), Kind(value));
        return value;
    }

    /// <summary>
    /// Try to decode a POI value and validate its content identifiers.
    /// </summary>
    public static Boolean TryParseCBOR<T>(ReadOnlySpan<Byte> data, Func<JObject, T> parser,
                                          [NotNullWhen(true)] out T? value,
                                          [NotNullWhen(false)] out String? error)
        where T : class, IImmutablePOI
    {
        try
        {
            value = ParseCBOR(data, parser);
            error = null;
            return true;
        }
        catch (Exception exception)
        {
            value = null;
            error = exception.Message;
            return false;
        }
    }

    /// <summary>
    /// Attach the immutable content identifiers to an existing JSON view.
    /// The identifiers always describe the full POI content, irrespective of expansion options.
    /// </summary>
    internal static JObject AddETags(IImmutablePOI value, JObject json)
    {
        if (untaggedSerializationDepth == 0)
            json["ETags"] = TagsJSON(value.ETags);
        return json;
    }

    private static JObject Content(IImmutablePOI value)
    {
        var json = Document(value);
        Prepare(json, Kind(value), false);
        return json;
    }

    private static ImmutableArray<ETag> Tags(JObject json, String kind)
        => [ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(json)),
            ETag.Compute(ETagFormat.CBOR, Encode(json, kind))];

    private static JArray TagsJSON(IEnumerable<ETag> tags, ETagDigestEncoding encoding = ETagDigestEncoding.HEX)
        => new(tags.Select(tag => tag.ToJSON(encoding)));

    private static Byte[] Encode(JObject json, String kind)
    {
        var paths = new HashSet<String>(StringComparer.Ordinal);
        var hasETags = false;
        Visit(json, kind, "", (document, type, path) =>
        {
            hasETags |= document["ETags"] is not null;
            foreach (var property in document.Properties())
                if (property.Value.Type == JTokenType.String && IsMeasurement(type, property.Name))
                {
                    if (!MetrologicalValue.TryParse(property.Value.Value<String>()!, out _, out var error))
                        throw new ArgumentException($"{path}/{property.Name}: cannot encode a Styx metrological value: {error}");
                    paths.Add(path + "/" + Escape(property.Name));
                }
        });
        var cbor = CBORJSON.ToCBOR(CanonicalJSON.ToUTF8Bytes(json), new CBORJSONOptions {
            DetectMetrologicalValues = (path, _) => paths.Contains(path)
        });
        return (hasETags ? ConvertETagCBOR(cbor, kind, true) : cbor).ToByteArray(CBORWriterOptions.Canonical);
    }

    // Convert only schema-owned ETag arrays. Customer fields with the same name stay untouched.
    private static CBORValue ConvertETagCBOR(CBORValue value, String kind, Boolean toBinary)
    {
        if (value.Kind != CBORValueKind.Map) return value;
        var entries = new List<KeyValuePair<CBORValue, CBORValue>>();
        foreach (var entry in value.AsMap())
        {
            var converted = entry.Value;
            if (entry.Key.TryGetText(out var field))
            {
                if (field == "ETags")
                {
                    if (converted.Kind != CBORValueKind.Array || converted.Count != 2)
                        throw new ArgumentException("ETags: expected the JSON and CBOR identifier arrays.");
                    var tags = converted.AsArray().Select(item => toBinary ? ETag.Parse(CBORJSON.ToJSON(item)) : ETag.Parse(item)).ToImmutableArray();
                    ETag.ValidatePair(tags, "ETags");
                    converted = CBORValue.FromArray(tags.Select(tag => toBinary ? tag.ToCBOR() : tag.ToTextCBOR()));
                }
                else if (ChildKind(kind, field) is { } childKind)
                    converted = converted.Kind == CBORValueKind.Array
                                    ? CBORValue.FromArray(converted.AsArray().Select(child => ConvertETagCBOR(child, childKind, toBinary)))
                                    : ConvertETagCBOR(converted, childKind, toBinary);
            }
            entries.Add(new(entry.Key, converted));
        }
        return CBORValue.FromMap(entries);
    }

    private static Boolean IsMeasurement(String kind, String field)
        => kind switch
        {
            nameof(EVSE) => field is "maxVoltage" or "maxCurrent" or "maxPower" or "maxCapacity" or
                                       "maxVoltageRealTime" or "maxCurrentRealTime" or "maxPowerRealTime" or "maxCapacityRealTime",
            nameof(ChargingStation) or nameof(ChargingPool) => field is "maxVoltage" or "maxCurrent" or "maxPower" or "maxCapacity",
            nameof(GridConnectionPoint) => field is "nominalVoltage" or "nominalFrequency" or "contractedImportPower" or
                                                  "contractedExportPower" or "contractedImportApparentPower" or "contractedExportApparentPower",
            nameof(ChargingCable) => field is "length" or "resistance",
            nameof(ChargingProduct) => field is "minPower" or "maxPower" or "minEnergy" or "stopChargingAfterEnergy" or
                                               "minDuration" or "stopChargingAfterTime",
            nameof(ParkingProduct) => field is "minDuration" or "stopParkingAfterTime",
            nameof(ChargingTariffRestriction) => field is "minEnergy" or "maxEnergy" or "minPower" or "maxPower" or "minDuration" or "maxDuration",
            nameof(ChargingPriceComponent) => field == "stepSize",
            "EnergySource" or "EnvironmentalImpact" => field == "percentage",
            "GeoLocation" or nameof(AdditionalGeoLocation) => field is "alt" or "altitude",
            _ => false
        };

    private static readonly HashSet<String> RuntimeEntityKinds = new(StringComparer.Ordinal) {
        nameof(RoamingNetwork), nameof(ChargingStationOperator), nameof(EMobilityProvider), nameof(ChargingPool),
        nameof(ChargingStation), nameof(EVSE), nameof(ChargingTariff), nameof(EnergyMeter), nameof(GridOperator),
        nameof(ParkingOperator), nameof(ParkingGarage), nameof(ParkingSpace), nameof(ParkingSensor), nameof(ParkingSpaceGroup),
        nameof(EVSEGroup), nameof(ChargingStationGroup), nameof(ChargingPoolGroup), nameof(ChargingTariffGroup)
    };

    private static readonly HashSet<String> TaggedKinds = new(RuntimeEntityKinds.Concat(new[] {
        nameof(ChargingConnector), nameof(ChargingCable), nameof(GridConnectionPoint), nameof(ChargingStationManufacturer),
        nameof(TransparencySoftware), nameof(TransparencySoftwareStatus), nameof(Brand), nameof(EnergyMix),
        nameof(ChargingProduct), nameof(ParkingProduct), nameof(ChargingTariffElement), nameof(ChargingTariffRestriction),
        nameof(ChargingPriceComponent), nameof(Image), nameof(AdditionalGeoLocation), nameof(AuthenticationModes),
        nameof(PublicKey), nameof(ImmutableCryptoKeyInfo), nameof(RootCAInfo), nameof(EVRoamingPartnerInfo)
    }), StringComparer.Ordinal);

    private static readonly String[] RuntimeFields = [
        "status", "adminStatus", "statusSchedule", "adminStatusSchedule", "lastStatusUpdate",
        "maxVoltageRealTime", "maxCurrentRealTime", "maxPowerRealTime", "maxCapacityRealTime",
        "maxVoltagePrognoses", "maxCurrentPrognoses", "maxPowerPrognoses", "maxCapacityPrognoses",
        "energyMixRealTime", "energyMixPrognoses"
    ];

    internal static Boolean IsRuntimeProperty(String kind, String field)
        => RuntimeEntityKinds.Contains(kind) && RuntimeFields.Contains(field, StringComparer.Ordinal);

    internal static void RemoveRuntime(JObject json, String kind)
        => Visit(json, kind, "", (document, type, _) =>
        {
            if (RuntimeEntityKinds.Contains(type))
                foreach (var field in RuntimeFields) document.Remove(field);
            if (type == nameof(ImmutableCryptoKeyInfo)) document.Remove("privateKey");
        });

    internal static void RequireStatic(JObject json, String kind)
        => Visit(json, kind, "", (document, type, path) =>
        {
            foreach (var property in document.Properties())
                if (IsRuntimeProperty(type, property.Name))
                    throw new ArgumentException($"{path}/{property.Name}: runtime data is not allowed in a static ChangeSet. Use runtime updates instead.");
                else if (type == nameof(ImmutableCryptoKeyInfo) && property.Name == "privateKey")
                    throw new ArgumentException($"{path}/privateKey: private keys are not static POI content.");
        });

    private static void Prepare(JObject json, String kind, Boolean includeRuntime, Boolean includeVersionMetadata = false)
        => Visit(json, kind, "", (document, type, _) =>
        {
            document.Remove("ETags");
            if (!includeRuntime && RuntimeEntityKinds.Contains(type))
            {
                foreach (var field in RuntimeFields)
                    document.Remove(field);
            }
            if (!includeVersionMetadata && type == nameof(RoamingNetwork))
            {
                document.Remove("revision");
                document.Remove("appliedChangeSetId");
            }
        });

    private static String? ChildKind(String kind, String field)
        => field switch
        {
            "chargingStationOperators" when kind == nameof(RoamingNetwork) => nameof(ChargingStationOperator),
            "gridOperators" when kind == nameof(RoamingNetwork) => nameof(GridOperator),
            "parkingOperators" when kind == nameof(RoamingNetwork) => nameof(ParkingOperator),
            "chargingStationManufacturers" when kind == nameof(RoamingNetwork) => nameof(ChargingStationManufacturer),
            "EVSEGroups" when kind == nameof(ChargingStationOperator) => nameof(EVSEGroup),
            "chargingStationGroups" when kind == nameof(ChargingStationOperator) => nameof(ChargingStationGroup),
            "chargingPoolGroups" when kind == nameof(ChargingStationOperator) => nameof(ChargingPoolGroup),
            "chargingTariffGroups" when kind == nameof(ChargingStationOperator) => nameof(ChargingTariffGroup),
            "parkingSpaces" when kind == nameof(ParkingOperator) => nameof(ParkingSpace),
            "parkingSensors" when kind == nameof(ParkingOperator) => nameof(ParkingSensor),
            "parkingSpaceGroups" when kind == nameof(ParkingOperator) => nameof(ParkingSpaceGroup),
            "eMobilityProviders" when kind == nameof(RoamingNetwork) => nameof(EMobilityProvider),
            "chargingPools" when kind is nameof(ChargingStationOperator) or nameof(ChargingPoolGroup) => nameof(ChargingPool),
            "chargingStations" when kind is nameof(ChargingPool) or nameof(ChargingStationGroup) => nameof(ChargingStation),
            "EVSEs" when kind is nameof(ChargingStation) or nameof(EVSEGroup) => nameof(EVSE),
            "socketOutlets" when kind == nameof(EVSE) => nameof(ChargingConnector),
            "chargingTariffs" when kind is nameof(ChargingStationOperator) or nameof(ChargingTariffGroup) => nameof(ChargingTariff),
            "parkingGarages" when kind == nameof(ParkingOperator) => nameof(ParkingGarage),
            "geometry" when kind is nameof(ParkingGarage) or nameof(ParkingSpace) or nameof(ParkingSensor) or nameof(ParkingSpaceGroup) => "GeoLocation",
            "energyMeters" when kind is nameof(ChargingPool) or nameof(ChargingStation) => nameof(EnergyMeter),
            "energyMeter" when kind is nameof(EVSE) or nameof(GridConnectionPoint) => nameof(EnergyMeter),
            "gridConnectionPoint" when kind == nameof(ChargingPool) => nameof(GridConnectionPoint),
            "gridOperator" when kind == nameof(GridConnectionPoint) => nameof(GridOperator),
            "transparencySoftware" when kind == nameof(EnergyMeter) => nameof(TransparencySoftwareStatus),
            "transparencySoftware" when kind == nameof(TransparencySoftwareStatus) => nameof(TransparencySoftware),
            "cable" when kind == nameof(ChargingConnector) => nameof(ChargingCable),
            "brand" or "brands" when RuntimeEntityKinds.Contains(kind) => nameof(Brand),
            "dataLicenses" when RuntimeEntityKinds.Contains(kind) || kind == nameof(Brand) => "DataLicense",
            "elements" when kind == nameof(ChargingTariff) => nameof(ChargingTariffElement),
            "priceComponents" when kind == nameof(ChargingTariffElement) => nameof(ChargingPriceComponent),
            "restrictions" when kind == nameof(ChargingTariffElement) => nameof(ChargingTariffRestriction),
            "energyMix" when RuntimeEntityKinds.Contains(kind) => nameof(EnergyMix),
            "energySources" when kind == nameof(EnergyMix) => "EnergySource",
            "environmentalImpacts" when kind == nameof(EnergyMix) => "EnvironmentalImpact",
            "geoLocation" => "GeoLocation",
            "additionalGeoLocations" => nameof(AdditionalGeoLocation),
            "relatedLocations" => nameof(AdditionalGeoLocation),
            "mobilityRootCAs" => nameof(RootCAInfo),
            "evRoamingPartners" => nameof(EVRoamingPartnerInfo),
            "address" => "Address",
            "images" => nameof(Image),
            "authenticationModes" => nameof(AuthenticationModes),
            "cryptoKeys" when kind == nameof(ChargingStationManufacturer) => nameof(ImmutableCryptoKeyInfo),
            "publicKeys" when kind == nameof(EnergyMeter) => nameof(PublicKey),
            _ => null
        };

    private static void Visit(JObject document, String kind, String path, Action<JObject, String, String> action)
    {
        action(document, kind, path);
        foreach (var property in document.Properties().ToArray())
        {
            var childKind = ChildKind(kind, property.Name);
            if (childKind is null) continue;
            var childPath = path + "/" + Escape(property.Name);
            if (property.Value is JObject child) Visit(child, childKind, childPath, action);
            if (property.Value is JArray children)
                for (var index = 0; index < children.Count; index++)
                    if (children[index] is JObject item) Visit(item, childKind, childPath + "/" + index, action);
        }
    }

    private static String Escape(String text) => text.Replace("~", "~0").Replace("/", "~1");

    private static ImmutableDictionary<String, ImmutableArray<ETag>> ReadDeclarations(JObject json, String kind)
    {
        var declarations = ImmutableDictionary.CreateBuilder<String, ImmutableArray<ETag>>(StringComparer.Ordinal);
        Visit(json, kind, "", (document, _, path) =>
        {
            if (document["ETags"] is { } tags) declarations.Add(path, ReadTags(tags));
        });
        return declarations.ToImmutable();
    }

    private static ImmutableArray<ETag> ReadTags(JToken token)
    {
        if (token is not JArray array || array.Count != 2)
            throw new ArgumentException("ETags: expected the JSON and CBOR SHA-256 identifier arrays.");
        return ETag.ValidatePair(array.Select(ETag.Parse).ToImmutableArray(), "ETags");
    }

    internal static void RemoveETags(JObject json, String kind)
        => Visit(json, kind, "", (document, _, _) => document.Remove("ETags"));

    /// <summary>
    /// Assign deterministic metadata defaults to nested POI nodes introduced by a ChangeSet.
    /// Existing timestamps and customer data remain content supplied by the caller.
    /// </summary>
    internal static void InitializeNestedMetadata(JObject json, String kind, DateTimeOffset timestamp)
        => Visit(json, kind, "", (document, type, path) =>
        {
            if (path.Length == 0 || !RuntimeEntityKinds.Contains(type)) return;
            var created = InfrastructureJson.Date(document, "created");
            var changed = InfrastructureJson.Date(document, "lastChange");
            document["created"] = (created ?? changed ?? timestamp).ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
            document["lastChange"] = (changed ?? created ?? timestamp).ToUniversalTime().ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        });

    internal static T WithoutETags<T>(Func<T> serialize)
    {
        untaggedSerializationDepth++;
        try { return serialize(); }
        finally { untaggedSerializationDepth--; }
    }

    private static void ValidateDeclarations(ImmutableDictionary<String, ImmutableArray<ETag>> declarations, JObject json, String kind)
    {
        var remaining = declarations.Keys.ToHashSet(StringComparer.Ordinal);
        Visit(json, kind, "", (document, type, path) =>
        {
            if (!declarations.TryGetValue(path, out var expected)) return;
            var content = (JObject) document.DeepClone();
            Prepare(content, type, false);
            if (!expected.SequenceEqual(Tags(content, type)))
                throw new ArgumentException($"{path}/ETags: content identifiers do not match the reconstructed POI content.");
            remaining.Remove(path);
        });
        if (remaining.Count != 0)
            throw new ArgumentException("ETags: a declared POI node is missing after reconstruction.");
    }

    private static String Kind(IImmutablePOI value)
        => value is RoamingNetworkDataSnapshot ? nameof(RoamingNetwork) :
           value is AuthenticationModes ? nameof(AuthenticationModes) :
           value is ECCPublicKey ? nameof(PublicKey) : value.GetType().Name;

    private static String Kind(Type type)
        => type == typeof(RoamingNetworkDataSnapshot) ? nameof(RoamingNetwork) :
           typeof(AuthenticationModes).IsAssignableFrom(type) ? nameof(AuthenticationModes) :
           typeof(PublicKey).IsAssignableFrom(type) ? nameof(PublicKey) : type.Name;

    private static JObject Document(IImmutablePOI value)
        => WithoutETags(() => POIJSON.Document(value));
}
