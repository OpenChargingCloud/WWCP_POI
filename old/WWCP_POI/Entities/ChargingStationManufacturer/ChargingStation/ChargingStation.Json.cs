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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class ChargingStation
    {

        #region Parse/TryParse

        /// <summary>
        /// Parse a station and its EVSEs into the supplied pool without registering it.
        /// </summary>
        public static ChargingStation Parse(JObject                                        JSON,
                                            ChargingPool?                                  ChargingPool                = null,
                                            CustomJObjectParserDelegate<ChargingStation>?  CustomChargingStationParser = null,
                                            InfrastructureJsonParsingContext?              Context                     = null)
        {

            if (TryParse(JSON, ChargingPool, out var station, out var error, CustomChargingStationParser, Context))
                return station;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to reconstruct a station, validating its supplied parents and nested EVSEs.
        /// </summary>
        public static Boolean TryParse(JObject                                        JSON,
                                       ChargingPool?                                  ChargingPool,
                                       [NotNullWhen(true)]  out ChargingStation?      station,
                                       [NotNullWhen(false)] out String?               error,
                                       CustomJObjectParserDelegate<ChargingStation>?  CustomChargingStationParser = null,
                                       InfrastructureJsonParsingContext?              Context                     = null)
        {

            station = null;
            error   = null;

            try
            {
                InfrastructureJson.Validate(JSON, JSONLDContext);

                var idText = InfrastructureJson.Text(JSON, "@id");

                if (idText is null || !ChargingStation_Id.TryParse(idText, out var id))
                    throw new ArgumentException("@id: invalid or missing charging station identifier.");

                if (ChargingPool is not null && id.OperatorId != ChargingPool.Id.OperatorId)
                    throw new ArgumentException("@id: station operator does not match the pool operator.");

                InfrastructureJson.Parent(JSON, "chargingPool",            ChargingPool?.Id.ToString());
                InfrastructureJson.Parent(JSON, "chargingStationOperator", ChargingPool?.Operator?.Id.ToString());
                InfrastructureJson.Parent(JSON, "roamingNetwork",          ChargingPool?.RoamingNetwork?.Id.ToString());

                var parsed = new ChargingStation(id,
                                                 ChargingPool,
                                                 Name:                InfrastructureJson.Name(JSON, "name"),
                                                 Description:         InfrastructureJson.Name(JSON, "description"),
                                                 Address:             InfrastructureJson.Address(JSON),
                                                 GeoLocation:         InfrastructureJson.Location(JSON),
                                                 OpeningTimes:        InfrastructureJson.Openings(JSON),
                                                 HotlinePhoneNumber:  InfrastructureJson.Scalar<PhoneNumber>(JSON, "hotlinePhoneNumber", PhoneNumber.TryParse),
                                                 AuthenticationModes: InfrastructureJson.Array(JSON, "authenticationModes",
                                                                                             token => POI.AuthenticationModes.Parse(InfrastructureJson.Entry(token))),
                                                 Brands:              InfrastructureJson.Brands(JSON, context: Context),
                                                 DataLicenses:        InfrastructureJson.Licenses(JSON),
                                                 DataSource:          InfrastructureJson.Text(JSON, "dataSource"),
                                                 CustomData:          InfrastructureJson.CustomData(JSON),
                                                 MaxCurrent:          MetrologyJson.Read<Ampere>(JSON, "maxCurrent", Ampere.TryParse),
                                                 MaxPower:            MetrologyJson.Read<Watt>(JSON, "maxPower", Watt.TryParse),
                                                 MaxCapacity:         MetrologyJson.Read<WattHour>(JSON, "maxCapacity", WattHour.TryParse),
                                                 EnergyMeters:        InfrastructureJson.Array(JSON, "energyMeters",
                                                                                             token => EnergyMeter.Parse(InfrastructureJson.Entry(token), Network: ChargingPool?.RoamingNetwork)));

                parsed.isFreeOfCharge = InfrastructureJson.Boolean(JSON, "isFreeOfCharge") ?? false;
                parsed.ParseStationEVSEs(JSON, Context);

                InfrastructureJson.RestoreMetadata(JSON, parsed, ChargingStationAdminStatusType.TryParse, ChargingStationStatusType.TryParse);

                station = CustomChargingStationParser is null
                              ? parsed
                              : CustomChargingStationParser(JSON, parsed) ??
                                throw new ArgumentException("The custom station parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                station = null;
                error   = $"ChargingStation: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Parse EVSEs

        private void ParseStationEVSEs(JObject JSON, InfrastructureJsonParsingContext? Context)
        {

            var children = InfrastructureJson.Entities(JSON,
                                                       "EVSEIds",
                                                       "EVSEs",
                                                       EVSE_Id.TryParse,
                                                       Context?.ResolveEVSE,
                                                       document => EVSE.Parse(document, this, Context: Context));

            InfrastructureJson.Unique(children, evse => evse.Id, "EVSEs");

            foreach (var evse in children)
            {
                if (evses.TryAdd(evse, Connect).Result != CommandResult.Success)
                    throw new ArgumentException($"EVSEs: failed to attach '{evse.Id}'.");
            }

        }

        #endregion

    }

}
