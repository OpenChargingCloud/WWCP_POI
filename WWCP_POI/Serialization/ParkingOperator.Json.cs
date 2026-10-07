using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public partial class ParkingOperator
{
    /// <summary>
    /// Reconstruct immutable parking-operator metadata and garages without registering them.
    /// </summary>
    public static ParkingOperator Parse(JObject json, RoamingNetwork network, IEnumerable<ChargingStation>? stations = null)
    {
        InfrastructureJson.Parent(json, "roamingNetwork", network.Id.ToString());
        var available = (stations ?? network.ChargingStationOperators.SelectMany(op => op.ChargingStations)).ToArray();
        var result = new ParkingOperator(ParkingOperator_Id.Parse(POIValueJSON.Required(json, "id")), network,
             POIValueJSON.Text(json, "name"), POIValueJSON.Text(json, "description"),
             DataSource: InfrastructureJson.Text(json, "dataSource"),
             Created: InfrastructureJson.Date(json, "created"), LastChange: InfrastructureJson.Date(json, "lastChange"),
             CustomData: InfrastructureJson.CustomData(json),
             Logo: InfrastructureJson.Array(json, "logos", token => POIValueJSON.Required(InfrastructureJson.Entry(token), "uri")).SingleOrDefault(),
             Address: InfrastructureJson.Address(json), GeoLocation: InfrastructureJson.Location(json),
             Telephone: InfrastructureJson.Text(json, "telephone"), EMailAddress: InfrastructureJson.Text(json, "eMailAddress"),
             Homepage: InfrastructureJson.Text(json, "homepage"), HotlinePhoneNumber: InfrastructureJson.Text(json, "hotlinePhoneNumber"),
             DataLicenses: InfrastructureJson.Licenses(json),
             ParkingGarages: InfrastructureJson.Array(json, "parkingGarages", token => ParkingGarage.Parse(InfrastructureJson.Entry(token), available)),
             InvalidParkingSpaceIds: InfrastructureJson.Array(json, "invalidParkingSpaceIds", token => ParkingSpace_Id.Parse(POIReferenceJSON.Id(token))),
             LocalParkingSpaceIds: InfrastructureJson.Array(json, "localParkingSpaceIds", token => ParkingSpace_Id.Parse(POIReferenceJSON.Id(token))),
             ParkingSpaces: InfrastructureJson.Array(json, "parkingSpaces", token => ParkingSpace.Parse(InfrastructureJson.Entry(token), available)),
             ParkingSensors: InfrastructureJson.Array(json, "parkingSensors", token => ParkingSensor.Parse(InfrastructureJson.Entry(token), available)),
             ParkingSpaceGroups: InfrastructureJson.Array(json, "parkingSpaceGroups", token => ParkingSpaceGroup.Parse(InfrastructureJson.Entry(token), available)));
        InfrastructureJson.RestoreMetadata(json, result, ParkingOperatorAdminStatusTypes.TryParse, ParkingOperatorStatusTypes.TryParse);
        return result;
    }

    /// <summary>
    /// Parse a CBOR parking operator and resolve its garage station references.
    /// </summary>
    public static ParkingOperator ParseCBOR(ReadOnlySpan<Byte> data, RoamingNetwork network, IEnumerable<ChargingStation>? stations = null)
        => POIRepresentation.ParseCBOR(data, json => Parse(json, network, stations));
}

public partial class RoamingNetworkDataSnapshot
{
    /// <summary>
    /// Reconstruct a frozen data snapshot from a CBOR POI hierarchy.
    /// </summary>
    public static RoamingNetworkDataSnapshot ParseCBOR(ReadOnlySpan<Byte> data)
        => POIRepresentation.ParseCBOR(data, json => RoamingNetwork.Parse(json).DataSnapshot);
}
