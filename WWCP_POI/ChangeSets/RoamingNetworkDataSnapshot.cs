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
        /// The change set that produced this version, if one has been applied.
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
            => References.Where(entry => entry.Key.Type == InfrastructureEntityType.ChargingTariff).ToImmutableDictionary();

        #endregion

        #region Constructor

        private RoamingNetworkDataSnapshot(InfrastructureEntityKey  root,
                                           EntityMap                entities,
                                           Int64                    revision,
                                           String?                  appliedChangeSetId,
                                           ReferenceMap?           references = null)
        {

            Root               = root;
            Entities           = entities;
            Revision           = revision;
            AppliedChangeSetId = appliedChangeSetId;
            References         = references ?? ReferenceMap.Empty;
            contentETags       = new(() => POIRepresentation.GetETags(this));

        }

        #endregion

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
                                                           String?  changeSetId = null)
        {

            var map        = EntityMap.Empty;
            var root       = Import(document, InfrastructureEntityType.RoamingNetwork, null, ref map, null);
            var references = ReferenceMap.Empty;

            foreach (var entity in map.Values)
                references = AddReferences(entity, map, references);

            return new RoamingNetworkDataSnapshot(root, map, revision, changeSetId, references);

        }

        #endregion

    }

}
