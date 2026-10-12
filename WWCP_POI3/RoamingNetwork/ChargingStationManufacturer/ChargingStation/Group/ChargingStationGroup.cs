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
    /// The admin status of a charging station group changed.
    /// </summary>
    public delegate Task OnChargingStationGroupAdminStatusChangedDelegate(DateTimeOffset                                     Timestamp,
                                                                          EventTracking_Id                                   EventTrackingId,
                                                                          ChargingStationGroup                               ChargingStationGroup,
                                                                          Timestamped<ChargingStationGroupAdminStatusType>   OldStatus,
                                                                          Timestamped<ChargingStationGroupAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of a charging station group changed.
    /// </summary>
    public delegate Task OnChargingStationGroupStatusChangedDelegate(DateTimeOffset                                Timestamp,
                                                                     EventTracking_Id                              EventTrackingId,
                                                                     ChargingStationGroup                          ChargingStationGroup,
                                                                     Timestamped<ChargingStationGroupStatusType>   OldStatus,
                                                                     Timestamped<ChargingStationGroupStatusType>   NewStatus);


    /// <summary>
    /// A group of charging stations of one charging station operator, embedded completely in it; groups may overlap.
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

            this.ChargingStationIds  = POIJSON.OrderedIds(ChargingStationIds, $"member of the charging station group '{Id}'");
            this.ChargingTariffId  = ChargingTariffId;

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
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
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
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
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
