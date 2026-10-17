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
    /// It may give its members the properties of an EVSE, which they take at runtime, as the effective EVSEs of their
    /// operator: a value of their own wins, arrays are united, and of the groups of a member the one with the higher
    /// priority wins.
    /// </summary>
    public sealed class EVSEGroup : AImmutableEMobilityEntity<EVSEGroup_Id,
                                                              EVSEGroupAdminStatusType,
                                                              EVSEGroupStatusType>,
                                    IPOIPrioritizedGroup,
                                    IEquatable<EVSEGroup>,
                                    IComparable<EVSEGroup>
    {

        #region Properties

        /// <summary>
        /// Its EVSEs, of the same charging station operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EVSE_Id>  EVSEIds           { get; }

        /// <summary>
        /// The charging tariff of its operator for its EVSEs without tariffs of their own, unless more specific groups
        /// name one: EVSE groups before charging station groups before charging pool groups, of one level the one with the
        /// highest priority (ChargingStationOperator.EffectiveEVSE(…)).
        /// </summary>
        public ChargingTariff_Id?       ChargingTariffId  { get; }

        /// <summary>
        /// Its priority over the other groups of a member, where they give it different values: the higher wins;
        /// 0, unless given. Groups of the same member with the same priority must not give it different values.
        /// </summary>
        public Int32?  Priority  { get; }

        /// <inheritdoc/>
        String IPOIPrioritizedGroup.GroupId
            => Id.ToString();


        /// <summary>
        /// The reference printed on its members, unless they have their own.
        /// </summary>
        public String?                            PhysicalReference         { get; }

        /// <summary>
        /// The current types of its members, unless they tell their own; Unspecified gives none.
        /// </summary>
        public CurrentTypes                       CurrentTypes              { get; }

        /// <summary>
        /// The charging modes of its members, unless they tell their own; Unspecified gives none.
        /// </summary>
        public ChargingModes                      ChargingModes             { get; }

        /// <summary>
        /// The maximum voltage of its members, unless they have their own.
        /// </summary>
        public Volt?                              MaxVoltage                { get; }

        /// <summary>
        /// The maximum current of its members, unless they have their own.
        /// </summary>
        public Ampere?                            MaxCurrent                { get; }

        /// <summary>
        /// The maximum power of its members, unless they have their own.
        /// </summary>
        public Watt?                              MaxPower                  { get; }

        /// <summary>
        /// Charging connectors of its members, besides their own, in the order of their identifications;
        /// of the same identification their own.
        /// </summary>
        public ImmutableArray<ChargingConnector>  ChargingConnectors        { get; }

        /// <summary>
        /// Brands its members also appear under, besides their own, in the order of their identifications.
        /// </summary>
        public POICollection<Brand_Id, Brand>     Brands                    { get; }

        /// <summary>
        /// Images of its members, besides their own, in their given order.
        /// </summary>
        public ImmutableArray<Image>              Images                    { get; }

        /// <summary>
        /// The geographical location of its members, unless they have their own.
        /// </summary>
        public GeoCoordinate?                     GeoLocation               { get; }

        /// <summary>
        /// The maximum reservation duration of its members, unless they have their own.
        /// </summary>
        public TimeSpan?                          MaxReservationDuration    { get; }

        /// <summary>
        /// The reach distance of its members, unless they have their own.
        /// </summary>
        public Meter?                             ReachDistance             { get; }

        /// <summary>
        /// The operation timeout of its members, unless they have their own.
        /// </summary>
        public TimeSpan?                          OperationTimeout          { get; }

        /// <summary>
        /// Whether its members have an extended operation timeout, unless they tell it themselves.
        /// </summary>
        public Boolean?                           ExtendedOperationTimeout  { get; }

        /// <summary>
        /// Standards of its members, besides their own, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>           Standards                 { get; }

        /// <summary>
        /// The floor level of its members, unless they tell it themselves.
        /// </summary>
        public String?                            FloorLevel                { get; }

        /// <summary>
        /// E-mobility providers whose contracts its members accept, besides their own, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EMobilityProvider_Id>  AcceptedEMobilityProviderIds  { get; }

        /// <summary>
        /// Whether it gives its members any property of an EVSE.
        /// </summary>
        public Boolean HasEVSEProperties

            => PhysicalReference is not null    || CurrentTypes  != POI.CurrentTypes. Unspecified || ChargingModes != POI.ChargingModes.Unspecified ||
               MaxVoltage.HasValue              || MaxCurrent.HasValue           || MaxPower.HasValue                  || ChargingConnectors.Length > 0 ||
               Brands.Count > 0                 || Images.Length > 0             || GeoLocation.HasValue               || MaxReservationDuration.HasValue ||
               ReachDistance.HasValue           || OperationTimeout.HasValue     || ExtendedOperationTimeout.HasValue  || Standards.Length > 0 ||
               FloorLevel is not null           || AcceptedEMobilityProviderIds.Length > 0;

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
                         Int32?                                   Priority                     = null,

                         String?                                  PhysicalReference            = null,
                         CurrentTypes?                            CurrentTypes                 = null,
                         ChargingModes?                           ChargingModes                = null,
                         Volt?                                    MaxVoltage                   = null,
                         Ampere?                                  MaxCurrent                   = null,
                         Watt?                                    MaxPower                     = null,
                         IEnumerable<ChargingConnector>?          ChargingConnectors           = null,
                         IEnumerable<Brand>?                      Brands                       = null,
                         IEnumerable<Image>?                      Images                       = null,
                         GeoCoordinate?                           GeoLocation                  = null,
                         TimeSpan?                                MaxReservationDuration       = null,
                         Meter?                                   ReachDistance                = null,
                         TimeSpan?                                OperationTimeout             = null,
                         Boolean?                                 ExtendedOperationTimeout     = null,
                         IEnumerable<Standard>?                   Standards                    = null,
                         String?                                  FloorLevel                   = null,
                         IEnumerable<EMobilityProvider_Id>?       AcceptedEMobilityProviderIds = null,

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

            if (MaxVoltage?.Value < 0 || MaxCurrent?.Value < 0 || MaxPower?.Value < 0)
                throw new ArgumentException($"The electrical limits of the EVSE group '{Id}' must not be negative!");

            if (MaxReservationDuration < TimeSpan.Zero)
                throw new ArgumentException($"The maximum reservation duration of the EVSE group '{Id}' must not be negative!", nameof(MaxReservationDuration));

            if (ReachDistance?.m < 0)
                throw new ArgumentException($"The reach distance of the EVSE group '{Id}' must not be negative!", nameof(ReachDistance));

            if (OperationTimeout < TimeSpan.Zero)
                throw new ArgumentException($"The operation timeout of the EVSE group '{Id}' must not be negative!", nameof(OperationTimeout));

            var chargingConnectors = (ChargingConnectors ?? []).ToArray();

            if (chargingConnectors.Any(chargingConnector => chargingConnector is null))
                throw new ArgumentException("Charging connectors must not contain null entries!", nameof(ChargingConnectors));

            if (chargingConnectors.Select(chargingConnector => chargingConnector.Id).Distinct().Count() != chargingConnectors.Length)
                throw new ArgumentException($"A charging connector of the EVSE group '{Id}' must not be given twice!", nameof(ChargingConnectors));

            foreach (var chargingConnector in chargingConnectors)
                if (chargingConnector.Validate() is String error)
                    throw new ArgumentException(error, nameof(ChargingConnectors));

            this.EVSEIds                   = POIJSON.OrderedIds(EVSEIds, $"member of the EVSE group '{Id}'");
            this.ChargingTariffId          = ChargingTariffId;
            this.Priority                  = Priority;

            this.PhysicalReference         = PhysicalReference;
            this.CurrentTypes              = CurrentTypes  ?? POI.CurrentTypes. Unspecified;
            this.ChargingModes             = ChargingModes ?? POI.ChargingModes.Unspecified;
            this.MaxVoltage                = MaxVoltage;
            this.MaxCurrent                = MaxCurrent;
            this.MaxPower                  = MaxPower;
            this.ChargingConnectors        = [.. chargingConnectors.OrderBy(chargingConnector => chargingConnector.Id.ToString(), StringComparer.Ordinal)];
            this.Brands                    = new (Brands, brand => brand.Id, "brand", $"the EVSE group '{Id}'");
            this.Images                    = POIJSON.Distinct(Images, image => image.URL.ToString(), $"image of the EVSE group '{Id}'");
            this.GeoLocation               = GeoLocation;
            this.MaxReservationDuration    = MaxReservationDuration;
            this.ReachDistance             = ReachDistance;
            this.OperationTimeout          = OperationTimeout;
            this.ExtendedOperationTimeout  = ExtendedOperationTimeout;
            this.Standards                 = POIJSON.OrderedIds(Standards, $"standard of the EVSE group '{Id}'");
            this.FloorLevel                = FloorLevel;
            this.AcceptedEMobilityProviderIds  = POIJSON.OrderedIds(AcceptedEMobilityProviderIds, $"accepted e-mobility provider of the EVSE group '{Id}'");

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

            if (Priority.HasValue)
                json.Add("priority",  Priority.Value);

            // The properties of an EVSE, as an EVSE writes them.
            if (PhysicalReference is not null)
                json.Add("physicalReference",         PhysicalReference);

            if (CurrentTypes  != POI.CurrentTypes. Unspecified)
                json.Add("currentTypes",              POIJSON.FlagsToJSON(CurrentTypes));

            if (ChargingModes != POI.ChargingModes.Unspecified)
                json.Add("chargingModes",             POIJSON.FlagsToJSON(ChargingModes));

            if (MaxVoltage.HasValue)
                json.Add("maxVoltage",                POIJSON.Number(MaxVoltage.Value.Value));

            if (MaxCurrent.HasValue)
                json.Add("maxCurrent",                POIJSON.Number(MaxCurrent.Value.Value));

            if (MaxPower.HasValue)
                json.Add("maxPower",                  POIJSON.Number(MaxPower.Value.Value));

            if (ChargingConnectors.Length > 0)
                json.Add("chargingConnectors",        new JArray(ChargingConnectors.Select(chargingConnector => chargingConnector.ToJSON())));

            if (Brands.Count > 0)
                json.Add("brands",                    new JArray(Brands.Select(brand => brand.ToJSON())));

            if (Images.Length > 0)
                json.Add("images",                    new JArray(Images.Select(image => image.ToJSON())));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",               GeoLocation.Value.ToJSON(Embedded: true));

            if (MaxReservationDuration.HasValue)
                json.Add("maxReservationDuration",    POIJSON.Seconds(MaxReservationDuration.Value));

            if (ReachDistance.HasValue)
                json.Add("reachDistance",             POIJSON.Number(ReachDistance.Value.m));

            if (OperationTimeout.HasValue)
                json.Add("operationTimeout",          POIJSON.Seconds(OperationTimeout.Value));

            if (ExtendedOperationTimeout.HasValue)
                json.Add("extendedOperationTimeout",  ExtendedOperationTimeout.Value);

            if (Standards.Length > 0)
                json.Add("standards",                 POIJSON.IdsToJSON(Standards));

            if (FloorLevel is not null)
                json.Add("floorLevel",                FloorLevel);

            if (AcceptedEMobilityProviderIds.Length > 0)
                json.Add("acceptedEMobilityProviderIds",  POIJSON.IdsToJSON(AcceptedEMobilityProviderIds));

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

                if (!POIJSON.TryParseInteger(JSON, "priority", "priority of the EVSE group", out var priority, out ErrorResponse))
                    return false;

                // The properties of an EVSE, as an EVSE reads them.
                if (!POIJSON.TryParseString(JSON, "physicalReference",  "physical reference",  out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseFlags (JSON, "currentTypes",       "current type",        out CurrentTypes  currentTypes,  out ErrorResponse) ||
                    !POIJSON.TryParseFlags (JSON, "chargingModes",      "charging mode",       out ChargingModes chargingModes, out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional("maxVoltage", "maximum voltage", out Volt?   maxVoltage, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("maxCurrent", "maximum current", out Ampere? maxCurrent, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional("maxPower",   "maximum power",   out Watt?   maxPower,   out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIJSON.TryParseChildren<ChargingConnector>(JSON,
                                                                 "chargingConnectors",
                                                                 "chargingConnectorIds",
                                                                 "charging connectors",
                                                                 ChargingConnector.TryParse,
                                                                 out var chargingConnectors,
                                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<Brand>(JSON, "brands", "brandIds", "brands", Brand.TryParse, out var brands, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, "images", "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseGeoLocation(JSON,                            out var geoLocation,             out ErrorResponse) ||
                    !POIJSON.TryParseSeconds    (JSON, "maxReservationDuration",
                                                       "maximum reservation duration", out var maxReservationDuration,  out ErrorResponse) ||
                    !POIJSON.TryParseSeconds    (JSON, "operationTimeout",
                                                       "operation timeout",            out var operationTimeout,        out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, "reachDistance",
                                                       "reach distance",               out var reachDistance,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "standards", "standard", Standard.TryParse, out ImmutableArray<Standard> standards, out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "floorLevel",           "floor level",                  out var floorLevel,              out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, "acceptedEMobilityProviderIds", "accepted e-mobility provider identification",
                                                 EMobilityProvider_Id.TryParse, out ImmutableArray<EMobilityProvider_Id> acceptedEMobilityProviderIds, out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional("extendedOperationTimeout", "extended operation timeout", out Boolean? extendedOperationTimeout, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

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
                                          priority,
                                          physicalReference,
                                          currentTypes,
                                          chargingModes,
                                          maxVoltage,
                                          maxCurrent,
                                          maxPower,
                                          chargingConnectors,
                                          brands,
                                          images,
                                          geoLocation,
                                          maxReservationDuration,
                                          reachDistance.HasValue ? Meter.From_m(reachDistance.Value) : null,
                                          operationTimeout,
                                          extendedOperationTimeout,
                                          standards,
                                          floorLevel,
                                          acceptedEMobilityProviderIds,
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
