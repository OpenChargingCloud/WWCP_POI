/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Hermod;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Explicit ownership, identity and editable fields for nested static POI operations.
/// Value collections without stable identities remain whole-property values.
/// </summary>
internal static class POIElementSchema
{
    internal sealed record Relation(String Kind, Boolean IsArray, String? IdField = null,
                                    Func<String, String>? Identity = null, Boolean Optional = true,
                                    Boolean IsReference = false)
    {
        internal String? ReadId(JToken token)
            => IsReference ? token.Type == JTokenType.String ? token.Value<String>() :
                                 throw new ArgumentException("Expected an identifier string.") :
               token is JObject document ? InfrastructureJson.Text(document, IdField ?? "id") :
                                 throw new ArgumentException("Expected an expanded POI object.");

        internal Boolean Matches(JToken token, String id)
            => ReadId(token) is { } actual && Identity!(actual) == Identity(id);

        internal void ValidateSelector(POIElementPathSegment segment)
        {
            if (IsArray && segment.ElementId is null)
                throw new ArgumentException($"{segment.PropertyName}: an element ID is required; array indices are not identities.");
            if (segment.ElementId is { } id)
            {
                if (Identity is null)
                    throw new ArgumentException($"{segment.PropertyName}: this value is identified only by its ownership slot.");
                _ = Identity(id);
            }
        }

        internal void ValidateValue(JToken token, POIElementPathSegment segment)
        {
            if (token.Type == JTokenType.Null)
                throw new ArgumentException("An element value cannot be null; use RemoveElement for optional values.");
            if (IsReference)
            {
                _ = Identity!(ReadId(token)!);
            }
            else
            {
                var document = token as JObject ?? throw new ArgumentException("Expected an expanded POI object.");
                ValidateDocument(document, Kind);
                if (Identity is not null && (Kind != nameof(GridConnectionPoint) || document[IdField!] is not null))
                    _ = Identity(ReadId(document) ?? throw new ArgumentException($"{IdField}: an identifier is required."));
            }
            if (segment.ElementId is { } expected && !Matches(token, expected))
                throw new ArgumentException("The element value does not match the path's identifier.");
        }
    }

    internal static Relation Child(String ownerKind, String property)
        => TryChild(ownerKind, property) ??
           throw new ArgumentException($"{ownerKind}.{property}: no addressed element relation is defined.");

