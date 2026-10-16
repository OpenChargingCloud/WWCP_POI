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
                                                                    Timestamped<ChargingTariffAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of a charging tariff changed.
    /// </summary>
    public delegate Task OnChargingTariffStatusChangedDelegate(DateTimeOffset                          Timestamp,
                                                               EventTracking_Id                        EventTrackingId,
                                                               ChargingTariff                          ChargingTariff,
                                                               Timestamped<ChargingTariffStatusType>   OldStatus,
                                                               Timestamped<ChargingTariffStatusType>   NewStatus);


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

        #region Properties

        /// <summary>
        /// The currency of all its prices, by its ISO 4217 code.
        /// </summary>
        public Currency                               Currency       { get; }

        /// <summary>
        /// For which charging sessions it is; for every one, unless given.
        /// </summary>
        public ChargingTariffType?                    TariffType     { get; }

        /// <summary>
        /// A web page about it, e.g. for EV drivers.
        /// </summary>
        public URL?                                   TariffURL      { get; }

        /// <summary>
        /// The elements, in their given order; at least one.
        /// </summary>
        public ImmutableArray<ChargingTariffElement>  Elements       { get; }

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
        /// The mix of the energy it delivers.
        /// </summary>
        public EnergyMix?  EnergyMix  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff.
        /// </summary>
        public ChargingTariff(ChargingTariff_Id                              Id,
                              Currency                                       Currency,
                              IEnumerable<ChargingTariffElement>             Elements,
                              I18NString?                                    Name                         = null,
                              I18NString?                                    Description                  = null,
                              URL?                                           TariffURL                    = null,
                              EnergyMix?                                     EnergyMix                    = null,
                              ChargingTariffType?                            TariffType                   = null,
                              ChargingPriceLimit?                            MinPrice                     = null,
                              ChargingPriceLimit?                            MaxPrice                     = null,
                              IEnumerable<PaymentMeans>?                     PaymentMeans                 = null,
                              IEnumerable<PaymentBrand>?                     PaymentBrands                = null,

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

            ArgumentNullException.ThrowIfNull(Currency);

            var elements = (Elements ?? []).ToArray();

            if (elements.Length == 0 || elements.Any(element => element is null))
                throw new ArgumentException($"The charging tariff '{Id}' needs at least one element, and no null entries!", nameof(Elements));

            if (ValidatePriceLimits(Id, MinPrice, MaxPrice) is String error)
                throw new ArgumentException(error, nameof(MinPrice));

            this.Currency       = Currency;
            this.TariffType     = TariffType;
            this.TariffURL      = TariffURL;
            this.EnergyMix      = EnergyMix;
            this.Elements       = [.. elements];
            this.MinPrice       = MinPrice;
            this.MaxPrice       = MaxPrice;
            this.PaymentMeans   = POIJSON.OrderedIds(PaymentMeans,  $"payment means of the charging tariff '{Id}'");
            this.PaymentBrands  = POIJSON.OrderedIds(PaymentBrands, $"payment brand of the charging tariff '{Id}'");

        }

        #endregion


        #region (private static) ValidatePriceLimits(Id, MinPrice, MaxPrice)

        /// <summary>
        /// The error, when the given price limits are invalid each, or the minimum exceeds the maximum.
        /// </summary>
        private static String? ValidatePriceLimits(ChargingTariff_Id    Id,
                                                   ChargingPriceLimit?  MinPrice,
                                                   ChargingPriceLimit?  MaxPrice)
        {

            if (MinPrice?.Validate() is String minError)
                return $"The minimum price of the charging tariff '{Id}' is invalid: {minError}";

            if (MaxPrice?.Validate() is String maxError)
                return $"The maximum price of the charging tariff '{Id}' is invalid: {maxError}";

            if (MinPrice is not null && MaxPrice is not null &&
               (MinPrice.ExcludingVAT > MaxPrice.ExcludingVAT ||
                MinPrice.IncludingVAT > MaxPrice.IncludingVAT))
            {
                return $"The minimum price of the charging tariff '{Id}' must not exceed its maximum price!";
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
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
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
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging tariff, as its operator embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",  Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",   DataSource);

            json.Add("currency",         Currency.ISOCode);

            if (TariffType.HasValue)
                json.Add("tariffType",   TariffType.Value.ToString());

            if (TariffURL.HasValue)
                json.Add("tariffURL",    TariffURL.Value.ToString());

            json.Add("elements",         new JArray(Elements.Select(element => element.ToJSON())));

            if (MinPrice is not null)
                json.Add("minPrice",     MinPrice.ToJSON());

            if (MaxPrice is not null)
                json.Add("maxPrice",     MaxPrice.ToJSON());

            if (PaymentMeans.Length > 0)
                json.Add("paymentMeans",   POIJSON.IdsToJSON(PaymentMeans));

            if (PaymentBrands.Length > 0)
                json.Add("paymentBrands",  POIJSON.IdsToJSON(PaymentBrands));

            if (EnergyMix is not null)
                json.Add("energyMix",    EnergyMix.ToJSON(Embedded: true));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

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

                if (!JSON.ParseMandatory("@id",
                                         "charging tariff identification",
                                         ChargingTariff_Id.TryParse,
                                         out ChargingTariff_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON["currency"]?.Type != JTokenType.String ||
                    !Currency.TryParseISO(JSON["currency"]!.Value<String>()!, out var currency))
                {
                    ErrorResponse = $"The charging tariff '{id}' needs its currency, an ISO 4217 code, not '{JSON["currency"]}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, "name",         "charging tariff name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, "description",  "charging tariff description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "dataSource",   "data source",                  out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "tariffURL",    "tariff URL",                   URL.TryParse, out URL? tariffURL, out ErrorResponse))
                {
                    return false;
                }

                if (JSON["elements"] is not JArray elementsJSON)
                {
                    ErrorResponse = $"The charging tariff '{id}' needs its elements as a JSON array!";
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

                if (!POIJSON.TryParseObject<EnergyMix>(JSON, "energyMix", "energy mix", (JObject json, out EnergyMix? energyMix, out String? error) => POI.EnergyMix.TryParse(json, out energyMix, out error), out var energyMix, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseEnum(JSON, "tariffType", "charging tariff type", out ChargingTariffType? tariffType, out ErrorResponse))
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

                if (ValidatePriceLimits(id, minPrice, maxPrice) is String priceLimitsError)
                {
                    ErrorResponse = priceLimitsError;
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
