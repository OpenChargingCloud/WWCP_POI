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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class ChargingStationOperator
    {

        #region Parse/TryParse

        /// <summary>
        /// Parse an operator and its tariffs and pools into the supplied network without registering it.
        /// </summary>
        public static ChargingStationOperator Parse(JObject                                                JSON,
                                                    RoamingNetwork                                         RoamingNetwork,
                                                    CustomJObjectParserDelegate<ChargingStationOperator>?  CustomChargingStationOperatorParser = null,
                                                    InfrastructureJsonParsingContext?                      Context                             = null)
        {

            if (TryParse(JSON,
                         RoamingNetwork,
                         out var result,
                         out var error,
                         CustomChargingStationOperatorParser,
                         Context))
            {
                return result;
            }

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to reconstruct an operator hierarchy, including lookup caches and tariff assignments.
        /// </summary>
        public static Boolean TryParse(JObject                                                JSON,
                                       RoamingNetwork                                         RoamingNetwork,
                                       [NotNullWhen(true)]  out ChargingStationOperator?      result,
                                       [NotNullWhen(false)] out String?                       error,
                                       CustomJObjectParserDelegate<ChargingStationOperator>?  CustomChargingStationOperatorParser = null,
                                       InfrastructureJsonParsingContext?                      Context                             = null)
        {

            result = null;
            error  = null;

            try
            {
                ArgumentNullException.ThrowIfNull(RoamingNetwork);
                InfrastructureJson.Validate(JSON, JSONLDContext);

                var text = InfrastructureJson.Text(JSON, "@id");

                if (text is null || !ChargingStationOperator_Id.TryParse(text, out var id))
                    throw new ArgumentException("@id: invalid or missing operator identifier.");

                InfrastructureJson.Parent(JSON, "roamingNetwork", RoamingNetwork.Id.ToString());

                foreach (var field in new[] { "chargingStations", "EVSEs" })
                    InfrastructureJson.RejectReferences(JSON, field);

                var parsed = new ChargingStationOperator(id,
                                                         RoamingNetwork,
                                                         Name:        InfrastructureJson.Name(JSON, "name"),
                                                         Description: InfrastructureJson.Name(JSON, "description"),
                                                         DataSource:  InfrastructureJson.Text(JSON, "dataSource"),
                                                         CustomData:  InfrastructureJson.CustomData(JSON))
                {
                    address            = InfrastructureJson.Address(JSON),
                    homepage           = InfrastructureJson.Scalar<URL>(JSON, "homepage", URL.TryParse),
                    hotlinePhoneNumber = InfrastructureJson.Scalar<PhoneNumber>(JSON, "hotline", PhoneNumber.TryParse)
                };

                parsed.ParseOperatorLogo(JSON);
                parsed.immutableDataLicenses = parsed.immutableDataLicenses.AddRange(InfrastructureJson.Licenses(JSON));

                foreach (var brand in InfrastructureJson.Brands(JSON, context: Context))
                    parsed.immutableBrands = parsed.immutableBrands.Add(brand);

                var tariffIds = parsed.ParseOperatorTariffs(JSON, Context);

                parsed.ParseOperatorPools(JSON, Context);
                parsed.ValidateOperatorReferences(JSON, tariffIds);

                InfrastructureJson.RestoreMetadata(JSON,
                                                   parsed,
                                                   ChargingStationOperatorAdminStatusTypes.TryParse,
                                                   ChargingStationOperatorStatusTypes.TryParse);

                result = CustomChargingStationOperatorParser is null
                             ? parsed
                             : CustomChargingStationOperatorParser(JSON, parsed) ??
                               throw new ArgumentException("The custom operator parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                result = null;
                error  = $"ChargingStationOperator: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Parse operator logo

        private void ParseOperatorLogo(JObject JSON)
        {

            var logos = InfrastructureJson.Array(JSON, "logos", token =>
            {
                var document = InfrastructureJson.Entry(token);
                var uri      = InfrastructureJson.Text(document, "uri");

                if (uri is null || !URL.TryParse(uri, out var logo))
                    throw new ArgumentException("Invalid logo URI.");

                return logo;
            });

            if (logos.Count > 1)
                throw new ArgumentException("logos: this operator supports one logo.");

            if (logos.Count == 1)
                logo = logos[0];

        }

        #endregion

        #region Parse tariffs and pools

        private HashSet<String> ParseOperatorTariffs(JObject JSON, InfrastructureJsonParsingContext? Context)
        {

            var tariffs = InfrastructureJson.Entities(JSON,
                                                      "chargingTariffIds",
                                                      "chargingTariffs",
                                                      ChargingTariff_Id.TryParse,
                                                      Context?.ResolveChargingTariff,
                                                      document => ChargingTariff.Parse(document, this, Context: Context));

            var tariffIds = new HashSet<String>(StringComparer.Ordinal);

            foreach (var tariff in tariffs)
            {
                var identity = InfrastructureChangeSchema.Identity(InfrastructureEntityType.ChargingTariff, tariff.Id.ToString());

                if (!tariffIds.Add(identity))
                    throw new ArgumentException($"chargingTariffs: duplicate identifier '{tariff.Id}'.");

                chargingTariffs.TryAdd(tariff.Id, tariff);
            }

            return tariffIds;

        }

        private void ParseOperatorPools(JObject JSON, InfrastructureJsonParsingContext? Context)
        {

            var pools = InfrastructureJson.Entities(JSON,
                                                    "chargingPoolIds",
                                                    "chargingPools",
                                                    ChargingPool_Id.TryParse,
                                                    Context?.ResolveChargingPool,
                                                    document => ChargingPool.Parse(document, this, Context: Context));

            InfrastructureJson.Unique(pools, pool => pool.Id, "chargingPools");

            foreach (var pool in pools)
            {
                if (chargingPools.TryAdd(pool).Result != CommandResult.Success)
                    throw new ArgumentException($"chargingPools: failed to attach '{pool.Id}'.");

                foreach (var station in pool.ChargingStations)
                {
                    if (!chargingStationLookup.TryAdd(station.Id, station))
                        throw new ArgumentException($"chargingStations: duplicate identifier '{station.Id}' across pools.");
                }

                foreach (var evse in pool.EVSEs)
                {
                    if (!evseLookup.TryAdd(evse.Id, evse))
                        throw new ArgumentException($"EVSEs: duplicate identifier '{evse.Id}' across stations.");
                }
            }

        }

        #endregion

        #region Validate operator references

        private void ValidateOperatorReferences(JObject JSON, HashSet<String> TariffIds)
        {

            InfrastructureJson.References(JSON, "chargingStationIds", ChargingStation_Id.TryParse, ChargingStations.Select(station => station.Id));
            InfrastructureJson.References(JSON, "EVSEIds",            EVSE_Id.TryParse,            EVSEs.           Select(evse    => evse.Id));

            foreach (var evse in EVSEs)
            {
                var assignedTariffIds = evse.ChargingTariffIds.Concat(
                                            evse.ChargingConnectors.SelectMany(connector => connector.TariffIds));

                foreach (var tariffId in assignedTariffIds)
                {
                    var identity = InfrastructureChangeSchema.Identity(InfrastructureEntityType.ChargingTariff, tariffId.ToString());

                    if (!TariffIds.Contains(identity))
                        throw new ArgumentException($"EVSEs[{evse.Id}].tariffIds: tariff '{tariffId}' is not registered with this operator.");
                }
            }

        }

        #endregion

    }

}
