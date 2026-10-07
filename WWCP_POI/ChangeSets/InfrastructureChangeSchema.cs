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

using System.Globalization;

using Newtonsoft.Json.Linq;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Defines hierarchy relationships, identifier semantics and editable entity properties.
    /// </summary>
    internal static class InfrastructureChangeSchema
    {

        #region Hierarchy relationships

        internal static readonly IReadOnlyDictionary<InfrastructureEntityType, (String Field, InfrastructureEntityType Parent)> Relations =
            new Dictionary<InfrastructureEntityType, (String, InfrastructureEntityType)>
            {
                [InfrastructureEntityType.ChargingStationOperator] = ("chargingStationOperators", InfrastructureEntityType.RoamingNetwork),
                [InfrastructureEntityType.EMobilityProvider]        = ("eMobilityProviders",        InfrastructureEntityType.RoamingNetwork),
                [InfrastructureEntityType.ChargingTariff]           = ("chargingTariffs",           InfrastructureEntityType.ChargingStationOperator),
                [InfrastructureEntityType.ChargingPool]             = ("chargingPools",             InfrastructureEntityType.ChargingStationOperator),
                [InfrastructureEntityType.ChargingStation]          = ("chargingStations",          InfrastructureEntityType.ChargingPool),
                [InfrastructureEntityType.EVSE]                     = ("EVSEs",                     InfrastructureEntityType.ChargingStation),
                [InfrastructureEntityType.ChargingConnector]        = ("socketOutlets",             InfrastructureEntityType.EVSE)
            };

        #endregion

        #region Entity types and identifier spelling

        internal static InfrastructureEntityType Type(String value)

            => Enum.TryParse<InfrastructureEntityType>(value, out var type) &&
               Enum.IsDefined(type) &&
               type.ToString() == value
                   ? type
                   : throw new ArgumentException($"Unsupported entity type '{value}'.");

        internal static String Id(InfrastructureEntityType  type,
                                  String                    text)

            => type switch
            {
                InfrastructureEntityType.RoamingNetwork           => RoamingNetwork_Id.          Parse(text).ToString(),
                InfrastructureEntityType.ChargingStationOperator => ChargingStationOperator_Id.Parse(text).ToString(),
                InfrastructureEntityType.EMobilityProvider        => EMobilityProvider_Id.       Parse(text).ToString(),
                InfrastructureEntityType.ChargingPool             => ChargingPool_Id.            Parse(text).ToString(),
                InfrastructureEntityType.ChargingStation          => ChargingStation_Id.         Parse(text).ToString(),
                InfrastructureEntityType.EVSE                     => EVSE_Id.                    Parse(text).ToString(),
                InfrastructureEntityType.ChargingConnector        => ChargingConnector_Id.       Parse(text).ToString(),
                InfrastructureEntityType.ChargingTariff           => ChargingTariff_Id.          Parse(text).ToString(),
                _                                                => throw new ArgumentOutOfRangeException(nameof(type))
            };

        #endregion

        #region Identifier equality

        // Preserve wire spelling while comparing identifiers according to their domain semantics.
        // Some dependency ID hash implementations do not agree with their case-insensitive equality.
        internal static String Identity(InfrastructureEntityType  type,
                                        String                    text)

            => type switch
            {
                InfrastructureEntityType.RoamingNetwork           => RoamingNetwork_Id.Parse(text).ToString().ToUpperInvariant(),
                InfrastructureEntityType.ChargingStationOperator => ChargingStationOperator_Id.Parse(text).ToString(OperatorIdFormats.ISO_STAR).ToUpperInvariant(),
                InfrastructureEntityType.EMobilityProvider        => ProviderIdentity(EMobilityProvider_Id.Parse(text)),
                InfrastructureEntityType.ChargingPool             => PoolIdentity(ChargingPool_Id.Parse(text)),
                InfrastructureEntityType.ChargingStation          => StationIdentity(ChargingStation_Id.Parse(text)),
                InfrastructureEntityType.EVSE                     => EVSEIdentity(EVSE_Id.Parse(text)),
                InfrastructureEntityType.ChargingConnector        => ChargingConnector_Id.Parse(text).ToString(),
                InfrastructureEntityType.ChargingTariff           => TariffIdentity(ChargingTariff_Id.Parse(text)),
                _                                                => throw new ArgumentOutOfRangeException(nameof(type))
            };

        private static String ProviderIdentity(EMobilityProvider_Id id)

            => id.CountryCode.Alpha2Code + ":" + id.Suffix;

        private static String PoolIdentity(ChargingPool_Id id)

            => OperatorEntityIdentity(id.OperatorId, id.Suffix);

        private static String StationIdentity(ChargingStation_Id id)

            => OperatorEntityIdentity(id.OperatorId, id.Suffix);

        private static String EVSEIdentity(EVSE_Id id)

            => OperatorEntityIdentity(id.OperatorId, id.Suffix);

        private static String TariffIdentity(ChargingTariff_Id id)

            => OperatorEntityIdentity(id.OperatorId, id.Suffix);
        private static String OperatorEntityIdentity(ChargingStationOperator_Id  operatorId,
                                                     String                      suffix)

            => operatorId.ToString(OperatorIdFormats.ISO_STAR).ToUpperInvariant() + ":" +
               suffix.Replace("*", "").ToUpperInvariant();

        internal static Boolean SameId(InfrastructureEntityType  type,
                                       String                    left,
                                       String                    right)

            => Identity(type, left) == Identity(type, right);

        #endregion

        #region Editable properties

        private static readonly HashSet<String> Common =
        [
            "name", "description", "dataSource", "customData", "status", "adminStatus"
        ];

        private static readonly IReadOnlyDictionary<InfrastructureEntityType, HashSet<String>> Fields =
            new Dictionary<InfrastructureEntityType, HashSet<String>>
            {
                [InfrastructureEntityType.RoamingNetwork] =
                [
                    "dataLicenses", "dataLicenseIds"
                ],
                [InfrastructureEntityType.ChargingStationOperator] =
                [
                    "address", "logos", "homepage", "hotline", "brands", "dataLicenses", "dataLicenseIds"
                ],
                [InfrastructureEntityType.EMobilityProvider] =
                [
                    "address", "logos", "homepage", "hotline", "dataLicenses", "dataLicenseIds", "priority"
                ],
                [InfrastructureEntityType.ChargingPool] =
                [
                    "address", "geoLocation", "locationType", "accessibility", "authenticationModes",
                    "hotlinePhoneNumber", "openingTimes", "brands", "dataLicenses", "dataLicenseIds",
                    "energyMeters", "gridConnectionPoint",
                    "timeZone", "chargingWhenClosed", "locationLanguages", "facilities", "services", "relatedLocations", "mobilityRootCAs", "evRoamingPartners"
                ],
                [InfrastructureEntityType.ChargingStation] =
                [
                    "address", "geoLocation", "authenticationModes", "hotlinePhoneNumber", "openingTimes",
                    "isFreeOfCharge", "brands", "dataLicenses", "dataLicenseIds", "energyMeters",
                    "chargingWhenClosed", "accessibility", "locationLanguage", "physicalReference", "paymentOptions", "features", "vehicleTypes", "images", "serviceIdentification", "modelCode", "published", "disabled", "mobilityRootCAs", "evRoamingPartners", "certificationInfo", "calibrationInfo"
                ],
                [InfrastructureEntityType.EVSE] =
                [
                    "physicalReference", "geoLocation", "brand", "isFreeOfCharge", "chargingModes",
                    "currentType", "maxVoltage", "maxCurrent", "maxPower", "maxCapacity",
                    "energyMeter", "dataLicenses", "dataLicenseIds", "tariffIds",
                    "photoURLs", "mobilityRootCAs", "energyMix", "calibrationInfo"
                ],
                [InfrastructureEntityType.ChargingTariff] =
                [
                    "elements", "currency", "brand", "uri", "energyMix"
                ],
                [InfrastructureEntityType.ChargingConnector] =
                [
                    "type", "cable", "lockable", "tariffIds", "termsAndConditions"
                ]
            };

        internal static void Property(InfrastructureEntityType  type,
                                      String                    name)
        {

            if (!Fields[type].Contains(name) &&
                (type == InfrastructureEntityType.ChargingConnector || !Common.Contains(name)))
            {
                throw new ArgumentException($"Property '{name}' is not an editable JSON property of {type}. IDs, parents, children and revision metadata cannot be updated as properties.");
            }

        }

        internal static void ValidateFields(InfrastructureEntityType  type,
                                            JObject                   document)
        {

            foreach (var property in document.Properties())
            {
                if (property.Name is "@id" or "@context" or "created" or "lastChange" or "ETags")
                    continue;

                if (type == InfrastructureEntityType.RoamingNetwork && property.Name is "revision" or "appliedChangeSetId")
                    continue;

                if (Relations.Any(relation => relation.Value.Parent == type && relation.Value.Field == property.Name))
                    continue;

                Property(type, property.Name);
            }

        }

        #endregion

        #region Initialize metadata for added entities

        internal static void InitializeMetadata(InfrastructureEntityType  type,
                                                JObject                   document,
                                                DateTimeOffset            timestamp)
        {

            if (type == InfrastructureEntityType.ChargingConnector)
                return;

            var time = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

            var status = type switch
            {
                InfrastructureEntityType.ChargingTariff    => "Unspecified",
                InfrastructureEntityType.EMobilityProvider => "Available",
                _                                         => "available"
            };

            var adminStatus = type switch
            {
                InfrastructureEntityType.ChargingTariff    => "Unspecified",
                InfrastructureEntityType.EMobilityProvider => "Operational",
                _                                         => "operational"
            };

            document["created"] = (InfrastructureJson.Date(document, "created") ?? timestamp).
                                      ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            document["lastChange"] = (InfrastructureJson.Date(document, "lastChange") ?? timestamp).
                                         ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            document["status"]      ??= new JObject(new JProperty("value", status),      new JProperty("timestamp", time));
            document["adminStatus"] ??= new JObject(new JProperty("value", adminStatus), new JProperty("timestamp", time));

        }

        #endregion

    }

}
