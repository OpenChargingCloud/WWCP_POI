using System.Collections.Immutable;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using EntityMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;

namespace WWCP_POI_Tests.Interoperability;

// The previous copying import, using unchanged scalar/schema helpers from production.
internal static class SnapshotImportOracle
{
    private static readonly Func<InfrastructureEntityType, String, String?, InfrastructureEntityKey> Key =
        typeof(RoamingNetworkDataSnapshot).GetMethod("Key", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<InfrastructureEntityType, String, String?, InfrastructureEntityKey>>();
    private static readonly Func<String, JsonElement> JsonDocumentValue =
        typeof(RoamingNetworkDataSnapshot).GetMethod("JsonDocumentValue", BindingFlags.Static | BindingFlags.NonPublic)!
            .CreateDelegate<Func<String, JsonElement>>();
        internal static InfrastructureEntityKey Import(JObject                         document,
                                                      InfrastructureEntityType        type,
                                                      InfrastructureEntityKey?        parent,
                                                      ref EntityMap                   map,
                                                      DateTimeOffset?                 timestamp,
                                                      List<InfrastructureEntityKey>?  imported  = null)
        {

            var copy = (JObject) document.DeepClone();
            POIRepresentation.RemoveETags(copy, type.ToString());
            POIRepresentation.RemoveRuntime(copy, type.ToString());
            var idField = InfrastructureChangeSchema.IdField(type);
            var text = InfrastructureJson.Text(copy, idField) ??
                       throw new ArgumentException($"Missing '{idField}'.");
            var key  = Key(type, text, parent?.Id);

            if (map.ContainsKey(key))
                throw new ArgumentException($"Duplicate entity '{key}'.");

            copy[idField] = key.Id;

            ValidateImportParent(copy, parent);
            InfrastructureChangeSchema.ValidateFields(type, copy);

            if (timestamp is { } time)
            {
                InfrastructureChangeSchema.InitializeMetadata(type, copy, time);
                POIRepresentation.InitializeNestedMetadata(copy, type.ToString(), time);
            }

            NormalizeImportedTimestamps(copy);
            POIRepresentation.NormalizeStaticTimestamps(copy, type.ToString());
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

}
