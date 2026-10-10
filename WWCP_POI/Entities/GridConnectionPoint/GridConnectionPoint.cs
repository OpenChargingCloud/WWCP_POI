﻿/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// An immutable description of a pool's public-grid connection. Its mandatory operator
/// reference and optional meter retain their own mutable runtime status schedules.
/// </summary>
public sealed partial class GridConnectionPoint
{
    public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/gridConnectionPoint";

    /// <summary>
    /// An optional connection-point identifier assigned by the grid operator.
    /// </summary>
    public String? Id { get; }
    public ImmutableI18NString Name { get; }
    public ImmutableI18NString Description { get; }
    public GridOperator GridOperator { get; }
    public GridOperator_Id GridOperatorId => GridOperator.Id;
    public EnergyMeter? EnergyMeter { get; }
    private readonly Address? address;
    public Address? Address => ImmutablePOIValues.Copy(address);
    public GeoCoordinate? GeoLocation { get; }
    public GridVoltageLevel? VoltageLevel { get; }
    public GridConnectionTypes? ConnectionType { get; }
    /// <summary>
    /// Nominal connection voltage in volts, line-to-line for a three-phase connection.
    /// </summary>
    public Volt? NominalVoltage { get; }
    /// <summary>
    /// Nominal grid frequency in hertz.
    /// </summary>
    public Hertz? NominalFrequency { get; }
    /// <summary>
    /// Agreed active power for import from the grid, in watts.
    /// </summary>
    public Watt? ContractedImportPower { get; }
    /// <summary>
    /// Agreed active power for export to the grid, in watts; zero means no export.
    /// </summary>
    public Watt? ContractedExportPower { get; }
    /// <summary>
    /// Agreed apparent power for import from the grid, in volt-amperes.
    /// </summary>
    public VoltAmpere? ContractedImportApparentPower { get; }
    /// <summary>
    /// Agreed apparent power for export to the grid, in volt-amperes.
    /// </summary>
    public VoltAmpere? ContractedExportApparentPower { get; }
    public String? ConnectionAgreementId { get; }
    public String? NetworkLocationId { get; }
    /// <summary>
    /// Associated market-location identifiers (e.g. separate import/export MaLo IDs).
    /// </summary>
    public ImmutableArray<String> MarketLocationIds { get; }
    /// <summary>
    /// Associated metering-location identifiers; these are not meter serial numbers.
    /// </summary>
    public ImmutableArray<String> MeteringLocationIds { get; }

    public GridConnectionPoint(GridOperator         GridOperator,
                               EnergyMeter?         EnergyMeter                    = null,
                               String?              Id                             = null,
                               I18NString?          Name                           = null,
                               I18NString?          Description                    = null,
                               Address?             Address                        = null,
                               GeoCoordinate?       GeoLocation                    = null,
                               GridVoltageLevel?    VoltageLevel                   = null,
                               GridConnectionTypes? ConnectionType                 = null,
                               Volt?                NominalVoltage                 = null,
                               Hertz?               NominalFrequency               = null,
                               Watt?                ContractedImportPower          = null,
                               Watt?                ContractedExportPower          = null,
                               VoltAmpere?          ContractedImportApparentPower  = null,
                               VoltAmpere?          ContractedExportApparentPower  = null,
                               String?              ConnectionAgreementId          = null,
                               String?              NetworkLocationId              = null,
                               IEnumerable<String>? MarketLocationIds              = null,
                               IEnumerable<String>? MeteringLocationIds            = null)
    {
        ArgumentNullException.ThrowIfNull(GridOperator);
        if (GridOperator.Id.IsNullOrEmpty)
            throw new ArgumentException("gridOperator: must have a non-empty ID.", nameof(GridOperator));
        if (EnergyMeter is not null && EnergyMeter.Id.IsNullOrEmpty)
            throw new ArgumentException("energyMeter: must have a non-empty ID.", nameof(EnergyMeter));
        if (VoltageLevel.HasValue && !Enum.IsDefined(VoltageLevel.Value))
            throw new ArgumentOutOfRangeException(nameof(VoltageLevel));
        if (ConnectionType.HasValue && !Enum.IsDefined(ConnectionType.Value))
            throw new ArgumentOutOfRangeException(nameof(ConnectionType));

        this.GridOperator = GridOperator;
        this.EnergyMeter = EnergyMeter?.Clone(GridOperator.RoamingNetwork);
        this.Id = Identifier(Id, "id");
        this.Name = new(Name ?? I18NString.Empty);
        this.Description = new(Description ?? I18NString.Empty);
        this.address = ImmutablePOIValues.Copy(Address);
        this.GeoLocation = GeoLocation;
        this.VoltageLevel = VoltageLevel;
        this.ConnectionType = ConnectionType;
        this.NominalVoltage = Positive(NominalVoltage, "nominalVoltage");
        this.NominalFrequency = Positive(NominalFrequency, "nominalFrequency");
        this.ContractedImportPower = NonNegative(ContractedImportPower, "contractedImportPower");
        this.ContractedExportPower = NonNegative(ContractedExportPower, "contractedExportPower");
        this.ContractedImportApparentPower = NonNegative(ContractedImportApparentPower, "contractedImportApparentPower");
        this.ContractedExportApparentPower = NonNegative(ContractedExportApparentPower, "contractedExportApparentPower");
        if (ContractedImportPower.HasValue && ContractedImportApparentPower.HasValue &&
            ContractedImportPower.Value.Value > ContractedImportApparentPower.Value.Value)
            throw new ArgumentException("contractedImportPower: must not exceed contractedImportApparentPower.");
        if (ContractedExportPower.HasValue && ContractedExportApparentPower.HasValue &&
            ContractedExportPower.Value.Value > ContractedExportApparentPower.Value.Value)
            throw new ArgumentException("contractedExportPower: must not exceed contractedExportApparentPower.");
        this.ConnectionAgreementId = Identifier(ConnectionAgreementId, "connectionAgreementId");
        this.NetworkLocationId = Identifier(NetworkLocationId, "networkLocationId");
        this.MarketLocationIds = Identifiers(MarketLocationIds, "marketLocationIds");
        this.MeteringLocationIds = Identifiers(MeteringLocationIds, "meteringLocationIds");
    }

