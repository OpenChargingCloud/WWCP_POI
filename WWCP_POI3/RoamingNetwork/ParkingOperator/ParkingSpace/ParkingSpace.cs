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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of a parking space changed.
    /// </summary>
    public delegate Task OnParkingSpaceAdminStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  ParkingSpace                               ParkingSpace,
                                                                  Timestamped<ParkingSpaceAdminStatusType>   OldStatus,
                                                                  Timestamped<ParkingSpaceAdminStatusType>   NewStatus,
                                                                  Context?                                   DataSource);

    /// <summary>
    /// The status of a parking space changed.
    /// </summary>
    public delegate Task OnParkingSpaceStatusChangedDelegate(DateTimeOffset                        Timestamp,
                                                             EventTracking_Id                      EventTrackingId,
                                                             ParkingSpace                          ParkingSpace,
                                                             Timestamped<ParkingSpaceStatusType>   OldStatus,
                                                             Timestamped<ParkingSpaceStatusType>   NewStatus,
                                                             Context?                              DataSource);


    /// <summary>
    /// A parking space, embedded completely in its parking operator.
    /// It may name a parking garage and parking sensors of the same parking operator.
    /// Its identification is unique within the roaming network.
    /// </summary>
    public sealed class ParkingSpace : AImmutableEMobilityEntity<ParkingSpace_Id,
                                                                 ParkingSpaceAdminStatusType,
                                                                 ParkingSpaceStatusType>,
                                       IEquatable<ParkingSpace>,
                                       IComparable<ParkingSpace>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/parkingSpace/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation                = "geoLocation";

            /// <summary>
            /// The property of its parking garage.
            /// </summary>
            public const String  ParkingGarageId            = "parkingGarageId";

            /// <summary>
            /// The property of the parking sensors watching it.
            /// </summary>
            public const String  ParkingSensorIds           = "parkingSensorIds";

            /// <summary>
            /// The property of its charging stations.
            /// </summary>
            public const String  ChargingStationIds         = "chargingStationIds";

            /// <summary>
            /// The property of its parking products.
            /// </summary>
            public const String  ParkingProductIds          = "parkingProductIds";

            /// <summary>
            /// The property of its parking restrictions.
            /// </summary>
            public const String  ParkingRestrictions        = "parkingRestrictions";

            /// <summary>
            /// The property of its EU vehicle categories.
            /// </summary>
            public const String  EUVehicleCategories        = "euVehicleCategories";

            /// <summary>
            /// The property of its physical reference.
            /// </summary>
            public const String  PhysicalReference          = "physicalReference";

            /// <summary>
            /// The property of its vehicle types.
            /// </summary>
            public const String  VehicleTypes               = "vehicleTypes";

            /// <summary>
            /// The property of its maximum vehicle weight, in kg.
            /// </summary>
            public const String  MaxVehicleWeight           = "maxVehicleWeight";

            /// <summary>
            /// The property of its maximum vehicle height, in m.
            /// </summary>
            public const String  MaxVehicleHeight           = "maxVehicleHeight";

            /// <summary>
            /// The property of its maximum vehicle length, in m.
            /// </summary>
            public const String  MaxVehicleLength           = "maxVehicleLength";

            /// <summary>
            /// The property of its maximum vehicle width, in m.
            /// </summary>
            public const String  MaxVehicleWidth            = "maxVehicleWidth";

            /// <summary>
            /// The property of its length, in m.
            /// </summary>
            public const String  Length                     = "length";

            /// <summary>
            /// The property of its width, in m.
            /// </summary>
            public const String  Width                      = "width";

            /// <summary>
            /// The property of the position of its charging station.
            /// </summary>
            public const String  EVSEPosition               = "evsePosition";

            /// <summary>
            /// The property of its direction.
            /// </summary>
            public const String  Direction                  = "direction";

            /// <summary>
            /// The property of its surface.
            /// </summary>
            public const String  Surface                    = "surface";

            /// <summary>
            /// The property of its slope.
            /// </summary>
            public const String  Slope                      = "slope";

            /// <summary>
            /// The property of its time limit, in seconds.
            /// </summary>
            public const String  TimeLimit                  = "timeLimit";

            /// <summary>
            /// The property of whether it is restricted to its vehicle types.
            /// </summary>
            public const String  RestrictedToVehicleTypes   = "restrictedToVehicleTypes";

            /// <summary>
            /// The property of whether it is a drive-through.
            /// </summary>
            public const String  DriveThrough               = "driveThrough";

            /// <summary>
            /// The property of whether it requires a reservation.
            /// </summary>
            public const String  ReservationRequired        = "reservationRequired";

            /// <summary>
            /// The property of whether it is roofed.
            /// </summary>
            public const String  Roofed                     = "roofed";

            /// <summary>
            /// The property of whether it is illuminated.
            /// </summary>
            public const String  Illuminated                = "illuminated";

            /// <summary>
            /// The property of whether it has a refrigeration outlet.
            /// </summary>
            public const String  RefrigerationOutlet        = "refrigerationOutlet";

            /// <summary>
            /// The property of whether dangerous goods are allowed.
            /// </summary>
            public const String  DangerousGoodsAllowed      = "dangerousGoodsAllowed";

            /// <summary>
            /// The property of whether it is in a protected area.
            /// </summary>
            public const String  ProtectedArea              = "protectedArea";

            /// <summary>
            /// The property of its APDS reference.
            /// </summary>
            public const String  APDSReference              = "apdsReference";

            /// <summary>
            /// The property of its standards.
            /// </summary>
            public const String  Standards                  = "standards";

            /// <summary>
            /// The property of its images.
            /// </summary>
            public const String  Images                     = "images";

            /// <summary>
            /// The property of its templates.
            /// </summary>
            public const String  TemplateIds                = "templateIds";

        }

        #endregion


        /// <summary>
        /// The geographical location.
        /// </summary>
        public GeoCoordinate?                      GeoLocation           { get; }

        /// <summary>
        /// The parking garage it is in, one of the same parking operator; it does not own the space.
        /// </summary>
        public ParkingGarage_Id?                   ParkingGarageId       { get; }

        /// <summary>
        /// The parking sensors watching it, of the same parking operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingSensor_Id>    ParkingSensorIds      { get; }

        /// <summary>
        /// The charging stations reachable from it, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingStation_Id>  ChargingStationIds    { get; }


        /// <summary>
        /// The parking products of its parking operator offered here, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingProduct_Id>  ParkingProductIds  { get; }

        /// <summary>
        /// Who may park there, e.g. DISABLED for persons with disabilities, in the order of their names.
        /// </summary>
        public ImmutableArray<ParkingRestrictionGroup>  ParkingRestrictions  { get; }

        /// <summary>
        /// The EU vehicle categories it is for, e.g. M1 for passenger cars, in the order of their names.
        /// </summary>
        public ImmutableArray<EUVehicleCategory>        EUVehicleCategories  { get; }

        /// <summary>
        /// Its reference visible on site, e.g. a number painted on the ground.
        /// </summary>
        public String?                                  PhysicalReference         { get; }

        /// <summary>
        /// The types of the vehicles it is meant for, e.g. VAN, in the order of their names.
        /// </summary>
        public ImmutableArray<VehicleType>              VehicleTypes              { get; }

        /// <summary>
        /// The maximum weight of a vehicle parking there.
        /// </summary>
        public Kilogram?                                MaxVehicleWeight          { get; }

        /// <summary>
        /// The maximum height of a vehicle parking there.
        /// </summary>
        public Meter?                                   MaxVehicleHeight          { get; }

        /// <summary>
        /// The maximum length of a vehicle parking there.
        /// </summary>
        public Meter?                                   MaxVehicleLength          { get; }

        /// <summary>
        /// The maximum width of a vehicle parking there.
        /// </summary>
        public Meter?                                   MaxVehicleWidth           { get; }

        /// <summary>
        /// Its length.
        /// </summary>
        public Meter?                                   Length                    { get; }

        /// <summary>
        /// Its width.
        /// </summary>
        public Meter?                                   Width                     { get; }

        /// <summary>
        /// Where the charging station is, seen from a vehicle parked there.
        /// </summary>
        public EVSEPosition?                           EVSEPosition              { get; }

        /// <summary>
        /// How it lies to the road or aisle it is entered from.
        /// </summary>
        public ParkingDirection?                       Direction                 { get; }

        /// <summary>
        /// Its surface.
        /// </summary>
        public ParkingSurface?                         Surface                   { get; }

        /// <summary>
        /// Its slope.
        /// </summary>
        public ParkingSlope?                           Slope                     { get; }

        /// <summary>
        /// How long a vehicle may park there at most.
        /// </summary>
        public TimeSpan?                                TimeLimit                 { get; }

        /// <summary>
        /// Whether only its vehicle types may park there, if known.
        /// </summary>
        public Boolean?                                 RestrictedToVehicleTypes  { get; }

        /// <summary>
        /// Whether a vehicle can drive through it, entering at the front and leaving at the back, if known.
        /// </summary>
        public Boolean?                                 DriveThrough              { get; }

        /// <summary>
        /// Whether it must be reserved before parking there, if known.
        /// </summary>
        public Boolean?                                 ReservationRequired       { get; }

        /// <summary>
        /// Whether it is roofed, if known.
        /// </summary>
        public Boolean?                                 Roofed                    { get; }

        /// <summary>
        /// Whether it is lit at night, if known.
        /// </summary>
        public Boolean?                                 Illuminated               { get; }

        /// <summary>
        /// Whether it has an outlet for refrigerated trailers, if known.
        /// </summary>
        public Boolean?                                 RefrigerationOutlet       { get; }

        /// <summary>
        /// Whether vehicles carrying dangerous goods may park there, if known.
        /// </summary>
        public Boolean?                                 DangerousGoodsAllowed     { get; }

        /// <summary>
        /// Whether it is in an area protected against theft and vandalism, if known.
        /// </summary>
        public Boolean?                                 ProtectedArea             { get; }

        /// <summary>
        /// Its reference in an automated parking data system (APDS), e.g. of a truck parking.
        /// </summary>
        public String?                                  APDSReference             { get; }

        /// <summary>
        /// The standards it conforms to, e.g. accessibility ones, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>                 Standards                 { get; }

        /// <summary>
        /// Images of it, each URL once, in their given order.
        /// </summary>
        public ImmutableArray<Image>                    Images                    { get; }

        /// <summary>
        /// The parking space templates it takes, each once, in its order: of every property they give, the first one giving it
        /// decides, unless it has its own. It names them; they do not name their members.
        /// </summary>
        public ImmutableArray<ParkingSpaceTemplate_Id>  TemplateIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space.
        /// </summary>
        public ParkingSpace(ParkingSpace_Id                              Id,
                            I18NString?                                  Name                         = null,
                            I18NString?                                  Description                  = null,
                            GeoCoordinate?                               GeoLocation                  = null,
                            ParkingGarage_Id?                            ParkingGarageId              = null,
                            IEnumerable<ParkingSensor_Id>?               ParkingSensorIds             = null,
                            IEnumerable<ChargingStation_Id>?             ChargingStationIds           = null,
                            IEnumerable<ParkingProduct_Id>?              ParkingProductIds            = null,
                            IEnumerable<ParkingRestrictionGroup>?        ParkingRestrictions          = null,
                            IEnumerable<EUVehicleCategory>?              EUVehicleCategories          = null,
                            String?                                      PhysicalReference            = null,
                            IEnumerable<VehicleType>?                    VehicleTypes                 = null,
                            Kilogram?                                    MaxVehicleWeight             = null,
                            Meter?                                       MaxVehicleHeight             = null,
                            Meter?                                       MaxVehicleLength             = null,
                            Meter?                                       MaxVehicleWidth              = null,
                            Meter?                                       Length                       = null,
                            Meter?                                       Width                        = null,
                            EVSEPosition?                                EVSEPosition                 = null,
                            ParkingDirection?                            Direction                    = null,
                            ParkingSurface?                              Surface                      = null,
                            ParkingSlope?                                Slope                        = null,
                            TimeSpan?                                    TimeLimit                    = null,
                            Boolean?                                     RestrictedToVehicleTypes     = null,
                            Boolean?                                     DriveThrough                 = null,
                            Boolean?                                     ReservationRequired          = null,
                            Boolean?                                     Roofed                       = null,
                            Boolean?                                     Illuminated                  = null,
                            Boolean?                                     RefrigerationOutlet          = null,
                            Boolean?                                     DangerousGoodsAllowed        = null,
                            Boolean?                                     ProtectedArea                = null,
                            String?                                      APDSReference                = null,
                            IEnumerable<Standard>?                       Standards                    = null,
                            IEnumerable<Image>?                          Images                       = null,

                            IEnumerable<ParkingSpaceTemplate_Id>?        TemplateIds                  = null,

                            Timestamped<ParkingSpaceAdminStatusType>?    InitialAdminStatus           = null,
                            Timestamped<ParkingSpaceStatusType>?         InitialStatus                = null,
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
                   InitialAdminStatus         ?? new Timestamped<ParkingSpaceAdminStatusType>(Created ?? Timestamp.Now, ParkingSpaceAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ParkingSpaceStatusType>     (Created ?? Timestamp.Now, ParkingSpaceStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.TemplateIds  = POIJSON.UniqueIds(TemplateIds, $"template of the parking space '{Id}'");

            this.GeoLocation         = GeoLocation;
            this.ParkingGarageId     = ParkingGarageId;
            this.ParkingSensorIds    = POIJSON.OrderedIds(ParkingSensorIds,   $"parking sensor of the parking space '{Id}'");
            this.ChargingStationIds  = POIJSON.OrderedIds(ChargingStationIds, $"charging station of the parking space '{Id}'");
            this.ParkingProductIds = POIJSON.OrderedIds(ParkingProductIds, $"parking product of the parking space '{Id}'");
            this.ParkingRestrictions  = POIJSON.OrderedIds(ParkingRestrictions, $"parking restriction of the parking space '{Id}'");
            this.EUVehicleCategories  = POIJSON.OrderedIds(EUVehicleCategories, $"EU vehicle category of the parking space '{Id}'");

            if (MaxVehicleWeight?.Value <= 0)
                throw new ArgumentException($"The maximum vehicle weight of the parking space '{Id}' must be positive!", nameof(MaxVehicleWeight));

            if (MaxVehicleHeight?.m <= 0)
                throw new ArgumentException($"The maxVehicleHeight of the parking space '{Id}' must be positive!", nameof(MaxVehicleHeight));

            if (MaxVehicleLength?.m <= 0)
                throw new ArgumentException($"The maxVehicleLength of the parking space '{Id}' must be positive!", nameof(MaxVehicleLength));

            if (MaxVehicleWidth?.m <= 0)
                throw new ArgumentException($"The maxVehicleWidth of the parking space '{Id}' must be positive!", nameof(MaxVehicleWidth));

            if (Length?.m <= 0)
                throw new ArgumentException($"The length of the parking space '{Id}' must be positive!", nameof(Length));

            if (Width?.m <= 0)
                throw new ArgumentException($"The width of the parking space '{Id}' must be positive!", nameof(Width));

            if (TimeLimit < TimeSpan.Zero)
                throw new ArgumentException($"The time limit of the parking space '{Id}' must not be negative!", nameof(TimeLimit));

            this.PhysicalReference         = PhysicalReference;
            this.VehicleTypes              = POIJSON.OrderedIds(VehicleTypes, $"vehicle type of the parking space '{Id}'");
            this.MaxVehicleWeight          = MaxVehicleWeight;
            this.MaxVehicleHeight          = MaxVehicleHeight;
            this.MaxVehicleLength          = MaxVehicleLength;
            this.MaxVehicleWidth           = MaxVehicleWidth;
            this.Length                    = Length;
            this.Width                     = Width;
            this.EVSEPosition              = EVSEPosition;
            this.Direction                 = Direction;
            this.Surface                   = Surface;
            this.Slope                     = Slope;
            this.TimeLimit                 = TimeLimit;
            this.RestrictedToVehicleTypes  = RestrictedToVehicleTypes;
            this.DriveThrough              = DriveThrough;
            this.ReservationRequired       = ReservationRequired;
            this.Roofed                    = Roofed;
            this.Illuminated               = Illuminated;
            this.RefrigerationOutlet       = RefrigerationOutlet;
            this.DangerousGoodsAllowed     = DangerousGoodsAllowed;
            this.ProtectedArea             = ProtectedArea;
            this.APDSReference             = APDSReference;
            this.Standards                 = POIJSON.OrderedIds(Standards, $"standard of the parking space '{Id}'");
            this.Images                    = POIJSON.Distinct  (Images, image => image.URL.ToString(), $"image of the parking space '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                       statusEventsLock  = new ();
        private          OnParkingSpaceAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnParkingSpaceStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnParkingSpaceAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnParkingSpaceStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                            Timestamp,
                                                                   EventTracking_Id                          EventTrackingId,
                                                                   Timestamped<ParkingSpaceAdminStatusType>  NewStatus,
                                                                   Timestamped<ParkingSpaceAdminStatusType>  OldStatus,
                                                                   Context?                                  DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                       Timestamp,
                                                              EventTracking_Id                     EventTrackingId,
                                                              Timestamped<ParkingSpaceStatusType>  NewStatus,
                                                              Timestamped<ParkingSpaceStatusType>  OldStatus,
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

        #region ToJSON(Embedded = false, CustomParkingSpaceSerializer = null)

        /// <summary>
        /// Return the JSON representation of this parking space, as its parking operator embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomParkingSpaceSerializer">A delegate to serialize custom parking space JSON objects.</param>
        public JObject ToJSON(Boolean                                         Embedded                       = false,
                              Boolean                                         IncludeETags                   = false,
                              ETagDigestEncoding?                             ETagDigestEncoding             = null,
                              CustomJObjectSerializerDelegate<ParkingSpace>?  CustomParkingSpaceSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,              JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,                 Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,          Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,           DataSource);

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,              GeoLocation.Value.ToJSON(Embedded: true));

            if (ParkingGarageId.HasValue)
                json.Add(PropertyKeys.ParkingGarageId,          ParkingGarageId.Value.ToString());

            if (ParkingSensorIds.Length > 0)
                json.Add(PropertyKeys.ParkingSensorIds,         POIJSON.IdsToJSON(ParkingSensorIds));

            if (ChargingStationIds.Length > 0)
                json.Add(PropertyKeys.ChargingStationIds,       POIJSON.IdsToJSON(ChargingStationIds));

            if (ParkingProductIds.Length > 0)
                json.Add(PropertyKeys.ParkingProductIds,        POIJSON.IdsToJSON(ParkingProductIds));

            if (ParkingRestrictions.Length > 0)
                json.Add(PropertyKeys.ParkingRestrictions,      POIJSON.IdsToJSON(ParkingRestrictions));

            if (EUVehicleCategories.Length > 0)
                json.Add(PropertyKeys.EUVehicleCategories,      POIJSON.IdsToJSON(EUVehicleCategories));

            if (PhysicalReference is not null)
                json.Add(PropertyKeys.PhysicalReference,        PhysicalReference);

            if (VehicleTypes.Length > 0)
                json.Add(PropertyKeys.VehicleTypes,             POIJSON.IdsToJSON(VehicleTypes));

            if (MaxVehicleWeight.HasValue)
                json.Add(PropertyKeys.MaxVehicleWeight,         POIJSON.Number(MaxVehicleWeight.Value.Value));

            if (MaxVehicleHeight.HasValue)
                json.Add(PropertyKeys.MaxVehicleHeight,         POIJSON.Number(MaxVehicleHeight.Value.m));

            if (MaxVehicleLength.HasValue)
                json.Add(PropertyKeys.MaxVehicleLength,         POIJSON.Number(MaxVehicleLength.Value.m));

            if (MaxVehicleWidth.HasValue)
                json.Add(PropertyKeys.MaxVehicleWidth,          POIJSON.Number(MaxVehicleWidth.Value.m));

            if (Length.HasValue)
                json.Add(PropertyKeys.Length,                   POIJSON.Number(Length.Value.m));

            if (Width.HasValue)
                json.Add(PropertyKeys.Width,                    POIJSON.Number(Width.Value.m));

            if (EVSEPosition.HasValue)
                json.Add(PropertyKeys.EVSEPosition,             EVSEPosition.Value.ToString());

            if (Direction.HasValue)
                json.Add(PropertyKeys.Direction,                Direction.Value.ToString());

            if (Surface.HasValue)
                json.Add(PropertyKeys.Surface,                  Surface.Value.ToString());

            if (Slope.HasValue)
                json.Add(PropertyKeys.Slope,                    Slope.Value.ToString());

            if (TimeLimit.HasValue)
                json.Add(PropertyKeys.TimeLimit,                POIJSON.Seconds(TimeLimit.Value));

            if (RestrictedToVehicleTypes.HasValue)
                json.Add(PropertyKeys.RestrictedToVehicleTypes, RestrictedToVehicleTypes.Value);

            if (DriveThrough.HasValue)
                json.Add(PropertyKeys.DriveThrough,             DriveThrough.Value);

            if (ReservationRequired.HasValue)
                json.Add(PropertyKeys.ReservationRequired,      ReservationRequired.Value);

            if (Roofed.HasValue)
                json.Add(PropertyKeys.Roofed,                   Roofed.Value);

            if (Illuminated.HasValue)
                json.Add(PropertyKeys.Illuminated,              Illuminated.Value);

            if (RefrigerationOutlet.HasValue)
                json.Add(PropertyKeys.RefrigerationOutlet,      RefrigerationOutlet.Value);

            if (DangerousGoodsAllowed.HasValue)
                json.Add(PropertyKeys.DangerousGoodsAllowed,    DangerousGoodsAllowed.Value);

            if (ProtectedArea.HasValue)
                json.Add(PropertyKeys.ProtectedArea,            ProtectedArea.Value);

            if (APDSReference is not null)
                json.Add(PropertyKeys.APDSReference,            APDSReference);

            if (Standards.Length > 0)
                json.Add(PropertyKeys.Standards,                POIJSON.IdsToJSON(Standards));

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,                   new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (TemplateIds.Length > 0)
                json.Add(PropertyKeys.TemplateIds,              POIJSON.IdsToJSON(TemplateIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomParkingSpaceSerializer is not null
                       ? CustomParkingSpaceSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ParkingSpace, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a parking space.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ParkingSpace">The parsed parking space.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                 JSON,
                                       [NotNullWhen(true)]  out ParkingSpace?  ParkingSpace,
                                       [NotNullWhen(false)] out String?        ErrorResponse)
        {

            ParkingSpace = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "parking space identification",
                                         ParkingSpace_Id.TryParse,
                                         out ParkingSpace_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the parking space '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N       (JSON, BasePropertyKeys.Name,                "parking space name",                out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, BasePropertyKeys.Description,         "parking space description",         out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, BasePropertyKeys.DataSource,          "data source",                       out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON, PropertyKeys.GeoLocation,  "geo location",                      out var geoLocation,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, PropertyKeys.ParkingGarageId,     "parking garage identification",     ParkingGarage_Id.  TryParse, out ParkingGarage_Id? parkingGarageId,   out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.ParkingSensorIds,    "parking sensor identification",     ParkingSensor_Id.  TryParse, out ImmutableArray<ParkingSensor_Id>   parkingSensorIds,   out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.ChargingStationIds,  "charging station identification",   ChargingStation_Id.TryParse, out ImmutableArray<ChargingStation_Id> chargingStationIds, out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseIds(JSON, PropertyKeys.ParkingProductIds,   "parking product identification", ParkingProduct_Id.      TryParse, out ImmutableArray<ParkingProduct_Id>       parkingProductIds,    out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, PropertyKeys.ParkingRestrictions, "parking restriction",            ParkingRestrictionGroup.TryParse, out ImmutableArray<ParkingRestrictionGroup> parkingRestrictions,  out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, PropertyKeys.EUVehicleCategories, "EU vehicle category",            EUVehicleCategory.      TryParse, out ImmutableArray<EUVehicleCategory>       euVehicleCategories,  out ErrorResponse))
                {
                    return false;
                }


                if (!POIJSON.TryParseString     (JSON, PropertyKeys.PhysicalReference,  "physical reference",            out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, PropertyKeys.APDSReference,      "APDS reference",                out var apdsReference,      out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.VehicleTypes,       "vehicle type",  VehicleType.TryParse, out ImmutableArray<VehicleType> vehicleTypes, out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Standards,          "standard",      Standard.   TryParse, out ImmutableArray<Standard>    standards,    out ErrorResponse) ||
                    !POIJSON.TryParseObjects<Image>(JSON, PropertyKeys.Images,          PropertyKeys.Images,        Image.      TryParse, out var images,                               out ErrorResponse) ||
                    !POIJSON.TryParseSeconds    (JSON, PropertyKeys.TimeLimit,          "time limit",                    out var timeLimit,          out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, PropertyKeys.MaxVehicleWeight,   "maximum vehicle weight",        out var maxVehicleWeight,   out ErrorResponse))
                {
                    return false;
                }

                var meters = new Dictionary<String, Decimal?>();

                foreach (var key in new[] { PropertyKeys.MaxVehicleHeight, PropertyKeys.MaxVehicleLength, PropertyKeys.MaxVehicleWidth, PropertyKeys.Length, PropertyKeys.Width })
                {
                    if (!POIJSON.TryParseNonNegative(JSON, key, key, out var meter, out ErrorResponse))
                        return false;
                    meters[key] = meter;
                }

                if (!POIJSON.TryParseEnum(JSON, PropertyKeys.EVSEPosition, PropertyKeys.EVSEPosition, out EVSEPosition? eVSEPosition, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseEnum(JSON, PropertyKeys.Direction, PropertyKeys.Direction, out ParkingDirection? direction, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseEnum(JSON, PropertyKeys.Surface, PropertyKeys.Surface, out ParkingSurface? surface, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseEnum(JSON, PropertyKeys.Slope, PropertyKeys.Slope, out ParkingSlope? slope, out ErrorResponse))
                    return false;

                var flags = new Dictionary<String, Boolean?>();

                foreach (var key in new[] { PropertyKeys.RestrictedToVehicleTypes, PropertyKeys.DriveThrough, PropertyKeys.ReservationRequired, PropertyKeys.Roofed, PropertyKeys.Illuminated, PropertyKeys.RefrigerationOutlet, PropertyKeys.DangerousGoodsAllowed, PropertyKeys.ProtectedArea })
                {
                    flags[key] = null;
                    if (JSON.ParseOptional(key, key, out Boolean? flag, out ErrorResponse))
                    {
                        if (ErrorResponse is not null)
                            return false;
                        flags[key] = flag;
                    }
                }

                if (!POIJSON.TryParseIds(JSON, PropertyKeys.TemplateIds, "template identification", ParkingSpaceTemplate_Id.TryParse, out ImmutableArray<ParkingSpaceTemplate_Id> templateIds, out ErrorResponse))
                    return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ParkingSpace = new ParkingSpace(id,
                                                name,
                                                description,
                                                geoLocation,
                                                parkingGarageId,
                                                parkingSensorIds,
                                                chargingStationIds,
                                                parkingProductIds,
                                                parkingRestrictions,
                                                euVehicleCategories,
                                                physicalReference,
                                                vehicleTypes,
                                                maxVehicleWeight.HasValue ? Kilogram.FromKG(maxVehicleWeight.Value) : null,
                                                meters[PropertyKeys.MaxVehicleHeight].HasValue ? Meter.From_m(meters[PropertyKeys.MaxVehicleHeight]!.Value) : null,
                                                meters[PropertyKeys.MaxVehicleLength].HasValue ? Meter.From_m(meters[PropertyKeys.MaxVehicleLength]!.Value) : null,
                                                meters[PropertyKeys.MaxVehicleWidth].HasValue ? Meter.From_m(meters[PropertyKeys.MaxVehicleWidth]!.Value) : null,
                                                meters[PropertyKeys.Length].HasValue ? Meter.From_m(meters[PropertyKeys.Length]!.Value) : null,
                                                meters[PropertyKeys.Width].HasValue ? Meter.From_m(meters[PropertyKeys.Width]!.Value) : null,
                                                eVSEPosition,
                                                direction,
                                                surface,
                                                slope,
                                                timeLimit,
                                                flags[PropertyKeys.RestrictedToVehicleTypes],
                                                flags[PropertyKeys.DriveThrough],
                                                flags[PropertyKeys.ReservationRequired],
                                                flags[PropertyKeys.Roofed],
                                                flags[PropertyKeys.Illuminated],
                                                flags[PropertyKeys.RefrigerationOutlet],
                                                flags[PropertyKeys.DangerousGoodsAllowed],
                                                flags[PropertyKeys.ProtectedArea],
                                                apdsReference,
                                                standards,
                                                images,
                                                TemplateIds:  templateIds,
                                                DataSource:  dataSource,
                                                Created:     created,
                                                LastChange:  lastChange,
                                                CustomData:  customData);

                if (!TryCheckETagsOf(JSON, ParkingSpace, out ErrorResponse))
                {
                    ParkingSpace = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ParkingSpace   = null;
                ErrorResponse  = "The given JSON representation of a parking space is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two parking spaces for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ParkingSpace? ParkingSpace1, ParkingSpace? ParkingSpace2)

            => ReferenceEquals(ParkingSpace1, ParkingSpace2) ||
               (ParkingSpace1 is not null && ParkingSpace1.Equals(ParkingSpace2));

        /// <summary>
        /// Compares two parking spaces for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ParkingSpace? ParkingSpace1, ParkingSpace? ParkingSpace2)

            => !(ParkingSpace1 == ParkingSpace2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ParkingSpace parkingSpace
                   ? CompareTo(parkingSpace)
                   : throw new ArgumentException("The given object is not a parking space!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ParkingSpace? ParkingSpace)

            => ParkingSpace is null
                   ? throw new ArgumentNullException(nameof(ParkingSpace), "The given parking space must not be null!")
                   : Id.CompareTo(ParkingSpace.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpace parkingSpace &&
               Equals(parkingSpace);

        /// <inheritdoc/>
        public Boolean Equals(ParkingSpace? ParkingSpace)

            => ParkingSpace is not null &&
               Id.Equals(ParkingSpace.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
