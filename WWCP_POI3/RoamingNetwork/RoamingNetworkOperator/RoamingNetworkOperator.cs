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
    /// The operator of a roaming network, embedded in the roaming network it operates as its "roamingNetworkOperator".
    /// One operator may operate many roaming networks, e.g. a production and a QA network: each of them embeds it
    /// with its keys, so that every one of them can be verified from its own history alone.
    /// </summary>
    /// <remarks>
    /// The roaming network operator owns the roaming network itself, its registries of manufacturers, energy meter
    /// manufacturers and transparency software, and decides who is a member: it adds and removes the other parties.
    /// It signs in the place of a party without keys, and its identity keys recover the keys of every other party.
    /// It never changes the data of another party.
    /// </remarks>
    public sealed class RoamingNetworkOperator : AImmutableEMobilityEntity<RoamingNetworkOperator_Id,
                                                                           RoamingNetworkOperatorAdminStatusType,
                                                                           RoamingNetworkOperatorStatusType>,
                                                 IEquatable<RoamingNetworkOperator>,
                                                 IComparable<RoamingNetworkOperator>,
                                                 IPOIParty
    {

        #region Data

        /// <summary>
        /// The property of a roaming network embedding its operator.
        /// </summary>
        public const String JSONProperty = "roamingNetworkOperator";

        #endregion

        #region Properties

        /// <summary>
        /// The address of the headquarters.
        /// </summary>
        public Address?      Address               { get; }

        /// <summary>
        /// The homepage.
        /// </summary>
        public URL?          Homepage              { get; }

        /// <summary>
        /// The hotline phone number.
        /// </summary>
        public PhoneNumber?  HotlinePhoneNumber    { get; }

        /// <summary>
        /// The keys signing the change sets and commits of the roaming network operator, in the order of their identifications.
        /// </summary>
        public ImmutableArray<POIPublicKey>  SigningKeys     { get; }

        /// <summary>
        /// The identity keys of the roaming network operator, signing changes of its own keys
        /// and, for recovery, of the keys of other parties, in the order of their identifications.
        /// </summary>
        public ImmutableArray<POIPublicKey>  IdentityKeys    { get; }

        /// <summary>
        /// Its key statement, if any: which signing keys it uses, signed with one of its identity keys, so that an app pinning
        /// its identity keys trusts them (SPECIFICATION.md, section 8.6).
        /// </summary>
        public RoamingNetworkKeyStatement?   KeyStatement    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new roaming network operator.
        /// </summary>
        public RoamingNetworkOperator(RoamingNetworkOperator_Id                               Id,
                                      I18NString?                                             Name                         = null,
                                      I18NString?                                             Description                  = null,
                                      Address?                                                Address                      = null,
                                      URL?                                                    Homepage                     = null,
                                      PhoneNumber?                                            HotlinePhoneNumber           = null,
                                      IEnumerable<POIPublicKey>?                              SigningKeys                  = null,
                                      IEnumerable<POIPublicKey>?                              IdentityKeys                 = null,

                                      Timestamped<RoamingNetworkOperatorAdminStatusType>?     InitialAdminStatus           = null,
                                      Timestamped<RoamingNetworkOperatorStatusType>?          InitialStatus                = null,
                                      UInt16?                                                 MaxAdminStatusScheduleSize   = null,
                                      UInt16?                                                 MaxStatusScheduleSize        = null,

                                      String?                                                 DataSource                   = null,
                                      DateTimeOffset?                                         Created                      = null,
                                      DateTimeOffset?                                         LastChange                   = null,

                                      CustomDataNew?                                          CustomData                   = null,
                                      UserDefinedDictionary?                                  InternalData                 = null,
                                      RoamingNetworkKeyStatement?                             KeyStatement                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
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
            this.HotlinePhoneNumber  = HotlinePhoneNumber;

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


        #region Status events

        private readonly Lock                                                 statusEventsLock  = new ();
        private          OnRoamingNetworkOperatorAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnRoamingNetworkOperatorStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnRoamingNetworkOperatorAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnRoamingNetworkOperatorStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
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


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this roaming network operator, as its roaming networks embed it.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id", Id.ToString())
                       );

            if (Name.IsNotNullOrEmpty())
                json.Add("name",         Name.ToJSON());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",  Description.ToJSON());

            if (DataSource is not null)
                json.Add("dataSource",   DataSource);

            if (Address is not null)
                json.Add("address",      Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add("homepage",     Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add("hotline",      HotlinePhoneNumber.Value.ToString());

            POIPartyKeys.AddTo(json, this);

            if (KeyStatement is not null)
                json.Add(RoamingNetworkKeyStatement.JSONProperty, KeyStatement.ToJSON());

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return the CBOR representation of this roaming network operator: the keys and values of its JSON representation.
        /// </summary>
        public CBORValue ToCBOR()

            => CBORJSON.ToCBOR(ToJSON());

        #endregion

        #region (static) TryParse(JSON, out RoamingNetworkOperator, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a roaming network operator.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="RoamingNetworkOperator">The parsed roaming network operator.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                           JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkOperator?  RoamingNetworkOperator,
                                       [NotNullWhen(false)] out String?                  ErrorResponse)
        {

            RoamingNetworkOperator = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "roaming network operator identification",
                                         RoamingNetworkOperator_Id.TryParse,
                                         out RoamingNetworkOperator_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, "name",         "roaming network operator name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, "description",  "roaming network operator description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, "dataSource",   "data source",                           out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress(JSON,                                                          out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "homepage",     "homepage",                              URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "hotline",      "hotline phone number",                  PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
                {
                    return false;
                }

                if (!POIPartyKeys.TryParse(JSON, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                RoamingNetworkKeyStatement? keyStatement = null;

                if (JSON[RoamingNetworkKeyStatement.JSONProperty] is JToken keyStatementJSON)
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

                RoamingNetworkOperator = new RoamingNetworkOperator(id,
                                                                    name,
                                                                    description,
                                                                    address,
                                                                    homepage,
                                                                    hotline,
                                                                    SigningKeys:   signingKeys,
                                                                    IdentityKeys:  identityKeys,
                                                                    DataSource:    dataSource,
                                                                    Created:       created,
                                                                    LastChange:    lastChange,
                                                                    CustomData:    customData,
                                                                    KeyStatement:  keyStatement);

                ErrorResponse = null;
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
