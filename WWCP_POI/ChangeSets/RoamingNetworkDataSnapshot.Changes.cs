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
using System.Globalization;
using System.Text.Json;

using Newtonsoft.Json.Linq;

using EntityMap          = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;
using ReferenceMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, System.Collections.Immutable.ImmutableHashSet<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey>>;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class RoamingNetworkDataSnapshot
    {

        #region ApplyChangeSet

        /// <summary>
        /// Apply the ordered operations atomically, sharing unchanged entities with this version.
        /// </summary>
        /// <param name="changeSet">The operations, expected base revision and before/after content identifiers.</param>
        /// <param name="verifySignature">An optional verifier, required for signed change sets.</param>
        public RoamingNetworkDataSnapshot ApplyChangeSet(RoamingNetworkChangeSet                  changeSet,
                                                         Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifySignature = null)
        {

            ArgumentNullException.ThrowIfNull(changeSet);

            ValidateChangeSet(changeSet);
            VerifyChangeSetSignatures(changeSet, verifySignature);

            var result = ApplyOperations(changeSet);
            ExpectETags(changeSet, "AfterETags", changeSet.AfterETags, result);
            return result;

        }

        /// <summary>
        /// Prepare an unsigned batch by calculating its source and resulting content identifiers.
        /// All operations are validated on local immutable maps; no version is published.
        /// </summary>
        public RoamingNetworkChangeSet CreateChangeSet(String id,
                                                       DateTimeOffset createdAt,
                                                       ImmutableArray<RoamingNetworkChange> changes)
        {
            var before = ETags;
            var pending = new RoamingNetworkChangeSet(id, Root.Id, Revision, createdAt, changes, before, before);
            ValidateChangeSet(pending);
            var result = ApplyOperations(pending);
            return new RoamingNetworkChangeSet(id, Root.Id, Revision, pending.CreatedAt, changes, before,
                                              ContentETags(pending, "AfterETags", result));
        }

        // Only preparation and the checked public applier can use this unpublished working result.
        private RoamingNetworkDataSnapshot ApplyOperations(RoamingNetworkChangeSet changeSet)
        {

            var map        = Entities;
            var references = References;

            for (var index = 0; index < changeSet.Changes.Length; index++)
            {
                var change = changeSet.Changes[index];

                try
                {
                    var type   = InfrastructureChangeSchema.Type(change.EntityType);
                    var id     = InfrastructureChangeSchema.Id(type, change.EntityId);
                    var parent = ResolveParent(change, type, map);
                    var key    = Key(type, id, parent?.Id);

                    (map, references) = change.Kind switch
                    {
                        RoamingNetworkChangeKind.Add            => AddEntity(change, key, parent, map, references, changeSet.CreatedAt),
                        RoamingNetworkChangeKind.Remove         => RemoveEntity(change, key, parent, map, references, changeSet.CreatedAt),
                        RoamingNetworkChangeKind.UpdateProperty => UpdateEntityProperty(change, key, parent, map, references, changeSet.CreatedAt),
                        RoamingNetworkChangeKind.AddElement or RoamingNetworkChangeKind.RemoveElement or
                        RoamingNetworkChangeKind.ReplaceElement or RoamingNetworkChangeKind.UpdateElementProperty
                                                                => ApplyElementChange(change, key, parent, map, references, changeSet.CreatedAt),
                        _                                       => throw new ArgumentException("Unsupported change kind.")
                    };
                }
                catch (Exception exception)
                {
                    throw new RoamingNetworkChangeSetException(changeSet.Id, index, exception.Message, exception);
                }
            }

            // An empty batch is a valid commit, advancing the revision as well.
            map = Touch(Root, map, changeSet.CreatedAt);

            return new RoamingNetworkDataSnapshot(Root,
                                                 map,
                                                 checked(Revision + 1),
                                                 changeSet.Id,
                                                 references);

        }

        #endregion

        #region Validate revision, target and signature

        private void ValidateChangeSet(RoamingNetworkChangeSet changeSet)
        {

            if (!RoamingNetwork_Id.TryParse(changeSet.RoamingNetworkId, out var networkId) ||
                !InfrastructureChangeSchema.SameId(InfrastructureEntityType.RoamingNetwork, networkId.ToString(), Root.Id))
            {
                throw new RoamingNetworkChangeSetException(changeSet.Id, null, "The change set targets a different roaming network.");
            }

            if (changeSet.BaseRevision != Revision)
                throw new RoamingNetworkChangeSetException(changeSet.Id, null, $"Revision conflict: expected {Revision}, received {changeSet.BaseRevision}.");

            if (Revision == Int64.MaxValue)
                throw new RoamingNetworkChangeSetException(changeSet.Id, null, "The revision cannot be incremented further.");

            ExpectETags(changeSet, "BeforeETags", changeSet.BeforeETags, this);

        }

        private static ImmutableArray<ETag> ContentETags(RoamingNetworkChangeSet changeSet, String field,
                                                           RoamingNetworkDataSnapshot snapshot)
        {
            try { return snapshot.ETags; }
            catch (Exception exception)
            {
                throw new RoamingNetworkChangeSetException(changeSet.Id, null,
                    $"{field}: cannot compute canonical POI content identifiers.", exception);
            }
        }

        private static void ExpectETags(RoamingNetworkChangeSet changeSet, String field,
                                        ImmutableArray<ETag> expected, RoamingNetworkDataSnapshot snapshot)
        {
            var actual = ContentETags(changeSet, field, snapshot);
            for (var index = 0; index < actual.Length; index++)
                if (expected[index] != actual[index])
                    throw new RoamingNetworkChangeSetException(changeSet.Id, null,
                        $"{field} conflict: expected '{expected[index]}', actual '{actual[index]}'.");
        }

        private static void VerifyChangeSetSignatures(RoamingNetworkChangeSet                  changeSet,
                                                     Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifySignature)
        {

            if (changeSet.Signatures.IsEmpty)
                return;

            if (verifySignature is null)
                throw new RoamingNetworkChangeSetException(changeSet.Id, null, "A signed change set requires a signature verifier.");

            for (var index = 0; index < changeSet.Signatures.Length; index++)
            {
                var signature = changeSet.Signatures[index];
                try
                {
                    if (!verifySignature(changeSet, signature))
                        throw new RoamingNetworkChangeSetException(changeSet.Id, null,
                            $"Signatures[{index}] ('{signature.KeyId}'): signature is invalid.");
                }
                catch (RoamingNetworkChangeSetException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    throw new RoamingNetworkChangeSetException(changeSet.Id, null,
                        $"Signatures[{index}] ('{signature.KeyId}'): verification failed.", exception);
                }
            }

        }

        #endregion

        #region Add, remove and update entities

        private static (EntityMap Entities, ReferenceMap References) AddEntity(RoamingNetworkChange      change,
                                                                                           InfrastructureEntityKey   key,
                                                                                           InfrastructureEntityKey?  parent,
                                                                                           EntityMap                 map,
                                                                                           ReferenceMap        references,
                                                                                           DateTimeOffset            timestamp)
        {

            if (key.Type == InfrastructureEntityType.RoamingNetwork)
                throw new ArgumentException("The root network cannot be added.");

            if (map.ContainsKey(key))
                throw new ArgumentException($"Entity '{key}' already exists.");

            var document  = ReadJSON(change.NewValue!.Value.GetRawText());
            POIRepresentation.RequireStatic(document, key.Type.ToString());
            var payloadId = InfrastructureJson.Text(document, InfrastructureChangeSchema.IdField(key.Type));

            if (payloadId is null || !InfrastructureChangeSchema.SameId(key.Type, payloadId, key.Id))
                throw new ArgumentException("The payload identifier does not match EntityId.");

            var imported = new List<InfrastructureEntityKey>();
            var newKey   = Import(document, key.Type, parent, ref map, timestamp, imported);
            var owner    = parent!.Value;

            map = map.SetItem(owner, map[owner].With(children: map[owner].Children.Add(newKey)));

            foreach (var importedKey in imported)
                Validate(importedKey, map);

            foreach (var importedKey in imported)
                references = AddReferences(map[importedKey], map, references);

            return (Touch(owner, map, timestamp), references);

        }

        private (EntityMap Entities, ReferenceMap References) RemoveEntity(RoamingNetworkChange      change,
                                                                                       InfrastructureEntityKey   key,
                                                                                       InfrastructureEntityKey?  parent,
                                                                                       EntityMap                 map,
                                                                                       ReferenceMap        references,
                                                                                       DateTimeOffset            timestamp)
        {

            var entity = Require(key, parent, map);

            if (key == Root)
                throw new ArgumentException("The root network cannot be removed.");

            if (change.OldValue is { } expected)
            {
                var expectedJSON = ReadJSON(expected.GetRawText());
                POIRepresentation.RequireStatic(expectedJSON, key.Type.ToString());
                POIRepresentation.RemoveETags(expectedJSON, key.Type.ToString());
                Expect(JsonDocumentValue(MetrologyJson.NormalizeHierarchy(expectedJSON, key.Type).
                                            ToString(Newtonsoft.Json.Formatting.None)),
                       Json(entity.Key, map), "Entity document");
            }

            var removed = Descendants(entity.Key, map).ToArray();

            foreach (var removedKey in removed)
                references = RemoveReferences(map[removedKey], references);

            foreach (var removedKey in removed)
            {
                if (references.TryGetValue(removedKey, out var consumers) && !consumers.IsEmpty)
                    throw new ArgumentException($"Entity '{removedKey}' is still referenced by '{consumers.First()}'. Remove its references first.");
            }

            map = Remove(entity.Key, map);

            var owner = entity.Parent!.Value;

            map = map.SetItem(owner, map[owner].With(children: map[owner].Children.Remove(entity.Key)));

            return (Touch(owner, map, timestamp), references);

        }

        private static (EntityMap Entities, ReferenceMap References) UpdateEntityProperty(RoamingNetworkChange      change,
                                                                                                      InfrastructureEntityKey   key,
                                                                                                      InfrastructureEntityKey?  parent,
                                                                                                      EntityMap                 map,
                                                                                                      ReferenceMap        references,
                                                                                                      DateTimeOffset            timestamp)
        {

            var entity   = Require(key, parent, map);
            var property = change.PropertyName!;

            InfrastructureChangeSchema.Property(key.Type, property);

            if (change.OldValue is { } expected)
            {
                var expectedDocument = new JObject(new JProperty(property, ReadToken(expected.GetRawText())));
                POIRepresentation.RequireStatic(expectedDocument, key.Type.ToString());
                POIRepresentation.RemoveETags(expectedDocument, key.Type.ToString());
                expected = JsonDocumentValue(expectedDocument[property]!.ToString(Newtonsoft.Json.Formatting.None));
                if (!entity.Properties.TryGetValue(property, out var actual))
                    throw new ArgumentException($"Property '{property}' is absent; it does not match the expected old value.");

                Expect(PropertyValue(key.Type, property, expected), actual, property);
            }

            var replacement = new JObject(new JProperty(property, ReadToken(change.NewValue!.Value.GetRawText())));
            POIRepresentation.RequireStatic(replacement, key.Type.ToString());
            POIRepresentation.RemoveETags(replacement, key.Type.ToString());
            POIRepresentation.InitializeNestedMetadata(replacement, key.Type.ToString(), timestamp);
            var properties = entity.Properties.SetItem(property, PropertyValue(key.Type, property,
                JsonDocumentValue(replacement[property]!.ToString(Newtonsoft.Json.Formatting.None))));

            map = map.SetItem(key, entity.With(properties: properties));

            Validate(key, map);

            references = RemoveReferences(entity, references);
            references = AddReferences(map[key], map, references);

            return (Touch(key, map, timestamp), references);

        }

        #endregion

        #region Resolve entity keys and parents

        private InfrastructureEntityKey? ResolveParent(RoamingNetworkChange      change,
                                                       InfrastructureEntityType  type,
                                                       EntityMap                 map)
        {

            if (type == InfrastructureEntityType.RoamingNetwork)
            {
                if (change.ParentEntityType is not null)
                    throw new ArgumentException("The root network has no parent.");

                return null;
            }

            var parentType = InfrastructureChangeSchema.Relations[type].Parent;

            if (change.ParentEntityType is not null)
            {
                if (InfrastructureChangeSchema.Type(change.ParentEntityType) != parentType)
                    throw new ArgumentException($"{type} requires a parent of type {parentType}.");

                var parent = Key(parentType, change.ParentEntityId!);

                if (!map.ContainsKey(parent))
                    throw new ArgumentException($"Parent '{parent}' does not exist.");

                return parent;
            }

            if (type == InfrastructureEntityType.ChargingConnector)
                throw new ArgumentException("Connector changes require an explicit EVSE parent because connector IDs are local.");

            if (change.Kind != RoamingNetworkChangeKind.Add)
                return null;

            if (parentType == InfrastructureEntityType.RoamingNetwork)
                return Root;

            throw new ArgumentException($"Adding {type} requires an explicit {parentType} parent.");

        }

        private static InfrastructureEntitySnapshot Require(InfrastructureEntityKey   key,
                                                            InfrastructureEntityKey?  parent,
                                                            EntityMap                 map)
        {

            if (!map.TryGetValue(key, out var entity))
                throw new ArgumentException($"Entity '{key}' does not exist.");

            if (parent is not null && entity.Parent != parent)
                throw new ArgumentException("The entity does not belong to the supplied parent.");

            return entity;

        }

        private static InfrastructureEntityKey Key(InfrastructureEntityType  type,
                                                   String                    id,
                                                   String?                   parentId = null)
        {

            var scope = type == InfrastructureEntityType.ChargingConnector
                            ? parentId is null
                                  ? throw new ArgumentException("An EVSE scope is required for connectors.")
                                  : InfrastructureChangeSchema.Id(InfrastructureEntityType.EVSE, parentId)
                            : null;

            return new InfrastructureEntityKey(type, InfrastructureChangeSchema.Id(type, id), scope);

        }

        #endregion

        #region Update timestamps and validate expected values

        private static EntityMap Touch(InfrastructureEntityKey  key,
                                       EntityMap                map,
                                       DateTimeOffset           timestamp)
        {

            var value = System.Text.Json.JsonSerializer.SerializeToElement(
                            timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture));

            while (true)
            {
                var entity = map[key];

                if (InfrastructureChangeSchema.HasMetadata(key.Type))
                    map = map.SetItem(key, entity.With(properties: entity.Properties.SetItem("lastChange", value)));

                if (entity.Parent is not { } parent)
                    return map;

                key = parent;
            }

        }

        private static void Expect(JsonElement  expected,
                                   JsonElement  actual,
                                   String       field)
        {

            if (!JsonElement.DeepEquals(expected, actual))
                throw new ArgumentException($"Old value conflict for '{field}'.");

        }

        private static JsonElement PropertyValue(InfrastructureEntityType type,
                                                 String                   property,
                                                 JsonElement              value)
        {

            if (MetrologyJson.HasQuantities(type, property) && value.ValueKind != JsonValueKind.Null)
                return JsonDocumentValue(MetrologyJson.NormalizeProperty(type, property, ReadToken(value.GetRawText())).
                                             ToString(Newtonsoft.Json.Formatting.None));

            return value;

        }

        #endregion

    }

}
