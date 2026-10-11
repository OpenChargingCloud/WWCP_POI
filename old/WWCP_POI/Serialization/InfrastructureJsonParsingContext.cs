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

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Resolves ID-only references to JSON documents. Resolved entities are parsed into the
    /// new hierarchy, so no mutable entities or parent links are shared with the source network.
    /// Returning null reports an unresolved reference. Expanded ancestor objects are rejected.
    /// </summary>
    public sealed class InfrastructureJsonParsingContext
    {

        /// <summary>
        /// Resolve a brand document.
        /// </summary>
        public Func<Brand_Id, JObject?>? ResolveBrand { get; init; }

        /// <summary>
        /// Resolve a charging tariff document.
        /// </summary>
        public Func<ChargingTariff_Id, JObject?>? ResolveChargingTariff { get; init; }

        /// <summary>
        /// Resolve an EVSE document.
        /// </summary>
        public Func<EVSE_Id, JObject?>? ResolveEVSE { get; init; }

        /// <summary>
        /// Resolve a charging station document.
        /// </summary>
        public Func<ChargingStation_Id, JObject?>? ResolveChargingStation { get; init; }

        /// <summary>
        /// Resolve a charging pool document.
        /// </summary>
        public Func<ChargingPool_Id, JObject?>? ResolveChargingPool { get; init; }

        /// <summary>
        /// Resolve a charging station operator document.
        /// </summary>
        public Func<ChargingStationOperator_Id, JObject?>? ResolveChargingStationOperator { get; init; }

        /// <summary>
        /// Resolve an e-mobility provider document.
        /// </summary>
        public Func<EMobilityProvider_Id, JObject?>? ResolveEMobilityProvider { get; init; }

    }

}
