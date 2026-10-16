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
    /// The admin status of a parking sensor changed.
    /// </summary>
    public delegate Task OnParkingSensorAdminStatusChangedDelegate(DateTimeOffset                              Timestamp,
                                                                   EventTracking_Id                            EventTrackingId,
                                                                   ParkingSensor                               ParkingSensor,
                                                                   Timestamped<ParkingSensorAdminStatusType>   OldStatus,
                                                                   Timestamped<ParkingSensorAdminStatusType>   NewStatus,
                                                                   Context?                                    DataSource);

    /// <summary>
    /// The status of a parking sensor changed.
    /// </summary>
    public delegate Task OnParkingSensorStatusChangedDelegate(DateTimeOffset                         Timestamp,
                                                              EventTracking_Id                       EventTrackingId,
                                                              ParkingSensor                          ParkingSensor,
                                                              Timestamped<ParkingSensorStatusType>   OldStatus,
                                                              Timestamped<ParkingSensorStatusType>   NewStatus,
                                                              Context?                               DataSource);


    /// <summary>
    /// A sensor detecting whether parking spaces are occupied, embedded completely in its parking operator.
    /// Its identification is unique within the roaming network.
    /// </summary>
    public sealed class ParkingSensor : AImmutableEMobilityEntity<ParkingSensor_Id,
                                                                  ParkingSensorAdminStatusType,
                                                                  ParkingSensorStatusType>,
                                        IEquatable<ParkingSensor>,
                                        IComparable<ParkingSensor>
    {

        #region Properties

        /// <summary>
        /// The geographical location.
        /// </summary>
        public GeoCoordinate?                      GeoLocation           { get; }

        /// <summary>
        /// The charging stations it watches, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingStation_Id>  ChargingStationIds    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking sensor.
        /// </summary>
        public ParkingSensor(ParkingSensor_Id                              Id,
                             I18NString?                                   Name                         = null,
                             I18NString?                                   Description                  = null,
                             GeoCoordinate?                                GeoLocation                  = null,
                             IEnumerable<ChargingStation_Id>?              ChargingStationIds           = null,

                             Timestamped<ParkingSensorAdminStatusType>?    InitialAdminStatus           = null,
                             Timestamped<ParkingSensorStatusType>?         InitialStatus                = null,
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
                   InitialAdminStatus         ?? new Timestamped<ParkingSensorAdminStatusType>(Created ?? Timestamp.Now, ParkingSensorAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ParkingSensorStatusType>     (Created ?? Timestamp.Now, ParkingSensorStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.GeoLocation         = GeoLocation;
            this.ChargingStationIds  = POIJSON.OrderedIds(ChargingStationIds, $"charging station of the parking sensor '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                        statusEventsLock  = new ();
        private          OnParkingSensorAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnParkingSensorStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnParkingSensorAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnParkingSensorStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                             Timestamp,
                                                                   EventTracking_Id                           EventTrackingId,
                                                                   Timestamped<ParkingSensorAdminStatusType>  NewStatus,
                                                                   Timestamped<ParkingSensorAdminStatusType>  OldStatus,
                                                                   Context?                                   DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                        Timestamp,
                                                              EventTracking_Id                      EventTrackingId,
                                                              Timestamped<ParkingSensorStatusType>  NewStatus,
                                                              Timestamped<ParkingSensorStatusType>  OldStatus,
                                                              Context?                              DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this parking sensor, as its parking operator embeds it.
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

            if (ChargingStationIds.Length > 0)
                json.Add("chargingStationIds",  POIJSON.IdsToJSON(ChargingStationIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ParkingSensor, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a parking sensor.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ParkingSensor">The parsed parking sensor.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                  JSON,
                                       [NotNullWhen(true)]  out ParkingSensor?  ParkingSensor,
                                       [NotNullWhen(false)] out String?         ErrorResponse)
        {

            ParkingSensor = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "parking sensor identification",
                                         ParkingSensor_Id.TryParse,
                                         out ParkingSensor_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N       (JSON, "name",                "parking sensor name",               out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "description",         "parking sensor description",        out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "dataSource",          "data source",                       out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON,                                                             out var geoLocation,  out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "chargingStationIds",  "charging station identification",   ChargingStation_Id.TryParse, out ImmutableArray<ChargingStation_Id> chargingStationIds, out ErrorResponse))
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

                ParkingSensor = new ParkingSensor(id,
                                                  name,
                                                  description,
                                                  geoLocation,
                                                  chargingStationIds,
                                                  DataSource:  dataSource,
                                                  Created:     created,
                                                  LastChange:  lastChange,
                                                  CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ParkingSensor  = null;
                ErrorResponse  = "The given JSON representation of a parking sensor is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two parking sensors for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ParkingSensor? ParkingSensor1, ParkingSensor? ParkingSensor2)

            => ReferenceEquals(ParkingSensor1, ParkingSensor2) ||
               (ParkingSensor1 is not null && ParkingSensor1.Equals(ParkingSensor2));

        /// <summary>
        /// Compares two parking sensors for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ParkingSensor? ParkingSensor1, ParkingSensor? ParkingSensor2)

            => !(ParkingSensor1 == ParkingSensor2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ParkingSensor parkingSensor
                   ? CompareTo(parkingSensor)
                   : throw new ArgumentException("The given object is not a parking sensor!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ParkingSensor? ParkingSensor)

            => ParkingSensor is null
                   ? throw new ArgumentNullException(nameof(ParkingSensor), "The given parking sensor must not be null!")
                   : Id.CompareTo(ParkingSensor.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSensor parkingSensor &&
               Equals(parkingSensor);

        /// <inheritdoc/>
        public Boolean Equals(ParkingSensor? ParkingSensor)

            => ParkingSensor is not null &&
               Id.Equals(ParkingSensor.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
