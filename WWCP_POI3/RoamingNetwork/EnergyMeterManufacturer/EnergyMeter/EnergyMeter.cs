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
                                                                 Timestamped<EnergyMeterAdminStatusType>   NewStatus,
                                                                 Context?                                  DataSource);

    /// <summary>
    /// The status of an energy meter changed.
    /// </summary>
    public delegate Task OnEnergyMeterStatusChangedDelegate(DateTimeOffset                       Timestamp,
                                                            EventTracking_Id                     EventTrackingId,
                                                            EnergyMeter                          EnergyMeter,
                                                            Timestamped<EnergyMeterStatusType>   OldStatus,
                                                            Timestamped<EnergyMeterStatusType>   NewStatus,
                                                            Context?                             DataSource);


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
        /// What it measures, e.g. the grid, a PV system or a battery.
        /// </summary>
        public EnergyMeterRole?             Role            { get; }

        /// <summary>
        /// Its manufacturer, one of its roaming network.
        /// </summary>
        public EnergyMeterManufacturer_Id?  ManufacturerId  { get; }

        /// <summary>
        /// Its model, as its manufacturer names it.
        /// </summary>
        public EnergyMeterModel_Id?         ModelId         { get; }

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

        /// <summary>
        /// The transparency software releases for its measurements, with their legal status, at most one per release.
        /// </summary>
        public ImmutableArray<TransparencySoftwareAssignment>  TransparencySoftwareStatus  { get; }

        /// <summary>
        /// How it signs its measurements, e.g. OCMF; in the order of their names, each once.
        /// </summary>
        public ImmutableArray<SignatureMethod>                 SignatureMethods            { get; }

        /// <summary>
        /// The public keys it signs its measurements with, with their certificate chains; in the order of their identifications.
        /// </summary>
        public ImmutableArray<EnergyMeterPublicKey>            PublicKeys                  { get; }

        /// <summary>
        /// The energy meter templates it takes, each once, in its order: of every property they give, the first one giving it
        /// decides, unless it has its own. It names them; they do not name their members.
        /// </summary>
        public ImmutableArray<EnergyMeterTemplate_Id>  TemplateIds  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new energy meter.
        /// </summary>
        public EnergyMeter(EnergyMeter_Id                              Id,
                           I18NString?                                 Name                         = null,
                           I18NString?                                 Description                  = null,
                           EnergyMeterRole?                            Role                         = null,
                           EnergyMeterManufacturer_Id?                 ManufacturerId               = null,
                           EnergyMeterModel_Id?                        ModelId                      = null,
                           URL?                                        ModelURL                     = null,
                           String?                                     SerialNumber                 = null,
                           String?                                     HardwareVersion              = null,
                           String?                                     FirmwareVersion              = null,
                           IEnumerable<TransparencySoftwareAssignment>? TransparencySoftwareStatus  = null,
                           IEnumerable<SignatureMethod>?               SignatureMethods             = null,
                           IEnumerable<EnergyMeterPublicKey>?          PublicKeys                   = null,

                           IEnumerable<EnergyMeterTemplate_Id>?        TemplateIds                  = null,

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

            this.TemplateIds  = POIJSON.UniqueIds(TemplateIds, $"template of the energy meter '{Id}'");

            this.Role             = Role;
            this.ManufacturerId   = ManufacturerId;
            this.ModelId          = ModelId;
            this.ModelURL         = ModelURL;
            this.SerialNumber     = SerialNumber;
            this.HardwareVersion  = HardwareVersion;
            this.FirmwareVersion  = FirmwareVersion;
            this.TransparencySoftwareStatus = TransparencySoftwareAssignment.Checked(TransparencySoftwareStatus, $"the energy meter '{Id}'");
            this.SignatureMethods = POIJSON.OrderedIds(SignatureMethods, $"signature method of the energy meter '{Id}'");
            this.PublicKeys       = EnergyMeterPublicKey.Ordered(PublicKeys, $"public key of the energy meter '{Id}'");

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
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
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
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
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

            if (Role.HasValue)
                json.Add("role",             Role.Value.ToString());

            if (ManufacturerId.HasValue)
                json.Add("manufacturerId",   ManufacturerId.Value.ToString());

            if (ModelId.HasValue)
                json.Add("modelId",          ModelId.Value.ToString());

            if (ModelURL.HasValue)
                json.Add("modelURL",         ModelURL.Value.ToString());

            if (SerialNumber is not null)
                json.Add("serialNumber",     SerialNumber);

            if (HardwareVersion is not null)
                json.Add("hardwareVersion",  HardwareVersion);

            if (FirmwareVersion is not null)
                json.Add("firmwareVersion",  FirmwareVersion);

            if (TransparencySoftwareStatus.Length > 0)
                json.Add("transparencySoftwareStatus", new JArray(TransparencySoftwareStatus.Select(assignment => assignment.ToJSON())));

            if (SignatureMethods.Length > 0)
                json.Add("signatureMethods", POIJSON.IdsToJSON(SignatureMethods));

            if (PublicKeys.Length > 0)
                json.Add("publicKeys",       new JArray(PublicKeys.Select(publicKey => publicKey.ToJSON())));

            if (TemplateIds.Length > 0)
                json.Add("templateIds", POIJSON.IdsToJSON(TemplateIds));

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
                    !POIJSON.TryParseText  (JSON, "role",             "energy meter role",           EnergyMeterRole.           TryParse, out EnergyMeterRole?            role,           out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "manufacturerId",   "manufacturer identification", EnergyMeterManufacturer_Id.TryParse, out EnergyMeterManufacturer_Id? manufacturerId, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "modelId",          "model identification",        EnergyMeterModel_Id.       TryParse, out EnergyMeterModel_Id?        modelId,        out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, "modelURL",         "model URL",                 URL.TryParse, out URL? modelURL,        out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "serialNumber",     "serial number",             out var serialNumber,     out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "hardwareVersion",  "hardware version",          out var hardwareVersion,  out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, "firmwareVersion",  "firmware version",          out var firmwareVersion,  out ErrorResponse) ||
                    !POIJSON.TryParseIds<SignatureMethod>(JSON, "signatureMethods", "signature method",          SignatureMethod.TryParse,      out var signatureMethods, out ErrorResponse) ||
                    !POIJSON.TryParseObjects<EnergyMeterPublicKey>(JSON, "publicKeys",      "public key",                EnergyMeterPublicKey.TryParse, out var publicKeys,       out ErrorResponse))
                {
                    return false;
                }

                var transparencySoftwareStatus = new List<TransparencySoftwareAssignment>();

                if (JSON["transparencySoftwareStatus"] is JToken transparencySoftwareStatusJSON)
                {

                    if (transparencySoftwareStatusJSON is not JArray transparencySoftwareStatusArray)
                    {
                        ErrorResponse = "The transparency software status of an energy meter must be a JSON array!";
                        return false;
                    }

                    foreach (var assignmentJSON in transparencySoftwareStatusArray)
                    {

                        if (assignmentJSON is not JObject assignmentObject)
                        {
                            ErrorResponse = "A transparency software status must be a JSON object!";
                            return false;
                        }

                        if (!TransparencySoftwareAssignment.TryParse(assignmentObject, out var assignment, out ErrorResponse))
                            return false;

                        transparencySoftwareStatus.Add(assignment);

                    }

                }

                if (!POIJSON.TryParseIds(JSON, "templateIds", "template identification", EnergyMeterTemplate_Id.TryParse, out ImmutableArray<EnergyMeterTemplate_Id> templateIds, out ErrorResponse))
                    return false;

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
                                              manufacturerId,
                                              modelId,
                                              modelURL,
                                              serialNumber,
                                              hardwareVersion,
                                              firmwareVersion,
                                              transparencySoftwareStatus,
                                              signatureMethods,
                                              publicKeys,
                                              TemplateIds:  templateIds,
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
