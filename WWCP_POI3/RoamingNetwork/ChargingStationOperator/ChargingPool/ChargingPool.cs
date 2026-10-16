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
                                                                  Timestamped<ChargingPoolAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of a charging pool changed.
    /// </summary>
    public delegate Task OnChargingPoolStatusChangedDelegate(DateTimeOffset                        Timestamp,
                                                             EventTracking_Id                      EventTrackingId,
                                                             ChargingPool                          ChargingPool,
                                                             Timestamped<ChargingPoolStatusType>   OldStatus,
                                                             Timestamped<ChargingPoolStatusType>   NewStatus);


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


        #endregion

        #region Properties

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
        /// The brands it is offered under, of the roaming network, in the order of their identifications.
        /// </summary>
        public ImmutableArray<Brand_Id>  BrandIds  { get; }

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
                            IEnumerable<Brand_Id>?                       BrandIds                     = null,
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
            this.BrandIds              = POIJSON.OrderedIds(BrandIds, $"brand of the charging pool '{Id}'");
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
            this.RelatedLocations     = POIJSON.Distinct  (RelatedLocations,    relatedLocation => relatedLocation.ToJSON().ToString(Newtonsoft.Json.Formatting.None), $"related location of the charging pool '{Id}'");
            this.Services                  = POIJSON.OrderedIds(Services,   $"service of the charging pool '{Id}'");
            this.AssistanceServiceDetails  = AssistanceServiceDetails ?? ImmutableI18NString.Empty;
            this.Standards                 = POIJSON.OrderedIds(Standards,  $"standard of the charging pool '{Id}'");
            this.NUTSCodes                 = POIJSON.OrderedIds(NUTSCodes,  $"NUTS code of the charging pool '{Id}'");

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
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
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
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging pool, as its operator embeds it:
        /// with its data licenses and charging stations.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",              Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",       Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",        DataSource);

            if (DataLicenses.Length > 0)
                json.Add("dataLicenses",      DataLicenses.ToPOIJSON());

            if (Address is not null)
                json.Add("address",           Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",       GeoLocation.Value.ToJSON(Embedded: true));

            if (ParkingType.HasValue)
                json.Add("parkingType",       ParkingType.Value.ToString());

            if (Accessibility.HasValue)
                json.Add("accessibility",     Accessibility.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",           HotlinePhoneNumber.Value.ToString());

            if (GridConnectionPointId.HasValue)
                json.Add("gridConnectionPointId",  GridConnectionPointId.Value.ToString());

            if (EnergyMeters.Length > 0)
                json.Add("energyMeters",      new JArray(EnergyMeters.Select(energyMeter => energyMeter.ToJSON())));

            if (ChargingStations.Count > 0)
                json.Add("chargingStations",  new JArray(ChargingStations.Select(chargingStation => chargingStation.ToJSON())));

            if (BrandIds.Length > 0)
                json.Add("brandIds",      POIJSON.IdsToJSON(BrandIds));

            if (OpeningTimes is not null)
                json.Add("openingTimes",  OpeningTimes.ToJSON());

            if (EnergyMix is not null)
                json.Add("energyMix",     EnergyMix.ToJSON(Embedded: true));

            if (Images.Length > 0)
                json.Add("images",  new JArray(Images.Select(image => image.ToJSON())));

            if (TimeZone.HasValue)
                json.Add("timeZone",             TimeZone.Value.ToString());

            if (Facilities.Length > 0)
                json.Add("facilities",           POIJSON.IdsToJSON(Facilities));

            if (PaymentOptions.Length > 0)
                json.Add("paymentOptions",       POIJSON.IdsToJSON(PaymentOptions));

            if (AuthenticationModes.Length > 0)
                json.Add("authenticationModes",  new JArray(AuthenticationModes.Select(authenticationMode => authenticationMode.ToJSON())));

            if (UIFeatures.Length > 0)
                json.Add("uiFeatures",           POIJSON.IdsToJSON(UIFeatures));

            if (Features.Length > 0)
                json.Add("features",             POIJSON.IdsToJSON(Features));

            if (LocationLanguages.Length > 0)
                json.Add("locationLanguages",    POIJSON.IdsToJSON(LocationLanguages));

            if (EntranceAddress is not null)
                json.Add("entranceAddress",      EntranceAddress.ToJSON(Embedded: true));

            if (EntranceLocation.HasValue)
                json.Add("entranceLocation",     EntranceLocation.Value.ToJSON(Embedded: true));

            if (ExitAddress is not null)
                json.Add("exitAddress",          ExitAddress.ToJSON(Embedded: true));

            if (ExitLocation.HasValue)
                json.Add("exitLocation",         ExitLocation.Value.ToJSON(Embedded: true));

            if (ArrivalInstructions.IsNotNullOrEmpty())
                json.Add("arrivalInstructions",  ArrivalInstructions.ToJSON());

            if (ChargingWhenClosed.HasValue)
                json.Add("chargingWhenClosed",   ChargingWhenClosed.Value);

            if (ParkingRestrictions.Length > 0)
                json.Add("parkingRestrictions",  POIJSON.IdsToJSON(ParkingRestrictions));

            if (RelatedLocations.Length > 0)
                json.Add("relatedLocations",     new JArray(RelatedLocations.Select(relatedLocation => relatedLocation.ToJSON())));

            if (Services.Length > 0)
                json.Add("services",                  POIJSON.IdsToJSON(Services));

            if (AssistanceServiceDetails.IsNotNullOrEmpty())
                json.Add("assistanceServiceDetails",  AssistanceServiceDetails.ToJSON());

            if (Standards.Length > 0)
                json.Add("standards",                 POIJSON.IdsToJSON(Standards));

            if (NUTSCodes.Length > 0)
                json.Add("nutsCodes",                 POIJSON.IdsToJSON(NUTSCodes));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

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

                if (!JSON.ParseMandatory("@id",
                                         "charging pool identification",
                                         ChargingPool_Id.TryParse,
                                         out ChargingPool_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N       (JSON, "name",           "charging pool name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "description",    "charging pool description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "dataSource",     "data source",                out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON,                                                 out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON,                                                 out var geoLocation,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "parkingType",    "parking type",               POI.ParkingType.      TryParse, out ParkingType?        parkingType,    out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "accessibility",  "accessibility",              POI.AccessibilityType.TryParse, out AccessibilityType?  accessibility,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "hotline",        "hotline phone number",       PhoneNumber.          TryParse, out PhoneNumber?        hotline,        out ErrorResponse))
                {
                    return false;
                }

                if (!POIDataLicenses.TryParse(JSON["dataLicenses"],
                                              JSON["dataLicenseIds"],
                                              out var dataLicenses,
                                              out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<ChargingStation>(JSON,
                                                               "chargingStations",
                                                               "chargingStationIds",
                                                               "charging stations",
                                                               ChargingStation.TryParse,
                                                               out var chargingStations,
                                                               out ErrorResponse))
                {
                    return false;
                }

                if (JSON["gridConnectionPoint"] is not null)
                {
                    ErrorResponse = $"The grid connection point of the charging pool '{id}' is embedded in its grid operator: name it by 'gridConnectionPointId'!";
                    return false;
                }

                if (!POIJSON.TryParseText(JSON, "gridConnectionPointId", "grid connection point identification", GridConnectionPoint_Id.TryParse, out GridConnectionPoint_Id? gridConnectionPointId, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<EnergyMeter>(JSON,
                                                           "energyMeters",
                                                           "energyMeterIds",
                                                           "energy meters",
                                                           EnergyMeter.TryParse,
                                                           out var energyMeters,
                                                           out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseIds(JSON, "brandIds", "brand identification", Brand_Id.TryParse, out ImmutableArray<Brand_Id> brandIds, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<ImmutableOpeningTimes>(JSON, "openingTimes", "opening times", ImmutableOpeningTimes.TryParse, out var openingTimes, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<EnergyMix>(JSON, "energyMix", "energy mix", (JObject json, out EnergyMix? energyMix, out String? error) => POI.EnergyMix.TryParse(json, out energyMix, out error), out var energyMix, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, "images", "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseText       (JSON, "timeZone",             "time zone",               Time_Zone.TryParse,                       out Time_Zone? timeZone,                                        out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "facilities",           "facility",                Facility.TryParse,                        out ImmutableArray<Facility>                facilities,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "paymentOptions",       "payment option",          POIJSON.TryParseName<POI.PaymentOptions>, out ImmutableArray<POI.PaymentOptions>      paymentOptions,       out ErrorResponse) ||
                    !POIJSON.TryParseObjects    (JSON, "authenticationModes",  "authentication mode",     AuthenticationMode.TryParse,              out ImmutableArray<AuthenticationMode>      authenticationModes,  out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "uiFeatures",           "user interface feature",  POI.UIFeatures.TryParse,                  out ImmutableArray<POI.UIFeatures>          uiFeatures,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "features",             "charging pool feature",   ChargingPoolFeature.TryParse,             out ImmutableArray<ChargingPoolFeature>     features,             out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "locationLanguages",    "location language",       POIJSON.TryParseName<Languages>,          out ImmutableArray<Languages>               locationLanguages,    out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, "entranceAddress",      "entrance address",                                                  out var entranceAddress,                                        out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, "entranceLocation",     "entrance location",                                                 out var entranceLocation,                                       out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON, "exitAddress",          "exit address",                                                      out var exitAddress,                                            out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, "exitLocation",         "exit location",                                                     out var exitLocation,                                           out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "arrivalInstructions",  "arrival instructions",                                              out var arrivalInstructions,                                    out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "parkingRestrictions",  "parking restriction",     ParkingRestrictionGroup.TryParse,         out ImmutableArray<ParkingRestrictionGroup> parkingRestrictions,  out ErrorResponse) ||
                    !POIJSON.TryParseObjects    (JSON, "relatedLocations",     "related location",        AdditionalGeoLocation.TryParse,           out ImmutableArray<AdditionalGeoLocation>   relatedLocations,     out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "services",             "location service",        LocationService.TryParse,                 out ImmutableArray<LocationService>         services,             out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "assistanceServiceDetails", "assistance service details",                                    out var assistanceServiceDetails,                               out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "standards",            "standard",                Standard.TryParse,                        out ImmutableArray<Standard>                standards,            out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "nutsCodes",            "NUTS code",               NUTSCode.TryParse,                        out ImmutableArray<NUTSCode>                nutsCodes,            out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional("chargingWhenClosed", "charging when closed", out Boolean? chargingWhenClosed, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

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
                                                brandIds,
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
                                                DataSource:  dataSource,
                                                Created:     created,
                                                LastChange:  lastChange,
                                                CustomData:  customData);

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
