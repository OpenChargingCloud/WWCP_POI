/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Concurrent;
using System.Collections.Immutable;
using Newtonsoft.Json.Linq;

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

    internal static ImmutableArray<TId> ReferenceIds<TId>(IEnumerable<TId> ids, InfrastructureEntityType type) where TId : org.GraphDefined.Vanaheimr.Illias.IId
    {
        var values = ids.ToImmutableArray();
        var seen = new HashSet<InfrastructureEntityKey>();
        foreach (var id in values)
            if (id.IsNullOrEmpty || !seen.Add(new(type, id.ToString()!)))
                throw new ArgumentException($"Empty or duplicate {type} reference.");
        return values;
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
        var garages = op.ParkingGarages.Select(value => value.Id).ToHashSet();
        foreach (var space in op.ParkingSpaces)
            if (space.ParkingGarageId is { } garage && !garages.Contains(garage))
                throw new ArgumentException($"Parking garage '{garage}' is not owned by this operator.");
        foreach (var group in op.ParkingSpaceGroups)
            foreach (var id in group.ParkingSpaceIds)
                if (!spaces.Contains(id))
                    throw new ArgumentException($"Parking group member '{id}' is not owned by this operator.");
        var products = op.ParkingProducts.Select(value => value.Id).ToHashSet();
        foreach (var ids in op.ParkingSpaces.Select(value => value.ParkingProductIds).
                           Concat(op.ParkingGarages.Select(value => value.ParkingProductIds)).
                           Concat(op.ParkingSpaceGroups.Select(value => value.ParkingProductIds)))
            foreach (var id in ids)
                if (!products.Contains(id))
                    throw new ArgumentException($"Parking product '{id}' is not owned by this operator.");
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
    private readonly ConcurrentDictionary<InfrastructureEntityKey, GridOperator> projectedGridOperatorReferences = [];
    private ImmutableDictionary<TransparencySoftware_Id, TransparencySoftware> projectedSoftwareReferences = ImmutableDictionary<TransparencySoftware_Id, TransparencySoftware>.Empty;
    private ImmutableDictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate> projectedCertificateReferences = ImmutableDictionary<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>.Empty;
    private ImmutableArray<TransparencySoftware> projectedTransparencySoftware = [];
    private ImmutableArray<TransparencySoftwareCertificate> projectedTransparencySoftwareCertificates = [];

    /// <summary>
    /// Software releases owned once by this network version and referenced by meters and certificates.
    /// </summary>
    public ImmutableArray<TransparencySoftware> TransparencySoftware
    {
        get { EnsureSnapshotProjection(); return projectedTransparencySoftware; }
    }

    /// <summary>
    /// Model/version approval documents owned once by this network version.
    /// </summary>
    public ImmutableArray<TransparencySoftwareCertificate> TransparencySoftwareCertificates
    {
        get { EnsureSnapshotProjection(); return projectedTransparencySoftwareCertificates; }
    }

    /// <summary>
    /// Resolve a software release within this version.
    /// </summary>
    public TransparencySoftware? GetTransparencySoftwareById(TransparencySoftware_Id id)
        { EnsureSnapshotProjection(); return projectedSoftwareReferences.GetValueOrDefault(id); }

    /// <summary>
    /// Resolve a model/version approval document within this version.
    /// </summary>
    public TransparencySoftwareCertificate? GetTransparencySoftwareCertificateById(TransparencySoftwareCertificate_Id id)
        { EnsureSnapshotProjection(); return projectedCertificateReferences.GetValueOrDefault(id); }

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

    private void ParseNetworkCatalogs(JObject json)
    {
        foreach (var value in POIGraphJSON.Read(json, InfrastructureEntityType.GridOperator, document => GridOperator.Parse(document, this), value => value.Id.ToString()))
        {
            projectedGridOperators.TryAdd(value.Id, value);
            projectedGridOperatorReferences.TryAdd(new(InfrastructureEntityType.GridOperator, value.Id.ToString()), value);
        }
        foreach (var value in POIGraphJSON.Read(json, InfrastructureEntityType.ChargingStationManufacturer, ChargingStationManufacturer.Parse, value => value.Id.ToString()))
            projectedManufacturers.TryAdd(value.Id, value);
        projectedTransparencySoftware = POIGraphJSON.Read(json, InfrastructureEntityType.TransparencySoftware,
            document => POI.TransparencySoftware.Parse(POIEnvelope.Content(document)), value => value.Id.ToString());
        projectedSoftwareReferences = projectedTransparencySoftware.ToImmutableDictionary(value => value.Id);
        projectedTransparencySoftwareCertificates = POIGraphJSON.Read(json, InfrastructureEntityType.TransparencySoftwareCertificate,
            document => TransparencySoftwareCertificate.Parse(document, this), value => value.Id.ToString());
        projectedCertificateReferences = projectedTransparencySoftwareCertificates.ToImmutableDictionary(value => value.Id);
    }

    private void ParseNetworkChildren(JObject json)
    {
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

    /// <summary>
    /// Products owned once by this parking operator.
    /// </summary>
    public ImmutableArray<ParkingProduct> ParkingProducts { get; }

    /// <summary>
    /// Available products from the space, its optional garage and all its groups, without inferred pricing precedence.
    /// </summary>
    public ImmutableArray<ParkingProduct> GetAvailableParkingProducts(ParkingSpace_Id spaceId)
    {
        var space = ParkingSpaces.SingleOrDefault(value => value.Id == spaceId) ??
                    throw new ArgumentException("The space is not owned by this operator.", nameof(spaceId));
        var ids = space.ParkingProductIds.ToHashSet();
        if (space.ParkingGarageId is { } garageId)
            ids.UnionWith(ParkingGarages.Single(value => value.Id == garageId).ParkingProductIds);
        foreach (var group in ParkingSpaceGroups.Where(value => value.ParkingSpaceIds.Contains(spaceId)))
            ids.UnionWith(group.ParkingProductIds);
        return ParkingProducts.Where(value => ids.Contains(value.Id)).OrderBy(value => value.Id.ToString(), StringComparer.Ordinal).ToImmutableArray();
    }
}
