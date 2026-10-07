/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

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
            return new(Archive(entries, head.Id), checkpointId, head.Id, entries.Count, chunkBytes, limits);
        }
    }

    internal static RoamingNetworkHistory RestoreBootstrap(ReadOnlySpan<Byte> bytes, RoamingNetworkBootstrapManifest manifest,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit)
    {
        var value = CBORValue.Parse(bytes);
        if (!value.ToByteArray(CBORWriterOptions.Canonical).AsSpan().SequenceEqual(bytes))
            throw new ArgumentException("Bootstrap requires the deterministic CBOR archive representation.");
        var fields = RoamingNetworkCommit.Fields(value, "Profile", "ContentProfile", "Checkpoint", "CheckpointCommit", "Commits", "Head");
        if (RoamingNetworkCommit.Text(fields["Profile"]) != ArchiveProfile) throw new ArgumentException("Unsupported history profile.");
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        if ((Int64) fields["Commits"].AsArray().Count + 1 != manifest.CommitCount)
            throw new ArgumentException("Archive commit count differs from the manifest.");
        var checkpoint = RoamingNetworkCommit.ParseCBORValue(fields["CheckpointCommit"]);
        var headId = new RoamingNetworkCommitId(ETag.Parse(fields["Head"]));
        if (checkpoint.Id != manifest.Checkpoint || headId != manifest.Head)
            throw new ArgumentException("Archive checkpoint or head differs from the manifest.");
        return Restore(RoamingNetworkDataSnapshot.ParseCBOR(fields["Checkpoint"].ToByteArray(CBORWriterOptions.Canonical)),
            checkpoint, fields["Commits"].AsArray().Select(RoamingNetworkCommit.ParseCBORValue), headId,
            verifyBatchSignature, verifyCommitSignature, authorizeCommit);
    }

    internal void PersistBootstrap(String path)
    {
        AcquireArchive(path);
        if (File.Exists(archivePath)) throw new IOException("Bootstrap activation requires a new archive path; an existing archive cannot be replaced.");
        Persist(entries, head.Id);
    }
}
