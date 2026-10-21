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

using System.Runtime.CompilerServices;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The digests of the entities of the effective content of a version (SPECIFICATION.md, section 8.2), kept with the
    /// entities: they are immutable and shared by the versions that did not change them, so a commit hashes what it changed
    /// and the paths to the root (section 8.7). The roaming network and its parties keep their nodes as well, with the leaf
    /// hashes of their child arrays, for proofs.
    /// </summary>
    /// <remarks>
    /// The digests are those POIEffectiveTree.TryDigest(…) calculates over ToEffectiveContentJSON(), without its JSON: an
    /// entity with child arrays writes its JSON without them, and their roots take their places, calculated from the digests
    /// of its children, so that every entity is written once. An effective member is an entity of its own; a member taking
    /// nothing from templates is itself, and keeps its digest as long as it is not changed.
    /// </remarks>
    internal static class POIEffectiveDigests
    {

        #region Data

        /// <summary>
        /// What is kept with an entity: its digest and, for the roaming network and its parties, its node, by hash algorithm.
        /// </summary>
        private sealed class Entry
        {
            public Byte[]?       SHA256Digest;
            public Byte[]?       SHA384Digest;
            public Byte[]?       SHA512Digest;
            public POITreeNode?  SHA256Node;
            public POITreeNode?  SHA384Node;
            public POITreeNode?  SHA512Node;
        }

        private static readonly ConditionalWeakTable<Object, Entry> entries = new ();

        #endregion


        #region Digest(RoamingNetwork, HashAlgorithm)

        /// <summary>
        /// The digest of the given version, the root of its tree: its effectiveTree ETag.
        /// </summary>
        /// <param name="RoamingNetwork">A version of a roaming network.</param>
        /// <param name="HashAlgorithm">The hash algorithm of the tree.</param>
        public static Byte[] Digest(RoamingNetwork     RoamingNetwork,
                                    ETagHashAlgorithm  HashAlgorithm)

            => NodeOf(RoamingNetwork, HashAlgorithm).Digest;

        #endregion

        #region Root(RoamingNetwork, HashAlgorithm)

        /// <summary>
        /// The node of the given version, the root of its tree, with the nodes of the children of its child arrays: of its
        /// parties with the leaf hashes of their child arrays, of the others with their JSON (section 8.4).
        /// </summary>
        /// <param name="RoamingNetwork">A version of a roaming network.</param>
        /// <param name="HashAlgorithm">The hash algorithm of the tree.</param>
        public static POITreeNode Root(RoamingNetwork     RoamingNetwork,
                                       ETagHashAlgorithm  HashAlgorithm)
        {

            var root      = NodeOf(RoamingNetwork, HashAlgorithm);
            var children  = new Dictionary<String, IReadOnlyList<POITreeNode>> {
                                { RoamingNetwork.PropertyKeys.ChargingStationOperators,          [ .. RoamingNetwork.ChargingStationOperators.        Select(party => NodeOf(party, HashAlgorithm)) ] },
                                { RoamingNetwork.PropertyKeys.EMobilityProviders,                [ .. RoamingNetwork.EMobilityProviders.              Select(party => NodeOf(party, HashAlgorithm)) ] },
                                { RoamingNetwork.PropertyKeys.GridOperators,                     [ .. RoamingNetwork.GridOperators.                   Select(party => NodeOf(party, HashAlgorithm)) ] },
                                { RoamingNetwork.PropertyKeys.ParkingOperators,                  [ .. RoamingNetwork.ParkingOperators.                Select(party => NodeOf(party, HashAlgorithm)) ] },
                                { RoamingNetwork.PropertyKeys.ChargingStationManufacturers,      [ .. RoamingNetwork.ChargingStationManufacturers.    Select(value => LeafNode(RoamingNetwork.PropertyKeys.ChargingStationManufacturers,     value, value.ToJSON(Embedded: true), HashAlgorithm)) ] },
                                { RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,          [ .. RoamingNetwork.EnergyMeterManufacturers.        Select(value => LeafNode(RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,         value, value.ToJSON(Embedded: true), HashAlgorithm)) ] },
                                { RoamingNetwork.PropertyKeys.TransparencySoftware,              [ .. RoamingNetwork.TransparencySoftware.            Select(value => LeafNode(RoamingNetwork.PropertyKeys.TransparencySoftware,             value, value.ToJSON(Embedded: true), HashAlgorithm)) ] },
                                { RoamingNetwork.PropertyKeys.TransparencySoftwareCertificates,  [ .. RoamingNetwork.TransparencySoftwareCertificates.Select(value => LeafNode(RoamingNetwork.PropertyKeys.TransparencySoftwareCertificates, value, value.ToJSON(Embedded: true), HashAlgorithm)) ] }
                            };

            return root with {
                       Arrays = root.Arrays.ToDictionary(array => array.Key,
                                                         array => array.Value with { Children = children[array.Key] })
                   };

        }

        #endregion


        #region (private) NodeOf(RoamingNetwork, HashAlgorithm)

        private static POITreeNode NodeOf(RoamingNetwork     RoamingNetwork,
                                          ETagHashAlgorithm  HashAlgorithm)

            => Kept(RoamingNetwork, HashAlgorithm, () => Node(

                   POIEffectiveTree.RoamingNetworkKind,

                   // As the effective content has it: ToJSON(IncludeETags: false), without its child arrays.
                   RoamingNetwork.ToJSON(Embedded:                                 false,
                                         ExpandRoamingNetworkIds:                  InfoStatus.ShowIdOnly,
                                         ExpandChargingStationOperatorIds:         InfoStatus.Expanded,
                                         ExpandChargingPoolIds:                    InfoStatus.ShowIdOnly,
                                         ExpandChargingStationIds:                 InfoStatus.ShowIdOnly,
                                         ExpandEVSEIds:                            InfoStatus.ShowIdOnly,
                                         ExpandBrandIds:                           InfoStatus.ShowIdOnly,
                                         ExpandDataLicenses:                       InfoStatus.Expanded,
                                         ExpandEMobilityProviderId:                InfoStatus.ShowIdOnly,
                                         CustomRoamingNetworkSerializer:           null,
                                         CustomChargingStationOperatorSerializer:  null,
                                         IncludeETags:                             false,
                                         ETagDigestEncoding:                       null,
                                         IncludeChildArrays:                       false),

                   [ (RoamingNetwork.PropertyKeys.ChargingStationOperators,          [ .. RoamingNetwork.ChargingStationOperators.        Select(party => NodeOf(party, HashAlgorithm).Digest) ]),
                     (RoamingNetwork.PropertyKeys.ChargingStationManufacturers,      Leaves(RoamingNetwork.ChargingStationManufacturers,     value => value.ToJSON(Embedded: true), HashAlgorithm)),
                     (RoamingNetwork.PropertyKeys.EMobilityProviders,                [ .. RoamingNetwork.EMobilityProviders.              Select(party => NodeOf(party, HashAlgorithm).Digest) ]),
                     (RoamingNetwork.PropertyKeys.GridOperators,                     [ .. RoamingNetwork.GridOperators.                   Select(party => NodeOf(party, HashAlgorithm).Digest) ]),
                     (RoamingNetwork.PropertyKeys.ParkingOperators,                  [ .. RoamingNetwork.ParkingOperators.                Select(party => NodeOf(party, HashAlgorithm).Digest) ]),
                     (RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,          Leaves(RoamingNetwork.EnergyMeterManufacturers,         value => value.ToJSON(Embedded: true), HashAlgorithm)),
                     (RoamingNetwork.PropertyKeys.TransparencySoftware,              Leaves(RoamingNetwork.TransparencySoftware,             value => value.ToJSON(Embedded: true), HashAlgorithm)),
                     (RoamingNetwork.PropertyKeys.TransparencySoftwareCertificates,  Leaves(RoamingNetwork.TransparencySoftwareCertificates,  value => value.ToJSON(Embedded: true), HashAlgorithm)) ],

                   HashAlgorithm));

        #endregion

        #region (private) NodeOf(ChargingStationOperator, HashAlgorithm)

        private static POITreeNode NodeOf(ChargingStationOperator  ChargingStationOperator,
                                          ETagHashAlgorithm        HashAlgorithm)

            => Kept(ChargingStationOperator, HashAlgorithm, () => {

                   var effectivePools    = ChargingStationOperator.EffectiveChargingPoolsOfTheContent();
                   var effectiveTariffs  = ChargingStationOperator.EffectiveChargingTariffsOfTheContent();

                   return Node(

                       RoamingNetwork.PropertyKeys.ChargingStationOperators,

                       // As its roaming network embeds it, without its child arrays.
                       ChargingStationOperator.ToJSON(Embedded:                                 true,
                                                      ExpandChargingPoolIds:                    InfoStatus.ShowIdOnly,
                                                      ExpandChargingStationIds:                 InfoStatus.ShowIdOnly,
                                                      ExpandEVSEIds:                            InfoStatus.ShowIdOnly,
                                                      ExpandBrandIds:                           InfoStatus.ShowIdOnly,
                                                      ExpandDataLicenses:                       InfoStatus.Expanded,
                                                      CustomChargingStationOperatorSerializer:  null,
                                                      IncludeChildArrays:                       false),

                       [ (ChargingStationOperator.PropertyKeys.ChargingPools,             [ .. ChargingStationOperator.ChargingPools.Select(chargingPool => DigestOf(effectivePools.TryGetValue(chargingPool.Id, out var effectivePool)
                                                                                                                                  ? effectivePool
                                                                                                                                  : chargingPool,
                                                                                                                              HashAlgorithm)) ]),
                         (ChargingStationOperator.PropertyKeys.ChargingTariffs,           Leaves(ChargingStationOperator.ChargingTariffs.Select(chargingTariff => effectiveTariffs.TryGetValue(chargingTariff.Id, out var effectiveTariff)
                                                                                                                                  ? effectiveTariff
                                                                                                                                  : chargingTariff),
                                                              value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.EVSETemplates,             Leaves(ChargingStationOperator.EVSETemplates,             value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.ChargingStationTemplates,  Leaves(ChargingStationOperator.ChargingStationTemplates,  value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.ChargingPoolTemplates,     Leaves(ChargingStationOperator.ChargingPoolTemplates,     value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.ChargingTariffTemplates,   Leaves(ChargingStationOperator.ChargingTariffTemplates,   value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.EnergyMeterTemplates,      Leaves(ChargingStationOperator.EnergyMeterTemplates,      value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.EVSEGroups,                Leaves(ChargingStationOperator.EVSEGroups,                value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.ChargingStationGroups,     Leaves(ChargingStationOperator.ChargingStationGroups,     value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.ChargingPoolGroups,        Leaves(ChargingStationOperator.ChargingPoolGroups,        value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.ChargingTariffGroups,      Leaves(ChargingStationOperator.ChargingTariffGroups,      value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.EnergyMeterGroups,         Leaves(ChargingStationOperator.EnergyMeterGroups,         value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.ChargingTariffOffers,      Leaves(ChargingStationOperator.ChargingTariffOffers,      value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ChargingStationOperator.PropertyKeys.ParkingSpaces,             Leaves(ChargingStationOperator.ParkingSpaces,             value => value.ToJSON(Embedded: true), HashAlgorithm)) ],

                       HashAlgorithm);

               });

        #endregion

        #region (private) NodeOf(EMobilityProvider, HashAlgorithm)

        private static POITreeNode NodeOf(EMobilityProvider  EMobilityProvider,
                                          ETagHashAlgorithm  HashAlgorithm)

            => Kept(EMobilityProvider, HashAlgorithm, () => Node(

                   RoamingNetwork.PropertyKeys.EMobilityProviders,

                   EMobilityProvider.ToJSON(Embedded: true, IncludeETags: false, ETagDigestEncoding: null, CustomEMobilityProviderSerializer: null, IncludeChildArrays: false),

                   [ (EMobilityProvider.PropertyKeys.EVSEGroups,             Leaves(EMobilityProvider.EVSEGroups,             value => value.ToJSON(Embedded: true), HashAlgorithm)),
                     (EMobilityProvider.PropertyKeys.ChargingStationGroups,  Leaves(EMobilityProvider.ChargingStationGroups,  value => value.ToJSON(Embedded: true), HashAlgorithm)),
                     (EMobilityProvider.PropertyKeys.ChargingPoolGroups,     Leaves(EMobilityProvider.ChargingPoolGroups,     value => value.ToJSON(Embedded: true), HashAlgorithm)),
                     (EMobilityProvider.PropertyKeys.ChargingTariffOffers,   Leaves(EMobilityProvider.ChargingTariffOffers,   value => value.ToJSON(Embedded: true), HashAlgorithm)) ],

                   HashAlgorithm));

        #endregion

        #region (private) NodeOf(GridOperator, HashAlgorithm)

        private static POITreeNode NodeOf(GridOperator       GridOperator,
                                          ETagHashAlgorithm  HashAlgorithm)

            => Kept(GridOperator, HashAlgorithm, () => Node(

                   RoamingNetwork.PropertyKeys.GridOperators,

                   GridOperator.ToJSON(IncludeChildArrays: false),

                   [ (GridOperator.PropertyKeys.GridConnectionPoints,  Leaves(GridOperator.GridConnectionPoints.Select(GridOperator.EffectiveGridConnectionPoint), value => value.ToJSON(Embedded: true), HashAlgorithm)),
                     (GridOperator.PropertyKeys.EnergyMeterTemplates,  Leaves(GridOperator.EnergyMeterTemplates,  value => value.ToJSON(Embedded: true), HashAlgorithm)),
                     (GridOperator.PropertyKeys.EnergyMeterGroups,     Leaves(GridOperator.EnergyMeterGroups,     value => value.ToJSON(Embedded: true), HashAlgorithm)) ],

                   HashAlgorithm));

        #endregion

        #region (private) NodeOf(ParkingOperator, HashAlgorithm)

        private static POITreeNode NodeOf(ParkingOperator    ParkingOperator,
                                          ETagHashAlgorithm  HashAlgorithm)

            => Kept(ParkingOperator, HashAlgorithm, () => {

                   var effectiveSpaces = ParkingOperator.EffectiveParkingSpacesOfTheContent();

                   return Node(

                       RoamingNetwork.PropertyKeys.ParkingOperators,

                       ParkingOperator.ToJSON(IncludeChildArrays: false),

                       [ (ParkingOperator.PropertyKeys.ParkingGarages,         Leaves(ParkingOperator.ParkingGarages,         value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ParkingOperator.PropertyKeys.ParkingSpaces,          Leaves(ParkingOperator.ParkingSpaces.Select(parkingSpace => effectiveSpaces.TryGetValue(parkingSpace.Id, out var effectiveSpace)
                                                                                                                    ? effectiveSpace
                                                                                                                    : parkingSpace),
                                                           value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ParkingOperator.PropertyKeys.ParkingSensors,         Leaves(ParkingOperator.ParkingSensors,         value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ParkingOperator.PropertyKeys.ParkingSpaceTemplates,  Leaves(ParkingOperator.ParkingSpaceTemplates,  value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ParkingOperator.PropertyKeys.ParkingSpaceGroups,     Leaves(ParkingOperator.ParkingSpaceGroups,     value => value.ToJSON(Embedded: true), HashAlgorithm)),
                         (ParkingOperator.PropertyKeys.ParkingProducts,        Leaves(ParkingOperator.ParkingProducts,        value => value.ToJSON(Embedded: true), HashAlgorithm)) ],

                       HashAlgorithm);

               });

        #endregion

        #region (private) DigestOf(ChargingPool,    HashAlgorithm)

        /// <summary>
        /// The digest of the given charging pool as its operator embeds it, its charging stations by their digests.
        /// </summary>
        private static Byte[] DigestOf(ChargingPool       ChargingPool,
                                       ETagHashAlgorithm  HashAlgorithm)

            => KeptDigest(ChargingPool, HashAlgorithm, () => {

                   var json = ChargingPool.ToJSON(Embedded: true, IncludeChildArrays: false);

                   if (ChargingPool.ChargingStations.Count > 0)
                       json[ChargingPool.PropertyKeys.ChargingStations] = Pruned([ .. ChargingPool.ChargingStations.Select(chargingStation => DigestOf(chargingStation, HashAlgorithm)) ], HashAlgorithm);

                   return POIMerkleTree.LeafHash(HashAlgorithm, CanonicalJSON.ToUTF8Bytes(json));

               });

        #endregion

        #region (private) DigestOf(ChargingStation, HashAlgorithm)

        /// <summary>
        /// The digest of the given charging station as its pool embeds it, its EVSEs by their digests.
        /// </summary>
        private static Byte[] DigestOf(ChargingStation    ChargingStation,
                                       ETagHashAlgorithm  HashAlgorithm)

            => KeptDigest(ChargingStation, HashAlgorithm, () => {

                   var json = ChargingStation.ToJSON(Embedded: true, IncludeETags: false, ETagDigestEncoding: null, CustomChargingStationSerializer: null, IncludeChildArrays: false);

                   if (ChargingStation.EVSEs.Count > 0)
                       json[ChargingStation.PropertyKeys.EVSEs] = Pruned(Leaves(ChargingStation.EVSEs, evse => evse.ToJSON(Embedded: true), HashAlgorithm), HashAlgorithm);

                   return POIMerkleTree.LeafHash(HashAlgorithm, CanonicalJSON.ToUTF8Bytes(json));

               });

        #endregion


        #region (private) Node(Kind, JSON, ChildArrays, HashAlgorithm)

        /// <summary>
        /// The node of an entity of the given JSON without its child arrays: those of the given child arrays having children
        /// take their pruned forms, as the entity writes only those having children.
        /// </summary>
        private static POITreeNode Node(String                                                Kind,
                                        JObject                                               JSON,
                                        IEnumerable<(String Name, IReadOnlyList<Byte[]> LeafHashes)>  ChildArrays,
                                        ETagHashAlgorithm                                     HashAlgorithm)
        {

            var arrays = new Dictionary<String, POITreeArray>();

            foreach (var (name, leafHashes) in ChildArrays)
            {

                if (leafHashes.Count == 0)
                    continue;

                var root = POIMerkleTree.RootOfLeafHashes(HashAlgorithm, leafHashes);

                JSON[name] = POIEffectiveTree.Pruned(root, (UInt64) leafHashes.Count);

                arrays.Add(name, new POITreeArray(name, leafHashes, root, null));

            }

            return new POITreeNode(Kind,
                                   JSON[POIEffectiveTree.PropertyKeys.Id]?.Type == JTokenType.String ? JSON[POIEffectiveTree.PropertyKeys.Id]!.Value<String>() : null,
                                   POIMerkleTree.LeafHash(HashAlgorithm, CanonicalJSON.ToUTF8Bytes(JSON)),
                                   JSON,
                                   arrays);

        }

        #endregion

        #region (private) LeafNode(Kind, Entity, JSON, HashAlgorithm)

        /// <summary>
        /// The node of an entity without child arrays: its JSON is its skeleton.
        /// </summary>
        private static POITreeNode LeafNode(String             Kind,
                                            Object             Entity,
                                            JObject            JSON,
                                            ETagHashAlgorithm  HashAlgorithm)

            => new (Kind,
                    JSON[POIEffectiveTree.PropertyKeys.Id]?.Type == JTokenType.String ? JSON[POIEffectiveTree.PropertyKeys.Id]!.Value<String>() : null,
                    KeptDigest(Entity, HashAlgorithm, () => POIMerkleTree.LeafHash(HashAlgorithm, CanonicalJSON.ToUTF8Bytes(JSON))),
                    JSON,
                    new Dictionary<String, POITreeArray>());

        #endregion

        #region (private) Leaves<T>(Entities, ToJSON, HashAlgorithm)

        /// <summary>
        /// The digests of the given entities without child arrays, of the JSON their parent embeds.
        /// </summary>
        private static Byte[][] Leaves<T>(IEnumerable<T>     Entities,
                                          Func<T, JObject>   ToJSON,
                                          ETagHashAlgorithm  HashAlgorithm)

            where T : class

            => [ .. Entities.Select(entity => KeptDigest(entity, HashAlgorithm, () => POIMerkleTree.LeafHash(HashAlgorithm, CanonicalJSON.ToUTF8Bytes(ToJSON(entity))))) ];

        #endregion

        #region (private) Pruned(LeafHashes, HashAlgorithm)

        private static JObject Pruned(IReadOnlyList<Byte[]>  LeafHashes,
                                      ETagHashAlgorithm      HashAlgorithm)

            => POIEffectiveTree.Pruned(POIMerkleTree.RootOfLeafHashes(HashAlgorithm, LeafHashes),
                                       (UInt64) LeafHashes.Count);

        #endregion


        #region (private) Kept(Entity, HashAlgorithm, Calculate) / KeptDigest(Entity, HashAlgorithm, Calculate)

        /// <summary>
        /// The node of the given entity, calculated once: concurrent callers may calculate it both, with the same result.
        /// </summary>
        private static POITreeNode Kept(Object             Entity,
                                        ETagHashAlgorithm  HashAlgorithm,
                                        Func<POITreeNode>  Calculate)
        {

            var entry = entries.GetValue(Entity, _ => new Entry());

            if (Volatile.Read(ref NodeOf(entry, HashAlgorithm)) is POITreeNode node)
                return node;

            node = Calculate();

            Volatile.Write(ref DigestOf(entry, HashAlgorithm), node.Digest);
            Volatile.Write(ref NodeOf  (entry, HashAlgorithm), node);

            return node;

        }

        /// <summary>
        /// The digest of the given entity, calculated once: concurrent callers may calculate it both, with the same result.
        /// </summary>
        private static Byte[] KeptDigest(Object             Entity,
                                         ETagHashAlgorithm  HashAlgorithm,
                                         Func<Byte[]>       Calculate)
        {

            var entry = entries.GetValue(Entity, _ => new Entry());

            if (Volatile.Read(ref DigestOf(entry, HashAlgorithm)) is Byte[] digest)
                return digest;

            digest = Calculate();

            Volatile.Write(ref DigestOf(entry, HashAlgorithm), digest);

            return digest;

        }

        #endregion

        #region (private) DigestOf(Entry, HashAlgorithm) / NodeOf(Entry, HashAlgorithm)

        private static ref Byte[]? DigestOf(Entry              Entry,
                                            ETagHashAlgorithm  HashAlgorithm)
        {
            switch (HashAlgorithm)
            {
                case ETagHashAlgorithm.SHA256:  return ref Entry.SHA256Digest;
                case ETagHashAlgorithm.SHA384:  return ref Entry.SHA384Digest;
                case ETagHashAlgorithm.SHA512:  return ref Entry.SHA512Digest;
                default:                        throw new ArgumentOutOfRangeException(nameof(HashAlgorithm), "Unsupported tree hash algorithm!");
            }
        }

        private static ref POITreeNode? NodeOf(Entry              Entry,
                                               ETagHashAlgorithm  HashAlgorithm)
        {
            switch (HashAlgorithm)
            {
                case ETagHashAlgorithm.SHA256:  return ref Entry.SHA256Node;
                case ETagHashAlgorithm.SHA384:  return ref Entry.SHA384Node;
                case ETagHashAlgorithm.SHA512:  return ref Entry.SHA512Node;
                default:                        throw new ArgumentOutOfRangeException(nameof(HashAlgorithm), "Unsupported tree hash algorithm!");
            }
        }

        #endregion

    }

}
