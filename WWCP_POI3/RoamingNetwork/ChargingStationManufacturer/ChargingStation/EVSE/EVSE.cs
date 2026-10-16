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
    /// The admin status of an EVSE changed.
    /// </summary>
    public delegate Task OnEVSEAdminStatusChangedDelegate(DateTimeOffset                     Timestamp,
                                                          EventTracking_Id                   EventTrackingId,
                                                          EVSE                               EVSE,
                                                          Timestamped<EVSEAdminStatusType>   OldStatus,
                                                          Timestamped<EVSEAdminStatusType>   NewStatus,
                                                          Context?                           DataSource);

    /// <summary>
    /// The status of an EVSE changed.
    /// </summary>
    public delegate Task OnEVSEStatusChangedDelegate(DateTimeOffset                Timestamp,
                                                     EventTracking_Id              EventTrackingId,
                                                     EVSE                          EVSE,
                                                     Timestamped<EVSEStatusType>   OldStatus,
                                                     Timestamped<EVSEStatusType>   NewStatus,
                                                     Context?                      DataSource);


    /// <summary>
    /// The energy status of an EVSE changed.
    /// </summary>
    public delegate Task OnEVSEEnergyStatusChangedDelegate(DateTimeOffset            Timestamp,
                                                        EventTracking_Id          EventTrackingId,
                                                        EVSE                      EVSE,
                                                        Timestamped<EnergyInfo>   OldEnergyStatus,
                                                        Timestamped<EnergyInfo>   NewEnergyStatus,
                                                        Context?                  DataSource);


    /// <summary>
    /// An Electric Vehicle Supply Equipment: what charges one vehicle at a time, with its charging connectors.
    /// Embedded completely in its charging station.
    /// </summary>
    /// <remarks>
    /// The electrical limits are in their base units: maxVoltage in V, maxCurrent in A, maxPower in W.
    /// </remarks>
    public sealed class EVSE : AImmutableEMobilityEntity<EVSE_Id,
                                                         EVSEAdminStatusType,
                                                         EVSEStatusType>,
                               IEquatable<EVSE>,
                               IComparable<EVSE>
    {

        #region Properties

        /// <summary>
        /// The reference printed on the EVSE, e.g. "A1".
        /// </summary>
        public String?                            PhysicalReference     { get; }

        /// <summary>
        /// The current types, e.g. AC_ThreePhases.
        /// </summary>
        public CurrentTypes                       CurrentTypes          { get; }

        /// <summary>
        /// The charging modes, e.g. Mode_3.
        /// </summary>
        public ChargingModes                      ChargingModes         { get; }

        /// <summary>
        /// The maximum voltage.
        /// </summary>
        public Volt?                              MaxVoltage            { get; }

        /// <summary>
        /// The maximum current.
        /// </summary>
        public Ampere?                            MaxCurrent            { get; }

        /// <summary>
        /// The maximum power.
        /// </summary>
        public Watt?                              MaxPower              { get; }

        /// <summary>
        /// The charging connectors, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingConnector>  ChargingConnectors    { get; }

        /// <summary>
        /// The energy meter.
        /// </summary>
        public EnergyMeter?                       EnergyMeter           { get; }

        /// <summary>
        /// The charging tariffs of its operator that apply, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ChargingTariff_Id>  ChargingTariffIds     { get; }

        /// <summary>
        /// Its energy meter, if any.
        /// </summary>
        public IEnumerable<EnergyMeter>           AllEnergyMeters
            => EnergyMeter is not null ? [ EnergyMeter ] : [];


        /// <summary>
        /// The brands it also appears under: its own, embedded completely, in the order of their identifications.
        /// Not those of its operator or of what it embeds; every level has brands of its own.
        /// </summary>
        public POICollection<Brand_Id, Brand>  Brands  { get; }


        /// <summary>
        /// Its images, in their given order.
        /// </summary>
        public ImmutableArray<Image>  Images  { get; }


        /// <summary>
        /// The geographical location, when it differs from the one of its charging station.
        /// </summary>
        public GeoCoordinate?  GeoLocation               { get; }

        /// <summary>
        /// How long it can be reserved at most.
        /// </summary>
        public TimeSpan?       MaxReservationDuration    { get; }

        /// <summary>
        /// The least distance a driver has to reach between obstacles and its connector, cable, display and controls.
        /// </summary>
        public Meter?                    ReachDistance              { get; }

        /// <summary>
        /// The longest time allowed between the steps of a charging process before it expires or is cancelled.
        /// </summary>
        public TimeSpan?                 OperationTimeout           { get; }

        /// <summary>
        /// Whether a driver can extend its timeouts, unless unknown.
        /// </summary>
        public Boolean?                  ExtendedOperationTimeout   { get; }

        /// <summary>
        /// The standards it conforms to, e.g. accessibility or ergonomic ones, in the order of their names.
        /// </summary>
        public ImmutableArray<Standard>  Standards                  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new EVSE.
        /// </summary>
        public EVSE(EVSE_Id                              Id,
                    I18NString?                          Name                         = null,
                    I18NString?                          Description                  = null,
                    String?                              PhysicalReference            = null,
                    CurrentTypes?                        CurrentTypes                 = null,
                    ChargingModes?                       ChargingModes                = null,
                    Volt?                                MaxVoltage                   = null,
                    Ampere?                              MaxCurrent                   = null,
                    Watt?                                MaxPower                     = null,
                    IEnumerable<ChargingConnector>?      ChargingConnectors           = null,
                    EnergyMeter?                         EnergyMeter                  = null,
                    IEnumerable<ChargingTariff_Id>?      ChargingTariffIds            = null,
                    IEnumerable<Brand>?                  Brands                       = null,
                    IEnumerable<Image>?                  Images                       = null,
                    GeoCoordinate?                       GeoLocation                  = null,
                    TimeSpan?                            MaxReservationDuration       = null,
                    Meter?                               ReachDistance                = null,
                    TimeSpan?                            OperationTimeout             = null,
                    Boolean?                             ExtendedOperationTimeout     = null,
                    IEnumerable<Standard>?               Standards                    = null,

                    Timestamped<EVSEAdminStatusType>?    InitialAdminStatus           = null,
                    Timestamped<EVSEStatusType>?         InitialStatus                = null,
                    UInt16?                              MaxAdminStatusScheduleSize   = null,
                    UInt16?                              MaxStatusScheduleSize        = null,

                    String?                              DataSource                   = null,
                    DateTimeOffset?                      Created                      = null,
                    DateTimeOffset?                      LastChange                   = null,

                    CustomDataNew?                       CustomData                   = null,
                    UserDefinedDictionary?               InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<EVSEAdminStatusType>(Created ?? Timestamp.Now, EVSEAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<EVSEStatusType>     (Created ?? Timestamp.Now, EVSEStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            if (MaxVoltage?.Value < 0 || MaxCurrent?.Value < 0 || MaxPower?.Value < 0)
                throw new ArgumentException($"The electrical limits of the EVSE '{Id}' must not be negative!");

            if (MaxReservationDuration < TimeSpan.Zero)
                throw new ArgumentException($"The maximum reservation duration of the EVSE '{Id}' must not be negative!", nameof(MaxReservationDuration));

            if (ReachDistance?.m < 0)
                throw new ArgumentException($"The reach distance of the EVSE '{Id}' must not be negative!", nameof(ReachDistance));

            if (OperationTimeout < TimeSpan.Zero)
                throw new ArgumentException($"The operation timeout of the EVSE '{Id}' must not be negative!", nameof(OperationTimeout));

            var chargingConnectors = (ChargingConnectors ?? []).ToArray();

            if (chargingConnectors.Any(chargingConnector => chargingConnector is null))
                throw new ArgumentException("Charging connectors must not contain null entries!", nameof(ChargingConnectors));

            if (chargingConnectors.Select(chargingConnector => chargingConnector.Id).Distinct().Count() != chargingConnectors.Length)
                throw new ArgumentException($"A charging connector of the EVSE '{Id}' must not be given twice!", nameof(ChargingConnectors));

            foreach (var chargingConnector in chargingConnectors)
                if (chargingConnector.Validate() is String error)
                    throw new ArgumentException(error, nameof(ChargingConnectors));

            this.PhysicalReference   = PhysicalReference;
            this.CurrentTypes        = CurrentTypes  ?? POI.CurrentTypes. Unspecified;
            this.ChargingModes       = ChargingModes ?? POI.ChargingModes.Unspecified;
            this.MaxVoltage          = MaxVoltage;
            this.MaxCurrent          = MaxCurrent;
            this.MaxPower            = MaxPower;
            this.ChargingConnectors  = [.. chargingConnectors.OrderBy(chargingConnector => chargingConnector.Id.ToString(), StringComparer.Ordinal)];
            this.EnergyMeter         = EnergyMeter;
            this.ChargingTariffIds   = POIJSON.OrderedIds(ChargingTariffIds, $"charging tariff of the EVSE '{Id}'");
            this.Brands              = new (Brands, brand => brand.Id, "brand", $"the EVSE '{Id}'");
            this.Images = POIJSON.Distinct(Images, image => image.URL.ToString(), $"image of the EVSE '{Id}'");
            this.GeoLocation             = GeoLocation;
            this.MaxReservationDuration  = MaxReservationDuration;
            this.ReachDistance             = ReachDistance;
            this.OperationTimeout          = OperationTimeout;
            this.ExtendedOperationTimeout  = ExtendedOperationTimeout;
            this.Standards                 = POIJSON.OrderedIds(Standards, $"standard of the EVSE '{Id}'");
            this.energyStatus        = new EnergyStatusRuntime(RaiseEnergyStatusChanged);

        }

        #endregion


        #region Status events

        private readonly Lock                               statusEventsLock  = new ();
        private          OnEVSEAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnEVSEStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnEVSEAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnEVSEStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                    Timestamp,
                                                                   EventTracking_Id                  EventTrackingId,
                                                                   Timestamped<EVSEAdminStatusType>  NewStatus,
                                                                   Timestamped<EVSEAdminStatusType>  OldStatus,
                                                                   Context?                          DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset               Timestamp,
                                                              EventTracking_Id             EventTrackingId,
                                                              Timestamped<EVSEStatusType>  NewStatus,
                                                              Timestamped<EVSEStatusType>  OldStatus,
                                                              Context?                     DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region Energy status

        private readonly EnergyStatusRuntime                energyStatus;
        private          OnEVSEEnergyStatusChangedDelegate?  onEnergyStatusChanged;

        /// <summary>
        /// The current energy status: the power used and available, and the energy stored;
        /// runtime data like the admin status and the status, unless none was reported yet.
        /// </summary>
        public Timestamped<EnergyInfo>?  EnergyStatus
            => energyStatus.Current;

        /// <summary>
        /// The next planned energy status.
        /// </summary>
        internal Timestamped<EnergyInfo>?  NextEnergyStatus
            => energyStatus.Next;

        /// <summary>
        /// Set the energy status, since the given timestamp or now; a later timestamp plans it.
        /// </summary>
        /// <param name="EnergyStatus">The power used and available, and the energy stored.</param>
        /// <param name="Timestamp">Since when; now, unless given.</param>
        /// <param name="DataSource">An optional source.</param>
        public void SetEnergyStatus(EnergyInfo       EnergyStatus,
                                    DateTimeOffset?  Timestamp    = null,
                                    Context?         DataSource   = null)

            => energyStatus.Set(EnergyStatus, Timestamp ?? org.GraphDefined.Vanaheimr.Illias.Timestamp.Now, DataSource);

        /// <summary>
        /// Take the given energy status as it is, also when it repeats the one before.
        /// </summary>
        internal void TakeEnergyStatus(EnergyInfo      EnergyStatus,
                                       DateTimeOffset  Timestamp,
                                       Context?        DataSource)

            => energyStatus.Take(EnergyStatus, Timestamp, DataSource);

        /// <summary>
        /// An event fired whenever the energy status changed.
        /// </summary>
        public event OnEVSEEnergyStatusChangedDelegate? OnEnergyStatusChanged
        {
            add    { lock (statusEventsLock) { onEnergyStatusChanged += value; energyStatus.Listen(onEnergyStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onEnergyStatusChanged -= value; energyStatus.Listen(onEnergyStatusChanged is not null); } }
        }

        private async Task RaiseEnergyStatusChanged(DateTimeOffset           Timestamp,
                                                    EventTracking_Id         EventTrackingId,
                                                    Timestamped<EnergyInfo>  NewEnergyStatus,
                                                    Timestamped<EnergyInfo>  OldEnergyStatus,
                                                    Context?                 DataSource)
        {
            var onEnergyStatusChanged = this.onEnergyStatusChanged;
            if (onEnergyStatusChanged is not null)
                await onEnergyStatusChanged(Timestamp, EventTrackingId, this, OldEnergyStatus, NewEnergyStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override void ShareFurtherRuntimeOf(AImmutableEMobilityEntity<EVSE_Id, EVSEAdminStatusType, EVSEStatusType> Previous)
        {
            if (Previous is EVSE previousEVSE)
                energyStatus.ShareOf(previousEVSE.energyStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this EVSE, as its charging station embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",               Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",        Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",         DataSource);

            if (PhysicalReference is not null)
                json.Add("physicalReference",  PhysicalReference);

            if (CurrentTypes  != POI.CurrentTypes. Unspecified)
                json.Add("currentTypes",       POIJSON.FlagsToJSON(CurrentTypes));

            if (ChargingModes != POI.ChargingModes.Unspecified)
                json.Add("chargingModes",      POIJSON.FlagsToJSON(ChargingModes));

            if (MaxVoltage.HasValue)
                json.Add("maxVoltage",         MaxVoltage.Value.Value);

            if (MaxCurrent.HasValue)
                json.Add("maxCurrent",         MaxCurrent.Value.Value);

            if (MaxPower.HasValue)
                json.Add("maxPower",           MaxPower.Value.Value);

            if (ChargingConnectors.Length > 0)
                json.Add("chargingConnectors", new JArray(ChargingConnectors.Select(chargingConnector => chargingConnector.ToJSON())));

            if (EnergyMeter is not null)
                json.Add("energyMeter",        EnergyMeter.ToJSON());

            if (ChargingTariffIds.Length > 0)
                json.Add("chargingTariffIds",  POIJSON.IdsToJSON(ChargingTariffIds));

            if (Brands.Count > 0)
                json.Add("brands",        new JArray(Brands.Select(brand => brand.ToJSON())));

            if (Images.Length > 0)
                json.Add("images",  new JArray(Images.Select(image => image.ToJSON())));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",             GeoLocation.Value.ToJSON(Embedded: true));

            if (MaxReservationDuration.HasValue)
                json.Add("maxReservationDuration",  POIJSON.Seconds(MaxReservationDuration.Value));

            if (ReachDistance.HasValue)
                json.Add("reachDistance",             ReachDistance.Value.m);

            if (OperationTimeout.HasValue)
                json.Add("operationTimeout",          POIJSON.Seconds(OperationTimeout.Value));

            if (ExtendedOperationTimeout.HasValue)
                json.Add("extendedOperationTimeout",  ExtendedOperationTimeout.Value);

            if (Standards.Length > 0)
                json.Add("standards",                 POIJSON.IdsToJSON(Standards));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out EVSE, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an EVSE.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EVSE">The parsed EVSE.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                           JSON,
                                       [NotNullWhen(true)]  out EVSE?    EVSE,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            EVSE = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "EVSE identification",
                                         EVSE_Id.TryParse,
                                         out EVSE_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, "name",               "EVSE name",                out var name,               out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, "description",        "EVSE description",         out var description,        out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "dataSource",         "data source",              out var dataSource,         out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "physicalReference",  "physical reference",       out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseFlags (JSON, "currentTypes",       "current type",             out CurrentTypes  currentTypes,  out ErrorResponse) ||
                    !POIJSON.TryParseFlags (JSON, "chargingModes",      "charging mode",            out ChargingModes chargingModes, out ErrorResponse))
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

                if (!POI.EnergyMeter.TryParseOne(JSON, "energyMeter", out var energyMeter, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseIds(JSON, "chargingTariffIds", "charging tariff identification", ChargingTariff_Id.TryParse, out ImmutableArray<ChargingTariff_Id> chargingTariffIds, out ErrorResponse))
                    return false;

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
                    !POIJSON.TryParseIds        (JSON, "standards", "standard", Standard.TryParse, out ImmutableArray<Standard> standards, out ErrorResponse))
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

                EVSE = new EVSE(id,
                                name,
                                description,
                                physicalReference,
                                currentTypes,
                                chargingModes,
                                maxVoltage,
                                maxCurrent,
                                maxPower,
                                chargingConnectors,
                                energyMeter,
                                chargingTariffIds,
                                brands,
                                images,
                                geoLocation,
                                maxReservationDuration,
                                reachDistance.HasValue ? Meter.From_m(reachDistance.Value) : null,
                                operationTimeout,
                                extendedOperationTimeout,
                                standards,
                                DataSource:  dataSource,
                                Created:     created,
                                LastChange:  lastChange,
                                CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EVSE           = null;
                ErrorResponse  = "The given JSON representation of an EVSE is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two EVSEs for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EVSE? EVSE1, EVSE? EVSE2)

            => ReferenceEquals(EVSE1, EVSE2) ||
               (EVSE1 is not null && EVSE1.Equals(EVSE2));

        /// <summary>
        /// Compares two EVSEs for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EVSE? EVSE1, EVSE? EVSE2)

            => !(EVSE1 == EVSE2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is EVSE evse
                   ? CompareTo(evse)
                   : throw new ArgumentException("The given object is not an EVSE!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EVSE? EVSE)

            => EVSE is null
                   ? throw new ArgumentNullException(nameof(EVSE), "The given EVSE must not be null!")
                   : Id.CompareTo(EVSE.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EVSE evse &&
               Equals(evse);

        /// <inheritdoc/>
        public Boolean Equals(EVSE? EVSE)

            => EVSE is not null &&
               Id.Equals(EVSE.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
