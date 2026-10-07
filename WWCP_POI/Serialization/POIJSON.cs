using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Aegir;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Explicit schema adapters for complete POI values, including all owned children.
/// Ancestors and group members are references and never expanded recursively.
/// </summary>
internal static class POIJSON
{
    internal static JObject Document(IImmutablePOI value)
    {
        var json = value switch
        {
            RoamingNetwork network => Network(network),
            RoamingNetworkDataSnapshot snapshot => Network(RoamingNetwork.Parse(snapshot.ToJSON())),
            ChargingStationOperator entity => Operator(entity),
            ChargingPool entity => Pool(entity),
            ChargingStation entity => Station(entity),
            EVSE entity => EVSEDocument(entity),
            EMobilityProvider entity => InfrastructureJson.SnapshotMetadata(entity.ToJSON(ExpandBrandIds: InfoStatus.Expanded,
                                                ExpandDataLicenses: InfoStatus.Expanded)!, entity),
            ChargingTariff entity => InfrastructureJson.SnapshotMetadata(entity.ToJSON(ExpandBrandIds: InfoStatus.Expanded,
                                                ExpandDataLicenses: InfoStatus.Expanded), entity),
            ChargingConnector entity => Connector(entity),
            ChargingCable entity => entity.ToJSON(),
            EnergyMeter entity => InfrastructureJson.SnapshotMetadata(entity.ToJSON(), entity),
            GridOperator entity => InfrastructureJson.SnapshotMetadata(entity.ToJSON(), entity),
            GridConnectionPoint entity => GridPoint(entity),
            ChargingStationManufacturer entity => entity.ToJSON(),
            TransparencySoftware entity => entity.ToJSON(),
            TransparencySoftwareStatus entity => entity.ToJSON(),
            Brand entity => entity.ToJSON(ExpandDataLicenses: InfoStatus.Expanded),
            EnergyMix entity => entity.ToJSON(),
            ChargingProduct entity => entity.ToJSON(),
            ChargingTariffRestriction entity => entity.ToJSON(),
            ChargingTariffElement entity => entity.ToJSON(),
            ChargingPriceComponent entity => entity.ToJSON(),
            AdditionalGeoLocation entity => entity.ToJSON(),
            Image entity => entity.ToJSON(),
            AuthenticationModes entity => entity.ToJSON(),
            ImmutableI18NString entity => entity.ToJSON(),
            ImmutableOpeningTimes entity => entity.ToJSON(),
            ImmutableCryptoKeyInfo entity => entity.ToJSON(),
            ECCPublicKey entity => ((PublicKey) entity).ToJSON(),
            PublicKey entity => entity.ToJSON(),
            EVSEGroup entity => Group(entity, entity.Operator, "EVSEIds", entity.EVSEIds.Select(id => id.ToString()),
                                       entity.AllowedMemberIds.Select(id => id.ToString()), entity.Brand, entity.Priority, entity.Tariff, entity.DataLicenses),
            ChargingStationGroup entity => Group(entity, entity.Operator, "chargingStationIds", entity.ChargingStationIds.Select(id => id.ToString()),
                                                  entity.AllowedMemberIds.Select(id => id.ToString()), entity.Brand, entity.Priority, entity.Tariff, entity.DataLicenses),
            ChargingPoolGroup entity => Group(entity, entity.Operator, "chargingPoolIds", entity.ChargingPoolIds.Select(id => id.ToString()),
                                               entity.AllowedMemberIds.Select(id => id.ToString()), entity.Brand, entity.Priority, entity.Tariff, entity.DataLicenses),
            ChargingTariffGroup entity => entity.ToJSON(),
            ParkingOperator entity => Parking(entity),
            ParkingGarage entity => ParkingNode(entity, entity.OSM_WayId, entity.Geometry, entity.ChargingStations),
            ParkingSpace entity => ParkingNode(entity, entity.OSM_WayId, entity.Geometry, entity.ChargingStations, entity.Sensors),
            ParkingSensor entity => ParkingNode(entity, entity.OSM_WayId, entity.Geometry, entity.ChargingStations),
            ParkingSpaceGroup entity => ParkingNode(entity, entity.OSM_WayId, entity.Geometry, entity.ChargingStations, entity.Sensors),
            ParkingProduct entity => new JObject(new JProperty("@id", entity.Id.ToString()),
                                                  entity.MinDuration is { } min ? new JProperty("minDuration", MetrologyJson.DurationText(min)) : null,
                                                  entity.StopParkingAfterTime is { } stop ? new JProperty("stopParkingAfterTime", MetrologyJson.DurationText(stop)) : null),
            EVRoamingPartnerInfo entity => new JObject(new JProperty("eMobilityProviderId", entity.EMPId.ToString()),
                                                        new JProperty("name", entity.Name.ToJSON()),
                                                        new JProperty("comment", entity.Comment.ToJSON()),
                                                        entity.NotBefore is { } start ? new JProperty("notBefore", start.ToUniversalTime().ToString("O")) : null,
                                                        entity.NotAfter is { } end ? new JProperty("notAfter", end.ToUniversalTime().ToString("O")) : null),
            RootCAInfo entity => new JObject(new JProperty("name", entity.Name.ToJSON()),
                                             new JProperty("protocol", entity.Protocol.ToString()),
                                             new JProperty("publicKey", Convert.ToHexStringLower(entity.PublicKey.Q.GetEncoded(false))),
                                             new JProperty("algorithm", entity.Algorithm),
                                             new JProperty("notBefore", entity.NotBefore.ToUniversalTime().ToString("O")),
                                             new JProperty("notAfter", entity.NotAfter.ToUniversalTime().ToString("O")),
                                             new JProperty("comment", entity.Comment.ToJSON())),
            _ => throw new ArgumentException($"Unsupported immutable POI type '{value.GetType().FullName}'.", nameof(value))
        };
        if (json["exception"] is not null)
            throw new InvalidOperationException("POI serialization failed: " + json["exception"]);
        var brands = value switch {
            ChargingStationOperator op => op.Brands,
            ChargingPool pool => pool.Brands,
            ChargingStation station => station.Brands,
            EVSE evse => evse.Brands,
            _ => default
        };
        if (!brands.IsDefaultOrEmpty)
            json[value is EVSE ? "brand" : "brands"] = Children(brands);
        return json;
    }

