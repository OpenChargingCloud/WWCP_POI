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
    /// The admin status of a charging tariff group changed.
    /// </summary>
    public delegate Task OnChargingTariffGroupAdminStatusChangedDelegate(DateTimeOffset                                    Timestamp,
                                                                         EventTracking_Id                                  EventTrackingId,
                                                                         ChargingTariffGroup                               ChargingTariffGroup,
                                                                         Timestamped<ChargingTariffGroupAdminStatusType>   OldStatus,
                                                                         Timestamped<ChargingTariffGroupAdminStatusType>   NewStatus,
                                                                         Context?                                          DataSource);

    /// <summary>
    /// The status of a charging tariff group changed.
    /// </summary>
    public delegate Task OnChargingTariffGroupStatusChangedDelegate(DateTimeOffset                               Timestamp,
                                                                    EventTracking_Id                             EventTrackingId,
                                                                    ChargingTariffGroup                          ChargingTariffGroup,
                                                                    Timestamped<ChargingTariffGroupStatusType>   OldStatus,
                                                                    Timestamped<ChargingTariffGroupStatusType>   NewStatus,
                                                                    Context?                                     DataSource);

    /// <summary>
    /// A group of charging tariffs of one charging station operator, embedded completely in it; groups may overlap.
    /// </summary>
    public sealed class ChargingTariffGroup : AImmutableEMobilityEntity<ChargingTariffGroup_Id,
                                                                        ChargingTariffGroupAdminStatusType,
                                                                        ChargingTariffGroupStatusType>,
                                              IEquatable<ChargingTariffGroup>,
                                              IComparable<ChargingTariffGroup>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingTariffGroup/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the identifications of its charging tariffs.
            /// </summary>
            public const String  ChargingTariffIds   = "chargingTariffIds";

        }

        #endregion


        /// <summary>
        /// Its charging tariffs, of the same charging station operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingTariff_Id>  ChargingTariffIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging tariff group.
        /// </summary>
        public ChargingTariffGroup(ChargingTariffGroup_Id                             Id,
                                   I18NString?                                        Name                         = null,
                                   I18NString?                                        Description                  = null,
                                   IEnumerable<ChargingTariff_Id>?                    ChargingTariffIds            = null,

                                   Timestamped<ChargingTariffGroupAdminStatusType>?   InitialAdminStatus           = null,
                                   Timestamped<ChargingTariffGroupStatusType>?        InitialStatus                = null,
                                   UInt16?                                            MaxAdminStatusScheduleSize   = null,
                                   UInt16?                                            MaxStatusScheduleSize        = null,

                                   String?                                            DataSource                   = null,
                                   DateTimeOffset?                                    Created                      = null,
                                   DateTimeOffset?                                    LastChange                   = null,

                                   CustomDataNew?                                     CustomData                   = null,
                                   UserDefinedDictionary?                             InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingTariffGroupAdminStatusType>(Created ?? Timestamp.Now, ChargingTariffGroupAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingTariffGroupStatusType>     (Created ?? Timestamp.Now, ChargingTariffGroupStatusType.Active),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.ChargingTariffIds  = POIJSON.OrderedIds(ChargingTariffIds, $"member of the charging tariff group '{Id}'");

        }

        #endregion

        #region Status events

        private readonly Lock                                              statusEventsLock  = new ();
        private          OnChargingTariffGroupAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingTariffGroupStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingTariffGroupAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingTariffGroupStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                    Timestamp,
                                                                   EventTracking_Id                                  EventTrackingId,
                                                                   Timestamped<ChargingTariffGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<ChargingTariffGroupAdminStatusType>   OldStatus,
                                                                   Context?                                          DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                               Timestamp,
                                                              EventTracking_Id                             EventTrackingId,
                                                              Timestamped<ChargingTariffGroupStatusType>   NewStatus,
                                                              Timestamped<ChargingTariffGroupStatusType>   OldStatus,
                                                              Context?                                     DataSource)
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

        #region ToJSON(Embedded = false, CustomChargingTariffGroupSerializer = null)

        /// <summary>
        /// Return the JSON representation of this charging tariff group, as its charging station operator embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomChargingTariffGroupSerializer">A delegate to serialize custom charging tariff groups.</param>
        public JObject ToJSON(Boolean                                                Embedded                              = false,
                              Boolean                                                IncludeETags                          = false,
                              ETagDigestEncoding?                                    ETagDigestEncoding                    = null,
                              CustomJObjectSerializerDelegate<ChargingTariffGroup>?  CustomChargingTariffGroupSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,          JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,             Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,      Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,       DataSource);

            if (ChargingTariffIds.Length > 0)
                json.Add(PropertyKeys.ChargingTariffIds,    POIJSON.IdsToJSON(ChargingTariffIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomChargingTariffGroupSerializer is not null
                       ? CustomChargingTariffGroupSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingTariffGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging tariff group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingTariffGroup">The parsed charging tariff group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                         JSON,
                                       [NotNullWhen(true)]  out ChargingTariffGroup?   ChargingTariffGroup,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            ChargingTariffGroup = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "charging tariff group identification",
                                         ChargingTariffGroup_Id.TryParse,
                                         out ChargingTariffGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging tariff group '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Name,                "charging tariff group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Description,         "charging tariff group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource,          "data source",                        out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, PropertyKeys.    ChargingTariffIds,   "member identification", ChargingTariff_Id.TryParse, out ImmutableArray<ChargingTariff_Id> members, out ErrorResponse))
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

                ChargingTariffGroup = new ChargingTariffGroup(id,
                                                              name,
                                                              description,
                                                              members,
                                                              DataSource:  dataSource,
                                                              Created:     created,
                                                              LastChange:  lastChange,
                                                              CustomData:  customData);

                if (!TryCheckETagsOf(JSON, ChargingTariffGroup, out ErrorResponse))
                {
                    ChargingTariffGroup = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingTariffGroup  = null;
                ErrorResponse        = "The given JSON representation of a charging tariff group is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging tariff groups for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingTariffGroup? ChargingTariffGroup1, ChargingTariffGroup? ChargingTariffGroup2)

            => ReferenceEquals(ChargingTariffGroup1, ChargingTariffGroup2) ||
               (ChargingTariffGroup1 is not null && ChargingTariffGroup1.Equals(ChargingTariffGroup2));

        /// <summary>
        /// Compares two charging tariff groups for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingTariffGroup? ChargingTariffGroup1, ChargingTariffGroup? ChargingTariffGroup2)

            => !(ChargingTariffGroup1 == ChargingTariffGroup2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingTariffGroup chargingTariffGroup
                   ? CompareTo(chargingTariffGroup)
                   : throw new ArgumentException("The given object is not a charging tariff group!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingTariffGroup? ChargingTariffGroup)

            => ChargingTariffGroup is null
                   ? throw new ArgumentNullException(nameof(ChargingTariffGroup), "The given charging tariff group must not be null!")
                   : Id.CompareTo(ChargingTariffGroup.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingTariffGroup chargingTariffGroup &&
               Equals(chargingTariffGroup);

        /// <inheritdoc/>
        public Boolean Equals(ChargingTariffGroup? ChargingTariffGroup)

            => ChargingTariffGroup is not null &&
               Id.Equals(ChargingTariffGroup.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
