using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Aegir;

namespace cloud.charging.open.protocols.WWCP.POI;

public partial class EVSEGroup
{
    /// <summary>
    /// Reconstruct immutable group configuration and resolve membership in the supplied operator.
    /// </summary>
    public static EVSEGroup Parse(JObject json, ChargingStationOperator op)
    {
        InfrastructureJson.Parent(json, "chargingStationOperator", op.Id.ToString());
        var members = POIReferenceJSON.Resolve(json, "EVSEIds", text => EVSE_Id.Parse(text), op.EVSEs, value => value.Id);
        var allowed = InfrastructureJson.Array(json, "allowedMemberIds", token => EVSE_Id.Parse(POIReferenceJSON.Id(token)));
        var result = new EVSEGroup(EVSEGroup_Id.Parse(POIValueJSON.Required(json, "@id")), op,
             POIValueJSON.Text(json, "name"), POIValueJSON.Text(json, "description"),
             InfrastructureJson.Object(json, "brand") is { } brand ? Brand.Parse(brand) : null,
             POIReferenceJSON.Priority(json), POIReferenceJSON.Tariff(json, op),
             InfrastructureJson.Licenses(json), Members: members, MemberIds: allowed);
        InfrastructureJson.RestoreMetadata(json, result, EVSEGroupAdminStatusTypes.TryParse, EVSEGroupStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Parse a CBOR group representation and validate its content identifiers.
    /// </summary>
    public static EVSEGroup ParseCBOR(ReadOnlySpan<Byte> data, ChargingStationOperator op)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, op));
}

public partial class ChargingStationGroup
{
    /// <summary>
    /// Reconstruct immutable group configuration and resolve membership in the supplied operator.
    /// </summary>
    public static ChargingStationGroup Parse(JObject json, ChargingStationOperator op)
    {
        InfrastructureJson.Parent(json, "chargingStationOperator", op.Id.ToString());
        var members = POIReferenceJSON.Resolve(json, "chargingStationIds", ChargingStation_Id.Parse, op.ChargingStations, value => value.Id);
        var allowed = InfrastructureJson.Array(json, "allowedMemberIds", token => ChargingStation_Id.Parse(POIReferenceJSON.Id(token)));
        var result = new ChargingStationGroup(ChargingStationGroup_Id.Parse(POIValueJSON.Required(json, "@id")), op,
             POIValueJSON.Text(json, "name"), POIValueJSON.Text(json, "description"),
             InfrastructureJson.Object(json, "brand") is { } brand ? Brand.Parse(brand) : null,
             POIReferenceJSON.Priority(json), POIReferenceJSON.Tariff(json, op),
             InfrastructureJson.Licenses(json), Members: members, MemberIds: allowed);
        InfrastructureJson.RestoreMetadata(json, result, ChargingStationGroupAdminStatusTypes.TryParse, ChargingStationGroupStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Parse a CBOR group representation and validate its content identifiers.
    /// </summary>
    public static ChargingStationGroup ParseCBOR(ReadOnlySpan<Byte> data, ChargingStationOperator op)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, op));
}

public partial class ChargingPoolGroup
{
    /// <summary>
    /// Reconstruct immutable group configuration and resolve membership in the supplied operator.
    /// </summary>
    public static ChargingPoolGroup Parse(JObject json, ChargingStationOperator op)
    {
        InfrastructureJson.Parent(json, "chargingStationOperator", op.Id.ToString());
        var members = POIReferenceJSON.Resolve(json, "chargingPoolIds", ChargingPool_Id.Parse, op.ChargingPools, value => value.Id);
        var allowed = InfrastructureJson.Array(json, "allowedMemberIds", token => ChargingPool_Id.Parse(POIReferenceJSON.Id(token)));
        var result = new ChargingPoolGroup(ChargingPoolGroup_Id.Parse(POIValueJSON.Required(json, "@id")), op,
             POIValueJSON.Text(json, "name"), POIValueJSON.Text(json, "description"),
             InfrastructureJson.Object(json, "brand") is { } brand ? Brand.Parse(brand) : null,
             POIReferenceJSON.Priority(json), POIReferenceJSON.Tariff(json, op),
             InfrastructureJson.Licenses(json), Members: members, MemberIds: allowed);
        InfrastructureJson.RestoreMetadata(json, result, ChargingPoolGroupAdminStatusTypes.TryParse, ChargingPoolGroupStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Parse a CBOR group representation and validate its content identifiers.
    /// </summary>
    public static ChargingPoolGroup ParseCBOR(ReadOnlySpan<Byte> data, ChargingStationOperator op)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, op));
}

