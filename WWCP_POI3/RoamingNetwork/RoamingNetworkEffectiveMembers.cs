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

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The effective members of the effective content of a version differing from those of its content, by their owners.
    /// </summary>
    internal sealed class RoamingNetworkEffectiveMembers
    {

        /// <summary>
        /// The effective charging pools, with their effective charging stations and EVSEs, by their charging station operators.
        /// </summary>
        public Dictionary<ChargingStationOperator_Id, IReadOnlyDictionary<ChargingPool_Id,   ChargingPool>>    ChargingPools    { get; } = [];

        /// <summary>
        /// The effective charging tariffs, by their charging station operators.
        /// </summary>
        public Dictionary<ChargingStationOperator_Id, IReadOnlyDictionary<ChargingTariff_Id, ChargingTariff>>  ChargingTariffs  { get; } = [];

        /// <summary>
        /// The effective parking spaces, by their parking operators.
        /// </summary>
        public Dictionary<ParkingOperator_Id,         IReadOnlyDictionary<ParkingSpace_Id,   ParkingSpace>>    ParkingSpaces    { get; } = [];

    }

}
