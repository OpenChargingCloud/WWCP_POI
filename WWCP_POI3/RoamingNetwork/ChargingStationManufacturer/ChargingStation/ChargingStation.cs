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
    /// The admin status of a charging station changed.
    /// </summary>
    public delegate Task OnChargingStationAdminStatusChangedDelegate(DateTimeOffset                                Timestamp,
                                                                     EventTracking_Id                              EventTrackingId,
                                                                     ChargingStation                               ChargingStation,
                                                                     Timestamped<ChargingStationAdminStatusType>   OldStatus,
                                                                     Timestamped<ChargingStationAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of a charging station changed.
    /// </summary>
    public delegate Task OnChargingStationStatusChangedDelegate(DateTimeOffset                           Timestamp,
                                                                EventTracking_Id                         EventTrackingId,
                                                                ChargingStation                          ChargingStation,
                                                                Timestamped<ChargingStationStatusType>   OldStatus,
                                                                Timestamped<ChargingStationStatusType>   NewStatus);


    /// <summary>
    /// A charging station: one device with its EVSEs, embedded completely in its charging pool.
    /// </summary>
    public sealed class ChargingStation : AImmutableEMobilityEntity<ChargingStation_Id,
                                                                    ChargingStationAdminStatusType,
                                                                    ChargingStationStatusType>,
                                          IEquatable<ChargingStation>,
                                          IComparable<ChargingStation>
    {

        #region Data

        private readonly ImmutableDictionary<EVSE_Id, EVSE> evseIndex;

        #endregion

        #region Properties

        /// <summary>
        /// The reference printed on the charging station, e.g. "Station 2".
        /// </summary>
        public String?               PhysicalReference     { get; }

        /// <summary>
        /// The address, when it differs from the one of its charging pool.
        /// </summary>
        public Address?              Address               { get; }

        /// <summary>
        /// The geographical location, when it differs from the one of its charging pool.
        /// </summary>
        public GeoCoordinate?        GeoLocation           { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?          HotlinePhoneNumber    { get; }

        /// <summary>
        /// The manufacturer, one of its roaming network.
        /// </summary>
        public ChargingStationManufacturer_Id?  ManufacturerId  { get; }

        /// <summary>
        /// The model, as its manufacturer names it.
        /// </summary>
        public ChargingStationModel_Id?         ModelId         { get; }

        /// <summary>
        /// The EVSEs, in the order of their identifications.
        /// </summary>
        public ImmutableArray<EVSE>  EVSEs                 { get; }

        /// <summary>
        /// Its own energy meters, in the order of their identifications; not those of its EVSEs.
        /// </summary>
        public ImmutableArray<EnergyMeter>  EnergyMeters  { get; }

        /// <summary>
        /// Its own energy meters and those of its EVSEs.
        /// </summary>
        public IEnumerable<EnergyMeter>     AllEnergyMeters
            => EnergyMeters.Concat(EVSEs.SelectMany(evse => evse.AllEnergyMeters));

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station.
        /// </summary>
        public ChargingStation(ChargingStation_Id                              Id,
                               I18NString?                                     Name                         = null,
                               I18NString?                                     Description                  = null,
                               String?                                         PhysicalReference            = null,
                               Address?                                        Address                      = null,
                               GeoCoordinate?                                  GeoLocation                  = null,
                               PhoneNumber?                                    HotlinePhoneNumber           = null,
                               ChargingStationManufacturer_Id?                 ManufacturerId               = null,
                               ChargingStationModel_Id?                        ModelId                      = null,
                               IEnumerable<EVSE>?                              EVSEs                        = null,
                               IEnumerable<EnergyMeter>?                       EnergyMeters                 = null,

                               Timestamped<ChargingStationAdminStatusType>?    InitialAdminStatus           = null,
                               Timestamped<ChargingStationStatusType>?         InitialStatus                = null,
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
                   InitialAdminStatus         ?? new Timestamped<ChargingStationAdminStatusType>(Created ?? Timestamp.Now, ChargingStationAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingStationStatusType>     (Created ?? Timestamp.Now, ChargingStationStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            var evses = (EVSEs ?? []).ToArray();

            if (evses.Any(evse => evse is null))
                throw new ArgumentException("EVSEs must not contain null entries!", nameof(EVSEs));

            foreach (var evse in evses)
                if (evse.Id.OperatorId != Id.OperatorId)
                    throw new ArgumentException($"The EVSE '{evse.Id}' does not belong to the operator '{Id.OperatorId}' of the charging station '{Id}'!", nameof(EVSEs));

            if (evses.Select(evse => evse.Id).Distinct().Count() != evses.Length)
                throw new ArgumentException($"An EVSE of the charging station '{Id}' must not be given twice!", nameof(EVSEs));

            this.PhysicalReference   = PhysicalReference;
            this.Address             = Address;
            this.GeoLocation         = GeoLocation;
            this.HotlinePhoneNumber  = HotlinePhoneNumber;
            this.ManufacturerId      = ManufacturerId;
            this.ModelId             = ModelId;
            this.EVSEs               = [.. evses.OrderBy(evse => evse.Id.ToString(), StringComparer.Ordinal)];
            this.evseIndex           = this.EVSEs.ToImmutableDictionary(evse => evse.Id);
            this.EnergyMeters        = EnergyMeter.Ordered(EnergyMeters, $"the charging station '{Id}'");

        }

        #endregion


        #region TryGetEVSE(EVSEId, out EVSE)

        /// <summary>
        /// Try to return the EVSE with the given identification.
        /// </summary>
        public Boolean TryGetEVSE(EVSE_Id                         EVSEId,
                                  [NotNullWhen(true)] out EVSE?  EVSE)

            => evseIndex.TryGetValue(EVSEId, out EVSE);

        #endregion


        #region Status events

        private readonly Lock                                          statusEventsLock  = new ();
        private          OnChargingStationAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingStationStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingStationAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingStationStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                               Timestamp,
                                                                   EventTracking_Id                             EventTrackingId,
                                                                   Timestamped<ChargingStationAdminStatusType>  NewStatus,
                                                                   Timestamped<ChargingStationAdminStatusType>  OldStatus,
                                                                   Context?                                     DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                          Timestamp,
                                                              EventTracking_Id                        EventTrackingId,
                                                              Timestamped<ChargingStationStatusType>  NewStatus,
                                                              Timestamped<ChargingStationStatusType>  OldStatus,
                                                              Context?                                DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this charging station, as its charging pool embeds it: with its EVSEs.
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

            if (Address is not null)
                json.Add("address",            Address.ToJSON(Embedded: true));

            if (GeoLocation.HasValue)
                json.Add("geoLocation",        GeoLocation.Value.ToJSON(Embedded: true));

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",            HotlinePhoneNumber.Value.ToString());

            if (ManufacturerId.HasValue)
                json.Add("manufacturerId",     ManufacturerId.Value.ToString());

            if (ModelId.HasValue)
                json.Add("modelId",            ModelId.Value.ToString());

            if (EnergyMeters.Length > 0)
                json.Add("energyMeters",       new JArray(EnergyMeters.Select(energyMeter => energyMeter.ToJSON())));

            if (EVSEs.Length > 0)
                json.Add("EVSEs",              new JArray(EVSEs.Select(evse => evse.ToJSON())));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out ChargingStation, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station, with its EVSEs.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStation">The parsed charging station.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                    JSON,
                                       [NotNullWhen(true)]  out ChargingStation?  ChargingStation,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            ChargingStation = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "charging station identification",
                                         ChargingStation_Id.TryParse,
                                         out ChargingStation_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N       (JSON, "name",               "charging station name",         out var name,               out ErrorResponse) ||
                    !POIJSON.TryParseI18N       (JSON, "description",        "charging station description",  out var description,        out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "dataSource",         "data source",                   out var dataSource,         out ErrorResponse) ||
                    !POIJSON.TryParseString     (JSON, "physicalReference",  "physical reference",            out var physicalReference,  out ErrorResponse) ||
                    !POIJSON.TryParseAddress    (JSON,                                                        out var address,            out ErrorResponse) ||
                    !POIJSON.TryParseGeoLocation(JSON,                                                        out var geoLocation,        out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "hotline",            "hotline phone number",          PhoneNumber.TryParse,       out PhoneNumber? hotline,  out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "manufacturerId",     "manufacturer identification",   ChargingStationManufacturer_Id.TryParse, out ChargingStationManufacturer_Id? manufacturerId, out ErrorResponse) ||
                    !POIJSON.TryParseText       (JSON, "modelId",            "model identification",          ChargingStationModel_Id.       TryParse, out ChargingStationModel_Id?        modelId,        out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<EVSE>(JSON,
                                                    "EVSEs",
                                                    "EVSEIds",
                                                    "EVSEs",
                                                    EVSE.TryParse,
                                                    out var evses,
                                                    out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<EnergyMeter>(JSON,
                                                           "energyMeters",
                                                           "energyMeterIds",
                                                           "energy meters",
                                                           EnergyMeter.TryParse,
                                                           out var energyMeters,
                                                           out ErrorResponse))
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

                ChargingStation = new ChargingStation(id,
                                                      name,
                                                      description,
                                                      physicalReference,
                                                      address,
                                                      geoLocation,
                                                      hotline,
                                                      manufacturerId,
                                                      modelId,
                                                      evses,
                                                      energyMeters,
                                                      DataSource:  dataSource,
                                                      Created:     created,
                                                      LastChange:  lastChange,
                                                      CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStation  = null;
                ErrorResponse    = "The given JSON representation of a charging station is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging stations for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingStation? ChargingStation1, ChargingStation? ChargingStation2)

            => ReferenceEquals(ChargingStation1, ChargingStation2) ||
               (ChargingStation1 is not null && ChargingStation1.Equals(ChargingStation2));

        /// <summary>
        /// Compares two charging stations for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingStation? ChargingStation1, ChargingStation? ChargingStation2)

            => !(ChargingStation1 == ChargingStation2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingStation chargingStation
                   ? CompareTo(chargingStation)
                   : throw new ArgumentException("The given object is not a charging station!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingStation? ChargingStation)

            => ChargingStation is null
                   ? throw new ArgumentNullException(nameof(ChargingStation), "The given charging station must not be null!")
                   : Id.CompareTo(ChargingStation.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStation chargingStation &&
               Equals(chargingStation);

        /// <inheritdoc/>
        public Boolean Equals(ChargingStation? ChargingStation)

            => ChargingStation is not null &&
               Id.Equals(ChargingStation.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
