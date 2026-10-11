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
    /// Prepare an unsigned full snapshot of the expected current head without publishing it.
    /// Administrators choose the timestamp, descriptions and metadata, then add peer signatures.
    /// Publish the result with TryPublish using the same expected head.
    /// </summary>
    public RoamingNetworkCommit PrepareSnapshot(RoamingNetworkCommitId expectedHead, DateTimeOffset createdAt,
        ImmutableDictionary<String, String>? description = null,
        ImmutableDictionary<String, JsonElement>? metadata = null)
    {
        lock (gate)
        {
            BeginMutation();
            try
            {
                VerifyBoundary();
                if (!expectedHead.IsValid || expectedHead != head.Id)
                    throw new ArgumentException("The expected head differs from the published head.");
                var commit = RoamingNetworkCommit.CreateSnapshot(head.Commit, head.Snapshot, createdAt, description, metadata);
                ValidateParents(commit);
                return commit;
            }
            finally { mutating = false; }
        }
    }

    private RoamingNetworkDataSnapshot ApplyCommit(RoamingNetworkDataSnapshot source, RoamingNetworkCommit commit)
        => commit.Snapshot is not null ? source.AdvanceSnapshotRevision() :
           source.ApplyChangeSet(commit.ChangeSet ?? throw new ArgumentException("A transition requires a payload."), verifyBatchSignature);

    private RoamingNetwork ApplyCommit(RoamingNetwork source, RoamingNetworkCommit commit)
        => commit.Snapshot is not null ? source.DeriveRetainedSnapshot(ApplyCommit(source.DataSnapshot, commit), []) :
           source.ApplyChangeSet(commit.ChangeSet ?? throw new ArgumentException("A transition requires a payload."), verifyBatchSignature);

    private static void ValidateSnapshotParent(RoamingNetworkCommit commit, Entry source)
    {
        if (commit.Revision != checked(source.Commit.Revision + 1) ||
            commit.AppliedChangeSetId != source.Commit.AppliedChangeSetId ||
            !commit.StateETags.SequenceEqual(source.Commit.StateETags) ||
            !commit.Snapshot!.State.ToCanonicalJSON().AsSpan().SequenceEqual(source.Snapshot.ToCanonicalJSON()))
            throw new ArgumentException("A snapshot must reproduce the exact first-parent state, advance revision once and retain the last batch ID.");
    }
}
