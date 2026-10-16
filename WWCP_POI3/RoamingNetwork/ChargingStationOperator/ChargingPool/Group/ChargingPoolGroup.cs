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
    /// The admin status of a charging pool group changed.
    /// </summary>
    public delegate Task OnChargingPoolGroupAdminStatusChangedDelegate(DateTimeOffset                                  Timestamp,
                                                                       EventTracking_Id                                EventTrackingId,
                                                                       ChargingPoolGroup                               ChargingPoolGroup,
                                                                       Timestamped<ChargingPoolGroupAdminStatusType>   OldStatus,
                                                                       Timestamped<ChargingPoolGroupAdminStatusType>   NewStatus,
                                                                       Context?                                        DataSource);

    /// <summary>
    /// The status of a charging pool group changed.
    /// </summary>
    public delegate Task OnChargingPoolGroupStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  ChargingPoolGroup                          ChargingPoolGroup,
                                                                  Timestamped<ChargingPoolGroupStatusType>   OldStatus,
                                                                  Timestamped<ChargingPoolGroupStatusType>   NewStatus,
                                                                  Context?                                   DataSource);


    /// <summary>
    /// A group of charging pools of one charging station operator, embedded completely in it; groups may overlap.
    /// It may give its members the properties of a charging pool, which they take at runtime, as the effective
    /// charging pools of their operator: a value of their own wins, arrays are united, and of the groups of a member
    /// the one with the higher priority wins.
    /// </summary>
    public sealed class ChargingPoolGroup : AImmutableEMobilityEntity<ChargingPoolGroup_Id,
                                                                      ChargingPoolGroupAdminStatusType,
                                                                      ChargingPoolGroupStatusType>,
                                            IPOIPrioritizedGroup,
                                            IEquatable<ChargingPoolGroup>,
                                            IComparable<ChargingPoolGroup>
    {

        #region Properties

        /// <summary>
        /// Its charging pools, of the same charging station operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingPool_Id>  ChargingPoolIds   { get; }

        /// <summary>
        /// The charging tariff of its operator for its EVSEs without tariffs of their own, unless more specific groups
        /// name one: EVSE groups before charging station groups before charging pool groups, of one level the one with the
        /// highest priority (ChargingStationOperator.EffectiveEVSE(…)).
        /// </summary>
        public ChargingTariff_Id?               ChargingTariffId  { get; }

        /// <summary>
        /// Its priority over the other groups of a member, where they give it different values: the higher wins;
        /// 0, unless given. Groups of the same member with the same priority must not give it different values.
        /// </summary>
        public Int32?  Priority  { get; }

        /// <inheritdoc/>
        String IPOIPrioritizedGroup.GroupId
            => Id.ToString();


        /// <summary>
        /// The address of its members, unless they have their own.
        /// </summary>
        public Address?                                  Address                   { get; }

        /// <summary>
        /// The geographical location of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                            GeoLocation               { get; }

        /// <summary>
        /// The type of parking of its members, unless they tell their own.
        /// </summary>
        public ParkingType?                              ParkingType               { get; }

        /// <summary>
        /// Who may use its members, unless they tell it themselves.
        /// </summary>
        public AccessibilityType?                        Accessibility             { get; }

        /// <summary>
        /// The hotline phone number of its members, unless they have their own.
        /// </summary>
        public PhoneNumber?                              HotlinePhoneNumber        { get; }

        /// <summary>
        /// Data licenses of its members, besides their own, ordered.
        /// </summary>
        public ImmutableArray<DataLicense>               DataLicenses              { get; }

        /// <summary>
        /// The grid connection point of its members, one of its roaming network, unless they have their own.
        /// </summary>
        public GridConnectionPoint_Id?                   GridConnectionPointId     { get; }

        /// <summary>
        /// Brands its members also appear under, besides their own, in the order of their identifications.
        /// </summary>
        public POICollection<Brand_Id, Brand>            Brands                    { get; }

        /// <summary>
        /// When its members are open, unless they have their own.
        /// </summary>
        public ImmutableOpeningTimes?                    OpeningTimes              { get; }

        /// <summary>
        /// The energy mix of its members, unless they have their own.
        /// </summary>
        public EnergyMix?                                EnergyMix                 { get; }

        /// <summary>
        /// Images of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<Image>                     Images                    { get; }

        /// <summary>
        /// The time zone of its members, unless they have their own.
        /// </summary>
        public Time_Zone?                                TimeZone                  { get; }

        /// <summary>
        /// Facilities of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<Facility>                  Facilities                { get; }

        /// <summary>
        /// Payment options of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<PaymentOptions>            PaymentOptions            { get; }

        /// <summary>
        /// Authentication modes of its members, besides their own, in the order of their types; of the same type their own.
        /// </summary>
        public ImmutableArray<AuthenticationMode>        AuthenticationModes       { get; }

        /// <summary>
        /// User interface features of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<UIFeatures>                UIFeatures                { get; }

        /// <summary>
        /// Features of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<ChargingPoolFeature>       Features                  { get; }

        /// <summary>
        /// Languages of its members, after their own, in the given order.
        /// </summary>
        public ImmutableArray<Languages>                 LocationLanguages         { get; }

        /// <summary>
        /// The address of the entrance of its members, unless they have their own.
        /// </summary>
        public Address?                                  EntranceAddress           { get; }

        /// <summary>
        /// The location of the entrance of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                            EntranceLocation          { get; }

        /// <summary>
        /// The address of the exit of its members, unless they have their own.
        /// </summary>
        public Address?                                  ExitAddress               { get; }

        /// <summary>
        /// The location of the exit of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                            ExitLocation              { get; }

        /// <summary>
        /// The arrival instructions of its members, unless they have their own; empty gives none.
        /// </summary>
        public ImmutableI18NString                       ArrivalInstructions       { get; }

        /// <summary>
        /// Whether its members charge while they are closed, unless they tell it themselves.
        /// </summary>
        public Boolean?                                  ChargingWhenClosed        { get; }

        /// <summary>
        /// Parking restrictions of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<ParkingRestrictionGroup>   ParkingRestrictions       { get; }

        /// <summary>
        /// Related locations of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<AdditionalGeoLocation>     RelatedLocations          { get; }

        /// <summary>
        /// Services of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<LocationService>           Services                  { get; }

        /// <summary>
        /// The details of the assistance service of its members, unless they have their own; empty gives none.
        /// </summary>
        public ImmutableI18NString                       AssistanceServiceDetails  { get; }

        /// <summary>
        /// Standards of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>                  Standards                 { get; }

        /// <summary>
        /// NUTS codes of its members, besides their own, in the order of their codes.
        /// </summary>
        public ImmutableArray<NUTSCode>                  NUTSCodes                 { get; }

        /// <summary>
        /// Whether it gives its members any property of a charging pool.
        /// </summary>
        public Boolean HasChargingPoolProperties

            => Address is not null              || GeoLocation.HasValue            || ParkingType.HasValue          || Accessibility.HasValue        ||
               HotlinePhoneNumber.HasValue      || DataLicenses.Length > 0         || GridConnectionPointId.HasValue || Brands.Count > 0              ||
               OpeningTimes is not null         || EnergyMix is not null           || Images.Length > 0             || TimeZone.HasValue             ||
               Facilities.Length > 0            || PaymentOptions.Length > 0       || AuthenticationModes.Length > 0 || UIFeatures.Length > 0        ||
               Features.Length > 0              || LocationLanguages.Length > 0    || EntranceAddress is not null   || EntranceLocation.HasValue     ||
               ExitAddress is not null          || ExitLocation.HasValue           || ArrivalInstructions.IsNotNullOrEmpty()                      ||
               ChargingWhenClosed.HasValue      || ParkingRestrictions.Length > 0  || RelatedLocations.Length > 0   || Services.Length > 0           ||
               AssistanceServiceDetails.IsNotNullOrEmpty()                         || Standards.Length > 0          || NUTSCodes.Length > 0;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging pool group.
        /// </summary>
        public ChargingPoolGroup(ChargingPoolGroup_Id                             Id,
                                 I18NString?                                      Name                         = null,
                                 I18NString?                                      Description                  = null,
                                 IEnumerable<ChargingPool_Id>?                    ChargingPoolIds              = null,
                                 ChargingTariff_Id?                               ChargingTariffId             = null,
                                 Int32?                                           Priority                     = null,

                                 Address?                                         Address                      = null,
                                 GeoCoordinate?                                   GeoLocation                  = null,
                                 ParkingType?                                     ParkingType                  = null,
                                 AccessibilityType?                               Accessibility                = null,
                                 PhoneNumber?                                     HotlinePhoneNumber           = null,
                                 IEnumerable<DataLicense>?                        DataLicenses                 = null,
                                 GridConnectionPoint_Id?                          GridConnectionPointId        = null,
                                 IEnumerable<Brand>?                              Brands                       = null,
                                 ImmutableOpeningTimes?                           OpeningTimes                 = null,
                                 EnergyMix?                                       EnergyMix                    = null,
                                 IEnumerable<Image>?                              Images                       = null,
                                 Time_Zone?                                       TimeZone                     = null,
                                 IEnumerable<Facility>?                           Facilities                   = null,
                                 IEnumerable<PaymentOptions>?                     PaymentOptions               = null,
                                 IEnumerable<AuthenticationMode>?                 AuthenticationModes          = null,
                                 IEnumerable<UIFeatures>?                         UIFeatures                   = null,
                                 IEnumerable<ChargingPoolFeature>?                Features                     = null,
                                 IEnumerable<Languages>?                          LocationLanguages            = null,
                                 Address?                                         EntranceAddress              = null,
                                 GeoCoordinate?                                   EntranceLocation             = null,
                                 Address?                                         ExitAddress                  = null,
                                 GeoCoordinate?                                   ExitLocation                 = null,
                                 I18NString?                                      ArrivalInstructions          = null,
                                 Boolean?                                         ChargingWhenClosed           = null,
                                 IEnumerable<ParkingRestrictionGroup>?            ParkingRestrictions          = null,
                                 IEnumerable<AdditionalGeoLocation>?              RelatedLocations             = null,
                                 IEnumerable<LocationService>?                    Services                     = null,
                                 I18NString?                                      AssistanceServiceDetails     = null,
                                 IEnumerable<Standard>?                           Standards                    = null,
                                 IEnumerable<NUTSCode>?                           NUTSCodes                    = null,

                                 Timestamped<ChargingPoolGroupAdminStatusType>?   InitialAdminStatus           = null,
                                 Timestamped<ChargingPoolGroupStatusType>?        InitialStatus                = null,
                                 UInt16?                                          MaxAdminStatusScheduleSize   = null,
                                 UInt16?                                          MaxStatusScheduleSize        = null,

                                 String?                                          DataSource                   = null,
                                 DateTimeOffset?                                  Created                      = null,
                                 DateTimeOffset?                                  LastChange                   = null,

                                 CustomDataNew?                                   CustomData                   = null,
                                 UserDefinedDictionary?                           InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingPoolGroupAdminStatusType>(Created ?? Timestamp.Now, ChargingPoolGroupAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingPoolGroupStatusType>     (Created ?? Timestamp.Now, ChargingPoolGroupStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            var dataLicenses = (DataLicenses ?? []).ToArray();

            if (dataLicenses.Any(dataLicense => dataLicense is null))
                throw new ArgumentException("Data licenses must not contain null entries!", nameof(DataLicenses));

            if (dataLicenses.Select(dataLicense => dataLicense.Id).Distinct().Count() != dataLicenses.Length)
                throw new ArgumentException("A data license must not be given twice!", nameof(DataLicenses));

            this.ChargingPoolIds           = POIJSON.OrderedIds(ChargingPoolIds, $"member of the charging pool group '{Id}'");
            this.ChargingTariffId          = ChargingTariffId;
            this.Priority                  = Priority;

            this.Address                   = Address;
            this.GeoLocation               = GeoLocation;
            this.ParkingType               = ParkingType;
            this.Accessibility             = Accessibility;
            this.HotlinePhoneNumber        = HotlinePhoneNumber;
            this.DataLicenses              = [.. dataLicenses.Ordered()];
            this.GridConnectionPointId     = GridConnectionPointId;
            this.Brands                    = new (Brands, brand => brand.Id, "brand", $"the charging pool group '{Id}'");
            this.OpeningTimes              = OpeningTimes;
            this.EnergyMix                 = EnergyMix;
            this.Images                    = POIJSON.Distinct  (Images,              image => image.URL.ToString(), $"image of the charging pool group '{Id}'");
            this.TimeZone                  = TimeZone;
            this.Facilities                = POIJSON.OrderedIds(Facilities,          $"facility of the charging pool group '{Id}'");
            this.PaymentOptions            = POIJSON.OrderedIds(PaymentOptions,      $"payment option of the charging pool group '{Id}'");
            this.AuthenticationModes       = [.. POIJSON.Distinct(AuthenticationModes, authenticationMode => authenticationMode.Type, $"authentication mode of the charging pool group '{Id}'").
                                                     OrderBy(authenticationMode => authenticationMode.Type, StringComparer.Ordinal)];
            this.UIFeatures                = POIJSON.OrderedIds(UIFeatures,          $"user interface feature of the charging pool group '{Id}'");
            this.Features                  = POIJSON.OrderedIds(Features,            $"feature of the charging pool group '{Id}'");
            this.LocationLanguages         = POIJSON.UniqueIds (LocationLanguages,   $"language of the charging pool group '{Id}'");
            this.EntranceAddress           = EntranceAddress;
            this.EntranceLocation          = EntranceLocation;
            this.ExitAddress               = ExitAddress;
            this.ExitLocation              = ExitLocation;
            this.ArrivalInstructions       = ArrivalInstructions ?? ImmutableI18NString.Empty;
            this.ChargingWhenClosed        = ChargingWhenClosed;
            this.ParkingRestrictions       = POIJSON.OrderedIds(ParkingRestrictions, $"parking restriction of the charging pool group '{Id}'");
            this.RelatedLocations          = POIJSON.Distinct  (RelatedLocations,    relatedLocation => relatedLocation.ToJSON().ToString(Newtonsoft.Json.Formatting.None), $"related location of the charging pool group '{Id}'");
            this.Services                  = POIJSON.OrderedIds(Services,            $"service of the charging pool group '{Id}'");
            this.AssistanceServiceDetails  = AssistanceServiceDetails ?? ImmutableI18NString.Empty;
            this.Standards                 = POIJSON.OrderedIds(Standards,           $"standard of the charging pool group '{Id}'");
            this.NUTSCodes                 = POIJSON.OrderedIds(NUTSCodes,           $"NUTS code of the charging pool group '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                            statusEventsLock  = new ();
        private          OnChargingPoolGroupAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingPoolGroupStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingPoolGroupAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingPoolGroupStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                  Timestamp,
                                                                   EventTracking_Id                                EventTrackingId,
                                                                   Timestamped<ChargingPoolGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<ChargingPoolGroupAdminStatusType>   OldStatus,
                                                                   Context?                                        DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                             Timestamp,
                                                              EventTracking_Id                           EventTrackingId,
                                                              Timestamped<ChargingPoolGroupStatusType>   NewStatus,
                                                              Timestamped<ChargingPoolGroupStatusType>   OldStatus,
                                                              Context?                                   DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging pool group, as its charging station operator embeds it.
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

            if (ChargingPoolIds.Length > 0)
                json.Add("chargingPoolIds",  POIJSON.IdsToJSON(ChargingPoolIds));

            if (ChargingTariffId.HasValue)
                json.Add("chargingTariffId",  ChargingTariffId.Value.ToString());

            if (Priority.HasValue)
                json.Add("priority",  Priority.Value);

            // The properties of a charging pool, as a charging pool writes them.
            if (DataLicenses.Length > 0)
                json.Add("dataLicenses",              DataLicenses.ToPOIJSON());

            if (Address is not null)
                json.Add("address",                   Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",               GeoLocation.Value.ToJSON(Embedded: true));

            if (ParkingType.HasValue)
                json.Add("parkingType",               ParkingType.Value.ToString());

            if (Accessibility.HasValue)
                json.Add("accessibility",             Accessibility.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",                   HotlinePhoneNumber.Value.ToString());

            if (GridConnectionPointId.HasValue)
                json.Add("gridConnectionPointId",     GridConnectionPointId.Value.ToString());

            if (Brands.Count > 0)
                json.Add("brands",                    new JArray(Brands.Select(brand => brand.ToJSON())));

            if (OpeningTimes is not null)
                json.Add("openingTimes",              OpeningTimes.ToJSON());

            if (EnergyMix is not null)
                json.Add("energyMix",                 EnergyMix.ToJSON(Embedded: true));

            if (Images.Length > 0)
                json.Add("images",                    new JArray(Images.Select(image => image.ToJSON())));

            if (TimeZone.HasValue)
                json.Add("timeZone",                  TimeZone.Value.ToString());

            if (Facilities.Length > 0)
                json.Add("facilities",                POIJSON.IdsToJSON(Facilities));

            if (PaymentOptions.Length > 0)
                json.Add("paymentOptions",            POIJSON.IdsToJSON(PaymentOptions));

            if (AuthenticationModes.Length > 0)
                json.Add("authenticationModes",       new JArray(AuthenticationModes.Select(authenticationMode => authenticationMode.ToJSON())));

            if (UIFeatures.Length > 0)
                json.Add("uiFeatures",                POIJSON.IdsToJSON(UIFeatures));

            if (Features.Length > 0)
                json.Add("features",                  POIJSON.IdsToJSON(Features));

            if (LocationLanguages.Length > 0)
                json.Add("locationLanguages",         POIJSON.IdsToJSON(LocationLanguages));

            if (EntranceAddress is not null)
                json.Add("entranceAddress",           EntranceAddress.ToJSON(Embedded: true));

            if (EntranceLocation.HasValue)
                json.Add("entranceLocation",          EntranceLocation.Value.ToJSON(Embedded: true));

            if (ExitAddress is not null)
                json.Add("exitAddress",               ExitAddress.ToJSON(Embedded: true));

            if (ExitLocation.HasValue)
                json.Add("exitLocation",              ExitLocation.Value.ToJSON(Embedded: true));

            if (ArrivalInstructions.IsNotNullOrEmpty())
                json.Add("arrivalInstructions",       ArrivalInstructions.ToJSON());

            if (ChargingWhenClosed.HasValue)
                json.Add("chargingWhenClosed",        ChargingWhenClosed.Value);

            if (ParkingRestrictions.Length > 0)
                json.Add("parkingRestrictions",       POIJSON.IdsToJSON(ParkingRestrictions));

            if (RelatedLocations.Length > 0)
                json.Add("relatedLocations",          new JArray(RelatedLocations.Select(relatedLocation => relatedLocation.ToJSON())));

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

        #region (static) TryParse(JSON, out ChargingPoolGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging pool group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingPoolGroup">The parsed charging pool group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                       JSON,
                                       [NotNullWhen(true)]  out ChargingPoolGroup?   ChargingPoolGroup,
                                       [NotNullWhen(false)] out String?              ErrorResponse)
        {

            ChargingPoolGroup = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "charging pool group identification",
                                         ChargingPoolGroup_Id.TryParse,
                                         out ChargingPoolGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, "name",         "charging pool group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, "description",  "charging pool group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "dataSource",   "data source",                    out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, "chargingPoolIds", "member identification", ChargingPool_Id.TryParse, out ImmutableArray<ChargingPool_Id> members, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "chargingTariffId",  "charging tariff identification",  ChargingTariff_Id.TryParse, out ChargingTariff_Id? chargingTariffId, out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseInteger(JSON, "priority", "priority of the charging pool group", out var priority, out ErrorResponse))
                    return false;

                // The properties of a charging pool, as a charging pool reads them.
                if (!POIJSON.TryParseAddress    (JSON,                                                 out var address,      out ErrorResponse) ||
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

                if (!POIJSON.TryParseText(JSON, "gridConnectionPointId", "grid connection point identification", GridConnectionPoint_Id.TryParse, out GridConnectionPoint_Id? gridConnectionPointId, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<Brand>(JSON, "brands", "brandIds", "brands", Brand.TryParse, out var brands, out ErrorResponse))
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

                ChargingPoolGroup = new ChargingPoolGroup(id,
                                                          name,
                                                          description,
                                                          members,
                                                          chargingTariffId,
                                                          priority,
                                                          address,
                                                          geoLocation,
                                                          parkingType,
                                                          accessibility,
                                                          hotline,
                                                          dataLicenses,
                                                          gridConnectionPointId,
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
                                                          DataSource:  dataSource,
                                                          Created:     created,
                                                          LastChange:  lastChange,
                                                          CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingPoolGroup  = null;
                ErrorResponse      = "The given JSON representation of a charging pool group is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging pool groups for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingPoolGroup? ChargingPoolGroup1, ChargingPoolGroup? ChargingPoolGroup2)

            => ReferenceEquals(ChargingPoolGroup1, ChargingPoolGroup2) ||
               (ChargingPoolGroup1 is not null && ChargingPoolGroup1.Equals(ChargingPoolGroup2));

        /// <summary>
        /// Compares two charging pool groups for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingPoolGroup? ChargingPoolGroup1, ChargingPoolGroup? ChargingPoolGroup2)

            => !(ChargingPoolGroup1 == ChargingPoolGroup2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingPoolGroup chargingPoolGroup
                   ? CompareTo(chargingPoolGroup)
                   : throw new ArgumentException("The given object is not a charging pool group!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingPoolGroup? ChargingPoolGroup)

            => ChargingPoolGroup is null
                   ? throw new ArgumentNullException(nameof(ChargingPoolGroup), "The given charging pool group must not be null!")
                   : Id.CompareTo(ChargingPoolGroup.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingPoolGroup chargingPoolGroup &&
               Equals(chargingPoolGroup);

        /// <inheritdoc/>
        public Boolean Equals(ChargingPoolGroup? ChargingPoolGroup)

            => ChargingPoolGroup is not null &&
               Id.Equals(ChargingPoolGroup.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
