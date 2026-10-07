/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
    /// The versioned archive contract containing one checkpoint, all commits and a head reference.
    /// </summary>
    public const String ArchiveProfile = "wwcp-poi-history-v1";

    /// <summary>
    /// The complete-history archive profile permitting full snapshot links in the original chain.
    /// </summary>
    public const String SnapshotArchiveProfile = "wwcp-poi-history-v2";

    private static String ArchiveProfileFor(ImmutableDictionary<RoamingNetworkCommitId, Entry> source)
        => source.Values.Any(entry => entry.Commit.Kind == RoamingNetworkCommitKind.Snapshot) ? SnapshotArchiveProfile : ArchiveProfile;

    private static void RequireArchiveProfile(String? profile, IEnumerable<RoamingNetworkCommit> commits)
    {
        var expected = commits.Any(commit => commit.Kind == RoamingNetworkCommitKind.Snapshot) ? SnapshotArchiveProfile : ArchiveProfile;
        if (profile != expected) throw new ArgumentException("Unsupported or inconsistent history profile.");
    }

    /// <summary>
    /// Export static history and original peer envelopes as JSON; runtime data is excluded.
    /// </summary>
    public String ToJSON()
    {
        lock (gate)
        {
            if (!HasCompleteAncestry) return BoundaryJSON(entries, anchorId, head.Id);
            using var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                writer.WriteStartObject();
                writer.WriteString("Profile", ArchiveProfileFor(entries));
                writer.WriteString("ContentProfile", POIContentProfile.Id);
                writer.WritePropertyName("Checkpoint"); entries[checkpointId].Snapshot.WriteTo(writer);
                writer.WritePropertyName("CheckpointCommit"); entries[checkpointId].Commit.WriteTo(writer);
                writer.WritePropertyName("Commits");
                writer.WriteStartArray();
                foreach (var entry in OrderedEntries(entries))
                    if (entry.Commit.Id != checkpointId) entry.Commit.WriteTo(writer);
                writer.WriteEndArray();
                writer.WritePropertyName("Head"); head.Id.Hash.WriteTo(writer);
                writer.WriteEndObject();
            }
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }

    /// <summary>
    /// Export deterministic CBOR with native snapshots, commit maps and binary digest tuples.
    /// </summary>
    public Byte[] ToCBOR()
    {
        lock (gate) return Archive(entries, head.Id);
    }

    private Byte[] Archive(ImmutableDictionary<RoamingNetworkCommitId, Entry> source, RoamingNetworkCommitId headId)
        => !HasCompleteAncestry ? BoundaryArchive(source, anchorId, headId) : RoamingNetworkCommit.Map(
            ("Profile", CBORValue.FromText(ArchiveProfileFor(source))),
            ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
            ("Checkpoint", CBORValue.Parse(source[checkpointId].Snapshot.ToCBOR(IncludeVersionMetadata: true))),
            ("CheckpointCommit", source[checkpointId].Commit.ToCBORValue()),
            ("Commits", CBORValue.FromArray(OrderedEntries(source).Where(entry => entry.Commit.Id != checkpointId)
                                                   .Select(entry => entry.Commit.ToCBORValue()))),
            ("Head", headId.ToCBOR())).ToByteArray(CBORWriterOptions.Canonical);

    /// <summary>
    /// Recover an in-memory archive by validating the checkpoint and replaying every retained branch.
    /// Signature verification and authorization are supplied afresh by the receiver.
    /// </summary>
    public static RoamingNetworkHistory Parse(String json,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null)
    {
        using var document = JsonDocument.Parse(json);
        var value = document.RootElement;
        if (value.GetProperty("Profile").GetString() is BoundaryArchiveProfile or RetentionArchiveProfile)
            return ParseBoundaryJSON(value, verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeSnapshotBoundary);
        RoamingNetworkCommit.RequireFields(value, "Profile", "ContentProfile", "Checkpoint", "CheckpointCommit", "Commits", "Head");
        if (value.GetProperty("Profile").GetString() is not (ArchiveProfile or SnapshotArchiveProfile))
            throw new ArgumentException("Unsupported history profile.");
        POIContentProfile.Require(value.GetProperty("ContentProfile").GetString());
        var commits = value.GetProperty("Commits").EnumerateArray().Select(RoamingNetworkCommit.Parse).ToImmutableArray();
        RequireArchiveProfile(value.GetProperty("Profile").GetString(), commits);
        return Restore(RoamingNetworkDataSnapshot.Parse(value.GetProperty("Checkpoint").GetRawText()),
                       RoamingNetworkCommit.Parse(value.GetProperty("CheckpointCommit")),
                       commits,
                       JsonSerializer.Deserialize<RoamingNetworkCommitId>(value.GetProperty("Head")),
                       verifyBatchSignature, verifyCommitSignature, authorizeCommit);
    }

    /// <summary>
    /// Recover CBOR history, verifying identity, ancestry, revision and before/after state for every commit.
    /// </summary>
    public static RoamingNetworkHistory ParseCBOR(ReadOnlySpan<Byte> bytes,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null)
    {
        var value = CBORValue.Parse(bytes);
        if (RoamingNetworkCommit.Text(value.AsMap().Single(entry => RoamingNetworkCommit.Text(entry.Key) == "Profile").Value) is BoundaryArchiveProfile or RetentionArchiveProfile)
            return ParseBoundaryCBOR(value, verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeSnapshotBoundary);
        var fields = RoamingNetworkCommit.Fields(value, "Profile", "ContentProfile", "Checkpoint", "CheckpointCommit", "Commits", "Head");
        if (RoamingNetworkCommit.Text(fields["Profile"]) is not (ArchiveProfile or SnapshotArchiveProfile))
            throw new ArgumentException("Unsupported history profile.");
        POIContentProfile.Require(RoamingNetworkCommit.Text(fields["ContentProfile"]));
        var commits = fields["Commits"].AsArray().Select(RoamingNetworkCommit.ParseCBORValue).ToImmutableArray();
        RequireArchiveProfile(RoamingNetworkCommit.Text(fields["Profile"]), commits);
        return Restore(RoamingNetworkDataSnapshot.ParseCBOR(fields["Checkpoint"].ToByteArray(CBORWriterOptions.Canonical)),
                       RoamingNetworkCommit.ParseCBORValue(fields["CheckpointCommit"]),
                       commits,
                       new(ETag.Parse(fields["Head"])), verifyBatchSignature, verifyCommitSignature, authorizeCommit);
    }

    private static RoamingNetworkHistory Restore(RoamingNetworkDataSnapshot snapshot, RoamingNetworkCommit checkpoint,
        IEnumerable<RoamingNetworkCommit> commits, RoamingNetworkCommitId headId,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit)
    {
        var history = new RoamingNetworkHistory(RoamingNetwork.Parse(snapshot.ToJSON()), verifyBatchSignature, verifyCommitSignature, authorizeCommit);
        try
        {
            if (history.checkpointId != checkpoint.Id || checkpoint.Kind != RoamingNetworkCommitKind.Checkpoint)
                throw new ArgumentException("Checkpoint commit does not match the archived static checkpoint.");
            if (!history.TryStoreCommit(checkpoint, out var rootResult)) throw new ArgumentException(rootResult.Error);
            var seen = new HashSet<RoamingNetworkCommitId> { checkpoint.Id };
            foreach (var commit in commits)
            {
                if (!seen.Add(commit.Id)) throw new ArgumentException("Archive contains duplicate commit identities.");
                if (!history.TryStoreCommit(commit, out var result)) throw new ArgumentException(result.Error);
            }
            if (!history.entries.TryGetValue(headId, out var headEntry)) throw new ArgumentException("Archive head is not retained.");
            history.head = new(headEntry.Commit, RoamingNetwork.Parse(headEntry.Snapshot.ToJSON()));
            return history;
        }
        catch { history.Dispose(); throw; }
    }

    /// <summary>
    /// Create a new CBOR archive with an exclusive writer lease; an existing archive is never overwritten.
    /// Subsequent successful storage/publication persists before returning and before changing the head.
    /// </summary>
    public static RoamingNetworkHistory CreatePersistent(String path, RoamingNetwork network,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null)
    {
        var history = new RoamingNetworkHistory(network, verifyBatchSignature, verifyCommitSignature, authorizeCommit);
        try
        {
            history.AcquireArchive(path);
            if (File.Exists(history.archivePath)) throw new IOException("The history archive already exists; open it for recovery.");
            history.Persist(history.entries, history.head.Id);
            return history;
        }
        catch { history.Dispose(); throw; }
    }

    /// <summary>
    /// Acquire the archive's writer lease and recover all retained commits before accepting publication.
    /// </summary>
    public static RoamingNetworkHistory Open(String path,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null)
    {
        var resolved = Path.GetFullPath(path);
        var lease = AcquireLease(resolved);
        try
        {
            var history = ParseCBOR(File.ReadAllBytes(resolved), verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeSnapshotBoundary);
            history.archivePath = resolved;
            history.archiveLease = lease;
            return history;
        }
        catch { lease.Dispose(); throw; }
    }

    private void AcquireArchive(String path)
    {
        var resolved = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(resolved)!);
        archiveLease = AcquireLease(resolved);
        archivePath = resolved;
    }

    private static FileStream AcquireLease(String path)
        => new(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    private void Persist(ImmutableDictionary<RoamingNetworkCommitId, Entry> source, RoamingNetworkCommitId headId)
    {
        if (archivePath is null) return;
        Persist(Archive(source, headId));
    }

    private void Persist(Byte[] bytes)
    {
        if (archivePath is null) return;
        var temporary = archivePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            ArchiveWriteObserver?.Invoke(ArchiveWriteStage.BeforeTemporaryWrite);
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes);
                file.Flush(flushToDisk: true);
            }
            ArchiveWriteObserver?.Invoke(ArchiveWriteStage.TemporaryFileFlushed);
            File.Move(temporary, archivePath, overwrite: true);
            ArchiveWriteObserver?.Invoke(ArchiveWriteStage.ArchiveReplaced);
        }
        finally
        {
            // Only this exact sibling temporary file belongs to this write attempt.
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
