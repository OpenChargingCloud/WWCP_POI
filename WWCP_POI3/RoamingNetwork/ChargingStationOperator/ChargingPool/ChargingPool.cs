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

using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of a charging pool changed.
    /// </summary>
    public delegate Task OnChargingPoolAdminStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  ChargingPool                               ChargingPool,
                                                                  Timestamped<ChargingPoolAdminStatusType>   OldStatus,
                                                                  Timestamped<ChargingPoolAdminStatusType>   NewStatus,
                                                                  Context?                                   DataSource);

    /// <summary>
    /// The status of a charging pool changed.
    /// </summary>
    /// <summary>
    /// A delegate called whenever the price status of a charging pool changed.
    /// </summary>
    public delegate Task OnChargingPoolPriceStatusChangedDelegate(DateTimeOffset                Timestamp,
                                                                  EventTracking_Id              EventTrackingId,
                                                                  ChargingPool                  ChargingPool,
                                                                  Timestamped<ChargingPrices>   OldPriceStatus,
                                                                  Timestamped<ChargingPrices>   NewPriceStatus,
                                                                  Context?                      DataSource);

    public delegate Task OnChargingPoolStatusChangedDelegate(DateTimeOffset                        Timestamp,
                                                             EventTracking_Id                      EventTrackingId,
                                                             ChargingPool                          ChargingPool,
                                                             Timestamped<ChargingPoolStatusType>   OldStatus,
                                                             Timestamped<ChargingPoolStatusType>   NewStatus,
                                                             Context?                              DataSource);


    /// <summary>
    /// A charging pool: a location with its charging stations, embedded completely in its charging station operator.
    /// </summary>
    public sealed class ChargingPool : AImmutableEMobilityEntity<ChargingPool_Id,
                                                                 ChargingPoolAdminStatusType,
                                                                 ChargingPoolStatusType>,
                                       IEquatable<ChargingPool>,
                                       IComparable<ChargingPool>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingPool/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address                   = "address";

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation               = "geoLocation";

            /// <summary>
            /// The property of its type of parking.
            /// </summary>
            public const String  ParkingType               = "parkingType";

            /// <summary>
            /// The property of its accessibility.
            /// </summary>
            public const String  Accessibility             = "accessibility";

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber        = "hotline";

            /// <summary>
            /// The property of its grid connection point, refused: it is embedded in its grid operator.
            /// </summary>
            public const String  GridConnectionPoint       = "gridConnectionPoint";

            /// <summary>
            /// The property of the identification of its grid connection point.
            /// </summary>
            public const String  GridConnectionPointId     = "gridConnectionPointId";

            /// <summary>
            /// The property of its own energy meters.
            /// </summary>
            public const String  EnergyMeters              = "energyMeters";

            /// <summary>
            /// The property of the identifications of its own energy meters.
            /// </summary>
            public const String  EnergyMeterIds            = "energyMeterIds";

            /// <summary>
            /// The property of its charging stations.
            /// </summary>
            public const String  ChargingStations          = "chargingStations";

            /// <summary>
            /// The property of the identifications of its charging stations.
            /// </summary>
            public const String  ChargingStationIds        = "chargingStationIds";

            /// <summary>
            /// The property of its brands.
            /// </summary>
            public const String  Brands                    = "brands";

            /// <summary>
            /// The property of the identifications of its brands.
            /// </summary>
            public const String  BrandIds                  = "brandIds";

            /// <summary>
            /// The property of its opening times.
            /// </summary>
            public const String  OpeningTimes              = "openingTimes";

            /// <summary>
            /// The property of its energy mix.
            /// </summary>
            public const String  EnergyMix                 = "energyMix";

            /// <summary>
            /// The property of its images.
            /// </summary>
            public const String  Images                    = "images";

            /// <summary>
            /// The property of its time zone.
            /// </summary>
            public const String  TimeZone                  = "timeZone";

            /// <summary>
            /// The property of its facilities.
            /// </summary>
            public const String  Facilities                = "facilities";

            /// <summary>
            /// The property of its payment options.
            /// </summary>
            public const String  PaymentOptions            = "paymentOptions";

            /// <summary>
            /// The property of its authentication modes.
            /// </summary>
            public const String  AuthenticationModes       = "authenticationModes";

            /// <summary>
            /// The property of the features of its user interfaces.
            /// </summary>
            public const String  UIFeatures                = "uiFeatures";

            /// <summary>
            /// The property of its features.
            /// </summary>
            public const String  Features                  = "features";

            /// <summary>
            /// The property of its location languages.
            /// </summary>
            public const String  LocationLanguages         = "locationLanguages";

            /// <summary>
            /// The property of the address of its entrance.
            /// </summary>
            public const String  EntranceAddress           = "entranceAddress";

            /// <summary>
            /// The property of the geographical location of its entrance.
            /// </summary>
            public const String  EntranceLocation          = "entranceLocation";

            /// <summary>
            /// The property of the address of its exit.
            /// </summary>
            public const String  ExitAddress               = "exitAddress";

            /// <summary>
            /// The property of the geographical location of its exit.
            /// </summary>
            public const String  ExitLocation              = "exitLocation";

            /// <summary>
            /// The property of its arrival instructions.
            /// </summary>
            public const String  ArrivalInstructions       = "arrivalInstructions";

            /// <summary>
            /// The property of whether it charges while closed.
            /// </summary>
            public const String  ChargingWhenClosed        = "chargingWhenClosed";

            /// <summary>
            /// The property of its parking restrictions.
            /// </summary>
            public const String  ParkingRestrictions       = "parkingRestrictions";

            /// <summary>
            /// The property of its related locations.
            /// </summary>
            public const String  RelatedLocations          = "relatedLocations";

            /// <summary>
            /// The property of its services.
            /// </summary>
            public const String  Services                  = "services";

            /// <summary>
            /// The property of the details of its assistance service.
            /// </summary>
            public const String  AssistanceServiceDetails  = "assistanceServiceDetails";

            /// <summary>
            /// The property of its standards.
            /// </summary>
            public const String  Standards                 = "standards";

            /// <summary>
            /// The property of its NUTS codes.
            /// </summary>
            public const String  NUTSCodes                 = "nutsCodes";

            /// <summary>
            /// The property of the identifications of its templates.
            /// </summary>
            public const String  TemplateIds               = "templateIds";

        }

        #endregion


        /// <summary>
        /// The address.
        /// </summary>
        public Address?                         Address               { get; }

        /// <summary>
        /// The geographical location.
        /// </summary>
        public GeoCoordinate?                   GeoLocation           { get; }

        /// <summary>
        /// The type of parking, e.g. PARKING_LOT.
        /// </summary>
        public ParkingType?                     ParkingType           { get; }

        /// <summary>
        /// Who may use it, e.g. FreePubliclyAccessible.
        /// </summary>
        public AccessibilityType?               Accessibility         { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?                     HotlinePhoneNumber    { get; }

        /// <summary>
        /// The data licenses, ordered.
        /// </summary>
        public ImmutableArray<DataLicense>      DataLicenses          { get; }

        /// <summary>
        /// The charging stations, in the order of their identifications.
        /// </summary>
        public POICollection<ChargingStation_Id, ChargingStation>  ChargingStations  { get; }

        /// <summary>
        /// The EVSEs of all its charging stations.
        /// </summary>
        public IEnumerable<EVSE>                EVSEs
            => ChargingStations.SelectMany(chargingStation => chargingStation.EVSEs);

        /// <summary>
        /// Where it is connected to the public grid: a grid connection point of a grid operator of its roaming network.
        /// </summary>
        public GridConnectionPoint_Id?          GridConnectionPointId { get; }

        /// <summary>
        /// Its own energy meters, in the order of their identifications; not those of its charging stations.
        /// The meter of its grid connection point belongs to the grid connection point.
        /// </summary>
        public ImmutableArray<EnergyMeter>      EnergyMeters          { get; }

        /// <summary>
        /// Its own energy meters, and those of its charging stations and their EVSEs.
        /// </summary>
        public IEnumerable<EnergyMeter>         AllEnergyMeters
            => EnergyMeters.
                   Concat(ChargingStations.SelectMany(chargingStation => chargingStation.AllEnergyMeters));


        /// <summary>
        /// The brands it also appears under: its own, embedded completely, in the order of their identifications.
        /// Not those of its operator or of what it embeds; every level has brands of its own.
        /// </summary>
        public POICollection<Brand_Id, Brand>  Brands  { get; }

        /// <summary>
        /// When it is open; always, unless given.
        /// </summary>
        public ImmutableOpeningTimes?  OpeningTimes  { get; }

        /// <summary>
        /// The mix of the energy it delivers.
        /// </summary>
        public EnergyMix?  EnergyMix  { get; }


        /// <summary>
        /// Its images, in their given order.
        /// </summary>
        public ImmutableArray<Image>  Images  { get; }


        /// <summary>
        /// Its time zone, e.g. Europe/Berlin.
        /// </summary>
        public Time_Zone?                                TimeZone               { get; }

        /// <summary>
        /// The facilities nearby, e.g. a restaurant, in the order of their names.
        /// </summary>
        public ImmutableArray<Facility>                  Facilities             { get; }

        /// <summary>
        /// How charging can be paid, in the order of their names.
        /// </summary>
        public ImmutableArray<PaymentOptions>            PaymentOptions         { get; }

        /// <summary>
        /// How drivers authenticate, in the order of their types; a type only once.
        /// </summary>
        public ImmutableArray<AuthenticationMode>        AuthenticationModes    { get; }

        /// <summary>
        /// The features of its user interfaces, in the order of their names.
        /// </summary>
        public ImmutableArray<UIFeatures>                UIFeatures             { get; }

        /// <summary>
        /// Its features, in the order of their names.
        /// </summary>
        public ImmutableArray<ChargingPoolFeature>       Features               { get; }

        /// <summary>
        /// The languages spoken or shown there, in their given order.
        /// </summary>
        public ImmutableArray<Languages>                 LocationLanguages      { get; }

        /// <summary>
        /// The address of its entrance, when it differs from its address.
        /// </summary>
        public Address?                                  EntranceAddress        { get; }

        /// <summary>
        /// The geographical location of its entrance.
        /// </summary>
        public GeoCoordinate?                            EntranceLocation       { get; }

        /// <summary>
        /// The address of its exit, when it differs from its address.
        /// </summary>
        public Address?                                  ExitAddress            { get; }

        /// <summary>
        /// The geographical location of its exit.
        /// </summary>
        public GeoCoordinate?                            ExitLocation           { get; }

        /// <summary>
        /// How to get there, e.g. "Second floor of the car park".
        /// </summary>
        public ImmutableI18NString                       ArrivalInstructions    { get; }

        /// <summary>
        /// Whether a vehicle may go on charging while it is closed, unless unknown.
        /// </summary>
        public Boolean?                                  ChargingWhenClosed     { get; }

        /// <summary>
        /// Who may park there, e.g. disabled persons only, in the order of their names.
        /// </summary>
        public ImmutableArray<ParkingRestrictionGroup>   ParkingRestrictions    { get; }

        /// <summary>
        /// Further geographical locations related to it, e.g. walkways, in their given order.
        /// </summary>
        public ImmutableArray<AdditionalGeoLocation>     RelatedLocations       { get; }

        /// <summary>
        /// The services offered there, e.g. accessible charging or assistance, in the order of their names.
        /// </summary>
        public ImmutableArray<LocationService>           Services                   { get; }

        /// <summary>
        /// How to get assistance there: how to reach it, when, and what kind of support it offers.
        /// </summary>
        public ImmutableI18NString                       AssistanceServiceDetails   { get; }

        /// <summary>
        /// The standards it conforms to, e.g. PAS 1899:2022 for accessible charging, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>                  Standards                  { get; }

        /// <summary>
        /// The NUTS regions it is in, e.g. DEG (NUTS 1) and DEG03 (NUTS 3), in the order of their codes.
        /// </summary>
        public ImmutableArray<NUTSCode>                  NUTSCodes                  { get; }

        /// <summary>
        /// The charging pool templates it takes, each once, in its order: of every property they give, the first one giving it
        /// decides, unless it has its own. It names them; they do not name their members.
        /// </summary>
        public ImmutableArray<ChargingPoolTemplate_Id>  TemplateIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging pool.
        /// </summary>
        public ChargingPool(ChargingPool_Id                              Id,
                            I18NString?                                  Name                         = null,
                            I18NString?                                  Description                  = null,
                            Address?                                     Address                      = null,
                            GeoCoordinate?                               GeoLocation                  = null,
                            ParkingType?                                 ParkingType                  = null,
                            AccessibilityType?                           Accessibility                = null,
                            PhoneNumber?                                 HotlinePhoneNumber           = null,
                            IEnumerable<DataLicense>?                    DataLicenses                 = null,
                            IEnumerable<ChargingStation>?                ChargingStations             = null,
                            GridConnectionPoint_Id?                      GridConnectionPointId        = null,
                            IEnumerable<EnergyMeter>?                    EnergyMeters                 = null,
                            IEnumerable<Brand>?                          Brands                       = null,
                            ImmutableOpeningTimes?                       OpeningTimes                 = null,
                            EnergyMix?                                   EnergyMix                    = null,
                            IEnumerable<Image>?                          Images                       = null,
                            Time_Zone?                                   TimeZone                     = null,
                            IEnumerable<Facility>?                       Facilities                   = null,
                            IEnumerable<PaymentOptions>?                 PaymentOptions               = null,
                            IEnumerable<AuthenticationMode>?             AuthenticationModes          = null,
                            IEnumerable<UIFeatures>?                     UIFeatures                   = null,
                            IEnumerable<ChargingPoolFeature>?            Features                     = null,
                            IEnumerable<Languages>?                      LocationLanguages            = null,
                            Address?                                     EntranceAddress              = null,
                            GeoCoordinate?                               EntranceLocation             = null,
                            Address?                                     ExitAddress                  = null,
                            GeoCoordinate?                               ExitLocation                 = null,
                            I18NString?                                  ArrivalInstructions          = null,
                            Boolean?                                     ChargingWhenClosed           = null,
                            IEnumerable<ParkingRestrictionGroup>?        ParkingRestrictions          = null,
                            IEnumerable<AdditionalGeoLocation>?          RelatedLocations             = null,
                            IEnumerable<LocationService>?                Services                     = null,
                            I18NString?                                  AssistanceServiceDetails     = null,
                            IEnumerable<Standard>?                       Standards                    = null,
                            IEnumerable<NUTSCode>?                       NUTSCodes                    = null,

                            IEnumerable<ChargingPoolTemplate_Id>?        TemplateIds                  = null,

                            Timestamped<ChargingPoolAdminStatusType>?    InitialAdminStatus           = null,
                            Timestamped<ChargingPoolStatusType>?         InitialStatus                = null,
                            UInt16?                                      MaxAdminStatusScheduleSize   = null,
                            UInt16?                                      MaxStatusScheduleSize        = null,

                            String?                                      DataSource                   = null,
                            DateTimeOffset?                              Created                      = null,
                            DateTimeOffset?                              LastChange                   = null,

                            CustomDataNew?                               CustomData                   = null,
                            UserDefinedDictionary?                       InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingPoolAdminStatusType>(Created ?? Timestamp.Now, ChargingPoolAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingPoolStatusType>     (Created ?? Timestamp.Now, ChargingPoolStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.TemplateIds  = POIJSON.UniqueIds(TemplateIds, $"template of the charging pool '{Id}'");

            var dataLicenses      = (DataLicenses     ?? []).ToArray();
            var chargingStations  = (ChargingStations ?? []).ToArray();

            if (dataLicenses.Any(dataLicense => dataLicense is null))
                throw new ArgumentException("Data licenses must not contain null entries!", nameof(DataLicenses));

            if (dataLicenses.Select(dataLicense => dataLicense.Id).Distinct().Count() != dataLicenses.Length)
                throw new ArgumentException("A data license must not be given twice!", nameof(DataLicenses));

            if (chargingStations.Any(chargingStation => chargingStation is null))
                throw new ArgumentException("Charging stations must not contain null entries!", nameof(ChargingStations));

            foreach (var chargingStation in chargingStations)
                if (chargingStation.Id.OperatorId != Id.OperatorId)
                    throw new ArgumentException($"The charging station '{chargingStation.Id}' does not belong to the operator '{Id.OperatorId}' of the charging pool '{Id}'!", nameof(ChargingStations));

            this.Address               = Address;
            this.GeoLocation           = GeoLocation;
            this.ParkingType           = ParkingType;
            this.Accessibility         = Accessibility;
            this.HotlinePhoneNumber    = HotlinePhoneNumber;
            this.DataLicenses          = [.. dataLicenses.Ordered()];
            this.ChargingStations      = new (chargingStations, chargingStation => chargingStation.Id, "charging station", $"the charging pool '{Id}'");
            this.GridConnectionPointId = GridConnectionPointId;
            this.EnergyMeters          = EnergyMeter.Ordered(EnergyMeters, $"the charging pool '{Id}'");
            this.Brands                = new (Brands, brand => brand.Id, "brand", $"the charging pool '{Id}'");
            this.OpeningTimes          = OpeningTimes;
            this.EnergyMix             = EnergyMix;
            this.Images = POIJSON.Distinct(Images, image => image.URL.ToString(), $"image of the charging pool '{Id}'");

            this.TimeZone             = TimeZone;
            this.Facilities           = POIJSON.OrderedIds(Facilities,          $"facility of the charging pool '{Id}'");
            this.PaymentOptions       = POIJSON.OrderedIds(PaymentOptions,      $"payment option of the charging pool '{Id}'");
            this.AuthenticationModes  = [.. POIJSON.Distinct(AuthenticationModes, authenticationMode => authenticationMode.Type, $"authentication mode of the charging pool '{Id}'").
                                                OrderBy(authenticationMode => authenticationMode.Type, StringComparer.Ordinal)];
            this.UIFeatures           = POIJSON.OrderedIds(UIFeatures,          $"user interface feature of the charging pool '{Id}'");
            this.Features             = POIJSON.OrderedIds(Features,            $"feature of the charging pool '{Id}'");
            this.LocationLanguages    = POIJSON.UniqueIds (LocationLanguages,   $"language of the charging pool '{Id}'");
            this.EntranceAddress      = EntranceAddress;
            this.EntranceLocation     = EntranceLocation;
            this.ExitAddress          = ExitAddress;
            this.ExitLocation         = ExitLocation;
            this.ArrivalInstructions  = ArrivalInstructions ?? ImmutableI18NString.Empty;
            this.ChargingWhenClosed   = ChargingWhenClosed;
            this.ParkingRestrictions  = POIJSON.OrderedIds(ParkingRestrictions, $"parking restriction of the charging pool '{Id}'");
            this.RelatedLocations     = POIJSON.Distinct  (RelatedLocations,    relatedLocation => relatedLocation.ToJSON(Embedded: true).ToString(Newtonsoft.Json.Formatting.None), $"related location of the charging pool '{Id}'");
            this.Services                  = POIJSON.OrderedIds(Services,   $"service of the charging pool '{Id}'");
            this.AssistanceServiceDetails  = AssistanceServiceDetails ?? ImmutableI18NString.Empty;
            this.Standards                 = POIJSON.OrderedIds(Standards,  $"standard of the charging pool '{Id}'");
            this.NUTSCodes                 = POIJSON.OrderedIds(NUTSCodes,  $"NUTS code of the charging pool '{Id}'");
            this.priceStatus               = new PriceStatusRuntime(RaisePriceStatusChanged);

        }

        #endregion


        #region TryGetChargingStation(ChargingStationId, out ChargingStation)

        /// <summary>
        /// Try to return the charging station with the given identification.
        /// </summary>
        public Boolean TryGetChargingStation(ChargingStation_Id                         ChargingStationId,
                                             [NotNullWhen(true)] out ChargingStation?  ChargingStation)

            => ChargingStations.TryGet(ChargingStationId, out ChargingStation);

        #endregion


        #region Price status

        private readonly PriceStatusRuntime                priceStatus;
        private          OnChargingPoolPriceStatusChangedDelegate?  onPriceStatusChanged;

        /// <summary>
        /// The current price status: the new prices of charging tariffs of its operator, e.g. ad hoc prices following
        /// the electricity market; runtime data like the admin status and the status, unless none was reported yet.
        /// </summary>
        public Timestamped<ChargingPrices>?  PriceStatus
            => priceStatus.Current;

        /// <summary>
        /// The next planned price status.
        /// </summary>
        internal Timestamped<ChargingPrices>?  NextPriceStatus
            => priceStatus.Next;

        /// <summary>
        /// Set the price status, since the given timestamp or now; a later timestamp plans it.
        /// </summary>
        /// <param name="PriceStatus">The new prices of charging tariffs of its operator.</param>
        /// <param name="Timestamp">Since when; now, unless given.</param>
        /// <param name="DataSource">An optional source.</param>
        public void SetPriceStatus(ChargingPrices   PriceStatus,
                                   DateTimeOffset?  Timestamp    = null,
                                   Context?         DataSource   = null)

            => priceStatus.Set(PriceStatus, Timestamp ?? org.GraphDefined.Vanaheimr.Illias.Timestamp.Now, DataSource);

        /// <summary>
        /// Take the given price status as it is, also when it repeats the one before.
        /// </summary>
        internal void TakePriceStatus(ChargingPrices  PriceStatus,
                                      DateTimeOffset  Timestamp,
                                      Context?        DataSource)

            => priceStatus.Take(PriceStatus, Timestamp, DataSource);

        /// <summary>
        /// An event fired whenever the price status changed.
        /// </summary>
        public event OnChargingPoolPriceStatusChangedDelegate? OnPriceStatusChanged
        {
            add    { lock (statusEventsLock) { onPriceStatusChanged += value; priceStatus.Listen(onPriceStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onPriceStatusChanged -= value; priceStatus.Listen(onPriceStatusChanged is not null); } }
        }

        private async Task RaisePriceStatusChanged(DateTimeOffset               Timestamp,
                                                   EventTracking_Id             EventTrackingId,
                                                   Timestamped<ChargingPrices>  NewPriceStatus,
                                                   Timestamped<ChargingPrices>  OldPriceStatus,
                                                   Context?                     DataSource)
        {
            var onPriceStatusChanged = this.onPriceStatusChanged;
            if (onPriceStatusChanged is not null)
                await onPriceStatusChanged(Timestamp, EventTrackingId, this, OldPriceStatus, NewPriceStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override void ShareFurtherRuntimeOf(AImmutableEMobilityEntity<ChargingPool_Id, ChargingPoolAdminStatusType, ChargingPoolStatusType> Previous)
        {
            if (Previous is ChargingPool previousChargingPool)
                priceStatus.ShareOf(previousChargingPool.priceStatus);
        }

        #endregion

        #region Status events

        private readonly Lock                                       statusEventsLock  = new ();
        private          OnChargingPoolAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingPoolStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingPoolAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingPoolStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                            Timestamp,
                                                                   EventTracking_Id                          EventTrackingId,
                                                                   Timestamped<ChargingPoolAdminStatusType>  NewStatus,
                                                                   Timestamped<ChargingPoolAdminStatusType>  OldStatus,
                                                                   Context?                                  DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                       Timestamp,
                                                              EventTracking_Id                     EventTrackingId,
                                                              Timestamped<ChargingPoolStatusType>  NewStatus,
                                                              Timestamped<ChargingPoolStatusType>  OldStatus,
                                                              Context?                             DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region (protected override) ContentJSON()

        /// <inheritdoc/>
        protected override JObject ContentJSON()

            => ToJSON(Embedded: true);

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this charging pool: with its data licenses and charging stations.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure, e.g. into its operator.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomChargingPoolSerializer">An optional delegate to customize the JSON representation.</param>
        public JObject ToJSON(Boolean                                         Embedded                       = false,
                              Boolean                                         IncludeETags                   = false,
                              ETagDigestEncoding?                             ETagDigestEncoding             = null,
                              CustomJObjectSerializerDelegate<ChargingPool>?  CustomChargingPoolSerializer   = null)

            => ToJSON(IncludeChildArrays:            true,
                      Embedded:                      Embedded,
                      IncludeETags:                  IncludeETags,
                      ETagDigestEncoding:            ETagDigestEncoding,
                      CustomChargingPoolSerializer:  CustomChargingPoolSerializer);

        /// <summary>
        /// The JSON representation of this charging pool, with or without its child arrays: without them for the skeleton of its
        /// tree (SPECIFICATION.md, section 8.2), which names their roots in their place; embedded, unless told otherwise.
        /// </summary>
        /// <param name="IncludeChildArrays">Whether to include its child arrays.</param>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomChargingPoolSerializer">An optional delegate to customize the JSON representation.</param>
        internal JObject ToJSON(Boolean                                         IncludeChildArrays,
                                Boolean                                         Embedded                       = true,
                                Boolean                                         IncludeETags                   = false,
                                ETagDigestEncoding?                             ETagDigestEncoding             = null,
                                CustomJObjectSerializerDelegate<ChargingPool>?  CustomChargingPoolSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,    JSONLDContext);
            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,              Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,       Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,        DataSource);

            if (DataLicenses.Length > 0)
                json.Add(BasePropertyKeys.DataLicenses,      DataLicenses.ToPOIJSON());

            if (Address is not null)
                json.Add(PropertyKeys.Address,           Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,       POIJSON.GeoCoordinateJSON(GeoLocation.Value));

            if (ParkingType.HasValue)
                json.Add(PropertyKeys.ParkingType,       ParkingType.Value.ToString());

            if (Accessibility.HasValue)
                json.Add(PropertyKeys.Accessibility,     Accessibility.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,           HotlinePhoneNumber.Value.ToString());

            if (GridConnectionPointId.HasValue)
                json.Add(PropertyKeys.GridConnectionPointId,  GridConnectionPointId.Value.ToString());

            if (EnergyMeters.Length > 0)
                json.Add(PropertyKeys.EnergyMeters,      new JArray(EnergyMeters.Select(energyMeter => energyMeter.ToJSON(Embedded: true))));

            if (IncludeChildArrays && ChargingStations.Count > 0)
                json.Add(PropertyKeys.ChargingStations,  new JArray(ChargingStations.Select(chargingStation => chargingStation.ToJSON(Embedded: true))));

            if (Brands.Count > 0)
                json.Add(PropertyKeys.Brands,        new JArray(Brands.Select(brand => brand.ToJSON(Embedded: true))));

            if (OpeningTimes is not null)
                json.Add(PropertyKeys.OpeningTimes,  OpeningTimes.ToJSON());

            if (EnergyMix is not null)
                json.Add(PropertyKeys.EnergyMix,     EnergyMix.ToJSON(Embedded: true));

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,  new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (TimeZone.HasValue)
                json.Add(PropertyKeys.TimeZone,             TimeZone.Value.ToString());

            if (Facilities.Length > 0)
                json.Add(PropertyKeys.Facilities,           POIJSON.IdsToJSON(Facilities));

            if (PaymentOptions.Length > 0)
                json.Add(PropertyKeys.PaymentOptions,       POIJSON.IdsToJSON(PaymentOptions));

            if (AuthenticationModes.Length > 0)
                json.Add(PropertyKeys.AuthenticationModes,  new JArray(AuthenticationModes.Select(authenticationMode => authenticationMode.ToJSON(Embedded: true))));

            if (UIFeatures.Length > 0)
                json.Add(PropertyKeys.UIFeatures,           POIJSON.IdsToJSON(UIFeatures));

            if (Features.Length > 0)
                json.Add(PropertyKeys.Features,             POIJSON.IdsToJSON(Features));

            if (LocationLanguages.Length > 0)
                json.Add(PropertyKeys.LocationLanguages,    POIJSON.IdsToJSON(LocationLanguages));

            if (EntranceAddress is not null)
                json.Add(PropertyKeys.EntranceAddress,      EntranceAddress.ToJSON(Embedded: true));

            if (EntranceLocation.HasValue)
                json.Add(PropertyKeys.EntranceLocation,     POIJSON.GeoCoordinateJSON(EntranceLocation.Value));

            if (ExitAddress is not null)
                json.Add(PropertyKeys.ExitAddress,          ExitAddress.ToJSON(Embedded: true));

            if (ExitLocation.HasValue)
                json.Add(PropertyKeys.ExitLocation,         POIJSON.GeoCoordinateJSON(ExitLocation.Value));

            if (ArrivalInstructions.IsNotNullOrEmpty())
                json.Add(PropertyKeys.ArrivalInstructions,  ArrivalInstructions.ToJSON());

            if (ChargingWhenClosed.HasValue)
                json.Add(PropertyKeys.ChargingWhenClosed,   ChargingWhenClosed.Value);

            if (ParkingRestrictions.Length > 0)
                json.Add(PropertyKeys.ParkingRestrictions,  POIJSON.IdsToJSON(ParkingRestrictions));

            if (RelatedLocations.Length > 0)
                json.Add(PropertyKeys.RelatedLocations,     new JArray(RelatedLocations.Select(relatedLocation => relatedLocation.ToJSON(Embedded: true))));

            if (Services.Length > 0)
                json.Add(PropertyKeys.Services,                  POIJSON.IdsToJSON(Services));

            if (AssistanceServiceDetails.IsNotNullOrEmpty())
                json.Add(PropertyKeys.AssistanceServiceDetails,  AssistanceServiceDetails.ToJSON());

            if (Standards.Length > 0)
                json.Add(PropertyKeys.Standards,                 POIJSON.IdsToJSON(Standards));

            if (NUTSCodes.Length > 0)
                json.Add(PropertyKeys.NUTSCodes,                 POIJSON.IdsToJSON(NUTSCodes));

            if (TemplateIds.Length > 0)
                json.Add(PropertyKeys.TemplateIds, POIJSON.IdsToJSON(TemplateIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingPool, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging pool, with its charging stations and EVSEs.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingPool">The parsed charging pool.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                 JSON,
                                       [NotNullWhen(true)]  out ChargingPool?  ChargingPool,
                                       [NotNullWhen(false)] out String?        ErrorResponse)
        {

            ChargingPool = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "charging pool identification",
                                         ChargingPool_Id.TryParse,
                                         out ChargingPool_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging pool '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N       (JSON, BasePropertyKeys.Name,            "charging pool name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, BasePropertyKeys.Description,     "charging pool description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, BasePropertyKeys.DataSource,      "data source",                out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, PropertyKeys.Address,             "address",                    out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.GeoLocation,         "geo location",               out var geoLocation,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.ParkingType,         "parking type",               POI.ParkingType.      TryParse, out ParkingType?        parkingType,    out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.Accessibility,       "accessibility",              POI.AccessibilityType.TryParse, out AccessibilityType?  accessibility,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",       PhoneNumber.          TryParse, out PhoneNumber?        hotline,        out ErrorResponse))
                {
                    return false;
                }

                if (!POIDataLicenses.TryParse(JSON[BasePropertyKeys.DataLicenses],
                                              JSON[BasePropertyKeys.DataLicenseIds],
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<ChargingStation>(JSON,
                                                               PropertyKeys.ChargingStations,
                                                               PropertyKeys.ChargingStationIds,
                                                               "charging stations",
                                                               ChargingStation.TryParse,
                                                               out var chargingStations,
                                                               out ErrorResponse))
                {
                    return false;
                }

                if (JSON[PropertyKeys.GridConnectionPoint] is not null)
                {
                    ErrorResponse = $"The grid connection point of the charging pool '{id}' is embedded in its grid operator: name it by '{PropertyKeys.GridConnectionPointId}'!";
                    return false;
                }

                if (!POIJSON.TryParseText(JSON, PropertyKeys.GridConnectionPointId,  "grid connection point identification", GridConnectionPoint_Id.TryParse, out GridConnectionPoint_Id? gridConnectionPointId, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<EnergyMeter>(JSON,
                                                           PropertyKeys.EnergyMeters,
                                                           PropertyKeys.EnergyMeterIds,
                                                           "energy meters",
                                                           EnergyMeter.TryParse,
                                                           out var energyMeters,
                                                           out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<Brand>(JSON, PropertyKeys.Brands, PropertyKeys.BrandIds, "brands", Brand.TryParse, out var brands, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<ImmutableOpeningTimes>(JSON, PropertyKeys.OpeningTimes, "opening times", ImmutableOpeningTimes.TryParse, out var openingTimes, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<EnergyMix>(JSON, PropertyKeys.EnergyMix, "energy mix", (JObject json, out EnergyMix? energyMix, out String? error) => POI.EnergyMix.TryParse(json, out energyMix, out error), out var energyMix, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, PropertyKeys.Images, "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseText       (JSON, PropertyKeys.TimeZone,                  "time zone",               Time_Zone.TryParse,                       out Time_Zone? timeZone,                                        out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Facilities,                "facility",                Facility.TryParse,                        out ImmutableArray<Facility>                facilities,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.PaymentOptions,            "payment option",          POIJSON.TryParseName<POI.PaymentOptions>, out ImmutableArray<POI.PaymentOptions>      paymentOptions,       out ErrorResponse) ||
                    !POIJSON.TryParseObjects    (JSON, PropertyKeys.AuthenticationModes,       "authentication mode",     AuthenticationMode.TryParse,              out ImmutableArray<AuthenticationMode>      authenticationModes,  out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.UIFeatures,                "user interface feature",  POI.UIFeatures.TryParse,                  out ImmutableArray<POI.UIFeatures>          uiFeatures,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Features,                  "charging pool feature",   ChargingPoolFeature.TryParse,             out ImmutableArray<ChargingPoolFeature>     features,             out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.LocationLanguages,         "location language",       POIJSON.TryParseName<Languages>,          out ImmutableArray<Languages>               locationLanguages,    out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, PropertyKeys.EntranceAddress,           "entrance address",                                                  out var entranceAddress,                                        out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.EntranceLocation,          "entrance location",                                                 out var entranceLocation,                                       out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, PropertyKeys.ExitAddress,               "exit address",                                                      out var exitAddress,                                            out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.ExitLocation,              "exit location",                                                     out var exitLocation,                                           out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, PropertyKeys.ArrivalInstructions,       "arrival instructions",                                              out var arrivalInstructions,                                    out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.ParkingRestrictions,       "parking restriction",     ParkingRestrictionGroup.TryParse,         out ImmutableArray<ParkingRestrictionGroup> parkingRestrictions,  out ErrorResponse) ||
                    !POIJSON.TryParseObjects    (JSON, PropertyKeys.RelatedLocations,          "related location",        AdditionalGeoLocation.TryParse,           out ImmutableArray<AdditionalGeoLocation>   relatedLocations,     out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Services,                  "location service",        LocationService.TryParse,                 out ImmutableArray<LocationService>         services,             out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, PropertyKeys.AssistanceServiceDetails,  "assistance service details",                                    out var assistanceServiceDetails,                               out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Standards,                 "standard",                Standard.TryParse,                        out ImmutableArray<Standard>                standards,            out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.NUTSCodes,                 "NUTS code",               NUTSCode.TryParse,                        out ImmutableArray<NUTSCode>                nutsCodes,            out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.ChargingWhenClosed, "charging when closed", out Boolean? chargingWhenClosed, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIJSON.TryParseIds(JSON, PropertyKeys.TemplateIds,  "template identification", ChargingPoolTemplate_Id.TryParse, out ImmutableArray<ChargingPoolTemplate_Id> templateIds, out ErrorResponse))
                    return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingPool = new ChargingPool(id,
                                                name,
                                                description,
                                                address,
                                                geoLocation,
                                                parkingType,
                                                accessibility,
                                                hotline,
                                                dataLicenses,
                                                chargingStations,
                                                gridConnectionPointId,
                                                energyMeters,
                                                brands,
                                                openingTimes,
                                                energyMix,
                                                images,
                                                timeZone,
                                                facilities,
                                                paymentOptions,
                                                authenticationModes,
                                                uiFeatures,
                                                features,
                                                locationLanguages,
                                                entranceAddress,
                                                entranceLocation,
                                                exitAddress,
                                                exitLocation,
                                                arrivalInstructions,
                                                chargingWhenClosed,
                                                parkingRestrictions,
                                                relatedLocations,
                                                services,
                                                assistanceServiceDetails,
                                                standards,
                                                nutsCodes,
                                                TemplateIds:  templateIds,
                                                DataSource:  dataSource,
                                                Created:     created,
                                                LastChange:  lastChange,
                                                CustomData:  customData);

                if (!TryCheckETagsOf(JSON, ChargingPool, out ErrorResponse))
                {
                    ChargingPool = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingPool   = null;
                ErrorResponse  = "The given JSON representation of a charging pool is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging pools for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingPool? ChargingPool1, ChargingPool? ChargingPool2)

            => ReferenceEquals(ChargingPool1, ChargingPool2) ||
               (ChargingPool1 is not null && ChargingPool1.Equals(ChargingPool2));

        /// <summary>
        /// Compares two charging pools for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingPool? ChargingPool1, ChargingPool? ChargingPool2)

            => !(ChargingPool1 == ChargingPool2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingPool chargingPool
                   ? CompareTo(chargingPool)
                   : throw new ArgumentException("The given object is not a charging pool!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingPool? ChargingPool)

            => ChargingPool is null
                   ? throw new ArgumentNullException(nameof(ChargingPool), "The given charging pool must not be null!")
                   : Id.CompareTo(ChargingPool.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPool chargingPool &&
               Equals(chargingPool);

        /// <inheritdoc/>
        public Boolean Equals(ChargingPool? ChargingPool)

            => ChargingPool is not null &&
               Id.Equals(ChargingPool.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