    internal static Relation? TryChild(String ownerKind, String property)
        => (ownerKind, property) switch
        {
            (nameof(ChargingPool) or nameof(ChargingStation), "energyMeters") => Meter(true),
            (nameof(EVSE) or nameof(GridConnectionPoint), "energyMeter") => Meter(false),
            (nameof(ChargingPool), "gridConnectionPoint") => new(nameof(GridConnectionPoint), false, "id", Identifier),
            (nameof(GridConnectionPoint), "gridOperatorId") => Reference(InfrastructureEntityType.GridOperator, false, false),
            (nameof(ChargingConnector), "cable") => new(nameof(ChargingCable), false),
            (nameof(ChargingStationOperator) or nameof(ChargingPool) or nameof(ChargingStation), "brands") => Brand(true),
            (nameof(EVSE), "brand") => Brand(true),
            (nameof(ChargingTariff) or nameof(EVSEGroup) or nameof(ChargingStationGroup) or nameof(ChargingPoolGroup), "brand") => Brand(false),
            (nameof(EVSEGroup), "EVSEIds" or "allowedMemberIds") => Reference(InfrastructureEntityType.EVSE),
            (nameof(ChargingStationGroup), "chargingStationIds" or "allowedMemberIds") => Reference(InfrastructureEntityType.ChargingStation),
            (nameof(ChargingPoolGroup), "chargingPoolIds" or "allowedMemberIds") => Reference(InfrastructureEntityType.ChargingPool),
            (nameof(ChargingTariffGroup), "chargingTariffIds") => Reference(InfrastructureEntityType.ChargingTariff),
            (nameof(ParkingGarage) or nameof(ParkingSpace) or nameof(ParkingSensor) or nameof(ParkingSpaceGroup), "chargingStationIds") => Reference(InfrastructureEntityType.ChargingStation),
            (nameof(ParkingSpace) or nameof(ParkingSpaceGroup), "sensors") => Reference(InfrastructureEntityType.ParkingSensor),
            (nameof(ParkingSpaceGroup), "parkingSpaceIds") => Reference(InfrastructureEntityType.ParkingSpace),
            (nameof(ParkingSpace), "parkingGarageId") => Reference(InfrastructureEntityType.ParkingGarage, false),
            (nameof(ParkingGarage) or nameof(ParkingSpace) or nameof(ParkingSpaceGroup), "parkingProductIds") => Reference(InfrastructureEntityType.ParkingProduct),
            (nameof(TransparencySoftwareCertificate), "verifiedTransparencySoftwareIds" or "compatibleTransparencySoftwareIds") => Reference(InfrastructureEntityType.TransparencySoftware),
            (nameof(TransparencySoftwareCertificate), "chargingStationManufacturerId") => Reference(InfrastructureEntityType.ChargingStationManufacturer, false),
            (nameof(ParkingOperator), "localParkingSpaceIds" or "invalidParkingSpaceIds") => Reference(InfrastructureEntityType.ParkingSpace),
            (nameof(RoamingNetwork) or nameof(ChargingStationOperator) or nameof(EMobilityProvider) or
             nameof(ChargingPool) or nameof(ChargingStation) or nameof(EVSE) or nameof(Brand) or nameof(GridOperator) or nameof(ParkingOperator) or
             nameof(EVSEGroup) or nameof(ChargingStationGroup) or nameof(ChargingPoolGroup), "dataLicenses")
                => new("DataLicense", true, "@id", LicenseIdentity),
            (nameof(RoamingNetwork) or nameof(ChargingStationOperator) or nameof(EMobilityProvider) or
             nameof(ChargingPool) or nameof(ChargingStation) or nameof(EVSE), "dataLicenseIds")
                => new("DataLicenseReference", true, Identity: LicenseIdentity, IsReference: true),
            (nameof(EVSE) or nameof(ChargingConnector), "tariffIds")
                => new("TariffReference", true, Identity: id => InfrastructureChangeSchema.Identity(InfrastructureEntityType.ChargingTariff, id), IsReference: true),
            (nameof(GridConnectionPoint), "marketLocationIds" or "meteringLocationIds")
                => new("LocationReference", true, Identity: Identifier, IsReference: true),
            _ => null
        };

    private static Relation Reference(InfrastructureEntityType type, Boolean array = true, Boolean optional = true)
        => new(type + "Reference", array, Optional: optional, Identity: id => InfrastructureChangeSchema.Identity(type, id), IsReference: true);

    private static Relation Meter(Boolean array)
        => new(nameof(EnergyMeter), array, "id", id => EnergyMeter_Id.Parse(id).ToString().ToUpperInvariant());

    private static Relation Brand(Boolean array)
        => new(nameof(Brand), array, "id", id => Brand_Id.Parse(id).ToString());

    private static String LicenseIdentity(String id)
        => DataLicense_Id.Parse(id).ToString().ToUpperInvariant();

    private static String Identifier(String id)
        => !String.IsNullOrWhiteSpace(id) ? id.Trim() : throw new ArgumentException("An identifier must not be empty.");

