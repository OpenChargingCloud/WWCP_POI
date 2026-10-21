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
    /// A template of charging tariffs of one charging station operator, embedded completely in it: what the charging
    /// tariffs naming it in their templateIds share, e.g. the currency, the payment means and brands, the energy mix and
    /// the web page of many tariffs, or elements they have in common. The tariffs take it at runtime, as the effective
    /// tariffs of their operator: a value of their own wins, else the one of the first of their templates giving one; the
    /// elements are taken as a whole, payment means and brands are united. No property is mandatory in a template.
    /// </summary>
    public sealed class ChargingTariffTemplate : APOITemplate<ChargingTariffTemplate_Id>,
                                                 IEquatable<ChargingTariffTemplate>,
                                                 IComparable<ChargingTariffTemplate>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/chargingTariffTemplate";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its identification.
            /// </summary>
            public const String  Id              = "@id";

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context         = "@context";

            /// <summary>
            /// The property of its multilingual name.
            /// </summary>
            public const String  Name            = "name";

            /// <summary>
            /// The property of its multilingual description.
            /// </summary>
            public const String  Description     = "description";

            /// <summary>
            /// The property of its data source.
            /// </summary>
            public const String  DataSource      = "dataSource";

            /// <summary>
            /// The property of the currency of its members.
            /// </summary>
            public const String  Currency        = ChargingTariff.PropertyKeys.Currency;

            /// <summary>
            /// The property of the tariff type of its members.
            /// </summary>
            public const String  TariffType      = ChargingTariff.PropertyKeys.TariffType;

            /// <summary>
            /// The property of the tariff URL of its members.
            /// </summary>
            public const String  TariffURL       = ChargingTariff.PropertyKeys.TariffURL;

            /// <summary>
            /// The property of the elements of its members.
            /// </summary>
            public const String  Elements        = ChargingTariff.PropertyKeys.Elements;

            /// <summary>
            /// The property of the minimum price of its members.
            /// </summary>
            public const String  MinPrice        = ChargingTariff.PropertyKeys.MinPrice;

            /// <summary>
            /// The property of the maximum price of its members.
            /// </summary>
            public const String  MaxPrice        = ChargingTariff.PropertyKeys.MaxPrice;

            /// <summary>
            /// The property of the payment means of its members.
            /// </summary>
            public const String  PaymentMeans    = ChargingTariff.PropertyKeys.PaymentMeans;

            /// <summary>
            /// The property of the payment brands of its members.
            /// </summary>
            public const String  PaymentBrands   = ChargingTariff.PropertyKeys.PaymentBrands;

            /// <summary>
            /// The property of the start of the validity of its members.
            /// </summary>
            public const String  ValidFrom       = ChargingTariff.PropertyKeys.ValidFrom;

            /// <summary>
            /// The property of the end of the validity of its members.
            /// </summary>
            public const String  ValidUntil      = ChargingTariff.PropertyKeys.ValidUntil;

            /// <summary>
            /// The property of the energy mix of its members.
            /// </summary>
            public const String  EnergyMix       = ChargingTariff.PropertyKeys.EnergyMix;

            /// <summary>
            /// The property of its creation timestamp, as of every entity.
            /// </summary>
            public const String  Created         = "created";

            /// <summary>
            /// The property of its last change timestamp, as of every entity.
            /// </summary>
            public const String  LastChange      = "lastChange";

            /// <summary>
            /// The property of its custom data, as of every entity.
            /// </summary>
            public const String  CustomData      = "customData";

        }

        #endregion


        /// <summary>
        /// The currency of all prices of its members, unless they have their own.
        /// </summary>
        public Currency?                              Currency       { get; }

        /// <summary>
        /// For which charging sessions its members are, unless they tell it themselves.
        /// </summary>
        public ChargingTariffType?                    TariffType     { get; }

        /// <summary>
        /// A web page about its members, unless they have their own.
        /// </summary>
        public URL?                                   TariffURL      { get; }

        /// <summary>
        /// The elements of its members, in their given order, as a whole, unless they have their own.
        /// </summary>
        public ImmutableArray<ChargingTariffElement>  Elements       { get; }

        /// <summary>
        /// The least a charging session of its members costs, unless they have their own.
        /// </summary>
        public ChargingPriceLimit?                    MinPrice       { get; }

        /// <summary>
        /// The most a charging session of its members costs, unless they have their own.
        /// </summary>
        public ChargingPriceLimit?                    MaxPrice       { get; }

        /// <summary>
        /// The means its members can be paid with, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<PaymentMeans>           PaymentMeans   { get; }

        /// <summary>
        /// The payment brands its members can be paid with, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<PaymentBrand>           PaymentBrands  { get; }

        /// <summary>
        /// From when its members are valid, inclusive, unless they have their own.
        /// </summary>
        public DateTimeOffset?                        ValidFrom      { get; }

        /// <summary>
        /// Until when its members are valid, exclusive, unless they have their own.
        /// </summary>
        public DateTimeOffset?                        ValidUntil     { get; }

        /// <summary>
        /// The mix of the energy its members deliver, unless they have their own.
        /// </summary>
        public EnergyMix?                             EnergyMix      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff template.
        /// </summary>
        public ChargingTariffTemplate(ChargingTariffTemplate_Id            Id,
                                      I18NString?                          Name            = null,
                                      I18NString?                          Description     = null,
                                      Currency?                            Currency        = null,
                                      IEnumerable<ChargingTariffElement>?  Elements        = null,
                                      URL?                                 TariffURL       = null,
                                      EnergyMix?                           EnergyMix       = null,
                                      ChargingTariffType?                  TariffType      = null,
                                      ChargingPriceLimit?                  MinPrice        = null,
                                      ChargingPriceLimit?                  MaxPrice        = null,
                                      IEnumerable<PaymentMeans>?           PaymentMeans    = null,
                                      IEnumerable<PaymentBrand>?           PaymentBrands   = null,
                                      DateTimeOffset?                      ValidFrom       = null,
                                      DateTimeOffset?                      ValidUntil      = null,

                                      String?                              DataSource      = null,
                                      DateTimeOffset?                      Created         = null,
                                      DateTimeOffset?                      LastChange      = null,

                                      CustomDataNew?                       CustomData      = null,
                                      UserDefinedDictionary?               InternalData    = null)

            : base(Id,
                   Name,
                   Description,
                   DataSource,
                   Created,
                   LastChange,
                   CustomData,
                   InternalData)

        {

            var elements = (Elements ?? []).ToArray();

            if (elements.Any(element => element is null))
                throw new ArgumentException($"The elements of the charging tariff template '{Id}' must not contain null entries!", nameof(Elements));

            if (ChargingTariff.ValidatePriceLimits($"the charging tariff template '{Id}'", MinPrice, MaxPrice) is String error)
                throw new ArgumentException(error, nameof(MinPrice));

            // To the millisecond, as every timestamp of the content.
            ValidFrom  = ValidFrom. HasValue ? POIEntityMetadata.Timestamp(ValidFrom. Value) : null;
            ValidUntil = ValidUntil.HasValue ? POIEntityMetadata.Timestamp(ValidUntil.Value) : null;

            if (ValidFrom >= ValidUntil)
                throw new ArgumentException($"The charging tariff template '{Id}' must be valid from before it is valid until!", nameof(ValidFrom));

            this.Currency       = Currency;
            this.TariffType     = TariffType;
            this.TariffURL      = TariffURL;
            this.EnergyMix      = EnergyMix;
            this.Elements       = [.. elements];
            this.MinPrice       = MinPrice;
            this.MaxPrice       = MaxPrice;
            this.PaymentMeans   = POIJSON.OrderedIds(PaymentMeans,  $"payment means of the charging tariff template '{Id}'");
            this.PaymentBrands  = POIJSON.OrderedIds(PaymentBrands, $"payment brand of the charging tariff template '{Id}'");
            this.ValidFrom      = ValidFrom;
            this.ValidUntil     = ValidUntil;

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging tariff template as its operator embeds it, without a JSON-LD
        /// context: the one of every template, see IPOITemplate.
        /// </summary>
        public override JObject ToJSON()

            => ToJSON(Embedded: true);

        #endregion

        #region ToJSON(Embedded = false, CustomChargingTariffTemplateSerializer = null)

        /// <summary>
        /// Return the JSON representation of this charging tariff template, as its operator embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingTariffTemplateSerializer">A delegate to serialize custom charging tariff templates.</param>
        public JObject ToJSON(Boolean                                                   Embedded                                 = false,
                              CustomJObjectSerializerDelegate<ChargingTariffTemplate>?  CustomChargingTariffTemplateSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(PropertyKeys.Context,        JSONLDContext);

            AddFrameTo(json);

            if (Currency is not null)
                json.Add(PropertyKeys.Currency,       Currency.ISOCode);

            if (TariffType.HasValue)
                json.Add(PropertyKeys.TariffType,     TariffType.Value.ToString());

            if (TariffURL.HasValue)
                json.Add(PropertyKeys.TariffURL,      TariffURL.Value.ToString());

            if (Elements.Length > 0)
                json.Add(PropertyKeys.Elements,       new JArray(Elements.Select(element => element.ToJSON(Embedded: true))));

            if (MinPrice is not null)
                json.Add(PropertyKeys.MinPrice,       MinPrice.ToJSON(Embedded: true));

            if (MaxPrice is not null)
                json.Add(PropertyKeys.MaxPrice,       MaxPrice.ToJSON(Embedded: true));

            if (PaymentMeans.Length > 0)
                json.Add(PropertyKeys.PaymentMeans,   POIJSON.IdsToJSON(PaymentMeans));

            if (PaymentBrands.Length > 0)
                json.Add(PropertyKeys.PaymentBrands,  POIJSON.IdsToJSON(PaymentBrands));

            if (ValidFrom.HasValue)
                json.Add(PropertyKeys.ValidFrom,      ValidFrom. Value.ToISO8601());

            if (ValidUntil.HasValue)
                json.Add(PropertyKeys.ValidUntil,     ValidUntil.Value.ToISO8601());

            if (EnergyMix is not null)
                json.Add(PropertyKeys.EnergyMix,      EnergyMix.ToJSON(Embedded: true));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return CustomChargingTariffTemplateSerializer is not null
                       ? CustomChargingTariffTemplateSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingTariffTemplate, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging tariff template.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingTariffTemplate">The parsed charging tariff template.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                            JSON,
                                       [NotNullWhen(true)]  out ChargingTariffTemplate?   ChargingTariffTemplate,
                                       [NotNullWhen(false)] out String?                   ErrorResponse)
        {

            ChargingTariffTemplate = null;

            try
            {

                if (!JSON.ParseMandatory(PropertyKeys.Id,
                                         "charging tariff template identification",
                                         ChargingTariffTemplate_Id.TryParse,
                                         out ChargingTariffTemplate_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging tariff template '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                Currency? currency = null;

                if (JSON[PropertyKeys.Currency] is JToken currencyJSON &&
                   (currencyJSON.Type != JTokenType.String ||
                    !Currency.TryParseISO(currencyJSON.Value<String>()!, out currency)))
                {
                    ErrorResponse = $"The currency of the charging tariff template '{id}' must be an ISO 4217 code, not '{currencyJSON}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, PropertyKeys.Name,         "charging tariff template name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, PropertyKeys.Description,  "charging tariff template description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.DataSource,   "data source",                           out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.TariffURL,    "tariff URL",                            URL.TryParse, out URL? tariffURL, out ErrorResponse))
                {
                    return false;
                }

                if (!ChargingTariff.TryParseElements(JSON, $"the charging tariff template '{id}'", out var elements, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<EnergyMix>(JSON, PropertyKeys.EnergyMix, "energy mix", (JObject json, out EnergyMix? energyMix, out String? error) => POI.EnergyMix.TryParse(json, out energyMix, out error), out var energyMix, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseEnum(JSON, PropertyKeys.TariffType, "charging tariff type", out ChargingTariffType? tariffType, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<ChargingPriceLimit>(JSON, PropertyKeys.MinPrice, "minimum price", ChargingPriceLimit.TryParse, out var minPrice, out ErrorResponse) ||
                    !POIJSON.TryParseObject<ChargingPriceLimit>(JSON, PropertyKeys.MaxPrice, "maximum price", ChargingPriceLimit.TryParse, out var maxPrice, out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseIds(JSON, PropertyKeys.PaymentMeans,  "payment means", POI.PaymentMeans.TryParse, out ImmutableArray<PaymentMeans> paymentMeans,  out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, PropertyKeys.PaymentBrands, "payment brand", POI.PaymentBrand.TryParse, out ImmutableArray<PaymentBrand> paymentBrands, out ErrorResponse))
                {
                    return false;
                }

                if (ChargingTariff.ValidatePriceLimits($"the charging tariff template '{id}'", minPrice, maxPrice) is String priceLimitsError)
                {
                    ErrorResponse = priceLimitsError;
                    return false;
                }

                if (!POIEntityMetadata.TryParseTimestamp(JSON[PropertyKeys.ValidFrom],   "start of the validity", out var validFrom,  out ErrorResponse) ||
                    !POIEntityMetadata.TryParseTimestamp(JSON[PropertyKeys.ValidUntil],  "end of the validity",   out var validUntil, out ErrorResponse))
                {
                    return false;
                }

                if (validFrom >= validUntil)
                {
                    ErrorResponse = $"The charging tariff template '{id}' must be valid from before it is valid until!";
                    return false;
                }

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingTariffTemplate = new ChargingTariffTemplate(id,
                                                                    name,
                                                                    description,
                                                                    currency,
                                                                    elements,
                                                                    tariffURL,
                                                                    energyMix,
                                                                    tariffType,
                                                                    minPrice,
                                                                    maxPrice,
                                                                    paymentMeans,
                                                                    paymentBrands,
                                                                    validFrom,
                                                                    validUntil,
                                                                    DataSource:  dataSource,
                                                                    Created:     created,
                                                                    LastChange:  lastChange,
                                                                    CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingTariffTemplate  = null;
                ErrorResponse           = "The given JSON representation of a charging tariff template is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region (internal static) Effective(ChargingTariff, Templates)

        /// <summary>
        /// The given charging tariff as it is at runtime, with the properties of the given templates, in the order of its
        /// templateIds: a single value is its own, else the one of the first of its templates giving one; its elements are
        /// its own, else those of the first template giving them, as a whole; payment means and brands are united. It must
        /// have a currency and elements then. It has the runtime status of the tariff; its last change is the latest of it
        /// and its templates.
        /// </summary>
        /// <param name="ChargingTariff">A charging tariff.</param>
        /// <param name="Templates">The templates of the charging tariff, in the order of its templateIds.</param>
        internal static ChargingTariff Effective(ChargingTariff                             ChargingTariff,
                                                 POITemplateValues<ChargingTariffTemplate>  Templates)
        {

            try
            {

                var elements  = ChargingTariff.Elements.Length > 0
                                    ? ChargingTariff.Elements
                                    : Templates.Templates.Select(template => template.Elements).FirstOrDefault(elements => elements.Length > 0);

                var effective = new ChargingTariff(

                                    ChargingTariff.Id,
                                    Templates.Single(ChargingTariff.Currency,    template => template.Currency),
                                    elements.IsDefault ? [] : elements,
                                    ChargingTariff.Name,
                                    ChargingTariff.Description,
                                    Templates.Single(ChargingTariff.TariffURL,   template => template.TariffURL),
                                    Templates.Single(ChargingTariff.EnergyMix,   template => template.EnergyMix),
                                    Templates.Single(ChargingTariff.TariffType,  template => template.TariffType),
                                    Templates.Single(ChargingTariff.MinPrice,    template => template.MinPrice),
                                    Templates.Single(ChargingTariff.MaxPrice,    template => template.MaxPrice),
                                    Templates.Union (ChargingTariff.PaymentMeans,   template => template.PaymentMeans),
                                    Templates.Union (ChargingTariff.PaymentBrands,  template => template.PaymentBrands),
                                    Templates.Single(ChargingTariff.ValidFrom,   template => template.ValidFrom),
                                    Templates.Single(ChargingTariff.ValidUntil,  template => template.ValidUntil),

                                    TemplateIds:  ChargingTariff.TemplateIds,

                                    DataSource:   ChargingTariff.DataSource,
                                    Created:      ChargingTariff.Created,
                                    LastChange:   Templates.LastChange(ChargingTariff.LastChangeDate),
                                    CustomData:   ChargingTariff.CustomData

                                );

                if (!effective.IsComplete)
                    throw new ArgumentException(effective.Currency is null
                                                    ? "it has no currency, neither of its own nor of its templates!"
                                                    : "it has no elements, neither of its own nor of its templates!");

                effective.ShareRuntimeOf(ChargingTariff);

                return effective;

            }
            catch (ArgumentException e)
            {
                throw new ArgumentException($"The charging tariff '{ChargingTariff.Id}' {Templates.With} is invalid: {e.Message}", e);
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging tariff templates for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingTariffTemplate? ChargingTariffTemplate1, ChargingTariffTemplate? ChargingTariffTemplate2)

            => ReferenceEquals(ChargingTariffTemplate1, ChargingTariffTemplate2) ||
               (ChargingTariffTemplate1 is not null && ChargingTariffTemplate1.Equals(ChargingTariffTemplate2));

        /// <summary>
        /// Compares two charging tariff templates for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingTariffTemplate? ChargingTariffTemplate1, ChargingTariffTemplate? ChargingTariffTemplate2)

            => !(ChargingTariffTemplate1 == ChargingTariffTemplate2);

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingTariffTemplate? ChargingTariffTemplate)

            => ChargingTariffTemplate is null
                   ? throw new ArgumentNullException(nameof(ChargingTariffTemplate), "The given charging tariff template must not be null!")
                   : Id.CompareTo(ChargingTariffTemplate.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffTemplate chargingTariffTemplate &&
               Equals(chargingTariffTemplate);

        /// <inheritdoc/>
        public Boolean Equals(ChargingTariffTemplate? ChargingTariffTemplate)

            => ChargingTariffTemplate is not null &&
               Id.Equals(ChargingTariffTemplate.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        #endregion

    }

}