public partial class ChargingTariffGroup
{
    /// <summary>
    /// Reconstruct immutable tariff-group membership in the supplied operator.
    /// </summary>
    public static ChargingTariffGroup Parse(JObject json, ChargingStationOperator op)
    {
        InfrastructureJson.Parent(json, "chargingStationOperator", op.Id.ToString());
        var result = new ChargingTariffGroup(ChargingTariffGroup_Id.Parse(POIValueJSON.Required(json, "@id")), op,
            POIValueJSON.Text(json, "description"), POIReferenceJSON.Resolve(json, "chargingTariffIds", ChargingTariff_Id.Parse,
            op.ChargingTariffs, value => value.Id));
        InfrastructureJson.RestoreMetadata(json, result, ChargingTariffGroupAdminStatusTypes.TryParse, ChargingTariffGroupStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Parse a CBOR tariff group and validate its content identifiers.
    /// </summary>
    public static ChargingTariffGroup ParseCBOR(ReadOnlySpan<Byte> data, ChargingStationOperator op)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, op));
}

public partial class ParkingGarage
{
    /// <summary>
    /// Reconstruct immutable parking metadata; referenced charging stations must be supplied explicitly.
    /// </summary>
    public static ParkingGarage Parse(JObject json, IEnumerable<ChargingStation>? stations = null)
    {
        var result = new ParkingGarage(ParkingGarage_Id.Parse(POIValueJSON.Required(json, "@id")),
             POIValueJSON.Text(json, "name"), POIValueJSON.Text(json, "description"),
             InfrastructureJson.Text(json, "osmWayId"),
             InfrastructureJson.Array(json, "geometry", token => InfrastructureJson.Location(new JObject(new JProperty("geoLocation", token.DeepClone()))) ??
                 throw new ArgumentException("geometry: invalid coordinate.")),
             POIReferenceJSON.Resolve(json, "chargingStationIds", ChargingStation_Id.Parse, stations ?? [], value => value.Id));
        InfrastructureJson.RestoreMetadata(json, result, ParkingGarageAdminStatusTypes.TryParse, ParkingGarageStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Serialize immutable parking metadata and its content identifiers.
    /// </summary>
    public JObject ToJSON() => this.ToJSONWithETags(IncludeRuntime: true);

    /// <summary>
    /// Parse a CBOR parking representation and resolve its station references.
    /// </summary>
    public static ParkingGarage ParseCBOR(ReadOnlySpan<Byte> data, IEnumerable<ChargingStation>? stations = null)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, stations));
}

public partial class ParkingSpace
{
    /// <summary>
    /// Reconstruct immutable parking metadata; referenced charging stations must be supplied explicitly.
    /// </summary>
    public static ParkingSpace Parse(JObject json, IEnumerable<ChargingStation>? stations = null)
    {
        var result = new ParkingSpace(ParkingSpace_Id.Parse(POIValueJSON.Required(json, "@id")),
             POIValueJSON.Text(json, "name"), POIValueJSON.Text(json, "description"),
             InfrastructureJson.Text(json, "osmWayId"),
             InfrastructureJson.Array(json, "geometry", token => InfrastructureJson.Location(new JObject(new JProperty("geoLocation", token.DeepClone()))) ??
                 throw new ArgumentException("geometry: invalid coordinate.")),
             POIReferenceJSON.Resolve(json, "chargingStationIds", ChargingStation_Id.Parse, stations ?? [], value => value.Id),
             InfrastructureJson.Array(json, "sensors", POIReferenceJSON.Id));
        InfrastructureJson.RestoreMetadata(json, result, ParkingSpaceAdminStatusTypes.TryParse, ParkingSpaceStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Serialize immutable parking metadata and its content identifiers.
    /// </summary>
    public JObject ToJSON() => this.ToJSONWithETags(IncludeRuntime: true);

    /// <summary>
    /// Parse a CBOR parking representation and resolve its station references.
    /// </summary>
    public static ParkingSpace ParseCBOR(ReadOnlySpan<Byte> data, IEnumerable<ChargingStation>? stations = null)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, stations));
}

