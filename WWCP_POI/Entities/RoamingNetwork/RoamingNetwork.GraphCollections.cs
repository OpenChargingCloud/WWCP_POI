/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetwork
{
    /// <summary>
    /// EVSE groups owned by this network's charging station operators.
    /// </summary>
    public ImmutableArray<EVSEGroup> EVSEGroups => ChargingStationOperators.SelectMany(op => op.EVSEGroups).ToImmutableArray();

    /// <summary>
    /// Station groups owned by this network's charging station operators.
    /// </summary>
    public ImmutableArray<ChargingStationGroup> ChargingStationGroups => ChargingStationOperators.SelectMany(op => op.ChargingStationGroups).ToImmutableArray();

    /// <summary>
    /// Pool groups owned by this network's charging station operators.
    /// </summary>
    public ImmutableArray<ChargingPoolGroup> ChargingPoolGroups => ChargingStationOperators.SelectMany(op => op.ChargingPoolGroups).ToImmutableArray();

    /// <summary>
    /// Tariff groups owned by this network's charging station operators.
    /// </summary>
    public ImmutableArray<ChargingTariffGroup> ChargingTariffGroups => ChargingStationOperators.SelectMany(op => op.ChargingTariffGroups).ToImmutableArray();

    /// <summary>
    /// Garages owned by this network's parking operators.
    /// </summary>
    public ImmutableArray<ParkingGarage> ParkingGarages => ParkingOperators.SelectMany(op => op.ParkingGarages).ToImmutableArray();

    /// <summary>
    /// Parking spaces owned by this network's parking operators.
    /// </summary>
    public ImmutableArray<ParkingSpace> ParkingSpaces => ParkingOperators.SelectMany(op => op.ParkingSpaces).ToImmutableArray();

    /// <summary>
    /// Parking sensors owned by this network's parking operators.
    /// </summary>
    public ImmutableArray<ParkingSensor> ParkingSensors => ParkingOperators.SelectMany(op => op.ParkingSensors).ToImmutableArray();

    /// <summary>
    /// Parking space groups owned by this network's parking operators.
    /// </summary>
    public ImmutableArray<ParkingSpaceGroup> ParkingSpaceGroups => ParkingOperators.SelectMany(op => op.ParkingSpaceGroups).ToImmutableArray();

    private Object? FindGraphRuntimeEntity(InfrastructureEntityType type, String id)
    {
        T? Find<T>(IEnumerable<T> values, Func<T, String> key) where T : class
            => values.FirstOrDefault(value => InfrastructureChangeSchema.SameId(type, key(value), id));
        return type switch
        {
            InfrastructureEntityType.GridOperator => Find(GridOperators, value => value.Id.ToString()),
            InfrastructureEntityType.ParkingOperator => Find(ParkingOperators, value => value.Id.ToString()),
            InfrastructureEntityType.EVSEGroup => Find(EVSEGroups, value => value.Id.ToString()),
            InfrastructureEntityType.ChargingStationGroup => Find(ChargingStationGroups, value => value.Id.ToString()),
            InfrastructureEntityType.ChargingPoolGroup => Find(ChargingPoolGroups, value => value.Id.ToString()),
            InfrastructureEntityType.ChargingTariffGroup => Find(ChargingTariffGroups, value => value.Id.ToString()),
            InfrastructureEntityType.ParkingGarage => Find(ParkingGarages, value => value.Id.ToString()),
            InfrastructureEntityType.ParkingSpace => Find(ParkingSpaces, value => value.Id.ToString()),
            InfrastructureEntityType.ParkingSensor => Find(ParkingSensors, value => value.Id.ToString()),
            InfrastructureEntityType.ParkingSpaceGroup => Find(ParkingSpaceGroups, value => value.Id.ToString()),
            _ => null
        };
    }
}
