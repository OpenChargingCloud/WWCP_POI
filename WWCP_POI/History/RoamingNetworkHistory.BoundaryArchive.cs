/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text;
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

    private String BoundaryJSON(ImmutableDictionary<RoamingNetworkCommitId, Entry> source,
                                RoamingNetworkCommitId root, RoamingNetworkCommitId headId)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
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
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private Byte[] BoundaryArchive(ImmutableDictionary<RoamingNetworkCommitId, Entry> source,
                                    RoamingNetworkCommitId root, RoamingNetworkCommitId headId,
                                    ImmutableArray<RoamingNetworkRetentionReceipt>? receipts = null)
    {
        var catalog = receipts ?? retentionReceipts;
        var fields = new List<(String Key, CBORValue Value)> {
            ("Profile", CBORValue.FromText(catalog.IsEmpty ? BoundaryArchiveProfile : RetentionArchiveProfile)),
            ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
            ("CheckpointId", checkpointId.ToCBOR()),
            ("SnapshotCommit", source[root].Commit.ToCBORValue()),
            ("Commits", CBORValue.FromArray(OrderedEntries(source, root).Where(entry => entry.Commit.Id != root)
                .Select(entry => entry.Commit.ToCBORValue()))),
            ("Head", headId.ToCBOR()) };
        if (!catalog.IsEmpty) fields.Add(("RetentionReceipts", CBORValue.FromArray(catalog.Select(receipt => receipt.ToCBORValue()))));
        return RoamingNetworkCommit.Map(fields.ToArray()).ToByteArray(CBORWriterOptions.Canonical);
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
        if (authorizeBoundary is null || verifyCommitSignature is null)
            throw new ArgumentException("Snapshot-boundary recovery requires an explicit boundary policy and trusted signature verifier.");
        var history = FromSnapshot(checkpoint, snapshot, authorizeBoundary, verifyCommitSignature,
                                   verifyBatchSignature, authorizeCommit);
        try
        {
            var seen = new HashSet<RoamingNetworkCommitId> { snapshot.Id };
            foreach (var commit in commits)
            {
                if (!seen.Add(commit.Id)) throw new ArgumentException("Archive contains duplicate commit identities.");
                if (!history.TryStoreCommit(commit, out var result)) throw new ArgumentException(result.Error);
            }
            if (!history.entries.TryGetValue(headId, out var entry)) throw new ArgumentException("Archive head is not retained.");
            history.head = new(entry.Commit, RoamingNetwork.Parse(entry.Snapshot.ToJSON()));
            history.retentionReceipts = receipts.IsDefault ? [] : receipts;
            history.archivedCommits = IndexReceipts(history.retentionReceipts, checkpoint);
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

    private static RoamingNetworkHistory ParseBoundaryCBOR(CBORValue value,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeBoundary)
    {
        var hasReceipts = RoamingNetworkCommit.Text(value.AsMap().Single(entry => RoamingNetworkCommit.Text(entry.Key) == "Profile").Value) == RetentionArchiveProfile;
        var fields = RoamingNetworkCommit.Fields(value, BoundaryFields(hasReceipts));
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        var receipts = hasReceipts ? fields["RetentionReceipts"].AsArray().Select(RoamingNetworkRetentionReceipt.ParseCBORValue).ToImmutableArray() : [];
        if (hasReceipts && receipts.IsEmpty) throw new ArgumentException("A retention archive requires a nonempty receipt catalog.");
        return RestoreBoundary(new(ETag.Parse(fields["CheckpointId"])), RoamingNetworkCommit.ParseCBORValue(fields["SnapshotCommit"]),
            fields["Commits"].AsArray().Select(RoamingNetworkCommit.ParseCBORValue), new(ETag.Parse(fields["Head"])),
            verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeBoundary, receipts);
    }
}
