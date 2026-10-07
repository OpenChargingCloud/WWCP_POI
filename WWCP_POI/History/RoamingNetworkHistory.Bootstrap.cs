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
    /// Export and final replay currently materialize a complete archive in memory.
    /// </summary>
    public RoamingNetworkBootstrapSource CreateBootstrap(Int32 chunkBytes = 64 * 1024, RoamingNetworkBootstrapLimits? limits = null)
    {
        lock (gate)
        {
            if (disposed || mutating) throw new InvalidOperationException("History is disposed or a mutation callback is reentrant.");
            limits ??= new();
            if (chunkBytes < 1 || chunkBytes > limits.MaxChunkBytes || entries.Count > limits.MaxCommits)
                throw new ArgumentException("Invalid chunk size or excessive retained commit count.");
            return new(Archive(entries, head.Id), checkpointId, head.Id, entries.Count, chunkBytes, limits,
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
            if (chunkBytes < 1 || chunkBytes > limits.MaxChunkBytes || ancestry.Count > limits.MaxCommits)
                throw new ArgumentException("Invalid chunk size or excessive retained commit count.");
            var selected = entries.Where(entry => ancestry.Contains(entry.Key)).ToImmutableDictionary();
            return new(BoundaryArchive(selected, snapshotId, tip), checkpointId, tip, selected.Count, chunkBytes, limits,
                CurrentBoundaryArchiveProfile, snapshotId);
        }
    }

    internal static RoamingNetworkHistory RestoreBootstrap(ReadOnlySpan<Byte> bytes, RoamingNetworkBootstrapManifest manifest,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary)
    {
        var value = CBORValue.Parse(bytes);
        if (!value.ToByteArray(CBORWriterOptions.Canonical).AsSpan().SequenceEqual(bytes))
            throw new ArgumentException("Bootstrap requires the deterministic CBOR archive representation.");
        if (manifest.ArchiveProfile is BoundaryArchiveProfile or RetentionArchiveProfile)
        {
            var hasReceipts = manifest.ArchiveProfile == RetentionArchiveProfile;
            var partial = RoamingNetworkCommit.Fields(value, BoundaryFields(hasReceipts));
            if (RoamingNetworkCommit.Text(partial["Profile"]) != manifest.ArchiveProfile ||
                (Int64) partial["Commits"].AsArray().Count + 1 != manifest.CommitCount ||
                new RoamingNetworkCommitId(ETag.Parse(partial["CheckpointId"])) != manifest.Checkpoint ||
                new RoamingNetworkCommitId(ETag.Parse(partial["Head"])) != manifest.Head)
                throw new ArgumentException("Boundary archive contract, chain, head or count differs from the manifest.");
            POIContentProfile.Require(RoamingNetworkCommit.Text(partial["ContentProfile"]));
            var snapshot = RoamingNetworkCommit.ParseCBORValue(partial["SnapshotCommit"]);
            if (snapshot.Id != manifest.Anchor) throw new ArgumentException("Snapshot anchor differs from the manifest.");
            var receipts = hasReceipts ? partial["RetentionReceipts"].AsArray().Select(RoamingNetworkRetentionReceipt.ParseCBORValue).ToImmutableArray() : [];
            if (hasReceipts && receipts.IsEmpty) throw new ArgumentException("A retention archive requires a nonempty receipt catalog.");
            return RestoreBoundary(manifest.Checkpoint, snapshot, partial["Commits"].AsArray().Select(RoamingNetworkCommit.ParseCBORValue),
                manifest.Head, verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeSnapshotBoundary, receipts);
        }
        var fields = RoamingNetworkCommit.Fields(value, "Profile", "ContentProfile", "Checkpoint", "CheckpointCommit", "Commits", "Head");
        if (RoamingNetworkCommit.Text(fields["Profile"]) != manifest.ArchiveProfile) throw new ArgumentException("Archive profile differs from the manifest.");
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        if ((Int64) fields["Commits"].AsArray().Count + 1 != manifest.CommitCount)
            throw new ArgumentException("Archive commit count differs from the manifest.");
        var checkpoint = RoamingNetworkCommit.ParseCBORValue(fields["CheckpointCommit"]);
        var headId = new RoamingNetworkCommitId(ETag.Parse(fields["Head"]));
        if (checkpoint.Id != manifest.Checkpoint || headId != manifest.Head)
            throw new ArgumentException("Archive checkpoint or head differs from the manifest.");
        var commits = fields["Commits"].AsArray().Select(RoamingNetworkCommit.ParseCBORValue).ToImmutableArray();
        RequireArchiveProfile(manifest.ArchiveProfile, commits);
        return Restore(RoamingNetworkDataSnapshot.ParseCBOR(fields["Checkpoint"].ToByteArray(CBORWriterOptions.Canonical)),
            checkpoint, commits, headId,
            verifyBatchSignature, verifyCommitSignature, authorizeCommit);
    }

    internal void PersistBootstrap(String path)
    {
        AcquireArchive(path);
        if (File.Exists(archivePath)) throw new IOException("Bootstrap activation requires a new archive path; an existing archive cannot be replaced.");
        Persist(entries, head.Id);
    }
}
