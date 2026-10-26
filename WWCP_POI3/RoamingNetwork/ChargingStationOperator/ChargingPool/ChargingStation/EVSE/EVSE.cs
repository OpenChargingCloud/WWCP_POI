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
    /// <summary>
    /// A delegate called whenever the price status of an EVSE changed.
    /// </summary>
    public delegate Task OnEVSEPriceStatusChangedDelegate(DateTimeOffset                Timestamp,
                                                          EventTracking_Id              EventTrackingId,
                                                          EVSE                          EVSE,
                                                          Timestamped<ChargingPrices>   OldPriceStatus,
                                                          Timestamped<ChargingPrices>   NewPriceStatus,
                                                          Context?                      DataSource);

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

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/evse/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the reference printed on it.
            /// </summary>
            public const String  PhysicalReference              = "physicalReference";

            /// <summary>
            /// The property of its current types.
            /// </summary>
            public const String  CurrentTypes                   = "currentTypes";

            /// <summary>
            /// The property of its charging modes.
            /// </summary>
            public const String  ChargingModes                  = "chargingModes";

            /// <summary>
            /// The property of its maximum voltage.
            /// </summary>
            public const String  MaxVoltage                     = "maxVoltage";

            /// <summary>
            /// The property of its maximum current.
            /// </summary>
            public const String  MaxCurrent                     = "maxCurrent";

            /// <summary>
            /// The property of its maximum power.
            /// </summary>
            public const String  MaxPower                       = "maxPower";

            /// <summary>
            /// The property of its charging connectors.
            /// </summary>
            public const String  ChargingConnectors             = "chargingConnectors";

            /// <summary>
            /// The property of the identifications of its charging connectors, refused: they are given completely.
            /// </summary>
            public const String  ChargingConnectorIds           = "chargingConnectorIds";

            /// <summary>
            /// The property of its energy meter.
            /// </summary>
            public const String  EnergyMeter                    = "energyMeter";

            /// <summary>
            /// The property of the identifications of its charging tariffs.
            /// </summary>
            public const String  ChargingTariffIds              = "chargingTariffIds";

            /// <summary>
            /// The property of its brands.
            /// </summary>
            public const String  Brands                         = "brands";

            /// <summary>
            /// The property of the identifications of its brands, refused: they are given completely.
            /// </summary>
            public const String  BrandIds                       = "brandIds";

            /// <summary>
            /// The property of its images.
            /// </summary>
            public const String  Images                         = "images";

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation                    = "geoLocation";

            /// <summary>
            /// The property of its maximum reservation duration.
            /// </summary>
            public const String  MaxReservationDuration         = "maxReservationDuration";

            /// <summary>
            /// The property of its reach distance.
            /// </summary>
            public const String  ReachDistance                  = "reachDistance";

            /// <summary>
            /// The property of its operation timeout.
            /// </summary>
            public const String  OperationTimeout               = "operationTimeout";

            /// <summary>
            /// The property of whether a driver can extend its timeouts.
            /// </summary>
            public const String  ExtendedOperationTimeout       = "extendedOperationTimeout";

            /// <summary>
            /// The property of its standards.
            /// </summary>
            public const String  Standards                      = "standards";

            /// <summary>
            /// The property of its floor level.
            /// </summary>
            public const String  FloorLevel                     = "floorLevel";

            /// <summary>
            /// The property of the e-mobility providers whose contracts it accepts.
            /// </summary>
            public const String  AcceptedEMobilityProviderIds   = "acceptedEMobilityProviderIds";

            /// <summary>
            /// The property of its ISO 15118 certificate.
            /// </summary>
            public const String  ISO15118Certificate            = "iso15118Certificate";

            /// <summary>
            /// The property of the identifications of its templates.
            /// </summary>
            public const String  TemplateIds                    = "templateIds";

        }

        #endregion


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

        /// <summary>
        /// The floor level it is on, e.g. in a car park, in the numbering shown there, e.g. "-1" or "2B".
        /// </summary>
        public String?                   FloorLevel                 { get; }

        /// <summary>
        /// The e-mobility providers whose contracts it accepts, in the order of their identifications; none: unknown.
        /// </summary>
        public ImmutableArray<EMobilityProvider_Id>  AcceptedEMobilityProviderIds  { get; }

        /// <summary>
        /// Its ISO 15118 certificate, e.g. the PEM of the certificate its charging controller (SECC) proves itself with to
        /// vehicles for Plug &amp; Charge, as it is given. It belongs to the single EVSE: no template gives it.
        /// </summary>
        public String?                               ISO15118Certificate           { get; }

        /// <summary>
        /// The EVSE templates it takes, each once, in its order: of every property they give, the first one giving it
        /// decides, unless it has its own. It names them; they do not name their members.
        /// </summary>
        public ImmutableArray<EVSETemplate_Id>  TemplateIds  { get; }

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
                    String?                              FloorLevel                   = null,
                    IEnumerable<EMobilityProvider_Id>?   AcceptedEMobilityProviderIds = null,
                    String?                              ISO15118Certificate          = null,

                    IEnumerable<EVSETemplate_Id>?        TemplateIds                  = null,

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

            this.TemplateIds  = POIJSON.UniqueIds(TemplateIds, $"template of the EVSE '{Id}'");

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
            this.FloorLevel                = FloorLevel;
            this.AcceptedEMobilityProviderIds  = POIJSON.OrderedIds(AcceptedEMobilityProviderIds, $"accepted e-mobility provider of the EVSE '{Id}'");
            this.ISO15118Certificate           = ISO15118Certificate;
            this.energyStatus        = new EnergyStatusRuntime(RaiseEnergyStatusChanged);
            this.priceStatus         = new PriceStatusRuntime (RaisePriceStatusChanged);

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


        #region Price status

        private readonly PriceStatusRuntime                priceStatus;
        private          OnEVSEPriceStatusChangedDelegate?  onPriceStatusChanged;

        /// <summary>
        /// The current price status: the new prices of charging tariffs of its operator, e.g. ad hoc prices following
        /// the electricity market; runtime data like the admin status and the status, unless none was reported yet.
        /// </summary>
        public Timestamped<ChargingPrices>?  PriceStatus
            => priceStatus.Current;

        /// <summary>
        /// The next planned price status.
        /// </summary>
        internal Timestamped<ChargingPrices>?  NextPriceStatus
            => priceStatus.Next;

        /// <summary>
        /// Set the price status, since the given timestamp or now; a later timestamp plans it.
        /// </summary>
        /// <param name="PriceStatus">The new prices of charging tariffs of its operator.</param>
        /// <param name="Timestamp">Since when; now, unless given.</param>
        /// <param name="DataSource">An optional source.</param>
        public void SetPriceStatus(ChargingPrices   PriceStatus,
                                   DateTimeOffset?  Timestamp    = null,
                                   Context?         DataSource   = null)

            => priceStatus.Set(PriceStatus, Timestamp ?? org.GraphDefined.Vanaheimr.Illias.Timestamp.Now, DataSource);

        /// <summary>
        /// Take the given price status as it is, also when it repeats the one before.
        /// </summary>
        internal void TakePriceStatus(ChargingPrices  PriceStatus,
                                      DateTimeOffset  Timestamp,
                                      Context?        DataSource)

            => priceStatus.Take(PriceStatus, Timestamp, DataSource);

        /// <summary>
        /// An event fired whenever the price status changed.
        /// </summary>
        public event OnEVSEPriceStatusChangedDelegate? OnPriceStatusChanged
        {
            add    { lock (statusEventsLock) { onPriceStatusChanged += value; priceStatus.Listen(onPriceStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onPriceStatusChanged -= value; priceStatus.Listen(onPriceStatusChanged is not null); } }
        }

        private async Task RaisePriceStatusChanged(DateTimeOffset               Timestamp,
                                                   EventTracking_Id             EventTrackingId,
                                                   Timestamped<ChargingPrices>  NewPriceStatus,
                                                   Timestamped<ChargingPrices>  OldPriceStatus,
                                                   Context?                     DataSource)
        {
            var onPriceStatusChanged = this.onPriceStatusChanged;
            if (onPriceStatusChanged is not null)
                await onPriceStatusChanged(Timestamp, EventTrackingId, this, OldPriceStatus, NewPriceStatus, DataSource);
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
            {
                energyStatus.ShareOf(previousEVSE.energyStatus);
                priceStatus. ShareOf(previousEVSE.priceStatus);
            }
        }

        #endregion


        #region (protected override) ContentJSON()

        /// <inheritdoc/>
        protected override JObject ContentJSON()

            => ToJSON(Embedded: true);

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this EVSE; embedded, as its charging station embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomEVSESerializer">A delegate to serialize custom EVSE JSON objects.</param>
        public JObject ToJSON(Boolean                                 Embedded               = false,
                              Boolean                                 IncludeETags           = false,
                              ETagDigestEncoding?                     ETagDigestEncoding     = null,
                              CustomJObjectSerializerDelegate<EVSE>?  CustomEVSESerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id,  Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,                   JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,                      Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,               Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,                DataSource);

            if (PhysicalReference is not null)
                json.Add(PropertyKeys.PhysicalReference,             PhysicalReference);

            if (CurrentTypes  != POI.CurrentTypes. Unspecified)
                json.Add(PropertyKeys.CurrentTypes,                  POIJSON.FlagsToJSON(CurrentTypes));

            if (ChargingModes != POI.ChargingModes.Unspecified)
                json.Add(PropertyKeys.ChargingModes,                 POIJSON.FlagsToJSON(ChargingModes));

            if (MaxVoltage.HasValue)
                json.Add(PropertyKeys.MaxVoltage,                    POIJSON.Number(MaxVoltage.Value.Value));

            if (MaxCurrent.HasValue)
                json.Add(PropertyKeys.MaxCurrent,                    POIJSON.Number(MaxCurrent.Value.Value));

            if (MaxPower.HasValue)
                json.Add(PropertyKeys.MaxPower,                      POIJSON.Number(MaxPower.Value.Value));

            if (ChargingConnectors.Length > 0)
                json.Add(PropertyKeys.ChargingConnectors,            new JArray(ChargingConnectors.Select(chargingConnector => chargingConnector.ToJSON(Embedded: true))));

            if (EnergyMeter is not null)
                json.Add(PropertyKeys.EnergyMeter,                   EnergyMeter.ToJSON(Embedded: true));

            if (ChargingTariffIds.Length > 0)
                json.Add(PropertyKeys.ChargingTariffIds,             POIJSON.IdsToJSON(ChargingTariffIds));

            if (Brands.Count > 0)
                json.Add(PropertyKeys.Brands,                        new JArray(Brands.Select(brand => brand.ToJSON(Embedded: true))));

            if (Images.Length > 0)
                json.Add(PropertyKeys.Images,                        new JArray(Images.Select(image => image.ToJSON(Embedded: true))));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,                   POIJSON.GeoCoordinateJSON(GeoLocation.Value));

            if (MaxReservationDuration.HasValue)
                json.Add(PropertyKeys.MaxReservationDuration,        POIJSON.Seconds(MaxReservationDuration.Value));

            if (ReachDistance.HasValue)
                json.Add(PropertyKeys.ReachDistance,                 POIJSON.Number(ReachDistance.Value.m));

            if (OperationTimeout.HasValue)
                json.Add(PropertyKeys.OperationTimeout,              POIJSON.Seconds(OperationTimeout.Value));

            if (ExtendedOperationTimeout.HasValue)
                json.Add(PropertyKeys.ExtendedOperationTimeout,      ExtendedOperationTimeout.Value);

            if (Standards.Length > 0)
                json.Add(PropertyKeys.Standards,                     POIJSON.IdsToJSON(Standards));

            if (FloorLevel is not null)
                json.Add(PropertyKeys.FloorLevel,                    FloorLevel);

            if (AcceptedEMobilityProviderIds.Length > 0)
                json.Add(PropertyKeys.AcceptedEMobilityProviderIds,  POIJSON.IdsToJSON(AcceptedEMobilityProviderIds));

            if (ISO15118Certificate is not null)
                json.Add(PropertyKeys.ISO15118Certificate,           ISO15118Certificate);

            if (TemplateIds.Length > 0)
                json.Add(PropertyKeys.TemplateIds,                   POIJSON.IdsToJSON(TemplateIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomEVSESerializer is not null
                       ? CustomEVSESerializer(this, json)
                       : json;

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

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "EVSE identification",
                                         EVSE_Id.TryParse,
                                         out EVSE_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the EVSE '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Name,           "EVSE name",                out var name,               out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Description,    "EVSE description",         out var description,        out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource,     "data source",              out var dataSource,         out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.PhysicalReference,  "physical reference",       out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseFlags (JSON, PropertyKeys.CurrentTypes,       "current type",             out CurrentTypes  currentTypes,  out ErrorResponse) ||
                    !POIJSON.TryParseFlags (JSON, PropertyKeys.ChargingModes,      "charging mode",            out ChargingModes chargingModes, out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.MaxVoltage, "maximum voltage", out Volt?   maxVoltage, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.MaxCurrent, "maximum current", out Ampere? maxCurrent, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (JSON.ParseOptional(PropertyKeys.MaxPower,   "maximum power",   out Watt?   maxPower,   out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIJSON.TryParseChildren<ChargingConnector>(JSON,
                                                                 PropertyKeys.ChargingConnectors,
                                                                 PropertyKeys.ChargingConnectorIds,
                                                                 "charging connectors",
                                                                 ChargingConnector.TryParse,
                                                                 out var chargingConnectors,
                                                                 out ErrorResponse))
                {
                    return false;
                }

                if (!POI.EnergyMeter.TryParseOne(JSON, PropertyKeys.EnergyMeter, out var energyMeter, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseIds(JSON, PropertyKeys.ChargingTariffIds, "charging tariff identification", ChargingTariff_Id.TryParse, out ImmutableArray<ChargingTariff_Id> chargingTariffIds, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<Brand>(JSON, PropertyKeys.Brands, PropertyKeys.BrandIds, "brands", Brand.TryParse, out var brands, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseObjects<Image>(JSON, PropertyKeys.Images, "images", Image.TryParse, out var images, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseGeoLocation(JSON, PropertyKeys.GeoLocation,
                                                       "geo location",                 out var geoLocation,             out ErrorResponse) ||
                    !POIJSON.TryParseSeconds    (JSON, PropertyKeys.MaxReservationDuration,
                                                       "maximum reservation duration", out var maxReservationDuration,  out ErrorResponse) ||
                    !POIJSON.TryParseSeconds    (JSON, PropertyKeys.OperationTimeout,
                                                       "operation timeout",            out var operationTimeout,        out ErrorResponse) ||
                    !POIJSON.TryParseNonNegative(JSON, PropertyKeys.ReachDistance,
                                                       "reach distance",               out var reachDistance,           out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.Standards, "standard", Standard.TryParse, out ImmutableArray<Standard> standards, out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, PropertyKeys.FloorLevel,           "floor level",                  out var floorLevel,              out ErrorResponse) ||
                    !POIJSON.TryParseIds        (JSON, PropertyKeys.AcceptedEMobilityProviderIds, "accepted e-mobility provider identification",
                                                 EMobilityProvider_Id.TryParse, out ImmutableArray<EMobilityProvider_Id> acceptedEMobilityProviderIds, out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, PropertyKeys.ISO15118Certificate,  "ISO 15118 certificate",        out var iso15118Certificate,     out ErrorResponse))
                {
                    return false;
                }

                if (JSON.ParseOptional(PropertyKeys.ExtendedOperationTimeout, "extended operation timeout", out Boolean? extendedOperationTimeout, out ErrorResponse))
                    if (ErrorResponse is not null) return false;

                if (!POIJSON.TryParseIds(JSON, PropertyKeys.TemplateIds, "template identification", EVSETemplate_Id.TryParse, out ImmutableArray<EVSETemplate_Id> templateIds, out ErrorResponse))
                    return false;

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
                                floorLevel,
                                acceptedEMobilityProviderIds,
                                iso15118Certificate,
                                TemplateIds:  templateIds,
                                DataSource:  dataSource,
                                Created:     created,
                                LastChange:  lastChange,
                                CustomData:  customData);

                if (!TryCheckETagsOf(JSON, EVSE, out ErrorResponse))
                {
                    EVSE = null;
                    return false;
                }

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
