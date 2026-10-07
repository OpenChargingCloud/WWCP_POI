/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Globalization;
using System.Collections.Immutable;
using System.Text.Json;
using Newtonsoft.Json.Linq;
using EntityMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;
using ReferenceMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, System.Collections.Immutable.ImmutableHashSet<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey>>;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkDataSnapshot
{
    /// <summary>
    /// Read a detached static element value for a complete RemoveElement/ReplaceElement precondition.
    /// Connector owners require their EVSE scope; optional absent values are not invented as null.
    /// </summary>
    public JsonElement GetElementValue(InfrastructureEntityType ownerType, String ownerId,
                                        ImmutableArray<POIElementPathSegment> elementPath,
                                        String? parentId = null)
        => JsonTokenValue(ReadElement(GetEntity(ownerType, ownerId, parentId), elementPath));

    /// <summary>
    /// Read one existing static element property for an UpdateElementProperty precondition.
    /// An absent property differs from an explicitly stored JSON null.
    /// </summary>
    public JsonElement GetElementPropertyValue(InfrastructureEntityType ownerType, String ownerId,
                                                ImmutableArray<POIElementPathSegment> elementPath,
                                                String propertyName, String? parentId = null)
    {
        var element = ReadElement(GetEntity(ownerType, ownerId, parentId), elementPath) as JObject ??
                      throw new ArgumentException("A reference has no object properties.");
        return element[propertyName] is { } value ? JsonTokenValue(value) :
               throw new ArgumentException($"{propertyName}: the property is absent.");
    }

    private static JToken ReadElement(InfrastructureEntitySnapshot owner,
                                       ImmutableArray<POIElementPathSegment> path)
    {
        if (path.IsDefaultOrEmpty || path.Any(segment => segment is null))
            throw new ArgumentException("A nonempty element path without null segments is required.", nameof(path));
        InfrastructureChangeSchema.Property(owner.Key.Type, path[0].PropertyName);
        JToken selected = OwnJSON(owner);
        var kind = owner.Key.Type.ToString();
        foreach (var segment in path)
        {
            var relation = POIElementSchema.Child(kind, segment.PropertyName);
            relation.ValidateSelector(segment);
            var value = (selected as JObject)?[segment.PropertyName];
            if (relation.IsArray)
            {
                var array = value as JArray ?? throw new ArgumentException($"{segment.PropertyName}: the collection is absent.");
                ValidateIdentities(array, relation);
                selected = array.FirstOrDefault(item => relation.Matches(item, segment.ElementId!)) ??
                           throw new ArgumentException("The addressed element does not exist.");
            }
            else
            {
                selected = value is not null && value.Type != JTokenType.Null ? value :
                           throw new ArgumentException("The addressed singleton does not exist.");
                if (segment.ElementId is { } id && !relation.Matches(selected, id))
                    throw new ArgumentException("The singleton identifier does not match the path.");
            }
            kind = relation.Kind;
        }
        return selected;
    }

    private static (EntityMap Entities, ReferenceMap References) ApplyElementChange(
        RoamingNetworkChange change, InfrastructureEntityKey key, InfrastructureEntityKey? parent,
        EntityMap map, ReferenceMap references, DateTimeOffset timestamp)
    {
        var entity = Require(key, parent, map);
        var rootProperty = change.ElementPath[0].PropertyName;
        InfrastructureChangeSchema.Property(key.Type, rootProperty);
        var document = OwnJSON(entity);
        var owner = document;
        var kind = key.Type.ToString();
        var ancestors = new List<(JObject Document, String Kind)>();
        var arrays = new List<(JArray Array, POIElementSchema.Relation Relation)>();

        for (var index = 0; index < change.ElementPath.Length; index++)
        {
            var segment = change.ElementPath[index];
            var relation = POIElementSchema.Child(kind, segment.PropertyName);
            relation.ValidateSelector(segment);
            var terminal = index == change.ElementPath.Length - 1;
            var value = owner[segment.PropertyName];
            JArray? array = null;
            JToken? selected;
            if (relation.IsArray)
            {
                if (value is null || value.Type == JTokenType.Null)
                {
                    if (!terminal || change.Kind != RoamingNetworkChangeKind.AddElement)
                        throw new ArgumentException($"{segment.PropertyName}: the collection is absent.");
                    owner[segment.PropertyName] = value = new JArray();
                }
                array = value as JArray ?? throw new ArgumentException($"{segment.PropertyName}: expected an array.");
                ValidateIdentities(array, relation);
                arrays.Add((array, relation));
                selected = array.FirstOrDefault(item => relation.Matches(item, segment.ElementId!));
            }
            else
            {
                selected = value?.Type == JTokenType.Null ? null : value;
                if (selected is not null && segment.ElementId is { } id && !relation.Matches(selected, id))
                    throw new ArgumentException($"{segment.PropertyName}: the singleton identifier does not match the path.");
            }

            if (!terminal)
            {
                owner = selected as JObject ?? throw new ArgumentException($"{segment.PropertyName}: the path owner does not exist.");
                ancestors.Add((owner, relation.Kind));
                kind = relation.Kind;
                continue;
            }

            if (change.Kind == RoamingNetworkChangeKind.AddElement)
            {
                if (selected is not null)
                    throw new ArgumentException("The addressed element already exists.");
            }
            else if (selected is null)
                throw new ArgumentException("The addressed element does not exist.");

            if (change.OldValue is { } expected)
            {
                var expectedToken = ReadToken(expected.GetRawText());
                JToken actual = selected!;
                if (change.Kind == RoamingNetworkChangeKind.UpdateElementProperty)
                {
                    POIElementSchema.Property(relation.Kind, change.PropertyName!);
                    actual = (selected as JObject)?[change.PropertyName!] ??
                             throw new ArgumentException($"{change.PropertyName}: absent; it does not match the expected old value.");
                    var expectedDocument = new JObject(new JProperty(change.PropertyName!, expectedToken));
                    POIRepresentation.RequireStatic(expectedDocument, relation.Kind);
                    POIRepresentation.RemoveETags(expectedDocument, relation.Kind);
                    expectedToken = POIElementSchema.NormalizeProperty(relation.Kind, change.PropertyName!, expectedDocument[change.PropertyName!]!);
                    actual = POIElementSchema.NormalizeProperty(relation.Kind, change.PropertyName!, actual.DeepClone());
                }
                else
                {
                    if (expectedToken is JObject expectedObject)
                    {
                        POIRepresentation.RequireStatic(expectedObject, relation.Kind);
                        POIRepresentation.RemoveETags(expectedObject, relation.Kind);
                    }
                    expectedToken = POIElementSchema.Normalize(relation.Kind, expectedToken);
                    actual = POIElementSchema.Normalize(relation.Kind, actual.DeepClone());
                }
                Expect(JsonTokenValue(expectedToken), JsonTokenValue(actual), "Element precondition");
            }

            if (change.Kind == RoamingNetworkChangeKind.RemoveElement)
            {
                if (array is not null) selected!.Remove();
                else
                {
                    if (!relation.Optional) throw new ArgumentException("A required singleton cannot be removed.");
                    owner[segment.PropertyName] = JValue.CreateNull();
                }
            }
            else
            {
                var replacement = ReadToken(change.NewValue!.Value.GetRawText());
                if (change.Kind == RoamingNetworkChangeKind.UpdateElementProperty)
                {
                    POIElementSchema.Property(relation.Kind, change.PropertyName!);
                    var current = selected as JObject ?? throw new ArgumentException("A reference has no editable object properties.");
                    current[change.PropertyName!] = replacement;
                    POIElementSchema.ValidateDocument(current, relation.Kind);
                    var normalized = POIElementSchema.Normalize(relation.Kind, current);
                    if (!ReferenceEquals(normalized, current))
                    {
                        if (array is not null) current.Replace(normalized);
                        else owner[segment.PropertyName] = normalized;
                        current = (JObject) normalized;
                    }
                    ancestors.Add((current, relation.Kind));
                }
                else
                {
                    relation.ValidateValue(replacement, segment);
                    replacement = POIElementSchema.Normalize(relation.Kind, replacement);
                    if (change.Kind == RoamingNetworkChangeKind.ReplaceElement && selected is JObject oldObject &&
                        replacement is JObject newObject && POIElementSchema.HasManagedMetadata(relation.Kind) &&
                        relation.ReadId(oldObject) is { } existingId && relation.Matches(newObject, existingId))
                    {
                        var created = InfrastructureJson.Date(oldObject, "created");
                        if (InfrastructureJson.Date(newObject, "created") is { } supplied && created is { } original && supplied != original)
                            throw new ArgumentException("Replacing an existing element cannot change its creation timestamp.");
                        if (created is { } preserved)
                            newObject["created"] = preserved.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
                    }
                    if (array is not null)
                    {
                        if (selected is null) array.Add(replacement);
                        else selected.Replace(replacement);
                    }
                    else owner[segment.PropertyName] = replacement;
                    if (replacement is JObject nested) ancestors.Add((nested, relation.Kind));
                }
            }
        }

        POIRepresentation.RequireStatic(document, key.Type.ToString());
        POIRepresentation.RemoveETags(document, key.Type.ToString());
        POIRepresentation.InitializeNestedMetadata(document, key.Type.ToString(), timestamp);
        foreach (var (nested, nestedKind) in ancestors)
            if (POIElementSchema.HasManagedMetadata(nestedKind))
                nested["lastChange"] = timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);

        // Membership arrays have deterministic identity order, so disjoint additions commute.
        foreach (var (array, relation) in arrays)
        {
            ValidateIdentities(array, relation);
            var ordered = array.OrderBy(item => relation.ReadId(item), StringComparer.Ordinal).ToArray();
            array.RemoveAll();
            array.Add(ordered);
        }
        var properties = entity.Properties.SetItem(rootProperty,
            PropertyValue(key.Type, rootProperty, JsonTokenValue(document[rootProperty]!)));
        map = map.SetItem(key, entity.With(properties: properties));
        Validate(key, map);
        references = RemoveReferences(entity, references);
        references = AddReferences(map[key], map, references);
        return (Touch(key, map, timestamp), references);
    }

    private static void ValidateIdentities(JArray array, POIElementSchema.Relation relation)
    {
        var seen = new HashSet<String>(StringComparer.Ordinal);
        foreach (var value in array)
        {
            var id = relation.ReadId(value) ?? throw new ArgumentException("Every collection element requires its identifier.");
            if (!seen.Add(relation.Identity!(id)))
                throw new ArgumentException($"Duplicate element identity '{id}'.");
        }
    }

    private static JsonElement JsonTokenValue(JToken value)
        => JsonDocumentValue(value.ToString(Newtonsoft.Json.Formatting.None));
}
