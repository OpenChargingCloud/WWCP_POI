/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    /// <summary>
    /// The state comparison and explicit-resolution profile recorded in prepared batch metadata.
    /// </summary>
    public const String ThreeWayMergeProfile = "wwcp-poi-three-way-merge-v1";

    /// <summary>
    /// The reserved batch metadata key binding the selected ancestor, tips and explicit resolutions.
    /// </summary>
    public const String MergeMetadataProperty = "wwcpPOIMerge";

    /// <summary>
    /// The original-operation lifetime evidence profile for optional merge audit records.
    /// </summary>
    public const String MergeLifetimeProfile = "wwcp-poi-operation-lifetime-v1";

    /// <summary>
    /// The explicit temporary reference-detachment and restoration audit profile.
    /// </summary>
    public const String MergeReferenceTransitionProfile = "wwcp-poi-reference-transition-v1";

    /// <summary>
    /// Preview a static three-way integration of retained tips using their best common ancestor.
    /// Explicit preparation returns a new unsigned commit against the left tip; it never publishes it.
    /// </summary>
    public Boolean TryMerge(RoamingNetworkCommitId leftId, RoamingNetworkCommitId rightId,
        out RoamingNetworkCommit? mergedCommit, out RoamingNetworkMergeResult result,
        Boolean merge = false, String? mergedChangeSetId = null, DateTimeOffset? createdAt = null,
        Func<RoamingNetworkMergeConflict, RoamingNetworkMergeResolution?>? resolveConflict = null,
        RoamingNetworkCommitId? commonAncestor = null,
        ImmutableDictionary<String, String>? description = null,
        ImmutableDictionary<String, JsonElement>? metadata = null)
    {
        mergedCommit = null;
        lock (gate)
        {
            if (disposed || mutating)
            {
                result = new(RoamingNetworkMergeStatus.Unavailable, "History is disposed or a callback is reentrant.", leftId, rightId);
                return false;
            }
            mutating = true;
            RoamingNetworkCommitId? selectedBase = null;
            ImmutableArray<RoamingNetworkCommitId> bases = [];
            POIThreeWayMerge? planner = null;
            try
            {
                if (!leftId.IsValid || !rightId.IsValid || commonAncestor is { IsValid: false })
                    throw new ArgumentException("Requested commit identities must be valid.");
                VerifyBoundary();
                if (!HasCompleteAncestry)
                {
                    var requested = new[] { leftId, rightId }.AsEnumerable();
                    if (commonAncestor is { } ancestor) requested = requested.Append(ancestor);
                    var missing = requested.Where(id => !entries.ContainsKey(id)).Distinct()
                        .OrderBy(id => id.ToString(), StringComparer.Ordinal).ToImmutableArray();
                    if (!missing.IsEmpty)
                    {
                        result = new(RoamingNetworkMergeStatus.HistoryRequired,
                            "Requested tips or ancestor are unavailable within this snapshot boundary; retrieve sufficient earlier history before merging.",
                            leftId, rightId, missingCommits: missing);
                        return false;
                    }
                }
                if (!entries.TryGetValue(leftId, out var leftEntry) || !entries.TryGetValue(rightId, out var rightEntry))
                    throw new ArgumentException("Both tips must already be retained in this history.");
                var leftAncestors = Ancestors(leftId);
                var rightAncestors = Ancestors(rightId);
                foreach (var id in leftAncestors.Union(rightAncestors).OrderBy(id => id.ToString(), StringComparer.Ordinal))
                {
                    VerifyCommit(entries[id].Commit);
                    VerifyBatch(entries[id].Commit);
                }
                if (leftAncestors.Contains(rightId))
                {
                    result = new(RoamingNetworkMergeStatus.AlreadyIntegrated,
                        "The right tip is already an ancestor of the left tip; no new commit was prepared.", leftId, rightId,
                        rightId, [rightId], afterETags: leftEntry.Snapshot.ETags);
                    return true;
                }
                var common = leftAncestors.Intersect(rightAncestors).ToHashSet();
                var predecessors = common.SelectMany(id => entries[id].Commit.Parents).ToHashSet();
                bases = common.Except(predecessors).OrderBy(id => id.ToString(), StringComparer.Ordinal).ToImmutableArray();
                if (bases.IsEmpty) throw new ArgumentException("The retained tips have no common ancestor.");
                if (commonAncestor is { } explicitBase)
                {
                    if (!bases.Contains(explicitBase)) throw new ArgumentException("The explicit ancestor must be one of the best common ancestors.");
                    selectedBase = explicitBase;
                }
                else if (bases.Length > 1)
                {
                    result = new(RoamingNetworkMergeStatus.AmbiguousAncestor,
                        "Multiple best common ancestors exist; explicitly choose one before preparing a merge.", leftId, rightId,
                        ancestors: bases);
                    return false;
                }
                else selectedBase = bases[0];
                if (merge && (String.IsNullOrWhiteSpace(mergedChangeSetId) || batchIds.ContainsKey(mergedChangeSetId)))
                    throw new ArgumentException("Preparation requires a fresh, nonempty mergedChangeSetId.");
                if (metadata?.ContainsKey(MergeMetadataProperty) == true)
                    throw new ArgumentException($"Metadata key '{MergeMetadataProperty}' is reserved for the merge record.");
                var timestamp = (createdAt ?? Later(leftEntry.Commit.CreatedAt, rightEntry.Commit.CreatedAt)).ToUniversalTime();
                var lifetimes = new Dictionary<RoamingNetworkCommitId, POILifetimeState>();
                var ancestorLifetime = Lifetime(selectedBase.Value, lifetimes);
                var leftLifetime = Lifetime(leftId, lifetimes);
                var rightLifetime = Lifetime(rightId, lifetimes);
                planner = new(entries[selectedBase.Value].Snapshot, leftEntry.Snapshot, rightEntry.Snapshot, resolveConflict, timestamp,
                              ancestorLifetime, leftLifetime, rightLifetime);
                var target = planner.Merge();
                var operations = target is null ? null : planner.Operations(target, timestamp);
                if (operations is null)
                {
                    result = new(RoamingNetworkMergeStatus.Conflicts, "Unresolved conflicts or candidate validation failures prevent preparation.",
                        leftId, rightId, selectedBase, bases, Report(planner));
                    return false;
                }
                var batch = leftEntry.Snapshot.CreateChangeSet(merge ? mergedChangeSetId! : "merge-preview", timestamp, operations.Value)
                    .WithDescription(description ?? ImmutableDictionary<String, String>.Empty)
                    .WithMetadata(metadata ?? ImmutableDictionary<String, JsonElement>.Empty)
                    .WithMetadata(MergeMetadataProperty, MergeRecord(selectedBase.Value, leftId, rightId, planner));
                if (merge) mergedCommit = RoamingNetworkCommit.Create(leftEntry.Commit, batch, [rightId]);
                result = new(merge ? RoamingNetworkMergeStatus.Prepared : RoamingNetworkMergeStatus.MergeAvailable,
                    merge ? "An unsigned merge commit was prepared against the left tip; sign and publish it explicitly." :
                            "The tips can be integrated; explicitly request preparation to create a merge commit.",
                        leftId, rightId, selectedBase, bases, Report(planner), batch.AfterETags,
                        operations.Value, planner.ReferenceTransitions);
                return true;
            }
            catch (Exception exception)
            {
                mergedCommit = null;
                var conflicts = planner is null ? ImmutableArray<RoamingNetworkMergeConflict>.Empty : Report(planner);
                result = new(RoamingNetworkMergeStatus.InvalidInput, exception.Message, leftId, rightId, selectedBase, bases, conflicts);
                return false;
            }
            finally { mutating = false; }
        }
    }

    private HashSet<RoamingNetworkCommitId> Ancestors(RoamingNetworkCommitId tip)
    {
        var result = new HashSet<RoamingNetworkCommitId>();
        var pending = new Stack<RoamingNetworkCommitId>();
        pending.Push(tip);
        while (pending.TryPop(out var id))
            if (result.Add(id) && !IsAnchor(id)) foreach (var parent in entries[id].Commit.Parents) pending.Push(parent);
        return result;
    }

    private POILifetimeState Lifetime(RoamingNetworkCommitId tip, Dictionary<RoamingNetworkCommitId, POILifetimeState> cache)
    {
        var pending = new Stack<Entry>();
        var current = tip;
        while (!cache.ContainsKey(current))
        {
            var entry = entries[current];
            if (IsAnchor(entry.Commit.Id))
            {
                cache[current] = POILifetimeState.Checkpoint(entry.Commit, entry.Snapshot);
                break;
            }
            pending.Push(entry);
            current = entry.Commit.Parents[0];
        }
        while (pending.TryPop(out var entry))
        {
            var parent = entry.Commit.Parents[0];
            cache[entry.Commit.Id] = cache[parent].Apply(entry.Commit, entries[parent].Snapshot);
        }
        return cache[tip];
    }

    private static DateTimeOffset Later(DateTimeOffset? left, DateTimeOffset? right)
    {
        var l = left ?? DateTimeOffset.UnixEpoch;
        var r = right ?? DateTimeOffset.UnixEpoch;
        return l >= r ? l : r;
    }

    private static ImmutableArray<RoamingNetworkMergeConflict> Report(POIThreeWayMerge planner)
        => planner.Conflicts.OrderBy(conflict => conflict.Path, StringComparer.Ordinal).
               ThenBy(conflict => conflict.RelatedEntity?.Type).ThenBy(conflict => conflict.RelatedEntity?.Id, StringComparer.Ordinal).
               ThenBy(conflict => conflict.RelatedEntity?.Scope, StringComparer.Ordinal).ToImmutableArray();

    private static JsonElement MergeRecord(RoamingNetworkCommitId ancestor, RoamingNetworkCommitId left,
                                           RoamingNetworkCommitId right, POIThreeWayMerge planner)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("Profile", ThreeWayMergeProfile);
            writer.WritePropertyName("Ancestor"); ancestor.Hash.WriteTo(writer);
            writer.WritePropertyName("Left"); left.Hash.WriteTo(writer);
            writer.WritePropertyName("Right"); right.Hash.WriteTo(writer);
            if (planner.LifetimeSelections.Any() || planner.Conflicts.Any(conflict =>
                    conflict.BaseLifetime != conflict.LeftLifetime || conflict.BaseLifetime != conflict.RightLifetime))
                writer.WriteString("LifetimeProfile", MergeLifetimeProfile);
            writer.WritePropertyName("Resolutions");
            writer.WriteStartArray();
            var sequence = 0;
            foreach (var conflict in planner.Conflicts.Where(conflict => conflict.Resolution is not null))
            {
                var resolution = conflict.Resolution!;
                writer.WriteStartObject();
                writer.WriteNumber("Sequence", sequence++);
                writer.WriteString("Path", conflict.Path);
                writer.WriteString("Kind", conflict.Kind.ToString());
                writer.WriteString("Choice", resolution.Choice.ToString());
                if (conflict.BaseLifetime != conflict.LeftLifetime || conflict.BaseLifetime != conflict.RightLifetime)
                {
                    WriteLifetime(writer, "BaseLifetime", conflict.BaseLifetime);
                    WriteLifetime(writer, "LeftLifetime", conflict.LeftLifetime);
                    WriteLifetime(writer, "RightLifetime", conflict.RightLifetime);
                }
                if (conflict.RelatedEntity is { } related)
                {
                    writer.WritePropertyName("RelatedEntity"); writer.WriteStartObject();
                    writer.WriteString("EntityType", related.Type.ToString()); writer.WriteString("EntityId", related.Id);
                    if (related.Scope is { } scope) writer.WriteString("Scope", scope);
                    writer.WriteEndObject();
                }
                if (resolution.Value is { } value) { writer.WritePropertyName("Value"); value.WriteTo(writer); }
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            if (planner.LifetimeSelections.Any())
            {
                writer.WritePropertyName("LifetimeSelections");
                writer.WriteStartArray();
                foreach (var selection in planner.LifetimeSelections)
                {
                    writer.WriteStartObject();
                    writer.WriteString("Path", selection.Key);
                    WriteLifetime(writer, "Origin", selection.Value);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            if (!planner.ReferenceTransitions.IsEmpty)
            {
                writer.WriteString("ReferenceTransitionProfile", MergeReferenceTransitionProfile);
                writer.WritePropertyName("ReferenceTransitions");
                writer.WriteStartArray();
                foreach (var transition in planner.ReferenceTransitions)
                {
                    writer.WriteStartObject();
                    writer.WritePropertyName("Consumer"); WriteEntity(writer, transition.Consumer);
                    writer.WriteString("PropertyName", transition.PropertyName);
                    writer.WritePropertyName("Targets"); writer.WriteStartArray();
                    foreach (var target in transition.Targets) WriteEntity(writer, target);
                    writer.WriteEndArray();
                    writer.WritePropertyName("BeforeValue"); transition.BeforeValue.WriteTo(writer);
                    if (transition.DetachedValue is { } detached)
                    { writer.WritePropertyName("DetachedValue"); detached.WriteTo(writer); }
                    if (transition.AfterValue is { } after)
                    { writer.WritePropertyName("AfterValue"); after.WriteTo(writer); }
                    writer.WritePropertyName("DetachOperationIndices"); JsonSerializer.Serialize(writer, transition.DetachOperationIndices);
                    writer.WritePropertyName("RestoreOperationIndices"); JsonSerializer.Serialize(writer, transition.RestoreOperationIndices);
                    writer.WriteEndObject();
                }
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }

    private static void WriteLifetime(Utf8JsonWriter writer, String name, RoamingNetworkLifetimeOrigin? origin)
    {
        if (origin is null) return;
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        writer.WritePropertyName("CommitId"); origin.CommitId.Hash.WriteTo(writer);
        writer.WriteNumber("OperationIndex", origin.OperationIndex);
        writer.WriteEndObject();
    }

    private static void WriteEntity(Utf8JsonWriter writer, InfrastructureEntityKey entity)
    {
        writer.WriteStartObject();
        writer.WriteString("EntityType", entity.Type.ToString());
        writer.WriteString("EntityId", entity.Id);
        if (entity.Scope is { } scope) writer.WriteString("Scope", scope);
        writer.WriteEndObject();
    }
}
