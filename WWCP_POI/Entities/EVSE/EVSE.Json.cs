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
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class EVSE
    {

        #region Parse/TryParse

        /// <summary>
        /// Parse an EVSE into the supplied station without registering it.
        /// </summary>
        public static EVSE Parse(JObject                             JSON,
                                 ChargingStation                     ChargingStation,
                                 CustomJObjectParserDelegate<EVSE>?  CustomEVSEParser = null,
                                 InfrastructureJsonParsingContext?   Context          = null)
        {

            if (TryParse(JSON, ChargingStation, out var evse, out var error, CustomEVSEParser, Context))
                return evse;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to reconstruct an EVSE, including hardware, tariff references and current statuses.
        /// </summary>
        public static Boolean TryParse(JObject                             JSON,
                                       ChargingStation                     ChargingStation,
                                       [NotNullWhen(true)]  out EVSE?      evse,
                                       [NotNullWhen(false)] out String?    error,
                                       CustomJObjectParserDelegate<EVSE>?  CustomEVSEParser = null,
                                       InfrastructureJsonParsingContext?   Context          = null)
        {

            evse  = null;
            error = null;

            try
            {
                ArgumentNullException.ThrowIfNull(ChargingStation);
                InfrastructureJson.Validate(JSON, JSONLDContext);
                if (CustomEVSEParser is null)
                    InfrastructureJson.ValidateFields(JSON, "@id", "@context", "name", "description", "dataSource", "customData",
                                                      "created", "lastChange", "status", "adminStatus", "physicalReference",
                                                      "roamingNetworkId", "roamingNetwork", "chargingStationOperatorId", "chargingStationOperator",
                                                      "chargingPoolId", "chargingPool", "chargingStationId", "chargingStation",
                                                      "geoLocation", "brandId", "brand", "dataLicenses", "dataLicenseIds",
                                                      "chargingModes", "currentType", "maxVoltage", "maxCurrent", "maxPower", "maxCapacity",
                                                      "energyMeter", "isFreeOfCharge", "socketOutlets", "tariffIds",
                                                      "address", "authenticationModes", "openingTimes");

                var idText = InfrastructureJson.Text(JSON, "@id");

                if (idText is null || !EVSE_Id.TryParse(idText, out var id))
                    throw new ArgumentException("@id: invalid or missing EVSE identifier.");

                if (id.OperatorId != ChargingStation.Id.OperatorId)
                    throw new ArgumentException("@id: EVSE operator does not match the station operator.");

                InfrastructureJson.Parent(JSON, "chargingStation",         ChargingStation.Id.ToString());
                InfrastructureJson.Parent(JSON, "chargingPool",            ChargingStation.ChargingPool?.Id.ToString());
                InfrastructureJson.Parent(JSON, "chargingStationOperator", ChargingStation.Operator?.Id.ToString());
                InfrastructureJson.Parent(JSON, "roamingNetwork",          ChargingStation.RoamingNetwork?.Id.ToString());

                var connectors = InfrastructureJson.Array(JSON, "socketOutlets",
                                                          token => ChargingConnector.Parse(InfrastructureJson.Entry(token)));

                InfrastructureJson.Unique(connectors, connector => connector.Id, "socketOutlets");

                var currentType = JSON["currentType"] is not null
                                      ? InfrastructureJson.Array(JSON, "currentType", InfrastructureJson.Flags<CurrentTypes>).Reduce()
                                      : CurrentTypes.AC_ThreePhases;

                var modes    = InfrastructureJson.Array(JSON, "chargingModes", InfrastructureJson.Flags<ChargingModes>);
                var voltage  = MetrologyJson.Read<Volt>(JSON, "maxVoltage", Volt.TryParse);
                var amperage = MetrologyJson.Read<Ampere>(JSON, "maxCurrent", Ampere.TryParse);
                var power    = MetrologyJson.Read<Watt>(JSON, "maxPower", Watt.TryParse);
                var capacity = MetrologyJson.Read<WattHour>(JSON, "maxCapacity", WattHour.TryParse);

                if (voltage?.Value < 0 || amperage?.Value < 0 || power?.Value < 0 || capacity?.Value < 0)
                    throw new ArgumentException("Electrical limits must not be negative.");

                ValidateInheritedStationData(JSON, ChargingStation);

                var parsed = new EVSE(id,
                                      ChargingStation,
                                      Name:               InfrastructureJson.Name(JSON, "name"),
                                      Description:        InfrastructureJson.Name(JSON, "description"),
                                      PhysicalReference:  InfrastructureJson.Text(JSON, "physicalReference"),
                                      GeoLocation:        InfrastructureJson.Location(JSON),
                                      Brands:             InfrastructureJson.Brands(JSON, "brandId", "brand", Context),
                                      DataLicenses:       InfrastructureJson.Licenses(JSON),
                                      ChargingModes:      modes,
                                      CurrentType:        currentType,
                                      MaxVoltage:         voltage,
                                      MaxCurrent:         amperage,
                                      MaxPower:           power,
                                      MaxCapacity:        capacity,
                                      EnergyMeter:        ParseEVSEEnergyMeter(JSON),
                                      IsFreeOfCharge:     InfrastructureJson.Boolean(JSON, "isFreeOfCharge"),
                                      ChargingConnectors: connectors,
                                      DataSource:         InfrastructureJson.Text(JSON, "dataSource"),
                                      CustomData:         InfrastructureJson.CustomData(JSON));

                InfrastructureJson.RestoreMetadata(JSON, parsed, EVSEAdminStatusType.TryParse, EVSEStatusType.TryParse);

                parsed.ChargingTariffIds = ParseEVSETariffReferences(JSON);

                evse = CustomEVSEParser is null
                           ? parsed
                           : CustomEVSEParser(JSON, parsed) ??
                             throw new ArgumentException("The custom EVSE parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                evse  = null;
                error = $"EVSE: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Validate inherited station data

        private static void ValidateInheritedStationData(JObject JSON, ChargingStation Station)
        {

            // Standalone EVSE exports include station data, which must agree with the supplied parent.
            foreach (var field in new[] { "address", "authenticationModes", "openingTimes" })
            {
                if (JSON[field] is not { } inherited)
                    continue;

                JToken? expected = field switch
                {
                    "address"             => (Station.Address ?? Station.ChargingPool?.Address)?.ToJSON(Embedded: true),
                    "authenticationModes" => Station.AuthenticationModes.ToJSON(),
                    _                     => Station.OpeningTimes?.ToJSON()
                };

                if (!JToken.DeepEquals(inherited, expected))
                    throw new ArgumentException($"{field}: inherited data does not match the supplied station.");
            }

        }

        #endregion

        #region Parse energy meter and tariff references

        private static EnergyMeter? ParseEVSEEnergyMeter(JObject JSON)

            => InfrastructureJson.Object(JSON, "energyMeter") is { } meter
                   ? InfrastructureJson.At("energyMeter", () => POI.EnergyMeter.Parse(meter))
                   : null;

        private static ImmutableArray<ChargingTariff_Id> ParseEVSETariffReferences(JObject JSON)
        {

            var tariffs = InfrastructureJson.Array(JSON, "tariffIds", token =>
            {
                if (token.Type != JTokenType.String || !ChargingTariff_Id.TryParse(token.Value<String>()!, out var tariff))
                    throw new ArgumentException("Invalid tariff identifier.");

                return tariff;
            });

            var identities = tariffs.Select(id => InfrastructureChangeSchema.Identity(InfrastructureEntityType.ChargingTariff, id.ToString()));

            if (identities.Distinct().Count() != tariffs.Count)
                throw new ArgumentException("tariffIds: duplicate tariff reference.");

            return tariffs.ToImmutableArray();

        }

        #endregion

    }

}
