/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    private RoamingNetworkCommitId anchorId;
    private Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary;

    /// <summary>
    /// The original chain checkpoint, including when its data and ancestry are not retained locally.
    /// </summary>
    public RoamingNetworkCommitId CheckpointId => checkpointId;

    /// <summary>
    /// The local replay root, either the original checkpoint or an explicitly trusted snapshot.
    /// </summary>
    public RoamingNetworkCommitId AnchorId { get { lock (gate) return anchorId; } }

    /// <summary>
    /// Whether retained ancestry reaches the original checkpoint rather than a trusted snapshot boundary.
    /// </summary>
    public Boolean HasCompleteAncestry { get { lock (gate) return anchorId == checkpointId; } }

    /// <summary>
    /// The current snapshot boundary envelope, or null for complete-history replicas.
    /// </summary>
    public RoamingNetworkSnapshotBoundary? SnapshotBoundary
    {
        get { lock (gate) return HasCompleteAncestry ? null : new(checkpointId, entries[anchorId].Commit); }
    }

    private RoamingNetworkHistory(RoamingNetworkSnapshotBoundary boundary,
        Func<RoamingNetworkSnapshotBoundary, Boolean> authorizeBoundary,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean> verifyCommit,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatch,
        Func<RoamingNetworkCommit, Boolean>? authorize)
        : this(RoamingNetwork.ParseSnapshot(boundary.SnapshotCommit.Snapshot!.State), verifyBatch, verifyCommit, authorize)
    {
        checkpointId = boundary.Checkpoint;
        anchorId = boundary.Anchor;
        authorizeSnapshotBoundary = authorizeBoundary;
        entries = ImmutableDictionary<RoamingNetworkCommitId, Entry>.Empty.Add(anchorId,
            new(boundary.SnapshotCommit, head.Snapshot));
        batchIds = ImmutableDictionary.Create<String, RoamingNetworkCommitId>(StringComparer.Ordinal);
        if (boundary.SnapshotCommit.AppliedChangeSetId is { } batch) batchIds = batchIds.Add(batch, anchorId);
        head = new(boundary.SnapshotCommit, head.Network);
    }

    /// <summary>
    /// Start a separate replica at an explicitly authorized signed snapshot with fresh local runtime.
    /// The policy must authorize the chain/anchor pair and enforce its own rollback constraints.
    /// Snapshot signatures alone do not prove the omitted chain or its original checkpoint.
    /// </summary>
    public static RoamingNetworkHistory FromSnapshot(RoamingNetworkCommitId checkpoint,
        RoamingNetworkCommit snapshotCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean> authorizeBoundary,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean> verifyCommitSignature,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null)
    {
        ArgumentNullException.ThrowIfNull(authorizeBoundary);
        ArgumentNullException.ThrowIfNull(verifyCommitSignature);
        var history = new RoamingNetworkHistory(new(checkpoint, snapshotCommit), authorizeBoundary,
            verifyCommitSignature, verifyBatchSignature, authorizeCommit);
        try
        {
            history.mutating = true;
            history.VerifyBoundary();
            MatchState(snapshotCommit, history.head.Snapshot);
            history.mutating = false;
            return history;
        }
        catch { history.mutating = false; history.Dispose(); throw; }
    }

    /// <summary>
    /// Create a new exclusive persistent snapshot-boundary replica without overwriting an archive.
    /// </summary>
    public static RoamingNetworkHistory CreatePersistentFromSnapshot(String path, RoamingNetworkCommitId checkpoint,
        RoamingNetworkCommit snapshotCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean> authorizeBoundary,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean> verifyCommitSignature,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null)
    {
        var history = FromSnapshot(checkpoint, snapshotCommit, authorizeBoundary, verifyCommitSignature,
                                   verifyBatchSignature, authorizeCommit);
        try
        {
            history.AcquireArchive(path);
            if (File.Exists(history.archivePath)) throw new IOException("The history archive already exists; open it for recovery.");
            history.Persist(history.entries, history.head.Id);
            return history;
        }
        catch { history.Dispose(); throw; }
    }

    private void VerifyBoundary()
    {
        if (!HasCompleteAncestry) VerifyCommit(entries[anchorId].Commit);
    }

    // Only the one explicitly authorized root may retain an external parent.
    private Boolean IsAnchor(RoamingNetworkCommitId id) => id == anchorId;

    private RoamingNetworkSnapshotBoundary? AvailableBoundary()
    {
        foreach (var entry in FirstParentLine(head.Id))
            if (entry.Commit.Kind == RoamingNetworkCommitKind.Snapshot && !entry.Commit.Signatures.IsEmpty &&
                TryAncestorsWithin(head.Id, entry.Commit.Id, out _, out _))
                return new(checkpointId, entry.Commit);
        return null;
    }

    private Boolean TryAncestorsWithin(RoamingNetworkCommitId tip, RoamingNetworkCommitId anchor,
        out HashSet<RoamingNetworkCommitId> ancestry, out ImmutableArray<RoamingNetworkCommitId> missing)
    {
        ancestry = [];
        var unresolved = new HashSet<RoamingNetworkCommitId>();
        var pending = new Stack<RoamingNetworkCommitId>();
        pending.Push(tip);
        while (pending.TryPop(out var id))
        {
            if (!ancestry.Add(id)) continue;
            if (id == anchor) continue;
            if (!entries.TryGetValue(id, out var entry) || IsAnchor(id) || entry.Commit.Parents.IsEmpty)
            {
                unresolved.Add(id);
                continue;
            }
            foreach (var parent in entry.Commit.Parents) pending.Push(parent);
        }
        missing = unresolved.OrderBy(id => id.ToString(), StringComparer.Ordinal).ToImmutableArray();
        return missing.IsEmpty && ancestry.Contains(anchor);
    }
}
