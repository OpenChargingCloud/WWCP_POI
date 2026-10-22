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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The new prices of one charging tariff: its elements, which replace those of the tariff where they apply,
    /// e.g. an ad hoc price following the electricity market; the currency is that of the tariff.
    /// </summary>
    /// <param name="ChargingTariffId">The charging tariff whose prices these are.</param>
    /// <param name="Elements">The elements replacing those of the tariff, in their order; at least one.</param>
    public sealed class ChargingTariffPrices(ChargingTariff_Id                   ChargingTariffId,
                                             IEnumerable<ChargingTariffElement>  Elements)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingTariffPrices/v1";

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
            public const String  Context            = "@context";

            /// <summary>
            /// The property of its charging tariff identification.
            /// </summary>
            public const String  ChargingTariffId   = "chargingTariffId";

            /// <summary>
            /// The property of its charging tariff elements.
            /// </summary>
            public const String  Elements           = ChargingTariff.PropertyKeys.Elements;

        }

        #endregion


        /// <summary>
        /// The charging tariff whose prices these are.
        /// </summary>
        public ChargingTariff_Id                      ChargingTariffId  { get; } = ChargingTariffId;

        /// <summary>
        /// The elements replacing those of the tariff, in their order; at least one.
        /// </summary>
        public ImmutableArray<ChargingTariffElement>  Elements          { get; } = Elements?.Any() == true && Elements.All(element => element is not null)
                                                                                       ? [.. Elements]
                                                                                       : throw new ArgumentException($"The prices of the charging tariff '{ChargingTariffId}' need at least one element, and no null entries!", nameof(Elements));

        #endregion


        #region ToJSON(Embedded = false, CustomChargingTariffPricesSerializer = null)

        /// <summary>
        /// The JSON representation: { chargingTariffId, elements }.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingTariffPricesSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                                 Embedded                               = false,
                              CustomJObjectSerializerDelegate<ChargingTariffPrices>?  CustomChargingTariffPricesSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,           JSONLDContext);

            json.Add(PropertyKeys.ChargingTariffId,      ChargingTariffId.ToString());
            json.Add(PropertyKeys.Elements,              new JArray(Elements.Select(element => element.ToJSON(Embedded: true))));

            return CustomChargingTariffPricesSerializer is not null
                       ? CustomChargingTariffPricesSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingTariffPrices, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the new prices of a charging tariff.
        /// </summary>
        public static Boolean TryParse(JToken?                                         JSON,
                                       [NotNullWhen(true)]  out ChargingTariffPrices?  ChargingTariffPrices,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            ChargingTariffPrices = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = $"The prices of a charging tariff must be a JSON object {{ {PropertyKeys.ChargingTariffId}, {PropertyKeys.Elements} }}!";
                return false;
            }

            foreach (var property in json.Properties())
                if (property.Name is not (PropertyKeys.Context or PropertyKeys.ChargingTariffId or PropertyKeys.Elements))
                {
                    ErrorResponse = $"Unknown property '{property.Name}' of the prices of a charging tariff!";
                    return false;
                }

            if (json[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the prices of a charging tariff must be '{JSONLDContext}'!";
                return false;
            }

            if (json[PropertyKeys.ChargingTariffId]?.Type != JTokenType.String ||
                !ChargingTariff_Id.TryParse(json[PropertyKeys.ChargingTariffId]!.Value<String>() ?? "", out var chargingTariffId))
            {
                ErrorResponse = $"The prices of a charging tariff need a valid '{PropertyKeys.ChargingTariffId}'!";
                return false;
            }

            if (json[PropertyKeys.Elements] is not JArray elementsJSON || elementsJSON.Count == 0)
            {
                ErrorResponse = $"The prices of the charging tariff '{chargingTariffId}' need their elements as a JSON array, at least one!";
                return false;
            }

            var elements = new List<ChargingTariffElement>();

            foreach (var elementJSON in elementsJSON)
            {

                if (elementJSON is not JObject elementObject)
                {
                    ErrorResponse = "A charging tariff element must be a JSON object!";
                    return false;
                }

                if (!ChargingTariffElement.TryParse(elementObject, out var element, out ErrorResponse))
                    return false;

                elements.Add(element);

            }

            ChargingTariffPrices  = new ChargingTariffPrices(chargingTariffId, elements);
            ErrorResponse         = null;
            return true;

        }

        #endregion

    }


    /// <summary>
    /// The price status of a charging pool, charging station or EVSE: the new prices of charging tariffs of its
    /// operator, each tariff at most once; runtime data like the energy status, without a commit. No prices mean that
    /// the tariffs apply as they are.
    /// </summary>
    public sealed class ChargingPrices : IEquatable<ChargingPrices>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingPrices/v1";

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
            public const String  Context   = "@context";

            /// <summary>
            /// The property of its prices.
            /// </summary>
            public const String  Prices    = "prices";

        }

        #endregion


        /// <summary>
        /// The new prices, in the order of their charging tariffs.
        /// </summary>
        public ImmutableArray<ChargingTariffPrices>  Prices  { get; }

        /// <summary>
        /// The text: the compact JSON, so that equal price statuses have equal texts.
        /// </summary>
        public String                                Text    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new price status of the given prices.
        /// </summary>
        public ChargingPrices(IEnumerable<ChargingTariffPrices>? Prices = null)
        {

            var prices = (Prices ?? []).ToArray();

            if (prices.Any(price => price is null))
                throw new ArgumentException("A price status must not have null entries!", nameof(Prices));

            if (prices.Select(price => price.ChargingTariffId).Distinct().Count() != prices.Length)
                throw new ArgumentException("A price status must name a charging tariff only once!", nameof(Prices));

            this.Prices  = [.. prices.OrderBy(price => price.ChargingTariffId.ToString(), StringComparer.Ordinal)];
            this.Text    = ToJSON(Embedded: true).ToString(Formatting.None);

        }

        #endregion


        #region ToJSON(Embedded = false, CustomChargingPricesSerializer = null)

        /// <summary>
        /// The JSON representation: { prices: [ { chargingTariffId, elements }, … ] }.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingPricesSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                           Embedded                         = false,
                              CustomJObjectSerializerDelegate<ChargingPrices>?  CustomChargingPricesSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,  JSONLDContext);

            json.Add(PropertyKeys.Prices,       new JArray(Prices.Select(price => price.ToJSON(Embedded: true))));

            return CustomChargingPricesSerializer is not null
                       ? CustomChargingPricesSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingPrices, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a price status.
        /// </summary>
        public static Boolean TryParse(JToken?                                   JSON,
                                       [NotNullWhen(true)]  out ChargingPrices?  ChargingPrices,
                                       [NotNullWhen(false)] out String?          ErrorResponse)
        {

            ChargingPrices = null;

            if (JSON is not JObject json || json[PropertyKeys.Prices] is not JArray pricesJSON)
            {
                ErrorResponse = $"A price status must be a JSON object {{ {PropertyKeys.Prices}: [ {{ {ChargingTariffPrices.PropertyKeys.ChargingTariffId}, {ChargingTariffPrices.PropertyKeys.Elements} }}, … ] }}!";
                return false;
            }

            foreach (var property in json.Properties())
                if (property.Name is not (PropertyKeys.Context or PropertyKeys.Prices))
                {
                    ErrorResponse = $"Unknown property '{property.Name}' of a price status!";
                    return false;
                }

            if (json[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of a price status must be '{JSONLDContext}'!";
                return false;
            }

            var prices = new List<ChargingTariffPrices>();

            foreach (var priceJSON in pricesJSON)
            {

                if (!ChargingTariffPrices.TryParse(priceJSON, out var price, out ErrorResponse))
                    return false;

                if (prices.Any(known => known.ChargingTariffId == price.ChargingTariffId))
                {
                    ErrorResponse = $"A price status must name the charging tariff '{price.ChargingTariffId}' only once!";
                    return false;
                }

                prices.Add(price);

            }

            ChargingPrices  = new ChargingPrices(prices);
            ErrorResponse   = null;
            return true;

        }

        #endregion

        #region (static) TryParseText(Text, out ChargingPrices)

        /// <summary>
        /// Try to parse the given text of a price status, its JSON.
        /// </summary>
        public static Boolean TryParseText(String                                   Text,
                                           [NotNullWhen(true)] out ChargingPrices?  ChargingPrices)
        {

            ChargingPrices = null;

            try
            {
                return TryParse(POIJSON.Read(Text), out ChargingPrices, out _);
            }
            catch
            {
                return false;
            }

        }

        #endregion

        #region (static) ToText(ChargingPrices)

        /// <summary>
        /// The text of the given price status: its compact JSON.
        /// </summary>
        public static String ToText(ChargingPrices ChargingPrices)

            => ChargingPrices.Text;

        #endregion

        #region (static) Effective(ChargingPool, ChargingStation, EVSE)

        /// <summary>
        /// The prices in effect at the given EVSE: for every charging tariff, those of the most specific price status
        /// naming it, the EVSE's own before its charging station's before its charging pool's; with since when.
        /// </summary>
        public static IReadOnlyList<Timestamped<ChargingTariffPrices>> Effective(ChargingPool?     ChargingPool,
                                                                                 ChargingStation?  ChargingStation,
                                                                                 EVSE              EVSE)
        {

            var effective = new Dictionary<ChargingTariff_Id, Timestamped<ChargingTariffPrices>>();

            foreach (var priceStatus in new[] { ChargingPool?.PriceStatus, ChargingStation?.PriceStatus, EVSE.PriceStatus })
                if (priceStatus.HasValue)
                    foreach (var price in priceStatus.Value.Value.Prices)
                        effective[price.ChargingTariffId] = new Timestamped<ChargingTariffPrices>(priceStatus.Value.Timestamp, price);

            return [.. effective.Values.OrderBy(price => price.Value.ChargingTariffId.ToString(), StringComparer.Ordinal)];

        }

        #endregion


        #region IEquatable<ChargingPrices> Members

        /// <inheritdoc/>
        public Boolean Equals(ChargingPrices? ChargingPrices)

            => ChargingPrices is not null &&
               Text == ChargingPrices.Text;

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPrices chargingPrices &&
               Equals(chargingPrices);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Text.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Text;

        #endregion

    }

}
