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

using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetwork
{
    /// <summary>
    /// Overlay current runtime state without editing the persistent POI version.
    /// </summary>
    private void WriteCurrentRuntimeStatuses(JObject document)
    {
        var states = new Dictionary<InfrastructureEntityKey, (JObject Admin, JObject Status)>();
        var meters = new Dictionary<InfrastructureEntityKey, Dictionary<String, (JObject Admin, JObject Status)>>();
        var connectionMeters = new Dictionary<InfrastructureEntityKey, (JObject Admin, JObject Status)>();
        var gridOperators = new Dictionary<InfrastructureEntityKey, (JObject Admin, JObject Status)>();

        (JObject Admin, JObject Status) ReadStatuses<TId, TAdmin, TStatus>(AImmutableEMobilityEntity<TId, TAdmin, TStatus> entity)
            where TId : IId
            where TAdmin : IComparable
            where TStatus : IComparable
        {
            var admin = entity.AdminStatus;
            var status = entity.Status;
            return (
                new JObject(new JProperty("value", admin.Value.ToString()),
                            new JProperty("timestamp", admin.Timestamp.ToUniversalTime().ToString("O"))),
                new JObject(new JProperty("value", status.Value.ToString()),
                            new JProperty("timestamp", status.Timestamp.ToUniversalTime().ToString("O"))));
        }

        void CaptureMeter(InfrastructureEntityKey owner, EnergyMeter meter)
        {
            if (!meters.TryGetValue(owner, out var ownedMeters))
                meters.Add(owner, ownedMeters = new(StringComparer.OrdinalIgnoreCase));
            ownedMeters.Add(meter.Id.ToString(), ReadStatuses(meter));
        }

        void Capture<TId, TAdmin, TStatus>(AImmutableEMobilityEntity<TId, TAdmin, TStatus> entity,
                                          InfrastructureEntityType type)
            where TId : IId
            where TAdmin : IComparable
            where TStatus : IComparable
        {
            states.Add(new(type, entity.Id.ToString()!), ReadStatuses(entity));
        }

        Capture(this, InfrastructureEntityType.RoamingNetwork);
        foreach (var entity in ChargingStationOperators) Capture(entity, InfrastructureEntityType.ChargingStationOperator);
        foreach (var entity in EMobilityProviders) Capture(entity, InfrastructureEntityType.EMobilityProvider);
        foreach (var entity in ChargingPools)
        {
            Capture(entity, InfrastructureEntityType.ChargingPool);
            var owner = new InfrastructureEntityKey(InfrastructureEntityType.ChargingPool, entity.Id.ToString());
            foreach (var meter in entity.EnergyMeters) CaptureMeter(owner, meter);
            if (entity.GridConnectionPoint is { } point)
            {
                gridOperators.Add(owner, ReadStatuses(point.GridOperator));
                if (point.EnergyMeter is { } meter) connectionMeters.Add(owner, ReadStatuses(meter));
            }
        }
        foreach (var entity in ChargingStations)
        {
            Capture(entity, InfrastructureEntityType.ChargingStation);
            foreach (var meter in entity.EnergyMeters)
                CaptureMeter(new(InfrastructureEntityType.ChargingStation, entity.Id.ToString()), meter);
        }
        foreach (var entity in EVSEs)
        {
            Capture(entity, InfrastructureEntityType.EVSE);
            if (entity.EnergyMeter is { } meter)
                CaptureMeter(new(InfrastructureEntityType.EVSE, entity.Id.ToString()), meter);
        }
        foreach (var entity in ChargingTariffs) Capture(entity, InfrastructureEntityType.ChargingTariff);

        void Write(JObject json, InfrastructureEntityType type)
        {
            if (states.TryGetValue(new(type, json["@id"]!.Value<String>()!), out var state))
            {
                json["adminStatus"] = state.Admin;
                json["status"] = state.Status;
            }
            if (meters.TryGetValue(new(type, json["@id"]!.Value<String>()!), out var ownedMeters))
            {
                void WriteMeter(JObject meter)
                {
                    if (meter["id"]?.Value<String>() is { } id && ownedMeters.TryGetValue(id, out var meterState))
                    {
                        meter["adminStatus"] = meterState.Admin;
                        meter["status"] = meterState.Status;
                    }
                }
                if (type == InfrastructureEntityType.EVSE && json["energyMeter"] is JObject meter)
                    WriteMeter(meter);
                if ((type is InfrastructureEntityType.ChargingStation or InfrastructureEntityType.ChargingPool) &&
                    json["energyMeters"] is JArray children)
                    foreach (var stationMeter in children.OfType<JObject>()) WriteMeter(stationMeter);
            }
            if (type == InfrastructureEntityType.ChargingPool && json["gridConnectionPoint"] is JObject point)
            {
                var owner = new InfrastructureEntityKey(type, json["@id"]!.Value<String>()!);
                if (point["gridOperator"] is JObject gridOperator && gridOperators.TryGetValue(owner, out var operatorState))
                {
                    gridOperator["adminStatus"] = operatorState.Admin;
                    gridOperator["status"] = operatorState.Status;
                }
                if (point["energyMeter"] is JObject meter && connectionMeters.TryGetValue(owner, out var meterState))
                {
                    meter["adminStatus"] = meterState.Admin;
                    meter["status"] = meterState.Status;
                }
            }
            foreach (var relation in InfrastructureChangeSchema.Relations)
            {
                if (relation.Value.Parent != type || json[relation.Value.Field] is not JArray children) continue;
                foreach (var child in children.OfType<JObject>())
                {
                    if (relation.Key != InfrastructureEntityType.ChargingConnector) Write(child, relation.Key);
                }
            }
        }
        Write(document, InfrastructureEntityType.RoamingNetwork);
    }
}
