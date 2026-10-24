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
using System.Text.RegularExpressions;

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
    /// The admin status of a conformity assessment body changed.
    /// </summary>
    public delegate Task OnConformityAssessmentBodyAdminStatusChangedDelegate(DateTimeOffset                                        Timestamp,
                                                                              EventTracking_Id                                      EventTrackingId,
                                                                              ConformityAssessmentBody                              ConformityAssessmentBody,
                                                                              Timestamped<ConformityAssessmentBodyAdminStatusType>  OldStatus,
                                                                              Timestamped<ConformityAssessmentBodyAdminStatusType>  NewStatus,
                                                                              Context?                                              DataSource);

    /// <summary>
    /// The status of a conformity assessment body changed.
    /// </summary>
    public delegate Task OnConformityAssessmentBodyStatusChangedDelegate(DateTimeOffset                                   Timestamp,
                                                                         EventTracking_Id                                 EventTrackingId,
                                                                         ConformityAssessmentBody                         ConformityAssessmentBody,
                                                                         Timestamped<ConformityAssessmentBodyStatusType>  OldStatus,
                                                                         Timestamped<ConformityAssessmentBodyStatusType>  NewStatus,
                                                                         Context?                                         DataSource);


    /// <summary>
    /// A conformity assessment body, e.g. a national metrology institute, a party of its roaming network: the roaming
    /// network operator adds and removes it, with its keys; it changes its own properties and the transparency software
    /// certificates it issued, embedded in it, signed by its own keys. Energy meters name its certificates by their
    /// identifications, unique within the roaming network. It names the legislations it is a notified body for, e.g.
    /// MID or CRA, and its number as a notified body.
    /// </summary>
    /// <remarks>
    /// A conformity assessment body not taking part (yet) is added without keys: then the roaming network operator signs
    /// its changes and its certificates in its place, until it gets keys from an identity key of the roaming network
    /// operator. Its certificates are statements of the roaming network operator then, not of the body.
    /// </remarks>
    public sealed class ConformityAssessmentBody : AImmutableEMobilityEntity<ConformityAssessmentBody_Id,
                                                                             ConformityAssessmentBodyAdminStatusType,
                                                                             ConformityAssessmentBodyStatusType>,
                                                   IEquatable<ConformityAssessmentBody>,
                                                   IComparable<ConformityAssessmentBody>,
                                                   IPOIParty
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/conformityAssessmentBody/v1";

        /// <summary>
        /// The four digits of the number of a notified body, as the NANDO database of the European Commission gives it.
        /// </summary>
        private static readonly Regex NotifiedBodyNumberPattern = new ("^[0-9]{4}$", RegexOptions.CultureInvariant);

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
            public const String  Logos                               = POIJSON.ContactKeys.Logos;

            /// <summary>
            /// The property of the URI of its logo.
            /// </summary>
            public const String  LogoURI                             = POIJSON.ContactKeys.LogoURI;

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address                             = POIJSON.ContactKeys.Address;

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation                         = POIJSON.ContactKeys.GeoLocation;

            /// <summary>
            /// The property of its telephone number.
            /// </summary>
            public const String  Telephone                           = POIJSON.ContactKeys.Telephone;

            /// <summary>
            /// The property of its e-mail address.
            /// </summary>
            public const String  EMailAddress                        = POIJSON.ContactKeys.EMailAddress;

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage                            = POIJSON.ContactKeys.Homepage;

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber                  = POIJSON.ContactKeys.HotlinePhoneNumber;

            /// <summary>
            /// The property of the URL of its terms and conditions.
            /// </summary>
            public const String  TermsAndConditions                  = POIJSON.ContactKeys.TermsAndConditions;

            /// <summary>
            /// The property of its four digit number as a notified body, e.g. 0102.
            /// </summary>
            public const String  NotifiedBodyNumber                  = "notifiedBodyNumber";

            /// <summary>
            /// The property of the legislations it is a notified body for, e.g. MID and CRA.
            /// </summary>
            public const String  NotifiedFor                         = "notifiedFor";

            /// <summary>
            /// The property of the transparency software certificates it issued.
            /// </summary>
            public const String  TransparencySoftwareCertificates    = "transparencySoftwareCertificates";

            /// <summary>
            /// The property naming its certificates by their identifications only, refused: they are embedded completely.
            /// </summary>
            public const String  TransparencySoftwareCertificateIds  = "transparencySoftwareCertificateIds";

            /// <summary>
            /// The property of its signing keys.
            /// </summary>
            public const String  SigningKeys                         = POIPartyKeys.SigningKeysProperty;

            /// <summary>
            /// The property of its identity keys.
            /// </summary>
            public const String  IdentityKeys                        = POIPartyKeys.IdentityKeysProperty;

        }

        #endregion

        #region (static) class Legislations

        /// <summary>
        /// Well-known legislations a conformity assessment body may be a notified body for; any other nonempty text names
        /// another one.
        /// </summary>
        public static class Legislations
        {

            /// <summary>
            /// The Measuring Instruments Directive 2014/32/EU, e.g. of the energy meters of charging stations.
            /// </summary>
            public const String  MID  = "MID";

            /// <summary>
            /// The Cyber Resilience Act, Regulation (EU) 2024/2847, e.g. of transparency software.
            /// </summary>
            public const String  CRA  = "CRA";

        }

        #endregion


        /// <summary>
        /// The URL of its logo.
        /// </summary>
        public URL?                                           Logo                              { get; }

        /// <summary>
        /// The address of the headquarters.
        /// </summary>
        public Address?                                       Address                           { get; }

        /// <summary>
        /// The geographical location of the headquarters.
        /// </summary>
        public GeoCoordinate?                                 GeoLocation                       { get; }

        /// <summary>
        /// The telephone number of its office.
        /// </summary>
        public PhoneNumber?                                   Telephone                         { get; }

        /// <summary>
        /// The e-mail address of its office, also with a name: "Example Office &lt;info@example.org&gt;".
        /// </summary>
        public EMailAddress?                                  EMailAddress                      { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?                                           Homepage                          { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?                                   HotlinePhoneNumber                { get; }

        /// <summary>
        /// The URL of its terms and conditions.
        /// </summary>
        public URL?                                           TermsAndConditionsURL             { get; }

        /// <summary>
        /// The licenses of its data, ordered by their identifications.
        /// </summary>
        public ImmutableArray<DataLicense>                    DataLicenses                      { get; }

        /// <summary>
        /// Its four digit number as a notified body, as the NANDO database of the European Commission gives it, if it is one.
        /// </summary>
        public String?                                        NotifiedBodyNumber                { get; }

        /// <summary>
        /// The legislations it is a notified body for, e.g. MID and CRA, in their ordinal order.
        /// </summary>
        public ImmutableArray<String>                         NotifiedFor                       { get; }

        /// <summary>
        /// The transparency software certificates it issued, ordered by their identifications.
        /// </summary>
        public POICollection<TransparencySoftwareCertificate_Id, TransparencySoftwareCertificate>  TransparencySoftwareCertificates  { get; }

        /// <summary>
        /// Its signing keys: it signs its own properties and its certificates with them; none while it does not take part.
        /// </summary>
        public ImmutableArray<POIPublicKey>                   SigningKeys                       { get; }

        /// <summary>
        /// Its identity keys, signing changes of its keys; none while it does not take part.
        /// </summary>
        public ImmutableArray<POIPublicKey>                   IdentityKeys                      { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new conformity assessment body.
        /// </summary>
        public ConformityAssessmentBody(ConformityAssessmentBody_Id                                Id,
                                        ImmutableI18NString?                                       Name                               = null,
                                        ImmutableI18NString?                                       Description                        = null,

                                        URL?                                                       Logo                               = null,
                                        Address?                                                   Address                            = null,
                                        GeoCoordinate?                                             GeoLocation                        = null,
                                        PhoneNumber?                                               Telephone                          = null,
                                        EMailAddress?                                              EMailAddress                       = null,
                                        URL?                                                       Homepage                           = null,
                                        PhoneNumber?                                               HotlinePhoneNumber                 = null,
                                        URL?                                                       TermsAndConditionsURL              = null,
                                        IEnumerable<DataLicense>?                                  DataLicenses                       = null,

                                        String?                                                    NotifiedBodyNumber                 = null,
                                        IEnumerable<String>?                                       NotifiedFor                        = null,

                                        IEnumerable<TransparencySoftwareCertificate>?              TransparencySoftwareCertificates   = null,

                                        IEnumerable<POIPublicKey>?                                 IdentityKeys                       = null,
                                        IEnumerable<POIPublicKey>?                                 SigningKeys                        = null,

                                        Timestamped<ConformityAssessmentBodyAdminStatusType>?      InitialAdminStatus                 = null,
                                        Timestamped<ConformityAssessmentBodyStatusType>?           InitialStatus                      = null,
                                        UInt16?                                                    MaxAdminStatusScheduleSize         = null,
                                        UInt16?                                                    MaxStatusScheduleSize              = null,

                                        String?                                                    DataSource                         = null,
                                        DateTimeOffset?                                            Created                            = null,
                                        DateTimeOffset?                                            LastChange                         = null,

                                        CustomDataNew?                                             CustomData                         = null,
                                        UserDefinedDictionary?                                     InternalData                       = null)

            : base(Id,
                   Name ?? I18NString.Create($"Conformity Assessment Body '{Id}'"),
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<ConformityAssessmentBodyAdminStatusType>(Created ?? Timestamp.Now, ConformityAssessmentBodyAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<ConformityAssessmentBodyStatusType>     (Created ?? Timestamp.Now, ConformityAssessmentBodyStatusType.Available),
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
                throw new ArgumentException($"The data licenses of the conformity assessment body '{Id}' must not contain null entries!", nameof(DataLicenses));

            if (dataLicenses.Select(dataLicense => dataLicense.Id).Distinct().Count() != dataLicenses.Length)
                throw new ArgumentException($"A data license of the conformity assessment body '{Id}' must not be given twice!", nameof(DataLicenses));

            this.DataLicenses           = [ .. dataLicenses.Ordered() ];

            if (NotifiedBodyNumber is not null && !NotifiedBodyNumberPattern.IsMatch(NotifiedBodyNumber))
                throw new ArgumentException($"The notified body number '{NotifiedBodyNumber}' of the conformity assessment body '{Id}' must be four digits!", nameof(NotifiedBodyNumber));

            var notifiedFor = (NotifiedFor ?? []).ToArray();

            if (notifiedFor.Any(String.IsNullOrWhiteSpace))
                throw new ArgumentException($"A legislation the conformity assessment body '{Id}' is notified for must not be empty!", nameof(NotifiedFor));

            if (notifiedFor.Distinct(StringComparer.Ordinal).Count() != notifiedFor.Length)
                throw new ArgumentException($"A legislation the conformity assessment body '{Id}' is notified for must not be given twice!", nameof(NotifiedFor));

            this.NotifiedBodyNumber     = NotifiedBodyNumber;
            this.NotifiedFor            = [ .. notifiedFor.Order(StringComparer.Ordinal) ];

            this.TransparencySoftwareCertificates = new (TransparencySoftwareCertificates, certificate => certificate.Id, "transparency software certificate", $"the conformity assessment body '{Id}'");

            // A certificate issued under a legislation is one of a notified body for it.
            if (this.TransparencySoftwareCertificates.FirstOrDefault(certificate => certificate.Legislation is String legislation && !this.NotifiedFor.Contains(legislation)) is TransparencySoftwareCertificate notNotified)
                throw new ArgumentException($"The transparency software certificate '{notNotified.Id}' is issued under the legislation '{notNotified.Legislation}', which the conformity assessment body '{Id}' is not notified for!", nameof(TransparencySoftwareCertificates));

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the conformity assessment body '{Id}'");

        }

        #endregion


        #region TryGetTransparencySoftwareCertificate(CertificateId, out Certificate)

        /// <summary>
        /// Try to get the certificate with the given identification.
        /// </summary>
        public Boolean TryGetTransparencySoftwareCertificate(TransparencySoftwareCertificate_Id                        CertificateId,
                                                             [NotNullWhen(true)] out TransparencySoftwareCertificate?  Certificate)

            => TransparencySoftwareCertificates.TryGet(CertificateId, out Certificate);

        #endregion


        #region Status events

        private readonly Lock                                                   statusEventsLock  = new ();
        private          OnConformityAssessmentBodyAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnConformityAssessmentBodyStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnConformityAssessmentBodyAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnConformityAssessmentBodyStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                        Timestamp,
                                                                   EventTracking_Id                                      EventTrackingId,
                                                                   Timestamped<ConformityAssessmentBodyAdminStatusType>  NewStatus,
                                                                   Timestamped<ConformityAssessmentBodyAdminStatusType>  OldStatus,
                                                                   Context?                                              DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                                   Timestamp,
                                                              EventTracking_Id                                 EventTrackingId,
                                                              Timestamped<ConformityAssessmentBodyStatusType>  NewStatus,
                                                              Timestamped<ConformityAssessmentBodyStatusType>  OldStatus,
                                                              Context?                                         DataSource)
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

        #region (static) TryParse(JSON, out ConformityAssessmentBody, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a conformity assessment body.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="ConformityAssessmentBody">The parsed conformity assessment body.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                             JSON,
                                       [NotNullWhen(true)]  out ConformityAssessmentBody?  ConformityAssessmentBody,
                                       [NotNullWhen(false)] out String?                    ErrorResponse)
        {

            ConformityAssessmentBody = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "conformity assessment body identification",
                                         ConformityAssessmentBody_Id.TryParse,
                                         out ConformityAssessmentBody_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the conformity assessment body '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Name,            "body name",                 out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Description,     "body description",          out var description,  out ErrorResponse) ||
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

                if (!POIJSON.TryParseString (JSON, PropertyKeys.NotifiedBodyNumber,  "notified body number",      out var notifiedBodyNumber,  out ErrorResponse) ||
                    !POIJSON.TryParseStrings(JSON, PropertyKeys.NotifiedFor,         "legislations notified for", out var notifiedFor,         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<TransparencySoftwareCertificate>(JSON,
                                                                               PropertyKeys.TransparencySoftwareCertificates,
                                                                               PropertyKeys.TransparencySoftwareCertificateIds,
                                                                               "transparency software certificates",
                                                                               TransparencySoftwareCertificate.TryParse,
                                                                               out var certificates,
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

                ConformityAssessmentBody = new ConformityAssessmentBody(id,
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
                                                                        notifiedBodyNumber,
                                                                        notifiedFor,
                                                                        certificates,
                                                                        IdentityKeys:  identityKeys,
                                                                        SigningKeys:   signingKeys,
                                                                        DataSource:    dataSource,
                                                                        Created:       created,
                                                                        LastChange:    lastChange,
                                                                        CustomData:    customData);

                if (!TryCheckETagsOf(JSON, ConformityAssessmentBody, out ErrorResponse))
                {
                    ConformityAssessmentBody = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ConformityAssessmentBody  = null;
                ErrorResponse             = "The given JSON representation of a conformity assessment body is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this conformity assessment body; embedded, as its roaming network embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="IncludeChildArrays">Whether to include its certificates; without them, its node of the effective tree.</param>
        /// <param name="CustomConformityAssessmentBodySerializer">A delegate to serialize custom conformity assessment body JSON objects.</param>
        public JObject ToJSON(Boolean                                                     Embedded                                   = false,
                              Boolean                                                     IncludeETags                               = false,
                              ETagDigestEncoding?                                         ETagDigestEncoding                         = null,
                              Boolean                                                     IncludeChildArrays                         = true,
                              CustomJObjectSerializerDelegate<ConformityAssessmentBody>?  CustomConformityAssessmentBodySerializer   = null)
        {

            var json = new JObject(
                           new JProperty(BasePropertyKeys.Id,  Id.ToString())
                       );

            if (!Embedded)
                json.Add(BasePropertyKeys.Context,                      JSONLDContext);

            if (Name.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Name,                         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add(BasePropertyKeys.Description,                  Description.ToJSON());

            if (DataSource is not null)
                json.Add(BasePropertyKeys.DataSource,                   DataSource);

            // No data licenses, no property: as the roaming network and the charging station operators write them.
            if (DataLicenses.Length > 0)
                json.Add(BasePropertyKeys.DataLicenses,                 DataLicenses.ToPOIJSON());

            if (Address is not null)
                json.Add(PropertyKeys.Address,                          Address.ToJSON(Embedded: true));

            if (Logo.HasValue)
                json.Add(PropertyKeys.Logos,                            new JArray(new JObject(new JProperty(PropertyKeys.LogoURI, Logo.Value.ToString()))));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,                      POIJSON.GeoCoordinateJSON(GeoLocation.Value));

            if (Telephone.HasValue)
                json.Add(PropertyKeys.Telephone,                        Telephone.Value.ToString());

            if (EMailAddress is not null)
                json.Add(PropertyKeys.EMailAddress,                     EMailAddress.ToString());

            if (Homepage.HasValue)
                json.Add(PropertyKeys.Homepage,                         Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,               HotlinePhoneNumber.Value.ToString());

            if (TermsAndConditionsURL.HasValue)
                json.Add(PropertyKeys.TermsAndConditions,               TermsAndConditionsURL.Value.ToString());

            if (NotifiedBodyNumber is not null)
                json.Add(PropertyKeys.NotifiedBodyNumber,               NotifiedBodyNumber);

            if (NotifiedFor.Length > 0)
                json.Add(PropertyKeys.NotifiedFor,                      new JArray(NotifiedFor));

            if (IncludeChildArrays && !TransparencySoftwareCertificates.IsEmpty)
                json.Add(PropertyKeys.TransparencySoftwareCertificates, new JArray(TransparencySoftwareCertificates.Select(certificate => certificate.ToJSON(Embedded: true))));

            if (SigningKeys.Length > 0)
                json.Add(PropertyKeys.SigningKeys,                      new JArray(SigningKeys. Select(key => key.ToJSON(Embedded: true))));

            if (IdentityKeys.Length > 0)
                json.Add(PropertyKeys.IdentityKeys,                     new JArray(IdentityKeys.Select(key => key.ToJSON(Embedded: true))));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomConformityAssessmentBodySerializer is not null
                       ? CustomConformityAssessmentBodySerializer(this, json)
                       : json;

        }

        #endregion


        #region (static) TryParse(CBOR, out ConformityAssessmentBody, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a conformity assessment body: the keys and values of its
        /// JSON representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="ConformityAssessmentBody">The parsed conformity assessment body.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                                   CBOR,
                                       [NotNullWhen(true)]  out ConformityAssessmentBody?  ConformityAssessmentBody,
                                       [NotNullWhen(false)] out String?                            ErrorResponse)
        {

            ConformityAssessmentBody = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a conformity assessment body must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "conformity assessment body identification",
                                         ConformityAssessmentBody_Id.TryParse,
                                         out ConformityAssessmentBody_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the conformity assessment body '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptional    (BasePropertyKeys.Name,         "body name",         I18NString.TryParse, out I18NString? name,         out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptional    (BasePropertyKeys.Description,  "body description",  I18NString.TryParse, out I18NString? description,  out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource,   "data source",                                    out var         dataSource,   out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                // What has no CBOR representation of its own is read as its JSON, with the parsers of the JSON
                // representation: the contact data and the data licenses, what it is notified for,
                // its certificates, the keys and the metadata.
                var json = new JObject();

                foreach (var key in new[] { PropertyKeys.Logos,
                                            PropertyKeys.Address,
                                            PropertyKeys.GeoLocation,
                                            PropertyKeys.Telephone,
                                            PropertyKeys.EMailAddress,
                                            PropertyKeys.Homepage,
                                            PropertyKeys.HotlinePhoneNumber,
                                            PropertyKeys.TermsAndConditions,
                                            PropertyKeys.NotifiedBodyNumber,
                                            PropertyKeys.NotifiedFor,
                                            PropertyKeys.TransparencySoftwareCertificates,
                                            PropertyKeys.TransparencySoftwareCertificateIds,
                                            BasePropertyKeys.DataLicenses,
                                            BasePropertyKeys.DataLicenseIds,
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

                if (!POIJSON.TryParseString (json, PropertyKeys.NotifiedBodyNumber,  "notified body number",      out var notifiedBodyNumber,  out ErrorResponse) ||
                    !POIJSON.TryParseStrings(json, PropertyKeys.NotifiedFor,         "legislations notified for", out var notifiedFor,         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<TransparencySoftwareCertificate>(json,
                                                                               PropertyKeys.TransparencySoftwareCertificates,
                                                                               PropertyKeys.TransparencySoftwareCertificateIds,
                                                                               "transparency software certificates",
                                                                               TransparencySoftwareCertificate.TryParse,
                                                                               out var certificates,
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

                ConformityAssessmentBody = new ConformityAssessmentBody(id,
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
                                                                                        notifiedBodyNumber,
                                                                                        notifiedFor,
                                                                                        certificates,
                                                                                        IdentityKeys:  identityKeys,
                                                                                        SigningKeys:   signingKeys,
                                                                                        DataSource:    dataSource,
                                                                                        Created:       created,
                                                                                        LastChange:    lastChange,
                                                                                        CustomData:    customData);

                if (!TryCheckETagsOf(CBOR, ConformityAssessmentBody, out ErrorResponse))
                {
                    ConformityAssessmentBody = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                ConformityAssessmentBody  = null;
                ErrorResponse                     = "The given CBOR representation of a conformity assessment body is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToCBOR(Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this conformity assessment body: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="CustomConformityAssessmentBodySerializer">A delegate to serialize custom conformity assessment body CBOR maps.</param>
        public CBORValue ToCBOR(Boolean                                                          Embedded                                           = false,
                                Boolean                                                          IncludeETags                                       = false,
                                CustomCBORSerializerDelegate<ConformityAssessmentBody>?  CustomConformityAssessmentBodySerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),                    CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),                   CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),                      Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),               Description.ToCBOR());

            if (DataSource is not null)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),                CBORValue.FromText(DataSource));

            if (DataLicenses.Length > 0)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataLicenses),              DataLicenses.ToPOICBOR());

            // The address, the geo location, the certificates and the keys have no CBOR representation of their own: they are
            // written as the keys and values of their JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),                       CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Logo.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Logos),
                         CBORValue.FromArray(new CBORMap {
                             { CBORValue.FromText(PropertyKeys.LogoURI),                         CBORValue.FromText(Logo.Value.ToString()) }
                         }));

            if (GeoLocation.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.GeoLocation),                   CBORJSON.ToCBOR(POIJSON.GeoCoordinateJSON(GeoLocation.Value)));

            if (Telephone.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Telephone),                     CBORValue.FromText(Telephone.Value.ToString()));

            if (EMailAddress is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.EMailAddress),                  CBORValue.FromText(EMailAddress.ToString()));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),                      CBORValue.FromText(Homepage.Value.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),            CBORValue.FromText(HotlinePhoneNumber.Value.ToString()));

            if (TermsAndConditionsURL.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.TermsAndConditions),            CBORValue.FromText(TermsAndConditionsURL.Value.ToString()));

            if (NotifiedBodyNumber is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.NotifiedBodyNumber),            CBORValue.FromText(NotifiedBodyNumber));

            if (NotifiedFor.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.NotifiedFor),                   CBORValue.FromArray(NotifiedFor.Select(CBORValue.FromText)));

            if (!TransparencySoftwareCertificates.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.TransparencySoftwareCertificates),
                         CBORValue.FromArray(TransparencySoftwareCertificates.Select(certificate => CBORJSON.ToCBOR(certificate.ToJSON(Embedded: true)))));

            if (SigningKeys.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.SigningKeys),
                         CBORValue.FromArray(SigningKeys. Select(key => CBORJSON.ToCBOR(key.ToJSON(Embedded: true)))));

            if (IdentityKeys.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.IdentityKeys),
                         CBORValue.FromArray(IdentityKeys.Select(key => CBORJSON.ToCBOR(key.ToJSON(Embedded: true)))));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(BasePropertyKeys.Created),                       CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(BasePropertyKeys.LastChange),                    CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.CustomData),                CBORJSON.ToCBOR(CustomData.ToJObject()));

            AddETagsTo(cbor, IncludeETags);

            return CustomConformityAssessmentBodySerializer is not null
                       ? CustomConformityAssessmentBodySerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two conformity assessment bodies for equality by their identifications.
        /// </summary>
        public static Boolean operator == (ConformityAssessmentBody? Body1, ConformityAssessmentBody? Body2)

            => ReferenceEquals(Body1, Body2) ||
               (Body1 is not null && Body1.Equals(Body2));

        /// <summary>
        /// Compares two conformity assessment bodies for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (ConformityAssessmentBody? Body1, ConformityAssessmentBody? Body2)

            => !(Body1 == Body2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is ConformityAssessmentBody body
                   ? CompareTo(body)
                   : throw new ArgumentException("The given object is not a conformity assessment body!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(ConformityAssessmentBody? ConformityAssessmentBody)

            => ConformityAssessmentBody is null
                   ? throw new ArgumentNullException(nameof(ConformityAssessmentBody), "The given conformity assessment body must not be null!")
                   : Id.CompareTo(ConformityAssessmentBody.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is ConformityAssessmentBody body &&
               Equals(body);

        /// <inheritdoc/>
        public Boolean Equals(ConformityAssessmentBody? ConformityAssessmentBody)

            => ConformityAssessmentBody is not null &&
               Id.Equals(ConformityAssessmentBody.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