    private static JObject Connector(ChargingConnector entity)
    {
        var json = entity.ToJSON();
        if (entity.ChargingCable is { } cable) json["cable"] = Document(cable);
        return json;
    }

    private static JObject EVSEDocument(EVSE entity)
    {
        var json = InfrastructureJson.SnapshotMetadata(entity.ToJSON(ExpandBrandIds: InfoStatus.Expanded,
                                                                  ExpandDataLicenses: InfoStatus.Expanded)!, entity);
        json["socketOutlets"] = Children(entity.ChargingConnectors);
        if (entity.EnergyMeter is { } meter) json["energyMeter"] = Document(meter);
        return json;
    }

    private static JObject GridPoint(GridConnectionPoint entity)
    {
        var json = entity.ToJSON();
        json["gridOperator"] = Document(entity.GridOperator);
        if (entity.EnergyMeter is { } meter) json["energyMeter"] = Document(meter);
        return json;
    }

    private static JObject Network(RoamingNetwork entity)
    {
        var json = entity.GetCapturedSnapshotDocument() ?? entity.ToJSON(
            ExpandChargingStationOperatorIds: InfoStatus.Hidden,
            ExpandChargingPoolIds: InfoStatus.Hidden,
            ExpandChargingStationIds: InfoStatus.Hidden,
            ExpandEVSEIds: InfoStatus.Hidden,
            ExpandBrandIds: InfoStatus.Expanded,
            ExpandDataLicenses: InfoStatus.Expanded,
            ExpandEMobilityProviderId: InfoStatus.Hidden);
        InfrastructureJson.SnapshotMetadata(json, entity);
        json.Remove("chargingStationOperatorIds");
        json.Remove("eMobilityProviderIds");
        // Membership is static. Runtime Removed statuses must not filter the content being hashed.
        json["chargingStationOperators"] = Children(entity.ChargingStationOperators);
        json["eMobilityProviders"] = Children(entity.EMobilityProviders);
        json["gridOperators"] = Children(entity.GridOperators);
        json["parkingOperators"] = Children(entity.ParkingOperators);
        json["chargingStationManufacturers"] = Children(entity.ChargingStationManufacturers);
        return json;
    }

    private static JObject Operator(ChargingStationOperator entity)
    {
        var json = InfrastructureJson.SnapshotMetadata(entity.ToJSON(ExpandChargingPoolIds: InfoStatus.Hidden,
                                    ExpandChargingStationIds: InfoStatus.Hidden, ExpandEVSEIds: InfoStatus.Hidden,
                                    ExpandChargingTariffIds: InfoStatus.Hidden, ExpandBrandIds: InfoStatus.Expanded,
                                    ExpandDataLicenses: InfoStatus.Expanded), entity);
        json.Remove("chargingPoolIds");
        json.Remove("chargingTariffIds");
        json["chargingPools"] = Children(entity.ChargingPools);
        json["chargingTariffs"] = Children(entity.ChargingTariffs);
        json["EVSEGroups"] = Children(entity.EVSEGroups);
        json["chargingStationGroups"] = Children(entity.ChargingStationGroups);
        json["chargingPoolGroups"] = Children(entity.ChargingPoolGroups);
        json["chargingTariffGroups"] = Children(entity.ChargingTariffGroups);
        return json;
    }

    private static JObject Pool(ChargingPool entity)
    {
        var json = InfrastructureJson.SnapshotMetadata(entity.ToJSON(ExpandChargingStationIds: InfoStatus.Hidden,
                           ExpandEVSEIds: InfoStatus.Hidden, ExpandBrandIds: InfoStatus.Expanded, ExpandDataLicenses: InfoStatus.Expanded), entity);
        json.Remove("chargingStationIds");
        json["chargingStations"] = Children(entity.ChargingStations);
        json["energyMeters"] = Children(entity.EnergyMeters);
        if (entity.GridConnectionPoint is { } point) json["gridConnectionPoint"] = Document(point);
        return json;
    }

