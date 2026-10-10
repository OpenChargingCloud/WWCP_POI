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
                [InfrastructureEntityType.ChargingConnector]        = ("socketOutlets",             InfrastructureEntityType.EVSE),
                [InfrastructureEntityType.EVSEGroup]                 = ("EVSEGroups",                InfrastructureEntityType.ChargingStationOperator),
                [InfrastructureEntityType.ChargingStationGroup]     = ("chargingStationGroups",     InfrastructureEntityType.ChargingStationOperator),
                [InfrastructureEntityType.ChargingPoolGroup]        = ("chargingPoolGroups",        InfrastructureEntityType.ChargingStationOperator),
                [InfrastructureEntityType.ChargingTariffGroup]      = ("chargingTariffGroups",      InfrastructureEntityType.ChargingStationOperator),
                [InfrastructureEntityType.ChargingStationManufacturer] = ("chargingStationManufacturers", InfrastructureEntityType.RoamingNetwork),
                [InfrastructureEntityType.GridOperator]             = ("gridOperators",             InfrastructureEntityType.RoamingNetwork),
                [InfrastructureEntityType.ParkingOperator]          = ("parkingOperators",          InfrastructureEntityType.RoamingNetwork),
                [InfrastructureEntityType.ParkingGarage]            = ("parkingGarages",            InfrastructureEntityType.ParkingOperator),
                [InfrastructureEntityType.ParkingSpace]             = ("parkingSpaces",             InfrastructureEntityType.ParkingOperator),
                [InfrastructureEntityType.ParkingSensor]            = ("parkingSensors",            InfrastructureEntityType.ParkingOperator),
                [InfrastructureEntityType.ParkingSpaceGroup]        = ("parkingSpaceGroups",        InfrastructureEntityType.ParkingOperator),
                [InfrastructureEntityType.ParkingProduct]           = ("parkingProducts",           InfrastructureEntityType.ParkingOperator),
                [InfrastructureEntityType.TransparencySoftware]     = ("transparencySoftware",      InfrastructureEntityType.RoamingNetwork),
                [InfrastructureEntityType.TransparencySoftwareCertificate] = ("transparencySoftwareCertificates", InfrastructureEntityType.RoamingNetwork)
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
                InfrastructureEntityType.EVSEGroup                 => EVSEGroup_Id.Parse(text).ToString(),
                InfrastructureEntityType.ChargingStationGroup     => ChargingStationGroup_Id.Parse(text).ToString(),
                InfrastructureEntityType.ChargingPoolGroup        => ChargingPoolGroup_Id.Parse(text).ToString(),
                InfrastructureEntityType.ChargingTariffGroup      => ChargingTariffGroup_Id.Parse(text).ToString(),
                InfrastructureEntityType.ChargingStationManufacturer => ChargingStationManufacturer_Id.Parse(text).ToString(),
                InfrastructureEntityType.GridOperator             => GridOperator_Id.Parse(text).ToString(),
                InfrastructureEntityType.ParkingOperator          => ParkingOperator_Id.Parse(text).ToString(),
                InfrastructureEntityType.ParkingGarage            => ParkingGarage_Id.Parse(text).ToString(),
                InfrastructureEntityType.ParkingSpace             => ParkingSpace_Id.Parse(text).ToString(),
                InfrastructureEntityType.ParkingSensor            => ParkingSensor_Id.Parse(text).ToString(),
                InfrastructureEntityType.ParkingSpaceGroup        => ParkingSpaceGroup_Id.Parse(text).ToString(),
                InfrastructureEntityType.ParkingProduct           => ParkingProduct_Id.Parse(text).ToString(),
                InfrastructureEntityType.TransparencySoftware     => TransparencySoftware_Id.Parse(text).ToString(),
                InfrastructureEntityType.TransparencySoftwareCertificate => TransparencySoftwareCertificate_Id.Parse(text).ToString(),
                _                                                => throw new ArgumentOutOfRangeException(nameof(type))
            };

        #endregion

        #region Identifier equality

        // Preserve wire spelling while comparing identifiers according to their domain semantics.
        internal static String Identity(InfrastructureEntityType  type,
                                        String                    text)

            => type switch
            {
                InfrastructureEntityType.RoamingNetwork           => RoamingNetwork_Id.Parse(text).ToString().ToUpperInvariant(),
                InfrastructureEntityType.ChargingStationOperator => ChargingStationOperator_Id.Parse(text).ToString().ToUpperInvariant(),
                InfrastructureEntityType.EMobilityProvider        => ProviderIdentity(EMobilityProvider_Id.Parse(text)),
                InfrastructureEntityType.ChargingPool             => PoolIdentity(ChargingPool_Id.Parse(text)),
                InfrastructureEntityType.ChargingStation          => StationIdentity(ChargingStation_Id.Parse(text)),
                InfrastructureEntityType.EVSE                     => EVSEIdentity(EVSE_Id.Parse(text)),
                InfrastructureEntityType.ChargingConnector        => ChargingConnector_Id.Parse(text).ToString(),
                InfrastructureEntityType.ChargingTariff           => TariffIdentity(ChargingTariff_Id.Parse(text)),
                InfrastructureEntityType.EVSEGroup                 => GroupIdentity(EVSEGroup_Id.Parse(text).OperatorId, EVSEGroup_Id.Parse(text).Suffix),
                InfrastructureEntityType.ChargingStationGroup     => GroupIdentity(ChargingStationGroup_Id.Parse(text).OperatorId, ChargingStationGroup_Id.Parse(text).Suffix),
                InfrastructureEntityType.ChargingPoolGroup        => GroupIdentity(ChargingPoolGroup_Id.Parse(text).OperatorId, ChargingPoolGroup_Id.Parse(text).Suffix),
                InfrastructureEntityType.ChargingTariffGroup      => GroupIdentity(ChargingTariffGroup_Id.Parse(text).OperatorId, ChargingTariffGroup_Id.Parse(text).Suffix),
                InfrastructureEntityType.ChargingStationManufacturer => Id(type, text).ToUpperInvariant(),
                InfrastructureEntityType.GridOperator             => GridOperator_Id.Parse(text).CountryCode.Alpha2Code + ":" + GridOperator_Id.Parse(text).Suffix,
                InfrastructureEntityType.ParkingOperator          => Id(type, text).ToUpperInvariant(),
                InfrastructureEntityType.ParkingGarage or InfrastructureEntityType.ParkingSpace or
                InfrastructureEntityType.ParkingSensor or InfrastructureEntityType.ParkingSpaceGroup or
                InfrastructureEntityType.ParkingProduct or InfrastructureEntityType.TransparencySoftware or
                InfrastructureEntityType.TransparencySoftwareCertificate => Id(type, text),
                _                                                => throw new ArgumentOutOfRangeException(nameof(type))
            };

        private static String ProviderIdentity(EMobilityProvider_Id id)

            => id.CountryCode.Alpha2Code + ":" + id.Suffix;

        private static String GroupIdentity(ChargingStationOperator_Id operatorId, String suffix)
            => operatorId.ToString().ToUpperInvariant() + ":" + suffix;

        internal static String IdField(InfrastructureEntityType type)
            => type is InfrastructureEntityType.GridOperator or InfrastructureEntityType.ParkingOperator ? "id" : "@id";

        internal static Boolean HasMetadata(InfrastructureEntityType type)
            => type is not (InfrastructureEntityType.ChargingConnector or InfrastructureEntityType.ChargingStationManufacturer or
                            InfrastructureEntityType.ParkingProduct or InfrastructureEntityType.TransparencySoftware or
                            InfrastructureEntityType.TransparencySoftwareCertificate);

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

            => operatorId.ToString().ToUpperInvariant() + ":" +
               suffix.Replace("*", "").ToUpperInvariant();

        internal static Boolean SameId(InfrastructureEntityType  type,
                                       String                    left,
                                       String                    right)

            => Identity(type, left) == Identity(type, right);

        #endregion

        #region Editable properties

        private static readonly HashSet<String> Common =
        [
            "name", "description", "dataSource", "customData"
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
                    "address", "geoLocation", "telephone", "eMailAddress", "logos", "homepage", "hotline", "termsAndConditions",
                    "brands", "dataLicenses", "dataLicenseIds"
                ],
                [InfrastructureEntityType.EMobilityProvider] =
                [
                    "address", "geoLocation", "telephone", "eMailAddress", "logos", "homepage", "hotline",
                    "dataLicenses", "dataLicenseIds", "priority"
                ],
                [InfrastructureEntityType.ChargingPool] =
                [
                    "address", "geoLocation", "locationType", "accessibility", "authenticationModes",
                    "hotlinePhoneNumber", "openingTimes", "brands", "dataLicenses", "dataLicenseIds",
                    "energyMeters", "gridConnectionPoint", "maxCurrent", "maxPower", "maxCapacity",
                    "timeZone", "chargingWhenClosed", "locationLanguages", "facilities", "services", "relatedLocations", "mobilityRootCAs", "evRoamingPartners"
                ],
                [InfrastructureEntityType.ChargingStation] =
                [
                    "address", "geoLocation", "authenticationModes", "hotlinePhoneNumber", "openingTimes",
                    "isFreeOfCharge", "brands", "dataLicenses", "dataLicenseIds", "energyMeters", "maxCurrent", "maxPower", "maxCapacity",
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
                ],
                [InfrastructureEntityType.EVSEGroup] = ["EVSEIds", "allowedMemberIds", "brand", "priority", "tariffId", "dataLicenses"],
                [InfrastructureEntityType.ChargingStationGroup] = ["chargingStationIds", "allowedMemberIds", "brand", "priority", "tariffId", "dataLicenses"],
                [InfrastructureEntityType.ChargingPoolGroup] = ["chargingPoolIds", "allowedMemberIds", "brand", "priority", "tariffId", "dataLicenses"],
                [InfrastructureEntityType.ChargingTariffGroup] = ["description", "chargingTariffIds"],
                [InfrastructureEntityType.ChargingStationManufacturer] = ["name", "description", "cryptoKeys"],
                [InfrastructureEntityType.GridOperator] = ["logos", "address", "geoLocation", "telephone", "eMailAddress", "homepage", "hotline", "priority", "dataLicenses"],
                [InfrastructureEntityType.ParkingOperator] = ["logos", "address", "geoLocation", "telephone", "eMailAddress", "homepage", "hotlinePhoneNumber", "dataLicenses", "invalidParkingSpaceIds", "localParkingSpaceIds"],
                [InfrastructureEntityType.ParkingGarage] = ["osmWayId", "geometry", "chargingStationIds", "parkingProductIds"],
                [InfrastructureEntityType.ParkingSpace] = ["osmWayId", "geometry", "chargingStationIds", "sensors", "parkingGarageId", "parkingProductIds"],
                [InfrastructureEntityType.ParkingSensor] = ["osmWayId", "geometry", "chargingStationIds"],
                [InfrastructureEntityType.ParkingSpaceGroup] = ["osmWayId", "geometry", "chargingStationIds", "sensors", "parkingSpaceIds", "parkingProductIds"],
                [InfrastructureEntityType.ParkingProduct] = ["minDuration", "stopParkingAfterTime"],
                [InfrastructureEntityType.TransparencySoftware] = ["name", "version", "openSourceLicenses", "vendor", "logo", "howToUse", "moreInformation", "sourceCodeRepository"],
                [InfrastructureEntityType.TransparencySoftwareCertificate] = ["issuer", "chargingStationModel", "chargingStationModelVersion",
                    "chargingStationManufacturerId", "documentNumber", "documentURL", "verifiedTransparencySoftwareIds",
                    "compatibleTransparencySoftwareIds", "notBefore", "notAfter"]
            };

        internal static void Property(InfrastructureEntityType  type,
                                      String                    name)
        {

            var simple = type is InfrastructureEntityType.ChargingStationManufacturer or InfrastructureEntityType.ChargingTariffGroup or
                                 InfrastructureEntityType.EVSEGroup or InfrastructureEntityType.ChargingStationGroup or
                                 InfrastructureEntityType.ChargingPoolGroup or InfrastructureEntityType.ParkingGarage or
                                 InfrastructureEntityType.ParkingSpace or InfrastructureEntityType.ParkingSensor or InfrastructureEntityType.ParkingSpaceGroup or
                                 InfrastructureEntityType.ParkingProduct or InfrastructureEntityType.TransparencySoftware or InfrastructureEntityType.TransparencySoftwareCertificate;

            if (!Fields[type].Contains(name) &&
                (type == InfrastructureEntityType.ChargingConnector || type == InfrastructureEntityType.ChargingTariffGroup || type == InfrastructureEntityType.ParkingProduct ||
                 type == InfrastructureEntityType.TransparencySoftware || type == InfrastructureEntityType.TransparencySoftwareCertificate || simple && name is not ("name" or "description") || !Common.Contains(name)))
            {
                throw new ArgumentException($"Property '{name}' is not an editable JSON property of {type}. IDs, parents, children and revision metadata cannot be updated as properties.");
            }

        }

        internal static void ValidateFields(InfrastructureEntityType  type,
                                            JObject                   document)
        {

            foreach (var property in document.Properties())
            {
                if (property.Name == IdField(type) || property.Name is "@context" or "ETags" or POIContentProfile.PropertyName ||
                    HasMetadata(type) && property.Name is "created" or "lastChange")
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

            if (!HasMetadata(type))
                return;

            document["created"] = (InfrastructureJson.Date(document, "created") ?? timestamp).
                                      ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            document["lastChange"] = (InfrastructureJson.Date(document, "lastChange") ?? timestamp).
                                         ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        }

        #endregion

    }

}
