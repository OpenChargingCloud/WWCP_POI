/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Derive owned-object lifetimes from original first-parent operations, including intermediate states.
/// </summary>
internal sealed class POILifetimeState
{
    private sealed record Slot(String Path, String? Parent, String? Identity, String? Created, Int32 Depth);

    internal ImmutableDictionary<String, RoamingNetworkLifetimeOrigin> Origins { get; }

    private POILifetimeState(ImmutableDictionary<String, RoamingNetworkLifetimeOrigin> origins)
        => Origins = origins;

    internal RoamingNetworkLifetimeOrigin? At(String path) => Origins.GetValueOrDefault(path);

    internal Boolean SameUnder(POILifetimeState other, String path)
        => Origins.Where(entry => Within(entry.Key, path)).All(entry => other.At(entry.Key) == entry.Value) &&
           other.Origins.Where(entry => Within(entry.Key, path)).All(entry => At(entry.Key) == entry.Value);

    internal static Boolean Within(String path, String prefix)
        => path == prefix || path.StartsWith(prefix + "/", StringComparison.Ordinal);

    internal static POILifetimeState Checkpoint(RoamingNetworkCommit commit, RoamingNetworkDataSnapshot snapshot)
    {
        var origin = new RoamingNetworkLifetimeOrigin(commit.Id, -1);
        return new(Slots(snapshot).ToImmutableDictionary(slot => slot.Path, _ => origin, StringComparer.Ordinal));
    }

    internal POILifetimeState Apply(RoamingNetworkCommit commit, RoamingNetworkDataSnapshot source)
    {
        if (commit.Kind == RoamingNetworkCommitKind.Snapshot) return this;
        var batch = commit.ChangeSet!;
        var origins = Origins;
        var previous = Slots(source).ToDictionary(slot => slot.Path, StringComparer.Ordinal);
        var working = source;
        for (var index = 0; index < batch.Changes.Length; index++)
        {
            if (!working.TryMergeOperation(batch.Changes[index], batch.CreatedAt, out var next, out var error))
                throw new ArgumentException($"Lifetime replay failed at {commit.Id}, operation {index}: {error}");
            working = next!;
            var current = Slots(working).ToDictionary(slot => slot.Path, StringComparer.Ordinal);
            var following = ImmutableDictionary.CreateBuilder<String, RoamingNetworkLifetimeOrigin>(StringComparer.Ordinal);
            var birth = new RoamingNetworkLifetimeOrigin(commit.Id, index);
            foreach (var slot in current.Values.OrderBy(slot => slot.Depth).ThenBy(slot => slot.Path, StringComparer.Ordinal))
            {
                var continuous = previous.TryGetValue(slot.Path, out var old) &&
                                 old.Parent == slot.Parent && old.Identity == slot.Identity && old.Created == slot.Created &&
                                 origins.ContainsKey(slot.Path) &&
                                 (slot.Parent is null || origins.GetValueOrDefault(slot.Parent) == following.GetValueOrDefault(slot.Parent));
                following[slot.Path] = continuous ? origins[slot.Path] : birth;
            }
            origins = following.ToImmutable();
            previous = current;
        }
        return new(origins);
    }

    private static IEnumerable<Slot> Slots(RoamingNetworkDataSnapshot snapshot)
    {
        foreach (var node in snapshot.Entities.Values)
        {
            var path = POIThreeWayMerge.Path(node.Key, []);
            var parent = node.Parent is { } owner ? POIThreeWayMerge.Path(owner, []) : null;
            var depth = 0;
            for (var cursor = node.Parent; cursor is { } key; cursor = snapshot.Entities[key].Parent) depth++;
            yield return new(path, parent, path, node.Properties.GetValueOrDefault("created").ValueKind == JsonValueKind.Undefined
                                                  ? null : node.Properties["created"].GetRawText(), depth);
            foreach (var property in node.Properties)
                foreach (var slot in Nested(node.Key.Type.ToString(), property.Key, property.Value, node.Key, [], path, depth + 1))
                    yield return slot;
        }
    }

    private static IEnumerable<Slot> Nested(String kind, String name, JsonElement value, InfrastructureEntityKey key,
                                             ImmutableArray<POIElementPathSegment> path, String parent, Int32 depth)
    {
        if (POIElementSchema.TryChild(kind, name) is not { IsReference: false } relation) yield break;
        var documents = relation.IsArray && value.ValueKind == JsonValueKind.Array ? value.EnumerateArray().ToArray() : [value];
        foreach (var document in documents)
        {
            if (document.ValueKind != JsonValueKind.Object) continue;
            var id = relation.Identity is not null && document.TryGetProperty(relation.IdField ?? "id", out var identifier) &&
                     identifier.ValueKind == JsonValueKind.String ? identifier.GetString() : null;
            var elementPath = path.Add(new(name, relation.IsArray ? id : null));
            var address = POIThreeWayMerge.Path(key, elementPath);
            yield return new(address, parent, id is null ? null : relation.Identity!(id),
                             document.TryGetProperty("created", out var created) ? created.GetRawText() : null, depth);
            foreach (var property in document.EnumerateObject())
                foreach (var slot in Nested(relation.Kind, property.Name, property.Value, key, elementPath, address, depth + 1))
                    yield return slot;
        }
    }
}
