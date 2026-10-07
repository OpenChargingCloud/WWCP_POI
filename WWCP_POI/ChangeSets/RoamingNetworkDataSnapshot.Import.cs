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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using EntityMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    public sealed partial class RoamingNetworkDataSnapshot
    {

        #region Import a nested hierarchy

        private static InfrastructureEntityKey Import(JObject                         document,
                                                      InfrastructureEntityType        type,
                                                      InfrastructureEntityKey?        parent,
                                                      ref EntityMap                   map,
                                                      DateTimeOffset?                 timestamp,
                                                      List<InfrastructureEntityKey>?  imported  = null)
        {

            var copy = (JObject) document.DeepClone();
            POIRepresentation.RemoveETags(copy, type.ToString());
            var text = InfrastructureJson.Text(copy, "@id") ??
                       throw new ArgumentException("Missing '@id'.");
            var key  = Key(type, text, parent?.Id);

            if (map.ContainsKey(key))
                throw new ArgumentException($"Duplicate entity '{key}'.");

            copy["@id"] = key.Id;

            ValidateImportParent(copy, parent);
            InfrastructureChangeSchema.ValidateFields(type, copy);

            if (timestamp is { } time)
            {
                InfrastructureChangeSchema.InitializeMetadata(type, copy, time);
                POIRepresentation.InitializeNestedMetadata(copy, type.ToString(), time);
            }

            NormalizeImportedTimestamps(copy);
            MetrologyJson.NormalizeEntity(copy, type);

            copy.Remove("revision");
            copy.Remove("appliedChangeSetId");

            var childDocuments = DetachChildDocuments(copy, type);
            var children       = ImmutableHashSet<InfrastructureEntityKey>.Empty;
            var properties     = copy.Properties().ToImmutableDictionary(
                                     property => property.Name,
                                     property => JsonDocumentValue(property.Value.ToString(Formatting.None)),
                                     StringComparer.Ordinal);

            // Insert a reservation before descendants, so duplicate IDs at every depth are detected.
            map = map.Add(key, new InfrastructureEntitySnapshot(key, parent, properties, children));

            imported?.Add(key);

            foreach (var child in childDocuments)
                children = children.Add(Import(child.Document, child.Type, key, ref map, timestamp, imported));

            map = map.SetItem(key, map[key].With(children: children));

            return key;

        }

        private static void ValidateImportParent(JObject                   document,
                                                 InfrastructureEntityKey?  parent)
        {

            if (parent is not { } owner)
                return;

            var parentField = owner.Type == InfrastructureEntityType.EVSE
                                  ? "EVSEId"
                                  : Char.ToLowerInvariant(owner.Type.ToString()[0]) + owner.Type.ToString()[1..] + "Id";

            if (document[parentField] is not { } reference)
                return;

            if (reference.Type != JTokenType.String ||
                !InfrastructureChangeSchema.SameId(owner.Type, reference.Value<String>()!, owner.Id))
            {
                throw new ArgumentException($"{parentField} does not match the supplied parent.");
            }

            document.Remove(parentField);

        }

        private static void NormalizeImportedTimestamps(JObject document)
        {

            // JObject.Parse may already have converted ISO strings into DateTime tokens.
            // Persist known timestamp fields consistently, independent of the reader's date handling.
            foreach (var field in new[] { "created", "lastChange" })
            {
                if (InfrastructureJson.Date(document, field) is { } date)
                    document[field] = date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
            }

            foreach (var field in new[] { "status", "adminStatus" })
            {
                if (InfrastructureJson.Object(document, field) is { } state &&
                    InfrastructureJson.Date(state, "timestamp") is { } date)
                {
                    state["timestamp"] = date.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
                }
            }

        }

        private static List<(InfrastructureEntityType Type, JObject Document)> DetachChildDocuments(JObject                   document,
                                                                                                    InfrastructureEntityType  type)
        {

            var children = new List<(InfrastructureEntityType Type, JObject Document)>();

            foreach (var relation in InfrastructureChangeSchema.Relations.Where(relation => relation.Value.Parent == type))
            {
                foreach (var child in InfrastructureJson.Array(document, relation.Value.Field, InfrastructureJson.Entry))
                    children.Add((relation.Key, child));

                document.Remove(relation.Value.Field);
            }

            return children;

        }

        #endregion

        #region Traverse and remove subtrees

        private static IEnumerable<InfrastructureEntityKey> Descendants(InfrastructureEntityKey  key,
                                                                        EntityMap                map)
        {

            yield return key;

            foreach (var child in map[key].Children)
            {
                foreach (var descendant in Descendants(child, map))
                    yield return descendant;
            }

        }

        private static EntityMap Remove(InfrastructureEntityKey  key,
                                        EntityMap                map)
        {

            foreach (var child in map[key].Children)
                map = Remove(child, map);

            return map.Remove(key);

        }

        #endregion

    }

}
