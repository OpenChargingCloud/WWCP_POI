/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    /// <summary>
    /// A partial-history archive whose signed snapshot root retains its original external parent.
    /// The original checkpoint claim requires a fresh explicit boundary authorization on recovery.
    /// </summary>
    public const String BoundaryArchiveProfile = "wwcp-poi-history-v3";

    /// <summary>
    /// A snapshot-boundary archive also preserving the hash-only catalog of explicit pruning receipts.
    /// </summary>
    public const String RetentionArchiveProfile = "wwcp-poi-history-v4";

    private String CurrentBoundaryArchiveProfile => retentionReceipts.IsEmpty ? BoundaryArchiveProfile : RetentionArchiveProfile;

    private void WriteBoundaryJSON(Utf8JsonWriter writer, ImmutableDictionary<RoamingNetworkCommitId, Entry> source,
                                     RoamingNetworkCommitId root, RoamingNetworkCommitId headId)
    {
        writer.WriteStartObject();
        writer.WriteString("Profile", CurrentBoundaryArchiveProfile);
        writer.WriteString("ContentProfile", POIContentProfile.Id);
        writer.WritePropertyName("CheckpointId"); checkpointId.Hash.WriteTo(writer);
        writer.WritePropertyName("SnapshotCommit"); source[root].Commit.WriteTo(writer);
        writer.WritePropertyName("Commits"); writer.WriteStartArray();
        foreach (var entry in OrderedEntries(source, root))
            if (entry.Commit.Id != root) entry.Commit.WriteTo(writer);
        writer.WriteEndArray();
        writer.WritePropertyName("Head"); headId.Hash.WriteTo(writer);
        if (!retentionReceipts.IsEmpty)
        {
            writer.WritePropertyName("RetentionReceipts"); writer.WriteStartArray();
            foreach (var receipt in retentionReceipts) receipt.WriteTo(writer);
            writer.WriteEndArray();
        }
        writer.WriteEndObject();
    }

    private void WriteBoundaryArchive(Stream destination, ImmutableDictionary<RoamingNetworkCommitId, Entry> source,
                                       RoamingNetworkCommitId root, RoamingNetworkCommitId headId,
                                       ImmutableArray<RoamingNetworkRetentionReceipt>? receipts = null)
    {
        var catalog = receipts ?? retentionReceipts;
        var fields = new List<(String Name, Action<POIArchiveCBORWriter> Write)> {
            ("Profile", value => value.Text(catalog.IsEmpty ? BoundaryArchiveProfile : RetentionArchiveProfile)),
            ("ContentProfile", value => value.Text(POIContentProfile.Id)),
            ("CheckpointId", value => value.Value(checkpointId.ToCBOR())),
            ("SnapshotCommit", value => source[root].Commit.WriteArchiveCBOR(value)),
            ("Commits", value => value.Array(source.Count - 1, OrderedEntries(source, root).Where(entry => entry.Commit.Id != root),
                (output, entry) => entry.Commit.WriteArchiveCBOR(output))),
            ("Head", value => value.Value(headId.ToCBOR())) };
        if (!catalog.IsEmpty) fields.Add(("RetentionReceipts", value => value.Array(catalog.Length, catalog,
            (output, receipt) => receipt.WriteArchiveCBOR(output))));
        using var writer = new POIArchiveCBORWriter(destination);
        writer.Map(fields.ToArray());
        writer.Complete();
    }

    private static String[] BoundaryFields(Boolean hasReceipts)
        => hasReceipts ? ["Profile", "ContentProfile", "CheckpointId", "SnapshotCommit", "Commits", "Head", "RetentionReceipts"] :
                         ["Profile", "ContentProfile", "CheckpointId", "SnapshotCommit", "Commits", "Head"];

    private static RoamingNetworkHistory RestoreBoundary(RoamingNetworkCommitId checkpoint,
        RoamingNetworkCommit snapshot, IEnumerable<RoamingNetworkCommit> commits, RoamingNetworkCommitId headId,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeBoundary,
        ImmutableArray<RoamingNetworkRetentionReceipt> receipts = default)
    {
        var cursor = new POIArchiveCommitCursor(commits);
        try { return RestoreBoundaryCore(checkpoint, snapshot, ref cursor, headId,
            verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeBoundary, receipts); }
        finally { cursor.Dispose(); }
    }

    private static RoamingNetworkHistory RestoreBoundaryCore(RoamingNetworkCommitId checkpoint,
        RoamingNetworkCommit snapshot, ref POIArchiveCommitCursor commits, RoamingNetworkCommitId headId,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeBoundary,
        ImmutableArray<RoamingNetworkRetentionReceipt> receipts, POIArchiveReadProgress progress = default)
    {
        if (authorizeBoundary is null || verifyCommitSignature is null)
            throw new ArgumentException("Snapshot-boundary recovery requires an explicit boundary policy and trusted signature verifier.");
        using var preparation = POICanonicalPreparation.Enter();
        progress.Check(POIArchiveReadStage.RootState);
        var history = FromSnapshot(checkpoint, snapshot, authorizeBoundary, verifyCommitSignature,
                                   verifyBatchSignature, authorizeCommit);
        try
        {
            progress.Check(POIArchiveReadStage.RootState);
            progress.Check(POIArchiveReadStage.Replay);
            var seen = new HashSet<RoamingNetworkCommitId> { snapshot.Id };
            while (commits.MoveNext(out var commit))
            {
                progress.Check(POIArchiveReadStage.Replay, seen.Count);
                if (!seen.Add(commit.Id)) throw new ArgumentException("Archive contains duplicate commit identities.");
                if (!history.TryStoreCommit(commit, out var result)) throw new ArgumentException(result.Error);
            }
            progress.Check(POIArchiveReadStage.HeadState);
            if (!history.entries.TryGetValue(headId, out var entry)) throw new ArgumentException("Archive head is not retained.");
            history.head = new(entry.Commit, ReferenceEquals(history.head.Snapshot, entry.Snapshot)
                ? history.head.Network : RoamingNetwork.ParseSnapshot(entry.Snapshot));
            progress.Check(POIArchiveReadStage.HeadState);
            history.retentionReceipts = receipts.IsDefault ? [] : receipts;
            history.archivedCommits = IndexReceipts(history.retentionReceipts, checkpoint);
            progress.Check(POIArchiveReadStage.HeadState);
            return history;
        }
        catch { history.Dispose(); throw; }
    }

    private static RoamingNetworkHistory ParseBoundaryJSON(JsonElement value,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeBoundary)
    {
        var hasReceipts = value.GetProperty("Profile").GetString() == RetentionArchiveProfile;
        RoamingNetworkCommit.RequireFields(value, BoundaryFields(hasReceipts));
        POIContentProfile.Require(value.GetProperty("ContentProfile").GetString());
        var receipts = hasReceipts ? value.GetProperty("RetentionReceipts").EnumerateArray()
            .Select(RoamingNetworkRetentionReceipt.Parse).ToImmutableArray() : [];
        if (hasReceipts && receipts.IsEmpty) throw new ArgumentException("A retention archive requires a nonempty receipt catalog.");
        return RestoreBoundary(JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("CheckpointId")),
            RoamingNetworkCommit.Parse(value.GetProperty("SnapshotCommit")),
            value.GetProperty("Commits").EnumerateArray().Select(RoamingNetworkCommit.Parse),
            JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Head")),
            verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeBoundary, receipts);
    }

}