    /// <summary>
    /// Create a detached copy, optionally binding the grid operator to another network version.
    /// </summary>
    public GridConnectionPoint Clone(RoamingNetwork? RoamingNetwork = null)
        => new((RoamingNetwork is null ? GridOperator :
               RoamingNetwork.GetGridOperator(GridOperatorId) ?? throw new ArgumentException("The target network has no referenced grid operator.")), EnergyMeter,
               Id, Name, Description, address, GeoLocation, VoltageLevel, ConnectionType,
               NominalVoltage, NominalFrequency, ContractedImportPower, ContractedExportPower,
               ContractedImportApparentPower, ContractedExportApparentPower, ConnectionAgreementId,
               NetworkLocationId, MarketLocationIds, MeteringLocationIds);

    /// <summary>
    /// Export the operator's registry reference and this connection point's owned static values.
    /// </summary>
    public JObject ToJSON(Boolean Embedded = false)
    {
        var json = new JObject(new JProperty("gridOperatorId", GridOperatorId.ToString()));
        if (!Embedded) json["@context"] = JSONLDContext;
        if (Id is not null) json["id"] = Id;
        if (Name.IsNotNullOrEmpty()) json["name"] = Name.ToJSON();
        if (Description.IsNotNullOrEmpty()) json["description"] = Description.ToJSON();
        if (EnergyMeter is not null) json["energyMeter"] = EnergyMeter.ToJSON(Embedded: true);
        if (address is not null) json["address"] = address.ToJSON(Embedded: true);
        if (GeoLocation.HasValue) json["geoLocation"] = InfrastructureJson.LocationJSON(GeoLocation.Value, true);
        if (VoltageLevel.HasValue) json["voltageLevel"] = VoltageLevel.Value.ToString();
        if (ConnectionType.HasValue) json["connectionType"] = ConnectionType.Value.ToString();
        if (NominalVoltage.HasValue) json["nominalVoltage"] = MetrologyJson.Text(NominalVoltage.Value);
        if (NominalFrequency.HasValue) json["nominalFrequency"] = MetrologyJson.Text(NominalFrequency.Value);
        if (ContractedImportPower.HasValue) json["contractedImportPower"] = MetrologyJson.Text(ContractedImportPower.Value);
        if (ContractedExportPower.HasValue) json["contractedExportPower"] = MetrologyJson.Text(ContractedExportPower.Value);
        if (ContractedImportApparentPower.HasValue) json["contractedImportApparentPower"] = MetrologyJson.Text(ContractedImportApparentPower.Value);
        if (ContractedExportApparentPower.HasValue) json["contractedExportApparentPower"] = MetrologyJson.Text(ContractedExportApparentPower.Value);
        if (ConnectionAgreementId is not null) json["connectionAgreementId"] = ConnectionAgreementId;
        if (NetworkLocationId is not null) json["networkLocationId"] = NetworkLocationId;
        if (!MarketLocationIds.IsEmpty) json["marketLocationIds"] = new JArray(MarketLocationIds);
        if (!MeteringLocationIds.IsEmpty) json["meteringLocationIds"] = new JArray(MeteringLocationIds);
        return POIRepresentation.AddETags(this, json);
    }

    public static GridConnectionPoint Parse(JObject JSON, RoamingNetwork? RoamingNetwork = null)
    {
        if (TryParse(JSON, out var point, out var error, RoamingNetwork)) return point;
        throw new ArgumentException(error, nameof(JSON));
    }

