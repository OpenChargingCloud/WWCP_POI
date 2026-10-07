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

    public sealed partial class ChargingTariff
    {

        /// <summary>
        /// The JSON-LD context of a charging tariff.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/ChargingTariff";

        #region Parse/TryParse JSON text

        /// <summary>
        /// Parse tariff JSON text without converting decimal prices to binary floating point values.
        /// </summary>
        public static ChargingTariff Parse(String                                        JSON,
                                           ChargingStationOperator                       Operator,
                                           CustomJObjectParserDelegate<ChargingTariff>?  CustomChargingTariffParser = null,
                                           InfrastructureJsonParsingContext?             Context                    = null)

            => Parse(InfrastructureJson.ReadObject(JSON),
                     Operator,
                     CustomChargingTariffParser,
                     Context);

        /// <summary>
        /// Try to parse a tariff JSON string, retaining decimal price precision.
        /// </summary>
        public static Boolean TryParse(String                                    JSON,
                                       ChargingStationOperator                   Operator,
                                       [NotNullWhen(true)]  out ChargingTariff?  result,
                                       [NotNullWhen(false)] out String?          error,
                                       InfrastructureJsonParsingContext?         Context  = null)
        {

            result = null;
            error  = null;

            try
            {
                return TryParse(InfrastructureJson.ReadObject(JSON),
                                Operator,
                                out result,
                                out error,
                                Context: Context);
            }
            catch (Exception exception)
            {
                error = $"ChargingTariff: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Parse/TryParse JSON objects

        /// <summary>
        /// Parse a tariff into the supplied operator without registering it.
        /// </summary>
        public static ChargingTariff Parse(JObject                                       JSON,
                                           ChargingStationOperator                       Operator,
                                           CustomJObjectParserDelegate<ChargingTariff>?  CustomChargingTariffParser = null,
                                           InfrastructureJsonParsingContext?             Context                    = null)
        {

            if (TryParse(JSON, Operator, out var result, out var error, CustomChargingTariffParser, Context))
                return result;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to reconstruct tariff elements, brand, energy mix and current statuses.
        /// </summary>
        public static Boolean TryParse(JObject                                       JSON,
                                       ChargingStationOperator                       Operator,
                                       [NotNullWhen(true)]  out ChargingTariff?      result,
                                       [NotNullWhen(false)] out String?              error,
                                       CustomJObjectParserDelegate<ChargingTariff>?  CustomChargingTariffParser = null,
                                       InfrastructureJsonParsingContext?             Context                    = null)
        {

            result = null;
            error  = null;

            try
            {
                ArgumentNullException.ThrowIfNull(Operator);
                InfrastructureJson.Validate(JSON, JSONLDContext);
                InfrastructureJson.ValidateFields(JSON,
                                                  "@id", "@context", "name", "description",
                                                  "currency", "elements", "brand", "brandId", "uri", "energyMix",
                                                  "dataSource", "customData", "created", "lastChange", "status", "adminStatus",
                                                  "chargingStationOperatorId", "roamingNetworkId");

                var text = InfrastructureJson.Text(JSON, "@id");

                if (text is null || !ChargingTariff_Id.TryParse(text, out var id))
                    throw new ArgumentException("@id: invalid or missing tariff identifier.");

                InfrastructureJson.Parent(JSON, "chargingStationOperator", Operator.Id.ToString());
                InfrastructureJson.Parent(JSON, "roamingNetwork",          Operator.RoamingNetwork.Id.ToString());

                var currency = ParseTariffCurrency(JSON);
                var elements = InfrastructureJson.Array(JSON, "elements",
                                                        token => ChargingTariffElement.Parse(InfrastructureJson.Entry(token)));
                var brand    = ParseTariffBrand(JSON, Context);
                var uri      = InfrastructureJson.Text(JSON, "uri");

                if (uri is not null && !Uri.TryCreate(uri, UriKind.Absolute, out _))
                    throw new ArgumentException("uri: expected an absolute URI.");

                var parsed = new ChargingTariff(id,
                                               Operator,
                                               InfrastructureJson.Name(JSON, "name"),
                                               InfrastructureJson.Name(JSON, "description"),
                                               elements,
                                               currency,
                                               Brand:      brand,
                                               TariffURL:  InfrastructureJson.Scalar<URL>(JSON, "uri", URL.TryParse),
                                               EnergyMix:  InfrastructureJson.Object(JSON, "energyMix") is { } mix
                                                               ? POI.EnergyMix.Parse(mix)
                                                               : null,
                                               DataSource: InfrastructureJson.Text(JSON, "dataSource"),
                                               CustomData: InfrastructureJson.CustomData(JSON));

                InfrastructureJson.RestoreMetadata(JSON, parsed, ParseAdminStatus, ParseStatus);

                result = CustomChargingTariffParser is null
                             ? parsed
                             : CustomChargingTariffParser(JSON, parsed) ??
                               throw new ArgumentException("The custom tariff parser returned null.");

                return true;
            }
            catch (Exception exception)
            {
                error = $"ChargingTariff: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Parse currency and brand

        private static Currency ParseTariffCurrency(JObject JSON)
        {

            var text = InfrastructureJson.Text(JSON, "currency");

            if (text is null || !Currency.TryParseISO(text, out var currency))
                throw new ArgumentException("currency: expected a supported ISO 4217 code.");

            return currency;

        }

        private static Brand? ParseTariffBrand(JObject JSON, InfrastructureJsonParsingContext? Context)
        {

            // A tariff has one brand; adapt its singular representation to the shared reference resolver.
            var document = new JObject();

            if (InfrastructureJson.Object(JSON, "brand") is { } brand)
                document["brands"] = new JArray(brand.DeepClone());

            if (InfrastructureJson.Text(JSON, "brandId") is { } brandId)
                document["brandIds"] = new JArray(brandId);

            var brands = InfrastructureJson.Brands(document, context: Context);

            if (brands.Count > 1)
                throw new ArgumentException("brand: expected a single brand.");

            return brands.SingleOrDefault();

        }

        #endregion

        #region Parse tariff statuses

        private static Boolean ParseAdminStatus(String Text, out ChargingTariffAdminStatusTypes Value)
            => TariffJson.EnumValue(Text, out Value);

        private static Boolean ParseStatus(String Text, out ChargingTariffStatusTypes Value)
            => TariffJson.EnumValue(Text, out Value);

        #endregion

    }

}
