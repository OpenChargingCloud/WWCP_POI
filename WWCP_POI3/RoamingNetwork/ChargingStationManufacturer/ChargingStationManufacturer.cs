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
using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Hermod.Mail;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of a charging station manufacturer changed.
    /// </summary>
    public delegate Task OnChargingStationManufacturerAdminStatusChangedDelegate(DateTimeOffset                                            Timestamp,
                                                                                 EventTracking_Id                                          EventTrackingId,
                                                                                 ChargingStationManufacturer                               ChargingStationManufacturer,
                                                                                 Timestamped<ChargingStationManufacturerAdminStatusType>   OldStatus,
                                                                                 Timestamped<ChargingStationManufacturerAdminStatusType>   NewStatus,
                                                                                 Context?                                                  DataSource);

    /// <summary>
    /// The status of a charging station manufacturer changed.
    /// </summary>
    public delegate Task OnChargingStationManufacturerStatusChangedDelegate(DateTimeOffset                                       Timestamp,
                                                                            EventTracking_Id                                     EventTrackingId,
                                                                            ChargingStationManufacturer                          ChargingStationManufacturer,
                                                                            Timestamped<ChargingStationManufacturerStatusType>   OldStatus,
                                                                            Timestamped<ChargingStationManufacturerStatusType>   NewStatus,
                                                                            Context?                                             DataSource);


    /// <summary>
    /// A manufacturer of charging stations, a party of its roaming network: the roaming network operator adds and removes it,
    /// with its keys; it changes its own properties and its models, signed by its own keys. Its models are embedded in
    /// it; charging stations, their templates and transparency software certificates name a model by its manufacturer and its identification, unique within its manufacturer.
    /// </summary>
    public sealed class ChargingStationManufacturer : AImmutableEMobilityEntity<ChargingStationManufacturer_Id,
                                                                                ChargingStationManufacturerAdminStatusType,
                                                                                ChargingStationManufacturerStatusType>,
                                                      IEquatable<ChargingStationManufacturer>,
                                                      IComparable<ChargingStationManufacturer>,
                                                      IPOIParty
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/chargingStationManufacturer/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its logos: at most one, as of a charging station operator.
            /// </summary>
            public const String  Logos                = POIJSON.ContactKeys.Logos;

            /// <summary>
            /// The property of the URI of its logo.
            /// </summary>
            public const String  LogoURI              = POIJSON.ContactKeys.LogoURI;

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address              = POIJSON.ContactKeys.Address;

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation          = POIJSON.ContactKeys.GeoLocation;

            /// <summary>
            /// The property of its telephone number.
            /// </summary>
            public const String  Telephone            = POIJSON.ContactKeys.Telephone;

            /// <summary>
            /// The property of its e-mail address.
            /// </summary>
            public const String  EMailAddress         = POIJSON.ContactKeys.EMailAddress;

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage             = POIJSON.ContactKeys.Homepage;

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber   = POIJSON.ContactKeys.HotlinePhoneNumber;

            /// <summary>
            /// The property of the URL of its terms and conditions.
            /// </summary>
            public const String  TermsAndConditions   = POIJSON.ContactKeys.TermsAndConditions;

            /// <summary>
            /// The property of its models.
            /// </summary>
            public const String  Models               = "chargingStationModels";

            /// <summary>
            /// The property naming its models by their identifications only, refused: they are embedded completely.
            /// </summary>
            public const String  ModelIds             = "chargingStationModelIds";

            /// <summary>
            /// The property of its signing keys.
            /// </summary>
            public const String  SigningKeys          = POIPartyKeys.SigningKeysProperty;

            /// <summary>
            /// The property of its identity keys.
            /// </summary>
            public const String  IdentityKeys         = POIPartyKeys.IdentityKeysProperty;

        }

        #endregion


        /// <summary>
        /// The URL of its logo.
        /// </summary>
        public URL?                          Logo                     { get; }

        /// <summary>
        /// The address of the headquarters.
        /// </summary>
        public Address?                      Address                  { get; }

        /// <summary>
        /// The geographical location of the headquarters.
        /// </summary>
        public GeoCoordinate?                GeoLocation              { get; }

        /// <summary>
        /// The telephone number of its (sales) office.
        /// </summary>
        public PhoneNumber?                  Telephone                { get; }

        /// <summary>
        /// The e-mail address of its (sales) office, also with a name: "Example Office &lt;info@example.org&gt;".
        /// </summary>
        public EMailAddress?                 EMailAddress             { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?                          Homepage                 { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?                  HotlinePhoneNumber       { get; }

        /// <summary>
        /// The URL of its terms and conditions.
        /// </summary>
        public URL?                          TermsAndConditionsURL    { get; }

        /// <summary>
        /// The licenses of its data, ordered by their identifications.
        /// </summary>
        public ImmutableArray<DataLicense>   DataLicenses             { get; }

        /// <summary>
        /// Its models, ordered by their identifications.
        /// </summary>
        public POICollection<ChargingStationModel_Id, ChargingStationModel>  Models  { get; }

        /// <summary>
        /// Its signing keys: it signs its own properties and its models with them.
        /// </summary>
        public ImmutableArray<POIPublicKey>  SigningKeys              { get; }

        /// <summary>
        /// Its identity keys, signing changes of its keys.
        /// </summary>
        public ImmutableArray<POIPublicKey>  IdentityKeys             { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new charging station manufacturer.
        /// </summary>
        public ChargingStationManufacturer(ChargingStationManufacturer_Id                              Id,
                                           I18NString?                                                 Name                         = null,
                                           I18NString?                                                 Description                  = null,

                                           URL?                                                        Logo                         = null,
                                           Address?                                                    Address                      = null,
                                           GeoCoordinate?                                              GeoLocation                  = null,
                                           PhoneNumber?                                                Telephone                    = null,
                                           EMailAddress?                                               EMailAddress                 = null,
                                           URL?                                                        Homepage                     = null,
                                           PhoneNumber?                                                HotlinePhoneNumber           = null,
                                           URL?                                                        TermsAndConditionsURL        = null,
                                           IEnumerable<DataLicense>?                                   DataLicenses                 = null,

                                           IEnumerable<ChargingStationModel>?                          Models                       = null,

                                           IEnumerable<POIPublicKey>?                                  IdentityKeys                 = null,
                                           IEnumerable<POIPublicKey>?                                  SigningKeys                  = null,

                                           Timestamped<ChargingStationManufacturerAdminStatusType>?    InitialAdminStatus           = null,
                                           Timestamped<ChargingStationManufacturerStatusType>?         InitialStatus                = null,
                                           UInt16?                                                     MaxAdminStatusScheduleSize   = null,
                                           UInt16?                                                     MaxStatusScheduleSize        = null,

                                           String?                                                     DataSource                   = null,
                                           DateTimeOffset?                                             Created                      = null,
                                           DateTimeOffset?                                             LastChange                   = null,

                                           CustomDataNew?                                              CustomData                   = null,
                                           UserDefinedDictionary?                                      InternalData                 = null)

            : base(Id,
                   Name ?? I18NString.Create($"Charging Station Manufacturer '{Id}'"),
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ChargingStationManufacturerAdminStatusType>(Created ?? Timestamp.Now, ChargingStationManufacturerAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ChargingStationManufacturerStatusType>     (Created ?? Timestamp.Now, ChargingStationManufacturerStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.Logo                   = Logo;
            this.Address                = Address;
            this.GeoLocation            = GeoLocation;
            this.Telephone              = Telephone;
            this.EMailAddress           = EMailAddress;
            this.Homepage               = Homepage;
            this.HotlinePhoneNumber     = HotlinePhoneNumber;
            this.TermsAndConditionsURL  = TermsAndConditionsURL;

            var dataLicenses = (DataLicenses ?? []).ToArray();

            if (dataLicenses.Any(dataLicense => dataLicense is null))
                throw new ArgumentException($"The data licenses of the charging station manufacturer '{Id}' must not contain null entries!", nameof(DataLicenses));

            if (dataLicenses.Select(dataLicense => dataLicense.Id).Distinct().Count() != dataLicenses.Length)
                throw new ArgumentException($"A data license of the charging station manufacturer '{Id}' must not be given twice!", nameof(DataLicenses));

            this.DataLicenses           = [ .. dataLicenses.Ordered() ];

            this.Models                 = new (Models, model => model.Id, "charging station model", $"the charging station manufacturer '{Id}'");

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the charging station manufacturer '{Id}'");

        }

        #endregion


        #region TryGetModel(ModelId, out Model)

        /// <summary>
        /// Try to get the model with the given identification.
        /// </summary>
        public Boolean TryGetModel(ChargingStationModel_Id                        ModelId,
                                   [NotNullWhen(true)] out ChargingStationModel?  Model)

            => Models.TryGet(ModelId, out Model);

        #endregion


        #region Status events

        private readonly Lock                                                      statusEventsLock  = new ();
        private          OnChargingStationManufacturerAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnChargingStationManufacturerStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnChargingStationManufacturerAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnChargingStationManufacturerStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                           Timestamp,
                                                                   EventTracking_Id                                         EventTrackingId,
                                                                   Timestamped<ChargingStationManufacturerAdminStatusType>  NewStatus,
                                                                   Timestamped<ChargingStationManufacturerAdminStatusType>  OldStatus,
                                                                   Context?                                                 DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                                      Timestamp,
                                                              EventTracking_Id                                    EventTrackingId,
                                                              Timestamped<ChargingStationManufacturerStatusType>  NewStatus,
                                                              Timestamped<ChargingStationManufacturerStatusType>  OldStatus,
                                                              Context?                                            DataSource)
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

        #region (static) TryParse(JSON, out ChargingStationManufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a charging station manufacturer.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ChargingStationManufacturer">The parsed charging station manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                                JSON,
                                       [NotNullWhen(true)]  out ChargingStationManufacturer?  ChargingStationManufacturer,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            ChargingStationManufacturer = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "charging station manufacturer identification",
                                         ChargingStationManufacturer_Id.TryParse,
                                         out ChargingStationManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station manufacturer '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Name,            "manufacturer name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Description,     "manufacturer description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, BasePropertyKeys.DataSource,      "data source",               out var dataSource,   out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseContact(JSON,
                                             out var logo,
                                             out var address,
                                             out var geoLocation,
                                             out var telephone,
                                             out var eMailAddress,
                                             out var homepage,
                                             out var hotline,
                                             out var termsAndConditions,
                                             out var dataLicenses,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<ChargingStationModel>(JSON,
                                                    PropertyKeys.Models,
                                                    PropertyKeys.ModelIds,
                                                    "charging station models",
                                                    ChargingStationModel.TryParse,
                                                    out var models,
                                                    out ErrorResponse))
                {
                    return false;
                }

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

                ChargingStationManufacturer = new ChargingStationManufacturer(id,
                                                                              name,
                                                                              description,
                                                                              logo,
                                                                              address,
                                                                              geoLocation,
                                                                              telephone,
                                                                              eMailAddress,
                                                                              homepage,
                                                                              hotline,
                                                                              termsAndConditions,
                                                                              dataLicenses,
                                                                              models,
                                                                              IdentityKeys:  identityKeys,
                                                                              SigningKeys:   signingKeys,
                                                                              DataSource:  dataSource,
                                                                              Created:     created,
                                                                              LastChange:  lastChange,
                                                                              CustomData:  customData);

                if (!TryCheckETagsOf(JSON, ChargingStationManufacturer, out ErrorResponse))
                {
                    ChargingStationManufacturer = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationManufacturer  = null;
                ErrorResponse                = "The given JSON representation of a charging station manufacturer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this charging station manufacturer; embedded, as its roaming network embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="IncludeChildArrays">Whether to include its models; without them, its node of the effective tree.</param>
        /// <param name="CustomChargingStationManufacturerSerializer">A delegate to serialize custom charging station manufacturer JSON objects.</param>
        public JObject ToJSON(Boolean                                                        Embedded                                      = false,
                              Boolean                                                        IncludeETags                                  = false,
                              ETagDigestEncoding?                                            ETagDigestEncoding                            = null,
                              Boolean                                                        IncludeChildArrays                            = true,
                              CustomJObjectSerializerDelegate<ChargingStationManufacturer>?  CustomChargingStationManufacturerSerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id,  Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,          JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,             Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,      Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,       DataSource);

            // No data licenses, no property: as the roaming network and the charging station operators write them.
            if (DataLicenses.Length > 0)
                json.Add(BasePropertyKeys.DataLicenses,     DataLicenses.ToPOIJSON());

            if (Address is not null)
                json.Add(PropertyKeys.Address,              Address.ToJSON(Embedded: true));

            if (Logo.HasValue)
                json.Add(PropertyKeys.Logos,                new JArray(new JObject(new JProperty(PropertyKeys.LogoURI, Logo.Value.ToString()))));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,          POIJSON.GeoCoordinateJSON(GeoLocation.Value));

            if (Telephone.HasValue)
                json.Add(PropertyKeys.Telephone,            Telephone.Value.ToString());

            if (EMailAddress is not null)
                json.Add(PropertyKeys.EMailAddress,         EMailAddress.ToString());

            if (Homepage.HasValue)
                json.Add(PropertyKeys.Homepage,             Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,   HotlinePhoneNumber.Value.ToString());

            if (TermsAndConditionsURL.HasValue)
                json.Add(PropertyKeys.TermsAndConditions,   TermsAndConditionsURL.Value.ToString());

            if (IncludeChildArrays && !Models.IsEmpty)
                json.Add(PropertyKeys.Models,               new JArray(Models.Select(model => model.ToJSON(Embedded: true))));

            if (SigningKeys.Length > 0)
                json.Add(PropertyKeys.SigningKeys,          new JArray(SigningKeys. Select(key => key.ToJSON(Embedded: true))));

            if (IdentityKeys.Length > 0)
                json.Add(PropertyKeys.IdentityKeys,         new JArray(IdentityKeys.Select(key => key.ToJSON(Embedded: true))));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomChargingStationManufacturerSerializer is not null
                       ? CustomChargingStationManufacturerSerializer(this, json)
                       : json;

        }

        #endregion


        #region (static) TryParse(CBOR, out ChargingStationManufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a charging station manufacturer: the keys and values of its JSON
        /// representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="ChargingStationManufacturer">The parsed charging station manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                              CBOR,
                                       [NotNullWhen(true)]  out ChargingStationManufacturer?  ChargingStationManufacturer,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            ChargingStationManufacturer = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a charging station manufacturer must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "charging station manufacturer identification",
                                         ChargingStationManufacturer_Id.TryParse,
                                         out ChargingStationManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the charging station manufacturer '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptional    (BasePropertyKeys.Name,         "manufacturer name",         I18NString.TryParse, out I18NString? name,         out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptional    (BasePropertyKeys.Description,  "manufacturer description",  I18NString.TryParse, out I18NString? description,  out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource,   "data source",                                    out var         dataSource,   out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                // What has no CBOR representation of its own is read as its JSON, with the parsers of the JSON
                // representation: the contact data and the data licenses, the models, the keys and the metadata.
                var json = new JObject();

                foreach (var key in new[] { PropertyKeys.Logos,
                                            PropertyKeys.Address,
                                            PropertyKeys.GeoLocation,
                                            PropertyKeys.Telephone,
                                            PropertyKeys.EMailAddress,
                                            PropertyKeys.Homepage,
                                            PropertyKeys.HotlinePhoneNumber,
                                            PropertyKeys.TermsAndConditions,
                                            BasePropertyKeys.DataLicenses,
                                            BasePropertyKeys.DataLicenseIds,
                                            PropertyKeys.Models,
                                            PropertyKeys.ModelIds,
                                            PropertyKeys.SigningKeys,
                                            PropertyKeys.IdentityKeys,
                                            BasePropertyKeys.Created,
                                            BasePropertyKeys.LastChange,
                                            BasePropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                if (!POIJSON.TryParseContact(json,
                                             out var logo,
                                             out var address,
                                             out var geoLocation,
                                             out var telephone,
                                             out var eMailAddress,
                                             out var homepage,
                                             out var hotline,
                                             out var termsAndConditions,
                                             out var dataLicenses,
                                             out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<ChargingStationModel>(json,
                                                    PropertyKeys.Models,
                                                    PropertyKeys.ModelIds,
                                                    "charging station models",
                                                    ChargingStationModel.TryParse,
                                                    out var models,
                                                    out ErrorResponse))
                {
                    return false;
                }

                if (!POIPartyKeys.TryParse(json, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                if (!POIEntityMetadata.TryParse(json,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                ChargingStationManufacturer = new ChargingStationManufacturer(id,
                                                                              name,
                                                                              description,
                                                                              logo,
                                                                              address,
                                                                              geoLocation,
                                                                              telephone,
                                                                              eMailAddress,
                                                                              homepage,
                                                                              hotline,
                                                                              termsAndConditions,
                                                                              dataLicenses,
                                                                              models,
                                                                              IdentityKeys:  identityKeys,
                                                                              SigningKeys:   signingKeys,
                                                                              DataSource:  dataSource,
                                                                              Created:     created,
                                                                              LastChange:  lastChange,
                                                                              CustomData:  customData);

                if (!TryCheckETagsOf(CBOR, ChargingStationManufacturer, out ErrorResponse))
                {
                    ChargingStationManufacturer = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ChargingStationManufacturer  = null;
                ErrorResponse                = "The given CBOR representation of a charging station manufacturer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToCBOR(Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this charging station manufacturer: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="CustomChargingStationManufacturerSerializer">A delegate to serialize custom charging station manufacturer CBOR maps.</param>
        public CBORValue ToCBOR(Boolean                                                      Embedded                                      = false,
                                Boolean                                                      IncludeETags                                  = false,
                                CustomCBORSerializerDelegate<ChargingStationManufacturer>?   CustomChargingStationManufacturerSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),           CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),          CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),             Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),      Description.ToCBOR());

            if (DataSource is not null)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),       CBORValue.FromText(DataSource));

            if (DataLicenses.Length > 0)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataLicenses),     DataLicenses.ToPOICBOR());

            // The address, the geo location and the keys have no CBOR representation of their own: they are written as
            // the keys and values of their JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),              CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Logo.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Logos),
                         CBORValue.FromArray(new CBORMap {
                             { CBORValue.FromText(PropertyKeys.LogoURI),                CBORValue.FromText(Logo.Value.ToString()) }
                         }));

            if (GeoLocation.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.GeoLocation),          CBORJSON.ToCBOR(POIJSON.GeoCoordinateJSON(GeoLocation.Value)));

            if (Telephone.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Telephone),            CBORValue.FromText(Telephone.Value.ToString()));

            if (EMailAddress is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.EMailAddress),         CBORValue.FromText(EMailAddress.ToString()));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),             CBORValue.FromText(Homepage.Value.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),   CBORValue.FromText(HotlinePhoneNumber.Value.ToString()));

            if (TermsAndConditionsURL.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.TermsAndConditions),   CBORValue.FromText(TermsAndConditionsURL.Value.ToString()));

            if (!Models.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.Models),
                         CBORValue.FromArray(Models.Select(model => CBORJSON.ToCBOR(model.ToJSON(Embedded: true)))));

            if (SigningKeys.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.SigningKeys),
                         CBORValue.FromArray(SigningKeys. Select(key => CBORJSON.ToCBOR(key.ToJSON(Embedded: true)))));

            if (IdentityKeys.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.IdentityKeys),
                         CBORValue.FromArray(IdentityKeys.Select(key => CBORJSON.ToCBOR(key.ToJSON(Embedded: true)))));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),              CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),           CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),       CBORJSON.ToCBOR(CustomData.ToJObject()));

            AddETagsTo(cbor, IncludeETags);

            return CustomChargingStationManufacturerSerializer is not null
                       ? CustomChargingStationManufacturerSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two charging station manufacturers for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ChargingStationManufacturer? Manufacturer1, ChargingStationManufacturer? Manufacturer2)

            => ReferenceEquals(Manufacturer1, Manufacturer2) ||
               (Manufacturer1 is not null && Manufacturer1.Equals(Manufacturer2));

        /// <summary>
        /// Compares two charging station manufacturers for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ChargingStationManufacturer? Manufacturer1, ChargingStationManufacturer? Manufacturer2)

            => !(Manufacturer1 == Manufacturer2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ChargingStationManufacturer manufacturer
                   ? CompareTo(manufacturer)
                   : throw new ArgumentException("The given object is not a charging station manufacturer!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ChargingStationManufacturer? ChargingStationManufacturer)

            => ChargingStationManufacturer is null
                   ? throw new ArgumentNullException(nameof(ChargingStationManufacturer), "The given charging station manufacturer must not be null!")
                   : Id.CompareTo(ChargingStationManufacturer.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ChargingStationManufacturer manufacturer &&
               Equals(manufacturer);

        /// <inheritdoc/>
        public Boolean Equals(ChargingStationManufacturer? ChargingStationManufacturer)

            => ChargingStationManufacturer is not null &&
               Id.Equals(ChargingStationManufacturer.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
