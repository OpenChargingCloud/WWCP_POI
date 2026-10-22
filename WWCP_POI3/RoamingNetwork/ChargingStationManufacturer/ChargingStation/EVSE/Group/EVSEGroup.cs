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
    /// A group of EVSEs, embedded completely in its owner; groups may overlap. It names its members and gives them
    /// nothing: what an EVSE is, it has of its own and of its templates. Groups are for sets that do not follow from
    /// properties: for a status of their own, for searches, or to name the EVSEs of a charging tariff offer.
    /// </summary>
    public sealed class EVSEGroup : AImmutableEMobilityEntity<EVSEGroup_Id,
                                                              EVSEGroupAdminStatusType,
                                                              EVSEGroupStatusType>,
                                    IEquatable<EVSEGroup>,
                                    IComparable<EVSEGroup>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/evseGroup/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the identifications of its EVSEs.
            /// </summary>
            public const String  EVSEIds   = "EVSEIds";

        }

        #endregion


        /// <summary>
        /// Its EVSEs, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EVSE_Id>  EVSEIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVSE group.
        /// </summary>
        public EVSEGroup(EVSEGroup_Id                             Id,
                         I18NString?                              Name                         = null,
                         I18NString?                              Description                  = null,
                         IEnumerable<EVSE_Id>?                    EVSEIds                      = null,

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

            this.EVSEIds  = POIJSON.OrderedIds(EVSEIds, $"member of the EVSE group '{Id}'");

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
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                    Timestamp,
                                                                   EventTracking_Id                  EventTrackingId,
                                                                   Timestamped<EVSEGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<EVSEGroupAdminStatusType>   OldStatus,
                                                                   Context?                          DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset               Timestamp,
                                                              EventTracking_Id             EventTrackingId,
                                                              Timestamped<EVSEGroupStatusType>   NewStatus,
                                                              Timestamped<EVSEGroupStatusType>   OldStatus,
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
        /// Return the JSON representation of this EVSE group; embedded, as its owner embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomEVSEGroupSerializer">A delegate to serialize custom EVSE group JSON objects.</param>
        public JObject ToJSON(Boolean                                      Embedded                    = false,
                              Boolean                                      IncludeETags                = false,
                              ETagDigestEncoding?                          ETagDigestEncoding          = null,
                              CustomJObjectSerializerDelegate<EVSEGroup>?  CustomEVSEGroupSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id,  Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,      JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,  Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,   DataSource);

            if (EVSEIds.Length > 0)
                json.Add(PropertyKeys.EVSEIds,          POIJSON.IdsToJSON(EVSEIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomEVSEGroupSerializer is not null
                       ? CustomEVSEGroupSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this EVSE group: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="CustomEVSEGroupSerializer">A delegate to serialize custom EVSE group CBOR maps.</param>
        public CBORValue ToCBOR(Boolean                                    Embedded                    = false,
                                Boolean                                    IncludeETags                = false,
                                CustomCBORSerializerDelegate<EVSEGroup>?   CustomEVSEGroupSerializer   = null)
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

            if (EVSEIds.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.EVSEIds),              CBORValue.FromArray(EVSEIds.Select(evseId => CBORValue.FromText(evseId.ToString()))));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),              CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),           CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),       CBORJSON.ToCBOR(CustomData.ToJObject()));

            AddETagsTo(cbor, IncludeETags);

            return CustomEVSEGroupSerializer is not null
                       ? CustomEVSEGroupSerializer(this, cbor)
                       : cbor;

        }

        #endregion

        #region (static) TryParse(JSON, out EVSEGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an EVSE group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EVSEGroup">The parsed EVSE group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out EVSEGroup?           EVSEGroup,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            EVSEGroup = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "EVSE group identification",
                                         EVSEGroup_Id.TryParse,
                                         out EVSEGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the EVSE group '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Name,         "EVSE group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Description,  "EVSE group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource,   "data source",     out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, PropertyKeys.EVSEIds, "member identification", EVSE_Id.TryParse, out ImmutableArray<EVSE_Id> members, out ErrorResponse))
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
                                          DataSource:  dataSource,
                                          Created:     created,
                                          LastChange:  lastChange,
                                          CustomData:  customData);

                if (!TryCheckETagsOf(JSON, EVSEGroup, out ErrorResponse))
                {
                    EVSEGroup = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EVSEGroup  = null;
                ErrorResponse  = "The given JSON representation of an EVSE group is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out EVSEGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of an EVSE group: the keys and values of its JSON
        /// representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="EVSEGroup">The parsed EVSE group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                         CBOR,
                                       [NotNullWhen(true)]  out EVSEGroup?  EVSEGroup,
                                       [NotNullWhen(false)] out String?     ErrorResponse)
        {

            EVSEGroup = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of an EVSE group must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "EVSE group identification",
                                         EVSEGroup_Id.TryParse,
                                         out EVSEGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the EVSE group '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptional    (BasePropertyKeys.Name,         "EVSE group name",         I18NString.TryParse, out I18NString? name,         out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptional    (BasePropertyKeys.Description,  "EVSE group description",  I18NString.TryParse, out I18NString? description,  out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource,   "data source",                                  out var         dataSource,   out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                // What has no CBOR representation of its own is read as its JSON, with the parsers of the JSON
                // representation: the identifications of its members and the metadata.
                var json = new JObject();

                foreach (var key in new[] { PropertyKeys.EVSEIds,
                                            BasePropertyKeys.Created,
                                            BasePropertyKeys.LastChange,
                                            BasePropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                if (!POIJSON.TryParseIds(json, PropertyKeys.EVSEIds, "member identification", EVSE_Id.TryParse, out ImmutableArray<EVSE_Id> members, out ErrorResponse))
                    return false;

                if (!POIEntityMetadata.TryParse(json,
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
                                          DataSource:  dataSource,
                                          Created:     created,
                                          LastChange:  lastChange,
                                          CustomData:  customData);

                if (!TryCheckETagsOf(CBOR, EVSEGroup, out ErrorResponse))
                {
                    EVSEGroup = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EVSEGroup      = null;
                ErrorResponse  = "The given CBOR representation of an EVSE group is invalid: " + e.Message;
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