    public static Boolean TryParse(JObject JSON,
                                   [NotNullWhen(true)] out GridConnectionPoint? Point,
                                   [NotNullWhen(false)] out String? Error,
                                   RoamingNetwork? RoamingNetwork = null)
    {
        Point = null;
        Error = null;
        try
        {
            InfrastructureJson.Validate(JSON, JSONLDContext);
            if (JSON["gridOperator"] is not null) throw new ArgumentException("Use gridOperatorId and the network registry.");
            var operatorId = GridOperator_Id.Parse(TransparencyJson.RequiredText(JSON, "gridOperatorId"));
            var gridOperator = RoamingNetwork?.GetGridOperator(operatorId) ??
                               throw new ArgumentException($"gridOperatorId: unresolved operator '{operatorId}'.");
            Point = new GridConnectionPoint(
                gridOperator,
                EnergyMeter: InfrastructureJson.Object(JSON, "energyMeter") is { } meter
                                 ? InfrastructureJson.At("energyMeter", () => POI.EnergyMeter.Parse(meter, Network: RoamingNetwork)) : null,
                Id: InfrastructureJson.Text(JSON, "id"),
                Name: InfrastructureJson.Name(JSON, "name"),
                Description: InfrastructureJson.Name(JSON, "description"),
                Address: InfrastructureJson.Address(JSON), GeoLocation: InfrastructureJson.Location(JSON),
                VoltageLevel: ReadEnum<GridVoltageLevel>(JSON, "voltageLevel"),
                ConnectionType: ReadEnum<GridConnectionTypes>(JSON, "connectionType"),
                NominalVoltage: MetrologyJson.Read<Volt>(JSON, "nominalVoltage", Volt.TryParse),
                NominalFrequency: MetrologyJson.Read<Hertz>(JSON, "nominalFrequency", Hertz.TryParse),
                ContractedImportPower: MetrologyJson.Read<Watt>(JSON, "contractedImportPower", Watt.TryParse),
                ContractedExportPower: MetrologyJson.Read<Watt>(JSON, "contractedExportPower", Watt.TryParse),
                ContractedImportApparentPower: MetrologyJson.Read<VoltAmpere>(JSON, "contractedImportApparentPower", VoltAmpere.TryParse),
                ContractedExportApparentPower: MetrologyJson.Read<VoltAmpere>(JSON, "contractedExportApparentPower", VoltAmpere.TryParse),
                ConnectionAgreementId: InfrastructureJson.Text(JSON, "connectionAgreementId"),
                NetworkLocationId: InfrastructureJson.Text(JSON, "networkLocationId"),
                MarketLocationIds: ReadIdentifiers(JSON, "marketLocationIds"),
                MeteringLocationIds: ReadIdentifiers(JSON, "meteringLocationIds"));
            return true;
        }
        catch (Exception exception)
        {
            Error = $"GridConnectionPoint: {exception.Message}";
            return false;
        }
    }

    private static String? Identifier(String? value, String field)
        => value is null ? null : !String.IsNullOrWhiteSpace(value) ? value.Trim()
            : throw new ArgumentException($"{field}: must not be empty.");

    private static ImmutableArray<String> Identifiers(IEnumerable<String>? values, String field)
    {
        var result = (values ?? []).Select(value => Identifier(value, field) ??
                         throw new ArgumentException($"{field}: must not contain null.")).ToImmutableArray();
        if (result.Distinct(StringComparer.Ordinal).Count() != result.Length)
            throw new ArgumentException($"{field}: duplicate identifier.");
        return result;
    }

    private static IEnumerable<String> ReadIdentifiers(JObject json, String field)
        => InfrastructureJson.Array(json, field, token => token.Type == JTokenType.String
               ? token.Value<String>()! : throw new ArgumentException("Expected an identifier string."));

    private static T? Positive<T>(T? value, String field) where T : struct, IMetrology<T>
        => value is { } quantity && quantity <= T.AdditiveIdentity
               ? throw new ArgumentOutOfRangeException(field, "Must be greater than zero.") : value;

    private static T? NonNegative<T>(T? value, String field) where T : struct, IMetrology<T>
        => value is { } quantity && quantity < T.AdditiveIdentity
               ? throw new ArgumentOutOfRangeException(field, "Must not be negative.") : value;

    private static T? ReadEnum<T>(JObject json, String field) where T : struct, Enum
    {
        var text = InfrastructureJson.Text(json, field);
        if (text is null) return null;
        if (!Enum.TryParse<T>(text, out var value) || !Enum.IsDefined(value) || value.ToString() != text)
            throw new ArgumentException($"{field}: invalid {typeof(T).Name} value.");
        return value;
    }
}
