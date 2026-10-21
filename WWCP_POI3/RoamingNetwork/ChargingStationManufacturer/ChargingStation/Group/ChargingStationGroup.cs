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
    /// A group of charging stations, embedded completely in its owner; groups may overlap. It names its members and
    /// gives them nothing: what a charging station is, it has of its own and of its templates. Groups are for sets that
    /// do not follow from properties: for a status of their own, for searches, or to name the EVSEs of a charging
    /// tariff offer.
    /// </summary>
    public sealed class ChargingStationGroup : AImmutableEMobilityEntity<ChargingStationGroup_Id,
                                                                         ChargingStationGroupAdminStatusType,
                                                                         ChargingStationGroupStatusType>,
                                               IEquatable<ChargingStationGroup>,
                                               IComparable<ChargingStationGroup>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/chargingStationGroup";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the identifications of its charging stations.
            /// </summary>
            public const String  ChargingStationIds   = "chargingStationIds";

        }

        #endregion


        /// <summary>
        /// Its charging stations, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingStation_Id>  ChargingStationIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station group.
        /// </summary>
        public ChargingStationGroup(ChargingStationGroup_Id                             Id,
                                    I18NString?                                         Name                         = null,
                                    I18NString?                                         Description                  = null,
                                    IEnumerable<ChargingStation_Id>?                    ChargingStationIds           = null,

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

        }

        #endregion


        #region Status events

        private readonly Lock                                    statusEventsLock  = new ();
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
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                    Timestamp,
                                                                   EventTracking_Id                  EventTrackingId,
                                                                   Timestamped<ChargingStationGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<ChargingStationGroupAdminStatusType>   OldStatus,
                                                                   Context?                          DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset               Timestamp,
                                                              EventTracking_Id             EventTrackingId,
                                                              Timestamped<ChargingStationGroupStatusType>   NewStatus,
                                                              Timestamped<ChargingStationGroupStatusType>   OldStatus,
                                                              Context?                     DataSource)
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

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this charging station group; embedded, as its owner embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomChargingStationGroupSerializer">A delegate to serialize custom charging station group JSON objects.</param>
        public JObject ToJSON(Boolean                                                 Embedded                               = false,
                              Boolean                                                 IncludeETags                           = false,
                              ETagDigestEncoding?                                     ETagDigestEncoding                     = null,
                              CustomJObjectSerializerDelegate<ChargingStationGroup>?  CustomChargingStationGroupSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id,  Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,           JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,              Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,       Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,        DataSource);

            if (ChargingStationIds.Length > 0)
                json.Add(PropertyKeys.ChargingStationIds,    POIJSON.IdsToJSON(ChargingStationIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomChargingStationGroupSerializer is not null
                       ? CustomChargingStationGroupSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this charging station group: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="CustomChargingStationGroupSerializer">A delegate to serialize custom charging station group CBOR maps.</param>
        public CBORValue ToCBOR(Boolean                                               Embedded                               = false,
                                Boolean                                               IncludeETags                           = false,
                                CustomCBORSerializerDelegate<ChargingStationGroup>?   CustomChargingStationGroupSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),           CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),          CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),             Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),      Description.ToCBOR());

            if (DataSource is not null)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),       CBORValue.FromText(DataSource));

            if (ChargingStationIds.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ChargingStationIds),   CBORValue.FromArray(ChargingStationIds.Select(chargingStationId => CBORValue.FromText(chargingStationId.ToString()))));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),              CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),           CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),       CBORJSON.ToCBOR(CustomData.ToJObject()));

            AddETagsTo(cbor, IncludeETags);

            return CustomChargingStationGroupSerializer is not null
                       ? CustomChargingStationGroupSerializer(this, cbor)
                       : cbor;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingStationGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStationGroup">The parsed charging station group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out ChargingStationGroup? ChargingStationGroup,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ChargingStationGroup = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "charging station group identification",
                                         ChargingStationGroup_Id.TryParse,
                                         out ChargingStationGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station group '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Name,         "charging station group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Description,  "charging station group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource,   "data source",                 out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, PropertyKeys.ChargingStationIds, "member identification", ChargingStation_Id.TryParse, out ImmutableArray<ChargingStation_Id> members, out ErrorResponse))
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
                                                                DataSource:  dataSource,
                                                                Created:     created,
                                                                LastChange:  lastChange,
                                                                CustomData:  customData);

                if (!TryCheckETagsOf(JSON, ChargingStationGroup, out ErrorResponse))
                {
                    ChargingStationGroup = null;
                    return false;
                }

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

        #region (static) TryParse(CBOR, out ChargingStationGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a charging station group: the keys and values of its JSON
        /// representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="ChargingStationGroup">The parsed charging station group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                       CBOR,
                                       [NotNullWhen(true)]  out ChargingStationGroup?  ChargingStationGroup,
                                       [NotNullWhen(false)] out String?                ErrorResponse)
        {

            ChargingStationGroup = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a charging station group must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "charging station group identification",
                                         ChargingStationGroup_Id.TryParse,
                                         out ChargingStationGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station group '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptional    (BasePropertyKeys.Name,         "charging station group name",         I18NString.TryParse, out I18NString? name,         out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptional    (BasePropertyKeys.Description,  "charging station group description",  I18NString.TryParse, out I18NString? description,  out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource,   "data source",                                              out var         dataSource,   out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                // What has no CBOR representation of its own is read as its JSON, with the parsers of the JSON
                // representation: the identifications of its members and the metadata.
                var json = new JObject();

                foreach (var key in new[] { PropertyKeys.ChargingStationIds,
                                            BasePropertyKeys.Created,
                                            BasePropertyKeys.LastChange,
                                            BasePropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                if (!POIJSON.TryParseIds(json, PropertyKeys.ChargingStationIds, "member identification", ChargingStation_Id.TryParse, out ImmutableArray<ChargingStation_Id> members, out ErrorResponse))
                    return false;

                if (!POIEntityMetadata.TryParse(json,
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
                                                                DataSource:  dataSource,
                                                                Created:     created,
                                                                LastChange:  lastChange,
                                                                CustomData:  customData);

                if (!TryCheckETagsOf(CBOR, ChargingStationGroup, out ErrorResponse))
                {
                    ChargingStationGroup = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationGroup  = null;
                ErrorResponse         = "The given CBOR representation of a charging station group is invalid: " + e.Message;
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
