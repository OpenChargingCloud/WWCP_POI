/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text.Json.Serialization;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The position of a runtime entity within its static owner.
/// </summary>
public enum POIRuntimeTargetKind
{
    Entity,
    EnergyMeter,
    GridConnectionPointEnergyMeter
}

/// <summary>
/// An immutable address using existing entity identities and an explicit ownership scope.
/// </summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed record POIRuntimeTarget
{
    /// <summary>
    /// Create and validate a runtime address. Connectors have no operational status schedule.
    /// </summary>
    [JsonConstructor]
    public POIRuntimeTarget(POIRuntimeTargetKind kind, InfrastructureEntityType ownerEntityType,
                            String ownerEntityId, String? childId = null)
    {
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(ownerEntityType) ||
            ownerEntityType is InfrastructureEntityType.ChargingConnector or InfrastructureEntityType.ChargingStationManufacturer or
                               InfrastructureEntityType.ParkingProduct or InfrastructureEntityType.TransparencySoftware or InfrastructureEntityType.TransparencySoftwareCertificate)
            throw new ArgumentException("Invalid runtime target kind or owner type.");
        OwnerEntityId = new InfrastructureEntityKey(ownerEntityType, ownerEntityId).Id;
        if (kind == POIRuntimeTargetKind.Entity && childId is not null)
            throw new ArgumentException("An entity target has no child ID.", nameof(childId));
        if (kind != POIRuntimeTargetKind.Entity && String.IsNullOrWhiteSpace(childId))
            throw new ArgumentException("A nested runtime target requires its child ID.", nameof(childId));
        if (kind == POIRuntimeTargetKind.EnergyMeter &&
            ownerEntityType is not (InfrastructureEntityType.ChargingPool or InfrastructureEntityType.ChargingStation or InfrastructureEntityType.EVSE))
            throw new ArgumentException("A direct meter requires a pool, station or EVSE owner.");
        if (kind == POIRuntimeTargetKind.GridConnectionPointEnergyMeter &&
            ownerEntityType != InfrastructureEntityType.ChargingPool)
            throw new ArgumentException("A grid connection point requires a pool owner.");
        if (kind != POIRuntimeTargetKind.Entity)
            _ = EnergyMeter_Id.Parse(childId!);
        Kind = kind;
        OwnerEntityType = ownerEntityType;
        ChildId = childId;
    }

    /// <summary>
    /// The ownership slot being addressed.
    /// </summary>
    [JsonInclude, JsonRequired, JsonConverter(typeof(POIRuntimeEnumConverter<POIRuntimeTargetKind>))]
    public POIRuntimeTargetKind Kind { get; private init; }

    /// <summary>
    /// The independently addressable static owner type.
    /// </summary>
    [JsonInclude, JsonRequired, JsonConverter(typeof(POIRuntimeEnumConverter<InfrastructureEntityType>))]
    public InfrastructureEntityType OwnerEntityType { get; private init; }

    /// <summary>
    /// The static owner's identifier; for entity targets this is the target's identifier.
    /// </summary>
    [JsonInclude, JsonRequired]
    public String OwnerEntityId { get; private init; }

    /// <summary>
    /// The meter identifier, required for nested targets.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public String? ChildId { get; }

    /// <summary>
    /// Address an independently owned graph entity with operational status schedules.
    /// </summary>
    public static POIRuntimeTarget Entity(InfrastructureEntityType type, String id)
        => new(POIRuntimeTargetKind.Entity, type, id);

    /// <summary>
    /// Address a directly owned pool/station meter or the EVSE's meter.
    /// </summary>
    public static POIRuntimeTarget Meter(InfrastructureEntityType ownerType, String ownerId, String meterId)
        => new(POIRuntimeTargetKind.EnergyMeter, ownerType, ownerId, meterId);

    /// <summary>
    /// Address the meter within a pool's grid connection point.
    /// </summary>
    public static POIRuntimeTarget ConnectionMeter(String poolId, String meterId)
        => new(POIRuntimeTargetKind.GridConnectionPointEnergyMeter, InfrastructureEntityType.ChargingPool, poolId, meterId);

    /// <summary>
    /// Address a shared grid operator in the network registry.
    /// </summary>
    public static POIRuntimeTarget GridOperator(String gridOperatorId)
        => Entity(InfrastructureEntityType.GridOperator, gridOperatorId);
}

internal sealed class POIRuntimeEnumConverter<T> : JsonStringEnumConverter<T> where T : struct, Enum
{
    public POIRuntimeEnumConverter() : base(allowIntegerValues: false) { }
}
