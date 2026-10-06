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
        /// Reconstruct only the entity and its ancestors for domain validation.
        /// Descendants remain in immutable storage, keeping validation independent of subtree size.
        /// </summary>
        private static Object Project(InfrastructureEntityKey  key,
                                      EntityMap                map)
        {

            var entity   = map[key];
            var document = OwnJSON(entity);
            var parent   = entity.Parent is { } parentKey
                               ? Project(parentKey, map)
                               : null;

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
                _                                                => throw new ArgumentOutOfRangeException(nameof(key))
            };

        }

        #endregion

    }

}
