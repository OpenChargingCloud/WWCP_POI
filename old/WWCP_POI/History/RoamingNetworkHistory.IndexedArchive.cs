/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    private static readonly String[] CompleteArchiveFields =
        ["Profile", "ContentProfile", "Checkpoint", "CheckpointCommit", "Commits", "Head"];

    private static ImmutableArray<RoamingNetworkCommit> ReadIndexedCommits(ReadOnlySpan<Byte> bytes,
        POIArchiveCBORIndex index, POIArchiveReadProgress progress = default)
    {
        var result = ImmutableArray.CreateBuilder<RoamingNetworkCommit>(index.Commits.Length);
        foreach (var part in index.Commits)
        {
            progress.Check(POIArchiveReadStage.CommitModel, part.Offset);
            result.Add(RoamingNetworkCommit.ParseCBOR(POIArchiveCBORIndex.Slice(bytes, part)));
            progress.Check(POIArchiveReadStage.CommitModel, part.Offset + part.Length);
        }
        return result.MoveToImmutable();
    }

    private static ImmutableArray<RoamingNetworkRetentionReceipt> ReadIndexedReceipts(ReadOnlySpan<Byte> bytes,
        POIArchiveCBORIndex index, POIArchiveReadProgress progress = default)
    {
        var result = ImmutableArray.CreateBuilder<RoamingNetworkRetentionReceipt>(index.Receipts.Length);
        foreach (var part in index.Receipts)
        {
            progress.Check(POIArchiveReadStage.ReceiptModel, part.Offset);
            result.Add(RoamingNetworkRetentionReceipt.ParseCBOR(POIArchiveCBORIndex.Slice(bytes, part)));
            progress.Check(POIArchiveReadStage.ReceiptModel, part.Offset + part.Length);
        }
        if (result.Count == 0) throw new ArgumentException("A retention archive requires a nonempty receipt catalog.");
        return result.MoveToImmutable();
    }

    private static RoamingNetworkHistory ParseIndexedArchive(ReadOnlySpan<Byte> bytes, POIArchiveCBORIndex index,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeBoundary, POIArchiveReadProgress progress)
    {
        var profile = RoamingNetworkCommit.Text(index.Value(bytes, "Profile"));
        if (profile is BoundaryArchiveProfile or RetentionArchiveProfile)
        {
            var hasReceipts = profile == RetentionArchiveProfile;
            index.RequireFields(BoundaryFields(hasReceipts));
            POIContentProfile.Require(RoamingNetworkCommit.Text(index.Value(bytes, "ContentProfile")));
            var receipts = hasReceipts ? ReadIndexedReceipts(bytes, index, progress) : [];
            var checkpoint = new RoamingNetworkCommitId(ETag.Parse(index.Value(bytes, "CheckpointId")));
            var root = ReadIndexedRoot(bytes, index, "SnapshotCommit", progress);
            var head = new RoamingNetworkCommitId(ETag.Parse(index.Value(bytes, "Head")));
            return RestoreIndexedBoundary(bytes, index, checkpoint, root, head,
                verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeBoundary, receipts, progress);
        }
        index.RequireFields(CompleteArchiveFields);
        if (profile is not (ArchiveProfile or SnapshotArchiveProfile)) throw new ArgumentException("Unsupported history profile.");
        POIContentProfile.Require(RoamingNetworkCommit.Text(index.Value(bytes, "ContentProfile")));
        var commits = ReadIndexedCommits(bytes, index, progress);
        RequireArchiveProfile(profile, commits);
        return RestoreCore(ReadIndexedState(bytes, index, progress),
            ReadIndexedRoot(bytes, index, "CheckpointCommit", progress), commits,
            new(ETag.Parse(index.Value(bytes, "Head"))), verifyBatchSignature, verifyCommitSignature, authorizeCommit, progress);
    }

    private static RoamingNetworkHistory RestoreIndexedBoundary(ReadOnlySpan<Byte> bytes, POIArchiveCBORIndex index,
        RoamingNetworkCommitId checkpoint, RoamingNetworkCommit root, RoamingNetworkCommitId head,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeBoundary,
        ImmutableArray<RoamingNetworkRetentionReceipt> receipts, POIArchiveReadProgress progress = default)
    {
        var cursor = new POIArchiveCommitCursor(bytes, index.Commits, progress);
        try { return RestoreBoundaryCore(checkpoint, root, ref cursor, head, verifyBatchSignature,
            verifyCommitSignature, authorizeCommit, authorizeBoundary, receipts, progress); }
        finally { cursor.Dispose(); }
    }

    private static RoamingNetworkDataSnapshot ReadIndexedState(ReadOnlySpan<Byte> bytes, POIArchiveCBORIndex index,
        POIArchiveReadProgress progress)
    {
        progress.Check(POIArchiveReadStage.RootState);
        var state = RoamingNetworkDataSnapshot.ParseCBOR(index.Slice(bytes, "Checkpoint"));
        progress.Check(POIArchiveReadStage.RootState);
        return state;
    }

    private static RoamingNetworkCommit ReadIndexedRoot(ReadOnlySpan<Byte> bytes, POIArchiveCBORIndex index,
        String name, POIArchiveReadProgress progress)
    {
        progress.Check(POIArchiveReadStage.CommitModel);
        var root = RoamingNetworkCommit.ParseCBOR(index.Slice(bytes, name));
        progress.Check(POIArchiveReadStage.CommitModel);
        return root;
    }

}
