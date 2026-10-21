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

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/eMobilityProvider";

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
            public const String  Address                   = "address";

            /// <summary>
            /// The property of its homepage.
            /// </summary>
            public const String  Homepage                  = "homepage";

            /// <summary>
            /// The property of its hotline phone number.
            /// </summary>
            public const String  HotlinePhoneNumber        = "hotline";

            /// <summary>
            /// The property of its signing keys, as of every party.
            /// </summary>
            public const String  SigningKeys               = POIPartyKeys.SigningKeysProperty;

            /// <summary>
            /// The property of its identity keys, as of every party.
            /// </summary>
            public const String  IdentityKeys              = POIPartyKeys.IdentityKeysProperty;

            /// <summary>
            /// The property of its EVSE groups.
            /// </summary>
            public const String  EVSEGroups                = "EVSEGroups";

            /// <summary>
            /// The property of the identifications of its EVSE groups, refused: they are given completely.
            /// </summary>
            public const String  EVSEGroupIds              = "EVSEGroupIds";

            /// <summary>
            /// The property of its charging station groups.
            /// </summary>
            public const String  ChargingStationGroups     = "chargingStationGroups";

            /// <summary>
            /// The property of the identifications of its charging station groups, refused: they are given completely.
            /// </summary>
            public const String  ChargingStationGroupIds   = "chargingStationGroupIds";

            /// <summary>
            /// The property of its charging pool groups.
            /// </summary>
            public const String  ChargingPoolGroups        = "chargingPoolGroups";

            /// <summary>
            /// The property of the identifications of its charging pool groups, refused: they are given completely.
            /// </summary>
            public const String  ChargingPoolGroupIds      = "chargingPoolGroupIds";

            /// <summary>
            /// The property of its charging tariff offers.
            /// </summary>
            public const String  ChargingTariffOffers      = "chargingTariffOffers";

            /// <summary>
            /// The property of the identifications of its charging tariff offers, refused: they are given completely.
            /// </summary>
            public const String  ChargingTariffOfferIds    = "chargingTariffOfferIds";

        }

        #endregion


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


        #region (protected override) ContentJSON()

        /// <inheritdoc/>
        protected override JObject ContentJSON()

            => ToJSON(Embedded: true);

        #endregion

        #region ToJSON(Embedded = false, ...)

        /// <summary>
        /// Return the JSON representation of this e-mobility provider; embedded, as its roaming network embeds it.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomEMobilityProviderSerializer">A delegate to serialize custom e-mobility provider JSON objects.</param>
        public JObject ToJSON(Boolean                                              Embedded                            = false,
                              Boolean                                              IncludeETags                        = false,
                              ETagDigestEncoding?                                  ETagDigestEncoding                  = null,
                              CustomJObjectSerializerDelegate<EMobilityProvider>?  CustomEMobilityProviderSerializer   = null)

            => ToJSON(Embedded,
                      IncludeETags,
                      ETagDigestEncoding,
                      CustomEMobilityProviderSerializer,
                      IncludeChildArrays: true);

        /// <summary>
        /// The JSON representation of this e-mobility provider, with or without its child arrays: without them for the skeleton of its
        /// tree (SPECIFICATION.md, section 8.2), which names their roots in their place.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="ETagDigestEncoding">The encoding of the digests of its ETags, Base64 by default.</param>
        /// <param name="CustomEMobilityProviderSerializer">A delegate to serialize custom e-mobility provider JSON objects.</param>
        /// <param name="IncludeChildArrays">Whether to include its child arrays.</param>
        internal JObject ToJSON(Boolean                                              Embedded,
                                Boolean                                              IncludeETags,
                                ETagDigestEncoding?                                  ETagDigestEncoding,
                                CustomJObjectSerializerDelegate<EMobilityProvider>?  CustomEMobilityProviderSerializer,
                                Boolean                                              IncludeChildArrays)
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

            if (Address is not null)
                json.Add(PropertyKeys.Address,              Address.ToJSON(Embedded: true));

            if (Homepage.HasValue)
                json.Add(PropertyKeys.Homepage,             Homepage.Value.ToString());

            if (HotlinePhoneNumber.HasValue)
                json.Add(PropertyKeys.HotlinePhoneNumber,   HotlinePhoneNumber.Value.ToString());

            if (IncludeChildArrays && EVSEGroups.Count > 0)
                json.Add(PropertyKeys.EVSEGroups,             new JArray(EVSEGroups.           Select(group => group.ToJSON(Embedded: true))));

            if (IncludeChildArrays && ChargingStationGroups.Count > 0)
                json.Add(PropertyKeys.ChargingStationGroups,  new JArray(ChargingStationGroups.Select(group => group.ToJSON(Embedded: true))));

            if (IncludeChildArrays && ChargingPoolGroups.Count > 0)
                json.Add(PropertyKeys.ChargingPoolGroups,     new JArray(ChargingPoolGroups.   Select(group => group.ToJSON(Embedded: true))));

            if (IncludeChildArrays && ChargingTariffOffers.Count > 0)
                json.Add(PropertyKeys.ChargingTariffOffers,   new JArray(ChargingTariffOffers. Select(offer => offer.ToJSON(Embedded: true))));

            POIPartyKeys.AddTo(json, this);

            POIEntityMetadata.AddTo(json, Created, LastChangeDate, CustomData);

            if (ETagsProperty(IncludeETags, ETagDigestEncoding) is JProperty eTags)
                json.Add(eTags);

            return CustomEMobilityProviderSerializer is not null
                       ? CustomEMobilityProviderSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, ...)

        /// <summary>
        /// Return the CBOR representation of this e-mobility provider: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="IncludeETags">Whether to include its ETags, which describe its content as its parent embeds it (ContentETag(…)), irrespective of the view options.</param>
        /// <param name="CustomEMobilityProviderSerializer">A delegate to serialize custom e-mobility provider CBOR maps.</param>
        public CBORValue ToCBOR(Boolean                                            Embedded                            = false,
                                Boolean                                            IncludeETags                        = false,
                                CustomCBORSerializerDelegate<EMobilityProvider>?   CustomEMobilityProviderSerializer   = null)
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

            // The address, the charging pool groups, the charging tariff offers and the keys have no CBOR representation
            // of their own: theirs are the keys and values of their JSON representation.
            if (Address is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Address),              CBORJSON.ToCBOR(Address.ToJSON(Embedded: true)));

            if (Homepage.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.Homepage),             CBORValue.FromText(Homepage.Value.ToString()));

            if (HotlinePhoneNumber.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.HotlinePhoneNumber),   CBORValue.FromText(HotlinePhoneNumber.Value.ToString()));

            if (EVSEGroups.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.EVSEGroups),
                         CBORValue.FromArray(EVSEGroups.           Select(group => group.ToCBOR(Embedded: true))));

            if (ChargingStationGroups.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ChargingStationGroups),
                         CBORValue.FromArray(ChargingStationGroups.Select(group => group.ToCBOR(Embedded: true))));

            if (ChargingPoolGroups.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ChargingPoolGroups),
                         CBORValue.FromArray(ChargingPoolGroups.   Select(group => CBORJSON.ToCBOR(group.ToJSON(Embedded: true)))));

            if (ChargingTariffOffers.Count > 0)
                cbor.Add(CBORValue.FromText(PropertyKeys.ChargingTariffOffers),
                         CBORValue.FromArray(ChargingTariffOffers. Select(offer => CBORJSON.ToCBOR(offer.ToJSON(Embedded: true)))));

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

            return CustomEMobilityProviderSerializer is not null
                       ? CustomEMobilityProviderSerializer(this, cbor)
                       : cbor;

        }

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

                if (!JSON.ParseMandatory(BasePropertyKeys.Id,
                                         "e-mobility provider identification",
                                         EMobilityProvider_Id.TryParse,
                                         out EMobilityProvider_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (JSON[BasePropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the e-mobility provider '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                if (!POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Name,            "e-mobility provider name",         out var name,         out ErrorResponse) ||
                    !POIJSON.TryParseI18N   (JSON, BasePropertyKeys.Description,     "e-mobility provider description",  out var description,  out ErrorResponse) ||
                    !POIJSON.TryParseString (JSON, BasePropertyKeys.DataSource,      "data source",                      out var dataSource,   out ErrorResponse) ||
                    !POIJSON.TryParseAddress(JSON, PropertyKeys.Address,             "address",                          out var address,      out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, PropertyKeys.Homepage,            "homepage",                         URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (JSON, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",             PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
                {
                    return false;
                }

                if (!POIPartyKeys.TryParse(JSON, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<EVSEGroup>           (JSON, PropertyKeys.EVSEGroups,            PropertyKeys.EVSEGroupIds,            "EVSEGroups",            EVSEGroup.           TryParse, out var evseGroups,            out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingStationGroup>(JSON, PropertyKeys.ChargingStationGroups, PropertyKeys.ChargingStationGroupIds, "ChargingStationGroups", ChargingStationGroup.TryParse, out var chargingStationGroups, out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingPoolGroup>   (JSON, PropertyKeys.ChargingPoolGroups,    PropertyKeys.ChargingPoolGroupIds,    "ChargingPoolGroups",    ChargingPoolGroup.   TryParse, out var chargingPoolGroups,    out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingTariffOffer> (JSON, PropertyKeys.ChargingTariffOffers,  PropertyKeys.ChargingTariffOfferIds,  "charging tariff offers", ChargingTariffOffer. TryParse, out var chargingTariffOffers,  out ErrorResponse))
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

                if (!TryCheckETagsOf(JSON, EMobilityProvider, out ErrorResponse))
                {
                    EMobilityProvider = null;
                    return false;
                }

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

        #region (static) TryParse(CBOR, out EMobilityProvider, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of an e-mobility provider: the keys and values of its JSON
        /// representation, see ToCBOR().
        /// </summary>
        /// <param name="CBOR">The CBOR to parse.</param>
        /// <param name="EMobilityProvider">The parsed e-mobility provider.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(CBORValue                                    CBOR,
                                       [NotNullWhen(true)]  out EMobilityProvider?  EMobilityProvider,
                                       [NotNullWhen(false)] out String?             ErrorResponse)
        {

            EMobilityProvider = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = "The CBOR representation of an e-mobility provider must be a map!";
                    return false;
                }

                if (!CBOR.ParseMandatory(BasePropertyKeys.Id,
                                         "e-mobility provider identification",
                                         EMobilityProvider_Id.TryParse,
                                         out EMobilityProvider_Id id,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (CBOR.TryGetValue(CBORValue.FromText(BasePropertyKeys.Context), out var contextCBOR) &&
                    !(contextCBOR.TryGetText(out var context) && context == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the e-mobility provider '{id}' must be '{JSONLDContext}'!";
                    return false;
                }

                CBOR.ParseOptional    (BasePropertyKeys.Name,         "e-mobility provider name",         I18NString.TryParse, out I18NString? name,         out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptional    (BasePropertyKeys.Description,  "e-mobility provider description",  I18NString.TryParse, out I18NString? description,  out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                CBOR.ParseOptionalText(BasePropertyKeys.DataSource,   "data source",                                           out var         dataSource,   out ErrorResponse);
                if (ErrorResponse is not null)
                    return false;

                if (!TryParseCBORChildren<EVSEGroup>           (CBOR, PropertyKeys.EVSEGroups,            PropertyKeys.EVSEGroupIds,            "EVSEGroups",            EVSEGroup.           TryParse, out var evseGroups,            out ErrorResponse) ||
                    !TryParseCBORChildren<ChargingStationGroup>(CBOR, PropertyKeys.ChargingStationGroups, PropertyKeys.ChargingStationGroupIds, "ChargingStationGroups", ChargingStationGroup.TryParse, out var chargingStationGroups, out ErrorResponse))
                {
                    return false;
                }

                // What has no CBOR representation of its own is read as its JSON, with the parsers of the JSON
                // representation: the address, the homepage, the hotline, the keys, the charging pool groups,
                // the charging tariff offers and the metadata.
                var json = new JObject();

                foreach (var key in new[] { PropertyKeys.Address,
                                            PropertyKeys.Homepage,
                                            PropertyKeys.HotlinePhoneNumber,
                                            PropertyKeys.SigningKeys,
                                            PropertyKeys.IdentityKeys,
                                            PropertyKeys.ChargingPoolGroups,
                                            PropertyKeys.ChargingPoolGroupIds,
                                            PropertyKeys.ChargingTariffOffers,
                                            PropertyKeys.ChargingTariffOfferIds,
                                            BasePropertyKeys.Created,
                                            BasePropertyKeys.LastChange,
                                            BasePropertyKeys.CustomData })
                {
                    if (CBOR.TryGetValue(CBORValue.FromText(key), out var value))
                        json.Add(key, CBORJSON.ToJSON(value));
                }

                if (!POIJSON.TryParseAddress(json, PropertyKeys.Address,             "address",               out var address,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (json, PropertyKeys.Homepage,            "homepage",              URL.        TryParse, out URL?          homepage,  out ErrorResponse) ||
                    !POIJSON.TryParseText   (json, PropertyKeys.HotlinePhoneNumber,  "hotline phone number",  PhoneNumber.TryParse, out PhoneNumber?  hotline,   out ErrorResponse))
                {
                    return false;
                }

                if (!POIPartyKeys.TryParse(json, out var signingKeys, out var identityKeys, out ErrorResponse))
                    return false;

                if (!POIJSON.TryParseChildren<ChargingPoolGroup>   (json, PropertyKeys.ChargingPoolGroups,    PropertyKeys.ChargingPoolGroupIds,    "ChargingPoolGroups",    ChargingPoolGroup.   TryParse, out var chargingPoolGroups,    out ErrorResponse) ||
                    !POIJSON.TryParseChildren<ChargingTariffOffer> (json, PropertyKeys.ChargingTariffOffers,  PropertyKeys.ChargingTariffOfferIds,  "charging tariff offers", ChargingTariffOffer. TryParse, out var chargingTariffOffers,  out ErrorResponse))
                {
                    return false;
                }

                if (!POIEntityMetadata.TryParse(json,
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

                if (!TryCheckETagsOf(CBOR, EMobilityProvider, out ErrorResponse))
                {
                    EMobilityProvider = null;
                    return false;
                }

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                EMobilityProvider  = null;
                ErrorResponse      = "The given CBOR representation of an e-mobility provider is invalid: " + e.Message;
                return false;
            }

        }

        #endregion

        #region (private static) TryParseCBORChildren<T>(CBOR, PropertyName, IdsPropertyName, Description, Parser, out Children, out ErrorResponse)

        /// <summary>
        /// The optional embedded children of the given CBOR map, given completely - as POIJSON.TryParseChildren does in JSON.
        /// </summary>
        private static Boolean TryParseCBORChildren<T>(CBORValue                         CBOR,
                                                       String                            PropertyName,
                                                       String                            IdsPropertyName,
                                                       String                            Description,
                                                       TryCBORParser<T>                  Parser,
                                                       out ImmutableArray<T>             Children,
                                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Children = [];

            if (CBOR.TryGetValue(CBORValue.FromText(IdsPropertyName), out _))
            {
                ErrorResponse = $"The {Description} must be given completely in '{PropertyName}', not by '{IdsPropertyName}'!";
                return false;
            }

            if (!CBOR.TryGetValue(CBORValue.FromText(PropertyName), out var array))
            {
                ErrorResponse = null;
                return true;
            }

            if (array.Kind != CBORValueKind.Array)
            {
                ErrorResponse = $"The {Description} must be a CBOR array!";
                return false;
            }

            var children = ImmutableArray.CreateBuilder<T>(array.Count);

            foreach (var item in array.AsArray())
            {

                if (item.Kind != CBORValueKind.Map)
                {
                    ErrorResponse = $"The {Description} must be CBOR maps!";
                    return false;
                }

                if (!Parser(item, out var child, out var childError))
                {
                    ErrorResponse = childError;
                    return false;
                }

                children.Add(child);

            }

            Children       = children.MoveToImmutable();
            ErrorResponse  = null;
            return true;

        }

        /// <summary>
        /// A parser of the CBOR representation of a child.
        /// </summary>
        private delegate Boolean TryCBORParser<T>(CBORValue                         CBOR,
                                                  [NotNullWhen(true)]  out T?       Child,
                                                  [NotNullWhen(false)] out String?  ErrorResponse);

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
