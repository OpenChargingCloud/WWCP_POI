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
    /// The admin status of a charging station group changed.
    /// </summary>
    public delegate Task OnChargingStationGroupAdminStatusChangedDelegate(DateTimeOffset                                     Timestamp,
                                                                          EventTracking_Id                                   EventTrackingId,
                                                                          ChargingStationGroup                               ChargingStationGroup,
                                                                          Timestamped<ChargingStationGroupAdminStatusType>   OldStatus,
                                                                          Timestamped<ChargingStationGroupAdminStatusType>   NewStatus,
                                                                          Context?                                           DataSource);

    /// <summary>
    /// The status of a charging station group changed.
    /// </summary>
    public delegate Task OnChargingStationGroupStatusChangedDelegate(DateTimeOffset                                Timestamp,
                                                                     EventTracking_Id                              EventTrackingId,
                                                                     ChargingStationGroup                          ChargingStationGroup,
                                                                     Timestamped<ChargingStationGroupStatusType>   OldStatus,
                                                                     Timestamped<ChargingStationGroupStatusType>   NewStatus,
                                                                     Context?                                      DataSource);


    /// <summary>
    /// A group of charging stations of one charging station operator, embedded completely in it; groups may overlap.
    /// It may give its members the properties of a charging station, which they take at runtime, as the effective
    /// charging stations of their operator: a value of their own wins, arrays are united, and of the groups of a
    /// member the one with the higher priority wins.
    /// </summary>
    public sealed class ChargingStationGroup : AImmutableEMobilityEntity<ChargingStationGroup_Id,
                                                                         ChargingStationGroupAdminStatusType,
                                                                         ChargingStationGroupStatusType>,
                                               IEquatable<ChargingStationGroup>,
                                               IComparable<ChargingStationGroup>
    {

        #region Properties

        /// <summary>
        /// Its charging stations, of the same charging station operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingStation_Id>  ChargingStationIds  { get; }

        /// <summary>
        /// The charging tariff of its operator for all its members, unless their own apply.
        /// </summary>
        public ChargingTariff_Id?                  ChargingTariffId    { get; }

        /// <summary>
        /// Its priority over the other groups of a member, where they give it different values: the higher wins;
        /// 0, unless given. Groups of the same member with the same priority must not give it different values.
        /// </summary>
        public Int32?  Priority  { get; }


        /// <summary>
        /// The reference printed on its members, e.g. "Station 2", unless they have their own.
        /// </summary>
        public String?                                 PhysicalReference    { get; }

        /// <summary>
        /// The address of its members, unless they have their own.
        /// </summary>
        public Address?                                Address              { get; }

        /// <summary>
        /// The geographical location of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                          GeoLocation          { get; }

        /// <summary>
        /// The hotline phone number of its members, unless they have their own.
        /// </summary>
        public PhoneNumber?                            HotlinePhoneNumber   { get; }

        /// <summary>
        /// The manufacturer of its members, one of its roaming network, unless they have their own.
        /// </summary>
        public ChargingStationManufacturer_Id?         ManufacturerId       { get; }

        /// <summary>
        /// The model of its members, as their manufacturer names it, unless they have their own.
        /// </summary>
        public ChargingStationModel_Id?                ModelId              { get; }

        /// <summary>
        /// Brands its members also appear under, besides their own, in the order of their identifications.
        /// </summary>
        public POICollection<Brand_Id, Brand>          Brands               { get; }

        /// <summary>
        /// When its members are open, unless they have their own.
        /// </summary>
        public ImmutableOpeningTimes?                  OpeningTimes         { get; }

        /// <summary>
        /// Images of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<Image>                   Images               { get; }

        /// <summary>
        /// Types of vehicles its members charge, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<VehicleType>             VehicleTypes         { get; }

        /// <summary>
        /// EU vehicle categories its members charge, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<EUVehicleCategory>       EUVehicleCategories  { get; }

        /// <summary>
        /// Who may use its members, unless they tell it themselves.
        /// </summary>
        public AccessibilityType?                      Accessibility        { get; }

        /// <summary>
        /// Features of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<ChargingStationFeature>  Features             { get; }

        /// <summary>
        /// Where to find information on the calibration of the energy meters of its members, unless they have their own.
        /// </summary>
        public URL?                                    CalibrationInfo      { get; }

        /// <summary>
        /// Where to find information on the certification of its members, unless they have their own.
        /// </summary>
        public URL?                                    CertificationInfo    { get; }

        /// <summary>
        /// The power each of its members can deliver at once, unless they have their own:
        /// never more than all the EVSEs of a member together.
        /// </summary>
        public Watt?                                   MaxPower             { get; }

        /// <summary>
        /// Parking spaces of the roaming network its members charge at, besides their own, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingSpace_Id>         ParkingSpaceIds      { get; }

        /// <summary>
        /// Whether it gives its members any property of a charging station.
        /// </summary>
        public Boolean HasChargingStationProperties

            => PhysicalReference is not null || Address is not null   || GeoLocation.HasValue      || HotlinePhoneNumber.HasValue ||
               ManufacturerId.HasValue       || ModelId.HasValue      || Brands.Count > 0          || OpeningTimes is not null    ||
               Images.Length > 0             || VehicleTypes.Length > 0 || EUVehicleCategories.Length > 0 || Accessibility.HasValue ||
               Features.Length > 0           || CalibrationInfo.HasValue || CertificationInfo.HasValue || MaxPower.HasValue         ||
               ParkingSpaceIds.Length > 0;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station group.
        /// </summary>
        public ChargingStationGroup(ChargingStationGroup_Id                             Id,
                                    I18NString?                                         Name                         = null,
                                    I18NString?                                         Description                  = null,
                                    IEnumerable<ChargingStation_Id>?                    ChargingStationIds           = null,
                                    ChargingTariff_Id?                                  ChargingTariffId             = null,
                                    Int32?                                              Priority                     = null,

                                    String?                                             PhysicalReference            = null,
                                    Address?                                            Address                      = null,
                                    GeoCoordinate?                                      GeoLocation                  = null,
                                    PhoneNumber?                                        HotlinePhoneNumber           = null,
                                    ChargingStationManufacturer_Id?                     ManufacturerId               = null,
                                    ChargingStationModel_Id?                            ModelId                      = null,
                                    IEnumerable<Brand>?                                 Brands                       = null,
                                    ImmutableOpeningTimes?                              OpeningTimes                 = null,
                                    IEnumerable<Image>?                                 Images                       = null,
                                    IEnumerable<VehicleType>?                           VehicleTypes                 = null,
                                    IEnumerable<EUVehicleCategory>?                     EUVehicleCategories          = null,
                                    AccessibilityType?                                  Accessibility                = null,
                                    IEnumerable<ChargingStationFeature>?                Features                     = null,
                                    URL?                                                CalibrationInfo              = null,
                                    URL?                                                CertificationInfo            = null,
                                    Watt?                                               MaxPower                     = null,
                                    IEnumerable<ParkingSpace_Id>?                       ParkingSpaceIds              = null,

                                    Timestamped<ChargingStationGroupAdminStatusType>?   InitialAdminStatus           = null,
                                    Timestamped<ChargingStationGroupStatusType>?        InitialStatus                = null,
                                    UInt16?                                             MaxAdminStatusScheduleSize   = null,
                                    UInt16?                                             MaxStatusScheduleSize        = null,

                                    String?                                             DataSource                   = null,
                                    DateTimeOffset?                                     Created                      = null,
                                    DateTimeOffset?                                     LastChange                   = null,

                                    CustomDataNew?                                      CustomData                   = null,
                                    UserDefinedDictionary?                              InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingStationGroupAdminStatusType>(Created ?? Timestamp.Now, ChargingStationGroupAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingStationGroupStatusType>     (Created ?? Timestamp.Now, ChargingStationGroupStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            if (MaxPower?.Value < 0)
                throw new ArgumentException($"The maximum power of the charging station group '{Id}' must not be negative!", nameof(MaxPower));

            this.ChargingStationIds   = POIJSON.OrderedIds(ChargingStationIds, $"member of the charging station group '{Id}'");
            this.ChargingTariffId     = ChargingTariffId;
            this.Priority             = Priority;

            this.PhysicalReference    = PhysicalReference;
            this.Address              = Address;
            this.GeoLocation          = GeoLocation;
            this.HotlinePhoneNumber   = HotlinePhoneNumber;
            this.ManufacturerId       = ManufacturerId;
            this.ModelId              = ModelId;
            this.Brands               = new (Brands, brand => brand.Id, "brand", $"the charging station group '{Id}'");
            this.OpeningTimes         = OpeningTimes;
            this.Images               = POIJSON.Distinct  (Images, image => image.URL.ToString(), $"image of the charging station group '{Id}'");
            this.VehicleTypes         = POIJSON.OrderedIds(VehicleTypes,        $"vehicle type of the charging station group '{Id}'");
            this.EUVehicleCategories  = POIJSON.OrderedIds(EUVehicleCategories, $"EU vehicle category of the charging station group '{Id}'");
            this.Accessibility        = Accessibility;
            this.Features             = POIJSON.OrderedIds(Features,            $"feature of the charging station group '{Id}'");
            this.CalibrationInfo      = CalibrationInfo;
            this.CertificationInfo    = CertificationInfo;
            this.MaxPower             = MaxPower;
            this.ParkingSpaceIds      = POIJSON.OrderedIds(ParkingSpaceIds,     $"parking space of the charging station group '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                               statusEventsLock  = new ();
        private          OnChargingStationGroupAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingStationGroupStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingStationGroupAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingStationGroupStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                     Timestamp,
                                                                   EventTracking_Id                                   EventTrackingId,
                                                                   Timestamped<ChargingStationGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<ChargingStationGroupAdminStatusType>   OldStatus,
                                                                   Context?                                           DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                                Timestamp,
                                                              EventTracking_Id                              EventTrackingId,
                                                              Timestamped<ChargingStationGroupStatusType>   NewStatus,
                                                              Timestamped<ChargingStationGroupStatusType>   OldStatus,
                                                              Context?                                      DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging station group, as its charging station operator embeds it.
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

            if (ChargingStationIds.Length > 0)
                json.Add("chargingStationIds",  POIJSON.IdsToJSON(ChargingStationIds));

            if (ChargingTariffId.HasValue)
                json.Add("chargingTariffId",  ChargingTariffId.Value.ToString());

            if (Priority.HasValue)
                json.Add("priority",  Priority.Value);

            // The properties of a charging station, as a charging station writes them.
            if (PhysicalReference is not null)
                json.Add("physicalReference",    PhysicalReference);

            if (Address is not null)
                json.Add("address",              Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",          GeoLocation.Value.ToJSON(Embedded: true));

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",              HotlinePhoneNumber.Value.ToString());

            if (ManufacturerId.HasValue)
                json.Add("manufacturerId",       ManufacturerId.Value.ToString());

            if (ModelId.HasValue)
                json.Add("modelId",              ModelId.Value.ToString());

            if (Brands.Count > 0)
                json.Add("brands",               new JArray(Brands.Select(brand => brand.ToJSON())));

            if (OpeningTimes is not null)
                json.Add("openingTimes",         OpeningTimes.ToJSON());

            if (Images.Length > 0)
                json.Add("images",               new JArray(Images.Select(image => image.ToJSON())));

            if (VehicleTypes.Length > 0)
                json.Add("vehicleTypes",         POIJSON.IdsToJSON(VehicleTypes));

            if (EUVehicleCategories.Length > 0)
                json.Add("euVehicleCategories",  POIJSON.IdsToJSON(EUVehicleCategories));

            if (Accessibility.HasValue)
                json.Add("accessibility",        Accessibility.Value.ToString());

            if (Features.Length > 0)
                json.Add("features",             POIJSON.IdsToJSON(Features));

            if (CalibrationInfo.HasValue)
                json.Add("calibrationInfo",      CalibrationInfo.Value.ToString());

            if (CertificationInfo.HasValue)
                json.Add("certificationInfo",    CertificationInfo.Value.ToString());

            if (MaxPower.HasValue)
                json.Add("maxPower",             MaxPower.Value.Value);

            if (ParkingSpaceIds.Length > 0)
                json.Add("parkingSpaceIds",      POIJSON.IdsToJSON(ParkingSpaceIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingStationGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStationGroup">The parsed charging station group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                          JSON,
                                       [NotNullWhen(true)]  out ChargingStationGroup?   ChargingStationGroup,
                                       [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            ChargingStationGroup = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "charging station group identification",
                                         ChargingStationGroup_Id.TryParse,
                                         out ChargingStationGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, "name",         "charging station group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, "description",  "charging station group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "dataSource",   "data source",                       out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, "chargingStationIds", "member identification", ChargingStation_Id.TryParse, out ImmutableArray<ChargingStation_Id> members, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "chargingTariffId",  "charging tariff identification",  ChargingTariff_Id.TryParse, out ChargingTariff_Id? chargingTariffId, out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseInteger(JSON, "priority", "priority of the charging station group", out var priority, out ErrorResponse))
                    return false;

                // The properties of a charging station, as a charging station reads them.
                if (!POIJSON.TryParseString     (JSON, "physicalReference",  "physical reference",            out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON,                                                        out var address,            out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON,                                                        out var geoLocation,        out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "hotline",            "hotline phone number",          PhoneNumber.TryParse,       out PhoneNumber? hotline,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "manufacturerId",     "manufacturer identification",   ChargingStationManufacturer_Id.TryParse, out ChargingStationManufacturer_Id? manufacturerId, out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "modelId",            "model identification",          ChargingStationModel_Id.       TryParse, out ChargingStationModel_Id?        modelId,        out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<Brand>(JSON, "brands", "brandIds", "brands", Brand.TryParse, out var brands, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObject<ImmutableOpeningTimes>(JSON, "openingTimes", "opening times", ImmutableOpeningTimes.TryParse, out var openingTimes, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, "images", "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseIds (JSON, "vehicleTypes",        "vehicle type",                  VehicleType.           TryParse, out ImmutableArray<VehicleType>             vehicleTypes,        out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, "euVehicleCategories", "EU vehicle category",           EUVehicleCategory.     TryParse, out ImmutableArray<EUVehicleCategory>       euVehicleCategories, out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, "accessibility",       "accessibility",                 AccessibilityType.     TryParse, out AccessibilityType?                      accessibility,       out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, "features",            "charging station feature",      ChargingStationFeature.TryParse, out ImmutableArray<ChargingStationFeature>  features,            out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, "calibrationInfo",     "calibration information",       URL.                   TryParse, out URL?                                    calibrationInfo,     out ErrorResponse) ||
                    !POIJSON.TryParseText(JSON, "certificationInfo",   "certification information",     URL.                   TryParse, out URL?                                    certificationInfo,   out ErrorResponse) ||
                    !POIJSON.TryParseIds (JSON, "parkingSpaceIds",     "parking space identification",  ParkingSpace_Id.       TryParse, out ImmutableArray<ParkingSpace_Id>         parkingSpaceIds,     out ErrorResponse))
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

                ChargingStationGroup = new ChargingStationGroup(id,
                                                                name,
                                                                description,
                                                                members,
                                                                chargingTariffId,
                                                                priority,
                                                                physicalReference,
                                                                address,
                                                                geoLocation,
                                                                hotline,
                                                                manufacturerId,
                                                                modelId,
                                                                brands,
                                                                openingTimes,
                                                                images,
                                                                vehicleTypes,
                                                                euVehicleCategories,
                                                                accessibility,
                                                                features,
                                                                calibrationInfo,
                                                                certificationInfo,
                                                                maxPower,
                                                                parkingSpaceIds,
                                                                DataSource:  dataSource,
                                                                Created:     created,
                                                                LastChange:  lastChange,
                                                                CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationGroup  = null;
                ErrorResponse         = "The given JSON representation of a charging station group is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging station groups for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingStationGroup? ChargingStationGroup1, ChargingStationGroup? ChargingStationGroup2)

            => ReferenceEquals(ChargingStationGroup1, ChargingStationGroup2) ||
               (ChargingStationGroup1 is not null && ChargingStationGroup1.Equals(ChargingStationGroup2));

        /// <summary>
        /// Compares two charging station groups for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingStationGroup? ChargingStationGroup1, ChargingStationGroup? ChargingStationGroup2)

            => !(ChargingStationGroup1 == ChargingStationGroup2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingStationGroup chargingStationGroup
                   ? CompareTo(chargingStationGroup)
                   : throw new ArgumentException("The given object is not a charging station group!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingStationGroup? ChargingStationGroup)

            => ChargingStationGroup is null
                   ? throw new ArgumentNullException(nameof(ChargingStationGroup), "The given charging station group must not be null!")
                   : Id.CompareTo(ChargingStationGroup.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationGroup chargingStationGroup &&
               Equals(chargingStationGroup);

        /// <inheritdoc/>
        public Boolean Equals(ChargingStationGroup? ChargingStationGroup)

            => ChargingStationGroup is not null &&
               Id.Equals(ChargingStationGroup.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
