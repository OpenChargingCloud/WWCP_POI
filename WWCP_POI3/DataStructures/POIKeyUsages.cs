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

using System.Collections.Immutable;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What a signing key of a party may sign (SPECIFICATION.md, section 4.1): a signing key naming no usages signs
    /// everything its party owns, one naming usages only what they name, e.g. a charging station operator signing its
    /// pools with one key and its tariffs with another. Identity keys name no usages.
    /// </summary>
    public static class POIKeyUsages
    {

        /// <summary>
        /// Charging pools, stations, EVSEs and their templates and groups, energy meter templates and groups, grid
        /// connection points and the parking data.
        /// </summary>
        public const String Infrastructure   = "infrastructure";

        /// <summary>
        /// Charging tariffs, their templates and groups, and charging tariff offers.
        /// </summary>
        public const String Tariffs          = "tariffs";

        /// <summary>
        /// The signed prices at runtime (SPECIFICATION.md, section 12).
        /// </summary>
        public const String PriceStatements  = "priceStatements";

        /// <summary>
        /// The transparency software releases of a transparency software manufacturer.
        /// </summary>
        public const String Software         = "software";

        /// <summary>
        /// The party itself: its own properties and, by a signing key, its signing keys; of the roaming network operator
        /// also the roaming network, its registries, who is a member, commits and attestations.
        /// </summary>
        public const String Party            = "party";

        /// <summary>
        /// All usages a signing key may name.
        /// </summary>
        public static readonly ImmutableHashSet<String> All = ImmutableHashSet.Create(StringComparer.Ordinal,
                                                                                       Infrastructure,
                                                                                       Tariffs,
                                                                                       PriceStatements,
                                                                                       Software,
                                                                                       Party);

        #region (internal) Of(Change)

        /// <summary>
        /// The usage a signing key needs to sign the given change, by the type of the entity it changes.
        /// </summary>
        internal static String Of(RoamingNetworkChange Change)

            => Change.EntityType switch {

                   nameof(ChargingTariff)          or
                   nameof(ChargingTariffTemplate)  or
                   nameof(ChargingTariffGroup)     or
                   nameof(ChargingTariffOffer)     => Tariffs,

                   nameof(ChargingPool)            or
                   nameof(ChargingStation)         or
                   nameof(EVSE)                    or
                   nameof(EVSETemplate)            or
                   nameof(ChargingStationTemplate) or
                   nameof(ChargingPoolTemplate)    or
                   nameof(EVSEGroup)               or
                   nameof(ChargingStationGroup)    or
                   nameof(ChargingPoolGroup)       or
                   nameof(EnergyMeterTemplate)     or
                   nameof(EnergyMeterGroup)        or
                   nameof(GridConnectionPoint)     or
                   nameof(ParkingGarage)           or
                   nameof(ParkingSpace)            or
                   nameof(ParkingSensor)           or
                   nameof(ParkingProduct)          or
                   nameof(ParkingSpaceTemplate)    or
                   nameof(ParkingSpaceGroup)       => Infrastructure,

                   nameof(TransparencySoftware)    => Software,

                   _                               => Party

               };

        #endregion

    }

}
