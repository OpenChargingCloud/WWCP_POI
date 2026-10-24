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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The kind of key of a party.
    /// </summary>
    public enum POIKeyKind
    {

        /// <summary>
        /// A short key signing change sets and commits.
        /// </summary>
        Signing,

        /// <summary>
        /// A long-term identity key signing changes of keys.
        /// </summary>
        Identity

    }


    /// <summary>
    /// A party of the roaming network, by the type and the identification of its entity.
    /// </summary>
    /// <param name="EntityType">The entity type, e.g. ChargingStationOperator or RoamingNetworkOperator.</param>
    /// <param name="EntityId">The identification of the entity.</param>
    public readonly record struct POIPartyId(String  EntityType,
                                             String  EntityId)
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/partyId/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context      = "@context";

            /// <summary>
            /// The property of its entity type.
            /// </summary>
            public const String  EntityType   = "entityType";

            /// <summary>
            /// The property of its entity identification.
            /// </summary>
            public const String  EntityId     = "entityId";

        }

        #endregion

        #endregion


        #region ToJSON(Embedded = false, CustomPOIPartyIdSerializer = null)

        /// <summary>
        /// { "entityType", "entityId" }.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomPOIPartyIdSerializer">An optional custom serializer.</param>
        public JObject ToJSON(Boolean                                       Embedded                     = false,
                              CustomJObjectSerializerDelegate<POIPartyId>?  CustomPOIPartyIdSerializer   = null)
        {

            var json = new JObject();

            if (!Embedded)
                json.Add(PropertyKeys.Context,     JSONLDContext);

            json.Add(PropertyKeys.EntityType,      EntityType);
            json.Add(PropertyKeys.EntityId,        EntityId);

            return CustomPOIPartyIdSerializer is not null
                       ? CustomPOIPartyIdSerializer(this, json)
                       : json;

        }

        #endregion

        #region TryParse(JSON, out PartyId, out ErrorResponse)

        /// <summary>
        /// Try to parse { "entityType", "entityId" }.
        /// </summary>
        /// <param name="JSON">The JSON to parse.</param>
        /// <param name="PartyId">The parsed party identification.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParse(JToken                            JSON,
                                       out POIPartyId                    PartyId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            PartyId = default;

            if (JSON is JObject withContext &&
                withContext[PropertyKeys.Context] is JToken context &&
                !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
            {
                ErrorResponse = $"The JSON-LD context of the party must be '{JSONLDContext}'!";
                return false;
            }

            if (JSON is not JObject json                                                                         ||
                json[PropertyKeys.EntityType] is not JValue { Type: JTokenType.String } entityType              ||
                json[PropertyKeys.EntityId]   is not JValue { Type: JTokenType.String } entityId                ||
                json.Count != (json.ContainsKey(PropertyKeys.Context) ? 3 : 2)                                   ||
                String.IsNullOrWhiteSpace(entityType.Value<String>())                                            ||
                String.IsNullOrWhiteSpace(entityId.  Value<String>()))
            {
                ErrorResponse = $"A party must be {{ \"{PropertyKeys.EntityType}\", \"{PropertyKeys.EntityId}\" }}!";
                return false;
            }

            PartyId        = new POIPartyId(entityType.Value<String>()!, entityId.Value<String>()!);
            ErrorResponse  = null;
            return true;

        }

        #endregion

        /// <inheritdoc/>
        public override String ToString()

            => EntityType == nameof(RoamingNetworkOperator)
                   ? EntityId.Length > 0
                         ? $"the roaming network operator '{EntityId}'"
                         : "the roaming network operator, of which this roaming network has none"
                   : $"the {EntityType} '{EntityId}'";

    }


    /// <summary>
    /// Who owns the data of a roaming network, and therefore must sign its changes.
    /// </summary>
    /// <remarks>
    /// Every change of a change set has an owner, and a change set needs a valid signature of every owner,
    /// by a key the owner has in the version the change set is applied to, valid at its creation:
    /// a signing key for data and for the signing keys of a party, an identity key for its identity keys.
    /// <list type="bullet">
    ///   <item>The roaming network operator (the RoamingNetworkOperator it embeds) owns itself, the roaming network itself,
    ///         manufacturers, energy meter manufacturers and transparency software certificates, and adds and removes parties:
    ///         it decides who is a member, so removing a party removes all its data with it, without its signature.</item>
    ///   <item>A charging station operator, e-mobility provider, grid operator, parking operator or transparency software
    ///         manufacturer owns its own properties, a charging station operator its brands, pools, stations, EVSEs, tariffs
    ///         and groups, a parking operator its parking data, a transparency software manufacturer its releases.
    ///         Brands are embedded in what they are a brand of, and so owned by its owner.</item>
    ///   <item>A grid connection point is owned by its maintainer: its grid operator, or the charging station operator or
    ///         parking operator its maintainedBy names. One of them adds it, with the maintainer it names, or its grid operator.
    ///         Its maintainedBy is changed by its grid operator, taking it over or handing it on, by its maintainer,
    ///         handing it on, or by an identity key of the roaming network operator.</item>
    ///   <item>A grid operator may be added without keys: it does not take part. Then the roaming network operator signs
    ///         in its place, until it gets keys from an identity key of the roaming network operator.</item>
    ///   <item>The signing keys of a party are changed with one of its current signing keys, or with one of its identity
    ///         keys when none of them can sign any more; its identity keys only with one of its identity keys.
    ///         For recovery, an identity key of the roaming network operator changes both kinds of keys of every other party.
    ///         A party always keeps at least one signing and one identity key.</item>
    ///   <item>A party is added with its keys and without embedded children: its data is its own to add.</item>
    /// </list>
    /// </remarks>
    public sealed partial class RoamingNetwork
    {

        #region Data

        /// <summary>
        /// The entity types of parties, besides the roaming network operator.
        /// </summary>
        private static readonly String[] PartyTypes = [
            nameof(ChargingStationOperator),
            nameof(EMobilityProvider),
            nameof(GridOperator),
            nameof(ParkingOperator),
            nameof(TransparencySoftwareManufacturer)
        ];

        /// <summary>
        /// The embedded children a party must not be added with, by its entity type.
        /// </summary>
        internal static readonly Dictionary<String, String[]> PartyChildren = new () {
            { nameof(ChargingStationOperator), [ ChargingStationOperator.PropertyKeys.ChargingPools,
                                                 ChargingStationOperator.PropertyKeys.ChargingTariffs,
                                                 ChargingStationOperator.PropertyKeys.EVSETemplates,
                                                 ChargingStationOperator.PropertyKeys.ChargingStationTemplates,
                                                 ChargingStationOperator.PropertyKeys.ChargingPoolTemplates,
                                                 ChargingStationOperator.PropertyKeys.ChargingTariffTemplates,
                                                 ChargingStationOperator.PropertyKeys.EnergyMeterTemplates,
                                                 ChargingStationOperator.PropertyKeys.EVSEGroups,
                                                 ChargingStationOperator.PropertyKeys.ChargingStationGroups,
                                                 ChargingStationOperator.PropertyKeys.ChargingPoolGroups,
                                                 ChargingStationOperator.PropertyKeys.ChargingTariffGroups,
                                                 ChargingStationOperator.PropertyKeys.EnergyMeterGroups,
                                                 ChargingStationOperator.PropertyKeys.ChargingTariffOffers,
                                                 ChargingStationOperator.PropertyKeys.ParkingSpaces ] },
            { nameof(EMobilityProvider),       [ EMobilityProvider.PropertyKeys.EVSEGroups,
                                                 EMobilityProvider.PropertyKeys.ChargingStationGroups,
                                                 EMobilityProvider.PropertyKeys.ChargingPoolGroups,
                                                 EMobilityProvider.PropertyKeys.ChargingTariffOffers ] },
            { nameof(GridOperator),            [ GridOperator.PropertyKeys.GridConnectionPoints,
                                                 GridOperator.PropertyKeys.EnergyMeterTemplates,
                                                 GridOperator.PropertyKeys.EnergyMeterGroups ] },
            { nameof(ParkingOperator),         [ ParkingOperator.PropertyKeys.ParkingGarages,
                                                 ParkingOperator.PropertyKeys.ParkingSpaces,
                                                 ParkingOperator.PropertyKeys.ParkingSensors,
                                                 ParkingOperator.PropertyKeys.ParkingSpaceTemplates,
                                                 ParkingOperator.PropertyKeys.ParkingSpaceGroups,
                                                 ParkingOperator.PropertyKeys.ParkingProducts ] },
            { nameof(TransparencySoftwareManufacturer),
                                               [ TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware ] }
        };

        #endregion


        #region TryGetParty(PartyId, out Party)

        /// <summary>
        /// Try to get the party with the given identification in this version.
        /// </summary>
        public Boolean TryGetParty(POIPartyId                          PartyId,
                                   [NotNullWhen(true)] out IPOIParty?  Party)
        {

            Party = PartyId.EntityType switch {

                nameof(RoamingNetworkOperator)   => Operator is not null &&
                                                    RoamingNetworkOperator_Id.TryParse(PartyId.EntityId, out var roamingNetworkOperatorId) &&
                                                    roamingNetworkOperatorId == Operator.Id
                                                        ? Operator : null,

                nameof(ChargingStationOperator)  => ChargingStationOperator_Id.TryParse(PartyId.EntityId, out var chargingStationOperatorId) &&
                                                    chargingStationOperators.TryGet(chargingStationOperatorId, out var chargingStationOperator)
                                                        ? chargingStationOperator : null,

                nameof(EMobilityProvider)        => EMobilityProvider_Id.TryParse(PartyId.EntityId, out var eMobilityProviderId) &&
                                                    eMobilityProviders.TryGet(eMobilityProviderId, out var eMobilityProvider)
                                                        ? eMobilityProvider : null,

                nameof(GridOperator)             => GridOperator_Id.TryParse(PartyId.EntityId, out var gridOperatorId) &&
                                                    gridOperators.TryGet(gridOperatorId, out var gridOperator)
                                                        ? gridOperator : null,

                nameof(ParkingOperator)          => ParkingOperator_Id.TryParse(PartyId.EntityId, out var parkingOperatorId) &&
                                                    parkingOperators.TryGet(parkingOperatorId, out var parkingOperator)
                                                        ? parkingOperator : null,

                nameof(TransparencySoftwareManufacturer)
                                                 => TransparencySoftwareManufacturer_Id.TryParse(PartyId.EntityId, out var softwareManufacturerId) &&
                                                    transparencySoftwareManufacturers.TryGet(softwareManufacturerId, out var softwareManufacturer)
                                                        ? softwareManufacturer : null,

                _                                => null

            };

            return Party is not null;

        }

        #endregion

        #region (private) IsThisRoamingNetwork(RoamingNetworkId)

        /// <summary>
        /// Whether the given text names this roaming network, as its identification type compares them.
        /// </summary>
        private Boolean IsThisRoamingNetwork(String? RoamingNetworkId)

            => RoamingNetworkId is not null &&
               RoamingNetwork_Id.TryParse(RoamingNetworkId, out var roamingNetworkId) &&
               roamingNetworkId == Id;

        #endregion

        #region OperatorParty

        /// <summary>
        /// The roaming network operator as a party. For a roaming network without one,
        /// a party nobody can sign for: everything only it may change can not be changed.
        /// </summary>
        public POIPartyId OperatorParty
            => new (nameof(RoamingNetworkOperator), Operator?.Id.ToString() ?? "");

        #endregion

        #region Parties

        /// <summary>
        /// All parties of this version: the roaming network operator and every operator and provider.
        /// </summary>
        public IEnumerable<(POIPartyId Id, IPOIParty Party)> Parties
        {
            get
            {

                if (Operator is not null)
                    yield return (OperatorParty, Operator);

                foreach (var chargingStationOperator in chargingStationOperators)
                    yield return (new POIPartyId(nameof(ChargingStationOperator), chargingStationOperator.Id.ToString()), chargingStationOperator);

                foreach (var eMobilityProvider in eMobilityProviders)
                    yield return (new POIPartyId(nameof(EMobilityProvider),       eMobilityProvider.      Id.ToString()), eMobilityProvider);

                foreach (var gridOperator in gridOperators)
                    yield return (new POIPartyId(nameof(GridOperator),            gridOperator.           Id.ToString()), gridOperator);

                foreach (var parkingOperator in parkingOperators)
                    yield return (new POIPartyId(nameof(ParkingOperator),         parkingOperator.        Id.ToString()), parkingOperator);

                foreach (var softwareManufacturer in transparencySoftwareManufacturers)
                    yield return (new POIPartyId(nameof(TransparencySoftwareManufacturer), softwareManufacturer.Id.ToString()), softwareManufacturer);

            }
        }

        #endregion


        #region TryCheckOwnership(ChangeSet, out ErrorResponse)

        /// <summary>
        /// Whether the given change set is signed by the owner of every change it makes,
        /// with keys of this version, the one it is applied to.
        /// Every signature must be one of a known key, valid at the creation of the change set.
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryCheckOwnership(RoamingNetworkChangeSet           ChangeSet,
                                         [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (ChangeSet.Signatures.IsEmpty)
            {
                ErrorResponse = "The change set is unsigned: every change set must be signed by the owners of what it changes!";
                return false;
            }

            if (!TryGetSigners(ChangeSet, out var signedBy, out var possessed, out ErrorResponse) ||
                !TryCheckOwners(ChangeSet, signedBy, possessed, true, out var missing, out _, out ErrorResponse))
            {
                return false;
            }

            if (missing.Count > 0)
            {
                ErrorResponse = missing[0];
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryCheckCoSigning(ChangeSet, out Missing, out SignedByAnOwner, out ErrorResponse)

        /// <summary>
        /// Whether the signatures the given change set has so far are all valid, by keys of this version, the one it is
        /// applied to, valid at its creation; and which signatures are still missing (SPECIFICATION.md, section 4.2): one
        /// text per change whose owners have not signed it by as many holders as their quorums need.
        /// </summary>
        /// <param name="ChangeSet">The change set.</param>
        /// <param name="Missing">The signatures still missing, by change; none once its owners signed it.</param>
        /// <param name="SignedByAnOwner">Whether an owner of one of its changes signed it already.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryCheckCoSigning(RoamingNetworkChangeSet           ChangeSet,
                                         out IReadOnlyList<String>         Missing,
                                         out Boolean                       SignedByAnOwner,
                                         [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Missing          = [];
            SignedByAnOwner  = false;

            return TryGetSigners(ChangeSet, out var signedBy, out var possessed, out ErrorResponse) &&
                   TryCheckOwners(ChangeSet, signedBy, possessed, false, out Missing, out SignedByAnOwner, out ErrorResponse);

        }

        #endregion

        #region TryCheckWithdrawal(Withdrawal, ChangeSet, out ErrorResponse)

        /// <summary>
        /// Whether the given withdrawal of the given change set waiting for signatures is signed by a key of an owner of
        /// one of its changes, of the kind that owner signs it with, a key of this version valid when it was withdrawn
        /// (SPECIFICATION.md, section 4.2).
        /// </summary>
        /// <param name="Withdrawal">The withdrawal.</param>
        /// <param name="ChangeSet">The change set it withdraws.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryCheckWithdrawal(RoamingNetworkChangeSetWithdrawal  Withdrawal,
                                          RoamingNetworkChangeSet            ChangeSet,
                                          [NotNullWhen(false)] out String?   ErrorResponse)
        {

            if (Withdrawal.RoamingNetworkId != ChangeSet.RoamingNetworkId || Withdrawal.ChangeSetId != ChangeSet.Id)
            {
                ErrorResponse = $"The withdrawal is one of the change set '{Withdrawal.ChangeSetId}' of the roaming network '{Withdrawal.RoamingNetworkId}', not of '{ChangeSet.Id}' of '{ChangeSet.RoamingNetworkId}'!";
                return false;
            }

            var addedParking               = new Dictionary<(String, Object), POIPartyId>();
            var addedGridConnectionPoints  = new Dictionary<GridConnectionPoint_Id, (POIPartyId GridOperator, POIPartyId Maintainer)>();
            var addedEnergyMeterChildren   = new Dictionary<(String, Object), POIPartyId>();
            var addedProviderGroups        = new Dictionary<(String, Object), POIPartyId>();

            for (var index = 0; index < ChangeSet.Changes.Length; index++)
            {

                if (!TryGetOwners(ChangeSet.Changes[index], addedParking, addedGridConnectionPoints, addedEnergyMeterChildren, addedProviderGroups, out var owners, out ErrorResponse))
                {
                    ErrorResponse = $"Change {index}: {ErrorResponse}";
                    return false;
                }

                foreach (var (partyId, kind) in owners)
                {

                    if (!TryGetParty(partyId, out var party))
                        continue;

                    if ((kind == POIKeyKind.Identity ? party.IdentityKeys : party.SigningKeys).FirstOrDefault(key => key.Id == Withdrawal.KeyId) is not POIPublicKey key ||
                        (kind == POIKeyKind.Signing && !key.MaySign(POIKeyUsages.Of(ChangeSet.Changes[index]))))
                        continue;

                    if (!key.IsValidAt(Withdrawal.WithdrawnAt))
                    {
                        ErrorResponse = $"The key '{key.Id}' of {partyId} is not valid at {Withdrawal.WithdrawnAt.ToISO8601()}!";
                        return false;
                    }

                    if (!Withdrawal.Verify(key.Key.ToPublicKey(), out var verificationError))
                    {
                        ErrorResponse = $"The withdrawal by the key '{key.Id}' of {partyId} is invalid: {verificationError}";
                        return false;
                    }

                    ErrorResponse = null;
                    return true;

                }

            }

            ErrorResponse = $"The key '{Withdrawal.KeyId}' is no key of an owner of a change of the change set '{ChangeSet.Id}'!";
            return false;

        }

        #endregion

        #region (private) TryGetSigners(ChangeSet, out SignedBy, out Possessed, out ErrorResponse)

        /// <summary>
        /// The parties and kinds of keys that signed the given change set, with the keys, their holders and usages, and the
        /// new identity keys that signed it, as the proof that their parties hold them; every signature must be valid, by a
        /// key of a party of this version valid at the creation of the change set, or by a new identity key of one of its
        /// changes (NewIdentityKeysOf).
        /// </summary>
        private Boolean TryGetSigners(RoamingNetworkChangeSet                                       ChangeSet,
                                      out Dictionary<(POIPartyId, POIKeyKind), List<POIPublicKey>>  SignedBy,
                                      out HashSet<String>                                           Possessed,
                                      [NotNullWhen(false)] out String?                              ErrorResponse)
        {

            #region The parties and kinds of keys that signed it, with the keys

            // A quorum counts holders, not keys: a holder renewing its key signs with both once.
            var signedBy = SignedBy = new Dictionary<(POIPartyId, POIKeyKind), List<POIPublicKey>>();

            // The new identity keys that signed it: a key proves its possession only, so it is not yet one of its party,
            // and also one valid only later does.
            var possessed = Possessed = new HashSet<String>(StringComparer.Ordinal);
            var newKeys   = ChangeSet.Changes.SelectMany(NewIdentityKeysOf).ToArray();

            for (var index = 0; index < ChangeSet.Signatures.Length; index++)
            {

                var signature  = ChangeSet.Signatures[index];
                var keyFound   = false;

                foreach (var (partyId, party) in Parties)
                {
                    foreach (var (kind, keys) in new[] { (POIKeyKind.Signing, party.SigningKeys), (POIKeyKind.Identity, party.IdentityKeys) })
                    {

                        if (keys.FirstOrDefault(key => key.Id == signature.KeyId) is not POIPublicKey key)
                            continue;

                        keyFound = true;

                        if (!key.IsValidAt(ChangeSet.CreatedAt))
                        {
                            ErrorResponse = $"Signature {index}: the key '{key.Id}' of {partyId} is not valid at {ChangeSet.CreatedAt.ToISO8601()}!";
                            return false;
                        }

                        if (!ChangeSet.VerifySignature(signature, key.Key, signature.KeyId, out var verificationError))
                        {
                            ErrorResponse = $"Signature {index} by the key '{key.Id}' of {partyId} is invalid: {verificationError}";
                            return false;
                        }

                        if (!signedBy.TryGetValue((partyId, kind), out var keysSigned))
                            signedBy[(partyId, kind)] = keysSigned = [];

                        keysSigned.Add(key);

                    }
                }

                foreach (var (partyId, key) in newKeys.Where(newKey => newKey.Key.Id == signature.KeyId))
                {

                    keyFound = true;

                    if (!ChangeSet.VerifySignature(signature, key.Key, signature.KeyId, out var verificationError))
                    {
                        ErrorResponse = $"Signature {index} by the new identity key '{key.Id}' of {partyId} is invalid: {verificationError}";
                        return false;
                    }

                    possessed.Add(key.Id);

                }

                if (!keyFound)
                {
                    ErrorResponse = $"Signature {index}: the key '{signature.KeyId}' is no key of a party of the roaming network '{Id}'!";
                    return false;
                }

            }

            #endregion

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private) TryCheckOwners(ChangeSet, SignedBy, Possessed, StopAtMissing, out Missing, out SignedByAnOwner, out ErrorResponse)

        /// <summary>
        /// Whether every change of the given change set is one its owners may make, and which of them miss signatures of
        /// their owners, or of the new identity keys they enter: only the first, when told to stop there.
        /// </summary>
        private Boolean TryCheckOwners(RoamingNetworkChangeSet                                   ChangeSet,
                                       Dictionary<(POIPartyId, POIKeyKind), List<POIPublicKey>>  SignedBy,
                                       IReadOnlySet<String>                                      Possessed,
                                       Boolean                                                StopAtMissing,
                                       out IReadOnlyList<String>                              Missing,
                                       out Boolean                                            SignedByAnOwner,
                                       [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            var signedBy  = SignedBy;
            var missing   = new List<String>();

            Missing          = missing;
            SignedByAnOwner  = false;

            #region Every change needs one of its owners

            // Parking entities added by an earlier change of this change set, by their parking operator;
            // keyed by their typed identifications, so that a later change may name them in another spelling.
            var addedParking = new Dictionary<(String, Object), POIPartyId>();

            // Grid connection points added by an earlier change of this change set: their grid operator and maintainer.
            var addedGridConnectionPoints = new Dictionary<GridConnectionPoint_Id, (POIPartyId GridOperator, POIPartyId Maintainer)>();

            // Energy meter templates and groups added by an earlier change of this change set, by their owners;
            // keyed by their typed identifications, so that a later change may name them in another spelling.
            var addedEnergyMeterChildren = new Dictionary<(String, Object), POIPartyId>();

            // Groups of e-mobility providers and charging tariff offers added by an earlier change of this change set, by
            // their owners.
            var addedProviderGroups      = new Dictionary<(String, Object), POIPartyId>();

            for (var index = 0; index < ChangeSet.Changes.Length; index++)
            {

                var change = ChangeSet.Changes[index];

                if (!TryGetOwners(change, addedParking, addedGridConnectionPoints, addedEnergyMeterChildren, addedProviderGroups, out var owners, out ErrorResponse))
                {
                    ErrorResponse = $"Change {index}: {ErrorResponse}";
                    return false;
                }

                // One of its owners, by as many holders of its keys of the kinds named as its quorum needs: signing keys
                // only that may sign what the change changes (POIKeyUsages), identity keys everything they own.
                var usage          = POIKeyUsages.Of(change);

                var holdersByParty = owners.GroupBy(owner => owner.Item1).
                                            Select (party => (Party:    party.Key,
                                                              Holders:  party.SelectMany(owner => signedBy.TryGetValue(owner, out var keys)
                                                                                                      ? keys.Where (key => owner.Item2 == POIKeyKind.Identity || key.MaySign(usage)).
                                                                                                             Select(key => key.HolderOrUnnamed)
                                                                                                      : []).
                                                                              Distinct(StringComparer.Ordinal).
                                                                              Count())).
                                            ToArray();

                if (holdersByParty.Any(party => party.Holders > 0))
                    SignedByAnOwner = true;

                // A holder of the roaming network operator changing only its own signing keys and their certificates.
                var byOwnHolder    = OwnSigningKeysHolderOf(change) is String holder &&
                                     signedBy.TryGetValue((OperatorParty, POIKeyKind.Identity), out var identityKeys) &&
                                     identityKeys.Any(key => key.HolderOrUnnamed == holder);

                if (byOwnHolder)
                    SignedByAnOwner = true;

                if (!byOwnHolder && !holdersByParty.Any(party => party.Holders >= MinConsensusOf(party.Party)))
                {

                    var quorums = owners.Any(owner => MinConsensusOf(owner.Item1) > 1);

                    // A signing key naming usages that do not cover the change is named, so that its signer learns why.
                    var usageNamed = owners.Any(owner => owner.Item2 == POIKeyKind.Signing &&
                                                         signedBy.TryGetValue(owner, out var keys) &&
                                                         keys.Any(key => !key.MaySign(usage)));

                    missing.Add($"Change {index}: {change.Kind} of the {change.EntityType} '{change.EntityId}'{(change.EntityPropertyName is not null ? $" ('{change.EntityPropertyName}')" : "")} needs {(quorums ? "signatures" : "a signature")} by " +
                                String.Join(" or ", owners.Select(owner => (MinConsensusOf(owner.Item1) > 1
                                                                                ? $"{(owner.Item2 == POIKeyKind.Identity ? "identity" : "signing")} keys of {MinConsensusOf(owner.Item1)} holders of {owner.Item1}"
                                                                                : $"a{(owner.Item2 == POIKeyKind.Identity ? "n identity" : " signing")} key of {owner.Item1}") +
                                                                           (owner.Item2 == POIKeyKind.Signing && usageNamed ? $" that may sign '{usage}'" : ""))) + "!");

                    if (StopAtMissing)
                    {
                        ErrorResponse = null;
                        return true;
                    }

                }

                if (!TryCheckPartyChange(change, out ErrorResponse))
                {
                    ErrorResponse = $"Change {index}: {ErrorResponse}";
                    return false;
                }

                // Every identity key it enters signs it as well: the proof that its party holds it.
                foreach (var (partyId, key) in NewIdentityKeysOf(change).Where(newKey => !Possessed.Contains(newKey.Key.Id)))
                {

                    missing.Add($"Change {index}: {change.Kind} of the {change.EntityType} '{change.EntityId}'{(change.EntityPropertyName is not null ? $" ('{change.EntityPropertyName}')" : "")} needs a signature by the new identity key '{key.Id}' of {partyId}, as the proof that it holds it!");

                    if (StopAtMissing)
                    {
                        ErrorResponse = null;
                        return true;
                    }

                }

            }

            #endregion

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private) NewIdentityKeysOf(Change)

        /// <summary>
        /// The identity keys the given change enters anew (SPECIFICATION.md, section 4.1): those of a party it adds, or
        /// those of the new identity keys of a party, also of the roaming network operator, its party has not in this
        /// version; and so of a charging station or energy meter manufacturer, which names keys as a party does, though no
        /// party. Each
        /// must sign the change set, as the proof that its party holds it: no party is added, and none gets an identity
        /// key, by a key it does not hold.
        /// </summary>
        private IEnumerable<(POIPartyId Party, POIPublicKey Key)> NewIdentityKeysOf(RoamingNetworkChange Change)
        {

            var isManufacturer = Change.EntityType is nameof(ChargingStationManufacturer) or nameof(EnergyMeterManufacturer);

            if (!PartyTypes.Contains(Change.EntityType) && Change.EntityType != nameof(RoamingNetworkOperator) && !isManufacturer)
                return [];

            var partyId = PartyIdOf(new POIPartyId(Change.EntityType, Change.EntityId));

            // Keys it can not read are refused when it is applied.
            if (Change.Kind == RoamingNetworkChangeKind.Add && (PartyTypes.Contains(Change.EntityType) || isManufacturer))
                return Change.NewValue is JObject party &&
                       POIPartyKeys.TryParse(party, out _, out var identityKeys, out _)
                           ? identityKeys.Select(identityKey => (partyId, identityKey))
                           : [];

            if (Change.Kind == RoamingNetworkChangeKind.UpdateProperty &&
                Change.EntityPropertyName == POIPartyKeys.IdentityKeysProperty &&
                Change.NewValue is JArray newIdentityKeys &&
                POIPartyKeys.TryParse(new JObject(new JProperty(POIPartyKeys.IdentityKeysProperty, newIdentityKeys)), out _, out var keys, out _))
            {

                var currentKeys = Change.EntityType == nameof(ChargingStationManufacturer)
                                      ? ChargingStationManufacturer_Id.TryParse(Change.EntityId, out var manufacturerId) &&
                                        TryGetChargingStationManufacturerById(manufacturerId, out var manufacturer)
                                            ? manufacturer.IdentityKeys
                                            : []
                                  : Change.EntityType == nameof(EnergyMeterManufacturer)
                                      ? EnergyMeterManufacturer_Id.TryParse(Change.EntityId, out var meterManufacturerId) &&
                                        TryGetEnergyMeterManufacturerById(meterManufacturerId, out var meterManufacturer)
                                            ? meterManufacturer.IdentityKeys
                                            : []
                                      : TryGetParty(partyId, out var knownParty)
                                            ? knownParty.IdentityKeys
                                            : [];

                var current     = currentKeys.Select(identityKey => identityKey.Id).ToHashSet(StringComparer.Ordinal);

                return keys.Where (identityKey => !current.Contains(identityKey.Id)).
                            Select(identityKey => (partyId, identityKey));

            }

            return [];

        }

        #endregion

        #region (private) OwnSigningKeysHolderOf(Change)

        /// <summary>
        /// The holder of the roaming network operator whose own signing keys, their certificates, the given change alone
        /// changes, or null (SPECIFICATION.md, section 4.1): what it adds and removes, compared with this version, are
        /// certificates of that holder only, so that it renews its signing keys with one of its identity keys, without the
        /// others. Its signing keys give it one voice only, so no holder decides more alone than with them.
        /// </summary>
        private String? OwnSigningKeysHolderOf(RoamingNetworkChange Change)
        {

            if (Operator is null                                                                                                                      ||
                Change.EntityType != nameof(RoamingNetworkOperator)                                                                                    ||
                Change.EntityPropertyName != RoamingNetworkOperator.PropertyKeys.SigningKeyCertificates                                             ||
                Change.Kind is not (RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty)                            ||
                !RoamingNetworkOperator_Id.TryParse(Change.EntityId, out var operatorId) || operatorId != Operator.Id)
            {
                return null;
            }

            var current         = Operator.SigningKeyCertificates.Select(certificate => certificate.ToJSON(Embedded: true)).ToList();

            var changed         = Change.Kind == RoamingNetworkChangeKind.RemoveProperty
                                      ? []
                                      : Change.NewValue is JArray newEntries && newEntries.All(entry => entry is JObject)
                                            ? newEntries.Cast<JObject>().ToList()
                                            : null;

            if (changed is null)
                return null;

            // What it adds and what it removes, as multisets compared by their canonical JSON.
            var added           = new List<JObject>(changed);
            var removed         = new List<JObject>();

            foreach (var entry in current)
            {
                var same = added.FindIndex(addedEntry => CanonicalJSON.Serialize(addedEntry) == CanonicalJSON.Serialize(entry));
                if (same >= 0)
                    added.RemoveAt(same);
                else
                    removed.Add(entry);
            }

            String? holder = null;

            foreach (var entry in added.Concat(removed))
            {

                var entryHolder = RoamingNetworkSigningKeyCertificate.TryParse(entry, out var certificate, out _)
                                      ? certificate.Holder
                                      : null;

                if (entryHolder is null || (holder is not null && holder != entryHolder))
                    return null;

                holder = entryHolder;

            }

            return holder;

        }

        #endregion

        #region (private) MinConsensusOf(PartyId)

        /// <summary>
        /// How many holders of the keys of the given party of this version sign what it owns: one, unless a quorum.
        /// </summary>
        private Byte MinConsensusOf(POIPartyId PartyId)

            => TryGetParty(PartyId, out var party)
                   ? party.MinConsensus
                   : (Byte) 1;

        #endregion

        #region (private) PartyIdOf(PartyId)

        /// <summary>
        /// The given party as the keys of the parties name it: a party may be named in every spelling
        /// its identification type considers equal.
        /// </summary>
        private POIPartyId PartyIdOf(POIPartyId PartyId)

            => TryGetParty(PartyId, out var party)
                   ? Parties.First(knownParty => ReferenceEquals(knownParty.Party, party)).Id
                   : PartyId;

        #endregion

        #region (private) SignerOf(PartyId)

        /// <summary>
        /// Who signs the data of the given party: the party with a signing key, or the roaming network operator
        /// in the place of a party of this version without signing keys, e.g. a grid operator not taking part.
        /// </summary>
        private (POIPartyId, POIKeyKind) SignerOf(POIPartyId PartyId)

            => TryGetParty(PartyId, out var party) && party.SigningKeys.IsDefaultOrEmpty && PartyId.EntityType != nameof(RoamingNetworkOperator)
                   ? (OperatorParty, POIKeyKind.Signing)
                   : (PartyIdOf(PartyId), POIKeyKind.Signing);

        #endregion

        #region (private) TryGetOwners(Change, AddedParking, AddedGridConnectionPoints, AddedEnergyMeterChildren, AddedProviderGroups, out Owners, out ErrorResponse)

        /// <summary>
        /// The parties and kinds of keys of which one must sign the given change.
        /// </summary>
        private Boolean TryGetOwners(RoamingNetworkChange                                                                    Change,
                                     Dictionary<(String, Object), POIPartyId>                                                AddedParking,
                                     Dictionary<GridConnectionPoint_Id, (POIPartyId GridOperator, POIPartyId Maintainer)>  AddedGridConnectionPoints,
                                     Dictionary<(String, Object), POIPartyId>                                                AddedEnergyMeterChildren,
                                     Dictionary<(String, Object), POIPartyId>                                                AddedProviderGroups,
                                     [NotNullWhen(true)]  out (POIPartyId, POIKeyKind)[]?                                    Owners,
                                     [NotNullWhen(false)] out String?                                                        ErrorResponse)
        {

            Owners         = null;
            ErrorResponse  = null;

            var roamingNetworkOperator  = OperatorParty;
            var isProperty              = Change.Kind is not (RoamingNetworkChangeKind.Add or RoamingNetworkChangeKind.Remove);

            #region The roaming network itself

            if (Change.EntityType == nameof(RoamingNetwork))
            {
                Owners = [ (roamingNetworkOperator, POIKeyKind.Signing) ];
                return true;
            }

            #endregion

            #region The roaming network operator: its keys as those of every party

            if (Change.EntityType == nameof(RoamingNetworkOperator))
            {
                // Its quorum as its identity keys: who must sign is decided by those signing its keys. Its signing keys,
                // its certificates, also by one holder for its own (OwnSigningKeysHolderOf).
                Owners = Change.EntityPropertyName == RoamingNetworkOperator.PropertyKeys.SigningKeyCertificates
                             ? [ (roamingNetworkOperator, POIKeyKind.Signing), (roamingNetworkOperator, POIKeyKind.Identity) ]
                             : [ (roamingNetworkOperator, POIPartyKeys.IsKeyProperty(Change.EntityPropertyName) ||
                                                         Change.EntityPropertyName == POIPartyKeys.MinConsensusProperty
                                                             ? POIKeyKind.Identity
                                                             : POIKeyKind.Signing) ];
                return true;
            }

            #endregion

            #region Parties: added and removed by the roaming network operator, changed by themselves

            if (PartyTypes.Contains(Change.EntityType))
            {

                // The party as its keys name it: a change may name it in every spelling its identification type considers equal.
                var party = TryGetParty(new POIPartyId(Change.EntityType, Change.EntityId), out var namedParty)
                                ? Parties.First(knownParty => ReferenceEquals(knownParty.Party, namedParty)).Id
                                : new POIPartyId(Change.EntityType, Change.EntityId);

                // Signing keys are rotated by the signing keys they replace, or by an identity key when none of them
                // can sign any more; identity keys only by an identity key. The roaming network operator recovers both.
                Owners = !isProperty
                             ? [ (roamingNetworkOperator, POIKeyKind.Signing) ]
                             : Change.EntityPropertyName == POIPartyKeys.SigningKeysProperty
                                   ? [ (party, POIKeyKind.Signing), (party, POIKeyKind.Identity), (roamingNetworkOperator, POIKeyKind.Identity) ]
                                   : POIPartyKeys.IsKeyProperty(Change.EntityPropertyName)
                                         ? [ (party, POIKeyKind.Identity), (roamingNetworkOperator, POIKeyKind.Identity) ]
                                         : [ SignerOf(party) ];

                return true;

            }

            #endregion

            #region Registries of the roaming network operator

            if (Change.EntityType is nameof(ChargingStationManufacturer)
                                  or nameof(EnergyMeterManufacturer)
                                  or nameof(TransparencySoftwareCertificate))
            {
                Owners = [ (roamingNetworkOperator, POIKeyKind.Signing) ];
                return true;
            }

            #endregion

            #region The transparency software releases of a transparency software manufacturer: added to it, or found in it

            if (Change.EntityType == nameof(TransparencySoftware))
            {

                if (!TransparencySoftware_Id.TryParse(Change.EntityId, out var releaseId))
                {
                    ErrorResponse = $"invalid {Change.EntityType} identification '{Change.EntityId}'!";
                    return false;
                }

                POIPartyId? softwareManufacturer = null;

                if (Change.Kind == RoamingNetworkChangeKind.Add)
                {

                    if (Change.ParentEntityType != nameof(TransparencySoftwareManufacturer) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the {Change.EntityType} '{Change.EntityId}' needs its TransparencySoftwareManufacturer as its parent!";
                        return false;
                    }

                    softwareManufacturer = PartyIdOf(new POIPartyId(Change.ParentEntityType, Change.ParentEntityId));

                }

                else if (TransparencySoftwareManufacturerOf(releaseId) is TransparencySoftwareManufacturer owner)
                    softwareManufacturer = PartyIdOf(new POIPartyId(nameof(TransparencySoftwareManufacturer), owner.Id.ToString()));

                if (softwareManufacturer is null)
                {
                    ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' is unknown!";
                    return false;
                }

                Owners = [ SignerOf(softwareManufacturer.Value) ];
                return true;

            }

            #endregion

            #region The charging tariff offers of a charging station operator or an e-mobility provider

            if (Change.EntityType == nameof(ChargingTariffOffer))
            {

                if (!ChargingTariffOffer_Id.TryParse(Change.EntityId, out var offerId))
                {
                    ErrorResponse = $"invalid {Change.EntityType} identification '{Change.EntityId}'!";
                    return false;
                }

                POIPartyId? owner = null;

                if (Change.Kind == RoamingNetworkChangeKind.Add)
                {

                    if (Change.ParentEntityType is not (nameof(ChargingStationOperator) or nameof(EMobilityProvider)) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the {Change.EntityType} '{Change.EntityId}' needs its ChargingStationOperator or EMobilityProvider as its parent!";
                        return false;
                    }

                    // The owner as the keys of the parties name it.
                    owner = PartyIdOf(new POIPartyId(Change.ParentEntityType, Change.ParentEntityId));

                    AddedProviderGroups[(Change.EntityType, offerId)] = owner.Value;

                }

                else if (AddedProviderGroups.TryGetValue((Change.EntityType, offerId), out var addedOwner))
                    owner = addedOwner;

                else
                {

                    var (chargingStationOperator, eMobilityProvider, _) = ChargingTariffOfferOwners(Change.EntityId, chargingStationOperators, eMobilityProviders);

                    if      (chargingStationOperator is not null)
                        owner = new POIPartyId(nameof(ChargingStationOperator), chargingStationOperator.Id.ToString());

                    else if (eMobilityProvider is not null)
                        owner = new POIPartyId(nameof(EMobilityProvider),       eMobilityProvider.      Id.ToString());

                }

                if (!owner.HasValue)
                {
                    ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' belongs to no charging station operator and no e-mobility provider!";
                    return false;
                }

                Owners = [ SignerOf(owner.Value) ];
                return true;

            }

            #endregion

            #region The groups of an e-mobility provider: added to it, or found in it

            if (ProviderGroupKind(Change.EntityType) is not null &&
                TypedId(Change.EntityType, Change.EntityId) is Object providerGroupId)
            {

                POIPartyId? provider = null;

                if (Change.Kind == RoamingNetworkChangeKind.Add)
                {
                    if (Change.ParentEntityType == nameof(EMobilityProvider) && Change.ParentEntityId is not null)
                    {
                        // The provider as the keys of the parties name it.
                        provider = PartyIdOf(new POIPartyId(nameof(EMobilityProvider), Change.ParentEntityId));
                        AddedProviderGroups[(Change.EntityType, providerGroupId)] = provider.Value;
                    }
                }

                else if (AddedProviderGroups.TryGetValue((Change.EntityType, providerGroupId), out var addedProvider))
                    provider = addedProvider;

                else if (ProviderOfGroup(eMobilityProviders, Change.EntityType, Change.EntityId).EMobilityProvider is EMobilityProvider eMobilityProvider)
                    provider = new POIPartyId(nameof(EMobilityProvider), eMobilityProvider.Id.ToString());

                if (provider.HasValue)
                {
                    Owners = [ SignerOf(provider.Value) ];
                    return true;
                }

            }

            #endregion

            #region The infrastructure of a charging station operator, named by its identification

            if (IsInfrastructure(Change.EntityType))
            {

                ChargingStationOperator_Id? operatorId = Change.EntityType switch {
                    nameof(ChargingPool)          => ChargingPool_Id.        TryParse(Change.EntityId, out var id1) ? id1.OperatorId : null,
                    nameof(ChargingStation)       => ChargingStation_Id.     TryParse(Change.EntityId, out var id2) ? id2.OperatorId : null,
                    nameof(ChargingTariff)        => ChargingTariff_Id.      TryParse(Change.EntityId, out var id3) ? id3.OperatorId : null,
                    nameof(EVSETemplate)             => EVSETemplate_Id.           TryParse(Change.EntityId, out var id4)  ? id4. OperatorId : null,
                    nameof(ChargingStationTemplate)  => ChargingStationTemplate_Id.TryParse(Change.EntityId, out var id5)  ? id5. OperatorId : null,
                    nameof(ChargingPoolTemplate)     => ChargingPoolTemplate_Id.   TryParse(Change.EntityId, out var id6)  ? id6. OperatorId : null,
                    nameof(ChargingTariffTemplate)   => ChargingTariffTemplate_Id. TryParse(Change.EntityId, out var id7)  ? id7. OperatorId : null,
                    nameof(EVSEGroup)                => EVSEGroup_Id.              TryParse(Change.EntityId, out var id8)  ? id8. OperatorId : null,
                    nameof(ChargingStationGroup)     => ChargingStationGroup_Id.   TryParse(Change.EntityId, out var id9)  ? id9. OperatorId : null,
                    nameof(ChargingPoolGroup)        => ChargingPoolGroup_Id.      TryParse(Change.EntityId, out var id10) ? id10.OperatorId : null,
                    nameof(ChargingTariffGroup)      => ChargingTariffGroup_Id.    TryParse(Change.EntityId, out var id11) ? id11.OperatorId : null,
                    _                                => EVSE_Id.                   TryParse(Change.EntityId, out var id12) ? id12.OperatorId : null
                };

                if (!operatorId.HasValue)
                {
                    ErrorResponse = $"the identification '{Change.EntityId}' of the {Change.EntityType} names no charging station operator!";
                    return false;
                }

                Owners = [ (new POIPartyId(nameof(ChargingStationOperator), operatorId.Value.ToString()), POIKeyKind.Signing) ];
                return true;

            }

            #endregion

            #region The grid connection points of a grid operator, by their maintainers

            if (Change.EntityType == nameof(GridConnectionPoint))
            {

                if (!GridConnectionPoint_Id.TryParse(Change.EntityId, out var gridConnectionPointId))
                {
                    ErrorResponse = $"invalid {Change.EntityType} identification '{Change.EntityId}'!";
                    return false;
                }

                if (Change.Kind == RoamingNetworkChangeKind.Add)
                {

                    if (Change.ParentEntityType != nameof(GridOperator) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the {Change.EntityType} '{Change.EntityId}' needs its GridOperator as its parent!";
                        return false;
                    }

                    var gridOperatorParty  = PartyIdOf(new POIPartyId(nameof(GridOperator), Change.ParentEntityId));

                    // An invalid maintainer is refused when the change is applied.
                    var newMaintainer      = Change.NewValue is JObject newPoint &&
                                             newPoint[MaintainedByProperty] is JToken maintainedByJSON &&
                                             POIPartyId.TryParse(maintainedByJSON, out var namedMaintainer, out _)
                                                 ? PartyIdOf(namedMaintainer)
                                                 : gridOperatorParty;

                    AddedGridConnectionPoints[gridConnectionPointId] = (gridOperatorParty, newMaintainer);

                    // Added by the maintainer it names, or by its grid operator.
                    Owners = [ .. new[] { SignerOf(newMaintainer), SignerOf(gridOperatorParty) }.Distinct() ];
                    return true;

                }

                POIPartyId gridOperatorId;
                POIPartyId maintainer;

                if (AddedGridConnectionPoints.TryGetValue(gridConnectionPointId, out var added))
                    (gridOperatorId, maintainer) = added;

                else if (TryGetGridConnectionPoint(gridConnectionPointId, out var gridOperator, out var gridConnectionPoint))
                {
                    gridOperatorId  = PartyIdOf(new POIPartyId(nameof(GridOperator), gridOperator.Id.ToString()));
                    maintainer      = gridConnectionPoint.MaintainedBy is POIPartyId maintainedBy
                                          ? PartyIdOf(maintainedBy)
                                          : gridOperatorId;
                }

                else
                {
                    ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' belongs to no grid operator!";
                    return false;
                }

                // Taken over or handed on by its grid operator, handed on by its maintainer, or reassigned by the roaming network operator.
                Owners = isProperty && Change.EntityPropertyName == MaintainedByProperty
                             ? [ .. new[] { SignerOf(gridOperatorId), (maintainer, POIKeyKind.Signing), (roamingNetworkOperator, POIKeyKind.Identity) }.Distinct() ]
                             : [ SignerOf(maintainer) ];

                return true;

            }

            #endregion

            #region The parking data of a parking operator, or the parking spaces of a charging station operator

            if (IsParking(Change.EntityType))
            {

                if (ParkingId(Change.EntityType, Change.EntityId) is not Object parkingId)
                {
                    ErrorResponse = $"invalid {Change.EntityType} identification '{Change.EntityId}'!";
                    return false;
                }

                POIPartyId? owner = null;

                if (Change.Kind == RoamingNetworkChangeKind.Add)
                {

                    // A parking space defined where no parking operator does.
                    if (Change.EntityType == nameof(ParkingSpace) &&
                        Change.ParentEntityType == nameof(ChargingStationOperator) && Change.ParentEntityId is not null)
                    {
                        // The charging station operator as the keys of the parties name it.
                        owner = new POIPartyId(nameof(ChargingStationOperator),
                                               ChargingStationOperator_Id.TryParse(Change.ParentEntityId, out var operatorId) &&
                                               chargingStationOperators.TryGet(operatorId, out var parentOperator)
                                                   ? parentOperator.Id.ToString()
                                                   : Change.ParentEntityId);
                    }

                    else if (Change.ParentEntityType != nameof(ParkingOperator) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = Change.EntityType == nameof(ParkingSpace)
                                            ? $"adding the {Change.EntityType} '{Change.EntityId}' needs its ParkingOperator or ChargingStationOperator as its parent!"
                                            : $"adding the {Change.EntityType} '{Change.EntityId}' needs its ParkingOperator as its parent!";
                        return false;
                    }

                    else
                    {
                        // The parking operator as the keys of the parties name it.
                        owner = new POIPartyId(nameof(ParkingOperator),
                                               ParkingOperator_Id.TryParse(Change.ParentEntityId, out var parentId) &&
                                               parkingOperators.TryGet(parentId, out var parent)
                                                   ? parent.Id.ToString()
                                                   : Change.ParentEntityId);
                    }

                    AddedParking[(Change.EntityType, parkingId)] = owner.Value;

                }

                else if (AddedParking.TryGetValue((Change.EntityType, parkingId), out var addedOwner))
                    owner = addedOwner;

                else if (parkingOperators.FirstOrDefault(parkingOperator => ParkingStoredId(parkingOperator, Change.EntityType, Change.EntityId) is not null) is ParkingOperator parkingOperator)
                    owner = new POIPartyId(nameof(ParkingOperator), parkingOperator.Id.ToString());

                else if (OperatorParkingSpace(chargingStationOperators, Change.EntityType, Change.EntityId) is ChargingStationOperator chargingStationOperator)
                    owner = new POIPartyId(nameof(ChargingStationOperator), chargingStationOperator.Id.ToString());

                if (!owner.HasValue)
                {
                    ErrorResponse = Change.EntityType == nameof(ParkingSpace)
                                        ? $"the {Change.EntityType} '{Change.EntityId}' belongs to no parking operator and no charging station operator!"
                                        : $"the {Change.EntityType} '{Change.EntityId}' belongs to no parking operator!";
                    return false;
                }

                Owners = [ (owner.Value, POIKeyKind.Signing) ];
                return true;

            }

            #endregion

            #region The energy meter templates and groups of a charging station operator or a grid operator

            if (Change.EntityType is nameof(EnergyMeterTemplate) or nameof(EnergyMeterGroup))
            {

                Object? energyMeterChildId = Change.EntityType == nameof(EnergyMeterTemplate)
                                                 ? EnergyMeterTemplate_Id.TryParse(Change.EntityId, out var templateId) ? templateId : null
                                                 : EnergyMeterGroup_Id.   TryParse(Change.EntityId, out var groupId)    ? groupId    : null;

                if (energyMeterChildId is null)
                {
                    ErrorResponse = $"invalid {Change.EntityType} identification '{Change.EntityId}'!";
                    return false;
                }

                POIPartyId? owner = null;

                if (Change.Kind == RoamingNetworkChangeKind.Add)
                {

                    if (Change.ParentEntityType is not (nameof(ChargingStationOperator) or nameof(GridOperator)) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the {Change.EntityType} '{Change.EntityId}' needs its ChargingStationOperator or GridOperator as its parent!";
                        return false;
                    }

                    // The owner as the keys of the parties name it.
                    owner = PartyIdOf(new POIPartyId(Change.ParentEntityType, Change.ParentEntityId));

                    AddedEnergyMeterChildren[(Change.EntityType, energyMeterChildId)] = owner.Value;

                }

                else if (AddedEnergyMeterChildren.TryGetValue((Change.EntityType, energyMeterChildId), out var addedOwner))
                    owner = addedOwner;

                else
                {

                    var (chargingStationOperator, gridOperator, _) = EnergyMeterChildOwners(Change.EntityType == nameof(EnergyMeterTemplate)
                                                                                                ? EnergyMeterTemplateKind
                                                                                                : EnergyMeterGroupKind,
                                                                                            Change.EntityId,
                                                                                            chargingStationOperators,
                                                                                            gridOperators);

                    if      (chargingStationOperator is not null)
                        owner = new POIPartyId(nameof(ChargingStationOperator), chargingStationOperator.Id.ToString());

                    else if (gridOperator is not null)
                        owner = new POIPartyId(nameof(GridOperator),            gridOperator.           Id.ToString());

                }

                if (!owner.HasValue)
                {
                    ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' belongs to no charging station operator and no grid operator!";
                    return false;
                }

                // For a grid operator not taking part, the roaming network operator.
                Owners = [ SignerOf(owner.Value) ];
                return true;

            }

            #endregion

            ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' has no owner!";
            return false;

        }

        #endregion

        #region (private static) TryCheckPartyChange(Change, out ErrorResponse)

        /// <summary>
        /// A party is added with at least one signing and one identity key and without embedded children,
        /// and always keeps at least one signing and one identity key.
        /// </summary>
        private static Boolean TryCheckPartyChange(RoamingNetworkChange              Change,
                                                   [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            var isParty = Change.EntityType == nameof(RoamingNetworkOperator) || PartyTypes.Contains(Change.EntityType);

            if (!isParty)
                return true;

            if (Change.Kind == RoamingNetworkChangeKind.Add)
            {

                if (Change.NewValue is not JObject newParty)
                    return true;

                // A grid operator not taking part is added without any keys.
                var withoutKeys = Change.EntityType == nameof(GridOperator) &&
                                  newParty[POIPartyKeys.SigningKeysProperty]  is null &&
                                  newParty[POIPartyKeys.IdentityKeysProperty] is null;

                if (!withoutKeys)
                    foreach (var keysProperty in new[] { POIPartyKeys.SigningKeysProperty, POIPartyKeys.IdentityKeysProperty })
                        if (newParty[keysProperty] is not JArray { Count: > 0 })
                        {
                            ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' must be added with its {keysProperty}!";
                            return false;
                        }

                if (PartyChildren.TryGetValue(Change.EntityType, out var children) &&
                    children.FirstOrDefault(child => newParty[child] is JArray { Count: > 0 }) is String child)
                {
                    ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' must be added without its {child}: they are its own to add!";
                    return false;
                }

                return true;

            }

            // The signing keys of the roaming network operator are its certificates.
            if (!POIPartyKeys.IsKeyProperty(Change.PropertyName) &&
                !(Change.EntityType == nameof(RoamingNetworkOperator) && Change.PropertyName == RoamingNetworkOperator.PropertyKeys.SigningKeyCertificates))
            {
                return true;
            }

            if (Change.Kind == RoamingNetworkChangeKind.RemoveProperty ||
               (Change.Kind == RoamingNetworkChangeKind.UpdateProperty && Change.NewValue is not JArray { Count: > 0 }))
            {
                ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' must keep at least one of its {Change.PropertyName}!";
                return false;
            }

            return true;

        }

        #endregion

    }

}