    private static readonly IReadOnlyDictionary<String, HashSet<String>> Fields =
        new Dictionary<String, HashSet<String>>(StringComparer.Ordinal)
        {
            [nameof(EnergyMeter)] = ["id", "@context", "name", "description", "role", "manufacturer", "manufacturerURL",
                                    "model", "modelURL", "serialNumber", "hardwareVersion", "firmwareVersion", "publicKeys",
                                    "publicKeyCertificateChain", "transparencySoftware", "dataSource", "customData", "created", "lastChange"],
            [nameof(GridConnectionPoint)] = ["id", "@context", "name", "description", "gridOperatorId", "energyMeter", "address", "geoLocation",
                                             "voltageLevel", "connectionType", "nominalVoltage", "nominalFrequency", "contractedImportPower",
                                             "contractedExportPower", "contractedImportApparentPower", "contractedExportApparentPower",
                                             "connectionAgreementId", "networkLocationId", "marketLocationIds", "meteringLocationIds"],
            [nameof(ChargingCable)] = ["@context", "length", "resistance", "lossCompensationName", "lossCompensationIdentification"],
            [nameof(Brand)] = ["id", "@context", "name", "description", "logo", "homepage", "dataLicenses"],
            ["DataLicense"] = ["@id", "@context", "description", "URLs"]
        };

    internal static void Property(String kind, String property)
    {
        if (!Fields.TryGetValue(kind, out var fields) || !fields.Contains(property) ||
            property is "id" or "@id" or "@context" or "created" or "lastChange" or "roamingNetworkId")
            throw new ArgumentException($"{kind}.{property}: not an editable static element property.");
    }

    internal static void ValidateDocument(JObject document, String kind)
    {
        POIRepresentation.RequireStatic(document, kind);
        POIRepresentation.RemoveETags(document, kind);
        if (!Fields.TryGetValue(kind, out var fields))
            throw new ArgumentException($"Unsupported nested POI kind '{kind}'.");
        InfrastructureJson.ValidateFields(document, fields.ToArray());
        foreach (var property in document.Properties().ToArray())
        {
            if (TryChild(kind, property.Name) is not { } relation || property.Value.Type == JTokenType.Null) continue;
            var segment = new POIElementPathSegment(property.Name);
            if (relation.IsArray)
            {
                var array = property.Value as JArray ?? throw new ArgumentException($"{property.Name}: expected an array.");
                foreach (var item in array) relation.ValidateValue(item, segment);
            }
            else relation.ValidateValue(property.Value, segment);
        }
    }

    internal static Boolean HasManagedMetadata(String kind)
        => kind == nameof(EnergyMeter);

    internal static JToken Normalize(String kind, JToken value)
    {
        if (kind == nameof(EnergyMeter) && value is JObject meter && meter["role"] is { } role)
        {
            var copy = (JObject) meter.DeepClone();
            copy["role"] = NormalizeRole(role);
            return copy;
        }
        if (kind == nameof(GridConnectionPoint))
        {
            var normalized = MetrologyJson.NormalizeProperty(InfrastructureEntityType.ChargingPool, "gridConnectionPoint", value);
            if (normalized is JObject point && point["energyMeter"] is JObject nestedMeter)
                point["energyMeter"] = Normalize(nameof(EnergyMeter), nestedMeter);
            return normalized;
        }
        if (kind == nameof(ChargingCable) && value is JObject cable)
        {
            var normalized = (JObject) MetrologyJson.NormalizeProperty(InfrastructureEntityType.ChargingConnector, "cable", cable);
            var copy = (JObject) cable.DeepClone();
            foreach (var field in new[] { "length", "resistance" })
                if (normalized[field] is { } quantity) copy[field] = quantity.DeepClone();
            return copy;
        }
        return value;
    }

    internal static JToken NormalizeProperty(String kind, String property, JToken value)
    {
        if (kind == nameof(EnergyMeter) && property == "role") return NormalizeRole(value);
        if (value.Type == JTokenType.Null || kind is not (nameof(GridConnectionPoint) or nameof(ChargingCable))) return value;
        var document = new JObject(new JProperty(property, value.DeepClone()));
        // Cable normalization parses a complete cable; all its fields are optional.
        return ((JObject) Normalize(kind, document))[property]?.DeepClone() ?? value.DeepClone();
    }

    private static JToken NormalizeRole(JToken value)
        => value.Type == JTokenType.Null ? value :
           value.Type == JTokenType.String && !String.IsNullOrWhiteSpace(value.Value<String>())
               ? new JValue(value.Value<String>()!.Trim().ToLowerInvariant())
               : throw new ArgumentException("role: expected a nonempty string or null.");
}
