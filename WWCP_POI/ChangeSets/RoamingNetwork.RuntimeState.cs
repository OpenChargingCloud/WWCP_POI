/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Collections.Immutable;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetwork
{
    /// <summary>
    /// Capture runtime state at derivation time. A derived version owns independent
    /// schedules, including when its hierarchy is not materialized until later.
    /// Existing nested entities retain runtime histories across static property replacements.
    /// New identities and removed/recreated subtrees start with their domain runtime defaults.
    /// </summary>
    private Action<RoamingNetwork> CaptureRuntimeState(RoamingNetworkChangeSet changeSet, RoamingNetwork result)
    {
        var restore = new List<Action<RoamingNetwork>>();
        var changed = changeSet.Changes;
        var removed = new HashSet<(InfrastructureEntityType, String)>();
        foreach (var change in changed.Where(change => change.Kind == RoamingNetworkChangeKind.Remove))
        {
            var type = InfrastructureChangeSchema.Type(change.EntityType);
            var pending = new Stack<InfrastructureEntityKey>();
            var key = new InfrastructureEntityKey(type, change.EntityId,
                                                  type == InfrastructureEntityType.ChargingConnector ? change.ParentEntityId : null);
            if (!DataSnapshot.Entities.ContainsKey(key)) continue;
            pending.Push(key);
            while (pending.TryPop(out var removedKey))
            {
                removed.Add((removedKey.Type, InfrastructureChangeSchema.Identity(removedKey.Type, removedKey.Id)));
                foreach (var child in DataSnapshot.Entities[removedKey].Children) pending.Push(child);
            }
        }

        Boolean Replaced(InfrastructureEntityType type, String id, String property)
            => removed.Contains((type, InfrastructureChangeSchema.Identity(type, id))) || changed.Any(change =>
                   InfrastructureChangeSchema.Type(change.EntityType) == type &&
                   InfrastructureChangeSchema.Identity(type, change.EntityId) == InfrastructureChangeSchema.Identity(type, id) &&
                   (change.Kind is RoamingNetworkChangeKind.Add or RoamingNetworkChangeKind.Remove ||
                    change.Kind == RoamingNetworkChangeKind.UpdateProperty && change.PropertyName == property));

        void Capture<TId, TAdmin, TStatus>(AImmutableEMobilityEntity<TId, TAdmin, TStatus> source,
                                          InfrastructureEntityType type,
                                          Func<RoamingNetwork, AImmutableEMobilityEntity<TId, TAdmin, TStatus>?> find)
            where TId : IId
            where TAdmin : IComparable
            where TStatus : IComparable
        {
            var admin = source.AdminStatusSchedule().ToImmutableArray();
            var status = source.StatusSchedule().ToImmutableArray();
            var historySizes = source.RuntimeHistorySizes;
            var copyRuntime = !Replaced(type, source.Id.ToString()!, "");
            Action<RoamingNetwork> apply = target =>
            {
                if (!copyRuntime || find(target) is not { } entity) return;
                entity.RestoreRuntimeHistorySizes(historySizes);
                entity.SetAdminStatus(admin);
                entity.SetStatus(status);
            };
            if (type == InfrastructureEntityType.RoamingNetwork) apply(result);
            else restore.Add(apply);
        }

        Capture(this, InfrastructureEntityType.RoamingNetwork, target => target);
        foreach (var entity in GridOperators)
            Capture(entity, InfrastructureEntityType.GridOperator, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.GridOperator, entity.Id.ToString()) as GridOperator);
        foreach (var entity in ParkingOperators)
            Capture(entity, InfrastructureEntityType.ParkingOperator, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.ParkingOperator, entity.Id.ToString()) as ParkingOperator);
        foreach (var entity in EVSEGroups)
            Capture(entity, InfrastructureEntityType.EVSEGroup, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.EVSEGroup, entity.Id.ToString()) as EVSEGroup);
        foreach (var entity in ChargingStationGroups)
            Capture(entity, InfrastructureEntityType.ChargingStationGroup, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.ChargingStationGroup, entity.Id.ToString()) as ChargingStationGroup);
        foreach (var entity in ChargingPoolGroups)
            Capture(entity, InfrastructureEntityType.ChargingPoolGroup, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.ChargingPoolGroup, entity.Id.ToString()) as ChargingPoolGroup);
        foreach (var entity in ChargingTariffGroups)
            Capture(entity, InfrastructureEntityType.ChargingTariffGroup, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.ChargingTariffGroup, entity.Id.ToString()) as ChargingTariffGroup);
        foreach (var entity in ParkingGarages)
            Capture(entity, InfrastructureEntityType.ParkingGarage, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.ParkingGarage, entity.Id.ToString()) as ParkingGarage);
        foreach (var entity in ParkingSpaces)
            Capture(entity, InfrastructureEntityType.ParkingSpace, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.ParkingSpace, entity.Id.ToString()) as ParkingSpace);
        foreach (var entity in ParkingSensors)
            Capture(entity, InfrastructureEntityType.ParkingSensor, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.ParkingSensor, entity.Id.ToString()) as ParkingSensor);
        foreach (var entity in ParkingSpaceGroups)
            Capture(entity, InfrastructureEntityType.ParkingSpaceGroup, target => target.FindGraphRuntimeEntity(InfrastructureEntityType.ParkingSpaceGroup, entity.Id.ToString()) as ParkingSpaceGroup);
        foreach (var entity in ChargingStationOperators)
            Capture(entity, InfrastructureEntityType.ChargingStationOperator, target => target.GetChargingStationOperatorById(entity.Id));
        foreach (var entity in EMobilityProviders)
            Capture(entity, InfrastructureEntityType.EMobilityProvider, target => target.GetEMobilityProviderById(entity.Id));
        foreach (var entity in ChargingPools)
        {
            Capture(entity, InfrastructureEntityType.ChargingPool, target => target.GetChargingPoolById(entity.Id));
            var electrical = CaptureElectrical(entity);
            var aggregate = entity.StatusAggregationDelegate;
            var meterRuntime = entity.EnergyMeters.ToDictionary(meter => meter.Id.ToString(),
                                                                meter => CaptureMeterRuntime(meter, ResetsNestedRuntime(changeSet,
                                                                    InfrastructureEntityType.ChargingPool, entity.Id.ToString(), [new("energyMeters", meter.Id.ToString())])),
                                                                StringComparer.OrdinalIgnoreCase);
            var sourcePoint = entity.GridConnectionPoint;
            var connectionMeterRuntime = CaptureMeterRuntime(sourcePoint?.EnergyMeter,
                sourcePoint?.EnergyMeter is { } sourceMeter && ResetsNestedRuntime(changeSet, InfrastructureEntityType.ChargingPool,
                    entity.Id.ToString(), [new("gridConnectionPoint", sourcePoint.Id), new("energyMeter", sourceMeter.Id.ToString())]));
            var gridOperatorRuntime = CaptureNestedRuntime(sourcePoint?.GridOperator,
                sourcePoint is not null && ResetsNestedRuntime(changeSet, InfrastructureEntityType.ChargingPool,
                    entity.Id.ToString(), [new("gridConnectionPoint", sourcePoint.Id), new("gridOperator", sourcePoint.GridOperator.Id.ToString())]));
            var connectionPointId = entity.GridConnectionPoint?.Id;
            restore.Add(target =>
            {
                if (target.GetChargingPoolById(entity.Id) is not { } pool ||
                    Replaced(InfrastructureEntityType.ChargingPool, entity.Id.ToString(), "")) return;
                electrical(pool);
                pool.StatusAggregationDelegate = aggregate;
                foreach (var meter in pool.EnergyMeters)
                    if (meterRuntime.TryGetValue(meter.Id.ToString(), out var restoreMeter))
                        restoreMeter(meter);
                if (String.Equals(connectionPointId, pool.GridConnectionPoint?.Id, StringComparison.Ordinal))
                {
                    connectionMeterRuntime(pool.GridConnectionPoint?.EnergyMeter);
                    gridOperatorRuntime(pool.GridConnectionPoint?.GridOperator);
                }
            });
        }
        foreach (var entity in ChargingStations)
        {
            Capture(entity, InfrastructureEntityType.ChargingStation, target => target.GetChargingStationById(entity.Id));
            var electrical = CaptureElectrical(entity);
            var aggregate = entity.StatusAggregationDelegate;
            var meterRuntime = entity.EnergyMeters.ToDictionary(meter => meter.Id.ToString(),
                                                                meter => CaptureMeterRuntime(meter, ResetsNestedRuntime(changeSet,
                                                                    InfrastructureEntityType.ChargingStation, entity.Id.ToString(), [new("energyMeters", meter.Id.ToString())])),
                                                                StringComparer.OrdinalIgnoreCase);
            restore.Add(target =>
            {
                if (target.GetChargingStationById(entity.Id) is not { } station ||
                    Replaced(InfrastructureEntityType.ChargingStation, entity.Id.ToString(), "")) return;
                electrical(station);
                station.StatusAggregationDelegate = aggregate;
                foreach (var meter in station.EnergyMeters)
                    if (meterRuntime.TryGetValue(meter.Id.ToString(), out var restoreMeter))
                        restoreMeter(meter);
            });
        }
        foreach (var entity in ChargingTariffs)
            Capture(entity, InfrastructureEntityType.ChargingTariff, target => target.ChargingTariffs.FirstOrDefault(tariff => tariff.Id == entity.Id));
        foreach (var entity in EVSEs)
        {
            Capture(entity, InfrastructureEntityType.EVSE, target => target.GetEVSEById(entity.Id));
            var voltage = entity.MaxVoltageRealTime;
            var current = entity.MaxCurrentRealTime;
            var power = entity.MaxPowerRealTime;
            var capacity = entity.MaxCapacityRealTime;
            var lastUpdate = entity.LastStatusUpdate;
            var energyMix = ImmutablePOIValues.Copy(entity.EnergyMixRealTime);
            var energyForecast = ImmutablePOIValues.Copy(entity.OwnEnergyMixPrognoses);
            var voltageForecast = entity.MaxVoltagePrognoses.ToImmutableArray();
            var currentForecast = entity.MaxCurrentPrognoses.ToImmutableArray();
            var powerForecast = entity.MaxPowerPrognoses.ToImmutableArray();
            var capacityForecast = entity.MaxCapacityPrognoses.ToImmutableArray();
            var meterRuntime = CaptureMeterRuntime(entity.EnergyMeter,
                entity.EnergyMeter is { } sourceMeter && ResetsNestedRuntime(changeSet, InfrastructureEntityType.EVSE,
                    entity.Id.ToString(), [new("energyMeter", sourceMeter.Id.ToString())]));
            restore.Add(target =>
            {
                if (target.GetEVSEById(entity.Id) is not { } evse ||
                    Replaced(InfrastructureEntityType.EVSE, entity.Id.ToString(), "")) return;
                evse.MaxVoltageRealTime = voltage;
                evse.MaxCurrentRealTime = current;
                evse.MaxPowerRealTime = power;
                evse.MaxCapacityRealTime = capacity;
                evse.LastStatusUpdate = lastUpdate;
                evse.EnergyMixRealTime = energyMix;
                evse.OwnEnergyMixPrognoses = energyForecast;
                evse.MaxVoltagePrognoses.Replace(voltageForecast);
                evse.MaxCurrentPrognoses.Replace(currentForecast);
                evse.MaxPowerPrognoses.Replace(powerForecast);
                evse.MaxCapacityPrognoses.Replace(capacityForecast);
                meterRuntime(evse.EnergyMeter);
            });
        }

        return target =>
        {
            foreach (var action in restore) action(target);
        };
    }

    private static Action<IRuntimeElectricalState> CaptureElectrical(IRuntimeElectricalState source)
    {
        var current = source.MaxCurrentRealTime;
        var power = source.MaxPowerRealTime;
        var capacity = source.MaxCapacityRealTime;
        var energy = ImmutablePOIValues.Copy(source.EnergyMixRealTime);
        var energyForecast = ImmutablePOIValues.Copy(source.OwnEnergyMixPrognoses);
        var currentForecast = source.MaxCurrentPrognoses.ToImmutableArray();
        var powerForecast = source.MaxPowerPrognoses.ToImmutableArray();
        var capacityForecast = source.MaxCapacityPrognoses.ToImmutableArray();
        return target =>
        {
            target.MaxCurrentRealTime = current;
            target.MaxPowerRealTime = power;
            target.MaxCapacityRealTime = capacity;
            target.EnergyMixRealTime = energy;
            target.OwnEnergyMixPrognoses = energyForecast;
            target.MaxCurrentPrognoses.Replace(currentForecast);
            target.MaxPowerPrognoses.Replace(powerForecast);
            target.MaxCapacityPrognoses.Replace(capacityForecast);
        };
    }

    private static Action<EnergyMeter?> CaptureMeterRuntime(EnergyMeter? source, Boolean reset = false)
        => CaptureNestedRuntime(source, reset);

    private static Action<AImmutableEMobilityEntity<TId, TAdmin, TStatus>?> CaptureNestedRuntime<TId, TAdmin, TStatus>(
        AImmutableEMobilityEntity<TId, TAdmin, TStatus>? source, Boolean reset = false)
        where TId : IId
        where TAdmin : IComparable
        where TStatus : IComparable
    {
        var admin = source?.AdminStatusSchedule().ToImmutableArray();
        var status = source?.StatusSchedule().ToImmutableArray();
        var sizes = source?.RuntimeHistorySizes;
        var id = source is not null ? source.Id : default!;
        return target =>
        {
            if (reset || target is null || admin is null || status is null || sizes is null ||
                !EqualityComparer<TId>.Default.Equals(id, target.Id)) return;
            target.RestoreRuntimeHistorySizes(sizes.Value);
            target.SetAdminStatus(admin.Value);
            target.SetStatus(status.Value);
        };
    }
}
