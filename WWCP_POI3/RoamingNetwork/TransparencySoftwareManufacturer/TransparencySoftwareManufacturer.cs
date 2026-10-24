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
    /// The admin status of a transparency software manufacturer changed.
    /// </summary>
    public delegate Task OnTransparencySoftwareManufacturerAdminStatusChangedDelegate(DateTimeOffset                                                Timestamp,
                                                                                      EventTracking_Id                                              EventTrackingId,
                                                                                      TransparencySoftwareManufacturer                              TransparencySoftwareManufacturer,
                                                                                      Timestamped<TransparencySoftwareManufacturerAdminStatusType>  OldStatus,
                                                                                      Timestamped<TransparencySoftwareManufacturerAdminStatusType>  NewStatus,
                                                                                      Context?                                                      DataSource);

    /// <summary>
    /// The status of a transparency software manufacturer changed.
    /// </summary>
    public delegate Task OnTransparencySoftwareManufacturerStatusChangedDelegate(DateTimeOffset                                           Timestamp,
                                                                                 EventTracking_Id                                         EventTrackingId,
                                                                                 TransparencySoftwareManufacturer                         TransparencySoftwareManufacturer,
                                                                                 Timestamped<TransparencySoftwareManufacturerStatusType>  OldStatus,
                                                                                 Timestamped<TransparencySoftwareManufacturerStatusType>  NewStatus,
                                                                                 Context?                                                 DataSource);


    /// <summary>
    /// A manufacturer of transparency software, a party of its roaming network: the roaming network operator adds and
    /// removes it, with its keys; it changes its own properties and its releases, signed by its own keys. Its releases
    /// are embedded in it, one per version of a software; energy meters and certificates name them by their
    /// identifications, unique within the roaming network.
    /// </summary>
    /// <remarks>
    /// Besides its contact data it names what the EU Cyber Resilience Act asks of it as the manufacturer of a product
    /// with digital elements (Regulation (EU) 2024/2847): its role, the single point of contact for vulnerabilities
    /// (Article 13(17), Annex II, points 1 and 2), its policy on coordinated vulnerability disclosure (Annex I, Part II,
    /// point 5) and where it publishes its security advisories (Annex I, Part II, point 4). What a release asks, the
    /// end of its support period, its declaration of conformity and its bill of materials, each release names.
    /// </remarks>
    public sealed class TransparencySoftwareManufacturer : AImmutableEMobilityEntity<TransparencySoftwareManufacturer_Id,
                                                                                     TransparencySoftwareManufacturerAdminStatusType,
                                                                                     TransparencySoftwareManufacturerStatusType>,
                                                           IEquatable<TransparencySoftwareManufacturer>,
                                                           IComparable<TransparencySoftwareManufacturer>,
                                                           IPOIParty
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/transparencySoftwareManufacturer/v1";

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
            public const String  Logos                          = POIJSON.ContactKeys.Logos;

            /// <summary>
            /// The property of the URI of its logo.
            /// </summary>
            public const String  LogoURI                        = POIJSON.ContactKeys.LogoURI;

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address                        = POIJSON.ContactKeys.Address;

            /// <summary>
            /// The property of its geographical location.
            /// </summary>
            public const String  GeoLocation                    = POIJSON.ContactKeys.GeoLocation;

            /// <summary>
            /// The property of its telephone number.
            /// </summary>
            public const String  Telephone                      = POIJSON.ContactKeys.Telephone;

            /// <summary>
            /// The property of its e-mail address.
            /// </summary>
            public const String  EMailAddress                   = POIJSON.ContactKeys.EMailAddress;

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage                       = POIJSON.ContactKeys.Homepage;

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber             = POIJSON.ContactKeys.HotlinePhoneNumber;

            /// <summary>
            /// The property of the URL of its terms and conditions.
            /// </summary>
            public const String  TermsAndConditions             = POIJSON.ContactKeys.TermsAndConditions;

            /// <summary>
            /// The property of its role under the Cyber Resilience Act, see CRARoles.
            /// </summary>
            public const String  CRARole                        = "craRole";

            /// <summary>
            /// The property of the e-mail address of its single point of contact for vulnerabilities.
            /// </summary>
            public const String  VulnerabilityContact           = "vulnerabilityContact";

            /// <summary>
            /// The property of the URL where vulnerabilities of its products are reported.
            /// </summary>
            public const String  VulnerabilityReporting         = "vulnerabilityReporting";

            /// <summary>
            /// The property of the URL of its policy on coordinated vulnerability disclosure.
            /// </summary>
            public const String  VulnerabilityDisclosurePolicy  = "vulnerabilityDisclosurePolicy";

            /// <summary>
            /// The property of the URL where it publishes its security advisories.
            /// </summary>
            public const String  SecurityAdvisories             = "securityAdvisories";

            /// <summary>
            /// The property of its transparency software releases.
            /// </summary>
            public const String  TransparencySoftware           = "transparencySoftware";

            /// <summary>
            /// The property naming its releases by their identifications only, refused: they are embedded completely.
            /// </summary>
            public const String  TransparencySoftwareIds        = "transparencySoftwareIds";

            /// <summary>
            /// The property of its signing keys.
            /// </summary>
            public const String  SigningKeys                    = POIPartyKeys.SigningKeysProperty;

            /// <summary>
            /// The property of its identity keys.
            /// </summary>
            public const String  IdentityKeys                   = POIPartyKeys.IdentityKeysProperty;

        }

        #endregion

        #region (static) class CRARoles

        /// <summary>
        /// The roles of an economic operator of the Cyber Resilience Act a transparency software manufacturer may have.
        /// </summary>
        public static class CRARoles
        {

            /// <summary>
            /// The manufacturer of a product with digital elements it makes available on the market (Article 3(13), Article 13).
            /// </summary>
            public const String  Manufacturer               = "manufacturer";

            /// <summary>
            /// An open-source software steward: it supports the development of free and open-source software intended
            /// for commercial activities, without being its manufacturer (Article 3(14), Article 24).
            /// </summary>
            public const String  OpenSourceSoftwareSteward  = "openSourceSoftwareSteward";

            /// <summary>
            /// All roles.
            /// </summary>
            public static readonly ImmutableHashSet<String> All = ImmutableHashSet.Create(StringComparer.Ordinal,
                                                                                           Manufacturer,
                                                                                           OpenSourceSoftwareSteward);

        }

        #endregion


        /// <summary>
        /// The URL of its logo.
        /// </summary>
        public URL?                                Logo                             { get; }

        /// <summary>
        /// The address of the headquarters, its postal address (CRA, Annex II, point 1).
        /// </summary>
        public Address?                            Address                          { get; }

        /// <summary>
        /// The geographical location of the headquarters.
        /// </summary>
        public GeoCoordinate?                      GeoLocation                      { get; }

        /// <summary>
        /// The telephone number of its office.
        /// </summary>
        public PhoneNumber?                        Telephone                        { get; }

        /// <summary>
        /// The e-mail address of its office, also with a name: "Example Office &lt;info@example.org&gt;".
        /// </summary>
        public EMailAddress?                       EMailAddress                     { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?                                Homepage                         { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?                        HotlinePhoneNumber               { get; }

        /// <summary>
        /// The URL of its terms and conditions.
        /// </summary>
        public URL?                                TermsAndConditionsURL            { get; }

        /// <summary>
        /// The licenses of its data, ordered by their identifications.
        /// </summary>
        public ImmutableArray<DataLicense>         DataLicenses                     { get; }

        /// <summary>
        /// Its role under the Cyber Resilience Act, one of CRARoles, if stated.
        /// </summary>
        public String?                             CRARole                          { get; }

        /// <summary>
        /// The e-mail address of its single point of contact for vulnerabilities (CRA, Article 13(17), Annex II, point 2).
        /// </summary>
        public EMailAddress?                       VulnerabilityContact             { get; }

        /// <summary>
        /// The URL where vulnerabilities of its products are reported, e.g. a form or a security.txt (RFC 9116).
        /// </summary>
        public URL?                                VulnerabilityReporting           { get; }

        /// <summary>
        /// The URL of its policy on coordinated vulnerability disclosure (CRA, Annex I, Part II, point 5).
        /// </summary>
        public URL?                                VulnerabilityDisclosurePolicy    { get; }

        /// <summary>
        /// The URL where it publishes the vulnerabilities it fixed (CRA, Annex I, Part II, point 4).
        /// </summary>
        public URL?                                SecurityAdvisories               { get; }

        /// <summary>
        /// Its transparency software releases, ordered by their identifications.
        /// </summary>
        public POICollection<TransparencySoftware_Id, TransparencySoftware>  TransparencySoftware  { get; }

        /// <summary>
        /// Its signing keys: it signs its own properties and its releases with them.
        /// </summary>
        public ImmutableArray<POIPublicKey>        SigningKeys                      { get; }

        /// <summary>
        /// Its identity keys, signing changes of its keys.
        /// </summary>
        public ImmutableArray<POIPublicKey>        IdentityKeys                     { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new transparency software manufacturer.
        /// </summary>
        public TransparencySoftwareManufacturer(TransparencySoftwareManufacturer_Id                               Id,
                                                ImmutableI18NString?                                              Name                            = null,
                                                ImmutableI18NString?                                              Description                     = null,

                                                URL?                                                              Logo                            = null,
                                                Address?                                                          Address                         = null,
                                                GeoCoordinate?                                                    GeoLocation                     = null,
                                                PhoneNumber?                                                      Telephone                       = null,
                                                EMailAddress?                                                     EMailAddress                    = null,
                                                URL?                                                              Homepage                        = null,
                                                PhoneNumber?                                                      HotlinePhoneNumber              = null,
                                                URL?                                                              TermsAndConditionsURL           = null,
                                                IEnumerable<DataLicense>?                                         DataLicenses                    = null,

                                                String?                                                           CRARole                         = null,
                                                EMailAddress?                                                     VulnerabilityContact            = null,
                                                URL?                                                              VulnerabilityReporting          = null,
                                                URL?                                                              VulnerabilityDisclosurePolicy   = null,
                                                URL?                                                              SecurityAdvisories              = null,

                                                IEnumerable<TransparencySoftware>?                                TransparencySoftware            = null,

                                                IEnumerable<POIPublicKey>?                                        IdentityKeys                    = null,
                                                IEnumerable<POIPublicKey>?                                        SigningKeys                     = null,

                                                Timestamped<TransparencySoftwareManufacturerAdminStatusType>?     InitialAdminStatus              = null,
                                                Timestamped<TransparencySoftwareManufacturerStatusType>?          InitialStatus                   = null,
                                                UInt16?                                                           MaxAdminStatusScheduleSize      = null,
                                                UInt16?                                                           MaxStatusScheduleSize           = null,

                                                String?                                                           DataSource                      = null,
                                                DateTimeOffset?                                                   Created                         = null,
                                                DateTimeOffset?                                                   LastChange                      = null,

                                                CustomDataNew?                                                    CustomData                      = null,
                                                UserDefinedDictionary?                                            InternalData                    = null)

            : base(Id,
                   Name ?? I18NString.Create($"Transparency Software Manufacturer '{Id}'"),
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<TransparencySoftwareManufacturerAdminStatusType>(Created ?? Timestamp.Now, TransparencySoftwareManufacturerAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<TransparencySoftwareManufacturerStatusType>     (Created ?? Timestamp.Now, TransparencySoftwareManufacturerStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.Logo                           = Logo;
            this.Address                        = Address;
            this.GeoLocation                    = GeoLocation;
            this.Telephone                      = Telephone;
            this.EMailAddress                   = EMailAddress;
            this.Homepage                       = Homepage;
            this.HotlinePhoneNumber             = HotlinePhoneNumber;
            this.TermsAndConditionsURL          = TermsAndConditionsURL;

            var dataLicenses = (DataLicenses ?? []).ToArray();

            if (dataLicenses.Any(dataLicense => dataLicense is null))
                throw new ArgumentException($"The data licenses of the transparency software manufacturer '{Id}' must not contain null entries!", nameof(DataLicenses));

            if (dataLicenses.Select(dataLicense => dataLicense.Id).Distinct().Count() != dataLicenses.Length)
                throw new ArgumentException($"A data license of the transparency software manufacturer '{Id}' must not be given twice!", nameof(DataLicenses));

            this.DataLicenses                   = [ .. dataLicenses.Ordered() ];

            if (CRARole is not null && !CRARoles.All.Contains(CRARole))
                throw new ArgumentException($"The role '{CRARole}' of the transparency software manufacturer '{Id}' must be one of {String.Join(", ", CRARoles.All.Order(StringComparer.Ordinal))}!", nameof(CRARole));

            this.CRARole                        = CRARole;
            this.VulnerabilityContact           = VulnerabilityContact;
            this.VulnerabilityReporting         = VulnerabilityReporting;
            this.VulnerabilityDisclosurePolicy  = VulnerabilityDisclosurePolicy;
            this.SecurityAdvisories             = SecurityAdvisories;

            this.TransparencySoftware           = new (TransparencySoftware, release => release.Id, "transparency software release", $"the transparency software manufacturer '{Id}'");

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the transparency software manufacturer '{Id}'");

        }

        #endregion


        #region TryGetTransparencySoftware(TransparencySoftwareId, out TransparencySoftware)

        /// <summary>
        /// Try to get the release with the given identification.
        /// </summary>
        public Boolean TryGetTransparencySoftware(TransparencySoftware_Id                        TransparencySoftwareId,
                                                  [NotNullWhen(true)] out TransparencySoftware?  TransparencySoftware)

            => this.TransparencySoftware.TryGet(TransparencySoftwareId, out TransparencySoftware);

        #endregion


        #region Status events

        private readonly Lock                                                           statusEventsLock  = new ();
        private          OnTransparencySoftwareManufacturerAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnTransparencySoftwareManufacturerStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnTransparencySoftwareManufacturerAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnTransparencySoftwareManufacturerStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                                Timestamp,
                                                                   EventTracking_Id                                              EventTrackingId,
                                                                   Timestamped<TransparencySoftwareManufacturerAdminStatusType>  NewStatus,
                                                                   Timestamped<TransparencySoftwareManufacturerAdminStatusType>  OldStatus,
                                                                   Context?                                                      DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                                           Timestamp,
                                                              EventTracking_Id                                         EventTrackingId,
                                                              Timestamped<TransparencySoftwareManufacturerStatusType>  NewStatus,
                                                              Timestamped<TransparencySoftwareManufacturerStatusType>  OldStatus,
                                                              Context?                                                 DataSource)
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

        #region (static) TryParse(JSON, out TransparencySoftwareManufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a transparency software manufacturer.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="TransparencySoftwareManufacturer">The parsed transparency software manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                                     JSON,
                                       [NotNullWhen(true)]  out TransparencySoftwareManufacturer?  TransparencySoftwareManufacturer,
                                       [NotNullWhen(false)] out String?                            ErrorResponse)
        {

            TransparencySoftwareManufacturer = null;

            try
            {

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "transparency software manufacturer identification",
                                         TransparencySoftwareManufacturer_Id.TryParse,
                                         out TransparencySoftwareManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the transparency software manufacturer '{id}' must be '{JSONLDContext}'!";
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

                if (!TryParseCRA(JSON,
                                 out var craRole,
                                 out var vulnerabilityContact,
                                 out var vulnerabilityReporting,
                                 out var vulnerabilityDisclosurePolicy,
                                 out var securityAdvisories,
                                 out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<TransparencySoftware>(JSON,
                                                                    PropertyKeys.TransparencySoftware,
                                                                    PropertyKeys.TransparencySoftwareIds,
                                                                    "transparency software releases",
                                                                    POI.TransparencySoftware.TryParse,
                                                                    out var releases,
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

                TransparencySoftwareManufacturer = new TransparencySoftwareManufacturer(id,
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
                                                                                        craRole,
                                                                                        vulnerabilityContact,
                                                                                        vulnerabilityReporting,
                                                                                        vulnerabilityDisclosurePolicy,
                                                                                        securityAdvisories,
                                                                                        releases,
                                                                                        IdentityKeys:  identityKeys,
                                                                                        SigningKeys:   signingKeys,
                                                                                        DataSource:    dataSource,
                                                                                        Created:       created,
                                                                                        LastChange:    lastChange,
                                                                                        CustomData:    customData);

                if (!TryCheckETagsOf(JSON, TransparencySoftwareManufacturer, out ErrorResponse))
                {
                    TransparencySoftwareManufacturer = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                TransparencySoftwareManufacturer  = null;
                ErrorResponse                     = "The given JSON representation of a transparency software manufacturer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (private static) TryParseCRA(JSON, out CRARole, ..., out ErrorResponse)

        /// <summary>
        /// The optional properties of the Cyber Resilience Act of the given JSON representation of a manufacturer.
        /// </summary>
        private static Boolean TryParseCRA(JObject                           JSON,
                                           out String?                       CRARole,
                                           out EMailAddress?                 VulnerabilityContact,
                                           out URL?                          VulnerabilityReporting,
                                           out URL?                          VulnerabilityDisclosurePolicy,
                                           out URL?                          SecurityAdvisories,
                                           [NotNullWhen(false)] out String?  ErrorResponse)
        {

            VulnerabilityContact           = null;
            VulnerabilityReporting         = null;
            VulnerabilityDisclosurePolicy  = null;
            SecurityAdvisories             = null;

            if (!POIJSON.TryParseString(JSON, PropertyKeys.CRARole, "role under the Cyber Resilience Act", out CRARole, out ErrorResponse))
                return false;

            if (CRARole is not null && !CRARoles.All.Contains(CRARole))
            {
                ErrorResponse = $"The role '{CRARole}' under the Cyber Resilience Act must be one of {String.Join(", ", CRARoles.All.Order(StringComparer.Ordinal))}!";
                return false;
            }

            return POIJSON.TryParseEMailAddress(JSON[PropertyKeys.VulnerabilityContact],  "vulnerability contact",                                         out VulnerabilityContact,           out ErrorResponse) &&
                   POIJSON.TryParseText        (JSON, PropertyKeys.VulnerabilityReporting,        "vulnerability reporting URL",          URL.TryParse, out VulnerabilityReporting,         out ErrorResponse) &&
                   POIJSON.TryParseText        (JSON, PropertyKeys.VulnerabilityDisclosurePolicy, "vulnerability disclosure policy URL",  URL.TryParse, out VulnerabilityDisclosurePolicy,  out ErrorResponse) &&
                   POIJSON.TryParseText        (JSON, PropertyKeys.SecurityAdvisories,            "security advisories URL",              URL.TryParse, out SecurityAdvisories,             out ErrorResponse);

        }

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this transparency software manufacturer; embedded, as its roaming network embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="IncludeChildArrays">Whether to include its releases; without them, its node of the effective tree.</param>
        /// <param name="CustomTransparencySoftwareManufacturerSerializer">A delegate to serialize custom transparency software manufacturer JSON objects.</param>
        public JObject ToJSON(Boolean                                                             Embedded                                           = false,
                              Boolean                                                             IncludeETags                                       = false,
                              ETagDigestEncoding?                                                 ETagDigestEncoding                                 = null,
                              Boolean                                                             IncludeChildArrays                                 = true,
                              CustomJObjectSerializerDelegate<TransparencySoftwareManufacturer>?  CustomTransparencySoftwareManufacturerSerializer   = null)
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

            // No data licenses, no property: as the roaming network and the charging station operators write them.
            if (DataLicenses.Length > 0)
                json.Add(BasePropertyKeys.DataLicenses,              DataLicenses.ToPOIJSON());

            if (Address is not null)
                json.Add(PropertyKeys.Address,                       Address.ToJSON(Embedded: true));

            if (Logo.HasValue)
                json.Add(PropertyKeys.Logos,                         new JArray(new JObject(new JProperty(PropertyKeys.LogoURI, Logo.Value.ToString()))));

            if (GeoLocation.HasValue)
                json.Add(PropertyKeys.GeoLocation,                   POIJSON.GeoCoordinateJSON(GeoLocation.Value));

            if (Telephone.HasValue)
                json.Add(PropertyKeys.Telephone,                     Telephone.Value.ToString());

            if (EMailAddress is not null)
                json.Add(PropertyKeys.EMailAddress,                  EMailAddress.ToString());

            if (Homepage.HasValue)
                json.Add(PropertyKeys.Homepage,                      Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,            HotlinePhoneNumber.Value.ToString());

            if (TermsAndConditionsURL.HasValue)
                json.Add(PropertyKeys.TermsAndConditions,            TermsAndConditionsURL.Value.ToString());

            if (CRARole is not null)
                json.Add(PropertyKeys.CRARole,                       CRARole);

            if (VulnerabilityContact is not null)
                json.Add(PropertyKeys.VulnerabilityContact,          VulnerabilityContact.ToString());

            if (VulnerabilityReporting.HasValue)
                json.Add(PropertyKeys.VulnerabilityReporting,        VulnerabilityReporting.Value.ToString());

            if (VulnerabilityDisclosurePolicy.HasValue)
                json.Add(PropertyKeys.VulnerabilityDisclosurePolicy, VulnerabilityDisclosurePolicy.Value.ToString());

            if (SecurityAdvisories.HasValue)
                json.Add(PropertyKeys.SecurityAdvisories,            SecurityAdvisories.Value.ToString());

            if (IncludeChildArrays && !TransparencySoftware.IsEmpty)
                json.Add(PropertyKeys.TransparencySoftware,          new JArray(TransparencySoftware.Select(release => release.ToJSON(Embedded: true))));

            if (SigningKeys.Length > 0)
                json.Add(PropertyKeys.SigningKeys,                   new JArray(SigningKeys. Select(key => key.ToJSON(Embedded: true))));

            if (IdentityKeys.Length > 0)
                json.Add(PropertyKeys.IdentityKeys,                  new JArray(IdentityKeys.Select(key => key.ToJSON(Embedded: true))));

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomTransparencySoftwareManufacturerSerializer is not null
                       ? CustomTransparencySoftwareManufacturerSerializer(this, json)
                       : json;

        }

        #endregion


        #region (static) TryParse(CBOR, out TransparencySoftwareManufacturer, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a transparency software manufacturer: the keys and values of its
        /// JSON representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="TransparencySoftwareManufacturer">The parsed transparency software manufacturer.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                                   CBOR,
                                       [NotNullWhen(true)]  out TransparencySoftwareManufacturer?  TransparencySoftwareManufacturer,
                                       [NotNullWhen(false)] out String?                            ErrorResponse)
        {

            TransparencySoftwareManufacturer = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a transparency software manufacturer must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "transparency software manufacturer identification",
                                         TransparencySoftwareManufacturer_Id.TryParse,
                                         out TransparencySoftwareManufacturer_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the transparency software manufacturer '{id}' must be '{JSONLDContext}'!";
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
                // representation: the contact data and the data licenses, the properties of the Cyber Resilience Act,
                // the releases, the keys and the metadata.
                var json = new JObject();

                foreach (var key in new[] { PropertyKeys.Logos,
                                            PropertyKeys.Address,
                                            PropertyKeys.GeoLocation,
                                            PropertyKeys.Telephone,
                                            PropertyKeys.EMailAddress,
                                            PropertyKeys.Homepage,
                                            PropertyKeys.HotlinePhoneNumber,
                                            PropertyKeys.TermsAndConditions,
                                            PropertyKeys.CRARole,
                                            PropertyKeys.VulnerabilityContact,
                                            PropertyKeys.VulnerabilityReporting,
                                            PropertyKeys.VulnerabilityDisclosurePolicy,
                                            PropertyKeys.SecurityAdvisories,
                                            PropertyKeys.TransparencySoftware,
                                            PropertyKeys.TransparencySoftwareIds,
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

                if (!TryParseCRA(json,
                                 out var craRole,
                                 out var vulnerabilityContact,
                                 out var vulnerabilityReporting,
                                 out var vulnerabilityDisclosurePolicy,
                                 out var securityAdvisories,
                                 out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseChildren<TransparencySoftware>(json,
                                                                    PropertyKeys.TransparencySoftware,
                                                                    PropertyKeys.TransparencySoftwareIds,
                                                                    "transparency software releases",
                                                                    POI.TransparencySoftware.TryParse,
                                                                    out var releases,
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

                TransparencySoftwareManufacturer = new TransparencySoftwareManufacturer(id,
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
                                                                                        craRole,
                                                                                        vulnerabilityContact,
                                                                                        vulnerabilityReporting,
                                                                                        vulnerabilityDisclosurePolicy,
                                                                                        securityAdvisories,
                                                                                        releases,
                                                                                        IdentityKeys:  identityKeys,
                                                                                        SigningKeys:   signingKeys,
                                                                                        DataSource:    dataSource,
                                                                                        Created:       created,
                                                                                        LastChange:    lastChange,
                                                                                        CustomData:    customData);

                if (!TryCheckETagsOf(CBOR, TransparencySoftwareManufacturer, out ErrorResponse))
                {
                    TransparencySoftwareManufacturer = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                TransparencySoftwareManufacturer  = null;
                ErrorResponse                     = "The given CBOR representation of a transparency software manufacturer is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToCBOR(Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this transparency software manufacturer: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="CustomTransparencySoftwareManufacturerSerializer">A delegate to serialize custom transparency software manufacturer CBOR maps.</param>
        public CBORValue ToCBOR(Boolean                                                          Embedded                                           = false,
                                Boolean                                                          IncludeETags                                       = false,
                                CustomCBORSerializerDelegate<TransparencySoftwareManufacturer>?  CustomTransparencySoftwareManufacturerSerializer   = null)
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

            // The address, the geo location, the releases and the keys have no CBOR representation of their own: they are
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

            if (CRARole is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.CRARole),                       CBORValue.FromText(CRARole));

            if (VulnerabilityContact is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.VulnerabilityContact),          CBORValue.FromText(VulnerabilityContact.ToString()));

            if (VulnerabilityReporting.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.VulnerabilityReporting),        CBORValue.FromText(VulnerabilityReporting.Value.ToString()));

            if (VulnerabilityDisclosurePolicy.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.VulnerabilityDisclosurePolicy), CBORValue.FromText(VulnerabilityDisclosurePolicy.Value.ToString()));

            if (SecurityAdvisories.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.SecurityAdvisories),            CBORValue.FromText(SecurityAdvisories.Value.ToString()));

            if (!TransparencySoftware.IsEmpty)
                cbor.Add(CBORValue.FromText(PropertyKeys.TransparencySoftware),
                         CBORValue.FromArray(TransparencySoftware.Select(release => CBORJSON.ToCBOR(release.ToJSON(Embedded: true)))));

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

            return CustomTransparencySoftwareManufacturerSerializer is not null
                       ? CustomTransparencySoftwareManufacturerSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two transparency software manufacturers for equality by their identifications.
        /// </summary>
        public static Boolean operator == (TransparencySoftwareManufacturer? Manufacturer1, TransparencySoftwareManufacturer? Manufacturer2)

            => ReferenceEquals(Manufacturer1, Manufacturer2) ||
               (Manufacturer1 is not null && Manufacturer1.Equals(Manufacturer2));

        /// <summary>
        /// Compares two transparency software manufacturers for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (TransparencySoftwareManufacturer? Manufacturer1, TransparencySoftwareManufacturer? Manufacturer2)

            => !(Manufacturer1 == Manufacturer2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is TransparencySoftwareManufacturer manufacturer
                   ? CompareTo(manufacturer)
                   : throw new ArgumentException("The given object is not a transparency software manufacturer!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(TransparencySoftwareManufacturer? TransparencySoftwareManufacturer)

            => TransparencySoftwareManufacturer is null
                   ? throw new ArgumentNullException(nameof(TransparencySoftwareManufacturer), "The given transparency software manufacturer must not be null!")
                   : Id.CompareTo(TransparencySoftwareManufacturer.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is TransparencySoftwareManufacturer manufacturer &&
               Equals(manufacturer);

        /// <inheritdoc/>
        public Boolean Equals(TransparencySoftwareManufacturer? TransparencySoftwareManufacturer)

            => TransparencySoftwareManufacturer is not null &&
               Id.Equals(TransparencySoftwareManufacturer.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
