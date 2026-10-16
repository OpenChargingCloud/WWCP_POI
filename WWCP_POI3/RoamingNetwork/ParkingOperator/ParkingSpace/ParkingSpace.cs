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

        #region Properties

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

            this.GeoLocation         = GeoLocation;
            this.ParkingGarageId     = ParkingGarageId;
            this.ParkingSensorIds    = POIJSON.OrderedIds(ParkingSensorIds,   $"parking sensor of the parking space '{Id}'");
            this.ChargingStationIds  = POIJSON.OrderedIds(ChargingStationIds, $"charging station of the parking space '{Id}'");
            this.ParkingProductIds = POIJSON.OrderedIds(ParkingProductIds, $"parking product of the parking space '{Id}'");
            this.ParkingRestrictions  = POIJSON.OrderedIds(ParkingRestrictions, $"parking restriction of the parking space '{Id}'");
            this.EUVehicleCategories  = POIJSON.OrderedIds(EUVehicleCategories, $"EU vehicle category of the parking space '{Id}'");

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


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this parking space, as its parking operator embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",                Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",         Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",          DataSource);

            if (GeoLocation.HasValue)
                json.Add("geoLocation",         GeoLocation.Value.ToJSON(Embedded: true));

            if (ParkingGarageId.HasValue)
                json.Add("parkingGarageId",     ParkingGarageId.Value.ToString());

            if (ParkingSensorIds.Length > 0)
                json.Add("parkingSensorIds",    POIJSON.IdsToJSON(ParkingSensorIds));

            if (ChargingStationIds.Length > 0)
                json.Add("chargingStationIds",  POIJSON.IdsToJSON(ChargingStationIds));

            if (ParkingProductIds.Length > 0)
                json.Add("parkingProductIds",  POIJSON.IdsToJSON(ParkingProductIds));

            if (ParkingRestrictions.Length > 0)
                json.Add("parkingRestrictions",  POIJSON.IdsToJSON(ParkingRestrictions));

            if (EUVehicleCategories.Length > 0)
                json.Add("euVehicleCategories",  POIJSON.IdsToJSON(EUVehicleCategories));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

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

                if (!JSON.ParseMandatory("@id",
                                         "parking space identification",
                                         ParkingSpace_Id.TryParse,
                                         out ParkingSpace_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N       (JSON, "name",                "parking space name",                out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "description",         "parking space description",         out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "dataSource",          "data source",                       out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON,                                                             out var geoLocation,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "parkingGarageId",     "parking garage identification",     ParkingGarage_Id.  TryParse, out ParkingGarage_Id? parkingGarageId,   out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "parkingSensorIds",    "parking sensor identification",     ParkingSensor_Id.  TryParse, out ImmutableArray<ParkingSensor_Id>   parkingSensorIds,   out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "chargingStationIds",  "charging station identification",   ChargingStation_Id.TryParse, out ImmutableArray<ChargingStation_Id> chargingStationIds, out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseIds(JSON, "parkingProductIds",   "parking product identification", ParkingProduct_Id.      TryParse, out ImmutableArray<ParkingProduct_Id>       parkingProductIds,    out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, "parkingRestrictions", "parking restriction",            ParkingRestrictionGroup.TryParse, out ImmutableArray<ParkingRestrictionGroup> parkingRestrictions,  out ErrorResponse) ||
                    !POIJSON.TryParseIds(JSON, "euVehicleCategories", "EU vehicle category",            EUVehicleCategory.      TryParse, out ImmutableArray<EUVehicleCategory>       euVehicleCategories,  out ErrorResponse))
                {
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
                                                DataSource:  dataSource,
                                                Created:     created,
                                                LastChange:  lastChange,
                                                CustomData:  customData);

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
