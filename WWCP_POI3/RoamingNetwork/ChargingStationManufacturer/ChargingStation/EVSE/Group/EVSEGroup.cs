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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of an EVSE group changed.
    /// </summary>
    public delegate Task OnEVSEGroupAdminStatusChangedDelegate(DateTimeOffset                          Timestamp,
                                                               EventTracking_Id                        EventTrackingId,
                                                               EVSEGroup                               EVSEGroup,
                                                               Timestamped<EVSEGroupAdminStatusType>   OldStatus,
                                                               Timestamped<EVSEGroupAdminStatusType>   NewStatus,
                                                               Context?                                DataSource);

    /// <summary>
    /// The status of an EVSE group changed.
    /// </summary>
    public delegate Task OnEVSEGroupStatusChangedDelegate(DateTimeOffset                     Timestamp,
                                                          EventTracking_Id                   EventTrackingId,
                                                          EVSEGroup                          EVSEGroup,
                                                          Timestamped<EVSEGroupStatusType>   OldStatus,
                                                          Timestamped<EVSEGroupStatusType>   NewStatus,
                                                          Context?                           DataSource);


    /// <summary>
    /// A group of EVSEs of one charging station operator, embedded completely in it; groups may overlap.
    /// </summary>
    public sealed class EVSEGroup : AImmutableEMobilityEntity<EVSEGroup_Id,
                                                              EVSEGroupAdminStatusType,
                                                              EVSEGroupStatusType>,
                                    IEquatable<EVSEGroup>,
                                    IComparable<EVSEGroup>
    {

        #region Properties

        /// <summary>
        /// Its EVSEs, of the same charging station operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EVSE_Id>  EVSEIds           { get; }

        /// <summary>
        /// The charging tariff of its operator for all its members, unless their own apply.
        /// </summary>
        public ChargingTariff_Id?       ChargingTariffId  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVSE group.
        /// </summary>
        public EVSEGroup(EVSEGroup_Id                             Id,
                         I18NString?                              Name                         = null,
                         I18NString?                              Description                  = null,
                         IEnumerable<EVSE_Id>?                    EVSEIds                      = null,
                         ChargingTariff_Id?                       ChargingTariffId             = null,

                         Timestamped<EVSEGroupAdminStatusType>?   InitialAdminStatus           = null,
                         Timestamped<EVSEGroupStatusType>?        InitialStatus                = null,
                         UInt16?                                  MaxAdminStatusScheduleSize   = null,
                         UInt16?                                  MaxStatusScheduleSize        = null,

                         String?                                  DataSource                   = null,
                         DateTimeOffset?                          Created                      = null,
                         DateTimeOffset?                          LastChange                   = null,

                         CustomDataNew?                           CustomData                   = null,
                         UserDefinedDictionary?                   InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<EVSEGroupAdminStatusType>(Created ?? Timestamp.Now, EVSEGroupAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<EVSEGroupStatusType>     (Created ?? Timestamp.Now, EVSEGroupStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.EVSEIds           = POIJSON.OrderedIds(EVSEIds, $"member of the EVSE group '{Id}'");
            this.ChargingTariffId  = ChargingTariffId;

        }

        #endregion


        #region Status events

        private readonly Lock                                    statusEventsLock  = new ();
        private          OnEVSEGroupAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnEVSEGroupStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnEVSEGroupAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnEVSEGroupStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                          Timestamp,
                                                                   EventTracking_Id                        EventTrackingId,
                                                                   Timestamped<EVSEGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<EVSEGroupAdminStatusType>   OldStatus,
                                                                   Context?                                DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                     Timestamp,
                                                              EventTracking_Id                   EventTrackingId,
                                                              Timestamped<EVSEGroupStatusType>   NewStatus,
                                                              Timestamped<EVSEGroupStatusType>   OldStatus,
                                                              Context?                           DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this EVSE group, as its charging station operator embeds it.
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

            if (EVSEIds.Length > 0)
                json.Add("EVSEIds",  POIJSON.IdsToJSON(EVSEIds));

            if (ChargingTariffId.HasValue)
                json.Add("chargingTariffId",  ChargingTariffId.Value.ToString());

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out EVSEGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an EVSE group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EVSEGroup">The parsed EVSE group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                               JSON,
                                       [NotNullWhen(true)]  out EVSEGroup?   EVSEGroup,
                                       [NotNullWhen(false)] out String?      ErrorResponse)
        {

            EVSEGroup = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "EVSE group identification",
                                         EVSEGroup_Id.TryParse,
                                         out EVSEGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, "name",         "EVSE group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, "description",  "EVSE group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "dataSource",   "data source",           out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, "EVSEIds", "member identification", EVSE_Id.TryParse, out ImmutableArray<EVSE_Id> members, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "chargingTariffId",  "charging tariff identification",  ChargingTariff_Id.TryParse, out ChargingTariff_Id? chargingTariffId, out ErrorResponse))
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

                EVSEGroup = new EVSEGroup(id,
                                          name,
                                          description,
                                          members,
                                          chargingTariffId,
                                          DataSource:  dataSource,
                                          Created:     created,
                                          LastChange:  lastChange,
                                          CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EVSEGroup      = null;
                ErrorResponse  = "The given JSON representation of an EVSE group is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two EVSE groups for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EVSEGroup? EVSEGroup1, EVSEGroup? EVSEGroup2)

            => ReferenceEquals(EVSEGroup1, EVSEGroup2) ||
               (EVSEGroup1 is not null && EVSEGroup1.Equals(EVSEGroup2));

        /// <summary>
        /// Compares two EVSE groups for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EVSEGroup? EVSEGroup1, EVSEGroup? EVSEGroup2)

            => !(EVSEGroup1 == EVSEGroup2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is EVSEGroup evseGroup
                   ? CompareTo(evseGroup)
                   : throw new ArgumentException("The given object is not an EVSE group!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EVSEGroup? EVSEGroup)

            => EVSEGroup is null
                   ? throw new ArgumentNullException(nameof(EVSEGroup), "The given EVSE group must not be null!")
                   : Id.CompareTo(EVSEGroup.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EVSEGroup evseGroup &&
               Equals(evseGroup);

        /// <inheritdoc/>
        public Boolean Equals(EVSEGroup? EVSEGroup)

            => EVSEGroup is not null &&
               Id.Equals(EVSEGroup.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
