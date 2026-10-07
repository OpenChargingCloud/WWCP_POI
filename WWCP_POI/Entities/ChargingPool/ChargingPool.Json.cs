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

    public sealed partial class ChargingPool
    {

        #region Parse/TryParse

        /// <summary>
        /// Parse a pool and its stations into the supplied operator without registering it.
        /// </summary>
        public static ChargingPool Parse(JObject                                     JSON,
                                         ChargingStationOperator                     Operator,
                                         CustomJObjectParserDelegate<ChargingPool>?  CustomChargingPoolParser = null,
                                         InfrastructureJsonParsingContext?           Context                  = null)
        {

            if (TryParse(JSON, Operator, out var pool, out var error, CustomChargingPoolParser, Context))
                return pool;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to reconstruct a pool, including its nested stations and EVSE references.
        /// </summary>
        public static Boolean TryParse(JObject                                     JSON,
                                       ChargingStationOperator                     Operator,
                                       [NotNullWhen(true)]  out ChargingPool?      pool,
                                       [NotNullWhen(false)] out String?            error,
                                       CustomJObjectParserDelegate<ChargingPool>?  CustomChargingPoolParser = null,
                                       InfrastructureJsonParsingContext?           Context                  = null)
        {

            pool  = null;
            error = null;

            try
            {
                ArgumentNullException.ThrowIfNull(Operator);
                InfrastructureJson.Validate(JSON, JSONLDContext);

                var idText = InfrastructureJson.Text(JSON, "@id");

                if (idText is null || !ChargingPool_Id.TryParse(idText, out var id))
                    throw new ArgumentException("@id: invalid or missing charging pool identifier.");

                if (id.OperatorId != Operator.Id)
                    throw new ArgumentException("@id: pool operator does not match the supplied operator.");

                InfrastructureJson.Parent(JSON, "chargingStationOperator", Operator.Id.ToString());
                InfrastructureJson.Parent(JSON, "roamingNetwork",          Operator.RoamingNetwork?.Id.ToString());
                InfrastructureJson.RejectReferences(JSON, "EVSEs");

                var parsed = new ChargingPool(id,
                                              Operator,
                                              Name:               InfrastructureJson.Name(JSON, "name"),
                                              Description:        InfrastructureJson.Name(JSON, "description"),
                                              Address:            InfrastructureJson.Address(JSON),
                                              GeoLocation:        InfrastructureJson.Location(JSON),
                                              OpeningTimes:       InfrastructureJson.Openings(JSON),
                                              ParkingType:        InfrastructureJson.Scalar<ParkingType>(JSON, "locationType", POI.ParkingType.TryParse),
                                              Accessibility:      InfrastructureJson.Scalar<AccessibilityType>(JSON, "accessibility", AccessibilityType.TryParse),
                                              HotlinePhoneNumber: InfrastructureJson.Scalar<PhoneNumber>(JSON, "hotlinePhoneNumber", PhoneNumber.TryParse),
                                              Brands:             InfrastructureJson.Brands(JSON, context: Context),
                                              DataLicenses:       InfrastructureJson.Licenses(JSON),
                                              DataSource:         InfrastructureJson.Text(JSON, "dataSource"),
                                              CustomData:         InfrastructureJson.CustomData(JSON),
                                              EnergyMeters:       InfrastructureJson.Array(JSON, "energyMeters",
                                                                                          token => EnergyMeter.Parse(InfrastructureJson.Entry(token))),
                                              GridConnectionPoint: InfrastructureJson.Object(JSON, "gridConnectionPoint") is { } point
                                                                       ? InfrastructureJson.At("gridConnectionPoint", () => POI.GridConnectionPoint.Parse(point, Operator.RoamingNetwork))
                                                                       : null);

                var authenticationModes = InfrastructureJson.Array(JSON, "authenticationModes",
                                                                   token => POI.AuthenticationModes.Parse(InfrastructureJson.Entry(token)));

                foreach (var mode in authenticationModes)
                    parsed.immutableAuthenticationModes = parsed.immutableAuthenticationModes.Add(mode);

                parsed.ParsePoolStations(JSON, Context);

                InfrastructureJson.References(JSON, "EVSEIds", EVSE_Id.TryParse, parsed.EVSEs.Select(evse => evse.Id));
                InfrastructureJson.RestoreMetadata(JSON, parsed, ChargingPoolAdminStatusType.TryParse, ChargingPoolStatusType.TryParse);

                pool = CustomChargingPoolParser is null
                           ? parsed
                           : CustomChargingPoolParser(JSON, parsed) ??
                             throw new ArgumentException("The custom pool parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                pool  = null;
                error = $"ChargingPool: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Parse charging stations

        private void ParsePoolStations(JObject JSON, InfrastructureJsonParsingContext? Context)
        {

            var stations = InfrastructureJson.Entities(JSON,
                                                       "chargingStationIds",
                                                       "chargingStations",
                                                       ChargingStation_Id.TryParse,
                                                       Context?.ResolveChargingStation,
                                                       document => ChargingStation.Parse(document, this, Context: Context));

            InfrastructureJson.Unique(stations, station => station.Id, "chargingStations");

            foreach (var station in stations)
            {
                if (chargingStations.TryAdd(station).Result != CommandResult.Success)
                    throw new ArgumentException($"chargingStations: failed to attach '{station.Id}'.");
            }

        }

        #endregion

    }

}
