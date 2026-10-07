/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;

namespace cloud.charging.open.protocols.WWCP.POI;

internal sealed record POIMergePlanningIssue(InfrastructureEntityKey Entity, String Message,
    String? PropertyName = null, InfrastructureEntityKey? RelatedEntity = null);

internal sealed class POIMergePlanningException(ImmutableArray<POIMergePlanningIssue> issues)
    : Exception(String.Join("; ", issues.Select(issue => issue.Message)))
{
    internal ImmutableArray<POIMergePlanningIssue> Issues { get; } = issues;
}

public sealed partial class RoamingNetworkDataSnapshot
{
    internal JsonElement MergeEntityValue(InfrastructureEntityKey key)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) WriteNode(writer, key);
        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    internal RoamingNetworkDataSnapshot MergeTarget(IEnumerable<InfrastructureEntitySnapshot> nodes)
    {
        var map = nodes.ToImmutableDictionary(node => node.Key);
        if (!map.ContainsKey(Root)) throw new POIMergePlanningException([new(Root, "A merge cannot remove its root network.")]);
        var issues = new List<POIMergePlanningIssue>();
        var children = map.Keys.ToDictionary(key => key, _ => ImmutableHashSet.CreateBuilder<InfrastructureEntityKey>());
        foreach (var node in map.Values.OrderBy(node => node.Key.Type).ThenBy(node => node.Key.Id, StringComparer.Ordinal).
                                       ThenBy(node => node.Key.Scope, StringComparer.Ordinal))
        {
            if (node.Key == Root)
            {
                if (node.Parent is not null) issues.Add(new(node.Key, "The network root cannot have a parent.", "$parent", node.Parent));
            }
            else if (node.Parent is not { } parent || !map.ContainsKey(parent) ||
                     parent.Type != InfrastructureChangeSchema.Relations[node.Key.Type].Parent)
                issues.Add(new(node.Key, $"{node.Key.Type}/{node.Key.Id}: missing or incompatible merged parent.", "$parent", node.Parent));
            else children[parent].Add(node.Key);
        }
        if (issues.Count > 0) throw new POIMergePlanningException([.. issues]);
        map = map.ToImmutableDictionary(entry => entry.Key, entry => entry.Value.With(children: children[entry.Key].ToImmutable()));
        // Diagnose every unresolved/out-of-scope reference before domain projection can obscure
        // it as a group/parser failure. Collect independently affected consumers and targets.
        foreach (var node in map.Values.OrderBy(node => node.Key.Type).ThenBy(node => node.Key.Id, StringComparer.Ordinal).
                                       ThenBy(node => node.Key.Scope, StringComparer.Ordinal))
        {
            try
            {
                foreach (var reference in ReferenceTargets(node).OrderBy(reference => reference.Field, StringComparer.Ordinal).
                            ThenBy(reference => reference.Key.Type).ThenBy(reference => reference.Key.Id, StringComparer.Ordinal))
                    if (ReferenceError(node, reference.Key, map) is { } error)
                        issues.Add(new(node.Key, error, reference.Field, reference.Key));
            }
            catch (Exception exception) { issues.Add(new(node.Key, exception.Message)); }
        }
        if (issues.Count > 0) throw new POIMergePlanningException([.. issues]);
        var references = System.Collections.Immutable.ImmutableDictionary<InfrastructureEntityKey,
            ImmutableHashSet<InfrastructureEntityKey>>.Empty;
        foreach (var node in map.Values.OrderBy(node => node.Key.Type).ThenBy(node => node.Key.Id, StringComparer.Ordinal).
                                       ThenBy(node => node.Key.Scope, StringComparer.Ordinal))
        {
            try
            {
                Validate(node.Key, map);
                references = AddReferences(node, map, references);
            }
            catch (Exception exception) { issues.Add(new(node.Key, exception.Message)); }
        }
        if (issues.Count > 0) throw new POIMergePlanningException([.. issues]);
        return new(Root, map, Revision, AppliedChangeSetId, references);
    }

    internal Boolean TryMergeOperation(RoamingNetworkChange change, DateTimeOffset timestamp,
                                       out RoamingNetworkDataSnapshot? next, out String? error)
    {
        try
        {
            var type = InfrastructureChangeSchema.Type(change.EntityType);
            var parent = ResolveParent(change, type, Entities);
            var key = Key(type, change.EntityId, parent?.Id);
            var (map, references) = change.Kind switch {
                RoamingNetworkChangeKind.Add => AddEntity(change, key, parent, Entities, References, timestamp),
                RoamingNetworkChangeKind.Remove => RemoveEntity(change, key, parent, Entities, References, timestamp),
                RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty =>
                    UpdateEntityProperty(change, key, parent, Entities, References, timestamp),
                _ => ApplyElementChange(change, key, parent, Entities, References, timestamp)
            };
            next = new(Root, map, Revision, AppliedChangeSetId, references);
            error = null;
            return true;
        }
        catch (Exception exception) { next = null; error = exception.Message; return false; }
    }
}
