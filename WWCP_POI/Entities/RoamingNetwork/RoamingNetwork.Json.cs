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
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class RoamingNetwork
    {

        #region Parse/TryParse JSON text

        /// <summary>
        /// Parse a nested roaming network document, retaining decimal precision in tariff prices.
        /// </summary>
        /// <param name="JSON">The JSON text to parse.</param>
        /// <param name="CustomRoamingNetworkParser">An optional parser for additional roaming network data.</param>
        /// <param name="Context">Optional resolvers for ID-only references.</param>
        public static RoamingNetwork Parse(String                                        JSON,
                                           CustomJObjectParserDelegate<RoamingNetwork>?  CustomRoamingNetworkParser = null,
                                           InfrastructureJsonParsingContext?             Context                    = null)

            => Parse(InfrastructureJson.ReadObject(JSON),
                     CustomRoamingNetworkParser,
                     Context);

        /// <summary>
        /// Try to parse JSON text without returning a partially reconstructed network.
        /// </summary>
        public static Boolean TryParse(String                                    JSON,
                                       [NotNullWhen(true)]  out RoamingNetwork?  network,
                                       [NotNullWhen(false)] out String?          error,
                                       InfrastructureJsonParsingContext?         Context = null)
        {

            network = null;
            error   = null;

            try
            {
                return TryParse(InfrastructureJson.ReadObject(JSON),
                                out network,
                                out error,
                                null,
                                Context);
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

        }

        #endregion

        #region Parse/TryParse JSON objects

        /// <summary>
        /// Parse a nested roaming network document, including current statuses and revision metadata.
        /// </summary>
        /// <param name="JSON">The JSON object to parse.</param>
        /// <param name="CustomRoamingNetworkParser">An optional parser for additional roaming network data.</param>
        /// <param name="Context">Optional resolvers for ID-only references.</param>
        public static RoamingNetwork Parse(JObject                                       JSON,
                                           CustomJObjectParserDelegate<RoamingNetwork>?  CustomRoamingNetworkParser = null,
                                           InfrastructureJsonParsingContext?             Context                    = null)
        {

            if (TryParse(JSON,
                         out var network,
                         out var error,
                         CustomRoamingNetworkParser,
                         Context))
            {
                return network;
            }

            throw new ArgumentException($"Invalid roaming network JSON: {error}", nameof(JSON));

        }

        /// <summary>
        /// Try to parse a nested roaming network document without custom parsing or reference resolvers.
        /// </summary>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out RoamingNetwork?  network,
                                       [NotNullWhen(false)] out String?          error)

            => TryParse(JSON,
                        out network,
                        out error,
                        null);

        /// <summary>
        /// Try to reconstruct the nested hierarchy. Unresolved references and flattened objects
        /// fail validation; the supplied JSON and resolver-owned documents are not modified.
        /// </summary>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out RoamingNetwork?      network,
                                       [NotNullWhen(false)] out String?              error,
                                       CustomJObjectParserDelegate<RoamingNetwork>?  CustomRoamingNetworkParser,
                                       InfrastructureJsonParsingContext?             Context                    = null)
        {

            network = null;
            error   = null;

            try
            {
                InfrastructureJson.Validate(JSON, JSONLDContext);

                var parsed = ParseNetworkProperties(JSON);

                parsed.ParseDataLicenses(JSON);
                parsed.ParseChargingStationOperators(JSON, Context);
                parsed.ParseEMobilityProviders(JSON, Context);
                parsed.ParseNetworkChildren(JSON);
                parsed.ValidateInfrastructureReferences(JSON);

                InfrastructureJson.RestoreMetadata(JSON,
                                                   parsed,
                                                   RoamingNetworkAdminStatusType.TryParse,
                                                   RoamingNetworkStatusType.TryParse);

                parsed.RestoreVersionedSnapshot(JSON);

                network = CustomRoamingNetworkParser is null
                              ? parsed
                              : CustomRoamingNetworkParser(JSON, parsed) ??
                                throw new ArgumentException("The custom roaming network parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                network = null;
                error   = exception.Message;
                return false;
            }

        }

        #endregion

        #region Parse network properties

        private static RoamingNetwork ParseNetworkProperties(JObject JSON)
        {

            if (!JSON.ParseMandatory("@id",
                                     "roaming network identification",
                                     RoamingNetwork_Id.TryParse,
                                     out RoamingNetwork_Id id,
                                     out var error))
            {
                throw new ArgumentException(error);
            }

            // An unnamed network is represented by an empty language object.
            if (JSON["name"] is not JObject nameJSON)
                throw new ArgumentException("'name' must be a language object.");

            I18NString? name = I18NString.Empty;

            if (nameJSON.HasValues && !I18NString.TryParse(nameJSON, out name, out error))
                throw new ArgumentException(error);

            JSON.ParseOptionalJSON("description",
                                   "network description",
                                   I18NString.TryParse,
                                   out I18NString? description,
                                   out error);

            if (error is not null)
                throw new ArgumentException(error);

            if (JSON["dataSource"] is { Type: not JTokenType.Null } source && source.Type != JTokenType.String)
                throw new ArgumentException("'dataSource' must be a string.");

            return new RoamingNetwork(id,
                                      name,
                                      description,
                                      DataSource: JSON["dataSource"]?.Value<String>(),
                                      CustomData: InfrastructureJson.CustomData(JSON));

        }

        #endregion

        #region Parse data licenses

        private void ParseDataLicenses(JObject JSON)
        {

            foreach (var field in new[] { "dataLicenseIds", "dataLicenses" })
            {
                if (JSON[field] is not { } token)
                    continue;

                if (token is not JArray array)
                    throw new ArgumentException($"'{field}' must be an array.");

                foreach (var entry in array)
                {
                    var license = ParseDataLicense(entry, field);

                    if (!dataLicenses.TryAdd(license.Id, license))
                        throw new ArgumentException($"Duplicate data license '{license.Id}'.");
                }
            }

        }

        private static DataLicense ParseDataLicense(JToken Entry, String Field)
        {

            if (Field == "dataLicenseIds" &&
                Entry.Type == JTokenType.String &&
                DataLicense_Id.TryParse(Entry.Value<String>()!, out var licenseId))
            {
                return new DataLicense(licenseId);
            }

            if (Field != "dataLicenses" || Entry is not JObject document)
                throw new ArgumentException($"Invalid entry in '{Field}'.");

            var copy = (JObject) document.DeepClone();

            copy["id"]   ??= copy["@id"]?.DeepClone();
            copy["URLs"] ??= new JArray();

            if (!DataLicense.TryParse(copy, out var license, out var error) || license is null)
                throw new ArgumentException($"Invalid data license: {error}");

            return license;

        }

        #endregion

        #region Parse operators and providers

        private void ParseChargingStationOperators(JObject JSON, InfrastructureJsonParsingContext? Context)
        {

            var operators = InfrastructureJson.Entities(JSON,
                                                        "chargingStationOperatorIds",
                                                        "chargingStationOperators",
                                                        ChargingStationOperator_Id.TryParse,
                                                        Context?.ResolveChargingStationOperator,
                                                        document => ChargingStationOperator.Parse(document, this, Context: Context));

            InfrastructureJson.Unique(operators, chargingStationOperator => chargingStationOperator.Id, "chargingStationOperators");

            foreach (var chargingStationOperator in operators)
                chargingStationOperators.TryAdd(chargingStationOperator.Id, chargingStationOperator);

        }

        private void ParseEMobilityProviders(JObject JSON, InfrastructureJsonParsingContext? Context)
        {

            var providers = InfrastructureJson.Entities(JSON,
                                                       "eMobilityProviderIds",
                                                       "eMobilityProviders",
                                                       EMobilityProvider_Id.TryParse,
                                                       Context?.ResolveEMobilityProvider,
                                                       document => EMobilityProvider.Parse(document, this));

            InfrastructureJson.Unique(providers, provider => provider.Id, "eMobilityProviders");

            foreach (var provider in providers)
                eMobilityProviders.TryAdd(provider.Id, provider);

        }

        #endregion

        #region Validate infrastructure references

        private void ValidateInfrastructureReferences(JObject JSON)
        {

            foreach (var field in new[] { "chargingPools", "chargingStations", "EVSEs" })
            {
                if (JSON[field] is not { } token)
                    continue;

                if (token is not JArray array)
                    throw new ArgumentException($"'{field}' must be an array.");

                if (array.Count > 0)
                    throw new ArgumentException($"'{field}': flattened objects are not supported; supply the nested hierarchy.");
            }

            InfrastructureJson.References(JSON, "chargingPoolIds",    ChargingPool_Id.TryParse,    ChargingPools.   Select(pool    => pool.Id));
            InfrastructureJson.References(JSON, "chargingStationIds", ChargingStation_Id.TryParse, ChargingStations.Select(station => station.Id));
            InfrastructureJson.References(JSON, "EVSEIds",            EVSE_Id.TryParse,            EVSEs.           Select(evse    => evse.Id));

        }

        #endregion

    }

}
