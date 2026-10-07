/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;

namespace cloud.charging.open.protocols.WWCP.POI;

internal sealed partial class POIThreeWayMerge
{
    private sealed record ReferenceTransitionDraft(InfrastructureEntityKey Consumer, String Field,
        ImmutableArray<InfrastructureEntityKey> Targets, JsonElement Before, JsonElement? Detached,
        ImmutableArray<RoamingNetworkChange> Detach, ImmutableArray<RoamingNetworkChange> Restore,
        ImmutableArray<RoamingNetworkChange> RemovalBarrier);

    private readonly List<ReferenceTransitionDraft> referenceDrafts = [];
    internal ImmutableArray<RoamingNetworkMergeReferenceTransition> ReferenceTransitions { get; private set; } = [];

    private Boolean AwaitingReferenceRemoval(RoamingNetworkChange operation, List<RoamingNetworkChange> pending)
        => referenceDrafts.Any(draft => draft.Restore.Any(restore => ReferenceEquals(restore, operation)) &&
             draft.RemovalBarrier.Any(removal => pending.Any(candidate => ReferenceEquals(candidate, removal))));

    private Boolean TryDetachReferences(ref RoamingNetworkDataSnapshot working, RoamingNetworkDataSnapshot target,
        List<RoamingNetworkChange> pending, ImmutableArray<RoamingNetworkChange>.Builder ordered, DateTimeOffset timestamp)
    {
        var source = working;
        var removals = pending.Where(operation => operation.Kind == RoamingNetworkChangeKind.Remove &&
                                    source.Entities.ContainsKey(ChangeKey(operation))).ToArray();
        if (removals.Length == 0) return false;
        var subtrees = new Dictionary<RoamingNetworkChange, ImmutableHashSet<InfrastructureEntityKey>>(ReferenceEqualityComparer.Instance);
        foreach (var operation in removals) subtrees[operation] = source.MergeSubtree(ChangeKey(operation));
        var removedKeys = subtrees.Values.SelectMany(keys => keys).ToHashSet();
        var candidates = source.MergeReferencesTo(removedKeys).Where(reference =>
            subtrees.Any(subtree => subtree.Value.Contains(reference.Target) && !subtree.Value.Contains(reference.Consumer))).
            GroupBy(reference => (reference.Consumer, reference.Field));
        foreach (var candidate in candidates)
        {
            var (consumer, field) = candidate.Key;
            var node = working.Entities[consumer];
            var before = node.Properties[field];
            var relation = POIElementSchema.TryChild(consumer.Type.ToString(), field);
            var targets = candidate.Select(reference => reference.Target).Distinct().
                OrderBy(key => key.Type).ThenBy(key => key.Id, StringComparer.Ordinal).ThenBy(key => key.Scope, StringComparer.Ordinal).ToImmutableArray();
            var detach = new List<RoamingNetworkChange>();
            if (relation is { IsArray: true, IsReference: true } && before.ValueKind == JsonValueKind.Array)
            {
                var identities = targets.Select(key => InfrastructureChangeSchema.Identity(key.Type, key.Id)).ToHashSet(StringComparer.Ordinal);
                foreach (var value in before.EnumerateArray().Where(value => identities.Contains(relation.Identity!(value.GetString()!))).
                             OrderBy(value => value.GetString(), StringComparer.Ordinal))
                    detach.Add(RoamingNetworkChange.RemoveElement(consumer.Type.ToString(), consumer.Id,
                        [new(field, value.GetString())], value, node.Parent?.Type.ToString(), node.Parent?.Id));
            }
            else if (before.ValueKind == JsonValueKind.String)
                detach.Add(RoamingNetworkChange.RemoveProperty(consumer.Type.ToString(), consumer.Id, field, before,
                                                               node.Parent?.Type.ToString(), node.Parent?.Id));
            else continue;
            if (detach.Count == 0) continue;
            var detached = working;
            var valid = true;
            foreach (var operation in detach)
            {
                if (!detached.TryMergeOperation(operation, timestamp, out var next, out _)) { valid = false; break; }
                detached = next!;
            }
            if (!valid) continue;
            var detachedValue = Property(detached.Entities[consumer], field);
            var after = target.Entities.TryGetValue(consumer, out var finalNode) ? Property(finalNode, field) : null;
            var restore = new List<RoamingNetworkChange>();
            if (subtrees.Values.Any(keys => keys.Contains(consumer)))
            {
                // Rebuilding the consumer imports its complete final references. Delay that Add
                // until removals finish, so imported references cannot point to the old targets.
                if (finalNode is not null)
                    restore.AddRange(pending.Where(operation => operation.Kind == RoamingNetworkChangeKind.Add &&
                        target.MergeSubtree(ChangeKey(operation)).Contains(consumer)));
            }
            else
            {
                DiffObject(consumer.Type.ToString(), ReferenceField(field, detachedValue), ReferenceField(field, after),
                           detached.Entities[consumer], [], restore);
                pending.RemoveAll(operation => ReferenceFieldOperation(operation, consumer, field));
                pending.AddRange(restore);
            }
            working = detached;
            ordered.AddRange(detach);
            referenceDrafts.Add(new(consumer, field, targets, before, detachedValue, [.. detach], [.. restore], [.. removals]));
            return true;
        }
        return false;
    }

    private static InfrastructureEntityKey ChangeKey(RoamingNetworkChange operation)
    {
        var type = InfrastructureChangeSchema.Type(operation.EntityType);
        return new(type, operation.EntityId, type == InfrastructureEntityType.ChargingConnector ? operation.ParentEntityId : null);
    }

    private static Boolean ReferenceFieldOperation(RoamingNetworkChange operation, InfrastructureEntityKey consumer, String field)
        => ChangeKey(operation) == consumer &&
           ((operation.Kind is RoamingNetworkChangeKind.UpdateProperty or RoamingNetworkChangeKind.RemoveProperty) && operation.PropertyName == field ||
            (operation.Kind is RoamingNetworkChangeKind.AddElement or RoamingNetworkChangeKind.RemoveElement or RoamingNetworkChangeKind.ReplaceElement) &&
            operation.ElementPath.Length == 1 && operation.ElementPath[0].PropertyName == field);

    private static JsonElement ReferenceField(String field, JsonElement? value)
        => JsonSerializer.SerializeToElement(value is { } present ? new Dictionary<String, JsonElement> { [field] = present } : []);

    private void CompleteReferenceTransitions(ImmutableArray<RoamingNetworkChange>.Builder ordered,
                                              RoamingNetworkDataSnapshot finalState)
    {
        Int32 Position(RoamingNetworkChange operation)
        {
            for (var index = 0; index < ordered.Count; index++) if (ReferenceEquals(ordered[index], operation)) return index;
            throw new ArgumentException("The planned reference transition contains an operation that was not scheduled.");
        }
        ReferenceTransitions = referenceDrafts.Select(draft => new RoamingNetworkMergeReferenceTransition(draft.Consumer, draft.Field,
            draft.Targets, draft.Before, draft.Detached,
            finalState.Entities.TryGetValue(draft.Consumer, out var node) ? Property(node, draft.Field) : null,
            draft.Detach.Select(Position).ToImmutableArray(),
            draft.Restore.Select(Position).ToImmutableArray())).ToImmutableArray();
    }
}
