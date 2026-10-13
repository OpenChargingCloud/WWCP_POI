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
    /// The admin status of an e-mobility provider changed.
    /// </summary>
    public delegate Task OnEMobilityProviderAdminStatusChangedDelegate(DateTimeOffset                                  Timestamp,
                                                                       EventTracking_Id                                EventTrackingId,
                                                                       EMobilityProvider                               EMobilityProvider,
                                                                       Timestamped<EMobilityProviderAdminStatusType>   OldStatus,
                                                                       Timestamped<EMobilityProviderAdminStatusType>   NewStatus);

    /// <summary>
    /// The status of an e-mobility provider changed.
    /// </summary>
    public delegate Task OnEMobilityProviderStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  EMobilityProvider                          EMobilityProvider,
                                                                  Timestamped<EMobilityProviderStatusType>   OldStatus,
                                                                  Timestamped<EMobilityProviderStatusType>   NewStatus);


    /// <summary>
    /// An e-mobility provider, embedded completely in its roaming network:
    /// it offers charging to EV drivers, e.g. by their contracts and authentication tokens.
    /// </summary>
    public sealed class EMobilityProvider : AImmutableEMobilityEntity<EMobilityProvider_Id,
                                                                      EMobilityProviderAdminStatusType,
                                                                      EMobilityProviderStatusType>,
                                            IEquatable<EMobilityProvider>,
                                            IComparable<EMobilityProvider>,
                                            IPOIParty
    {

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

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>  SigningKeys     { get; }

        /// <inheritdoc/>
        public ImmutableArray<POIPublicKey>  IdentityKeys    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new e-mobility provider.
        /// </summary>
        public EMobilityProvider(EMobilityProvider_Id                              Id,
                                 I18NString?                                       Name                         = null,
                                 I18NString?                                       Description                  = null,
                                 Address?                                          Address                      = null,
                                 URL?                                              Homepage                     = null,
                                 PhoneNumber?                                      HotlinePhoneNumber           = null,
                                 IEnumerable<POIPublicKey>?                        SigningKeys                  = null,
                                 IEnumerable<POIPublicKey>?                        IdentityKeys                 = null,

                                 Timestamped<EMobilityProviderAdminStatusType>?    InitialAdminStatus           = null,
                                 Timestamped<EMobilityProviderStatusType>?         InitialStatus                = null,
                                 UInt16?                                           MaxAdminStatusScheduleSize   = null,
                                 UInt16?                                           MaxStatusScheduleSize        = null,

                                 String?                                           DataSource                   = null,
                                 DateTimeOffset?                                   Created                      = null,
                                 DateTimeOffset?                                   LastChange                   = null,

                                 CustomDataNew?                                    CustomData                   = null,
                                 UserDefinedDictionary?                            InternalData                 = null)

            : base(Id,
                   Name,
                   Description,

                   // Since its creation, unless told otherwise.
                   InitialAdminStatus         ?? new Timestamped<EMobilityProviderAdminStatusType>(Created ?? Timestamp.Now, EMobilityProviderAdminStatusType.Operational),
                   InitialStatus              ?? new Timestamped<EMobilityProviderStatusType>     (Created ?? Timestamp.Now, EMobilityProviderStatusType.Available),
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

            (this.SigningKeys, this.IdentityKeys) = POIPartyKeys.Check(SigningKeys, IdentityKeys, $"the e-mobility provider '{Id}'");

        }

        #endregion


        #region Status events

        private readonly Lock                                            statusEventsLock  = new ();
        private          OnEMobilityProviderAdminStatusChangedDelegate?  onAdminStatusChanged;
        private          OnEMobilityProviderStatusChangedDelegate?       onStatusChanged;

        /// <summary>
        /// An event fired whenever the admin status changed.
        /// </summary>
        public event OnEMobilityProviderAdminStatusChangedDelegate? OnAdminStatusChanged
        {
            add    { lock (statusEventsLock) { onAdminStatusChanged += value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onAdminStatusChanged -= value; ListenToAdminStatus(onAdminStatusChanged is not null); } }
        }

        /// <summary>
        /// An event fired whenever the status changed.
        /// </summary>
        public event OnEMobilityProviderStatusChangedDelegate? OnStatusChanged
        {
            add    { lock (statusEventsLock) { onStatusChanged += value; ListenToStatus(onStatusChanged is not null); } }
            remove { lock (statusEventsLock) { onStatusChanged -= value; ListenToStatus(onStatusChanged is not null); } }
        }

        /// <inheritdoc/>
        protected override async Task OnAdminStatusScheduleChanged(DateTimeOffset                                 Timestamp,
                                                                   EventTracking_Id                               EventTrackingId,
                                                                   Timestamped<EMobilityProviderAdminStatusType>  NewStatus,
                                                                   Timestamped<EMobilityProviderAdminStatusType>  OldStatus,
                                                                   Context?                                       DataSource)
        {
            var onAdminStatusChanged = this.onAdminStatusChanged;
            if (onAdminStatusChanged is not null)
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        /// <inheritdoc/>
        protected override async Task OnStatusScheduleChanged(DateTimeOffset                            Timestamp,
                                                              EventTracking_Id                          EventTrackingId,
                                                              Timestamped<EMobilityProviderStatusType>  NewStatus,
                                                              Timestamped<EMobilityProviderStatusType>  OldStatus,
                                                              Context?                                  DataSource)
        {
            var onStatusChanged = this.onStatusChanged;
            if (onStatusChanged is not null)
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this e-mobility provider, as its roaming network embeds it.
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

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return the CBOR representation of this e-mobility provider: the keys and values of its JSON representation.
        /// </summary>
        public CBORValue ToCBOR()

            => CBORJSON.ToCBOR(ToJSON());

        #endregion

        #region (static) TryParse(JSON, out EMobilityProvider, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of an e-mobility provider.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="EMobilityProvider">The parsed e-mobility provider.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JObject                                      JSON,
                                       [NotNullWhen(true)]  out EMobilityProvider?  EMobilityProvider,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            EMobilityProvider = null;

            try
            {

                if (!JSON.ParseMandatory("@id",
                                         "e-mobility provider identification",
                                         EMobilityProvider_Id.TryParse,
                                         out EMobilityProvider_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, "name",         "e-mobility provider name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, "description",  "e-mobility provider description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, "dataSource",   "data source",                      out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress(JSON,                                                     out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "homepage",     "homepage",                         URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, "hotline",      "hotline phone number",             PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
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

                EMobilityProvider = new EMobilityProvider(id,
                                                          name,
                                                          description,
                                                          address,
                                                          homepage,
                                                          hotline,
                                                          SigningKeys:   signingKeys,
                                                          IdentityKeys:  identityKeys,
                                                          DataSource:  dataSource,
                                                          Created:     created,
                                                          LastChange:  lastChange,
                                                          CustomData:  customData);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EMobilityProvider  = null;
                ErrorResponse      = "The given JSON representation of an e-mobility provider is invalid: " + e.Message;
                return false;
            }

        }

        #endregion


        #region Operator overloading, IEquatable, IComparable

        /// <summary>
        /// Compares two e-mobility providers for equality by their identifications.
        /// </summary>
        public static Boolean operator == (EMobilityProvider? EMobilityProvider1, EMobilityProvider? EMobilityProvider2)

            => ReferenceEquals(EMobilityProvider1, EMobilityProvider2) ||
               (EMobilityProvider1 is not null && EMobilityProvider1.Equals(EMobilityProvider2));

        /// <summary>
        /// Compares two e-mobility providers for inequality by their identifications.
        /// </summary>
        public static Boolean operator != (EMobilityProvider? EMobilityProvider1, EMobilityProvider? EMobilityProvider2)

            => !(EMobilityProvider1 == EMobilityProvider2);

        /// <inheritdoc/>
        public override Int32 CompareTo(Object? Object)

            => Object is EMobilityProvider eMobilityProvider
                   ? CompareTo(eMobilityProvider)
                   : throw new ArgumentException("The given object is not an e-mobility provider!", nameof(Object));

        /// <inheritdoc/>
        public Int32 CompareTo(EMobilityProvider? EMobilityProvider)

            => EMobilityProvider is null
                   ? throw new ArgumentNullException(nameof(EMobilityProvider), "The given e-mobility provider must not be null!")
                   : Id.CompareTo(EMobilityProvider.Id);

        /// <inheritdoc/>
        public override Boolean Equals(Object? Object)

            => Object is EMobilityProvider eMobilityProvider &&
               Equals(eMobilityProvider);

        /// <inheritdoc/>
        public Boolean Equals(EMobilityProvider? EMobilityProvider)

            => EMobilityProvider is not null &&
               Id.Equals(EMobilityProvider.Id);

        /// <inheritdoc/>
        public override Int32 GetHashCode()

            => Id.GetHashCode();

        /// <inheritdoc/>
        public override String ToString()

            => Id.ToString();

        #endregion

    }

}