public partial class ParkingSensor
{
    /// <summary>
    /// Reconstruct immutable parking metadata; referenced charging stations must be supplied explicitly.
    /// </summary>
    public static ParkingSensor Parse(JObject json, IEnumerable<ChargingStation>? stations = null)
    {
        var result = new ParkingSensor(ParkingSensor_Id.Parse(POIValueJSON.Required(json, "@id")),
             POIValueJSON.Text(json, "name"), POIValueJSON.Text(json, "description"),
             InfrastructureJson.Text(json, "osmWayId"),
             InfrastructureJson.Array(json, "geometry", token => InfrastructureJson.Location(new JObject(new JProperty("geoLocation", token.DeepClone()))) ??
                 throw new ArgumentException("geometry: invalid coordinate.")),
             POIReferenceJSON.Resolve(json, "chargingStationIds", ChargingStation_Id.Parse, stations ?? [], value => value.Id));
        InfrastructureJson.RestoreMetadata(json, result, ParkingSensorAdminStatusTypes.TryParse, ParkingSensorStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Serialize immutable parking metadata and its content identifiers.
    /// </summary>
    public JObject ToJSON() => this.ToJSONWithETags(IncludeRuntime: true);

    /// <summary>
    /// Parse a CBOR parking representation and resolve its station references.
    /// </summary>
    public static ParkingSensor ParseCBOR(ReadOnlySpan<Byte> data, IEnumerable<ChargingStation>? stations = null)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, stations));
}

public partial class ParkingSpaceGroup
{
    /// <summary>
    /// Reconstruct immutable parking metadata; referenced charging stations must be supplied explicitly.
    /// </summary>
    public static ParkingSpaceGroup Parse(JObject json, IEnumerable<ChargingStation>? stations = null)
    {
        var result = new ParkingSpaceGroup(ParkingSpaceGroup_Id.Parse(POIValueJSON.Required(json, "@id")),
             POIValueJSON.Text(json, "name"), POIValueJSON.Text(json, "description"),
             InfrastructureJson.Text(json, "osmWayId"),
             InfrastructureJson.Array(json, "geometry", token => InfrastructureJson.Location(new JObject(new JProperty("geoLocation", token.DeepClone()))) ??
                 throw new ArgumentException("geometry: invalid coordinate.")),
             POIReferenceJSON.Resolve(json, "chargingStationIds", ChargingStation_Id.Parse, stations ?? [], value => value.Id),
             InfrastructureJson.Array(json, "sensors", POIReferenceJSON.Id));
        InfrastructureJson.RestoreMetadata(json, result, ParkingSpaceGroupAdminStatusTypes.TryParse, ParkingSpaceGroupStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Serialize immutable parking metadata and its content identifiers.
    /// </summary>
    public JObject ToJSON() => this.ToJSONWithETags(IncludeRuntime: true);

    /// <summary>
    /// Parse a CBOR parking representation and resolve its station references.
    /// </summary>
    public static ParkingSpaceGroup ParseCBOR(ReadOnlySpan<Byte> data, IEnumerable<ChargingStation>? stations = null)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, stations));
}

internal static class POIReferenceJSON
{
    internal static String Id(JToken token)
        => token.Type == JTokenType.String ? token.Value<String>()! : throw new ArgumentException("Expected an identifier string.");

    internal static ImmutableArray<T> Resolve<TId, T>(JObject json, String field, Func<String, TId> parse,
                                                     IEnumerable<T> candidates, Func<T, TId> key) where TId : notnull
    {
        var ids = InfrastructureJson.Array(json, field, token => parse(Id(token)));
        InfrastructureJson.Unique(ids, id => id, field);
        var available = candidates.ToArray();
        return ids.Select(id => available.SingleOrDefault(value => EqualityComparer<TId>.Default.Equals(id, key(value))) ??
                throw new ArgumentException($"{field}: unresolved identifier '{id}'.")).ToImmutableArray();
    }

    internal static Priority? Priority(JObject json)
        => json["priority"] is null ? null : json["priority"]!.Type == JTokenType.Integer ? new(json["priority"]!.Value<Int32>()) :
               throw new ArgumentException("priority: expected an integer.");

    internal static ChargingTariff? Tariff(JObject json, ChargingStationOperator op)
        => InfrastructureJson.Text(json, "tariffId") is { } text ?
           op.ChargingTariffs.SingleOrDefault(value => value.Id == ChargingTariff_Id.Parse(text)) ??
               throw new ArgumentException("tariffId: unresolved tariff.") : null;
}
