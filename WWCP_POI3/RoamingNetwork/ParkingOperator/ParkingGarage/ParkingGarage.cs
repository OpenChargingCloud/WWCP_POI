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
    /// The admin status of a parking garage changed.
    /// </summary>
    public delegate Task OnParkingGarageAdminStatusChangedDelegate(DateTimeOffset                              Timestamp,
                                                                   EventTracking_Id                            EventTrackingId,
                                                                   ParkingGarage                               ParkingGarage,
                                                                   Timestamped<ParkingGarageAdminStatusType>   OldStatus,
                                                                   Timestamped<ParkingGarageAdminStatusType>   NewStatus,
                                                                   Context?                                    DataSource);

    /// <summary>
    /// The status of a parking garage changed.
    /// </summary>
    public delegate Task OnParkingGarageStatusChangedDelegate(DateTimeOffset                         Timestamp,
                                                              EventTracking_Id                       EventTrackingId,
                                                              ParkingGarage                          ParkingGarage,
                                                              Timestamped<ParkingGarageStatusType>   OldStatus,
                                                              Timestamped<ParkingGarageStatusType>   NewStatus,
                                                              Context?                               DataSource);


    /// <summary>
    /// A parking garage or parking lot with its parking spaces, embedded completely in its parking operator.
    /// Its parking spaces name it; they belong to the parking operator.
    /// Its identification is unique within the roaming network.
    /// </summary>
    public sealed class ParkingGarage : AImmutableEMobilityEntity<ParkingGarage_Id,
                                                                  ParkingGarageAdminStatusType,
                                                                  ParkingGarageStatusType>,
                                        IEquatable<ParkingGarage>,
                                        IComparable<ParkingGarage>
    {

        #region Properties

        /// <summary>
        /// The address.
        /// </summary>
        public Address?                            Address               { get; }

        /// <summary>
        /// The geographical location.
        /// </summary>
        public GeoCoordinate?                      GeoLocation           { get; }

        /// <summary>
        /// The charging stations within, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingStation_Id>  ChargingStationIds    { get; }


        /// <summary>
        /// When it is open; always, unless given.
        /// </summary>
        public ImmutableOpeningTimes?  OpeningTimes  { get; }


        /// <summary>
        /// Its images, in their given order.
        /// </summary>
        public ImmutableArray<Image>  Images  { get; }

        /// <summary>
        /// The parking products of its parking operator offered here, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingProduct_Id>  ParkingProductIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking garage.
        /// </summary>
        public ParkingGarage(ParkingGarage_Id                              Id,
                             I18NString?                                   Name                         = null,
                             I18NString?                                   Description                  = null,
                             Address?                                      Address                      = null,
                             GeoCoordinate?                                GeoLocation                  = null,
                             IEnumerable<ChargingStation_Id>?              ChargingStationIds           = null,
                             ImmutableOpeningTimes?                        OpeningTimes                 = null,
                             IEnumerable<Image>?                           Images                       = null,
                             IEnumerable<ParkingProduct_Id>?               ParkingProductIds            = null,

                             Timestamped<ParkingGarageAdminStatusType>?    InitialAdminStatus           = null,
                             Timestamped<ParkingGarageStatusType>?         InitialStatus                = null,
                             UInt16?                                       MaxAdminStatusScheduleSize   = null,
                             UInt16?                                       MaxStatusScheduleSize        = null,

                             String?                                       DataSource                   = null,
                             DateTimeOffset?                               Created                      = null,
                             DateTimeOffset?                               LastChange                   = null,

                             CustomDataNew?                                CustomData                   = null,
                             UserDefinedDictionary?                        InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ParkingGarageAdminStatusType>(Created ?? Timestamp.Now, ParkingGarageAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ParkingGarageStatusType>     (Created ?? Timestamp.Now, ParkingGarageStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.Address             = Address;
            this.GeoLocation         = GeoLocation;
            this.ChargingStationIds  = POIJSON.OrderedIds(ChargingStationIds, $"charging station of the parking garage '{Id}'");
            this.OpeningTimes        = OpeningTimes;
            this.Images = POIJSON.Distinct(Images, image => image.URL.ToString(), $"image of the parking garage '{Id}'");
            this.ParkingProductIds = POIJSON.OrderedIds(ParkingProductIds, $"parking product of the parking garage '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                        statusEventsLock  = new ();
        private          OnParkingGarageAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnParkingGarageStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnParkingGarageAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnParkingGarageStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                             Timestamp,
                                                                   EventTracking_Id                           EventTrackingId,
                                                                   Timestamped<ParkingGarageAdminStatusType>  NewStatus,
                                                                   Timestamped<ParkingGarageAdminStatusType>  OldStatus,
                                                                   Context?                                   DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                        Timestamp,
                                                              EventTracking_Id                      EventTrackingId,
                                                              Timestamped<ParkingGarageStatusType>  NewStatus,
                                                              Timestamped<ParkingGarageStatusType>  OldStatus,
                                                              Context?                              DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this parking garage, as its parking operator embeds it.
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

            if (Address is not null)
                json.Add("address",             Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",         GeoLocation.Value.ToJSON(Embedded: true));

            if (ChargingStationIds.Length > 0)
                json.Add("chargingStationIds",  POIJSON.IdsToJSON(ChargingStationIds));

            if (OpeningTimes is not null)
                json.Add("openingTimes",  OpeningTimes.ToJSON());

            if (Images.Length > 0)
                json.Add("images",  new JArray(Images.Select(image => image.ToJSON())));
            if (ParkingProductIds.Length > 0)
                json.Add("parkingProductIds",  POIJSON.IdsToJSON(ParkingProductIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ParkingGarage, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a parking garage.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ParkingGarage">The parsed parking garage.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out ParkingGarage?  ParkingGarage,
                                       [NotNullWhen(false)] out String?         ErrorResponse)
        {

            ParkingGarage = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "parking garage identification",
                                         ParkingGarage_Id.TryParse,
                                         out ParkingGarage_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N       (JSON, "name",                "parking garage name",               out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "description",         "parking garage description",        out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "dataSource",          "data source",                       out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON,                                                             out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON,                                                             out var geoLocation,  out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "chargingStationIds",  "charging station identification",   ChargingStation_Id.TryParse, out ImmutableArray<ChargingStation_Id> chargingStationIds, out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseObject<ImmutableOpeningTimes>(JSON, "openingTimes", "opening times", ImmutableOpeningTimes.TryParse, out var openingTimes, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, "images", "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseIds(JSON, "parkingProductIds", "parking product identification", ParkingProduct_Id.TryParse, out ImmutableArray<ParkingProduct_Id> parkingProductIds, out ErrorResponse))
                    return false;

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ParkingGarage = new ParkingGarage(id,
                                                  name,
                                                  description,
                                                  address,
                                                  geoLocation,
                                                  chargingStationIds,
                                                  openingTimes,
                                                  images,
                                                  parkingProductIds,
                                                  DataSource:  dataSource,
                                                  Created:     created,
                                                  LastChange:  lastChange,
                                                  CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ParkingGarage  = null;
                ErrorResponse  = "The given JSON representation of a parking garage is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two parking garages for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ParkingGarage? ParkingGarage1, ParkingGarage? ParkingGarage2)

            => ReferenceEquals(ParkingGarage1, ParkingGarage2) ||
               (ParkingGarage1 is not null && ParkingGarage1.Equals(ParkingGarage2));

        /// <summary>
        /// Compares two parking garages for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ParkingGarage? ParkingGarage1, ParkingGarage? ParkingGarage2)

            => !(ParkingGarage1 == ParkingGarage2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ParkingGarage parkingGarage
                   ? CompareTo(parkingGarage)
                   : throw new ArgumentException("The given object is not a parking garage!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ParkingGarage? ParkingGarage)

            => ParkingGarage is null
                   ? throw new ArgumentNullException(nameof(ParkingGarage), "The given parking garage must not be null!")
                   : Id.CompareTo(ParkingGarage.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ParkingGarage parkingGarage &&
               Equals(parkingGarage);

        /// <inheritdoc/>
        public Boolean Equals(ParkingGarage? ParkingGarage)

            => ParkingGarage is not null &&
               Id.Equals(ParkingGarage.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
