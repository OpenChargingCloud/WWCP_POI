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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The hash tree along the entities of the effective content of a roaming network (SPECIFICATION.md, section 8.2), and
    /// the forms a proof gives its child arrays in (section 8.4): complete, pruned or partial.
    /// </summary>
    /// <remarks>
    /// The skeleton of an entity is its JSON as the effective content embeds it, with every child array replaced by
    /// { "@merkleRoot": "&lt;MTH of its children's skeletons, lowercase hex&gt;", "@size": n }; its digest is
    /// H(0x00 || canonical JSON of its skeleton), the leaf hash it is in the tree of its parent. The digest of the roaming
    /// network is the root, its effectiveTree ETag. Kinds of entities are named by the child array embedding them, the
    /// roaming network by "".
    /// </remarks>
    public static class POIEffectiveTree
    {

        #region Data

        /// <summary>
        /// The root of the tree of a pruned child array.
        /// </summary>
        public const String MerkleRootProperty  = "@merkleRoot";

        /// <summary>
        /// The number of children of a pruned or partial child array.
        /// </summary>
        public const String SizeProperty        = "@size";

        /// <summary>
        /// The children a partial child array gives, with their audit paths.
        /// </summary>
        public const String LeavesProperty      = "@leaves";

        /// <summary>
        /// The index of a child of a partial child array.
        /// </summary>
        public const String IndexProperty       = "@index";

        /// <summary>
        /// The audit path of a child of a partial child array.
        /// </summary>
        public const String AuditPathProperty   = "@auditPath";

        /// <summary>
        /// The child itself of a partial child array, in any form.
        /// </summary>
        public const String EntityProperty      = "@entity";

        /// <summary>
        /// The kind of the roaming network.
        /// </summary>
        public const String RoamingNetworkKind  = "";

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization of pruned and partial child arrays.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of the identification of an entity.
            /// </summary>
            public const String  Id           = "@id";

            /// <summary>
            /// The property of the root of the tree of a pruned child array.
            /// </summary>
            public const String  MerkleRoot   = MerkleRootProperty;

            /// <summary>
            /// The property of the number of children of a pruned or partial child array.
            /// </summary>
            public const String  Size         = SizeProperty;

            /// <summary>
            /// The property of the children a partial child array gives.
            /// </summary>
            public const String  Leaves       = LeavesProperty;

            /// <summary>
            /// The property of the index of a child of a partial child array.
            /// </summary>
            public const String  Index        = IndexProperty;

            /// <summary>
            /// The property of the audit path of a child of a partial child array.
            /// </summary>
            public const String  AuditPath    = AuditPathProperty;

            /// <summary>
            /// The property of the child itself of a partial child array.
            /// </summary>
            public const String  Entity       = EntityProperty;

        }

        #endregion

        /// <summary>
        /// The child arrays of the kinds of entities having some, by their kind: the arrays of entities change sets add
        /// and remove. Everything else is part of the entity embedding it.
        /// </summary>
        public static readonly ImmutableDictionary<String, ImmutableArray<String>> ChildArrays = new Dictionary<String, ImmutableArray<String>> {

            { RoamingNetworkKind,
              [ RoamingNetwork.PropertyKeys.ChargingStationOperators,
                RoamingNetwork.PropertyKeys.ChargingStationManufacturers,
                RoamingNetwork.PropertyKeys.EMobilityProviders,
                RoamingNetwork.PropertyKeys.GridOperators,
                RoamingNetwork.PropertyKeys.ParkingOperators,
                RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,
                RoamingNetwork.PropertyKeys.TransparencySoftwareManufacturers,
                RoamingNetwork.PropertyKeys.ConformityAssessmentBodies ] },

            { RoamingNetwork.PropertyKeys.ChargingStationManufacturers,
              [ ChargingStationManufacturer.PropertyKeys.Models ] },

            { RoamingNetwork.PropertyKeys.EnergyMeterManufacturers,
              [ EnergyMeterManufacturer.PropertyKeys.Models ] },

            { RoamingNetwork.PropertyKeys.TransparencySoftwareManufacturers,
              [ TransparencySoftwareManufacturer.PropertyKeys.TransparencySoftware ] },

            { RoamingNetwork.PropertyKeys.ConformityAssessmentBodies,
              [ ConformityAssessmentBody.PropertyKeys.Certificates ] },

            { RoamingNetwork.PropertyKeys.ChargingStationOperators,
              [ ChargingStationOperator.PropertyKeys.ChargingPools,
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

            { ChargingStationOperator.PropertyKeys.ChargingPools,
              [ ChargingPool.PropertyKeys.ChargingStations ] },

            { ChargingPool.PropertyKeys.ChargingStations,
              [ ChargingStation.PropertyKeys.EVSEs ] },

            { RoamingNetwork.PropertyKeys.EMobilityProviders,
              [ EMobilityProvider.PropertyKeys.EVSEGroups,
                EMobilityProvider.PropertyKeys.ChargingStationGroups,
                EMobilityProvider.PropertyKeys.ChargingPoolGroups,
                EMobilityProvider.PropertyKeys.ChargingTariffOffers ] },

            { RoamingNetwork.PropertyKeys.GridOperators,
              [ GridOperator.PropertyKeys.GridConnectionPoints,
                GridOperator.PropertyKeys.EnergyMeterTemplates,
                GridOperator.PropertyKeys.EnergyMeterGroups ] },

            { RoamingNetwork.PropertyKeys.ParkingOperators,
              [ ParkingOperator.PropertyKeys.ParkingGarages,
                ParkingOperator.PropertyKeys.ParkingSpaces,
                ParkingOperator.PropertyKeys.ParkingSensors,
                ParkingOperator.PropertyKeys.ParkingSpaceTemplates,
                ParkingOperator.PropertyKeys.ParkingSpaceGroups,
                ParkingOperator.PropertyKeys.ParkingProducts ] },

        }.ToImmutableDictionary();

        #endregion


        #region ChildArraysOf(Kind)

        /// <summary>
        /// The child arrays of the given kind of entities, none for most.
        /// </summary>
        /// <param name="Kind">The kind of entities: the child array embedding them, "" for the roaming network.</param>
        public static ImmutableArray<String> ChildArraysOf(String Kind)

            => ChildArrays.TryGetValue(Kind, out var childArrays)
                   ? childArrays
                   : [];

        #endregion

        #region Pruned(Root, Size)

        /// <summary>
        /// A child array in its pruned form: { "@merkleRoot": "&lt;hex&gt;", "@size": n }.
        /// </summary>
        /// <param name="Root">The root of the tree of its children.</param>
        /// <param name="Size">The number of its children.</param>
        public static JObject Pruned(Byte[]  Root,
                                     UInt64  Size)

            => new (
                   new JProperty(MerkleRootProperty,  Convert.ToHexStringLower(Root)),
                   new JProperty(SizeProperty,        Size)
               );

        #endregion

        #region TryDigest(Entity, Kind, HashAlgorithm, out Digest, out ErrorResponse)

        /// <summary>
        /// Try to calculate the digest of the given entity, whose child arrays may be given in any form of a proof
        /// (SPECIFICATION.md, section 8.4): complete, pruned or partial. The given JSON becomes its skeleton: every child
        /// array is replaced by its pruned form. Clone it before, to keep it.
        /// </summary>
        /// <param name="Entity">The entity, e.g. a roaming network.</param>
        /// <param name="Kind">The kind of the entity: the child array embedding it, "" for the roaming network.</param>
        /// <param name="HashAlgorithm">The hash algorithm of the tree.</param>
        /// <param name="Digest">Its digest, H(0x00 || canonical JSON of its skeleton).</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryDigest(JObject                           Entity,
                                        String                            Kind,
                                        ETagHashAlgorithm                 HashAlgorithm,
                                        out Byte[]                        Digest,
                                        [NotNullWhen(false)] out String?  ErrorResponse)

            => TryDigest(Entity,
                         Kind,
                         HashAlgorithm,
                         0,
                         Kind.Length == 0 ? "roamingNetwork" : Kind,
                         out Digest,
                         out _,
                         out ErrorResponse);

        #endregion

        #region (internal) TryDigest(Entity, Kind, HashAlgorithm, KeepDepth, Path, out Digest, out Node, out ErrorResponse)

        /// <summary>
        /// Try to calculate the digest of the given entity, and keep its node, with the leaf hashes of its complete child
        /// arrays, when the given depth is above 0; the nodes of its children as well when it is above 1, and so on.
        /// </summary>
        internal static Boolean TryDigest(JObject                           Entity,
                                          String                            Kind,
                                          ETagHashAlgorithm                 HashAlgorithm,
                                          Int32                             KeepDepth,
                                          String                            Path,
                                          out Byte[]                        Digest,
                                          out POITreeNode?                  Node,
                                          [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Digest  = [];
            Node    = null;

            var arrays = KeepDepth > 0 ? new Dictionary<String, POITreeArray>() : null;

            foreach (var childArray in ChildArraysOf(Kind))
            {

                // An absent child array stays absent.
                if (!Entity.TryGetValue(childArray, out var value))
                    continue;

                var     path  = $"{Path}.{childArray}";
                Byte[]  root;
                UInt64  size;

                switch (value)
                {

                    #region Complete: the array of its children, each in any form

                    case JArray children:
                    {

                        var leafHashes  = new Byte[children.Count][];
                        var nodes       = KeepDepth > 1 ? new List<POITreeNode>(children.Count) : null;

                        for (var index = 0; index < children.Count; index++)
                        {

                            if (children[index] is not JObject child)
                            {
                                ErrorResponse = $"{path}[{index}] must be a JSON object!";
                                return false;
                            }

                            if (!TryDigest(child, childArray, HashAlgorithm, KeepDepth - 1, $"{path}[{index}]", out leafHashes[index], out var node, out ErrorResponse))
                                return false;

                            if (node is not null)
                                nodes?.Add(node);

                        }

                        root  = POIMerkleTree.RootOfLeafHashes(HashAlgorithm, leafHashes);
                        size  = (UInt64) children.Count;

                        arrays?.Add(childArray, new POITreeArray(childArray, leafHashes, root, nodes));

                        break;

                    }

                    #endregion

                    #region Partial: some children with their audit paths

                    case JObject partialForm when partialForm.ContainsKey(LeavesProperty):
                    {

                        if (partialForm.Count != 2 || !TryParseSize(partialForm, out size) || size == 0 ||
                            partialForm[LeavesProperty] is not JArray leaves || leaves.Count == 0)
                        {
                            ErrorResponse = $"{path} must be {{ \"{SizeProperty}\": n, \"{LeavesProperty}\": [ … ] }}, with at least one leaf!";
                            return false;
                        }

                        Byte[]? commonRoot  = null;
                        Int64   lastIndex   = -1;

                        for (var position = 0; position < leaves.Count; position++)
                        {

                            var leafPath = $"{path}.{LeavesProperty}[{position}]";

                            if (leaves[position] is not JObject leaf ||
                                leaf.Count != 3 ||
                                leaf[IndexProperty]     is not JValue { Type: JTokenType.Integer } indexJSON ||
                                leaf[AuditPathProperty] is not JArray auditPathJSON ||
                                leaf[EntityProperty]    is not JObject child)
                            {
                                ErrorResponse = $"{leafPath} must be {{ \"{IndexProperty}\": i, \"{AuditPathProperty}\": [ … ], \"{EntityProperty}\": {{ … }} }}!";
                                return false;
                            }

                            var index = indexJSON.Value<Decimal>();

                            // In ascending order, each child once.
                            if (index <= lastIndex || index >= size)
                            {
                                ErrorResponse = $"{leafPath}: its index must be below {size} and above the one before!";
                                return false;
                            }

                            lastIndex = (Int64) index;

                            if (!TryParseHashes(auditPathJSON, HashAlgorithm, out var auditPath))
                            {
                                ErrorResponse = $"{leafPath}: its audit path must be an array of lowercase hexadecimal digests of {HashAlgorithm.ToText()}!";
                                return false;
                            }

                            if (!TryDigest(child, childArray, HashAlgorithm, 0, $"{leafPath}.{EntityProperty}", out var childDigest, out _, out ErrorResponse))
                                return false;

                            if (!POIMerkleTree.TryCalculateRootOfLeafHash(HashAlgorithm, childDigest, (UInt64) index, size, auditPath, out var leafRoot))
                            {
                                ErrorResponse = $"{leafPath}: its audit path does not fit its index and the size!";
                                return false;
                            }

                            if (commonRoot is not null && !commonRoot.AsSpan().SequenceEqual(leafRoot))
                            {
                                ErrorResponse = $"{leafPath}: it leads to another root than the leaves before!";
                                return false;
                            }

                            commonRoot = leafRoot;

                        }

                        root = commonRoot!;

                        break;

                    }

                    #endregion

                    #region Pruned: the root and the size, taken as they are

                    case JObject pruned:
                    {

                        if (pruned.Count != 2 || !TryParseSize(pruned, out size) ||
                            pruned[MerkleRootProperty] is not JValue { Type: JTokenType.String } rootJSON ||
                            !TryParseHash(rootJSON.Value<String>(), HashAlgorithm, out root))
                        {
                            ErrorResponse = $"{path} must be {{ \"{MerkleRootProperty}\": \"<{HashAlgorithm.ToText()} in lowercase hex>\", \"{SizeProperty}\": n }}!";
                            return false;
                        }

                        if (size == 0 && !root.AsSpan().SequenceEqual(POIMerkleTree.Hash(HashAlgorithm, [])))
                        {
                            ErrorResponse = $"{path}: the root of no children is the hash of nothing!";
                            return false;
                        }

                        break;

                    }

                    #endregion

                    default:
                        ErrorResponse = $"{path} must be an array of its children, pruned or partial!";
                        return false;

                }

                Entity[childArray] = Pruned(root, size);

            }

            Digest = POIMerkleTree.LeafHash(HashAlgorithm, CanonicalJSON.ToUTF8Bytes(Entity));

            if (arrays is not null)
                Node = new POITreeNode(Kind,
                                       Entity[PropertyKeys.Id]?.Type == JTokenType.String ? Entity[PropertyKeys.Id]!.Value<String>() : null,
                                       Digest,
                                       Entity,
                                       arrays);

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (private static) TryParseSize  (JSON, out Size)

        private static Boolean TryParseSize(JObject     JSON,
                                            out UInt64  Size)
        {

            Size = 0;

            if (JSON[SizeProperty] is not JValue { Type: JTokenType.Integer } size ||
                size.Value<Decimal>() < 0 ||
                size.Value<Decimal>() > Int64.MaxValue)
            {
                return false;
            }

            Size = size.Value<UInt64>();
            return true;

        }

        #endregion

        #region (private static) TryParseHash  (Text, HashAlgorithm, out Hash)

        private static Boolean TryParseHash(String?            Text,
                                            ETagHashAlgorithm  HashAlgorithm,
                                            out Byte[]         Hash)
        {

            Hash = [];

            if (Text is null ||
                Text.Length != 2 * ETag.DigestLength(HashAlgorithm) ||
                Text.AsSpan().IndexOfAnyExcept("0123456789abcdef") >= 0)
            {
                return false;
            }

            Hash = Convert.FromHexString(Text);
            return true;

        }

        #endregion

        #region (private static) TryParseHashes(JSON, HashAlgorithm, out Hashes)

        private static Boolean TryParseHashes(JArray                 JSON,
                                              ETagHashAlgorithm      HashAlgorithm,
                                              out List<Byte[]>       Hashes)
        {

            Hashes = new (JSON.Count);

            foreach (var item in JSON)
            {

                if (item.Type != JTokenType.String || !TryParseHash(item.Value<String>(), HashAlgorithm, out var hash))
                    return false;

                Hashes.Add(hash);

            }

            return true;

        }

        #endregion

    }


    /// <summary>
    /// An entity of an effective tree, kept for proofs: its digest, its skeleton and the leaf hashes of its child arrays.
    /// </summary>
    /// <param name="Kind">The kind of the entity: the child array embedding it, "" for the roaming network.</param>
    /// <param name="Id">The identification of the entity, its @id.</param>
    /// <param name="Digest">Its digest.</param>
    /// <param name="Skeleton">Its skeleton: its JSON with every child array pruned.</param>
    /// <param name="Arrays">Its child arrays, by their names.</param>
    public sealed record POITreeNode(String                                     Kind,
                                     String?                                    Id,
                                     Byte[]                                     Digest,
                                     JObject                                    Skeleton,
                                     IReadOnlyDictionary<String, POITreeArray>  Arrays);


    /// <summary>
    /// A child array of an entity of an effective tree, kept for proofs.
    /// </summary>
    /// <param name="Name">The name of the child array.</param>
    /// <param name="LeafHashes">The digests of its children, in their order.</param>
    /// <param name="Root">The Merkle tree hash of its children.</param>
    /// <param name="Children">The nodes of its children, where they are kept.</param>
    public sealed record POITreeArray(String                       Name,
                                      IReadOnlyList<Byte[]>        LeafHashes,
                                      Byte[]                       Root,
                                      IReadOnlyList<POITreeNode>?  Children);

}
