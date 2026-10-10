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

    public partial class EnergyMix
    {

        #region Parse/TryParse

        /// <summary>
        /// Parse the supplier, product and optional composition of an energy mix.
        /// </summary>
        public static EnergyMix Parse(JObject JSON)
        {

            if (TryParse(JSON, out var result, out var error))
                return result;

            throw new ArgumentException(error, nameof(JSON));

        }

        /// <summary>
        /// Try to reconstruct an energy mix with valid source and environmental impact percentages.
        /// </summary>
        public static Boolean TryParse(JObject                              JSON,
                                       [NotNullWhen(true)]  out EnergyMix?  result,
                                       [NotNullWhen(false)] out String?     error)
        {

            result = null;
            error  = null;

            try
            {
                InfrastructureJson.ValidateFields(JSON,
                                                  "energySources", "environmentalImpacts",
                                                  "supplierName", "productName", "additionalRemarks");

                if (JSON["energySources"] is not JArray || JSON["environmentalImpacts"] is not JArray)
                    throw new ArgumentException("energySources and environmentalImpacts: expected composition arrays; use [] for unknown composition.");

                var sources = InfrastructureJson.Array(JSON, "energySources",
                                                       token => ParsePercentage<EnergySourceCategories>(token, "source", EnergySourceCategories.TryParse));

                var impacts = InfrastructureJson.Array(JSON, "environmentalImpacts",
                                                       token => ParsePercentage<EnvironmentalImpacts>(token, "impact", POI.EnvironmentalImpacts.TryParse));

                var supplier = InfrastructureJson.Name(JSON, "supplierName") ??
                               throw new ArgumentException("supplierName: missing language object.");

                var product = InfrastructureJson.Name(JSON, "productName") ??
                              throw new ArgumentException("productName: missing language object.");

                var remarks = JSON["additionalRemarks"] is null
                                  ? null
                                  : InfrastructureJson.Name(JSON, "additionalRemarks");

                result = new EnergyMix(sources, impacts, supplier, product, remarks);

                return true;
            }
            catch (Exception exception)
            {
                error = $"EnergyMix: {exception.Message}";
                return false;
            }

        }

        #endregion

        #region Parse composition entries

        private static PercentageOf<T> ParsePercentage<T>(JToken                              Token,
                                                           String                              CategoryField,
                                                           JsonValueParsing.ScalarParser<T>    CategoryParser)
            where T : struct, IEquatable<T>
        {

            var document = InfrastructureJson.Entry(Token);

            InfrastructureJson.ValidateFields(document, CategoryField, "percentage");

            var text = InfrastructureJson.Text(document, CategoryField);

            if (text is null || !CategoryParser(text, out var category))
                throw new ArgumentException($"{CategoryField}: invalid category.");

            var percent = MetrologyJson.ReadPercent(document, "percentage");
            return new PercentageOf<T>(category, percent);

        }

        #endregion

    }

}
