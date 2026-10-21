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

    /// <summary>
    /// The least or the most a charging session with a charging tariff costs, as in OCPI: an immutable value
    /// without an identification, in the currency of the tariff.
    /// </summary>
    /// <param name="ExcludingVAT">The amount excluding VAT.</param>
    /// <param name="IncludingVAT">The amount including VAT, unless unknown; not less than the amount excluding it.</param>
    public sealed record ChargingPriceLimit(Decimal   ExcludingVAT,
                                            Decimal?  IncludingVAT   = null)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/chargingPriceLimit";

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context        = "@context";

            /// <summary>
            /// The property of its amount excluding VAT.
            /// </summary>
            public const String  ExcludingVAT   = "excludingVAT";

            /// <summary>
            /// The property of its amount including VAT.
            /// </summary>
            public const String  IncludingVAT   = "includingVAT";

        }

        #endregion


        #region Validate()

        /// <summary>
        /// The error, when this price limit is invalid.
        /// </summary>
        internal String? Validate()
        {

            if (ExcludingVAT < 0)
                return "A price limit excluding VAT must not be negative!";

            if (IncludingVAT < ExcludingVAT)
                return "A price limit including VAT must not be less than the one excluding VAT!";

            return null;

        }

        #endregion

        #region ToJSON(Embedded = false, CustomChargingPriceLimitSerializer = null)

        /// <summary>
        /// Return the JSON representation of this price limit.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingPriceLimitSerializer">A delegate to serialize custom price limits.</param>
        public JObject ToJSON(Boolean                                               Embedded                             = false,
                              CustomJObjectSerializerDelegate<ChargingPriceLimit>?  CustomChargingPriceLimitSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,       JSONLDContext);

            json.Add(PropertyKeys.ExcludingVAT,      POIJSON.Number(ExcludingVAT));

            if (IncludingVAT.HasValue)
                json.Add(PropertyKeys.IncludingVAT,  POIJSON.Number(IncludingVAT.Value));

            return CustomChargingPriceLimitSerializer is not null
                       ? CustomChargingPriceLimitSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingPriceLimit, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a price limit.
        /// </summary>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out ChargingPriceLimit?  ChargingPriceLimit,
                                       [NotNullWhen(false)] out String?              ErrorResponse)
        {

            ChargingPriceLimit = null;

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the price limit must be '{JSONLDContext}'!";
                return false;
            }

            if (!JSON.ParseMandatory(PropertyKeys.ExcludingVAT,
                                     "price limit excluding VAT",
                                     out Decimal excludingVAT,
                                     out ErrorResponse))
            {
                return false;
            }

            if (JSON.ParseOptional(PropertyKeys.IncludingVAT,"price limit including VAT", out Decimal? includingVAT, out ErrorResponse))
                if (ErrorResponse is not null) return false;

            var priceLimit = new ChargingPriceLimit(excludingVAT, includingVAT);

            if (priceLimit.Validate() is String error)
            {
                ErrorResponse = error;
                return false;
            }

            ChargingPriceLimit  = priceLimit;
            ErrorResponse       = null;
            return true;

        }

        #endregion

    }

}
