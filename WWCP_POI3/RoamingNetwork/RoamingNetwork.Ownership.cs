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
    /// a signing key for data, an identity key for the keys of a party.
    /// <list type="bullet">
    ///   <item>The roaming network operator (the keys of the roaming network) owns the roaming network itself, brands,
    ///         manufacturers, energy meter manufacturers and transparency software, and adds and removes parties.</item>
    ///   <item>A charging station operator, e-mobility provider, grid operator or parking operator owns its own properties,
    ///         a charging station operator its pools, stations, EVSEs, tariffs and groups, a parking operator its parking data.</item>
    ///   <item>The keys of a party are changed with one of its identity keys or, for recovery, with an identity key
    ///         of the roaming network operator. A party always keeps at least one signing and one identity key.</item>
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
            { nameof(GridOperator),            [] },
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

                nameof(RoamingNetwork)           => PartyId.EntityId == Id.ToString() ? this : null,

                nameof(ChargingStationOperator)  => ChargingStationOperator_Id.TryParse(PartyId.EntityId, out var chargingStationOperatorId) &&
                                                    chargingStationOperators.TryGetValue(chargingStationOperatorId, out var chargingStationOperator)
                                                        ? chargingStationOperator : null,

                nameof(EMobilityProvider)        => EMobilityProvider_Id.TryParse(PartyId.EntityId, out var eMobilityProviderId) &&
                                                    eMobilityProviders.TryGetValue(eMobilityProviderId, out var eMobilityProvider)
                                                        ? eMobilityProvider : null,

                nameof(GridOperator)             => GridOperator_Id.TryParse(PartyId.EntityId, out var gridOperatorId) &&
                                                    gridOperators.TryGetValue(gridOperatorId, out var gridOperator)
                                                        ? gridOperator : null,

                nameof(ParkingOperator)          => ParkingOperator_Id.TryParse(PartyId.EntityId, out var parkingOperatorId) &&
                                                    parkingOperators.TryGetValue(parkingOperatorId, out var parkingOperator)
                                                        ? parkingOperator : null,

                _                                => null

            };

            return Party is not null;

        }

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

                foreach (var chargingStationOperator in chargingStationOperators.Values)
                    yield return (new POIPartyId(nameof(ChargingStationOperator), chargingStationOperator.Id.ToString()), chargingStationOperator);

                foreach (var eMobilityProvider in eMobilityProviders.Values)
                    yield return (new POIPartyId(nameof(EMobilityProvider),       eMobilityProvider.      Id.ToString()), eMobilityProvider);

                foreach (var gridOperator in gridOperators.Values)
                    yield return (new POIPartyId(nameof(GridOperator),            gridOperator.           Id.ToString()), gridOperator);

                foreach (var parkingOperator in parkingOperators.Values)
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

            // Parking entities added by an earlier change of this change set, by their parking operator.
            var addedParking = new Dictionary<(String, String), String>();

            for (var index = 0; index < ChangeSet.Changes.Length; index++)
            {

                var change = ChangeSet.Changes[index];

                if (!TryGetOwners(change, addedParking, out var owners, out ErrorResponse))
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

        #region (private) TryGetOwners(Change, AddedParking, out Owners, out ErrorResponse)

        /// <summary>
        /// The parties and kinds of keys of which one must sign the given change.
        /// </summary>
        private Boolean TryGetOwners(RoamingNetworkChange                                    Change,
                                     Dictionary<(String, String), String>                    AddedParking,
                                     [NotNullWhen(true)]  out (POIPartyId, POIKeyKind)[]?    Owners,
                                     [NotNullWhen(false)] out String?                        ErrorResponse)
        {

            Owners         = null;
            ErrorResponse  = null;

            var roamingNetworkOperator  = new POIPartyId(nameof(RoamingNetwork), Id.ToString());
            var isProperty              = Change.Kind is not (RoamingNetworkChangeKind.Add or RoamingNetworkChangeKind.Remove);

            #region The roaming network itself

            if (Change.EntityType == nameof(RoamingNetwork))
            {
                Owners = [ (roamingNetworkOperator, POIPartyKeys.IsKeyProperty(Change.PropertyName) ? POIKeyKind.Identity : POIKeyKind.Signing) ];
                return true;
            }

            #endregion

            #region Parties: added and removed by the roaming network operator, changed by themselves

            if (PartyTypes.Contains(Change.EntityType))
            {

                var party = new POIPartyId(Change.EntityType, Change.EntityId);

                Owners = !isProperty
                             ? [ (roamingNetworkOperator, POIKeyKind.Signing) ]
                             : POIPartyKeys.IsKeyProperty(Change.PropertyName)
                                   ? [ (party, POIKeyKind.Identity), (roamingNetworkOperator, POIKeyKind.Identity) ]
                                   : [ (party, POIKeyKind.Signing) ];

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

            #region The parking data of a parking operator

            if (IsParking(Change.EntityType))
            {

                String? parkingOperatorId = null;

                if (Change.Kind == RoamingNetworkChangeKind.Add)
                {

                    if (Change.ParentEntityType != nameof(ParkingOperator) || Change.ParentEntityId is null)
                    {
                        ErrorResponse = $"adding the {Change.EntityType} '{Change.EntityId}' needs its ParkingOperator as its parent!";
                        return false;
                    }

                    parkingOperatorId = Change.ParentEntityId;
                    AddedParking[(Change.EntityType, Change.EntityId)] = parkingOperatorId;

                }

                else if (!AddedParking.TryGetValue((Change.EntityType, Change.EntityId), out parkingOperatorId))
                    parkingOperatorId = parkingOperators.Values.FirstOrDefault(parkingOperator => ParkingIds(parkingOperator, Change.EntityType).Contains(Change.EntityId))?.Id.ToString();

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

        #region (private static) ParkingIds(ParkingOperator, EntityType)

        /// <summary>
        /// The identifications of the parking entities of the given type of the given parking operator.
        /// </summary>
        private static IEnumerable<String> ParkingIds(ParkingOperator  ParkingOperator,
                                                      String           EntityType)

            => EntityType switch {
                   nameof(ParkingGarage)      => ParkingOperator.ParkingGarages.    Select(parkingGarage     => parkingGarage.    Id.ToString()),
                   nameof(ParkingSpace)       => ParkingOperator.ParkingSpaces.     Select(parkingSpace      => parkingSpace.     Id.ToString()),
                   nameof(ParkingSensor)      => ParkingOperator.ParkingSensors.    Select(parkingSensor     => parkingSensor.    Id.ToString()),
                   nameof(ParkingSpaceGroup)  => ParkingOperator.ParkingSpaceGroups.Select(parkingSpaceGroup => parkingSpaceGroup.Id.ToString()),
                   _                          => ParkingOperator.ParkingProducts.   Select(parkingProduct    => parkingProduct.   Id.ToString())
               };

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
