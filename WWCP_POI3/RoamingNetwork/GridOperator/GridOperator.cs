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
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of a grid operator changed.
    /// </summary>
    public delegate Task OnGridOperatorAdminStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  GridOperator                               GridOperator,
                                                                  Timestamped<GridOperatorAdminStatusType>   OldStatus,
                                                                  Timestamped<GridOperatorAdminStatusType>   NewStatus,
                                                                  Context?                                   DataSource);

    /// <summary>
    /// The status of a grid operator changed.
    /// </summary>
    public delegate Task OnGridOperatorStatusChangedDelegate(DateTimeOffset                        Timestamp,
                                                             EventTracking_Id                      EventTrackingId,
                                                             GridOperator                          GridOperator,
                                                             Timestamped<GridOperatorStatusType>   OldStatus,
                                                             Timestamped<GridOperatorStatusType>   NewStatus,
                                                             Context?                              DataSource);


    /// <summary>
    /// An operator of an electricity grid, embedded completely in its roaming network,
    /// with its grid connection points. A grid operator without keys does not take part:
    /// the roaming network operator maintains it, and charging station operators or parking
    /// operators may maintain its grid connection points.
    /// </summary>
    public sealed class GridOperator : AImmutableEMobilityEntity<GridOperator_Id,
                                                                 GridOperatorAdminStatusType,
                                                                 GridOperatorStatusType>,
                                       IEquatable<GridOperator>,
                                       IComparable<GridOperator>,
                                       IPOIParty
    {

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address                  = "address";

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage                 = "homepage";

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber       = "hotline";

            /// <summary>
            /// The property of its grid connection points.
            /// </summary>
            public const String  GridConnectionPoints     = "gridConnectionPoints";

            /// <summary>
            /// The property of the roots of its grid connection points, in the skeleton of its tree.
            /// </summary>
            public const String  GridConnectionPointIds   = "gridConnectionPointIds";

            /// <summary>
            /// The property of its energy meter templates.
            /// </summary>
            public const String  EnergyMeterTemplates     = "energyMeterTemplates";

            /// <summary>
            /// The property of the roots of its energy meter templates, in the skeleton of its tree.
            /// </summary>
            public const String  EnergyMeterTemplateIds   = "energyMeterTemplateIds";

            /// <summary>
            /// The property of its energy meter groups.
            /// </summary>
            public const String  EnergyMeterGroups        = "energyMeterGroups";

            /// <summary>
            /// The property of the roots of its energy meter groups, in the skeleton of its tree.
            /// </summary>
            public const String  EnergyMeterGroupIds      = "energyMeterGroupIds";

            /// <summary>
            /// The property of its signing keys, as of every party.
            /// </summary>
            public const String  SigningKeys              = POIPartyKeys.SigningKeysProperty;

            /// <summary>
            /// The property of its identity keys, as of every party.
            /// </summary>
            public const String  IdentityKeys             = POIPartyKeys.IdentityKeysProperty;

        }

        #endregion


        /// <summary>
        /// The address of the headquarters.
        /// </summary>
        public Address?      Address               { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?          Homepage              { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?  HotlinePhoneNumber    { get; }

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>  SigningKeys     { get; }

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>  IdentityKeys    { get; }

        /// <summary>
        /// The grid connection points, in the order of their identifications.
        /// </summary>
        public POICollection<GridConnectionPoint_Id, GridConnectionPoint>  GridConnectionPoints    { get; }

        /// <summary>
        /// The energy meters of its grid connection points.
        /// </summary>
        public IEnumerable<EnergyMeter>  EnergyMeters
            => GridConnectionPoints.Select(gridConnectionPoint => gridConnectionPoint.EnergyMeter).OfType<EnergyMeter>();

        /// <summary>
        /// The templates of the energy meters of its grid connection points, embedded completely, in the order of their
        /// identifications.
        /// </summary>
        public POICollection<EnergyMeterTemplate_Id, EnergyMeterTemplate>  EnergyMeterTemplates    { get; }

        /// <summary>
        /// The groups of the energy meters of its grid connection points, embedded completely, in the order of their
        /// identifications.
        /// </summary>
        public POICollection<EnergyMeterGroup_Id, EnergyMeterGroup>        EnergyMeterGroups       { get; }

        /// <summary>
        /// The grid connection points as they are at runtime, with their energy meters taking the properties of their
        /// templates, in the order of their identifications: a grid connection point whose energy meter names no templates
        /// is itself.
        /// </summary>
        public POICollection<GridConnectionPoint_Id, GridConnectionPoint>  EffectiveGridConnectionPoints
            => effectiveGridConnectionPoints.Value;

        #endregion

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/gridOperator/v1";

        /// <summary>
        /// The effective grid connection points differing from the grid connection points, by their identifications.
        /// </summary>
        private readonly ImmutableDictionary<GridConnectionPoint_Id, GridConnectionPoint>        effectiveGridConnectionPointIndex;

        /// <summary>
        /// The effective grid connection points, calculated when asked for.
        /// </summary>
        private readonly Lazy<POICollection<GridConnectionPoint_Id, GridConnectionPoint>>       effectiveGridConnectionPoints;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new grid operator.
        /// </summary>
        public GridOperator(GridOperator_Id                               Id,
                            I18NString?                                   Name                         = null,
                            I18NString?                                   Description                  = null,
                            Address?                                      Address                      = null,
                            URL?                                          Homepage                     = null,
                            PhoneNumber?                                  HotlinePhoneNumber           = null,
                            IEnumerable<GridConnectionPoint>?             GridConnectionPoints         = null,
                            IEnumerable<POIPublicKey>?                    SigningKeys                  = null,
                            IEnumerable<POIPublicKey>?                    IdentityKeys                 = null,
                            IEnumerable<EnergyMeterGroup>?                EnergyMeterGroups            = null,
                            IEnumerable<EnergyMeterTemplate>?             EnergyMeterTemplates         = null,

                            Timestamped<GridOperatorAdminStatusType>?     InitialAdminStatus           = null,
                            Timestamped<GridOperatorStatusType>?          InitialStatus                = null,
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
                   InitialAdminStatus         ?? new Timestamped<GridOperatorAdminStatusType>(Created ?? Timestamp.Now, GridOperatorAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<GridOperatorStatusType>     (Created ?? Timestamp.Now, GridOperatorStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.Address             = Address;
            this.Homepage            = Homepage;
            this.HotlinePhoneNumber  = HotlinePhoneNumber;

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the grid operator '{Id}'");

            // Grid connection point identifications compare without case.
            this.GridConnectionPoints = new (GridConnectionPoints, gridConnectionPoint => gridConnectionPoint.Id, "grid connection point", $"the grid operator '{Id}'");

            #region Energy meter groups, of the energy meters of its grid connection points

            var energyMeterGroups = (EnergyMeterGroups ?? []).ToArray();

            if (energyMeterGroups.Any(group => group is null))
                throw new ArgumentException("Groups must not contain null entries!", nameof(EnergyMeterGroups));

            this.EnergyMeterGroups = new (energyMeterGroups, group => group.Id, "energy meter group", $"the grid operator '{Id}'");

            var energyMeterIds = this.EnergyMeters.Select(energyMeter => energyMeter.Id).ToHashSet();

            foreach (var energyMeterGroup in this.EnergyMeterGroups)
                foreach (var memberId in energyMeterGroup.EnergyMeterIds)
                    if (!energyMeterIds.Contains(memberId))
                        throw new ArgumentException($"The member '{memberId}' of the group '{energyMeterGroup.Id}' is unknown at the grid operator '{Id}'!", nameof(EnergyMeterGroups));

            #endregion

            #region Energy meter templates, of the energy meters of its grid connection points

            this.EnergyMeterTemplates = new (EnergyMeterTemplates, template => template.Id, "energy meter template", $"the grid operator '{Id}'");

            foreach (var energyMeter in this.EnergyMeters)
                foreach (var templateId in energyMeter.TemplateIds)
                    if (!this.EnergyMeterTemplates.Contains(templateId))
                        throw new ArgumentException($"The template '{templateId}' of the energy meter '{energyMeter.Id}' is unknown at the grid operator '{Id}'!", nameof(EnergyMeterTemplates));

            #endregion

            #region Effective grid connection points, with their energy meters taking the properties of their templates

            this.effectiveGridConnectionPointIndex  = CalculateEffective();
            this.effectiveGridConnectionPoints      = new (() => effectiveGridConnectionPointIndex.IsEmpty
                                                                    ? this.GridConnectionPoints
                                                                    : new (this.GridConnectionPoints.Select(EffectiveGridConnectionPoint), gridConnectionPoint => gridConnectionPoint.Id, "grid connection point", $"the grid operator '{Id}'"));

            #endregion

        }

        #endregion


        #region TryGetGridConnectionPoint(GridConnectionPointId, out GridConnectionPoint)

        /// <summary>
        /// Try to return the grid connection point with the given identification.
        /// </summary>
        public Boolean TryGetGridConnectionPoint(GridConnectionPoint_Id                         GridConnectionPointId,
                                                 [NotNullWhen(true)] out GridConnectionPoint?   GridConnectionPoint)

            => GridConnectionPoints.TryGet(GridConnectionPointId, out GridConnectionPoint);

        #endregion

        #region EffectiveGridConnectionPoint(GridConnectionPoint)

        /// <summary>
        /// The given grid connection point of this grid operator as it is at runtime, with its energy meter taking the
        /// properties of its templates: the grid connection point itself, when it names none.
        /// </summary>
        /// <param name="GridConnectionPoint">A grid connection point of this grid operator.</param>
        public GridConnectionPoint EffectiveGridConnectionPoint(GridConnectionPoint GridConnectionPoint)

            => effectiveGridConnectionPointIndex.TryGetValue(GridConnectionPoint.Id, out var effectiveGridConnectionPoint)
                   ? effectiveGridConnectionPoint
                   : GridConnectionPoint;

        #endregion

        #region TryGetEffectiveGridConnectionPoint(GridConnectionPointId, out GridConnectionPoint)

        /// <summary>
        /// Try to return the grid connection point with the given identification as it is at runtime.
        /// </summary>
        public Boolean TryGetEffectiveGridConnectionPoint(GridConnectionPoint_Id                        GridConnectionPointId,
                                                          [NotNullWhen(true)] out GridConnectionPoint?  GridConnectionPoint)
        {

            if (!TryGetGridConnectionPoint(GridConnectionPointId, out GridConnectionPoint))
                return false;

            GridConnectionPoint = EffectiveGridConnectionPoint(GridConnectionPoint);
            return true;

        }

        #endregion

        #region TryGetEnergyMeterTemplate(EnergyMeterTemplateId, out EnergyMeterTemplate)

        /// <summary>
        /// Try to return the energy meter template with the given identification.
        /// </summary>
        public Boolean TryGetEnergyMeterTemplate(EnergyMeterTemplate_Id                        EnergyMeterTemplateId,
                                                 [NotNullWhen(true)] out EnergyMeterTemplate?  EnergyMeterTemplate)

            => EnergyMeterTemplates.TryGet(EnergyMeterTemplateId, out EnergyMeterTemplate);

        #endregion

        #region TemplatesOf(EnergyMeter)

        /// <summary>
        /// The templates the given energy meter of one of its grid connection points names, in its order.
        /// </summary>
        public IEnumerable<EnergyMeterTemplate> TemplatesOf(EnergyMeter EnergyMeter)

            => ChargingStationOperator.TemplatesOf(EnergyMeter.TemplateIds, EnergyMeterTemplates);

        #endregion

        #region TryGetEnergyMeterGroup(EnergyMeterGroupId, out EnergyMeterGroup)

        /// <summary>
        /// Try to return the energy meter group with the given identification.
        /// </summary>
        public Boolean TryGetEnergyMeterGroup(EnergyMeterGroup_Id                        EnergyMeterGroupId,
                                              [NotNullWhen(true)] out EnergyMeterGroup?  EnergyMeterGroup)

            => EnergyMeterGroups.TryGet(EnergyMeterGroupId, out EnergyMeterGroup);

        #endregion

        #region EnergyMeterGroupsOf(EnergyMeterId)

        /// <summary>
        /// The groups the given energy meter is a member of, in the order of their identifications.
        /// </summary>
        public IEnumerable<EnergyMeterGroup> EnergyMeterGroupsOf(EnergyMeter_Id EnergyMeterId)

            => EnergyMeterGroups.Where(energyMeterGroup => energyMeterGroup.EnergyMeterIds.Contains(EnergyMeterId));

        #endregion

        #region (internal) ShareRuntimeOfAll(Previous)

        /// <summary>
        /// Take the runtime status and internal data of the given previous version of this grid operator,
        /// and of the energy meters of its grid connection points and its energy meter groups, by their identifications.
        /// </summary>
        internal void ShareRuntimeOfAll(GridOperator Previous)
        {

            ShareRuntimeOf(Previous);

            var previousMeters = Previous.EnergyMeters.ToDictionary(energyMeter => energyMeter.Id);

            foreach (var energyMeter in EnergyMeters)
                if (previousMeters.TryGetValue(energyMeter.Id, out var previousMeter))
                    energyMeter.ShareRuntimeOf(previousMeter);

            foreach (var energyMeterGroup in EnergyMeterGroups)
                if (Previous.TryGetEnergyMeterGroup(energyMeterGroup.Id, out var previousEnergyMeterGroup))
                    energyMeterGroup.ShareRuntimeOf(previousEnergyMeterGroup);

            // The effective energy meters take the runtime status of the energy meters again.
            foreach (var (gridConnectionPointId, effectiveGridConnectionPoint) in effectiveGridConnectionPointIndex)
                if (TryGetGridConnectionPoint(gridConnectionPointId, out var gridConnectionPoint) &&
                    gridConnectionPoint.         EnergyMeter is EnergyMeter energyMeter &&
                    effectiveGridConnectionPoint.EnergyMeter is EnergyMeter effectiveEnergyMeter)
                {
                    effectiveEnergyMeter.ShareRuntimeOf(energyMeter);
                }

        }

        #endregion

        #region (private) CalculateEffective()

        /// <summary>
        /// The effective grid connection points differing from the grid connection points, by their identifications:
        /// those whose energy meter names templates. Grid connection points have no timestamps; the energy meter has the
        /// latest last change of it and its templates.
        /// </summary>
        private ImmutableDictionary<GridConnectionPoint_Id, GridConnectionPoint> CalculateEffective()
        {

            var effective = ImmutableDictionary.CreateBuilder<GridConnectionPoint_Id, GridConnectionPoint>();

            foreach (var gridConnectionPoint in GridConnectionPoints)
                if (gridConnectionPoint.EnergyMeter is EnergyMeter energyMeter &&
                    energyMeter.TemplateIds.Length > 0)
                {
                    effective.Add(gridConnectionPoint.Id,
                                  gridConnectionPoint.WithEnergyMeter(EnergyMeterTemplate.Effective(energyMeter,
                                                                                                    ChargingStationOperator.Values(energyMeter.TemplateIds, EnergyMeterTemplates))));
                }

            return effective.ToImmutable();

        }

        #endregion


        #region Status events

        private readonly Lock                                       statusEventsLock  = new ();
        private          OnGridOperatorAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnGridOperatorStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnGridOperatorAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnGridOperatorStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                            Timestamp,
                                                                   EventTracking_Id                          EventTrackingId,
                                                                   Timestamped<GridOperatorAdminStatusType>  NewStatus,
                                                                   Timestamped<GridOperatorAdminStatusType>  OldStatus,
                                                                   Context?                                  DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                       Timestamp,
                                                              EventTracking_Id                     EventTrackingId,
                                                              Timestamped<GridOperatorStatusType>  NewStatus,
                                                              Timestamped<GridOperatorStatusType>  OldStatus,
                                                              Context?                             DataSource)
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

        #region ToJSON(Embedded = false, CustomGridOperatorSerializer = null)

        /// <summary>
        /// Return the JSON representation of this grid operator, as its roaming network embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomGridOperatorSerializer">A delegate to serialize custom grid operator JSON objects.</param>
        public JObject ToJSON(Boolean                                         Embedded                       = false,
                              Boolean                                         IncludeETags                   = false,
                              ETagDigestEncoding?                             ETagDigestEncoding             = null,
                              CustomJObjectSerializerDelegate<GridOperator>?  CustomGridOperatorSerializer   = null)

            => ToJSON(Embedded,
                      IncludeETags,
                      ETagDigestEncoding,
                      CustomGridOperatorSerializer,
                      IncludeChildArrays: true);

        /// <summary>
        /// The JSON representation of this grid operator, as its roaming network embeds it, with or without its child arrays:
        /// without them for the skeleton of its tree (SPECIFICATION.md, section 8.2), which names their roots in their place.
        /// </summary>
        /// <param name="IncludeChildArrays">Whether to include its child arrays.</param>
        internal JObject ToJSON(Boolean IncludeChildArrays)

            => ToJSON(Embedded:                      true,
                      IncludeETags:                  false,
                      ETagDigestEncoding:            null,
                      CustomGridOperatorSerializer:  null,
                      IncludeChildArrays:            IncludeChildArrays);

        /// <summary>
        /// The JSON representation of this grid operator, with or without its child arrays.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomGridOperatorSerializer">A delegate to serialize custom grid operator JSON objects.</param>
        /// <param name="IncludeChildArrays">Whether to include its child arrays.</param>
        private JObject ToJSON(Boolean                                         Embedded,
                               Boolean                                         IncludeETags,
                               ETagDigestEncoding?                             ETagDigestEncoding,
                               CustomJObjectSerializerDelegate<GridOperator>?  CustomGridOperatorSerializer,
                               Boolean                                         IncludeChildArrays)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,             JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,                Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,         Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,          DataSource);

            if (Address is not null)
                json.Add(PropertyKeys.Address,                 Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add(PropertyKeys.Homepage,                Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,      HotlinePhoneNumber.Value.ToString());

            if (IncludeChildArrays && GridConnectionPoints.Count > 0)
                json.Add(PropertyKeys.GridConnectionPoints,    new JArray(GridConnectionPoints.Select(gridConnectionPoint => gridConnectionPoint.ToJSON(Embedded: true))));

            if (IncludeChildArrays && EnergyMeterTemplates.Count > 0)
                json.Add(PropertyKeys.EnergyMeterTemplates,    new JArray(EnergyMeterTemplates.Select(energyMeterTemplate => energyMeterTemplate.ToJSON(Embedded: true))));

            if (IncludeChildArrays && EnergyMeterGroups.Count > 0)
                json.Add(PropertyKeys.EnergyMeterGroups,       new JArray(EnergyMeterGroups.   Select(energyMeterGroup    => energyMeterGroup.   ToJSON(Embedded: true))));

            POIPartyKeys.AddTo(json, this);

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomGridOperatorSerializer is not null
                       ? CustomGridOperatorSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomGridOperatorSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this grid operator: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="CustomGridOperatorSerializer">A delegate to serialize custom grid operator CBOR values.</param>
        public CBORValue ToCBOR(Boolean                                       Embedded                       = false,
                                Boolean                                       IncludeETags                   = false,
                                CustomCBORSerializerDelegate<GridOperator>?   CustomGridOperatorSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),                CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),           CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),              Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),       Description.ToCBOR());

            if (DataSource is not null)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),        CBORValue.FromText(DataSource));

            // The address, the grid connection points, the energy meter templates and groups and the keys have no CBOR
            // representation of their own: theirs are the keys and values of their JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),               CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),              CBORValue.FromText(Homepage.Value.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),    CBORValue.FromText(HotlinePhoneNumber.Value.ToString()));

            if (GridConnectionPoints.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.GridConnectionPoints),
                         CBORValue.FromArray(GridConnectionPoints.Select(gridConnectionPoint => CBORJSON.ToCBOR(gridConnectionPoint.ToJSON(Embedded: true)))));

            if (EnergyMeterTemplates.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.EnergyMeterTemplates),
                         CBORValue.FromArray(EnergyMeterTemplates.Select(energyMeterTemplate => CBORJSON.ToCBOR(energyMeterTemplate.ToJSON(Embedded: true)))));

            if (EnergyMeterGroups.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.EnergyMeterGroups),
                         CBORValue.FromArray(EnergyMeterGroups.   Select(energyMeterGroup    => CBORJSON.ToCBOR(energyMeterGroup.   ToJSON(Embedded: true)))));

            foreach (var keys in POIPartyKeys.AddTo(new JObject(), this).Properties())
                cbor.Add(CBORValue.FromText(keys.Name),                          CBORJSON.ToCBOR(keys.Value));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),               CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),            CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),        CBORJSON.ToCBOR(CustomData.ToJObject()));

            AddETagsTo(cbor, IncludeETags);

            return CustomGridOperatorSerializer is not null
                       ? CustomGridOperatorSerializer(this, cbor)
                       : cbor;

        }

        #endregion

        #region (static) TryParse(JSON, out GridOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a grid operator.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="GridOperator">The parsed grid operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                 JSON,
                                       [NotNullWhen(true)]  out GridOperator?  GridOperator,
                                       [NotNullWhen(false)] out String?        ErrorResponse)
        {

            GridOperator = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "grid operator identification",
                                         GridOperator_Id.TryParse,
                                         out GridOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the grid operator '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource, "data source", out var dataSource, out ErrorResponse))
                    return false;

                return TryParse(id,
                                dataSource,
                                JSON,
                                out GridOperator,
                                out ErrorResponse);

            }
            catch (Exception e)
            {
                GridOperator   = null;
                ErrorResponse  = "The given JSON representation of a grid operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out GridOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a grid operator: the keys and values of its JSON representation,
        /// see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="GridOperator">The parsed grid operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                               CBOR,
                                       [NotNullWhen(true)]  out GridOperator?  GridOperator,
                                       [NotNullWhen(false)] out String?        ErrorResponse)
        {

            GridOperator = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a grid operator must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "grid operator identification",
                                         GridOperator_Id.TryParse,
                                         out GridOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var context) &&
                    !(context.TryGetText(out var contextText) && contextText == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the grid operator '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource, "data source", out var dataSource, out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                // What has no CBOR parser of its own is read as its JSON, with the parsers of the JSON representation:
                // the multilingual texts, the address, the homepage, the hotline, the children, the keys and the metadata.
                var json = new JObject(
                               new JProperty(BasePropertyKeys.Id, id.ToString())
                           );

                foreach (var key in new[] { BasePropertyKeys.Name,
                                            BasePropertyKeys.Description,
                                            PropertyKeys.    Address,
                                            PropertyKeys.    Homepage,
                                            PropertyKeys.    HotlinePhoneNumber,
                                            PropertyKeys.    GridConnectionPoints,
                                            PropertyKeys.    GridConnectionPointIds,
                                            PropertyKeys.    EnergyMeterTemplates,
                                            PropertyKeys.    EnergyMeterTemplateIds,
                                            PropertyKeys.    EnergyMeterGroups,
                                            PropertyKeys.    EnergyMeterGroupIds,
                                            PropertyKeys.    SigningKeys,
                                            PropertyKeys.    IdentityKeys,
                                            BasePropertyKeys.Created,
                                            BasePropertyKeys.LastChange,
                                            BasePropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                if (!TryParse(id,
                              dataSource,
                              json,
                              out GridOperator,
                              out ErrorResponse))
                {
                    return false;
                }

                if (!TryCheckETagsOf(CBOR, GridOperator, out ErrorResponse))
                {
                    GridOperator = null;
                    return false;
                }

                return true;

            }
            catch (Exception e)
            {
                GridOperator   = null;
                ErrorResponse  = "The given CBOR representation of a grid operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (private static) TryParse(Id, DataSource, JSON, out GridOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the rest of the given JSON representation of a grid operator, or of the JSON of the parts of its CBOR
        /// representation without a CBOR parser of their own.
        /// </summary>
        private static Boolean TryParse(GridOperator_Id                         Id,
                                        String?                                 DataSource,
                                        JObject                                 JSON,
                                        [NotNullWhen(true)]  out GridOperator?  GridOperator,
                                        [NotNullWhen(false)] out String?        ErrorResponse)
        {

            GridOperator = null;

            if (!POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Name,               "grid operator name",         out var name,         out ErrorResponse) ||
                !POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Description,        "grid operator description",  out var description,  out ErrorResponse) ||
                !POIJSON.TryParseAddress(JSON, PropertyKeys.    Address,            "address",                    out var address,      out ErrorResponse) ||
                !POIJSON.TryParseText   (JSON, PropertyKeys.    Homepage,           "homepage",                   URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                !POIJSON.TryParseText   (JSON, PropertyKeys.    HotlinePhoneNumber, "hotline phone number",       PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
            {
                return false;
            }

            if (!POIJSON.TryParseChildren<GridConnectionPoint>(JSON, PropertyKeys.GridConnectionPoints, PropertyKeys.GridConnectionPointIds, "grid connection points", GridConnectionPoint.TryParse, out var gridConnectionPoints, out ErrorResponse))
                return false;

            if (!POIJSON.TryParseChildren<EnergyMeterTemplate>(JSON, PropertyKeys.EnergyMeterTemplates, PropertyKeys.EnergyMeterTemplateIds, "energy meter templates", EnergyMeterTemplate.TryParse, out var energyMeterTemplates, out ErrorResponse))
                return false;

            if (!POIJSON.TryParseChildren<EnergyMeterGroup>   (JSON, PropertyKeys.EnergyMeterGroups,    PropertyKeys.EnergyMeterGroupIds,    "energy meter groups",    EnergyMeterGroup.   TryParse, out var energyMeterGroups,    out ErrorResponse))
                return false;

            if (!POIPartyKeys.TryParse(JSON, out var signingKeys, out var identityKeys, out ErrorResponse))
                return false;

            if (!POIEntityMetadata.TryParse(JSON,
                                            out var created,
                                            out var lastChange,
                                            out var customData,
                                            out ErrorResponse))
            {
                return false;
            }

            GridOperator = new GridOperator(Id,
                                            name,
                                            description,
                                            address,
                                            homepage,
                                            hotline,
                                            gridConnectionPoints,
                                            SigningKeys:        signingKeys,
                                            IdentityKeys:       identityKeys,
                                            EnergyMeterGroups:     energyMeterGroups,
                                            EnergyMeterTemplates:  energyMeterTemplates,
                                            DataSource:  DataSource,
                                            Created:     created,
                                            LastChange:  lastChange,
                                            CustomData:  customData);

            if (!TryCheckETagsOf(JSON, GridOperator, out ErrorResponse))
            {
                GridOperator = null;
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two grid operators for equality by their identifications.
        /// </summary>
        public static Boolean operator == (GridOperator? GridOperator1, GridOperator? GridOperator2)

            => ReferenceEquals(GridOperator1, GridOperator2) ||
               (GridOperator1 is not null && GridOperator1.Equals(GridOperator2));

        /// <summary>
        /// Compares two grid operators for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (GridOperator? GridOperator1, GridOperator? GridOperator2)

            => !(GridOperator1 == GridOperator2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is GridOperator gridOperator
                   ? CompareTo(gridOperator)
                   : throw new ArgumentException("The given object is not a grid operator!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(GridOperator? GridOperator)

            => GridOperator is null
                   ? throw new ArgumentNullException(nameof(GridOperator), "The given grid operator must not be null!")
                   : Id.CompareTo(GridOperator.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is GridOperator gridOperator &&
               Equals(gridOperator);

        /// <inheritdoc/>
        public Boolean Equals(GridOperator? GridOperator)

            => GridOperator is not null &&
               Id.Equals(GridOperator.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
