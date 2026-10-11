/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkDataSnapshot
{
    /// <summary>
    /// Check completed model input against every immutable property and owned child before reuse.
    /// Lexical property equality preserves numbers, nested arrays and metadata without normalization.
    /// </summary>
    internal Boolean MatchesCompletedDocument(JObject document, Int64 revision, String? changeSetId)
    {
        if (Revision != revision || AppliedChangeSetId != changeSetId) return false;
        var visited = new HashSet<InfrastructureEntityKey>();
        return Match(Root, document) && visited.Count == Entities.Count;

        Boolean Match(InfrastructureEntityKey key, JObject node)
        {
            if (!visited.Add(key) || !Entities.TryGetValue(key, out var entity)) return false;
            var fields = 0;
            foreach (var property in node.Properties())
            {
                if (entity.Properties.TryGetValue(property.Name, out var expected))
                {
                    if (property.Value.ToString(Formatting.None) != expected.GetRawText()) return false;
                    fields++;
                    continue;
                }
                if (key == Root && property.Name is "revision" or "appliedChangeSetId") continue;
                if (property.Name == POIContentProfile.PropertyName)
                {
                    if (property.Value.Type != JTokenType.String || property.Value.Value<String>() != POIContentProfile.Id)
                        return false;
                    continue;
                }
                if (InfrastructureChangeSchema.Relations.Any(relation => relation.Value.Parent == key.Type &&
                                                                        relation.Value.Field == property.Name)) continue;
                return false;
            }
            if (fields != entity.Properties.Count) return false;
            foreach (var relation in InfrastructureChangeSchema.Relations.Where(relation => relation.Value.Parent == key.Type))
            {
                if (node[relation.Value.Field] is not JArray children ||
                    children.Count != entity.Children.Count(child => child.Type == relation.Key)) return false;
                foreach (var child in children)
                {
                    if (child is not JObject value || value[InfrastructureChangeSchema.IdField(relation.Key)] is not
                        JValue { Type: JTokenType.String } id) return false;
                    InfrastructureEntityKey childKey;
                    try { childKey = Key(relation.Key, id.Value<String>()!, key.Id); }
                    catch (ArgumentException) { return false; }
                    if (!entity.Children.Contains(childKey) || !Match(childKey, value)) return false;
                }
            }
            return true;
        }
    }
}
