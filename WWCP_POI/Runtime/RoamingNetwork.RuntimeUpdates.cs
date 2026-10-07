/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetwork
{
    /// <summary>
    /// Apply one scoped operational/admin status update in place, without changing static data,
    /// ETags or revision. Expected current status checks and insertion share the schedule lock.
    /// </summary>
    public void ApplyRuntimeUpdate(RoamingNetworkRuntimeUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);
        lock (snapshotLock)
        {
            if (!InfrastructureChangeSchema.SameId(InfrastructureEntityType.RoamingNetwork, update.RoamingNetworkId, Id.ToString()))
                throw new ArgumentException("The runtime update targets a different roaming network.", nameof(update));
            if (!update.StaticETags.IsEmpty && !update.StaticETags.SequenceEqual(ETags))
                throw new InvalidOperationException("The runtime update's static-content precondition failed.");

            var target = ResolveRuntimeTarget(update.Target);
            switch (target)
            {
                case RoamingNetwork entity:
                    Apply(entity, update, RoamingNetworkAdminStatusType.TryParse, RoamingNetworkStatusType.TryParse); break;
                case ChargingStationOperator entity:
                    Apply(entity, update, ChargingStationOperatorAdminStatusTypes.TryParse, ChargingStationOperatorStatusTypes.TryParse); break;
                case EMobilityProvider entity:
                    Apply(entity, update, EMobilityProviderAdminStatusTypes.TryParse, EMobilityProviderStatusTypes.TryParse); break;
                case ChargingPool entity:
                    Apply(entity, update, ChargingPoolAdminStatusType.TryParse, ChargingPoolStatusType.TryParse); break;
                case ChargingStation entity:
                    Apply(entity, update, ChargingStationAdminStatusType.TryParse, ChargingStationStatusType.TryParse); break;
                case EVSE entity:
                    Apply(entity, update, EVSEAdminStatusType.TryParse, EVSEStatusType.TryParse); break;
                case ChargingTariff entity:
                    Apply(entity, update, ChargingTariffAdminStatusTypes.TryParse, ChargingTariffStatusTypes.TryParse); break;
                case EnergyMeter entity:
                    Apply(entity, update, EnergyMeterAdminStatusTypes.TryParse, EnergyMeterStatusTypes.TryParse); break;
                case GridOperator entity:
                    Apply(entity, update, GridOperatorAdminStatusTypes.TryParse, GridOperatorStatusTypes.TryParse); break;
                case ParkingOperator entity:
                    Apply(entity, update, ParkingOperatorAdminStatusTypes.TryParse, ParkingOperatorStatusTypes.TryParse); break;
                case EVSEGroup entity:
                    Apply(entity, update, EVSEGroupAdminStatusTypes.TryParse, EVSEGroupStatusTypes.TryParse); break;
                case ChargingStationGroup entity:
                    Apply(entity, update, ChargingStationGroupAdminStatusTypes.TryParse, ChargingStationGroupStatusTypes.TryParse); break;
                case ChargingPoolGroup entity:
                    Apply(entity, update, ChargingPoolGroupAdminStatusTypes.TryParse, ChargingPoolGroupStatusTypes.TryParse); break;
                case ChargingTariffGroup entity:
                    Apply(entity, update, ChargingTariffGroupAdminStatusTypes.TryParse, ChargingTariffGroupStatusTypes.TryParse); break;
                case ParkingGarage entity:
                    Apply(entity, update, ParkingGarageAdminStatusTypes.TryParse, ParkingGarageStatusTypes.TryParse); break;
                case ParkingSpace entity:
                    Apply(entity, update, ParkingSpaceAdminStatusTypes.TryParse, ParkingSpaceStatusTypes.TryParse); break;
                case ParkingSensor entity:
                    Apply(entity, update, ParkingSensorAdminStatusTypes.TryParse, ParkingSensorStatusTypes.TryParse); break;
                case ParkingSpaceGroup entity:
                    Apply(entity, update, ParkingSpaceGroupAdminStatusTypes.TryParse, ParkingSpaceGroupStatusTypes.TryParse); break;
                default:
                    throw new ArgumentException("Unsupported runtime target.");
            }
        }
    }

    private Object ResolveRuntimeTarget(POIRuntimeTarget target)
    {
        Object? owner = target.OwnerEntityType switch
        {
            InfrastructureEntityType.RoamingNetwork => InfrastructureChangeSchema.SameId(InfrastructureEntityType.RoamingNetwork,
                                                                                          target.OwnerEntityId, Id.ToString()) ? this : null,
            InfrastructureEntityType.ChargingStationOperator => GetChargingStationOperatorById(ChargingStationOperator_Id.Parse(target.OwnerEntityId)),
            InfrastructureEntityType.EMobilityProvider => GetEMobilityProviderById(EMobilityProvider_Id.Parse(target.OwnerEntityId)),
            InfrastructureEntityType.ChargingPool => GetChargingPoolById(ChargingPool_Id.Parse(target.OwnerEntityId)),
            InfrastructureEntityType.ChargingStation => GetChargingStationById(ChargingStation_Id.Parse(target.OwnerEntityId)),
            InfrastructureEntityType.EVSE => GetEVSEById(EVSE_Id.Parse(target.OwnerEntityId)),
            InfrastructureEntityType.ChargingTariff => ChargingTariffs.FirstOrDefault(tariff => tariff.Id == ChargingTariff_Id.Parse(target.OwnerEntityId)),
            _ => FindGraphRuntimeEntity(target.OwnerEntityType, target.OwnerEntityId)
        };
        if (owner is null) throw new ArgumentException("The runtime target's owner does not exist.");
        if (target.Kind == POIRuntimeTargetKind.Entity) return owner;
        Object? child = target.Kind switch
        {
            POIRuntimeTargetKind.EnergyMeter => owner switch
            {
                ChargingPool pool => pool.EnergyMeters.FirstOrDefault(meter => meter.Id == EnergyMeter_Id.Parse(target.ChildId!)),
                ChargingStation station => station.EnergyMeters.FirstOrDefault(meter => meter.Id == EnergyMeter_Id.Parse(target.ChildId!)),
                EVSE evse => evse.EnergyMeter is { } meter && meter.Id == EnergyMeter_Id.Parse(target.ChildId!) ? meter : null,
                _ => null
            },
            POIRuntimeTargetKind.GridConnectionPointEnergyMeter => owner is ChargingPool pool &&
                pool.GridConnectionPoint?.EnergyMeter is { } meter && meter.Id == EnergyMeter_Id.Parse(target.ChildId!) ? meter : null,
            POIRuntimeTargetKind.GridConnectionPointGridOperator => owner is ChargingPool pool &&
                pool.GridConnectionPoint?.GridOperator is { } op && op.Id == GridOperator_Id.Parse(target.ChildId!) ? op : null,
            _ => null
        };
        return child ?? throw new ArgumentException("The runtime child does not exist in the specified ownership slot.");
    }

    private static void Apply<TId, TAdmin, TStatus>(AImmutableEMobilityEntity<TId, TAdmin, TStatus> entity,
                                                   RoamingNetworkRuntimeUpdate update,
                                                   JsonValueParsing.ScalarParser<TAdmin> adminParser,
                                                   JsonValueParsing.ScalarParser<TStatus> statusParser)
        where TId : IId
        where TAdmin : struct, IComparable
        where TStatus : struct, IComparable
    {
        static Timestamped<T> Parse<T>(POIRuntimeStatusValue value, JsonValueParsing.ScalarParser<T> parser) where T : struct
            => parser(value.Value, out var parsed)
                   ? new Timestamped<T>(value.Timestamp, parsed)
                   : throw new ArgumentException($"Invalid {typeof(T).Name} value '{value.Value}'.");

        if (update.Kind == POIRuntimeStatusKind.AdminStatus)
            entity.ApplyRuntimeAdminStatus(Parse(update.NewStatus, adminParser),
                                            update.ExpectedStatus is { } expected ? Parse(expected, adminParser) : null, update.Mode);
        else
            entity.ApplyRuntimeStatus(Parse(update.NewStatus, statusParser),
                                       update.ExpectedStatus is { } expected ? Parse(expected, statusParser) : null, update.Mode);
    }
}
