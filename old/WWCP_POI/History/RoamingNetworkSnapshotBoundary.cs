/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// A proposed original chain identity and signed snapshot anchor with omitted earlier ancestry.
/// Construction validates structure; an application must explicitly authorize the pair.
/// </summary>
public sealed class RoamingNetworkSnapshotBoundary
{
    /// <summary>
    /// The original checkpoint ID, whose ancestry cannot be independently proved from this boundary.
    /// </summary>
    public RoamingNetworkCommitId Checkpoint { get; }

    /// <summary>
    /// The original snapshot envelope, including its unchanged predecessor and equal peer signatures.
    /// </summary>
    public RoamingNetworkCommit SnapshotCommit { get; }

    /// <summary>
    /// The retained anchor ID; earlier parents are external to this replica's history.
    /// </summary>
    public RoamingNetworkCommitId Anchor => SnapshotCommit.Id;

    /// <summary>
    /// Construct a proposal without treating hashes or supplied signatures as application authority.
    /// </summary>
    public RoamingNetworkSnapshotBoundary(RoamingNetworkCommitId checkpoint, RoamingNetworkCommit snapshotCommit)
    {
        ArgumentNullException.ThrowIfNull(snapshotCommit);
        if (!checkpoint.IsValid || snapshotCommit.Kind != RoamingNetworkCommitKind.Snapshot ||
            checkpoint == snapshotCommit.Id || snapshotCommit.Signatures.IsEmpty)
            throw new ArgumentException("A boundary requires a distinct original checkpoint and a signed snapshot commit.");
        Checkpoint = checkpoint;
        SnapshotCommit = snapshotCommit;
    }
}
