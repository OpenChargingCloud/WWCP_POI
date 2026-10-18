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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The price of a charging tariff offer of its own, as a charging tariff writes it, without its identification, type,
    /// validity and timestamps: a currency and at least one element, and optionally price limits, a tariff URL, payment
    /// means and brands, and an energy mix.
    /// </summary>
    public sealed class ChargingTariffOfferPrice : IEquatable<ChargingTariffOfferPrice>
    {

        #region Properties

        /// <summary>
        /// The currency of its prices.
        /// </summary>
        public Currency                               Currency         { get; }

        /// <summary>
        /// Its elements, at least one.
        /// </summary>
        public ImmutableArray<ChargingTariffElement>  Elements         { get; }

        /// <summary>
        /// The URL of a page telling it.
        /// </summary>
        public URL?                                   TariffURL        { get; }

        /// <summary>
        /// The energy mix it is for.
        /// </summary>
        public EnergyMix?                             EnergyMix        { get; }

        /// <summary>
        /// The minimum price of a charging session.
        /// </summary>
        public ChargingPriceLimit?                    MinPrice         { get; }

        /// <summary>
        /// The maximum price of a charging session.
        /// </summary>
        public ChargingPriceLimit?                    MaxPrice         { get; }

        /// <summary>
        /// How a session can be paid ad hoc.
        /// </summary>
        public ImmutableArray<PaymentMeans>           PaymentMeans     { get; }

        /// <summary>
        /// The brands of payment cards taken.
        /// </summary>
        public ImmutableArray<PaymentBrand>           PaymentBrands    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new price of a charging tariff offer.
        /// </summary>
        /// <exception cref="ArgumentException">Without elements, or with a minimum price above its maximum price.</exception>
        public ChargingTariffOfferPrice(Currency                             Currency,
                                        IEnumerable<ChargingTariffElement>   Elements,
                                        URL?                                 TariffURL       = null,
                                        EnergyMix?                           EnergyMix       = null,
                                        ChargingPriceLimit?                  MinPrice        = null,
                                        ChargingPriceLimit?                  MaxPrice        = null,
                                        IEnumerable<PaymentMeans>?           PaymentMeans    = null,
                                        IEnumerable<PaymentBrand>?           PaymentBrands   = null)
        {

            this.Currency       = Currency;
            this.Elements       = [.. Elements];
            this.TariffURL      = TariffURL;
            this.EnergyMix      = EnergyMix;
            this.MinPrice       = MinPrice;
            this.MaxPrice       = MaxPrice;
            this.PaymentMeans   = POIJSON.OrderedIds(PaymentMeans,  "payment means of a charging tariff offer");
            this.PaymentBrands  = POIJSON.OrderedIds(PaymentBrands, "payment brand of a charging tariff offer");

            if (this.Elements.IsEmpty)
                throw new ArgumentException("The price of a charging tariff offer must have at least one element!", nameof(Elements));

            if (ChargingTariff.ValidatePriceLimits("the price of a charging tariff offer", MinPrice, MaxPrice) is String priceLimitsError)
                throw new ArgumentException(priceLimitsError, nameof(MinPrice));

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this price, as its charging tariff offer embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("currency",  Currency.ISOCode)
                       );

            if (TariffURL.HasValue)
                json.Add("tariffURL",      TariffURL.Value.ToString());

            json.Add("elements",           new JArray(Elements.Select(element => element.ToJSON())));

            if (MinPrice is not null)
                json.Add("minPrice",       MinPrice.ToJSON());

            if (MaxPrice is not null)
                json.Add("maxPrice",       MaxPrice.ToJSON());

            if (PaymentMeans.Length > 0)
                json.Add("paymentMeans",   POIJSON.IdsToJSON(PaymentMeans));

            if (PaymentBrands.Length > 0)
                json.Add("paymentBrands",  POIJSON.IdsToJSON(PaymentBrands));

            if (EnergyMix is not null)
                json.Add("energyMix",      EnergyMix.ToJSON(Embedded: true));

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, Owner, out Price, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the price of a charging tariff offer.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="Owner">Whose price it is in words, for a refusal, e.g. "the charging tariff offer 'gef-summer'".</param>
        /// <param name="Price">The parsed price.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       String                                              Owner,
                                       [NotNullWhen(true)]  out ChargingTariffOfferPrice?  Price,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)
        {

            Price = null;

            // Neither an identification, nor a type, validity or timestamps of its own: it is part of the offer.
            foreach (var property in new[] { "@id", "tariffType", "validFrom", "validUntil", "templateIds", "created", "lastChange" })
                if (JSON[property] is not null)
                {
                    ErrorResponse = $"The price of {Owner} must not have a '{property}'!";
                    return false;
                }

            if (JSON["currency"] is not JValue { Type: JTokenType.String } currencyJSON ||
                !Currency.TryParseISO(currencyJSON.Value<String>()!, out var currency))
            {
                ErrorResponse = $"The currency of the price of {Owner} must be an ISO 4217 code!";
                return false;
            }

            if (!POIJSON.TryParseText(JSON, "tariffURL", "tariff URL", URL.TryParse, out URL? tariffURL, out ErrorResponse))
                return false;

            if (!ChargingTariff.TryParseElements(JSON, $"the price of {Owner}", out var elements, out ErrorResponse))
                return false;

            if (elements.Count == 0)
            {
                ErrorResponse = $"The price of {Owner} must have at least one element!";
                return false;
            }

            if (!POIJSON.TryParseObject<EnergyMix>(JSON, "energyMix", "energy mix", (JObject json, out EnergyMix? energyMix, out String? error) => POI.EnergyMix.TryParse(json, out energyMix, out error), out var energyMix, out ErrorResponse))
                return false;

            if (!POIJSON.TryParseObject<ChargingPriceLimit>(JSON, "minPrice", "minimum price", ChargingPriceLimit.TryParse, out var minPrice, out ErrorResponse) ||
                !POIJSON.TryParseObject<ChargingPriceLimit>(JSON, "maxPrice", "maximum price", ChargingPriceLimit.TryParse, out var maxPrice, out ErrorResponse))
            {
                return false;
            }

            if (!POIJSON.TryParseIds(JSON, "paymentMeans",  "payment means", POI.PaymentMeans.TryParse, out ImmutableArray<PaymentMeans> paymentMeans,  out ErrorResponse) ||
                !POIJSON.TryParseIds(JSON, "paymentBrands", "payment brand", POI.PaymentBrand.TryParse, out ImmutableArray<PaymentBrand> paymentBrands, out ErrorResponse))
            {
                return false;
            }

            if (ChargingTariff.ValidatePriceLimits($"the price of {Owner}", minPrice, maxPrice) is String priceLimitsError)
            {
                ErrorResponse = priceLimitsError;
                return false;
            }

            try
            {
                Price = new ChargingTariffOfferPrice(currency, elements, tariffURL, energyMix, minPrice, maxPrice, paymentMeans, paymentBrands);
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


        #region IEquatable<ChargingTariffOfferPrice>

        /// <inheritdoc/>
        public Boolean Equals(ChargingTariffOfferPrice? Other)

            => Other is not null &&
               POIJSON.SameValue(ToJSON(), Other.ToJSON());

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffOfferPrice other && Equals(other);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Currency.ISOCode.GetHashCode() ^ Elements.Length;

        #endregion

    }

}
