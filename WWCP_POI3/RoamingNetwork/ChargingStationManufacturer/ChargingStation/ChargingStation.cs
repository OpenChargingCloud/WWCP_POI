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
    /// The admin status of a charging station changed.
    /// </summary>
    public delegate Task OnChargingStationAdminStatusChangedDelegate(DateTimeOffset                                Timestamp,
                                                                     EventTracking_Id                              EventTrackingId,
                                                                     ChargingStation                               ChargingStation,
                                                                     Timestamped<ChargingStationAdminStatusType>   OldStatus,
                                                                     Timestamped<ChargingStationAdminStatusType>   NewStatus,
                                                                     Context?                                      DataSource);

    /// <summary>
    /// The status of a charging station changed.
    /// </summary>
    public delegate Task OnChargingStationStatusChangedDelegate(DateTimeOffset                           Timestamp,
                                                                EventTracking_Id                         EventTrackingId,
                                                                ChargingStation                          ChargingStation,
                                                                Timestamped<ChargingStationStatusType>   OldStatus,
                                                                Timestamped<ChargingStationStatusType>   NewStatus,
                                                                Context?                                 DataSource);


    /// <summary>
    /// The energy status of a charging station changed.
    /// </summary>
    public delegate Task OnChargingStationEnergyStatusChangedDelegate(DateTimeOffset            Timestamp,
                                                                   EventTracking_Id          EventTrackingId,
                                                                   ChargingStation           ChargingStation,
                                                                   Timestamped<EnergyInfo>   OldEnergyStatus,
                                                                   Timestamped<EnergyInfo>   NewEnergyStatus,
                                                                   Context?                  DataSource);


    /// <summary>
    /// A charging station: one device with its EVSEs, embedded completely in its charging pool.
    /// </summary>
    public sealed class ChargingStation : AImmutableEMobilityEntity<ChargingStation_Id,
                                                                    ChargingStationAdminStatusType,
                                                                    ChargingStationStatusType>,
                                          IEquatable<ChargingStation>,
                                          IComparable<ChargingStation>
    {

        #region Data


        #endregion

        #region Properties

        /// <summary>
        /// The reference printed on the charging station, e.g. "Station 2".
        /// </summary>
        public String?               PhysicalReference     { get; }

        /// <summary>
        /// The address, when it differs from the one of its charging pool.
        /// </summary>
        public Address?              Address               { get; }

        /// <summary>
        /// The geographical location, when it differs from the one of its charging pool.
        /// </summary>
        public GeoCoordinate?        GeoLocation           { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?          HotlinePhoneNumber    { get; }

        /// <summary>
        /// The manufacturer, one of its roaming network.
        /// </summary>
        public ChargingStationManufacturer_Id?  ManufacturerId  { get; }

        /// <summary>
        /// The model, as its manufacturer names it.
        /// </summary>
        public ChargingStationModel_Id?         ModelId         { get; }

        /// <summary>
        /// The EVSEs, in the order of their identifications.
        /// </summary>
        public POICollection<EVSE_Id, EVSE>  EVSEs         { get; }

        /// <summary>
        /// Its own energy meters, in the order of their identifications; not those of its EVSEs.
        /// </summary>
        public ImmutableArray<EnergyMeter>  EnergyMeters  { get; }

        /// <summary>
        /// Its own energy meters and those of its EVSEs.
        /// </summary>
        public IEnumerable<EnergyMeter>     AllEnergyMeters
            => EnergyMeters.Concat(EVSEs.SelectMany(evse => evse.AllEnergyMeters));


        /// <summary>
        /// The brands it is offered under, of the roaming network, in the order of their identifications.
        /// </summary>
        public ImmutableArray<Brand_Id>  BrandIds  { get; }

        /// <summary>
        /// When it is open; always, unless given.
        /// </summary>
        public ImmutableOpeningTimes?  OpeningTimes  { get; }


        /// <summary>
        /// Its images, in their given order.
        /// </summary>
        public ImmutableArray<Image>  Images  { get; }


        /// <summary>
        /// The types of vehicles it charges, e.g. motorcycles, in the order of their names.
        /// </summary>
        public ImmutableArray<VehicleType>             VehicleTypes          { get; }

        /// <summary>
        /// The EU vehicle categories it charges, e.g. M1 for passenger cars, in the order of their names.
        /// </summary>
        public ImmutableArray<EUVehicleCategory>       EUVehicleCategories   { get; }

        /// <summary>
        /// Who may use it, when it differs from its charging pool, e.g. FreePubliclyAccessible.
        /// </summary>
        public AccessibilityType?                      Accessibility         { get; }

        /// <summary>
        /// Its features, in the order of their names.
        /// </summary>
        public ImmutableArray<ChargingStationFeature>  Features              { get; }

        /// <summary>
        /// Where to find information on the calibration of its energy meters.
        /// </summary>
        public URL?                                    CalibrationInfo       { get; }

        /// <summary>
        /// Where to find information on its certification, e.g. its conformity with calibration law.
        /// </summary>
        public URL?                                    CertificationInfo     { get; }

        /// <summary>
        /// The power it can deliver at once, to all its EVSEs together: with load management often
        /// less than the sum of theirs, never more.
        /// </summary>
        public Watt?                                   MaxPower              { get; }

        /// <summary>
        /// The parking spaces of the roaming network it charges at, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingSpace_Id>         ParkingSpaceIds       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station.
        /// </summary>
        public ChargingStation(ChargingStation_Id                              Id,
                               I18NString?                                     Name                         = null,
                               I18NString?                                     Description                  = null,
                               String?                                         PhysicalReference            = null,
                               Address?                                        Address                      = null,
                               GeoCoordinate?                                  GeoLocation                  = null,
                               PhoneNumber?                                    HotlinePhoneNumber           = null,
                               ChargingStationManufacturer_Id?                 ManufacturerId               = null,
                               ChargingStationModel_Id?                        ModelId                      = null,
                               IEnumerable<EVSE>?                              EVSEs                        = null,
                               IEnumerable<EnergyMeter>?                       EnergyMeters                 = null,
                               IEnumerable<Brand_Id>?                          BrandIds                     = null,
                               ImmutableOpeningTimes?                          OpeningTimes                 = null,
                               IEnumerable<Image>?                             Images                       = null,
                               IEnumerable<VehicleType>?                       VehicleTypes                 = null,
                               AccessibilityType?                              Accessibility                = null,
                               IEnumerable<ChargingStationFeature>?            Features                     = null,
                               URL?                                            CalibrationInfo              = null,
                               URL?                                            CertificationInfo            = null,
                               Watt?                                           MaxPower                     = null,
                               IEnumerable<ParkingSpace_Id>?                   ParkingSpaceIds              = null,
                               IEnumerable<EUVehicleCategory>?                 EUVehicleCategories          = null,

                               Timestamped<ChargingStationAdminStatusType>?    InitialAdminStatus           = null,
                               Timestamped<ChargingStationStatusType>?         InitialStatus                = null,
                               UInt16?                                         MaxAdminStatusScheduleSize   = null,
                               UInt16?                                         MaxStatusScheduleSize        = null,

                               String?                                         DataSource                   = null,
                               DateTimeOffset?                                 Created                      = null,
                               DateTimeOffset?                                 LastChange                   = null,

                               CustomDataNew?                                  CustomData                   = null,
                               UserDefinedDictionary?                          InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingStationAdminStatusType>(Created ?? Timestamp.Now, ChargingStationAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingStationStatusType>     (Created ?? Timestamp.Now, ChargingStationStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            if (MaxPower?.Value < 0)
                throw new ArgumentException($"The maximum power of the charging station '{Id}' must not be negative!", nameof(MaxPower));

            var evses = (EVSEs ?? []).ToArray();

            if (evses.Any(evse => evse is null))
                throw new ArgumentException("EVSEs must not contain null entries!", nameof(EVSEs));

            foreach (var evse in evses)
                if (evse.Id.OperatorId != Id.OperatorId)
                    throw new ArgumentException($"The EVSE '{evse.Id}' does not belong to the operator '{Id.OperatorId}' of the charging station '{Id}'!", nameof(EVSEs));

            // It can not deliver more than all its EVSEs together, where all of them tell theirs.
            if (MaxPower.HasValue && evses.Length > 0 && evses.All(evse => evse.MaxPower.HasValue))
            {

                var evsesMaxPower = evses.Sum(evse => evse.MaxPower!.Value.Value);

                if (MaxPower.Value.Value > evsesMaxPower)
                    throw new ArgumentException($"The maximum power of the charging station '{Id}', {MaxPower.Value.Value} W, must not exceed the {evsesMaxPower} W of all its EVSEs together!", nameof(MaxPower));

            }

            this.PhysicalReference   = PhysicalReference;
            this.Address             = Address;
            this.GeoLocation         = GeoLocation;
            this.HotlinePhoneNumber  = HotlinePhoneNumber;
            this.ManufacturerId      = ManufacturerId;
            this.ModelId             = ModelId;
            this.EVSEs               = new (evses, evse => evse.Id, "EVSE", $"the charging station '{Id}'");
            this.EnergyMeters        = EnergyMeter.Ordered(EnergyMeters, $"the charging station '{Id}'");
            this.BrandIds            = POIJSON.OrderedIds(BrandIds, $"brand of the charging station '{Id}'");
            this.OpeningTimes        = OpeningTimes;
            this.Images = POIJSON.Distinct(Images, image => image.URL.ToString(), $"image of the charging station '{Id}'");
            this.VehicleTypes        = POIJSON.OrderedIds(VehicleTypes,    $"vehicle type of the charging station '{Id}'");
            this.EUVehicleCategories = POIJSON.OrderedIds(EUVehicleCategories, $"EU vehicle category of the charging station '{Id}'");
            this.Accessibility       = Accessibility;
            this.Features            = POIJSON.OrderedIds(Features,        $"feature of the charging station '{Id}'");
            this.CalibrationInfo     = CalibrationInfo;
            this.CertificationInfo   = CertificationInfo;
            this.MaxPower            = MaxPower;
            this.ParkingSpaceIds     = POIJSON.OrderedIds(ParkingSpaceIds, $"parking space of the charging station '{Id}'");
            this.energyStatus        = new EnergyStatusRuntime(RaiseEnergyStatusChanged);

        }

        #endregion


        #region TryGetEVSE(EVSEId, out EVSE)

        /// <summary>
        /// Try to return the EVSE with the given identification.
        /// </summary>
        public Boolean TryGetEVSE(EVSE_Id                         EVSEId,
                                  [NotNullWhen(true)] out EVSE?  EVSE)

            => EVSEs.TryGet(EVSEId, out EVSE);

        #endregion


        #region Status events

        private readonly Lock                                          statusEventsLock  = new ();
        private          OnChargingStationAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingStationStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingStationAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingStationStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                               Timestamp,
                                                                   EventTracking_Id                             EventTrackingId,
                                                                   Timestamped<ChargingStationAdminStatusType>  NewStatus,
                                                                   Timestamped<ChargingStationAdminStatusType>  OldStatus,
                                                                   Context?                                     DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                          Timestamp,
                                                              EventTracking_Id                        EventTrackingId,
                                                              Timestamped<ChargingStationStatusType>  NewStatus,
                                                              Timestamped<ChargingStationStatusType>  OldStatus,
                                                              Context?                                DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region Energy status

        private readonly EnergyStatusRuntime                energyStatus;
        private          OnChargingStationEnergyStatusChangedDelegate?  onEnergyStatusChanged;

        /// <summary>
        /// The current energy status: the power used and available, and the energy stored;
        /// runtime data like the admin status and the status, unless none was reported yet.
        /// </summary>
        public Timestamped<EnergyInfo>?  EnergyStatus
            => energyStatus.Current;

        /// <summary>
        /// The next planned energy status.
        /// </summary>
        internal Timestamped<EnergyInfo>?  NextEnergyStatus
            => energyStatus.Next;

        /// <summary>
        /// Set the energy status, since the given timestamp or now; a later timestamp plans it.
        /// </summary>
        /// <param name="EnergyStatus">The power used and available, and the energy stored; without the current voltage
        /// and the remaining charging time, which only an EVSE has.</param>
        /// <param name="Timestamp">Since when; now, unless given.</param>
        /// <param name="DataSource">An optional source.</param>
        public void SetEnergyStatus(EnergyInfo       EnergyStatus,
                                    DateTimeOffset?  Timestamp    = null,
                                    Context?         DataSource   = null)

            => energyStatus.Set(WithoutEVSEValues(EnergyStatus), Timestamp ?? org.GraphDefined.Vanaheimr.Illias.Timestamp.Now, DataSource);

        /// <summary>
        /// Take the given energy status as it is, also when it repeats the one before.
        /// </summary>
        internal void TakeEnergyStatus(EnergyInfo      EnergyStatus,
                                       DateTimeOffset  Timestamp,
                                       Context?        DataSource)

            => energyStatus.Take(WithoutEVSEValues(EnergyStatus), Timestamp, DataSource);

        /// <summary>
        /// The given energy status, unless it has values only an EVSE has.
        /// </summary>
        private EnergyInfo WithoutEVSEValues(EnergyInfo EnergyStatus)

            => EnergyStatus.HasEVSEValues
                   ? throw new ArgumentException($"The energy status of the charging station '{Id}' must not have a current voltage or a remaining charging time: only an EVSE has them!",
                                                 nameof(EnergyStatus))
                   : EnergyStatus;

        /// <summary>
        /// An event fired whenever the energy status changed.
        /// </summary>
        public event OnChargingStationEnergyStatusChangedDelegate? OnEnergyStatusChanged
        {
            add    { lock (statusEventsLock) { onEnergyStatusChanged += value; energyStatus.Listen(onEnergyStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onEnergyStatusChanged -= value; energyStatus.Listen(onEnergyStatusChanged is not null); } }
        }

        private async Task RaiseEnergyStatusChanged(DateTimeOffset           Timestamp,
                                                    EventTracking_Id         EventTrackingId,
                                                    Timestamped<EnergyInfo>  NewEnergyStatus,
                                                    Timestamped<EnergyInfo>  OldEnergyStatus,
                                                    Context?                 DataSource)
        {
            var onEnergyStatusChanged = this.onEnergyStatusChanged;
            if (onEnergyStatusChanged is not null)
                await onEnergyStatusChanged(Timestamp, EventTrackingId, this, OldEnergyStatus, NewEnergyStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override void ShareFurtherRuntimeOf(AImmutableEMobilityEntity<ChargingStation_Id, ChargingStationAdminStatusType, ChargingStationStatusType> Previous)
        {
            if (Previous is ChargingStation previousChargingStation)
                energyStatus.ShareOf(previousChargingStation.energyStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging station, as its charging pool embeds it: with its EVSEs.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",               Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",        Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",         DataSource);

            if (PhysicalReference is not null)
                json.Add("physicalReference",  PhysicalReference);

            if (Address is not null)
                json.Add("address",            Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",        GeoLocation.Value.ToJSON(Embedded: true));

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",            HotlinePhoneNumber.Value.ToString());

            if (ManufacturerId.HasValue)
                json.Add("manufacturerId",     ManufacturerId.Value.ToString());

            if (ModelId.HasValue)
                json.Add("modelId",            ModelId.Value.ToString());

            if (EnergyMeters.Length > 0)
                json.Add("energyMeters",       new JArray(EnergyMeters.Select(energyMeter => energyMeter.ToJSON())));

            if (EVSEs.Count > 0)
                json.Add("EVSEs",              new JArray(EVSEs.Select(evse => evse.ToJSON())));

            if (BrandIds.Length > 0)
                json.Add("brandIds",      POIJSON.IdsToJSON(BrandIds));

            if (OpeningTimes is not null)
                json.Add("openingTimes",  OpeningTimes.ToJSON());

            if (Images.Length > 0)
                json.Add("images",  new JArray(Images.Select(image => image.ToJSON())));

            if (VehicleTypes.Length > 0)
                json.Add("vehicleTypes",       POIJSON.IdsToJSON(VehicleTypes));

            if (EUVehicleCategories.Length > 0)
                json.Add("euVehicleCategories", POIJSON.IdsToJSON(EUVehicleCategories));

            if (Accessibility.HasValue)
                json.Add("accessibility",      Accessibility.Value.ToString());

            if (Features.Length > 0)
                json.Add("features",           POIJSON.IdsToJSON(Features));

            if (CalibrationInfo.HasValue)
                json.Add("calibrationInfo",    CalibrationInfo.Value.ToString());

            if (CertificationInfo.HasValue)
                json.Add("certificationInfo",  CertificationInfo.Value.ToString());

            if (MaxPower.HasValue)
                json.Add("maxPower",           MaxPower.Value.Value);

            if (ParkingSpaceIds.Length > 0)
                json.Add("parkingSpaceIds",    POIJSON.IdsToJSON(ParkingSpaceIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingStation, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station, with its EVSEs.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStation">The parsed charging station.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out ChargingStation?  ChargingStation,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            ChargingStation = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "charging station identification",
                                         ChargingStation_Id.TryParse,
                                         out ChargingStation_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N       (JSON, "name",               "charging station name",         out var name,               out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "description",        "charging station description",  out var description,        out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "dataSource",         "data source",                   out var dataSource,         out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "physicalReference",  "physical reference",            out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON,                                                        out var address,            out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON,                                                        out var geoLocation,        out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "hotline",            "hotline phone number",          PhoneNumber.TryParse,       out PhoneNumber? hotline,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "manufacturerId",     "manufacturer identification",   ChargingStationManufacturer_Id.TryParse, out ChargingStationManufacturer_Id? manufacturerId, out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "modelId",            "model identification",          ChargingStationModel_Id.       TryParse, out ChargingStationModel_Id?        modelId,        out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<EVSE>(JSON,
                                                    "EVSEs",
                                                    "EVSEIds",
                                                    "EVSEs",
                                                    EVSE.TryParse,
                                                    out var evses,
                                                    out ErrorResponse))
                {
                    return false;
                }

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

                if (!POIJSON.TryParseObjects<Image>(JSON, "images", "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseIds (JSON, "vehicleTypes",       "vehicle type",               VehicleType.           TryParse, out ImmutableArray<VehicleType>             vehicleTypes,       out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, "euVehicleCategories", "EU vehicle category",       EUVehicleCategory.     TryParse, out ImmutableArray<EUVehicleCategory>       euVehicleCategories, out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, "accessibility",      "accessibility",              AccessibilityType.     TryParse, out AccessibilityType?                      accessibility,      out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, "features",           "charging station feature",   ChargingStationFeature.TryParse, out ImmutableArray<ChargingStationFeature>  features,           out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, "calibrationInfo",    "calibration information",    URL.                   TryParse, out URL?                                    calibrationInfo,    out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, "certificationInfo",  "certification information",  URL.                   TryParse, out URL?                                    certificationInfo,  out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, "parkingSpaceIds",    "parking space identification", ParkingSpace_Id.     TryParse, out ImmutableArray<ParkingSpace_Id>         parkingSpaceIds,    out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional("maxPower", "maximum power", out Watt? maxPower, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingStation = new ChargingStation(id,
                                                      name,
                                                      description,
                                                      physicalReference,
                                                      address,
                                                      geoLocation,
                                                      hotline,
                                                      manufacturerId,
                                                      modelId,
                                                      evses,
                                                      energyMeters,
                                                      brandIds,
                                                      openingTimes,
                                                      images,
                                                      vehicleTypes,
                                                      accessibility,
                                                      features,
                                                      calibrationInfo,
                                                      certificationInfo,
                                                      maxPower,
                                                      parkingSpaceIds,
                                                      euVehicleCategories,
                                                      DataSource:  dataSource,
                                                      Created:     created,
                                                      LastChange:  lastChange,
                                                      CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStation  = null;
                ErrorResponse    = "The given JSON representation of a charging station is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging stations for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingStation? ChargingStation1, ChargingStation? ChargingStation2)

            => ReferenceEquals(ChargingStation1, ChargingStation2) ||
               (ChargingStation1 is not null && ChargingStation1.Equals(ChargingStation2));

        /// <summary>
        /// Compares two charging stations for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingStation? ChargingStation1, ChargingStation? ChargingStation2)

            => !(ChargingStation1 == ChargingStation2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingStation chargingStation
                   ? CompareTo(chargingStation)
                   : throw new ArgumentException("The given object is not a charging station!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingStation? ChargingStation)

            => ChargingStation is null
                   ? throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!")
                   : Id.CompareTo(ChargingStation.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStation chargingStation &&
               Equals(chargingStation);

        /// <inheritdoc/>
        public Boolean Equals(ChargingStation? ChargingStation)

            => ChargingStation is not null &&
               Id.Equals(ChargingStation.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
