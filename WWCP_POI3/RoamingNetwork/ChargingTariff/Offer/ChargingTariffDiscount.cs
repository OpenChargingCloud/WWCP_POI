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

    /// <summary>
    /// The discount of a charging tariff offer: it reduces a price by a percentage, the price components of the dimensions
    /// it names, or, naming none, all of them and the minimum and maximum price.
    /// </summary>
    public sealed class ChargingTariffDiscount : IEquatable<ChargingTariffDiscount>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/chargingTariffDiscount/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context      = "@context";

            /// <summary>
            /// The property of its percentage.
            /// </summary>
            public const String  Percent      = "percent";

            /// <summary>
            /// The property of the dimensions it reduces.
            /// </summary>
            public const String  Dimensions   = "dimensions";

        }

        #endregion


        /// <summary>
        /// The percentage a price is reduced by: above 0, at most 100.
        /// </summary>
        public Decimal                                  Percent       { get; }

        /// <summary>
        /// The dimensions of the price components it reduces, in the order of their names; none for all of them and the
        /// minimum and maximum price.
        /// </summary>
        public ImmutableArray<ChargingDimensionTypes>   Dimensions    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new discount of a charging tariff offer.
        /// </summary>
        /// <param name="Percent">The percentage a price is reduced by: above 0, at most 100.</param>
        /// <param name="Dimensions">The dimensions of the price components it reduces; none for all of them and the minimum and maximum price.</param>
        /// <exception cref="ArgumentException">A percentage out of range, or a dimension given twice.</exception>
        public ChargingTariffDiscount(Decimal                               Percent,
                                      IEnumerable<ChargingDimensionTypes>?  Dimensions   = null)
        {

            if (Percent <= 0 || Percent > 100)
                throw new ArgumentException($"The percentage of a discount must be above 0 and at most 100, not {Percent}!", nameof(Percent));

            var dimensions = (Dimensions ?? []).ToArray();

            if (dimensions.Distinct().Count() != dimensions.Length)
                throw new ArgumentException("A dimension of a discount must not be given twice!", nameof(Dimensions));

            this.Percent     = Percent;
            this.Dimensions  = [.. dimensions.OrderBy(dimension => dimension.ToString(), StringComparer.Ordinal)];

        }

        #endregion


        #region Reduces(Dimension)

        /// <summary>
        /// Whether it reduces the price components of the given dimension.
        /// </summary>
        public Boolean Reduces(ChargingDimensionTypes Dimension)

            => Dimensions.IsEmpty || Dimensions.Contains(Dimension);

        #endregion

        #region ToJSON(Embedded = false, CustomChargingTariffDiscountSerializer = null)

        /// <summary>
        /// Return the JSON representation of this discount.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingTariffDiscountSerializer">A delegate to serialize custom discounts.</param>
        public JObject ToJSON(Boolean                                                   Embedded                                 = false,
                              CustomJObjectSerializerDelegate<ChargingTariffDiscount>?  CustomChargingTariffDiscountSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,     JSONLDContext);

            json.Add(PropertyKeys.Percent,         POIJSON.Number(Percent));

            if (Dimensions.Length > 0)
                json.Add(PropertyKeys.Dimensions,  new JArray(Dimensions.Select(dimension => dimension.ToString())));

            return CustomChargingTariffDiscountSerializer is not null
                       ? CustomChargingTariffDiscountSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out Discount, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a discount of a charging tariff offer.
        /// </summary>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out ChargingTariffDiscount?  Discount,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)
        {

            Discount = null;

            if (JSON[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the discount must be '{JSONLDContext}'!";
                return false;
            }

            if (JSON[PropertyKeys.Percent] is not JValue { Type: JTokenType.Integer or JTokenType.Float } percentJSON)
            {
                ErrorResponse = "The percentage of a discount must be a number!";
                return false;
            }

            Decimal percent;

            try
            {
                percent = percentJSON.Value<Decimal>();
            }
            catch (OverflowException)
            {
                ErrorResponse = "The percentage of a discount must be above 0 and at most 100!";
                return false;
            }

            if (!POIJSON.TryParseIds(JSON,
                                     PropertyKeys.Dimensions,
                                     "dimension of a discount",
                                     (String text, out ChargingDimensionTypes dimension) => POIJSON.TryParseName(text, out dimension),
                                     out ImmutableArray<ChargingDimensionTypes> dimensions,
                                     out ErrorResponse))
            {
                return false;
            }

            if (JSON[PropertyKeys.Dimensions] is JArray { Count: 0 })
            {
                ErrorResponse = "The dimensions of a discount must not be empty: without them, it reduces all of them!";
                return false;
            }

            try
            {
                Discount = new ChargingTariffDiscount(percent, dimensions);
            }
            catch (ArgumentException e)
            {
                ErrorResponse = e.Message;
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region IEquatable<ChargingTariffDiscount>

        /// <inheritdoc/>
        public Boolean Equals(ChargingTariffDiscount? Other)

            => Other is not null &&
               Percent == Other.Percent &&
               Dimensions.SequenceEqual(Other.Dimensions);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffDiscount other && Equals(other);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Percent.GetHashCode() ^ Dimensions.Length;

        #endregion

    }

}
