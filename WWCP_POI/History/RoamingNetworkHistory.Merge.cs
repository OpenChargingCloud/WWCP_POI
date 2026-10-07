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
                var timestamp = (createdAt ?? Later(leftEntry.Commit.ChangeSet?.CreatedAt, rightEntry.Commit.ChangeSet?.CreatedAt)).ToUniversalTime();
                planner = new(entries[selectedBase.Value].Snapshot, leftEntry.Snapshot, rightEntry.Snapshot, resolveConflict, timestamp);
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
                    leftId, rightId, selectedBase, bases, Report(planner), batch.AfterETags);
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
            if (result.Add(id)) foreach (var parent in entries[id].Commit.Parents) pending.Push(parent);
        return result;
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
            writer.WriteEndObject();
        }
        using var document = JsonDocument.Parse(stream.ToArray());
        return document.RootElement.Clone();
    }
}
