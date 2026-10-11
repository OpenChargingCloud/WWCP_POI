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
using System.Text.Json;

using Newtonsoft.Json.Linq;

using EntityMap          = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;
using ReferenceMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, System.Collections.Immutable.ImmutableHashSet<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey>>;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Persistent immutable static infrastructure data, without runtime statuses or measurements.
    /// Applying a change set shares unchanged entities and immutable map branches.
    /// </summary>
    public sealed partial class RoamingNetworkDataSnapshot
    {

        private readonly Lazy<ImmutableArray<ETag>> contentETags;
        // The context binds the complete entity map, root and fixed content profile.
        // Share it only with snapshot-only revisions, never with a changed entity map.
        private readonly Lazy<POIChildETagCache> childETags;
        internal POIChildETagCache ChildETags => childETags.Value;
        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<ReferenceMap, ReferenceMap> tariffIndexes = new();

        #region Properties

        /// <summary>
        /// The root roaming network.
        /// </summary>
        public InfrastructureEntityKey Root { get; }

        /// <summary>
        /// Immutable entity documents indexed by their domain identity.
        /// </summary>
        public EntityMap Entities { get; }

        /// <summary>
        /// The revision of this snapshot.
        /// </summary>
        public Int64 Revision { get; }

        /// <summary>
        /// The last applied change set identifier, retained unchanged by snapshot-only history links.
        /// </summary>
        public String? AppliedChangeSetId { get; }

        /// <summary>
        /// Referenced entity keys mapped to their consumers, maintained incrementally across versions.
        /// </summary>
        public ReferenceMap References { get; }

        /// <summary>
        /// The tariff subset of the complete reverse reference index.
        /// </summary>
        public ReferenceMap TariffReferences
            => tariffIndexes.GetValue(References, references => references.Where(entry => entry.Key.Type == InfrastructureEntityType.ChargingTariff).ToImmutableDictionary());

        #endregion

        #region Constructor

        private RoamingNetworkDataSnapshot(InfrastructureEntityKey  root,
                                           EntityMap                entities,
                                           Int64                    revision,
                                           String?                  appliedChangeSetId,
                                           ReferenceMap?           references = null,
                                           Lazy<ImmutableArray<ETag>>? sharedContentETags = null,
                                           Lazy<POIChildETagCache>? sharedChildETags = null)
        {

            Root               = root;
            Entities           = entities;
            Revision           = revision;
            AppliedChangeSetId = appliedChangeSetId;
            References         = references ?? ReferenceMap.Empty;
            contentETags       = sharedContentETags ?? new(() => POIRepresentation.GetETags(this));
            childETags         = sharedChildETags ?? new(() => new POIChildETagCache());

        }

        #endregion

        /// <summary>
        /// Advance history bookkeeping while sharing unchanged static entities, indexes and content digests.
        /// </summary>
        internal RoamingNetworkDataSnapshot AdvanceSnapshotRevision()
            => new(Root, Entities, checked(Revision + 1), AppliedChangeSetId, References, contentETags, childETags);

        #region Read entities and JSON

        /// <summary>
        /// Retrieve an immutable entity. Connector identifiers require their EVSE scope.
        /// </summary>
        public InfrastructureEntitySnapshot GetEntity(InfrastructureEntityType  type,
                                                      String                    id,
                                                      String?                   parentId = null)

            => Entities[Key(type, id, parentId)];

        /// <summary>
        /// Export an entity including its nested descendants.
        /// </summary>
        public JObject GetEntityJSON(InfrastructureEntityType  type,
                                     String                    id,
                                     String?                   parentId = null)

            => ReadJSON(Export(Key(type, id, parentId)));

        /// <summary>
        /// Export the complete nested infrastructure snapshot with revision metadata.
        /// </summary>
        public JObject ToJSON()

            => POIRepresentation.AddETags(this, ReadJSON(Export(Root)));

        /// <summary>
        /// Write a frozen snapshot, optionally including its derived POI content identifiers.
        /// </summary>
        public void WriteTo(Utf8JsonWriter writer, Boolean IncludeETags = true)
        {

            ArgumentNullException.ThrowIfNull(writer);

            WriteNode(writer, Root, IncludeETags);

        }

        #endregion

        #region Capture a hierarchy

        internal static RoamingNetworkDataSnapshot Capture(JObject  document,
                                                           Int64    revision,
                                                           String?  changeSetId = null,
                                                           RoamingNetworkDataSnapshot? reuse = null)
        {

            var map        = reuse?.Entities ?? EntityMap.Empty;
            var visited    = reuse is null ? null : new HashSet<InfrastructureEntityKey>();
            var root       = Import(document, InfrastructureEntityType.RoamingNetwork, null, ref map, null, reuse: reuse, visited: visited);
            if (visited is not null)
                map = map.RemoveRange(map.Keys.Where(key => !visited.Contains(key)));

            // Domain-equal aliases can have different visible ID/scope spellings.
            // SetItem retains an existing equal dictionary key; ordinary capture is
            // required when that retained key differs from the newly imported record.
            if (reuse is not null && map.Any(entry => entry.Key.Id != entry.Value.Key.Id || entry.Key.Scope != entry.Value.Key.Scope ||
                reuse.Entities.TryGetValue(entry.Key, out var previous) &&
                (previous.Key.Id != entry.Value.Key.Id || previous.Key.Scope != entry.Value.Key.Scope)))
                return Capture(document, revision, changeSetId);

            var references = reuse?.References ?? ReferenceMap.Empty;
            if (reuse is not null)
                foreach (var (key, previous) in reuse.Entities)
                    if (!map.TryGetValue(key, out var current) || !ReferenceEquals(previous, current))
                        references = RemoveReferences(previous, references);

            // Every current entity still passes the ordinary reference and scope checks.
            foreach (var entity in map.Values)
                references = AddReferences(entity, map, references);

            if (reuse is not null && references.Count == reuse.References.Count &&
                references.All(entry => reuse.References.TryGetValue(entry.Key, out var existing) && existing.SetEquals(entry.Value)))
                references = reuse.References;

            var sameMap = reuse is not null && ReferenceEquals(map, reuse.Entities) && root == reuse.Root;
            return new RoamingNetworkDataSnapshot(root, map, revision, changeSetId, references,
                                                 sameMap ? reuse!.contentETags : null,
                                                 sameMap ? reuse!.childETags : null);

        }

        #endregion

    }

}
