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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The hash tree of one version of a roaming network with one hash algorithm (SPECIFICATION.md, section 8.2), with what
    /// proofs of its entities need: the skeletons of the roaming network and of its parties, and the digests of the
    /// children of their child arrays. The charging pools, charging stations and EVSEs below are calculated again for a
    /// proof: a pool is small, and the operator keeps the digests of all of them.
    /// </summary>
    public sealed class RoamingNetworkEffectiveTree
    {

        #region Data

        private readonly RoamingNetworkEffectiveMembers members;

        #endregion

        #region Properties

        /// <summary>
        /// The version.
        /// </summary>
        public RoamingNetwork     RoamingNetwork    { get; }

        /// <summary>
        /// The hash algorithm of the tree.
        /// </summary>
        public ETagHashAlgorithm  HashAlgorithm     { get; }

        /// <summary>
        /// The node of the roaming network, its root, with the nodes of its children.
        /// </summary>
        public POITreeNode        Root              { get; }

        /// <summary>
        /// The effectiveTree ETag of the version.
        /// </summary>
        public ETag               ETag
            => new (ETagFormat.EffectiveTree, HashAlgorithm, Root.Digest);

        #endregion

        #region Constructor(s)

        private RoamingNetworkEffectiveTree(RoamingNetwork                   RoamingNetwork,
                                            ETagHashAlgorithm                HashAlgorithm,
                                            POITreeNode                      Root,
                                            RoamingNetworkEffectiveMembers   Members)
        {

            this.RoamingNetwork  = RoamingNetwork;
            this.HashAlgorithm   = HashAlgorithm;
            this.Root            = Root;
            this.members         = Members;

        }

        #endregion


        #region (static) Of(RoamingNetwork, HashAlgorithm)

        /// <summary>
        /// The hash tree of the given version with the given hash algorithm, from the digests its entities keep: only what
        /// the versions before did not hash yet is hashed.
        /// </summary>
        /// <param name="RoamingNetwork">A version of a roaming network.</param>
        /// <param name="HashAlgorithm">The hash algorithm of the tree.</param>
        public static RoamingNetworkEffectiveTree Of(RoamingNetwork     RoamingNetwork,
                                                     ETagHashAlgorithm  HashAlgorithm)
        {

            return new RoamingNetworkEffectiveTree(RoamingNetwork,
                                                   HashAlgorithm,
                                                   POIEffectiveDigests.Root(RoamingNetwork, HashAlgorithm),
                                                   RoamingNetwork.EffectiveMembersOfTheContent());

        }

        #endregion


        #region TryGetChargingStationProof(ChargingStationId, out Proof, out ErrorResponse, CommitId = null, Revision = null, Attestations = null)

        /// <summary>
        /// Try to make the proof of the given charging station (SPECIFICATION.md, section 8.4): the roaming network pruned,
        /// giving completely its own properties with its operator and keys, the skeletons of the station's operator and
        /// pool, the station with its EVSEs and their energy meters, the charging tariffs its EVSEs take, the manufacturers
        /// of the station and of its energy meters, the transparency software releases and certificates these name, the
        /// parking spaces the station names and the grid connection point of its pool.
        /// </summary>
        /// <param name="ChargingStationId">The identification of a charging station.</param>
        /// <param name="Proof">The proof.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CommitId">The commit of the version, if known.</param>
        /// <param name="Revision">The revision of the commit, if known.</param>
        /// <param name="Attestations">The attestations of the version to embed.</param>
        public Boolean TryGetChargingStationProof(ChargingStation_Id                                ChargingStationId,
                                                  [NotNullWhen(true)]  out RoamingNetworkProof?     Proof,
                                                  [NotNullWhen(false)] out String?                  ErrorResponse,
                                                  RoamingNetworkCommit_Id?                          CommitId       = null,
                                                  UInt64?                                           Revision       = null,
                                                  IEnumerable<RoamingNetworkAttestation>?           Attestations   = null)
        {

            Proof = null;

            #region The charging station, its charging pool and its operator: the one its identification names first

            ChargingStationOperator? chargingStationOperator = null;

            IEnumerable<ChargingStationOperator> candidates = RoamingNetwork.TryGetChargingStationOperatorById(ChargingStationId.OperatorId, out var namedOperator)
                                                                  ? [ namedOperator, .. RoamingNetwork.ChargingStationOperators.Where(other => other.Id != namedOperator.Id) ]
                                                                  : RoamingNetwork.ChargingStationOperators;

            foreach (var candidate in candidates)
            {
                if (candidate.TryGetChargingStation(ChargingStationId, out _))
                {
                    chargingStationOperator = candidate;
                    break;
                }
            }

            if (chargingStationOperator is null)
            {
                ErrorResponse = $"The charging station '{ChargingStationId}' is unknown in the roaming network '{RoamingNetwork.Id}'!";
                return false;
            }

            var chargingPool     = chargingStationOperator.ChargingPools.First(pool => pool.ChargingStations.Contains(ChargingStationId));
            var effectivePool    = members.ChargingPools.TryGetValue(chargingStationOperator.Id, out var effectivePools) &&
                                   effectivePools.TryGetValue(chargingPool.Id, out var effective)
                                       ? effective
                                       : chargingPool;

            var operatorIndex    = RoamingNetwork.ChargingStationOperators.Ids.IndexOf(chargingStationOperator.Id);
            var poolIndex        = chargingStationOperator.ChargingPools.Ids.IndexOf(chargingPool.Id);
            var stationIndex     = effectivePool.ChargingStations.Ids.IndexOf(ChargingStationId);
            var chargingStation  = effectivePool.ChargingStations[stationIndex];

            var operatorNode     = Root.Arrays[POI.RoamingNetwork.PropertyKeys.ChargingStationOperators].Children![operatorIndex];

            #endregion

            #region The charging pool: its skeleton, with the charging station complete

            var poolJSON = effectivePool.ToJSON(Embedded: true);

            if (!POIEffectiveTree.TryDigest(poolJSON, ChargingStationOperator.PropertyKeys.ChargingPools, HashAlgorithm, 1, ChargingStationOperator.PropertyKeys.ChargingPools, out var poolDigest, out var poolNode, out ErrorResponse))
                return false;

            if (!poolDigest.AsSpan().SequenceEqual(operatorNode.Arrays[ChargingStationOperator.PropertyKeys.ChargingPools].LeafHashes[poolIndex]))
            {
                ErrorResponse = $"The charging pool '{chargingPool.Id}' is not the one of the tree!";
                return false;
            }

            poolJSON[ChargingPool.PropertyKeys.ChargingStations] = Partial(poolNode!.Arrays[ChargingPool.PropertyKeys.ChargingStations],
                                                                           [ (stationIndex, chargingStation.ToJSON(Embedded: true)) ]);

            #endregion

            #region The operator: its skeleton, with the charging pool, the charging tariffs of the EVSEs and its parking spaces the station names

            var operatorJSON = (JObject) operatorNode.Skeleton.DeepClone();

            operatorJSON[ChargingStationOperator.PropertyKeys.ChargingPools] = Partial(operatorNode.Arrays[ChargingStationOperator.PropertyKeys.ChargingPools],
                                                                                       [ (poolIndex, poolJSON) ]);

            var effectiveTariffs = members.ChargingTariffs.TryGetValue(chargingStationOperator.Id, out var tariffs) ? tariffs : null;

            SetPartial(operatorJSON,
                       operatorNode,
                       ChargingStationOperator.PropertyKeys.ChargingTariffs,
                       chargingStation.EVSEs.SelectMany(evse => evse.ChargingTariffIds).Distinct().
                           Select(tariffId => chargingStationOperator.ChargingTariffs.Ids.IndexOf(tariffId)).
                           Where (index    => index >= 0).
                           Select(index    => (index, (effectiveTariffs is not null && effectiveTariffs.TryGetValue(chargingStationOperator.ChargingTariffs[index].Id, out var effectiveTariff)
                                                            ? effectiveTariff
                                                            : chargingStationOperator.ChargingTariffs[index]).ToJSON(Embedded: true))));

            SetPartial(operatorJSON,
                       operatorNode,
                       ChargingStationOperator.PropertyKeys.ParkingSpaces,
                       chargingStation.ParkingSpaceIds.
                           Select(parkingSpaceId => chargingStationOperator.ParkingSpaces.Ids.IndexOf(parkingSpaceId)).
                           Where (index          => index >= 0).
                           Select(index          => (index, chargingStationOperator.ParkingSpaces[index].ToJSON(Embedded: true))));

            #endregion

            #region The roaming network: its skeleton, with the operator, and what the station and its energy meters name

            var networkJSON = (JObject) Root.Skeleton.DeepClone();

            networkJSON[POI.RoamingNetwork.PropertyKeys.ChargingStationOperators] = Partial(Root.Arrays[POI.RoamingNetwork.PropertyKeys.ChargingStationOperators],
                                                              [ (operatorIndex, operatorJSON) ]);

            var energyMeters = chargingStation.AllEnergyMeters.ToArray();

            SetPartialOfNetwork(networkJSON,
                                POI.RoamingNetwork.PropertyKeys.ChargingStationManufacturers,
                                RoamingNetwork.ChargingStationManufacturers.Ids,
                                chargingStation.ManufacturerId.HasValue ? [ chargingStation.ManufacturerId.Value ] : []);

            SetPartialOfNetwork(networkJSON,
                                POI.RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,
                                RoamingNetwork.EnergyMeterManufacturers.Ids,
                                energyMeters.Select(energyMeter => energyMeter.ManufacturerId).OfType<EnergyMeterManufacturer_Id>());

            SetPartialOfNetwork(networkJSON,
                                POI.RoamingNetwork.PropertyKeys.TransparencySoftware,
                                RoamingNetwork.TransparencySoftware.Ids,
                                energyMeters.SelectMany(energyMeter => energyMeter.TransparencySoftwareStatus).Select(assignment => assignment.TransparencySoftwareId));

            SetPartialOfNetwork(networkJSON,
                                POI.RoamingNetwork.PropertyKeys.TransparencySoftwareCertificates,
                                RoamingNetwork.TransparencySoftwareCertificates.Ids,
                                energyMeters.SelectMany(energyMeter => energyMeter.TransparencySoftwareStatus).Select(assignment => assignment.CertificateId).OfType<TransparencySoftwareCertificate_Id>());

            #region The grid connection point of the charging pool

            if (effectivePool.GridConnectionPointId.HasValue &&
                RoamingNetwork.TryGetGridConnectionPoint(effectivePool.GridConnectionPointId.Value, out var gridOperator, out var gridConnectionPoint))
            {

                var gridOperatorIndex  = RoamingNetwork.GridOperators.Ids.IndexOf(gridOperator.Id);
                var gridOperatorNode   = Root.Arrays[POI.RoamingNetwork.PropertyKeys.GridOperators].Children![gridOperatorIndex];
                var gridOperatorJSON   = (JObject) gridOperatorNode.Skeleton.DeepClone();

                SetPartial(gridOperatorJSON,
                           gridOperatorNode,
                           GridOperator.PropertyKeys.GridConnectionPoints,
                           [ (gridOperator.GridConnectionPoints.Ids.IndexOf(gridConnectionPoint.Id), gridOperator.EffectiveGridConnectionPoint(gridConnectionPoint).ToJSON(Embedded: true)) ]);

                networkJSON[POI.RoamingNetwork.PropertyKeys.GridOperators] = Partial(Root.Arrays[POI.RoamingNetwork.PropertyKeys.GridOperators],
                                                       [ (gridOperatorIndex, gridOperatorJSON) ]);

            }

            #endregion

            #region The parking spaces of parking operators the station names

            var parkingOperators = new List<(Int32, JObject)>();

            for (var parkingOperatorIndex = 0; parkingOperatorIndex < RoamingNetwork.ParkingOperators.Count; parkingOperatorIndex++)
            {

                var parkingOperator  = RoamingNetwork.ParkingOperators[parkingOperatorIndex];
                var spaceIndices     = chargingStation.ParkingSpaceIds.
                                           Select(parkingSpaceId => parkingOperator.ParkingSpaces.Ids.IndexOf(parkingSpaceId)).
                                           Where (index          => index >= 0).
                                           ToArray();

                if (spaceIndices.Length == 0)
                    continue;

                var effectiveSpaces       = members.ParkingSpaces.TryGetValue(parkingOperator.Id, out var spaces) ? spaces : null;
                var parkingOperatorNode   = Root.Arrays[POI.RoamingNetwork.PropertyKeys.ParkingOperators].Children![parkingOperatorIndex];
                var parkingOperatorJSON   = (JObject) parkingOperatorNode.Skeleton.DeepClone();

                SetPartial(parkingOperatorJSON,
                           parkingOperatorNode,
                           ParkingOperator.PropertyKeys.ParkingSpaces,
                           spaceIndices.Select(index => (index, (effectiveSpaces is not null && effectiveSpaces.TryGetValue(parkingOperator.ParkingSpaces[index].Id, out var effectiveSpace)
                                                                     ? effectiveSpace
                                                                     : parkingOperator.ParkingSpaces[index]).ToJSON(Embedded: true))));

                parkingOperators.Add((parkingOperatorIndex, parkingOperatorJSON));

            }

            if (parkingOperators.Count > 0)
                networkJSON[POI.RoamingNetwork.PropertyKeys.ParkingOperators] = Partial(Root.Arrays[POI.RoamingNetwork.PropertyKeys.ParkingOperators], parkingOperators);

            #endregion

            #endregion

            #region It leads to the root, as it must

            if (!POIEffectiveTree.TryDigest((JObject) networkJSON.DeepClone(), POIEffectiveTree.RoamingNetworkKind, HashAlgorithm, out var digest, out ErrorResponse))
                return false;

            if (!digest.AsSpan().SequenceEqual(Root.Digest))
            {
                ErrorResponse = $"The proof of the charging station '{ChargingStationId}' does not lead to the root of the tree!";
                return false;
            }

            #endregion

            Proof          = new RoamingNetworkProof(HashAlgorithm,
                                                     networkJSON,
                                                     CommitId,
                                                     Revision,
                                                     [ .. (Attestations ?? []).Where(attestation => attestation.ETag.Equals(ETag)) ]);

            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region TryGetChargingStationProof(EVSEId,            out Proof, out ErrorResponse, CommitId = null, Revision = null, Attestations = null)

        /// <summary>
        /// Try to make the proof of the charging station of the given EVSE.
        /// </summary>
        /// <param name="EVSEId">The identification of an EVSE.</param>
        /// <param name="Proof">The proof.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="CommitId">The commit of the version, if known.</param>
        /// <param name="Revision">The revision of the commit, if known.</param>
        /// <param name="Attestations">The attestations of the version to embed.</param>
        public Boolean TryGetChargingStationProof(EVSE_Id                                           EVSEId,
                                                  [NotNullWhen(true)]  out RoamingNetworkProof?     Proof,
                                                  [NotNullWhen(false)] out String?                  ErrorResponse,
                                                  RoamingNetworkCommit_Id?                          CommitId       = null,
                                                  UInt64?                                           Revision       = null,
                                                  IEnumerable<RoamingNetworkAttestation>?           Attestations   = null)
        {

            foreach (var chargingStationOperator in RoamingNetwork.ChargingStationOperators)
                if (chargingStationOperator.TryGetEVSE(EVSEId, out _))
                    foreach (var chargingStation in chargingStationOperator.ChargingStations)
                        if (chargingStation.EVSEs.Contains(EVSEId))
                            return TryGetChargingStationProof(chargingStation.Id, out Proof, out ErrorResponse, CommitId, Revision, Attestations);

            Proof          = null;
            ErrorResponse  = $"The EVSE '{EVSEId}' is unknown in the roaming network '{RoamingNetwork.Id}'!";
            return false;

        }

        #endregion


        #region (private) Partial(Array, Leaves)

        /// <summary>
        /// The given child array in its partial form: the given children, at their indices, with their audit paths.
        /// </summary>
        private JObject Partial(POITreeArray                         Array,
                                IEnumerable<(Int32 Index, JObject Entity)>  Leaves)

            => new (
                   new JProperty(POIEffectiveTree.SizeProperty,    (UInt64) Array.LeafHashes.Count),
                   new JProperty(POIEffectiveTree.LeavesProperty,  new JArray(
                       Leaves.OrderBy(leaf => leaf.Index).
                              Select (leaf => new JObject(
                                                  new JProperty(POIEffectiveTree.IndexProperty,      leaf.Index),
                                                  new JProperty(POIEffectiveTree.AuditPathProperty,  new JArray(POIMerkleTree.AuditPathOfLeafHashes(HashAlgorithm, Array.LeafHashes, leaf.Index).
                                                                                                                                 Select(hash => Convert.ToHexStringLower(hash)))),
                                                  new JProperty(POIEffectiveTree.EntityProperty,     leaf.Entity)
                                              ))
                   ))
               );

        #endregion

        #region (private) SetPartial(JSON, Node, ChildArray, Leaves)

        /// <summary>
        /// Give the given children of the given child array of an entity, where there are any; it stays pruned otherwise.
        /// </summary>
        private void SetPartial(JObject                                     JSON,
                                POITreeNode                                 Node,
                                String                                      ChildArray,
                                IEnumerable<(Int32 Index, JObject Entity)>  Leaves)
        {

            var leaves = Leaves.DistinctBy(leaf => leaf.Index).ToArray();

            if (leaves.Length > 0 && Node.Arrays.TryGetValue(ChildArray, out var array))
                JSON[ChildArray] = Partial(array, leaves);

        }

        #endregion

        #region (private) SetPartialOfNetwork(JSON, ChildArray, Ids, Named)

        /// <summary>
        /// Give the children of the given child array of the roaming network the given identifications name, as its tree
        /// keeps them.
        /// </summary>
        private void SetPartialOfNetwork<TId>(JObject                       JSON,
                                              String                        ChildArray,
                                              ImmutableArray<TId>           Ids,
                                              IEnumerable<TId>              Named)
        {

            if (!Root.Arrays.TryGetValue(ChildArray, out var array) || array.Children is null)
                return;

            SetPartial(JSON,
                       Root,
                       ChildArray,
                       Named.Distinct().
                             Select(id    => Ids.IndexOf(id)).
                             Where (index => index >= 0).
                             Select(index => (index, (JObject) array.Children[index].Skeleton.DeepClone())));

        }

        #endregion

    }

}
