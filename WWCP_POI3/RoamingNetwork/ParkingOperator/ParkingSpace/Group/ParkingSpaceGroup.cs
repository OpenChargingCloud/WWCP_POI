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
    /// The admin status of a parking space group changed.
    /// </summary>
    public delegate Task OnParkingSpaceGroupAdminStatusChangedDelegate(DateTimeOffset                                  Timestamp,
                                                                       EventTracking_Id                                EventTrackingId,
                                                                       ParkingSpaceGroup                               ParkingSpaceGroup,
                                                                       Timestamped<ParkingSpaceGroupAdminStatusType>   OldStatus,
                                                                       Timestamped<ParkingSpaceGroupAdminStatusType>   NewStatus,
                                                                       Context?                                        DataSource);

    /// <summary>
    /// The status of a parking space group changed.
    /// </summary>
    public delegate Task OnParkingSpaceGroupStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  ParkingSpaceGroup                          ParkingSpaceGroup,
                                                                  Timestamped<ParkingSpaceGroupStatusType>   OldStatus,
                                                                  Timestamped<ParkingSpaceGroupStatusType>   NewStatus,
                                                                  Context?                                   DataSource);


    /// <summary>
    /// A group of parking spaces of one parking operator, embedded completely in it, e.g. one level or zone of a garage;
    /// groups may overlap. It names its members and gives them nothing: what a parking space is, it has of its own and
    /// of its templates. Its identification is unique within the roaming network.
    /// </summary>
    public sealed class ParkingSpaceGroup : AImmutableEMobilityEntity<ParkingSpaceGroup_Id,
                                                                      ParkingSpaceGroupAdminStatusType,
                                                                      ParkingSpaceGroupStatusType>,
                                            IEquatable<ParkingSpaceGroup>,
                                            IComparable<ParkingSpaceGroup>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/parkingSpaceGroup";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its parking spaces.
            /// </summary>
            public const String  ParkingSpaceIds   = "parkingSpaceIds";

        }

        #endregion


        /// <summary>
        /// Its parking spaces, of the same parking operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ParkingSpace_Id>  ParkingSpaceIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new parking space group.
        /// </summary>
        public ParkingSpaceGroup(ParkingSpaceGroup_Id                             Id,
                                 I18NString?                                      Name                         = null,
                                 I18NString?                                      Description                  = null,
                                 IEnumerable<ParkingSpace_Id>?                    ParkingSpaceIds              = null,

                                 Timestamped<ParkingSpaceGroupAdminStatusType>?   InitialAdminStatus           = null,
                                 Timestamped<ParkingSpaceGroupStatusType>?        InitialStatus                = null,
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
                   InitialAdminStatus         ?? new Timestamped<ParkingSpaceGroupAdminStatusType>(Created ?? Timestamp.Now, ParkingSpaceGroupAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ParkingSpaceGroupStatusType>     (Created ?? Timestamp.Now, ParkingSpaceGroupStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.ParkingSpaceIds  = POIJSON.OrderedIds(ParkingSpaceIds, $"member of the parking space group '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                    statusEventsLock  = new ();
        private          OnParkingSpaceGroupAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnParkingSpaceGroupStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnParkingSpaceGroupAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnParkingSpaceGroupStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                    Timestamp,
                                                                   EventTracking_Id                  EventTrackingId,
                                                                   Timestamped<ParkingSpaceGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<ParkingSpaceGroupAdminStatusType>   OldStatus,
                                                                   Context?                          DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset               Timestamp,
                                                              EventTracking_Id             EventTrackingId,
                                                              Timestamped<ParkingSpaceGroupStatusType>   NewStatus,
                                                              Timestamped<ParkingSpaceGroupStatusType>   OldStatus,
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

        #region ToJSON(Embedded = false, CustomParkingSpaceGroupSerializer = null)

        /// <summary>
        /// Return the JSON representation of this parking space group, as its owner embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomParkingSpaceGroupSerializer">A delegate to serialize custom parking space group JSON objects.</param>
        public JObject ToJSON(Boolean                                              Embedded                            = false,
                              Boolean                                              IncludeETags                        = false,
                              ETagDigestEncoding?                                  ETagDigestEncoding                  = null,
                              CustomJObjectSerializerDelegate<ParkingSpaceGroup>?  CustomParkingSpaceGroupSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,      JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,  Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,   DataSource);

            if (ParkingSpaceIds.Length > 0)
                json.Add(PropertyKeys.ParkingSpaceIds,  POIJSON.IdsToJSON(ParkingSpaceIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomParkingSpaceGroupSerializer is not null
                       ? CustomParkingSpaceGroupSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out ParkingSpaceGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a parking space group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ParkingSpaceGroup">The parsed parking space group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out ParkingSpaceGroup?   ParkingSpaceGroup,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ParkingSpaceGroup = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "parking space group identification",
                                         ParkingSpaceGroup_Id.TryParse,
                                         out ParkingSpaceGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the parking space group '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Name,         "parking space group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Description,  "parking space group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource,   "data source",              out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, PropertyKeys.ParkingSpaceIds,  "member identification", ParkingSpace_Id.TryParse, out ImmutableArray<ParkingSpace_Id> members, out ErrorResponse))
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

                ParkingSpaceGroup = new ParkingSpaceGroup(id,
                                                          name,
                                                          description,
                                                          members,
                                                          DataSource:  dataSource,
                                                          Created:     created,
                                                          LastChange:  lastChange,
                                                          CustomData:  customData);

                if (!TryCheckETagsOf(JSON, ParkingSpaceGroup, out ErrorResponse))
                {
                    ParkingSpaceGroup = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ParkingSpaceGroup  = null;
                ErrorResponse      = "The given JSON representation of a parking space group is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two parking space groups for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ParkingSpaceGroup? ParkingSpaceGroup1, ParkingSpaceGroup? ParkingSpaceGroup2)

            => ReferenceEquals(ParkingSpaceGroup1, ParkingSpaceGroup2) ||
               (ParkingSpaceGroup1 is not null && ParkingSpaceGroup1.Equals(ParkingSpaceGroup2));

        /// <summary>
        /// Compares two parking space groups for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ParkingSpaceGroup? ParkingSpaceGroup1, ParkingSpaceGroup? ParkingSpaceGroup2)

            => !(ParkingSpaceGroup1 == ParkingSpaceGroup2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ParkingSpaceGroup parkingSpaceGroup
                   ? CompareTo(parkingSpaceGroup)
                   : throw new ArgumentException("The given object is not a parking space group!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ParkingSpaceGroup? ParkingSpaceGroup)

            => ParkingSpaceGroup is null
                   ? throw new ArgumentNullException(nameof(ParkingSpaceGroup), "The given parking space group must not be null!")
                   : Id.CompareTo(ParkingSpaceGroup.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ParkingSpaceGroup parkingSpaceGroup &&
               Equals(parkingSpaceGroup);

        /// <inheritdoc/>
        public Boolean Equals(ParkingSpaceGroup? ParkingSpaceGroup)

            => ParkingSpaceGroup is not null &&
               Id.Equals(ParkingSpaceGroup.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
