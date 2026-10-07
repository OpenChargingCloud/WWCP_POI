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

#region Usings

using EntityMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class RoamingNetworkDataSnapshot
    {

        #region Validate entity documents

        private static void Validate(InfrastructureEntityKey  key,
                                     EntityMap                map)

            => Project(key, map);

        /// <summary>
        /// Reconstruct the entity and its ancestors for domain validation.
        /// Groups additionally resolve the owning operator's infrastructure; parking resolves station references.
        /// </summary>
        private static Object Project(InfrastructureEntityKey  key,
                                      EntityMap                map)
        {

            var entity   = map[key];
            var document = OwnJSON(entity);
            var parent   = entity.Parent is { } parentKey
                               ? Project(parentKey, map)
                               : null;

            if (key.Type is InfrastructureEntityType.EVSEGroup or InfrastructureEntityType.ChargingStationGroup or
                            InfrastructureEntityType.ChargingPoolGroup or InfrastructureEntityType.ChargingTariffGroup)
            {
                var owner = map[entity.Parent!.Value];
                var ownerDocument = OwnJSON(owner);
                foreach (var type in new[] { InfrastructureEntityType.ChargingPool, InfrastructureEntityType.ChargingTariff })
                    ownerDocument[InfrastructureChangeSchema.Relations[type].Field] = new Newtonsoft.Json.Linq.JArray(
                        owner.Children.Where(child => child.Type == type).Select(child => ValidationDocument(child, map)));
                var op = ChargingStationOperator.Parse(ownerDocument, ((ChargingStationOperator) parent!).RoamingNetwork);
                return key.Type switch
                {
                    InfrastructureEntityType.EVSEGroup => EVSEGroup.Parse(document, op),
                    InfrastructureEntityType.ChargingStationGroup => ChargingStationGroup.Parse(document, op),
                    InfrastructureEntityType.ChargingPoolGroup => ChargingPoolGroup.Parse(document, op),
                    _ => ChargingTariffGroup.Parse(document, op)
                };
            }

            ChargingStation[] Stations()
                => map.Keys.Where(item => item.Type == InfrastructureEntityType.ChargingStation).
                       Select(item => (ChargingStation) Project(item, map)).ToArray();

            return key.Type switch
            {
                InfrastructureEntityType.RoamingNetwork           => RoamingNetwork.Parse(document),
                InfrastructureEntityType.ChargingStationOperator => ChargingStationOperator.Parse(document, (RoamingNetwork) parent!),
                InfrastructureEntityType.EMobilityProvider        => EMobilityProvider.Parse(document, (RoamingNetwork) parent!),
                InfrastructureEntityType.ChargingPool             => ChargingPool.Parse(document, (ChargingStationOperator) parent!),
                InfrastructureEntityType.ChargingStation          => ChargingStation.Parse(document, (ChargingPool) parent!),
                InfrastructureEntityType.EVSE                     => EVSE.Parse(document, (ChargingStation) parent!),
                InfrastructureEntityType.ChargingConnector        => ChargingConnector.Parse(document),
                InfrastructureEntityType.ChargingTariff           => ChargingTariff.Parse(document, (ChargingStationOperator) parent!),
                InfrastructureEntityType.ChargingStationManufacturer => ChargingStationManufacturer.Parse(document),
                InfrastructureEntityType.GridOperator             => GridOperator.Parse(document, (RoamingNetwork) parent!),
                InfrastructureEntityType.ParkingOperator          => ParkingOperator.Parse(ValidationDocument(key, map), (RoamingNetwork) parent!, Stations()),
                InfrastructureEntityType.ParkingGarage            => ParkingGarage.Parse(document, Stations()),
                InfrastructureEntityType.ParkingSpace             => ParkingSpace.Parse(document, Stations()),
                InfrastructureEntityType.ParkingSensor            => ParkingSensor.Parse(document, Stations()),
                InfrastructureEntityType.ParkingSpaceGroup        => ParkingSpaceGroup.Parse(document, Stations()),
                _                                                => throw new ArgumentOutOfRangeException(nameof(key))
            };

        }

        private static Newtonsoft.Json.Linq.JObject ValidationDocument(InfrastructureEntityKey key, EntityMap map)
        {
            var entity = map[key];
            var document = OwnJSON(entity);
            foreach (var relation in InfrastructureChangeSchema.Relations.Where(relation => relation.Value.Parent == key.Type))
                document[relation.Value.Field] = new Newtonsoft.Json.Linq.JArray(
                    entity.Children.Where(child => child.Type == relation.Key).OrderBy(child => child.Id, StringComparer.Ordinal).
                        Select(child => ValidationDocument(child, map)));
            return document;
        }

        #endregion

    }

}
