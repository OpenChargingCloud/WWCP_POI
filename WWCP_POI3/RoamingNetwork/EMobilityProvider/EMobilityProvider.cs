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
                                                                       Timestamped<EMobilityProviderAdminStatusType>   NewStatus,
                                                                       Context?                                        DataSource);

    /// <summary>
    /// The status of an e-mobility provider changed.
    /// </summary>
    public delegate Task OnEMobilityProviderStatusChangedDelegate(DateTimeOffset                             Timestamp,
                                                                  EventTracking_Id                           EventTrackingId,
                                                                  EMobilityProvider                          EMobilityProvider,
                                                                  Timestamped<EMobilityProviderStatusType>   OldStatus,
                                                                  Timestamped<EMobilityProviderStatusType>   NewStatus,
                                                                  Context?                                   DataSource);


    /// <summary>
    /// An e-mobility provider, embedded completely in its roaming network:
    /// it offers charging to EV drivers, e.g. by their contracts and authentication tokens.
    /// Its groups of EVSEs, charging stations and charging pools name members of any operator of the network, by weak
    /// references: a member that does not exist (any more) is left out, and its operator removes it without the provider.
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

        /// <summary>
        /// Its groups of EVSEs of any operator.
        /// </summary>
        public POICollection<EVSEGroup_Id,            EVSEGroup>             EVSEGroups               { get; } = POICollection<EVSEGroup_Id,            EVSEGroup>.           Empty;

        /// <summary>
        /// Its groups of charging stations of any operator.
        /// </summary>
        public POICollection<ChargingStationGroup_Id, ChargingStationGroup>  ChargingStationGroups    { get; } = POICollection<ChargingStationGroup_Id, ChargingStationGroup>.Empty;

        /// <summary>
        /// Its groups of charging pools of any operator.
        /// </summary>
        public POICollection<ChargingPoolGroup_Id,    ChargingPoolGroup>     ChargingPoolGroups       { get; } = POICollection<ChargingPoolGroup_Id,    ChargingPoolGroup>.   Empty;

        /// <summary>
        /// Its charging tariff offers for its customers, at the EVSEs of any operator.
        /// </summary>
        public POICollection<ChargingTariffOffer_Id,  ChargingTariffOffer>   ChargingTariffOffers     { get; } = POICollection<ChargingTariffOffer_Id,  ChargingTariffOffer>. Empty;

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
                                 IEnumerable<EVSEGroup>?                           EVSEGroups                   = null,
                                 IEnumerable<ChargingStationGroup>?                ChargingStationGroups        = null,
                                 IEnumerable<ChargingPoolGroup>?                   ChargingPoolGroups           = null,
                                 IEnumerable<ChargingTariffOffer>?                 ChargingTariffOffers         = null,

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

            #region Its groups: named by its own identification, of members of any operator

            this.EVSEGroups             = new (EVSEGroups,            group => group.Id, "EVSE group",             $"the e-mobility provider '{Id}'");
            this.ChargingStationGroups  = new (ChargingStationGroups, group => group.Id, "charging station group", $"the e-mobility provider '{Id}'");
            this.ChargingPoolGroups     = new (ChargingPoolGroups,    group => group.Id, "charging pool group",    $"the e-mobility provider '{Id}'");

            foreach (var groupId in this.EVSEGroups.           Select(group => (Id: group.Id.ToString(), group.Id.OperatorId)).
                             Concat(this.ChargingStationGroups.Select(group => (Id: group.Id.ToString(), group.Id.OperatorId))).
                             Concat(this.ChargingPoolGroups.   Select(group => (Id: group.Id.ToString(), group.Id.OperatorId))))
            {
                if (!IsOwnPrefix(groupId.OperatorId))
                    throw new ArgumentException($"The group '{groupId.Id}' does not belong to the e-mobility provider '{Id}'!", nameof(EVSEGroups));
            }

            #endregion

            #region Its charging tariff offers: for sessions with its own contracts

            this.ChargingTariffOffers = new (ChargingTariffOffers, offer => offer.Id, "charging tariff offer", $"the e-mobility provider '{Id}'");

            foreach (var offer in this.ChargingTariffOffers)
                if (offer.Sessions.HasValue || !offer.EMobilityProviderIds.IsEmpty)
                    throw new ArgumentException($"The charging tariff offer '{offer.Id}' of the e-mobility provider '{Id}' is for sessions with its own contracts: it must name neither sessions nor e-mobility providers!", nameof(ChargingTariffOffers));

            #endregion

        }

        #endregion


        #region IsOwnPrefix(Prefix)

        /// <summary>
        /// Whether the given prefix of the identification of a group, written as the identification of a charging station
        /// operator, is the identification of this e-mobility provider.
        /// </summary>
        /// <param name="Prefix">The prefix of the identification of a group.</param>
        public Boolean IsOwnPrefix(ChargingStationOperator_Id Prefix)

            => EMobilityProvider_Id.TryParse(Prefix.ToString(), out var providerId) &&
               providerId == Id;

        #endregion

        #region TryGetEVSEGroup / TryGetChargingStationGroup / TryGetChargingPoolGroup

        /// <summary>
        /// Try to get its EVSE group with the given identification.
        /// </summary>
        public Boolean TryGetEVSEGroup(EVSEGroup_Id                        EVSEGroupId,
                                       [NotNullWhen(true)] out EVSEGroup?  EVSEGroup)

            => EVSEGroups.TryGet(EVSEGroupId, out EVSEGroup);

        /// <summary>
        /// Try to get its charging station group with the given identification.
        /// </summary>
        public Boolean TryGetChargingStationGroup(ChargingStationGroup_Id                        ChargingStationGroupId,
                                                  [NotNullWhen(true)] out ChargingStationGroup?  ChargingStationGroup)

            => ChargingStationGroups.TryGet(ChargingStationGroupId, out ChargingStationGroup);

        /// <summary>
        /// Try to get its charging pool group with the given identification.
        /// </summary>
        public Boolean TryGetChargingPoolGroup(ChargingPoolGroup_Id                        ChargingPoolGroupId,
                                               [NotNullWhen(true)] out ChargingPoolGroup?  ChargingPoolGroup)

            => ChargingPoolGroups.TryGet(ChargingPoolGroupId, out ChargingPoolGroup);

        /// <summary>
        /// Try to get its charging tariff offer with the given identification.
        /// </summary>
        public Boolean TryGetChargingTariffOffer(ChargingTariffOffer_Id                        ChargingTariffOfferId,
                                                 [NotNullWhen(true)] out ChargingTariffOffer?  ChargingTariffOffer)

            => ChargingTariffOffers.TryGet(ChargingTariffOfferId, out ChargingTariffOffer);

        #endregion

        #region (internal) ShareRuntimeOfAll(Previous)

        /// <summary>
        /// Take the runtime status and internal data of the given previous version of this e-mobility provider, and of
        /// every group it had, by their identifications.
        /// </summary>
        internal void ShareRuntimeOfAll(EMobilityProvider Previous)
        {

            ShareRuntimeOf(Previous);

            foreach (var evseGroup in EVSEGroups)
                if (Previous.TryGetEVSEGroup(evseGroup.Id, out var previousEVSEGroup))
                    evseGroup.ShareRuntimeOf(previousEVSEGroup);

            foreach (var chargingStationGroup in ChargingStationGroups)
                if (Previous.TryGetChargingStationGroup(chargingStationGroup.Id, out var previousChargingStationGroup))
                    chargingStationGroup.ShareRuntimeOf(previousChargingStationGroup);

            foreach (var chargingPoolGroup in ChargingPoolGroups)
                if (Previous.TryGetChargingPoolGroup(chargingPoolGroup.Id, out var previousChargingPoolGroup))
                    chargingPoolGroup.ShareRuntimeOf(previousChargingPoolGroup);

            foreach (var chargingTariffOffer in ChargingTariffOffers)
                if (Previous.TryGetChargingTariffOffer(chargingTariffOffer.Id, out var previousChargingTariffOffer))
                    chargingTariffOffer.ShareRuntimeOf(previousChargingTariffOffer);

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
                await onAdminStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
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
                await onStatusChanged(Timestamp, EventTrackingId, this, OldStatus, NewStatus, DataSource);
        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this e-mobility provider, as its roaming network embeds it.
        /// </summary>
        public JObject ToJSON()

            => ToJSON(IncludeChildArrays: true);

        /// <summary>
        /// The JSON representation of this e-mobility provider, with or without its child arrays: without them for the skeleton of its
        /// tree (SPECIFICATION.md, section 8.2), which names their roots in their place.
        /// </summary>
        /// <param name="IncludeChildArrays">Whether to include its child arrays.</param>
        internal JObject ToJSON(Boolean IncludeChildArrays)
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

            if (IncludeChildArrays && EVSEGroups.Count > 0)
                json.Add("EVSEGroups",             new JArray(EVSEGroups.           Select(group => group.ToJSON())));

            if (IncludeChildArrays && ChargingStationGroups.Count > 0)
                json.Add("chargingStationGroups",  new JArray(ChargingStationGroups.Select(group => group.ToJSON())));

            if (IncludeChildArrays && ChargingPoolGroups.Count > 0)
                json.Add("chargingPoolGroups",     new JArray(ChargingPoolGroups.   Select(group => group.ToJSON())));

            if (IncludeChildArrays && ChargingTariffOffers.Count > 0)
                json.Add("chargingTariffOffers",   new JArray(ChargingTariffOffers. Select(offer => offer.ToJSON())));

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

                if (!POIJSON.TryParseChildren<EVSEGroup>           (JSON, "EVSEGroups",            "EVSEGroupIds",            "EVSEGroups",            EVSEGroup.           TryParse, out var evseGroups,            out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingStationGroup>(JSON, "chargingStationGroups", "chargingStationGroupIds", "ChargingStationGroups", ChargingStationGroup.TryParse, out var chargingStationGroups, out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingPoolGroup>   (JSON, "chargingPoolGroups",    "chargingPoolGroupIds",    "ChargingPoolGroups",    ChargingPoolGroup.   TryParse, out var chargingPoolGroups,    out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingTariffOffer> (JSON, "chargingTariffOffers",  "chargingTariffOfferIds",  "charging tariff offers", ChargingTariffOffer. TryParse, out var chargingTariffOffers,  out ErrorResponse))
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

                EMobilityProvider = new EMobilityProvider(id,
                                                          name,
                                                          description,
                                                          address,
                                                          homepage,
                                                          hotline,
                                                          SigningKeys:            signingKeys,
                                                          IdentityKeys:           identityKeys,
                                                          EVSEGroups:             evseGroups,
                                                          ChargingStationGroups:  chargingStationGroups,
                                                          ChargingPoolGroups:     chargingPoolGroups,
                                                          ChargingTariffOffers:   chargingTariffOffers,
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
