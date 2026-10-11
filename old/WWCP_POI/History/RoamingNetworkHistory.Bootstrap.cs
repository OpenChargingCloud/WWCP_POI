/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    /// <summary>
    /// Freeze the complete static archive under the history gate for bounded bootstrap transfer.
    /// Original checkpoint, head, all retained branches and peer signatures are preserved.
    /// Export encodes directly into bounded frozen fragments; final replay retains decoded models and states.
    /// </summary>
    public RoamingNetworkBootstrapSource CreateBootstrap(Int32 chunkBytes = 64 * 1024, RoamingNetworkBootstrapLimits? limits = null)
    {
        lock (gate)
        {
            if (disposed || mutating) throw new InvalidOperationException("History is disposed or a mutation callback is reentrant.");
            limits ??= new();
            if (chunkBytes < 1 || chunkBytes > limits.MaxChunkBytes) throw new ArgumentException("Invalid chunk size.");
            limits.HistoryLimits.Require(RoamingNetworkHistoryLimitKind.RetainedCommits, entries.Count);
            if (!HasCompleteAncestry) RequireCatalogLimits(limits.HistoryLimits);
            return new(stream => WriteArchiveCBOR(stream, entries, head.Id), checkpointId, head.Id, entries.Count, chunkBytes, limits,
                HasCompleteAncestry ? ArchiveProfileFor(entries) : CurrentBoundaryArchiveProfile, anchorId);
        }
    }

    /// <summary>
    /// Freeze a signed snapshot and the requested tip's complete retained suffix without deleting history.
    /// Every parent path must terminate at this snapshot; older cross-boundary merge dependencies reject export.
    /// The receiver must authorize the original chain/anchor pair before preview or activation.
    /// </summary>
    public RoamingNetworkBootstrapSource CreateSnapshotBootstrap(RoamingNetworkCommitId snapshotId,
        RoamingNetworkCommitId? targetTip = null, Int32 chunkBytes = 64 * 1024, RoamingNetworkBootstrapLimits? limits = null)
    {
        lock (gate)
        {
            if (disposed || mutating) throw new InvalidOperationException("History is disposed or a mutation callback is reentrant.");
            limits ??= new();
            if (!entries.TryGetValue(snapshotId, out var snapshot)) throw new KeyNotFoundException("Unknown snapshot.");
            _ = new RoamingNetworkSnapshotBoundary(checkpointId, snapshot.Commit);
            var tip = targetTip ?? head.Id;
            if (!entries.ContainsKey(tip)) throw new KeyNotFoundException("Unknown target tip.");
            if (!TryAncestorsWithin(tip, snapshotId, out var ancestry, out var missing))
                throw new ArgumentException("The selected snapshot does not cover all target parent paths; retrieve earlier history: " + String.Join(", ", missing));
            if (chunkBytes < 1 || chunkBytes > limits.MaxChunkBytes) throw new ArgumentException("Invalid chunk size.");
            limits.HistoryLimits.Require(RoamingNetworkHistoryLimitKind.RetainedCommits, ancestry.Count);
            RequireCatalogLimits(limits.HistoryLimits);
            var selected = entries.Where(entry => ancestry.Contains(entry.Key)).ToImmutableDictionary();
            return new(stream => WriteBoundaryArchive(stream, selected, snapshotId, tip), checkpointId, tip, selected.Count, chunkBytes, limits,
                CurrentBoundaryArchiveProfile, snapshotId);
        }
    }

    internal static RoamingNetworkHistory RestoreBootstrap(ReadOnlySpan<Byte> bytes, RoamingNetworkBootstrapManifest manifest,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary,
        RoamingNetworkHistoryLimits limits, POIArchiveReadProgress progress = default)
    {
        CheckCBORArchiveCore(bytes, limits, progress);
        var index = POIArchiveCBORIndex.Read(bytes, progress);
        POIArchiveCBORIndex.RequireCanonical(bytes, progress);
        using var preparation = POICanonicalPreparation.Enter();
        if (manifest.ArchiveProfile is BoundaryArchiveProfile or RetentionArchiveProfile)
        {
            var hasReceipts = manifest.ArchiveProfile == RetentionArchiveProfile;
            index.RequireFields(BoundaryFields(hasReceipts));
            if (RoamingNetworkCommit.Text(index.Value(bytes, "Profile")) != manifest.ArchiveProfile ||
                (Int64) index.Commits.Length + 1 != manifest.CommitCount ||
                new RoamingNetworkCommitId(ETag.Parse(index.Value(bytes, "CheckpointId"))) != manifest.Checkpoint ||
                new RoamingNetworkCommitId(ETag.Parse(index.Value(bytes, "Head"))) != manifest.Head)
                throw new ArgumentException("Boundary archive contract, chain, head or count differs from the manifest.");
            POIContentProfile.Require(RoamingNetworkCommit.Text(index.Value(bytes, "ContentProfile")));
            var snapshot = ReadIndexedRoot(bytes, index, "SnapshotCommit", progress);
            if (snapshot.Id != manifest.Anchor) throw new ArgumentException("Snapshot anchor differs from the manifest.");
            var receipts = hasReceipts ? ReadIndexedReceipts(bytes, index, progress) : [];
            return RestoreIndexedBoundary(bytes, index, manifest.Checkpoint, snapshot, manifest.Head,
                verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeSnapshotBoundary, receipts, progress);
        }
        index.RequireFields(CompleteArchiveFields);
        if (RoamingNetworkCommit.Text(index.Value(bytes, "Profile")) != manifest.ArchiveProfile)
            throw new ArgumentException("Archive profile differs from the manifest.");
        POIContentProfile.Require(RoamingNetworkCommit.Text(index.Value(bytes, "ContentProfile")));
        if ((Int64) index.Commits.Length + 1 != manifest.CommitCount)
            throw new ArgumentException("Archive commit count differs from the manifest.");
        var checkpoint = ReadIndexedRoot(bytes, index, "CheckpointCommit", progress);
        var headId = new RoamingNetworkCommitId(ETag.Parse(index.Value(bytes, "Head")));
        if (checkpoint.Id != manifest.Checkpoint || headId != manifest.Head)
            throw new ArgumentException("Archive checkpoint or head differs from the manifest.");
        var commits = ReadIndexedCommits(bytes, index, progress);
        RequireArchiveProfile(manifest.ArchiveProfile, commits);
        return RestoreCore(ReadIndexedState(bytes, index, progress),
            checkpoint, commits, headId, verifyBatchSignature, verifyCommitSignature, authorizeCommit, progress);
    }

    internal void PersistBootstrap(String path, Action<ArchiveWriteStage>? observer = null)
    {
        ArchiveWriteObserver = observer;
        try
        {
            AcquireArchive(path);
            if (File.Exists(archivePath)) throw new IOException("Bootstrap activation requires a new archive path; an existing archive cannot be replaced.");
            Persist(entries, head.Id);
        }
        finally { ArchiveWriteObserver = null; }
    }

    internal static RoamingNetworkHistory RecoverBootstrapArchive(String path, ReadOnlySpan<Byte> expectedBytes,
        RoamingNetworkBootstrapManifest manifest,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary,
        RoamingNetworkHistoryLimits limits, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var resolved = Path.GetFullPath(path);
        var lease = AcquireLease(resolved);
        RoamingNetworkHistory? history = null;
        try
        {
            using (var stored = new POIArchiveMappedFile(resolved, limits))
            {
                if (!stored.Bytes.SequenceEqual(expectedBytes))
                    throw new ArgumentException("The existing activation archive differs from the exact manifest-bound archive.");
                history = RestoreCapturedInput(stored.Bytes, limits, verifyBatchSignature, verifyCommitSignature,
                    authorizeCommit, authorizeSnapshotBoundary, cancellationToken, manifest);
            }
            cancellationToken.ThrowIfCancellationRequested();
            history.archivePath = resolved; history.archiveLease = lease;
            return history;
        }
        catch { history?.Dispose(); lease.Dispose(); throw; }
    }
}
