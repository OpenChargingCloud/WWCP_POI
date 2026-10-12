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
    /// The admin status of an energy meter changed.
    /// </summary>
    public delegate Task OnEnergyMeterAdminStatusChangedDelegate(DateTimeOffset                            Timestamp,
                                                                 EventTracking_Id                          EventTrackingId,
                                                                 EnergyMeter                               EnergyMeter,
                                                                 Timestamped<EnergyMeterAdminStatusType>   OldStatus,
                                                                 Timestamped<EnergyMeterAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of an energy meter changed.
    /// </summary>
    public delegate Task OnEnergyMeterStatusChangedDelegate(DateTimeOffset                       Timestamp,
                                                            EventTracking_Id                     EventTrackingId,
                                                            EnergyMeter                          EnergyMeter,
                                                            Timestamped<EnergyMeterStatusType>   OldStatus,
                                                            Timestamped<EnergyMeterStatusType>   NewStatus);


    /// <summary>
    /// An energy meter, embedded completely in what it measures: a charging pool, its grid connection point,
    /// a charging station or an EVSE. Its identification is unique within the roaming network.
    /// </summary>
    public sealed class EnergyMeter : AImmutableEMobilityEntity<EnergyMeter_Id,
                                                                EnergyMeterAdminStatusType,
                                                                EnergyMeterStatusType>,
                                      IEquatable<EnergyMeter>,
                                      IComparable<EnergyMeter>
    {

        #region Properties

        /// <summary>
        /// What it measures, e.g. "grid", "pv" or "battery".
        /// </summary>
        public String?  Role               { get; }

        /// <summary>
        /// The name of its manufacturer.
        /// </summary>
        public String?  Manufacturer       { get; }

        /// <summary>
        /// The homepage of its manufacturer.
        /// </summary>
        public URL?     ManufacturerURL    { get; }

        /// <summary>
        /// The name of its model.
        /// </summary>
        public String?  Model              { get; }

        /// <summary>
        /// The homepage of its model, e.g. with its data sheet.
        /// </summary>
        public URL?     ModelURL           { get; }

        /// <summary>
        /// Its serial number.
        /// </summary>
        public String?  SerialNumber       { get; }

        /// <summary>
        /// Its hardware version.
        /// </summary>
        public String?  HardwareVersion    { get; }

        /// <summary>
        /// Its firmware version.
        /// </summary>
        public String?  FirmwareVersion    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter.
        /// </summary>
        public EnergyMeter(EnergyMeter_Id                              Id,
                           I18NString?                                 Name                         = null,
                           I18NString?                                 Description                  = null,
                           String?                                     Role                         = null,
                           String?                                     Manufacturer                 = null,
                           URL?                                        ManufacturerURL              = null,
                           String?                                     Model                        = null,
                           URL?                                        ModelURL                     = null,
                           String?                                     SerialNumber                 = null,
                           String?                                     HardwareVersion              = null,
                           String?                                     FirmwareVersion              = null,

                           Timestamped<EnergyMeterAdminStatusType>?    InitialAdminStatus           = null,
                           Timestamped<EnergyMeterStatusType>?         InitialStatus                = null,
                           UInt16?                                     MaxAdminStatusScheduleSize   = null,
                           UInt16?                                     MaxStatusScheduleSize        = null,

                           String?                                     DataSource                   = null,
                           DateTimeOffset?                             Created                      = null,
                           DateTimeOffset?                             LastChange                   = null,

                           CustomDataNew?                              CustomData                   = null,
                           UserDefinedDictionary?                      InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<EnergyMeterAdminStatusType>(Created ?? Timestamp.Now, EnergyMeterAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<EnergyMeterStatusType>     (Created ?? Timestamp.Now, EnergyMeterStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.Role             = Role;
            this.Manufacturer     = Manufacturer;
            this.ManufacturerURL  = ManufacturerURL;
            this.Model            = Model;
            this.ModelURL         = ModelURL;
            this.SerialNumber     = SerialNumber;
            this.HardwareVersion  = HardwareVersion;
            this.FirmwareVersion  = FirmwareVersion;

        }

        #endregion


        #region Status events

        private readonly Lock                                      statusEventsLock  = new ();
        private          OnEnergyMeterAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnEnergyMeterStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnEnergyMeterAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnEnergyMeterStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                           Timestamp,
                                                                   EventTracking_Id                         EventTrackingId,
                                                                   Timestamped<EnergyMeterAdminStatusType>  NewStatus,
                                                                   Timestamped<EnergyMeterAdminStatusType>  OldStatus,
                                                                   Context?                                 DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                      Timestamp,
                                                              EventTracking_Id                    EventTrackingId,
                                                              Timestamped<EnergyMeterStatusType>  NewStatus,
                                                              Timestamped<EnergyMeterStatusType>  OldStatus,
                                                              Context?                            DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this energy meter, as what it measures embeds it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",             Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",      Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",       DataSource);

            if (Role is not null)
                json.Add("role",             Role);

            if (Manufacturer is not null)
                json.Add("manufacturer",     Manufacturer);

            if (ManufacturerURL.HasValue)
                json.Add("manufacturerURL",  ManufacturerURL.Value.ToString());

            if (Model is not null)
                json.Add("model",            Model);

            if (ModelURL.HasValue)
                json.Add("modelURL",         ModelURL.Value.ToString());

            if (SerialNumber is not null)
                json.Add("serialNumber",     SerialNumber);

            if (HardwareVersion is not null)
                json.Add("hardwareVersion",  HardwareVersion);

            if (FirmwareVersion is not null)
                json.Add("firmwareVersion",  FirmwareVersion);

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out EnergyMeter, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an energy meter.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EnergyMeter">The parsed energy meter.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                JSON,
                                       [NotNullWhen(true)]  out EnergyMeter?  EnergyMeter,
                                       [NotNullWhen(false)] out String?       ErrorResponse)
        {

            EnergyMeter = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "energy meter identification",
                                         EnergyMeter_Id.TryParse,
                                         out EnergyMeter_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, "name",             "energy meter name",         out var name,             out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, "description",      "energy meter description",  out var description,      out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "dataSource",       "data source",               out var dataSource,       out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "role",             "energy meter role",         out var role,             out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "manufacturer",     "manufacturer",              out var manufacturer,     out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "manufacturerURL",  "manufacturer URL",          URL.TryParse, out URL? manufacturerURL, out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "model",            "model",                     out var model,            out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "modelURL",         "model URL",                 URL.TryParse, out URL? modelURL,        out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "serialNumber",     "serial number",             out var serialNumber,     out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "hardwareVersion",  "hardware version",          out var hardwareVersion,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "firmwareVersion",  "firmware version",          out var firmwareVersion,  out ErrorResponse))
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

                EnergyMeter = new EnergyMeter(id,
                                              name,
                                              description,
                                              role,
                                              manufacturer,
                                              manufacturerURL,
                                              model,
                                              modelURL,
                                              serialNumber,
                                              hardwareVersion,
                                              firmwareVersion,
                                              DataSource:  dataSource,
                                              Created:     created,
                                              LastChange:  lastChange,
                                              CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EnergyMeter    = null;
                ErrorResponse  = "The given JSON representation of an energy meter is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParseOne(JSON, PropertyName, out EnergyMeter, out ErrorResponse)

        /// <summary>
        /// Try to parse the optional energy meter embedded in the given property.
        /// </summary>
        internal static Boolean TryParseOne(JObject                           JSON,
                                            String                            PropertyName,
                                            out EnergyMeter?                  EnergyMeter,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            EnergyMeter = null;

            if (JSON[PropertyName] is not JToken token)
            {
                ErrorResponse = null;
                return true;
            }

            if (token is not JObject meterJSON)
            {
                ErrorResponse = "The energy meter must be a JSON object!";
                return false;
            }

            return TryParse(meterJSON, out EnergyMeter, out ErrorResponse);

        }

        #endregion


        #region (static) Ordered    (EnergyMeters, Owner)

        /// <summary>
        /// The given energy meters of one owner in the order of their identifications,
        /// without null entries and without one given twice.
        /// </summary>
        internal static ImmutableArray<EnergyMeter> Ordered(IEnumerable<EnergyMeter>?  EnergyMeters,
                                                            String                     Owner)
        {

            var energyMeters = (EnergyMeters ?? []).ToArray();

            if (energyMeters.Any(energyMeter => energyMeter is null))
                throw new ArgumentException($"The energy meters of {Owner} must not contain null entries!", nameof(EnergyMeters));

            if (energyMeters.Select(energyMeter => energyMeter.Id).Distinct().Count() != energyMeters.Length)
                throw new ArgumentException($"An energy meter of {Owner} must not be given twice!", nameof(EnergyMeters));

            return [.. energyMeters.OrderBy(energyMeter => energyMeter.Id.ToString(), StringComparer.Ordinal)];

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two energy meters for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EnergyMeter? EnergyMeter1, EnergyMeter? EnergyMeter2)

            => ReferenceEquals(EnergyMeter1, EnergyMeter2) ||
               (EnergyMeter1 is not null && EnergyMeter1.Equals(EnergyMeter2));

        /// <summary>
        /// Compares two energy meters for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EnergyMeter? EnergyMeter1, EnergyMeter? EnergyMeter2)

            => !(EnergyMeter1 == EnergyMeter2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is EnergyMeter energyMeter
                   ? CompareTo(energyMeter)
                   : throw new ArgumentException("The given object is not an energy meter!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EnergyMeter? EnergyMeter)

            => EnergyMeter is null
                   ? throw new ArgumentNullException(nameof(EnergyMeter), "The given energy meter must not be null!")
                   : Id.CompareTo(EnergyMeter.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EnergyMeter energyMeter &&
               Equals(energyMeter);

        /// <inheritdoc/>
        public Boolean Equals(EnergyMeter? EnergyMeter)

            => EnergyMeter is not null &&
               Id.Equals(EnergyMeter.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