    private static JObject Station(ChargingStation entity)
    {
        var json = InfrastructureJson.SnapshotMetadata(entity.ToJSON(ExpandEVSEIds: InfoStatus.Hidden,
                           ExpandBrandIds: InfoStatus.Expanded, ExpandDataLicenses: InfoStatus.Expanded), entity);
        json.Remove("EVSEIds");
        json["EVSEs"] = Children(entity.EVSEs);
        json["energyMeters"] = Children(entity.EnergyMeters);
        return json;
    }

    internal static JArray Children<T>(IEnumerable<T> values) where T : IImmutablePOI
        => new(values.Select(value => Document(value)).OrderBy(json => (json["@id"] ?? json["id"])?.Value<String>(), StringComparer.Ordinal));

    private static JObject Group<TId, TAdmin, TStatus>(AImmutableEMobilityEntity<TId, TAdmin, TStatus> entity,
                    ChargingStationOperator op, String memberField, IEnumerable<String> memberIds,
                    IEnumerable<String> allowedIds, Brand? brand, Priority? priority, ChargingTariff? tariff, IEnumerable<DataLicense> licenses)
        where TId : IId where TAdmin : IComparable where TStatus : IComparable
    {
        var json = InfrastructureJson.SnapshotMetadata(new JObject(new JProperty("@id", entity.Id.ToString()),
            new JProperty("@context", "https://open.charging.cloud/contexts/wwcp+json/" + entity.GetType().Name),
            new JProperty("name", entity.Name.ToJSON()), new JProperty("description", entity.Description.ToJSON()),
            new JProperty("chargingStationOperatorId", op.Id.ToString()),
            new JProperty(memberField, new JArray(memberIds.Order(StringComparer.Ordinal)))), entity);
        json["allowedMemberIds"] = new JArray(allowedIds.Order(StringComparer.Ordinal));
        json["dataLicenses"] = new JArray(licenses.Select(license => license.ToJSON()).OrderBy(json => CanonicalJSON.Serialize(json), StringComparer.Ordinal));
        if (brand is not null) json["brand"] = Document(brand);
        if (priority is { } rank) json["priority"] = Int32.Parse(rank.ToString(), System.Globalization.CultureInfo.InvariantCulture);
        if (tariff is not null) json["tariffId"] = tariff.Id.ToString();
        return json;
    }

    private static JObject ParkingNode<TId, TAdmin, TStatus>(AImmutableEMobilityEntity<TId, TAdmin, TStatus> entity,
                             String? osmId, IEnumerable<GeoCoordinate> geometry, IEnumerable<ChargingStation> stations,
                             IEnumerable<String>? sensors = null)
        where TId : IId where TAdmin : IComparable where TStatus : IComparable
    {
        var json = InfrastructureJson.SnapshotMetadata(new JObject(new JProperty("@id", entity.Id.ToString()),
                                     new JProperty("name", entity.Name.ToJSON()), new JProperty("description", entity.Description.ToJSON()),
                                     new JProperty("geometry", new JArray(geometry.Select(point => InfrastructureJson.LocationJSON(point, true)))),
                                     new JProperty("chargingStationIds", new JArray(stations.Select(station => station.Id.ToString()).Order(StringComparer.Ordinal)))), entity);
        if (osmId is not null) json["osmWayId"] = osmId;
        if (sensors is not null) json["sensors"] = new JArray(sensors.Order(StringComparer.Ordinal));
        return json;
    }

    private static JObject Parking(ParkingOperator entity)
    {
        var json = InfrastructureJson.SnapshotMetadata(entity.ToJSON(), entity);
        json["parkingGarages"] = Children(entity.ParkingGarages);
        json["parkingSpaces"] = Children(entity.ParkingSpaces);
        json["parkingSensors"] = Children(entity.ParkingSensors);
        json["parkingSpaceGroups"] = Children(entity.ParkingSpaceGroups);
        json["invalidParkingSpaceIds"] = new JArray(entity.InvalidParkingSpaceIds.Select(id => id.ToString()).Order(StringComparer.Ordinal));
        json["localParkingSpaceIds"] = new JArray(entity.LocalParkingSpaceIds.Select(id => id.ToString()).Order(StringComparer.Ordinal));
        if (entity.Address is { } address) json["address"] = address.ToJSON(Embedded: true);
        if (!entity.GeoLocation.Equals(default(GeoCoordinate))) json["geoLocation"] = InfrastructureJson.LocationJSON(entity.GeoLocation, true);
        if (entity.Telephone is not null) json["telephone"] = entity.Telephone;
        if (entity.EMailAddress is not null) json["eMailAddress"] = entity.EMailAddress;
        if (entity.Homepage is not null) json["homepage"] = entity.Homepage;
        if (entity.HotlinePhoneNumber is not null) json["hotlinePhoneNumber"] = entity.HotlinePhoneNumber;
        return json;
    }
}
