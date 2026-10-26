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

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/energyMeter/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its role.
            /// </summary>
            public const String  Role                         = "role";

            /// <summary>
            /// The property of its manufacturer.
            /// </summary>
            public const String  ManufacturerId               = "manufacturerId";

            /// <summary>
            /// The property of its model.
            /// </summary>
            public const String  ModelId                      = "modelId";

            /// <summary>
            /// The property of the homepage of its model.
            /// </summary>
            public const String  ModelURL                     = "modelURL";

            /// <summary>
            /// The property of its serial number.
            /// </summary>
            public const String  SerialNumber                 = "serialNumber";

            /// <summary>
            /// The property of its hardware revision.
            /// </summary>
            public const String  HardwareRevision             = "hardwareRevision";

            /// <summary>
            /// The property of its firmware version.
            /// </summary>
            public const String  FirmwareVersion              = "firmwareVersion";

            /// <summary>
            /// The property of its transparency software releases with their legal status.
            /// </summary>
            public const String  TransparencySoftwareStatus   = "transparencySoftwareStatus";

            /// <summary>
            /// The property of its signature methods.
            /// </summary>
            public const String  SignatureMethods             = "signatureMethods";

            /// <summary>
            /// The property of its public keys.
            /// </summary>
            public const String  PublicKeys                   = "publicKeys";

            /// <summary>
            /// The property of the identifications of the certificates of its model it names.
            /// </summary>
            public const String  CertificateIds               = "certificateIds";

            /// <summary>
            /// The property of its templates.
            /// </summary>
            public const String  TemplateIds                  = "templateIds";

        }

        #endregion


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
        /// The hardware revision it was built in, one of its model.
        /// </summary>
        public HardwareRevision?  HardwareRevision  { get; }

        /// <summary>
        /// The firmware version it runs, one of its model.
        /// </summary>
        public FirmwareVersion?   FirmwareVersion   { get; }

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
        /// The certificates of conformity of its model, in its hardware revision and firmware version, of the roaming
        /// network, in the order of their identifications.
        /// </summary>
        public ImmutableArray<ConformityCertificate_Id>        CertificateIds              { get; }

        /// <summary>
        /// The energy meter templates it takes, each once, in its order: of every property they give, the first one giving it
        /// decides, unless it has its own. It names them; they do not name their members.
        /// </summary>
        public ImmutableArray<EnergyMeterTemplate_Id>          TemplateIds                 { get; }

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
                           HardwareRevision?                           HardwareRevision             = null,
                           FirmwareVersion?                            FirmwareVersion              = null,
                           IEnumerable<TransparencySoftwareAssignment>? TransparencySoftwareStatus  = null,
                           IEnumerable<SignatureMethod>?               SignatureMethods             = null,
                           IEnumerable<EnergyMeterPublicKey>?          PublicKeys                   = null,
                           IEnumerable<ConformityCertificate_Id>?      CertificateIds               = null,

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
            this.HardwareRevision = HardwareRevision;
            this.FirmwareVersion  = FirmwareVersion;
            this.TransparencySoftwareStatus = TransparencySoftwareAssignment.Checked(TransparencySoftwareStatus, $"the energy meter '{Id}'");
            this.SignatureMethods = POIJSON.OrderedIds(SignatureMethods, $"signature method of the energy meter '{Id}'");
            this.PublicKeys       = EnergyMeterPublicKey.Ordered(PublicKeys, $"public key of the energy meter '{Id}'");
            this.CertificateIds   = POIJSON.OrderedIds(CertificateIds, $"certificate of the energy meter '{Id}'");

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


        #region (protected override) ContentJSON()

        /// <inheritdoc/>
        protected override JObject ContentJSON()

            => ToJSON(Embedded: true);

        #endregion

        #region ToJSON(Embedded = false, CustomEnergyMeterSerializer = null)

        /// <summary>
        /// Return the JSON representation of this energy meter, as what it measures embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomEnergyMeterSerializer">A delegate to serialize custom energy meter JSON objects.</param>
        public JObject ToJSON(Boolean                                        Embedded                      = false,
                              Boolean                                        IncludeETags                  = false,
                              ETagDigestEncoding?                            ETagDigestEncoding            = null,
                              CustomJObjectSerializerDelegate<EnergyMeter>?  CustomEnergyMeterSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id, Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,                 JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,                    Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,             Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,              DataSource);

            if (Role.HasValue)
                json.Add(PropertyKeys.Role,                        Role.Value.ToString());

            if (ManufacturerId.HasValue)
                json.Add(PropertyKeys.ManufacturerId,              ManufacturerId.Value.ToString());

            if (ModelId.HasValue)
                json.Add(PropertyKeys.ModelId,                     ModelId.Value.ToString());

            if (ModelURL.HasValue)
                json.Add(PropertyKeys.ModelURL,                    ModelURL.Value.ToString());

            if (SerialNumber is not null)
                json.Add(PropertyKeys.SerialNumber,                SerialNumber);

            if (HardwareRevision.HasValue)
                json.Add(PropertyKeys.HardwareRevision,             HardwareRevision.Value.ToString());

            if (FirmwareVersion.HasValue)
                json.Add(PropertyKeys.FirmwareVersion,             FirmwareVersion.Value.ToString());

            if (TransparencySoftwareStatus.Length > 0)
                json.Add(PropertyKeys.TransparencySoftwareStatus,  new JArray(TransparencySoftwareStatus.Select(assignment => assignment.ToJSON(Embedded: true))));

            if (SignatureMethods.Length > 0)
                json.Add(PropertyKeys.SignatureMethods,            POIJSON.IdsToJSON(SignatureMethods));

            if (PublicKeys.Length > 0)
                json.Add(PropertyKeys.PublicKeys,                  new JArray(PublicKeys.Select(publicKey => publicKey.ToJSON(Embedded: true))));

            if (CertificateIds.Length > 0)
                json.Add(PropertyKeys.CertificateIds,              POIJSON.IdsToJSON(CertificateIds));

            if (TemplateIds.Length > 0)
                json.Add(PropertyKeys.TemplateIds,                 POIJSON.IdsToJSON(TemplateIds));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomEnergyMeterSerializer is not null
                       ? CustomEnergyMeterSerializer(this, json)
                       : json;

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

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "energy meter identification",
                                         EnergyMeter_Id.TryParse,
                                         out EnergyMeter_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the energy meter '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Name,             "energy meter name",         out var name,             out ErrorResponse) ||
                    !POIJSON.TryParseI18N  (JSON, BasePropertyKeys.Description,      "energy meter description",  out var description,      out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, BasePropertyKeys.DataSource,       "data source",               out var dataSource,       out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.Role,                 "energy meter role",           EnergyMeterRole.           TryParse, out EnergyMeterRole?            role,           out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.ManufacturerId,       "manufacturer identification", EnergyMeterManufacturer_Id.TryParse, out EnergyMeterManufacturer_Id? manufacturerId, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.ModelId,              "model identification",        EnergyMeterModel_Id.       TryParse, out EnergyMeterModel_Id?        modelId,        out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.ModelURL,             "model URL",                 URL.TryParse, out URL? modelURL,        out ErrorResponse) ||
                    !POIJSON.TryParseString(JSON, PropertyKeys.SerialNumber,         "serial number",             out var serialNumber,     out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.HardwareRevision,     "hardware revision",         POI.HardwareRevision.TryParse, out POI.HardwareRevision? hardwareRevision, out ErrorResponse) ||
                    !POIJSON.TryParseText  (JSON, PropertyKeys.FirmwareVersion,      "firmware version",          POI.FirmwareVersion. TryParse, out POI.FirmwareVersion?  firmwareVersion,  out ErrorResponse) ||
                    !POIJSON.TryParseIds<SignatureMethod>(JSON, PropertyKeys.SignatureMethods, "signature method",          SignatureMethod.TryParse,      out var signatureMethods, out ErrorResponse) ||
                    !POIJSON.TryParseObjects<EnergyMeterPublicKey>(JSON, PropertyKeys.PublicKeys,      "public key",                EnergyMeterPublicKey.TryParse, out var publicKeys,       out ErrorResponse) ||
                    !POIJSON.TryParseIds    (JSON, PropertyKeys.CertificateIds,  "certificate identification", ConformityCertificate_Id.TryParse, out ImmutableArray<ConformityCertificate_Id> certificateIds, out ErrorResponse))
                {
                    return false;
                }

                var transparencySoftwareStatus = new List<TransparencySoftwareAssignment>();

                if (JSON[PropertyKeys.TransparencySoftwareStatus] is JToken transparencySoftwareStatusJSON)
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

                if (!POIJSON.TryParseIds(JSON, PropertyKeys.TemplateIds, "template identification", EnergyMeterTemplate_Id.TryParse, out ImmutableArray<EnergyMeterTemplate_Id> templateIds, out ErrorResponse))
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
                                              hardwareRevision,
                                              firmwareVersion,
                                              transparencySoftwareStatus,
                                              signatureMethods,
                                              publicKeys,
                                              certificateIds,
                                              TemplateIds:  templateIds,
                                              DataSource:  dataSource,
                                              Created:     created,
                                              LastChange:  lastChange,
                                              CustomData:  customData);

                if (!TryCheckETagsOf(JSON, EnergyMeter, out ErrorResponse))
                {
                    EnergyMeter = null;
                    return false;
                }

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
