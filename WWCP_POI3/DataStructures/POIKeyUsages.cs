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

using Newtonsoft.Json.Linq;

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
        /// The charge transparency records of a charging station operator, e.g. the signed documents of its charging sessions:
        /// signed outside the history, apart from its price statements, with a key a verifier takes from a verified proof of
        /// the station, as WWCP_POI.TS verifyChargingDocumentKeys does.
        /// </summary>
        public const String ChargeTransparencyRecords = "chargeTransparencyRecords";

        /// <summary>
        /// The receipts of an independent witness that it retrieved a price publicly (PricePublicationReceipt), as WWCP_POI.TS
        /// takes them: its key, named in a witness publication policy of a verifier, signs nothing of the history.
        /// </summary>
        public const String WitnessPublications       = "witnessPublications";

        /// <summary>
        /// The models of a charging station or energy meter manufacturer.
        /// </summary>
        public const String Models           = "models";

        /// <summary>
        /// The transparency software releases of a transparency software manufacturer.
        /// </summary>
        public const String Software         = "software";

        /// <summary>
        /// The certificates of a conformity assessment body.
        /// </summary>
        public const String Certificates     = "certificates";

        /// <summary>
        /// The EV Taler merchant of a charging station operator (concepts/EVTaler.md, section 4.4) and the web payment page
        /// of a charging station operator, a charging pool, a charging station and their templates: whoever changes them
        /// redirects the payments of the stations, so its key may be kept apart, e.g. offline.
        /// </summary>
        public const String Payments         = "payments";

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
                                                                                       ChargeTransparencyRecords,
                                                                                       WitnessPublications,
                                                                                       Models,
                                                                                       Software,
                                                                                       Certificates,
                                                                                       Payments,
                                                                                       Party);

        #region (internal) Of(Change)

        /// <summary>
        /// The usages a signing key needs, all of them, to sign the given change: the one of what it changes (UsageOf) and,
        /// where it adds a charging pool, a charging station or their templates with a web payment page, its stations'
        /// included, "payments" as well, as whoever names a web payment page redirects payments.
        /// </summary>
        internal static ImmutableArray<String> Of(RoamingNetworkChange Change)
        {

            var usage = UsageOf(Change);

            return usage != Payments && AddsWebPaymentURL(Change)
                       ? [ usage, Payments ]
                       : [ usage ];

        }

        #endregion

        #region (private) UsageOf(Change)

        /// <summary>
        /// The usage of what the given change changes, by the type of the entity it changes; by the property it changes,
        /// "payments" for the EV Taler merchant of a charging station operator and for the web payment page of an operator,
        /// a charging pool, a charging station or their templates, and "party" for the other properties of an operator.
        /// </summary>
        private static String UsageOf(RoamingNetworkChange Change)

            => Change.EntityType switch {

                   nameof(ChargingStationOperator)
                       when Change.EntityPropertyName == ChargingStationOperator.PropertyKeys.EVTaler => Payments,

                   // "webPaymentURL" of each of them alike.
                   nameof(ChargingStationOperator) or
                   nameof(ChargingPool)            or
                   nameof(ChargingStation)         or
                   nameof(ChargingPoolTemplate)    or
                   nameof(ChargingStationTemplate)
                       when Change.EntityPropertyName == ChargingStationOperator.PropertyKeys.WebPaymentURL => Payments,

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

                   nameof(ChargingStationModel)    or
                   nameof(EnergyMeterModel)        => Models,

                   nameof(TransparencySoftware)    => Software,

                   nameof(ConformityCertificate) => Certificates,

                   _                               => Party

               };

        #endregion

        #region (private) AddsWebPaymentURL(Change)

        /// <summary>
        /// Whether the given change adds a charging pool, a charging station or their templates naming a web payment page,
        /// a pool by one of its stations as well. A charging station operator is added by the roaming network operator, with
        /// its web payment page as with its EV Taler merchant.
        /// </summary>
        private static Boolean AddsWebPaymentURL(RoamingNetworkChange Change)
        {

            if (Change.Kind != RoamingNetworkChangeKind.Add || Change.NewValue is not JObject entity)
                return false;

            return Change.EntityType switch {

                       nameof(ChargingPool)            => entity[ChargingPool.PropertyKeys.WebPaymentURL] is not null ||
                                                          (entity[ChargingPool.PropertyKeys.ChargingStations] is JArray stations &&
                                                           stations.OfType<JObject>().Any(station => station[ChargingStation.PropertyKeys.WebPaymentURL] is not null)),

                       nameof(ChargingStation)         => entity[ChargingStation.        PropertyKeys.WebPaymentURL] is not null,
                       nameof(ChargingPoolTemplate)    => entity[ChargingPoolTemplate.   PropertyKeys.WebPaymentURL] is not null,
                       nameof(ChargingStationTemplate) => entity[ChargingStationTemplate.PropertyKeys.WebPaymentURL] is not null,

                       _                               => false

                   };

        }

        #endregion

    }

}
