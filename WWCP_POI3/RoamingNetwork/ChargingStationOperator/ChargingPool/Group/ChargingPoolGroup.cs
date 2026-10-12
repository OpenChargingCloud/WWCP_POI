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
    /// The admin status of a charging pool group changed.
    /// </summary>
    public delegate Task OnChargingPoolGroupAdminStatusChangedDelegate(DateTimeOffset                                  Timestamp,
                                                                       EventTracking_Id                                EventTrackingId,
                                                                       ChargingPoolGroup                               ChargingPoolGroup,
                                                                       Timestamped<ChargingPoolGroupAdminStatusType>   OldStatus,
                                                                       Timestamped<ChargingPoolGroupAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of a charging pool group changed.
    /// </summary>
    public delegate Task OnChargingPoolGroupStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  ChargingPoolGroup                          ChargingPoolGroup,
                                                                  Timestamped<ChargingPoolGroupStatusType>   OldStatus,
                                                                  Timestamped<ChargingPoolGroupStatusType>   NewStatus);


    /// <summary>
    /// A group of charging pools of one charging station operator, embedded completely in it; groups may overlap.
    /// </summary>
    public sealed class ChargingPoolGroup : AImmutableEMobilityEntity<ChargingPoolGroup_Id,
                                                                      ChargingPoolGroupAdminStatusType,
                                                                      ChargingPoolGroupStatusType>,
                                            IEquatable<ChargingPoolGroup>,
                                            IComparable<ChargingPoolGroup>
    {

        #region Properties

        /// <summary>
        /// Its charging pools, of the same charging station operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingPool_Id>  ChargingPoolIds   { get; }

        /// <summary>
        /// The charging tariff of its operator for all its members, unless their own apply.
        /// </summary>
        public ChargingTariff_Id?               ChargingTariffId  { get; }

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

            this.ChargingPoolIds   = POIJSON.OrderedIds(ChargingPoolIds, $"member of the charging pool group '{Id}'");
            this.ChargingTariffId  = ChargingTariffId;

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
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
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
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
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
