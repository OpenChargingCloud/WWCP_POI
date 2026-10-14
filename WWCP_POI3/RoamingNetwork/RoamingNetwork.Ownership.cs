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
    /// <param name="EntityType">The entity type, e.g. ChargingStationOperator, or RoamingNetwork for the roaming network operator.</param>
    /// <param name="EntityId">The identification of the entity.</param>
    public readonly record struct POIPartyId(String  EntityType,
                                             String  EntityId)
    {

        /// <summary>
        /// { "entityType", "entityId" }.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty("entityType",  EntityType),
                   new JProperty("entityId",    EntityId)
               );

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

            if (JSON is not JObject json                                                                         ||
                json["entityType"] is not JValue { Type: JTokenType.String } entityType                         ||
                json["entityId"]   is not JValue { Type: JTokenType.String } entityId                           ||
                json.Count != 2                                                                                  ||
                String.IsNullOrWhiteSpace(entityType.Value<String>())                                            ||
                String.IsNullOrWhiteSpace(entityId.  Value<String>()))
            {
                ErrorResponse = "A party must be { \"entityType\", \"entityId\" }!";
                return false;
            }

            PartyId        = new POIPartyId(entityType.Value<String>()!, entityId.Value<String>()!);
            ErrorResponse  = null;
            return true;

        }

        /// <inheritdoc/>
        public override String ToString()

            => EntityType == nameof(RoamingNetwork)
                   ? $"the roaming network operator of '{EntityId}'"
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
    ///   <item>The roaming network operator (the keys of the roaming network) owns the roaming network itself, brands,
    ///         manufacturers, energy meter manufacturers and transparency software, and adds and removes parties:
    ///         it decides who is a member, so removing a party removes all its data with it, without its signature.</item>
    ///   <item>A charging station operator, e-mobility provider, grid operator or parking operator owns its own properties,
    ///         a charging station operator its pools, stations, EVSEs, tariffs and groups, a parking operator its parking data.</item>
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
        /// The entity types of parties, besides the roaming network itself.
        /// </summary>
        private static readonly String[] PartyTypes = [
            nameof(ChargingStationOperator),
            nameof(EMobilityProvider),
            nameof(GridOperator),
            nameof(ParkingOperator)
        ];

        /// <summary>
        /// The embedded children a party must not be added with, by its entity type.
        /// </summary>
        private static readonly Dictionary<String, String[]> PartyChildren = new () {
            { nameof(ChargingStationOperator), [ "chargingPools", "chargingTariffs", "EVSEGroups", "chargingStationGroups", "chargingPoolGroups", "chargingTariffGroups" ] },
            { nameof(EMobilityProvider),       [] },
            { nameof(GridOperator),            [ "gridConnectionPoints" ] },
            { nameof(ParkingOperator),         [ "parkingGarages", "parkingSpaces", "parkingSensors", "parkingSpaceGroups", "parkingProducts" ] }
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

                nameof(RoamingNetwork)           => IsThisRoamingNetwork(PartyId.EntityId) ? this : null,

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

        #region Parties

        /// <summary>
        /// All parties of this version: the roaming network operator and every operator and provider.
        /// </summary>
        public IEnumerable<(POIPartyId Id, IPOIParty Party)> Parties
        {
            get
            {

                yield return (new POIPartyId(nameof(RoamingNetwork), Id.ToString()), this);

                foreach (var chargingStationOperator in chargingStationOperators)
                    yield return (new POIPartyId(nameof(ChargingStationOperator), chargingStationOperator.Id.ToString()), chargingStationOperator);

                foreach (var eMobilityProvider in eMobilityProviders)
                    yield return (new POIPartyId(nameof(EMobilityProvider),       eMobilityProvider.      Id.ToString()), eMobilityProvider);

                foreach (var gridOperator in gridOperators)
                    yield return (new POIPartyId(nameof(GridOperator),            gridOperator.           Id.ToString()), gridOperator);

                foreach (var parkingOperator in parkingOperators)
                    yield return (new POIPartyId(nameof(ParkingOperator),         parkingOperator.        Id.ToString()), parkingOperator);

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

            #region The parties and kinds of keys that signed it

            var signedBy = new HashSet<(POIPartyId, POIKeyKind)>();

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

                        signedBy.Add((partyId, kind));

                    }
                }

                if (!keyFound)
                {
                    ErrorResponse = $"Signature {index}: the key '{signature.KeyId}' is no key of a party of the roaming network '{Id}'!";
                    return false;
                }

            }

            #endregion

            #region Every change needs one of its owners

            // Parking entities added by an earlier change of this change set, by their parking operator;
            // keyed by their typed identifications, so that a later change may name them in another spelling.
            var addedParking = new Dictionary<(String, Object), String>();

            // Grid connection points added by an earlier change of this change set: their grid operator and maintainer.
            var addedGridConnectionPoints = new Dictionary<GridConnectionPoint_Id, (POIPartyId GridOperator, POIPartyId Maintainer)>();

            for (var index = 0; index < ChangeSet.Changes.Length; index++)
            {

                var change = ChangeSet.Changes[index];

                if (!TryGetOwners(change, addedParking, addedGridConnectionPoints, out var owners, out ErrorResponse))
                {
                    ErrorResponse = $"Change {index}: {ErrorResponse}";
                    return false;
                }

                if (!owners.Any(owner => signedBy.Contains(owner)))
                {
                    ErrorResponse = $"Change {index}: {change.Kind} of the {change.EntityType} '{change.EntityId}'{(change.PropertyName is not null ? $" ('{change.PropertyName}')" : "")} needs a signature by " +
                                    String.Join(" or ", owners.Select(owner => $"a{(owner.Item2 == POIKeyKind.Identity ? "n identity" : " signing")} key of {owner.Item1}")) + "!";
                    return false;
                }

                if (!TryCheckPartyChange(change, out ErrorResponse))
                {
                    ErrorResponse = $"Change {index}: {ErrorResponse}";
                    return false;
                }

            }

            #endregion

            ErrorResponse = null;
            return true;

        }

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

            => TryGetParty(PartyId, out var party) && party.SigningKeys.IsDefaultOrEmpty && PartyId.EntityType != nameof(RoamingNetwork)
                   ? (new POIPartyId(nameof(RoamingNetwork), Id.ToString()), POIKeyKind.Signing)
                   : (PartyIdOf(PartyId), POIKeyKind.Signing);

        #endregion

        #region (private) TryGetOwners(Change, AddedParking, AddedGridConnectionPoints, out Owners, out ErrorResponse)

        /// <summary>
        /// The parties and kinds of keys of which one must sign the given change.
        /// </summary>
        private Boolean TryGetOwners(RoamingNetworkChange                                                                    Change,
                                     Dictionary<(String, Object), String>                                                    AddedParking,
                                     Dictionary<GridConnectionPoint_Id, (POIPartyId GridOperator, POIPartyId Maintainer)>  AddedGridConnectionPoints,
                                     [NotNullWhen(true)]  out (POIPartyId, POIKeyKind)[]?                                    Owners,
                                     [NotNullWhen(false)] out String?                                                        ErrorResponse)
        {

            Owners         = null;
            ErrorResponse  = null;

            var roamingNetworkOperator  = new POIPartyId(nameof(RoamingNetwork), Id.ToString());
            var isProperty              = Change.Kind is not (RoamingNetworkChangeKind.Add or RoamingNetworkChangeKind.Remove);

            #region The roaming network itself

            if (Change.EntityType == nameof(RoamingNetwork))
            {
                Owners = Change.PropertyName == POIPartyKeys.SigningKeysProperty
                             ? [ (roamingNetworkOperator, POIKeyKind.Signing), (roamingNetworkOperator, POIKeyKind.Identity) ]
                             : [ (roamingNetworkOperator, POIPartyKeys.IsKeyProperty(Change.PropertyName) ? POIKeyKind.Identity : POIKeyKind.Signing) ];
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
                             : Change.PropertyName == POIPartyKeys.SigningKeysProperty
                                   ? [ (party, POIKeyKind.Signing), (party, POIKeyKind.Identity), (roamingNetworkOperator, POIKeyKind.Identity) ]
                                   : POIPartyKeys.IsKeyProperty(Change.PropertyName)
                                         ? [ (party, POIKeyKind.Identity), (roamingNetworkOperator, POIKeyKind.Identity) ]
                                         : [ SignerOf(party) ];

                return true;

            }

            #endregion

            #region Registries of the roaming network operator

            if (Change.EntityType is nameof(ChargingStationManufacturer)
                                  or nameof(EnergyMeterManufacturer)
                                  or nameof(Brand)
                                  or nameof(TransparencySoftware)
                                  or nameof(TransparencySoftwareCertificate))
            {
                Owners = [ (roamingNetworkOperator, POIKeyKind.Signing) ];
                return true;
            }

            #endregion

            #region The infrastructure of a charging station operator, named by its identification

            if (IsInfrastructure(Change.EntityType))
            {

                ChargingStationOperator_Id? operatorId = Change.EntityType switch {
                    nameof(ChargingPool)          => ChargingPool_Id.        TryParse(Change.EntityId, out var id1) ? id1.OperatorId : null,
                    nameof(ChargingStation)       => ChargingStation_Id.     TryParse(Change.EntityId, out var id2) ? id2.OperatorId : null,
                    nameof(ChargingTariff)        => ChargingTariff_Id.      TryParse(Change.EntityId, out var id3) ? id3.OperatorId : null,
                    nameof(EVSEGroup)             => EVSEGroup_Id.           TryParse(Change.EntityId, out var id4) ? id4.OperatorId : null,
                    nameof(ChargingStationGroup)  => ChargingStationGroup_Id.TryParse(Change.EntityId, out var id5) ? id5.OperatorId : null,
                    nameof(ChargingPoolGroup)     => ChargingPoolGroup_Id.   TryParse(Change.EntityId, out var id6) ? id6.OperatorId : null,
                    nameof(ChargingTariffGroup)   => ChargingTariffGroup_Id. TryParse(Change.EntityId, out var id7) ? id7.OperatorId : null,
                    _                             => EVSE_Id.                TryParse(Change.EntityId, out var id8) ? id8.OperatorId : null
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
                Owners = isProperty && Change.PropertyName == MaintainedByProperty
                             ? [ .. new[] { SignerOf(gridOperatorId), (maintainer, POIKeyKind.Signing), (roamingNetworkOperator, POIKeyKind.Identity) }.Distinct() ]
                             : [ SignerOf(maintainer) ];

                return true;

            }

            #endregion

            #region The parking data of a parking operator

            if (IsParking(Change.EntityType))
            {

                if (ParkingId(Change.EntityType, Change.EntityId) is not Object parkingId)
                {
                    ErrorResponse = $"invalid {Change.EntityType} identification '{Change.EntityId}'!";
                    return false;
                }

                String? parkingOperatorId = null;

                if (Change.Kind == RoamingNetworkChangeKind.Add)
                {

                    if (Change.ParentEntityType != nameof(ParkingOperator) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the {Change.EntityType} '{Change.EntityId}' needs its ParkingOperator as its parent!";
                        return false;
                    }

                    // The parking operator as the keys of the parties name it.
                    parkingOperatorId = ParkingOperator_Id.TryParse(Change.ParentEntityId, out var parentId) &&
                                        parkingOperators.TryGet(parentId, out var parent)
                                            ? parent.Id.ToString()
                                            : Change.ParentEntityId;

                    AddedParking[(Change.EntityType, parkingId)] = parkingOperatorId;

                }

                else if (!AddedParking.TryGetValue((Change.EntityType, parkingId), out parkingOperatorId))
                    parkingOperatorId = parkingOperators.FirstOrDefault(parkingOperator => ParkingStoredId(parkingOperator, Change.EntityType, Change.EntityId) is not null)?.Id.ToString();

                if (parkingOperatorId is null)
                {
                    ErrorResponse = $"the {Change.EntityType} '{Change.EntityId}' belongs to no parking operator!";
                    return false;
                }

                Owners = [ (new POIPartyId(nameof(ParkingOperator), parkingOperatorId), POIKeyKind.Signing) ];
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

            var isParty = Change.EntityType == nameof(RoamingNetwork) || PartyTypes.Contains(Change.EntityType);

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

            if (!POIPartyKeys.IsKeyProperty(Change.PropertyName))
                return true;

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
