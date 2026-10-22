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
    /// The admin status of an energy meter group changed.
    /// </summary>
    public delegate Task OnEnergyMeterGroupAdminStatusChangedDelegate(DateTimeOffset                                 Timestamp,
                                                                      EventTracking_Id                               EventTrackingId,
                                                                      EnergyMeterGroup                               EnergyMeterGroup,
                                                                      Timestamped<EnergyMeterGroupAdminStatusType>   OldStatus,
                                                                      Timestamped<EnergyMeterGroupAdminStatusType>   NewStatus,
                                                                      Context?                                       DataSource);

    /// <summary>
    /// The status of an energy meter group changed.
    /// </summary>
    public delegate Task OnEnergyMeterGroupStatusChangedDelegate(DateTimeOffset                            Timestamp,
                                                                 EventTracking_Id                          EventTrackingId,
                                                                 EnergyMeterGroup                          EnergyMeterGroup,
                                                                 Timestamped<EnergyMeterGroupStatusType>   OldStatus,
                                                                 Timestamped<EnergyMeterGroupStatusType>   NewStatus,
                                                                 Context?                                  DataSource);


    /// <summary>
    /// A group of energy meters, embedded completely in its owner; groups may overlap. Its identification is unique
    /// within the roaming network. A charging station operator groups the energy meters of its charging pools, charging
    /// stations and EVSEs, a grid operator those of its grid connection points. It names its members and gives them
    /// nothing: what an energy meter is, it has of its own and of its templates.
    /// </summary>
    public sealed class EnergyMeterGroup : AImmutableEMobilityEntity<EnergyMeterGroup_Id,
                                                                     EnergyMeterGroupAdminStatusType,
                                                                     EnergyMeterGroupStatusType>,
                                           IEquatable<EnergyMeterGroup>,
                                           IComparable<EnergyMeterGroup>
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/energyMeterGroup/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its energy meters.
            /// </summary>
            public const String  EnergyMeterIds   = "energyMeterIds";

        }

        #endregion


        /// <summary>
        /// Its energy meters, of the same owner, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EnergyMeter_Id>  EnergyMeterIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter group.
        /// </summary>
        public EnergyMeterGroup(EnergyMeterGroup_Id                             Id,
                                I18NString?                                     Name                         = null,
                                I18NString?                                     Description                  = null,
                                IEnumerable<EnergyMeter_Id>?                    EnergyMeterIds               = null,

                                Timestamped<EnergyMeterGroupAdminStatusType>?   InitialAdminStatus           = null,
                                Timestamped<EnergyMeterGroupStatusType>?        InitialStatus                = null,
                                UInt16?                                         MaxAdminStatusScheduleSize   = null,
                                UInt16?                                         MaxStatusScheduleSize        = null,

                                String?                                         DataSource                   = null,
                                DateTimeOffset?                                 Created                      = null,
                                DateTimeOffset?                                 LastChange                   = null,

                                CustomDataNew?                                  CustomData                   = null,
                                UserDefinedDictionary?                          InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<EnergyMeterGroupAdminStatusType>(Created ?? Timestamp.Now, EnergyMeterGroupAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<EnergyMeterGroupStatusType>     (Created ?? Timestamp.Now, EnergyMeterGroupStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.EnergyMeterIds  = POIJSON.OrderedIds(EnergyMeterIds, $"member of the energy meter group '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                    statusEventsLock  = new ();
        private          OnEnergyMeterGroupAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnEnergyMeterGroupStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnEnergyMeterGroupAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnEnergyMeterGroupStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                    Timestamp,
                                                                   EventTracking_Id                  EventTrackingId,
                                                                   Timestamped<EnergyMeterGroupAdminStatusType>   NewStatus,
                                                                   Timestamped<EnergyMeterGroupAdminStatusType>   OldStatus,
                                                                   Context?                          DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset               Timestamp,
                                                              EventTracking_Id             EventTrackingId,
                                                              Timestamped<EnergyMeterGroupStatusType>   NewStatus,
                                                              Timestamped<EnergyMeterGroupStatusType>   OldStatus,
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

        #region ToJSON(Embedded = false, CustomEnergyMeterGroupSerializer = null)

        /// <summary>
        /// Return the JSON representation of this energy meter group, as its owner embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomEnergyMeterGroupSerializer">A delegate to serialize custom energy meter group JSON objects.</param>
        public JObject ToJSON(Boolean                                             Embedded                           = false,
                              Boolean                                             IncludeETags                       = false,
                              ETagDigestEncoding?                                 ETagDigestEncoding                 = null,
                              CustomJObjectSerializerDelegate<EnergyMeterGroup>?  CustomEnergyMeterGroupSerializer   = null)
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

            if (EnergyMeterIds.Length > 0)
                json.Add(PropertyKeys.EnergyMeterIds,   POIJSON.IdsToJSON(EnergyMeterIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomEnergyMeterGroupSerializer is not null
                       ? CustomEnergyMeterGroupSerializer(this, json)
                       : json;

        }

        #endregion

        #region (static) TryParse(JSON, out EnergyMeterGroup, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an energy meter group.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyMeterGroup">The parsed energy meter group.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out EnergyMeterGroup?    EnergyMeterGroup,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            EnergyMeterGroup = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "energy meter group identification",
                                         EnergyMeterGroup_Id.TryParse,
                                         out EnergyMeterGroup_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the energy meter group '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Name,         "energy meter group name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Description,  "energy meter group description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource,   "data source",             out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseIds   (JSON, PropertyKeys.EnergyMeterIds,   "member identification", EnergyMeter_Id.TryParse, out ImmutableArray<EnergyMeter_Id> members, out ErrorResponse))
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

                EnergyMeterGroup = new EnergyMeterGroup(id,
                                                        name,
                                                        description,
                                                        members,
                                                        DataSource:  dataSource,
                                                        Created:     created,
                                                        LastChange:  lastChange,
                                                        CustomData:  customData);

                if (!TryCheckETagsOf(JSON, EnergyMeterGroup, out ErrorResponse))
                {
                    EnergyMeterGroup = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EnergyMeterGroup  = null;
                ErrorResponse     = "The given JSON representation of an energy meter group is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two energy meter groups for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EnergyMeterGroup? EnergyMeterGroup1, EnergyMeterGroup? EnergyMeterGroup2)

            => ReferenceEquals(EnergyMeterGroup1, EnergyMeterGroup2) ||
               (EnergyMeterGroup1 is not null && EnergyMeterGroup1.Equals(EnergyMeterGroup2));

        /// <summary>
        /// Compares two energy meter groups for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EnergyMeterGroup? EnergyMeterGroup1, EnergyMeterGroup? EnergyMeterGroup2)

            => !(EnergyMeterGroup1 == EnergyMeterGroup2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is EnergyMeterGroup energyMeterGroup
                   ? CompareTo(energyMeterGroup)
                   : throw new ArgumentException("The given object is not an energy meter group!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EnergyMeterGroup? EnergyMeterGroup)

            => EnergyMeterGroup is null
                   ? throw new ArgumentNullException(nameof(EnergyMeterGroup), "The given energy meter group must not be null!")
                   : Id.CompareTo(EnergyMeterGroup.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeterGroup energyMeterGroup &&
               Equals(energyMeterGroup);

        /// <inheritdoc/>
        public Boolean Equals(EnergyMeterGroup? EnergyMeterGroup)

            => EnergyMeterGroup is not null &&
               Id.Equals(EnergyMeterGroup.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
