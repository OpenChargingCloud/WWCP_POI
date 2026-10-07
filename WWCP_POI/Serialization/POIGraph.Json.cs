/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;
using cloud.charging.open.protocols.WWCP.POI.CSM;

namespace cloud.charging.open.protocols.WWCP.POI;

internal static class POIGraphJSON
{
    internal static ImmutableArray<T> Unique<T>(IEnumerable<T> values, InfrastructureEntityType type, Func<T, String> id)
    {
        var result = values.ToImmutableArray();
        var seen = new HashSet<InfrastructureEntityKey>();
        foreach (var value in result)
            if (value is null || !seen.Add(new(type, id(value))))
                throw new ArgumentException($"Duplicate or null {type} entry.");
        return result;
    }

    internal static ImmutableArray<T> Read<T>(JObject json, InfrastructureEntityType type, Func<JObject, T> parse, Func<T, String> id)
        => Unique(InfrastructureJson.Array(json, InfrastructureChangeSchema.Relations[type].Field,
                                           token => parse(InfrastructureJson.Entry(token))), type, id);

    internal static void ValidateParkingReferences(ParkingOperator op)
    {
        var spaces = op.ParkingSpaces.Select(value => value.Id).ToHashSet();
        foreach (var values in new[] { op.LocalParkingSpaceIds, op.InvalidParkingSpaceIds })
        {
            var seen = new HashSet<ParkingSpace_Id>();
            foreach (var id in values)
                if (!seen.Add(id) || !spaces.Contains(id))
                    throw new ArgumentException($"Parking space reference '{id}' is duplicated or not owned by this operator.");
        }
        var sensors = op.ParkingSensors.Select(value => value.Id.ToString()).ToHashSet(StringComparer.Ordinal);
        foreach (var values in op.ParkingSpaces.Select(value => value.Sensors).Concat(op.ParkingSpaceGroups.Select(value => value.Sensors)))
        {
            var seen = new HashSet<String>(StringComparer.Ordinal);
            foreach (var text in values)
            {
                var id = ParkingSensor_Id.Parse(text).ToString();
                if (!seen.Add(id) || !sensors.Contains(id))
                    throw new ArgumentException($"Parking sensor reference '{id}' is duplicated or not owned by this operator.");
            }
        }
    }
}

public sealed partial class RoamingNetwork
{
    private readonly ConcurrentDictionary<ChargingStationManufacturer_Id, ChargingStationManufacturer> projectedManufacturers = [];

    /// <summary>
    /// Manufacturers owned by this immutable network version.
    /// </summary>
    public ImmutableArray<ChargingStationManufacturer> ChargingStationManufacturers
    {
        get { EnsureSnapshotProjection(); return projectedManufacturers.Values.ToImmutableArray(); }
    }

    /// <summary>
    /// Retrieve a manufacturer by its domain identity.
    /// </summary>
    public ChargingStationManufacturer? GetChargingStationManufacturerById(ChargingStationManufacturer_Id id)
        => ChargingStationManufacturers.FirstOrDefault(value => value.Id == id);

    private void ParseNetworkChildren(JObject json)
    {
        foreach (var value in POIGraphJSON.Read(json, InfrastructureEntityType.GridOperator, document => GridOperator.Parse(document, this), value => value.Id.ToString()))
            projectedGridOperators.TryAdd(value.Id, value);
        foreach (var value in POIGraphJSON.Read(json, InfrastructureEntityType.ChargingStationManufacturer, ChargingStationManufacturer.Parse, value => value.Id.ToString()))
            projectedManufacturers.TryAdd(value.Id, value);
        // Charging station references are resolved after the owned infrastructure has been parsed.
        foreach (var value in POIGraphJSON.Read(json, InfrastructureEntityType.ParkingOperator, document => ParkingOperator.Parse(document, this), value => value.Id.ToString()))
            projectedParkingOperators.TryAdd(value.Id, value);
    }
}

public sealed partial class ChargingStationOperator
{
    private ImmutableDictionary<ChargingPoolGroup_Id, ChargingPoolGroup> chargingPoolGroups = ImmutableDictionary<ChargingPoolGroup_Id, ChargingPoolGroup>.Empty;

    /// <summary>
    /// Pool groups owned by this operator. Members remain references to owned pools.
    /// </summary>
    public ImmutableArray<ChargingPoolGroup> ChargingPoolGroups => chargingPoolGroups.Values.ToImmutableArray();

    /// <summary>
    /// Retrieve a pool group by its domain identity.
    /// </summary>
    public ChargingPoolGroup? GetChargingPoolGroupById(ChargingPoolGroup_Id id)
        => ChargingPoolGroups.FirstOrDefault(value => value.Id == id);

    private void ParseOperatorGroups(JObject json)
    {
        evseGroups = POIGraphJSON.Read(json, InfrastructureEntityType.EVSEGroup, document => EVSEGroup.Parse(document, this), value => value.Id.ToString()).ToImmutableDictionary(value => value.Id);
        chargingStationGroups = POIGraphJSON.Read(json, InfrastructureEntityType.ChargingStationGroup, document => ChargingStationGroup.Parse(document, this), value => value.Id.ToString()).ToImmutableDictionary(value => value.Id);
        chargingPoolGroups = POIGraphJSON.Read(json, InfrastructureEntityType.ChargingPoolGroup, document => ChargingPoolGroup.Parse(document, this), value => value.Id.ToString()).ToImmutableDictionary(value => value.Id);
        chargingTariffGroups = POIGraphJSON.Read(json, InfrastructureEntityType.ChargingTariffGroup, document => ChargingTariffGroup.Parse(document, this), value => value.Id.ToString()).ToImmutableDictionary(value => value.Id);
    }
}

public sealed partial class ParkingOperator
{
    /// <summary>
    /// Independently addressed parking spaces owned by this operator.
    /// </summary>
    public ImmutableArray<ParkingSpace> ParkingSpaces { get; }

    /// <summary>
    /// Independently addressed sensors owned by this operator.
    /// </summary>
    public ImmutableArray<ParkingSensor> ParkingSensors { get; }

    /// <summary>
    /// Independently addressed parking space groups owned by this operator.
    /// </summary>
    public ImmutableArray<ParkingSpaceGroup> ParkingSpaceGroups { get; }
}
