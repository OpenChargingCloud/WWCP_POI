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
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of a charging tariff changed.
    /// </summary>
    public delegate Task OnChargingTariffAdminStatusChangedDelegate(DateTimeOffset                               Timestamp,
                                                                    EventTracking_Id                             EventTrackingId,
                                                                    ChargingTariff                               ChargingTariff,
                                                                    Timestamped<ChargingTariffAdminStatusType>   OldStatus,
                                                                    Timestamped<ChargingTariffAdminStatusType>   NewStatus,
                                                                    Context?                                     DataSource);

    /// <summary>
    /// The status of a charging tariff changed.
    /// </summary>
    public delegate Task OnChargingTariffStatusChangedDelegate(DateTimeOffset                          Timestamp,
                                                               EventTracking_Id                        EventTrackingId,
                                                               ChargingTariff                          ChargingTariff,
                                                               Timestamped<ChargingTariffStatusType>   OldStatus,
                                                               Timestamped<ChargingTariffStatusType>   NewStatus,
                                                               Context?                                DataSource);


    /// <summary>
    /// A charging tariff, as in OCPI, embedded completely in its charging station operator.
    /// EVSEs of the same operator name it by its identification.
    /// </summary>
    public sealed class ChargingTariff : AImmutableEMobilityEntity<ChargingTariff_Id,
                                                                   ChargingTariffAdminStatusType,
                                                                   ChargingTariffStatusType>,
                                         IEquatable<ChargingTariff>,
                                         IComparable<ChargingTariff>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/chargingTariff";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its currency.
            /// </summary>
            public const String  Currency        = "currency";

            /// <summary>
            /// The property of its tariff type.
            /// </summary>
            public const String  TariffType      = "tariffType";

            /// <summary>
            /// The property of its tariff URL.
            /// </summary>
            public const String  TariffURL       = "tariffURL";

            /// <summary>
            /// The property of its elements.
            /// </summary>
            public const String  Elements        = "elements";

            /// <summary>
            /// The property of its minimum price.
            /// </summary>
            public const String  MinPrice        = "minPrice";

            /// <summary>
            /// The property of its maximum price.
            /// </summary>
            public const String  MaxPrice        = "maxPrice";

            /// <summary>
            /// The property of its payment means.
            /// </summary>
            public const String  PaymentMeans    = "paymentMeans";

            /// <summary>
            /// The property of its payment brands.
            /// </summary>
            public const String  PaymentBrands   = "paymentBrands";

            /// <summary>
            /// The property of the start of its validity.
            /// </summary>
            public const String  ValidFrom       = "validFrom";

            /// <summary>
            /// The property of the end of its validity.
            /// </summary>
            public const String  ValidUntil      = "validUntil";

            /// <summary>
            /// The property of its energy mix.
            /// </summary>
            public const String  EnergyMix       = "energyMix";

            /// <summary>
            /// The property of the identifications of its templates.
            /// </summary>
            public const String  TemplateIds     = "templateIds";

        }

        #endregion


        /// <summary>
        /// The currency of all its prices, by its ISO 4217 code: its own, if any; the effective tariff, with what its
        /// templates give it, has one.
        /// </summary>
        public Currency?                              Currency       { get; }

        /// <summary>
        /// For which charging sessions it is; for every one, unless given.
        /// </summary>
        public ChargingTariffType?                    TariffType     { get; }

        /// <summary>
        /// A web page about it, e.g. for EV drivers.
        /// </summary>
        public URL?                                   TariffURL      { get; }

        /// <summary>
        /// The elements, in their given order: its own, if any; the effective tariff, with what its templates give it,
        /// has at least one.
        /// </summary>
        public ImmutableArray<ChargingTariffElement>  Elements       { get; }

        /// <summary>
        /// Whether it has a currency and elements, as an effective tariff must: of its own, or of its templates.
        /// </summary>
        public Boolean                                IsComplete
            => Currency is not null && Elements.Length > 0;

        /// <summary>
        /// The least a charging session costs: a session costing less costs this.
        /// </summary>
        public ChargingPriceLimit?                    MinPrice       { get; }

        /// <summary>
        /// The most a charging session costs: a session costing more costs this.
        /// </summary>
        public ChargingPriceLimit?                    MaxPrice       { get; }

        /// <summary>
        /// The means it can be paid with, e.g. a credit card, in the order of their names.
        /// </summary>
        public ImmutableArray<PaymentMeans>           PaymentMeans   { get; }

        /// <summary>
        /// The payment brands it can be paid with, e.g. Visa, in the order of their names.
        /// </summary>
        public ImmutableArray<PaymentBrand>           PaymentBrands  { get; }

        /// <summary>
        /// From when it is valid, inclusive; always since, unless given.
        /// </summary>
        public DateTimeOffset?                        ValidFrom      { get; }

        /// <summary>
        /// Until when it is valid, exclusive; for good, unless given.
        /// </summary>
        public DateTimeOffset?                        ValidUntil     { get; }


        /// <summary>
        /// The mix of the energy it delivers.
        /// </summary>
        public EnergyMix?  EnergyMix  { get; }

        /// <summary>
        /// The charging tariff templates it takes, each once, in its order: of every property they give, the first one giving it
        /// decides, unless it has its own. It names them; they do not name their members.
        /// </summary>
        public ImmutableArray<ChargingTariffTemplate_Id>  TemplateIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff.
        /// </summary>
        public ChargingTariff(ChargingTariff_Id                              Id,
                              Currency?                                      Currency,
                              IEnumerable<ChargingTariffElement>?            Elements,
                              I18NString?                                    Name                         = null,
                              I18NString?                                    Description                  = null,
                              URL?                                           TariffURL                    = null,
                              EnergyMix?                                     EnergyMix                    = null,
                              ChargingTariffType?                            TariffType                   = null,
                              ChargingPriceLimit?                            MinPrice                     = null,
                              ChargingPriceLimit?                            MaxPrice                     = null,
                              IEnumerable<PaymentMeans>?                     PaymentMeans                 = null,
                              IEnumerable<PaymentBrand>?                     PaymentBrands                = null,
                              DateTimeOffset?                                ValidFrom                    = null,
                              DateTimeOffset?                                ValidUntil                   = null,

                              IEnumerable<ChargingTariffTemplate_Id>?        TemplateIds                  = null,

                              Timestamped<ChargingTariffAdminStatusType>?    InitialAdminStatus           = null,
                              Timestamped<ChargingTariffStatusType>?         InitialStatus                = null,
                              UInt16?                                        MaxAdminStatusScheduleSize   = null,
                              UInt16?                                        MaxStatusScheduleSize        = null,

                              String?                                        DataSource                   = null,
                              DateTimeOffset?                                Created                      = null,
                              DateTimeOffset?                                LastChange                   = null,

                              CustomDataNew?                                 CustomData                   = null,
                              UserDefinedDictionary?                         InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingTariffAdminStatusType>(Created ?? Timestamp.Now, ChargingTariffAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingTariffStatusType>     (Created ?? Timestamp.Now, ChargingTariffStatusType.Active),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.TemplateIds  = POIJSON.UniqueIds(TemplateIds, $"template of the charging tariff '{Id}'");

            // A currency and elements of its own, or of its templates: the effective tariff is checked by its operator.
            var elements = (Elements ?? []).ToArray();

            if (elements.Any(element => element is null))
                throw new ArgumentException($"The elements of the charging tariff '{Id}' must not contain null entries!", nameof(Elements));

            if (ValidatePriceLimits($"the charging tariff '{Id}'", MinPrice, MaxPrice) is String error)
                throw new ArgumentException(error, nameof(MinPrice));

            // To the millisecond, as every timestamp of the content.
            ValidFrom  = ValidFrom. HasValue ? POIEntityMetadata.Timestamp(ValidFrom. Value) : null;
            ValidUntil = ValidUntil.HasValue ? POIEntityMetadata.Timestamp(ValidUntil.Value) : null;

            if (ValidFrom >= ValidUntil)
                throw new ArgumentException($"The charging tariff '{Id}' must be valid from before it is valid until!", nameof(ValidFrom));

            this.Currency       = Currency;
            this.TariffType     = TariffType;
            this.TariffURL      = TariffURL;
            this.EnergyMix      = EnergyMix;
            this.Elements       = [.. elements];
            this.MinPrice       = MinPrice;
            this.MaxPrice       = MaxPrice;
            this.PaymentMeans   = POIJSON.OrderedIds(PaymentMeans,  $"payment means of the charging tariff '{Id}'");
            this.PaymentBrands  = POIJSON.OrderedIds(PaymentBrands, $"payment brand of the charging tariff '{Id}'");
            this.ValidFrom      = ValidFrom;
            this.ValidUntil     = ValidUntil;

        }

        #endregion


        #region (internal static) TryParseElements(JSON, Owner, out Elements, out ErrorResponse)

        /// <summary>
        /// Try to parse the optional elements of the given JSON of a charging tariff or a charging tariff template.
        /// </summary>
        /// <param name="JSON">The JSON of a charging tariff or a charging tariff template.</param>
        /// <param name="Owner">What it is, for a message, e.g. "the charging tariff 'DE*GEF*T1'".</param>
        /// <param name="Elements">The elements, in their given order; none when absent.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        internal static Boolean TryParseElements(JObject                                    JSON,
                                                 String                                     Owner,
                                                 out List<ChargingTariffElement>            Elements,
                                                 [NotNullWhen(false)] out String?           ErrorResponse)
        {

            Elements = [];

            if (JSON[PropertyKeys.Elements] is not JToken elementsToken)
            {
                ErrorResponse = null;
                return true;
            }

            if (elementsToken is not JArray elementsJSON)
            {
                ErrorResponse = $"The elements of {Owner} must be a JSON array!";
                return false;
            }

            foreach (var elementJSON in elementsJSON)
            {

                if (elementJSON is not JObject elementObject)
                {
                    ErrorResponse = "A charging tariff element must be a JSON object!";
                    return false;
                }

                if (!ChargingTariffElement.TryParse(elementObject, out var element, out ErrorResponse))
                    return false;

                Elements.Add(element);

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (internal static) ValidatePriceLimits(Owner, MinPrice, MaxPrice)

        /// <summary>
        /// The error, when the given price limits are invalid each, or the minimum exceeds the maximum.
        /// </summary>
        internal static String? ValidatePriceLimits(String               Owner,
                                                   ChargingPriceLimit?  MinPrice,
                                                   ChargingPriceLimit?  MaxPrice)
        {

            if (MinPrice?.Validate() is String minError)
                return $"The minimum price of {Owner} is invalid: {minError}";

            if (MaxPrice?.Validate() is String maxError)
                return $"The maximum price of {Owner} is invalid: {maxError}";

            if (MinPrice is not null && MaxPrice is not null &&
               (MinPrice.ExcludingVAT > MaxPrice.ExcludingVAT ||
                MinPrice.IncludingVAT > MaxPrice.IncludingVAT))
            {
                return $"The minimum price of {Owner} must not exceed its maximum price!";
            }

            return null;

        }

        #endregion


        #region Status events

        private readonly Lock                                         statusEventsLock  = new ();
        private          OnChargingTariffAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingTariffStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingTariffAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingTariffStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                              Timestamp,
                                                                   EventTracking_Id                            EventTrackingId,
                                                                   Timestamped<ChargingTariffAdminStatusType>  NewStatus,
                                                                   Timestamped<ChargingTariffAdminStatusType>  OldStatus,
                                                                   Context?                                    DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                         Timestamp,
                                                              EventTracking_Id                       EventTrackingId,
                                                              Timestamped<ChargingTariffStatusType>  NewStatus,
                                                              Timestamped<ChargingTariffStatusType>  OldStatus,
                                                              Context?                               DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON(Embedded = false, CustomChargingTariffSerializer = null)

        /// <summary>
        /// Return the JSON representation of this charging tariff, as its operator embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomChargingTariffSerializer">A delegate to serialize custom charging tariffs.</param>
        public JObject ToJSON(Boolean                                           Embedded                         = false,
                              CustomJObjectSerializerDelegate<ChargingTariff>?  CustomChargingTariffSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,      JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,  Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,   DataSource);

            if (Currency is not null)
                json.Add(PropertyKeys.Currency,         Currency.ISOCode);

            if (TariffType.HasValue)
                json.Add(PropertyKeys.TariffType,       TariffType.Value.ToString());

            if (TariffURL.HasValue)
                json.Add(PropertyKeys.TariffURL,        TariffURL.Value.ToString());

            if (Elements.Length > 0)
                json.Add(PropertyKeys.Elements,         new JArray(Elements.Select(element => element.ToJSON(Embedded: true))));

            if (MinPrice is not null)
                json.Add(PropertyKeys.MinPrice,         MinPrice.ToJSON(Embedded: true));

            if (MaxPrice is not null)
                json.Add(PropertyKeys.MaxPrice,         MaxPrice.ToJSON(Embedded: true));

            if (PaymentMeans.Length > 0)
                json.Add(PropertyKeys.PaymentMeans,     POIJSON.IdsToJSON(PaymentMeans));

            if (PaymentBrands.Length > 0)
                json.Add(PropertyKeys.PaymentBrands,    POIJSON.IdsToJSON(PaymentBrands));

            if (ValidFrom.HasValue)
                json.Add(PropertyKeys.ValidFrom,        ValidFrom. Value.ToISO8601());

            if (ValidUntil.HasValue)
                json.Add(PropertyKeys.ValidUntil,       ValidUntil.Value.ToISO8601());

            if (EnergyMix is not null)
                json.Add(PropertyKeys.EnergyMix,        EnergyMix.ToJSON(Embedded: true));

            if (TemplateIds.Length > 0)
                json.Add(PropertyKeys.TemplateIds,      POIJSON.IdsToJSON(TemplateIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return CustomChargingTariffSerializer is not null
                       ? CustomChargingTariffSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingTariff, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging tariff.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingTariff">The parsed charging tariff.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                   JSON,
                                       [NotNullWhen(true)]  out ChargingTariff?  ChargingTariff,
                                       [NotNullWhen(false)] out String?          ErrorResponse)
        {

            ChargingTariff = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "charging tariff identification",
                                         ChargingTariff_Id.TryParse,
                                         out ChargingTariff_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging tariff '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                // Its currency and elements, unless its templates give them.
                Currency? currency = null;

                if (JSON[PropertyKeys.Currency] is JToken currencyJSON &&
                   (currencyJSON.Type != JTokenType.String ||
                    !Currency.TryParseISO(currencyJSON.Value<String>()!, out currency)))
                {
                    ErrorResponse = $"The currency of the charging tariff '{id}' must be an ISO 4217 code, not '{currencyJSON}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Name,         "charging tariff name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Description,  "charging tariff description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource,   "data source",                  out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.    TariffURL,    "tariff URL",                   URL.TryParse, out URL? tariffURL, out ErrorResponse))
                {
                    return false;
                }

                if (!TryParseElements(JSON, $"the charging tariff '{id}'", out var elements, out ErrorResponse))
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

                if (ValidatePriceLimits($"the charging tariff '{id}'", minPrice, maxPrice) is String priceLimitsError)
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
                    ErrorResponse = $"The charging tariff '{id}' must be valid from before it is valid until!";
                    return false;
                }

                if (!POIJSON.TryParseIds(JSON, PropertyKeys.TemplateIds, "template identification", ChargingTariffTemplate_Id.TryParse, out ImmutableArray<ChargingTariffTemplate_Id> templateIds, out ErrorResponse))
                    return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingTariff = new ChargingTariff(id,
                                                    currency,
                                                    elements,
                                                    name,
                                                    description,
                                                    tariffURL,
                                                    energyMix,
                                                    tariffType,
                                                    minPrice,
                                                    maxPrice,
                                                    paymentMeans,
                                                    paymentBrands,
                                                    validFrom,
                                                    validUntil,
                                                    TemplateIds:  templateIds,
                                                    DataSource:  dataSource,
                                                    Created:     created,
                                                    LastChange:  lastChange,
                                                    CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingTariff  = null;
                ErrorResponse   = "The given JSON representation of a charging tariff is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging tariffs for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingTariff? ChargingTariff1, ChargingTariff? ChargingTariff2)

            => ReferenceEquals(ChargingTariff1, ChargingTariff2) ||
               (ChargingTariff1 is not null && ChargingTariff1.Equals(ChargingTariff2));

        /// <summary>
        /// Compares two charging tariffs for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingTariff? ChargingTariff1, ChargingTariff? ChargingTariff2)

            => !(ChargingTariff1 == ChargingTariff2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingTariff chargingTariff
                   ? CompareTo(chargingTariff)
                   : throw new ArgumentException("The given object is not a charging tariff!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingTariff? ChargingTariff)

            => ChargingTariff is null
                   ? throw new ArgumentNullException(nameof(ChargingTariff), "The given charging tariff must not be null!")
                   : Id.CompareTo(ChargingTariff.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariff chargingTariff &&
               Equals(chargingTariff);

        /// <inheritdoc/>
        public Boolean Equals(ChargingTariff? ChargingTariff)

            => ChargingTariff is not null &&
               Id.Equals(ChargingTariff.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
