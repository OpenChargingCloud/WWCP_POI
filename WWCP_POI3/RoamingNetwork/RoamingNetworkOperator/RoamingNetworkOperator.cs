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
using org.GraphDefined.Vanaheimr.Hermod.Mail;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The admin status of a roaming network operator changed.
    /// </summary>
    public delegate Task OnRoamingNetworkOperatorAdminStatusChangedDelegate(DateTimeOffset                                       Timestamp,
                                                                            EventTracking_Id                                     EventTrackingId,
                                                                            RoamingNetworkOperator                               RoamingNetworkOperator,
                                                                            Timestamped<RoamingNetworkOperatorAdminStatusType>   OldStatus,
                                                                            Timestamped<RoamingNetworkOperatorAdminStatusType>   NewStatus,
                                                                            Context?                                             DataSource);

    /// <summary>
    /// The status of a roaming network operator changed.
    /// </summary>
    public delegate Task OnRoamingNetworkOperatorStatusChangedDelegate(DateTimeOffset                                  Timestamp,
                                                                       EventTracking_Id                                EventTrackingId,
                                                                       RoamingNetworkOperator                          RoamingNetworkOperator,
                                                                       Timestamped<RoamingNetworkOperatorStatusType>   OldStatus,
                                                                       Timestamped<RoamingNetworkOperatorStatusType>   NewStatus,
                                                                       Context?                                        DataSource);


    /// <summary>
    /// Extension methods for RoamingNetworkOperators.
    /// </summary>
    public static class RoamingNetworkOperatorExtensions
    {

        #region CreateRoamingNetwork(this RoamingNetworkOperator, RoamingNetworkId, ...)

        /// <summary>
        /// Creates a new roaming network and associates it with the specified roaming network operator.
        /// </summary>
        /// <param name="RoamingNetworkOperator">The roaming network operator that owns the roaming network.</param>
        /// 
        /// <param name="RoamingNetworkId">The unique identifier of the roaming network.</param>
        /// <param name="Name">The multilingual name of the roaming network.</param>
        /// <param name="Description">The multilingual description of the roaming network.</param>
        /// <param name="InitialAdminStatus">The initial administrative status of the roaming network.</param>
        /// <param name="InitialStatus">The initial operational status of the roaming network.</param>
        /// <param name="MaxAdminStatusScheduleSize">The maximum number of administrative status schedule entries.</param>
        /// <param name="MaxStatusScheduleSize">The maximum number of status schedule entries.</param>
        /// 
        /// <param name="DataSource">The source of the roaming network data.</param>
        /// <param name="Created">The creation timestamp of the roaming network.</param>
        /// <param name="LastChange">The timestamp of the last modification.</param>
        /// 
        /// <param name="CustomData">Custom data attached to the roaming network.</param>
        /// <param name="InternalData">Internal key-value data attached to the roaming network.</param>
        /// 
        /// <param name="DataLicenses">The data licenses associated with the roaming network.</param>
        /// <param name="ChargingStationOperators">The charging station operators linked to the roaming network.</param>
        /// <param name="ChargingStationManufacturers">The charging station manufacturers linked to the roaming network.</param>
        /// <param name="EMobilityProviders">The e-mobility providers linked to the roaming network.</param>
        /// <param name="GridOperators">The grid operators linked to the roaming network.</param>
        /// <param name="ParkingOperators">The parking operators linked to the roaming network.</param>
        /// <param name="TransparencySoftware">The transparency software linked to the roaming network.</param>
        /// <param name="TransparencySoftwareCertificates">The transparency software certificates linked to the roaming network.</param>
        /// <param name="EnergyMeterManufacturers">The energy meter manufacturers linked to the roaming network.</param>
        /// 
        /// <param name="TreeHashAlgorithms">The hash algorithms used for tree hash calculations.</param>
        public static RoamingNetwork CreateRoamingNetwork(this RoamingNetworkOperator                    RoamingNetworkOperator,

                                                          RoamingNetwork_Id                              RoamingNetworkId,
                                                          ImmutableI18NString?                           Name                               = null,
                                                          ImmutableI18NString?                           Description                        = null,
                                                          RoamingNetworkAdminStatusType?                 InitialAdminStatus                 = null,
                                                          RoamingNetworkStatusType?                      InitialStatus                      = null,
                                                          UInt16?                                        MaxAdminStatusScheduleSize         = null,
                                                          UInt16?                                        MaxStatusScheduleSize              = null,

                                                          String?                                        DataSource                         = null,
                                                          DateTimeOffset?                                Created                            = null,
                                                          DateTimeOffset?                                LastChange                         = null,

                                                          CustomDataNew?                                 CustomData                         = null,
                                                          UserDefinedDictionary?                         InternalData                       = null,

                                                          IEnumerable<DataLicense>?                      DataLicenses                       = null,
                                                          IEnumerable<ChargingStationOperator>?          ChargingStationOperators           = null,
                                                          IEnumerable<ChargingStationManufacturer>?      ChargingStationManufacturers       = null,
                                                          IEnumerable<EMobilityProvider>?                EMobilityProviders                 = null,
                                                          IEnumerable<GridOperator>?                     GridOperators                      = null,
                                                          IEnumerable<ParkingOperator>?                  ParkingOperators                   = null,
                                                          IEnumerable<TransparencySoftware>?             TransparencySoftware               = null,
                                                          IEnumerable<TransparencySoftwareCertificate>?  TransparencySoftwareCertificates   = null,
                                                          IEnumerable<EnergyMeterManufacturer>?          EnergyMeterManufacturers           = null,

                                                          IEnumerable<ETagHashAlgorithm>?                TreeHashAlgorithms                 = null)


            => new (RoamingNetworkId,
                    Name,
                    Description,
                    InitialAdminStatus,
                    InitialStatus,
                    MaxAdminStatusScheduleSize,
                    MaxStatusScheduleSize,

                    DataSource,
                    Created,
                    LastChange,

                    CustomData,
                    InternalData,

                    DataLicenses,
                    ChargingStationOperators,
                    ChargingStationManufacturers,
                    EMobilityProviders,
                    GridOperators,
                    ParkingOperators,
                    TransparencySoftware,
                    TransparencySoftwareCertificates,
                    EnergyMeterManufacturers,
                    RoamingNetworkOperator,
                    TreeHashAlgorithms);

        #endregion

    }


    /// <summary>
    /// The operator of a roaming network, embedded in the roaming network it operates as its "roamingNetworkOperator".
    /// One operator may operate many roaming networks, e.g. a production and a QA network: each of them embeds it
    /// with its keys, so that every one of them can be verified from its own history alone.
    /// </summary>
    public sealed class RoamingNetworkOperator : AImmutableEMobilityEntity<RoamingNetworkOperator_Id,
                                                                           RoamingNetworkOperatorAdminStatusType,
                                                                           RoamingNetworkOperatorStatusType>,
                                                 IEquatable<RoamingNetworkOperator>,
                                                 IComparable<RoamingNetworkOperator>,
                                                 IPOIParty
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetworkOperator";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its address.
            /// </summary>
            public const String  Address               = "address";

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage              = "homepage";

            /// <summary>
            /// The property of its e-mail address.
            /// </summary>
            public const String  EMailAddress          = "eMailAddress";

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber    = "hotline";

            /// <summary>
            /// The property of its hotline e-mail address.
            /// </summary>
            public const String  HotlineEMailAddress   = "hotlineEMailAddress";

            /// <summary>
            /// The property of its signing keys, as of every party.
            /// </summary>
            public const String  SigningKeys           = POIPartyKeys.SigningKeysProperty;

            /// <summary>
            /// The property of its identity keys, as of every party.
            /// </summary>
            public const String  IdentityKeys          = POIPartyKeys.IdentityKeysProperty;

            /// <summary>
            /// The property of its key statement.
            /// </summary>
            public const String  KeyStatement          = RoamingNetworkKeyStatement.JSONProperty;

            /// <summary>
            /// The property of its creation timestamp, as of every entity.
            /// </summary>
            public const String  Created               = "created";

            /// <summary>
            /// The property of its last change timestamp, as of every entity.
            /// </summary>
            public const String  LastChange            = "lastChange";

            /// <summary>
            /// The property of its custom data, as of every entity.
            /// </summary>
            public const String  CustomData            = "customData";

        }

        #endregion


        /// <summary>
        /// The optional address of the roaming network operator.
        /// </summary>
        public Address?                      Address               { get; }

        /// <summary>
        /// The optional homepage of the roaming network operator.
        /// </summary>
        public URL?                          Homepage              { get; }

        /// <summary>
        /// The optional e-mail address of the roaming network operator.
        /// </summary>
        public EMailAddress?                 EMail                 { get; }

        /// <summary>
        /// The hotline phone number of the roaming network operator.
        /// </summary>
        public PhoneNumber?                  HotlinePhoneNumber    { get; }

        /// <summary>
        /// The optional hotline e-mail address of the roaming network operator.
        /// </summary>
        public EMailAddress?                 HotlineEMail          { get; }

        /// <summary>
        /// The keys signing the change sets and commits of the roaming network operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<POIPublicKey>  SigningKeys           { get; }

        /// <summary>
        /// The identity keys of the roaming network operator, signing changes of its own keys
        /// and, for recovery, of the keys of other parties, in the order of their identifications.
        /// </summary>
        public ImmutableArray<POIPublicKey>  IdentityKeys          { get; }

        /// <summary>
        /// Its key statement, if any: which signing keys it uses, signed with one of its identity keys, so that an app pinning
        /// its identity keys trusts them (SPECIFICATION.md, section 8.6).
        /// </summary>
        public RoamingNetworkKeyStatement?   KeyStatement          { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network operator.
        /// </summary>
        /// <param name="Id">The unique identifier of the roaming network operator.</param>
        /// <param name="Name">The multilingual display name of the roaming network operator.</param>
        /// <param name="Description">The multilingual description of the roaming network operator.</param>
        /// <param name="Address">The postal address of the roaming network operator.</param>
        /// <param name="Homepage">The homepage URL of the roaming network operator.</param>
        /// <param name="EMail">The general contact email address of the roaming network operator.</param>
        /// <param name="HotlinePhoneNumber">The hotline phone number for support or incident handling.</param>
        /// <param name="HotlineEMail">The hotline email address for support or incident handling.</param>
        /// <param name="SigningKeys">The public keys used to verify signatures created by the roaming network operator.</param>
        /// <param name="IdentityKeys">The public keys used to identify the roaming network operator.</param>
        /// <param name="KeyStatement">The key statement describing key ownership or trust information.</param>
        /// 
        /// <param name="InitialAdminStatus">The initial administrative status with timestamp.</param>
        /// <param name="InitialStatus">The initial operational status with timestamp.</param>
        /// <param name="MaxAdminStatusScheduleSize">The maximum number of administrative status schedule entries.</param>
        /// <param name="MaxStatusScheduleSize">The maximum number of status schedule entries.</param>
        /// 
        /// <param name="DataSource">The data source identifier for this entity.</param>
        /// <param name="Created">The creation timestamp of this entity.</param>
        /// <param name="LastChange">The timestamp of the last change.</param>
        /// 
        /// <param name="CustomData">Custom data associated with this entity.</param>
        /// <param name="InternalData">Internal data associated with this entity.</param>
        public RoamingNetworkOperator(RoamingNetworkOperator_Id                            Id,
                                      I18NString?                                          Name                         = null,
                                      I18NString?                                          Description                  = null,
                                      Address?                                             Address                      = null,
                                      URL?                                                 Homepage                     = null,
                                      EMailAddress?                                        EMail                        = null,
                                      PhoneNumber?                                         HotlinePhoneNumber           = null,
                                      EMailAddress?                                        HotlineEMail                 = null,
                                      IEnumerable<POIPublicKey>?                           SigningKeys                  = null,
                                      IEnumerable<POIPublicKey>?                           IdentityKeys                 = null,
                                      RoamingNetworkKeyStatement?                          KeyStatement                 = null,

                                      Timestamped<RoamingNetworkOperatorAdminStatusType>?  InitialAdminStatus           = null,
                                      Timestamped<RoamingNetworkOperatorStatusType>?       InitialStatus                = null,
                                      UInt16?                                              MaxAdminStatusScheduleSize   = null,
                                      UInt16?                                              MaxStatusScheduleSize        = null,

                                      String?                                              DataSource                   = null,
                                      DateTimeOffset?                                      Created                      = null,
                                      DateTimeOffset?                                      LastChange                   = null,

                                      CustomDataNew?                                       CustomData                   = null,
                                      UserDefinedDictionary?                               InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   InitialAdminStatus         ?? new Timestamped<RoamingNetworkOperatorAdminStatusType>(Created ?? Timestamp.Now, RoamingNetworkOperatorAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<RoamingNetworkOperatorStatusType>     (Created ?? Timestamp.Now, RoamingNetworkOperatorStatusType.Available),
                   MaxAdminStatusScheduleSize ?? DefaultMaxAdminStatusScheduleSize,
                   MaxStatusScheduleSize      ?? DefaultMaxStatusScheduleSize,

                   DataSource,
                   Created,
                   LastChange,

                   CustomData,
                   InternalData)

        {

            this.Address             = Address;
            this.Homepage            = Homepage;
            this.EMail               = EMail;
            this.HotlinePhoneNumber  = HotlinePhoneNumber;
            this.HotlineEMail        = HotlineEMail;

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the roaming network operator '{Id}'");

            this.KeyStatement        = KeyStatement;
            this.keyStatementError   = new (CheckKeyStatement);

        }

        #endregion


        #region (internal) KeyStatementError(RoamingNetworkId)

        private readonly Lazy<String?> keyStatementError;

        /// <summary>
        /// Why its key statement does not fit it in the given roaming network, or null: it states this operator and network,
        /// only signing and identity keys this operator names, and is signed by one of its identity keys valid then. Its
        /// validity in time is the one of the verifier: a version keeps a key statement that expired.
        /// </summary>
        internal String? KeyStatementError(RoamingNetwork_Id RoamingNetworkId)

            => KeyStatement is null
                   ? null
                   : KeyStatement.RoamingNetworkId != RoamingNetworkId
                         ? $"The key statement of the roaming network operator '{Id}' is one of the roaming network '{KeyStatement.RoamingNetworkId}', not of '{RoamingNetworkId}'!"
                         : keyStatementError.Value;

        private String? CheckKeyStatement()
        {

            if (KeyStatement is null)
                return null;

            if (KeyStatement.RoamingNetworkOperatorId != Id)
                return $"The key statement of the roaming network operator '{Id}' is one of '{KeyStatement.RoamingNetworkOperatorId}'!";

            if (KeyStatement.SigningKeys.FirstOrDefault(key => !SigningKeys.Contains(key)) is POIPublicKey unknownSigningKey)
                return $"The key statement of the roaming network operator '{Id}' names the signing key '{unknownSigningKey.Id}', which is not one of its signing keys!";

            if (KeyStatement.IdentityKeys.FirstOrDefault(key => !IdentityKeys.Contains(key)) is POIPublicKey unknownIdentityKey)
                return $"The key statement of the roaming network operator '{Id}' names the identity key '{unknownIdentityKey.Id}', which is not one of its identity keys!";

            return KeyStatement.TryVerify(IdentityKeys.Select(key => key.Id), IdentityKeys, out var error)
                       ? null
                       : $"The key statement of the roaming network operator '{Id}' is not signed by one of its identity keys: {error}";

        }

        #endregion


        #region (Admin-)Status management

        private readonly Lock                                                 statusEventsLock = new ();
        private          OnRoamingNetworkOperatorAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnRoamingNetworkOperatorStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnRoamingNetworkOperatorAdminStatusChangedDelegate? OnAdminStatusChanged
        {

            add
            {
                lock (statusEventsLock)
                {
                    onAdminStatusChanged += value;
                    ListenToAdminStatus(onAdminStatusChanged is not null);
                }
            }

            remove
            {
                lock (statusEventsLock)
                {
                    onAdminStatusChanged -= value;
                    ListenToAdminStatus(onAdminStatusChanged is not null);
                }
            }

        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnRoamingNetworkOperatorStatusChangedDelegate? OnStatusChanged
        {

            add
            {
                lock (statusEventsLock)
                {
                    onStatusChanged += value;
                    ListenToStatus(onStatusChanged is not null);
                }
            }

            remove
            {
                lock (statusEventsLock)
                {
                    onStatusChanged -= value;
                    ListenToStatus(onStatusChanged is not null);
                }
            }

        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                      Timestamp,
                                                                   EventTracking_Id                                    EventTrackingId,
                                                                   Timestamped<RoamingNetworkOperatorAdminStatusType>  NewStatus,
                                                                   Timestamped<RoamingNetworkOperatorAdminStatusType>  OldStatus,
                                                                   Context?                                            DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                                 Timestamp,
                                                              EventTracking_Id                               EventTrackingId,
                                                              Timestamped<RoamingNetworkOperatorStatusType>  NewStatus,
                                                              Timestamped<RoamingNetworkOperatorStatusType>  OldStatus,
                                                              Context?                                       DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region (static) Parse    (JSON, ...)

        /// <summary>
        /// Parse the given JSON representation of a roaming network.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="CustomRoamingNetworkOperatorParser">An optional delegate to parse custom roaming network operator JSON objects.</param>
        public static RoamingNetworkOperator Parse(JObject                                               JSON,
                                                   CustomJObjectParserDelegate<RoamingNetworkOperator>?  CustomRoamingNetworkOperatorParser   = null)
        {

            if (TryParse(JSON,
                         out var roamingNetworkOperator,
                         out var errorResponse,
                         CustomRoamingNetworkOperatorParser))
            {
                return roamingNetworkOperator;
            }

            throw new ArgumentException("The given JSON representation of a roaming network operator is invalid: " + errorResponse,
                                        nameof(JSON));

        }

        #endregion

        #region (static) TryParse (JSON, out RoamingNetworkOperator, out ErrorResponse)

        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkOperator?      RoamingNetworkOperator,
                                       [NotNullWhen(false)] out String?                      ErrorResponse)

            => TryParse(JSON,
                        out RoamingNetworkOperator,
                        out ErrorResponse,
                        null);


        /// <summary>
        /// Try to parse the given JSON representation of a roaming network operator.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="RoamingNetworkOperator">The parsed roaming network operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CustomRoamingNetworkOperatorParser">An optional custom parser.</param>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkOperator?      RoamingNetworkOperator,
                                       [NotNullWhen(false)] out String?                      ErrorResponse,
                                       CustomJObjectParserDelegate<RoamingNetworkOperator>?  CustomRoamingNetworkOperatorParser)
        {

            RoamingNetworkOperator  = null;
            ErrorResponse           = null;

            try
            {

                if (JSON?.HasValues != true)
                {
                    ErrorResponse = "The given JSON object must not be null or empty!";
                    return false;
                }

                #region Parse Id                 [mandatory]

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "roaming network operator identification",
                                         RoamingNetworkOperator_Id.TryParse,
                                         out RoamingNetworkOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                #endregion

                #region Check JSON-LD context    [optional]

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the roaming network operator '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                #endregion

                #region Parse Name               [optional]

                if (JSON.ParseOptionalJSON(BasePropertyKeys.Name,
                                           "roaming network operator name",
                                           ImmutableI18NString.TryParse,
                                           out ImmutableI18NString? name,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion

                #region Parse Description        [optional]

                if (JSON.ParseOptionalJSON(BasePropertyKeys.Description,
                                           "network description",
                                           I18NString.TryParse,
                                           out I18NString? description,
                                           out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion




                #region Parse DataSource         [optional]

                if (JSON.ParseOptional(BasePropertyKeys.DataSource,
                                       "data source",
                                       out String? dataSource,
                                       out ErrorResponse))
                {
                    if (ErrorResponse is not null)
                        return false;
                }

                #endregion


                if (!POIJSON.TryParseAddress(JSON, PropertyKeys.Address,              "address",                out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, PropertyKeys.Homepage,             "homepage",               URL.        TryParse, out URL?          homepage, out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, PropertyKeys.HotlinePhoneNumber,   "hotline phone number",   PhoneNumber.TryParse, out PhoneNumber?  hotline,  out ErrorResponse) ||
                    !TryParseEMailAddress   (JSON[ PropertyKeys.EMailAddress],        "e-mail address",         out var eMail,        out ErrorResponse) ||
                    !TryParseEMailAddress   (JSON[ PropertyKeys.HotlineEMailAddress], "hotline e-mail address", out var hotlineEMail, out ErrorResponse))
                {
                    return false;
                }

                if (!POIPartyKeys.TryParse(JSON, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                RoamingNetworkKeyStatement? keyStatement = null;

                if (JSON[PropertyKeys.KeyStatement] is JToken keyStatementJSON)
                {

                    if (keyStatementJSON is not JObject keyStatementObject ||
                        !RoamingNetworkKeyStatement.TryParse(keyStatementObject, out keyStatement, out ErrorResponse))
                    {
                        ErrorResponse ??= "The key statement must be a JSON object!";
                        return false;
                    }

                }

                if (!POIEntityMetadata.TryParse(JSON,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                RoamingNetworkOperator = new RoamingNetworkOperator(

                                             id,
                                             name,
                                             description,
                                             address,
                                             homepage,
                                             eMail,
                                             hotline,
                                             hotlineEMail,
                                             signingKeys,
                                             identityKeys,
                                             keyStatement,

                                             DataSource:  dataSource,
                                             Created:     created,
                                             LastChange:  lastChange,

                                             CustomData:  customData

                                         );

                #region Check ETags          [optional]

                //if (JSON[BasePropertyKeys.ETags] is JToken eTagsJSON &&
                //    !TryCheckETags(RoamingNetworkOperator,
                //                   eTagsJSON is JArray eTagsArray
                //                       ? [ .. eTagsArray.Select(eTag => ETag.Parse(eTag)) ]
                //                       : [],
                //                   out ErrorResponse))
                //{
                //    RoamingNetworkOperator = null;
                //    return false;
                //}

                #endregion

                if (CustomRoamingNetworkOperatorParser is not null)
                    RoamingNetworkOperator = CustomRoamingNetworkOperatorParser(JSON,
                                                                                RoamingNetworkOperator);

                return true;

            }
            catch (Exception e)
            {
                RoamingNetworkOperator  = null;
                ErrorResponse           = "The given JSON representation of a roaming network operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON (Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this roaming network operator.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        public JObject ToJSON(Boolean                                                   Embedded                                 = false,
                              Boolean                                                   IncludeETags                             = false,
                              ETagDigestEncoding?                                       ETagDigestEncoding                       = null,
                              CustomJObjectSerializerDelegate<RoamingNetworkOperator>?  CustomRoamingNetworkOperatorSerializer   = null)
        {

            var json = JSONObject.Create(

                                 new JProperty(BasePropertyKeys.Id,                    Id.                      ToString()),

                          !Embedded
                               ? new JProperty(BasePropertyKeys.Context,               JSONLDContext)
                               : null,

                           Name.IsNotNullOrEmpty()
                               ? new JProperty(BasePropertyKeys.Name,                  Name.                    ToJSON())
                               : null,

                           Description.IsNotNullOrEmpty()
                               ? new JProperty(BasePropertyKeys.Description,           Description.             ToJSON())
                               : null,

                           DataSource is not null
                               ? new JProperty(BasePropertyKeys.DataSource,            DataSource)
                               : null,

                           Address is not null
                               ? new JProperty(PropertyKeys.    Address,               Address.                 ToJSON(Embedded: true))
                               : null,

                           Homepage.HasValue
                               ? new JProperty(PropertyKeys.    Homepage,              Homepage.Value.          ToString())
                               : null,

                           EMail is not null
                               ? new JProperty(PropertyKeys.    EMailAddress,          EMail.                   ToString())
                               : null,

                           HotlinePhoneNumber.HasValue
                               ? new JProperty(PropertyKeys.    HotlinePhoneNumber,    HotlinePhoneNumber.Value.ToString())
                               : null,

                           HotlineEMail is not null
                               ? new JProperty(PropertyKeys.    HotlineEMailAddress,   HotlineEMail.            ToString())
                               : null,

                           KeyStatement is not null
                               ? new JProperty(PropertyKeys.    KeyStatement,          KeyStatement.            ToJSON(Embedded: true))
                               : null,

                           IncludeETags && ETags.Length > 0
                               ? new JProperty(BasePropertyKeys.ETags,                 new JArray(ETags.Select(eTag => eTag.ToJSON(ETagDigestEncoding ?? DefaultETagDigestEncoding))))
                               : null

            );

            POIPartyKeys.     AddTo(json, this);
            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return CustomRoamingNetworkOperatorSerializer is not null
                       ? CustomRoamingNetworkOperatorSerializer(this, json)
                       : json;

        }

        #endregion


        #region (static) Parse    (CBOR, ...)

        /// <summary>
        /// Parse the given CBOR representation of a roaming network operator.
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="CustomRoamingNetworkOperatorParser">An optional delegate to parse custom roaming network operator CBOR maps.</param>
        public static RoamingNetworkOperator Parse(CBORValue                                          CBOR,
                                                   CustomCBORParserDelegate<RoamingNetworkOperator>?  CustomRoamingNetworkOperatorParser   = null)
        {

            if (TryParse(CBOR,
                         out var roamingNetworkOperator,
                         out var errorResponse,
                         CustomRoamingNetworkOperatorParser))
            {
                return roamingNetworkOperator;
            }

            throw new ArgumentException("The given CBOR representation of a roaming network operator is invalid: " + errorResponse,
                                        nameof(CBOR));

        }

        #endregion

        #region (static) TryParse (CBOR, out RoamingNetworkOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a roaming network operator: the keys and values of its JSON
        /// representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="RoamingNetworkOperator">The parsed roaming network operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                          CBOR,
                                       [NotNullWhen(true)]  out RoamingNetworkOperator?   RoamingNetworkOperator,
                                       [NotNullWhen(false)] out String?                   ErrorResponse,
                                       CustomCBORParserDelegate<RoamingNetworkOperator>?  CustomRoamingNetworkOperatorParser   = null)
        {

            RoamingNetworkOperator = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of a roaming network operator must be a map!";
                    return false;
                }

                #region Parse Id, Name, Description, DataSource

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "roaming network operator identification",
                                         RoamingNetworkOperator_Id.TryParse,
                                         out RoamingNetworkOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                CBOR.ParseOptional    (BasePropertyKeys.Name,         "roaming network operator name",         I18NString.TryParse, out I18NString? name,         out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptional    (BasePropertyKeys.Description,  "roaming network operator description",  I18NString.TryParse, out I18NString? description,  out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource,   "data source",                                                out var         dataSource,   out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                #endregion

                #region Parse Homepage, EMail, Hotline, HotlineEMail

                if (!TryParseCBORText(CBOR, PropertyKeys.Homepage,            "homepage",              URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !TryParseCBORText(CBOR, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",  PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
                {
                    return false;
                }

                CBOR.ParseOptionalText(PropertyKeys.EMailAddress,         "e-mail address",          out var eMailText,         out ErrorResponse);
                if (ErrorResponse is not null || !TryParseEMailAddress(eMailText,         "e-mail address",          out var eMail,         out ErrorResponse))
                    return false;

                CBOR.ParseOptionalText(PropertyKeys.HotlineEMailAddress,  "hotline e-mail address",  out var hotlineEMailText,  out ErrorResponse);
                if (ErrorResponse is not null || !TryParseEMailAddress(hotlineEMailText,  "hotline e-mail address",  out var hotlineEMail,  out ErrorResponse))
                    return false;

                #endregion

                #region Parse Address, SigningKeys, IdentityKeys, KeyStatement, Created, LastChange, CustomData

                // What has no CBOR representation of its own is read as its JSON, with the parsers of the JSON
                // representation: the address, the keys, the key statement and the metadata.
                var json = new JObject();

                foreach (var key in new[] { PropertyKeys.Address,
                                            PropertyKeys.SigningKeys,
                                            PropertyKeys.IdentityKeys,
                                            PropertyKeys.KeyStatement,
                                            PropertyKeys.Created,
                                            PropertyKeys.LastChange,
                                            PropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                if (!POIJSON.TryParseAddress(json, PropertyKeys.Address, "address", out var address, out ErrorResponse))
                    return false;

                if (!POIPartyKeys.TryParse(json, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                RoamingNetworkKeyStatement? keyStatement = null;

                if (json[PropertyKeys.KeyStatement] is JToken keyStatementJSON)
                {

                    if (keyStatementJSON is not JObject keyStatementObject ||
                        !RoamingNetworkKeyStatement.TryParse(keyStatementObject, out keyStatement, out ErrorResponse))
                    {
                        ErrorResponse ??= "The key statement must be a map!";
                        return false;
                    }

                }

                if (!POIEntityMetadata.TryParse(json,
                                                out var created,
                                                out var lastChange,
                                                out var customData,
                                                out ErrorResponse))
                {
                    return false;
                }

                #endregion

                RoamingNetworkOperator = new RoamingNetworkOperator(

                                             id,
                                             name,
                                             description,
                                             address,
                                             homepage,
                                             eMail,
                                             hotline,
                                             hotlineEMail,
                                             signingKeys,
                                             identityKeys,
                                             keyStatement,

                                             DataSource:  dataSource,
                                             Created:     created,
                                             LastChange:  lastChange,

                                             CustomData:  customData

                                         );

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                RoamingNetworkOperator  = null;
                ErrorResponse           = "The given CBOR representation of a roaming network operator is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToCBOR (Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this roaming network operator.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        public CBORValue ToCBOR(Boolean                                                Embedded                                 = false,
                                Boolean                                                IncludeETags                             = false,
                                ETagDigestEncoding?                                    ETagDigestEncoding                       = null,
                                CustomCBORSerializerDelegate<RoamingNetworkOperator>?  CustomRoamingNetworkOperatorSerializer   = null)
        {

            var cbor = new CBORMap {
                           { CBORValue.FromText(BasePropertyKeys.Id),             CBORValue.FromText(Id.ToString()) }
                       };

            if (!Embedded)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Context),            CBORValue.FromText(JSONLDContext));

            if (Name.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Name),               Name.       ToCBOR());

            if (Description.IsNotNullOrEmpty())
                cbor.Add(CBORValue.FromText(BasePropertyKeys.Description),        Description.ToCBOR());

            if (DataSource is not null)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.DataSource),         CBORValue.FromText(DataSource));

            // The address, the keys and the key statement have no CBOR representation of their own: theirs are the keys
            // and values of their JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),                CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),               CBORValue.FromText(Homepage.Value.ToString()));

            if (EMail is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.EMailAddress),           CBORValue.FromText(EMail.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),     CBORValue.FromText(HotlinePhoneNumber.Value.ToString()));

            if (HotlineEMail is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlineEMailAddress),    CBORValue.FromText(HotlineEMail.ToString()));

            if (SigningKeys.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.SigningKeys),
                         CBORValue.FromArray(SigningKeys. Select(key => CBORJSON.ToCBOR(key.ToJSON(Embedded: true)))));

            if (IdentityKeys.Length > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.IdentityKeys),
                         CBORValue.FromArray(IdentityKeys.Select(key => CBORJSON.ToCBOR(key.ToJSON(Embedded: true)))));

            if (KeyStatement is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.KeyStatement),           CBORJSON.ToCBOR(KeyStatement.ToJSON(Embedded: true)));

            // Always part of the content, as in the JSON representation; custom data is JSON.
            cbor.Add(CBORValue.FromText(PropertyKeys.Created),                    CBORValue.FromText(Created.       ToISO8601()));
            cbor.Add(CBORValue.FromText(PropertyKeys.LastChange),                 CBORValue.FromText(LastChangeDate.ToISO8601()));

            if (CustomData.HasValues)
                cbor.Add(CBORValue.FromText(PropertyKeys.CustomData),             CBORJSON.ToCBOR(CustomData.ToJObject()));

            // The ETags describe the content without them, irrespective of the view options.
            if (IncludeETags && ETags.Length > 0)
                cbor.Add(CBORValue.FromText(BasePropertyKeys.ETags),              CBORValue.FromArray(ETags.Select(eTag => eTag.ToCBOR())));

            return CustomRoamingNetworkOperatorSerializer is not null
                       ? CustomRoamingNetworkOperatorSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region (private static) TryParseCBORText(CBOR, PropertyName, PropertyDescription, Parser, out Value, out ErrorResponse)

        /// <summary>
        /// An optional text of the given CBOR map, parsed - as POIJSON.TryParseText does in JSON.
        /// </summary>
        private static Boolean TryParseCBORText<T>(CBORValue                         CBOR,
                                                   String                            PropertyName,
                                                   String                            PropertyDescription,
                                                   TryParser<T>                      Parser,
                                                   out T?                            Value,
                                                   [NotNullWhen(false)] out String?  ErrorResponse)

            where T : struct

        {

            Value = null;

            if (!CBOR.TryGetValue(CBORValue.FromText(PropertyName), out var token))
            {
                ErrorResponse = null;
                return true;
            }

            if (!token.TryGetText(out var text) || !Parser(text, out var value))
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{token}'!";
                return false;
            }

            Value          = value;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (private static) TryParseEMailAddress(Value, PropertyDescription, out EMailAddress, out ErrorResponse)

        /// <summary>
        /// An optional e-mail address, as its text: "info@example.org", or with the name of its owner,
        /// "Example Hotline &lt;hotline@example.org&gt;", as EMailAddress.ToString() writes it.
        /// </summary>
        private static Boolean TryParseEMailAddress(JToken?                           Value,
                                                    String                            PropertyDescription,
                                                    out EMailAddress?                 EMailAddress,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (Value is null)
            {
                EMailAddress   = null;
                ErrorResponse  = null;
                return true;
            }

            if (Value.Type != JTokenType.String)
            {
                EMailAddress   = null;
                ErrorResponse  = $"Invalid {PropertyDescription} '{Value}'!";
                return false;
            }

            return TryParseEMailAddress(Value.Value<String>(), PropertyDescription, out EMailAddress, out ErrorResponse);

        }

        private static Boolean TryParseEMailAddress(String?                           Text,
                                                    String                            PropertyDescription,
                                                    out EMailAddress?                 EMailAddress,
                                                    [NotNullWhen(false)] out String?  ErrorResponse)
        {

            EMailAddress   = null;
            ErrorResponse  = null;

            if (Text is null)
                return true;

            try
            {
                EMailAddress = EMailAddress.Parse(Text);
            }
            catch (Exception)
            { }

            if (EMailAddress is null)
            {
                ErrorResponse = $"Invalid {PropertyDescription} '{Text}'!";
                return false;
            }

            return true;

        }

        #endregion



        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two roaming network operators for equality by their identifications.
        /// </summary>
        public static Boolean operator == (RoamingNetworkOperator? RoamingNetworkOperator1, RoamingNetworkOperator? RoamingNetworkOperator2)

            => ReferenceEquals(RoamingNetworkOperator1, RoamingNetworkOperator2) ||
               (RoamingNetworkOperator1 is not null && RoamingNetworkOperator1.Equals(RoamingNetworkOperator2));

        /// <summary>
        /// Compares two roaming network operators for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (RoamingNetworkOperator? RoamingNetworkOperator1, RoamingNetworkOperator? RoamingNetworkOperator2)

            => !(RoamingNetworkOperator1 == RoamingNetworkOperator2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is RoamingNetworkOperator roamingNetworkOperator
                   ? CompareTo(roamingNetworkOperator)
                   : throw new ArgumentException("The given object is not a roaming network operator!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(RoamingNetworkOperator? RoamingNetworkOperator)

            => RoamingNetworkOperator is null
                   ? throw new ArgumentNullException(nameof(RoamingNetworkOperator), "The given roaming network operator must not be null!")
                   : Id.CompareTo(RoamingNetworkOperator.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is RoamingNetworkOperator roamingNetworkOperator &&
               Equals(roamingNetworkOperator);

        /// <inheritdoc/>
        public Boolean Equals(RoamingNetworkOperator? RoamingNetworkOperator)

            => RoamingNetworkOperator is not null &&
               Id.Equals(RoamingNetworkOperator.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
