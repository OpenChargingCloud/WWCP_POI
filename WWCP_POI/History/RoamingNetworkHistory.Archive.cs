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
            using var stream = new MemoryStream();
            WriteArchiveJSON(stream, entries, head.Id);
            return Encoding.UTF8.GetString(stream.GetBuffer().AsSpan(0, checked((Int32)stream.Length)));
        }
    }

    /// <summary>
    /// Write the exact JSON archive to a writable stream without closing or flushing it.
    /// The export holds the history gate; reentrant mutation is rejected. A stream failure may
    /// leave partial output, while retained history, the published head and runtime remain unchanged.
    /// </summary>
    public void WriteJSON(Stream destination)
    {
        RequireArchiveStream(destination);
        lock (gate)
        {
            if (disposed || mutating) throw new InvalidOperationException("History is disposed or a mutation callback is reentrant.");
            mutating = true;
            try { WriteArchiveJSON(destination, entries, head.Id); }
            finally { mutating = false; }
        }
    }

    private void WriteArchiveJSON(Stream destination, ImmutableDictionary<RoamingNetworkCommitId, Entry> source,
                                   RoamingNetworkCommitId headId)
    {
        using var output = new POIArchiveStreamBuffer(destination);
        using var writer = new Utf8JsonWriter(output);
        WriteArchiveJSON(writer, source, headId);
        writer.Flush();
        output.Flush();
    }

    private void WriteArchiveJSON(Utf8JsonWriter writer, ImmutableDictionary<RoamingNetworkCommitId, Entry> source,
                                   RoamingNetworkCommitId headId)
    {
        if (!HasCompleteAncestry) { WriteBoundaryJSON(writer, source, anchorId, headId); return; }
        writer.WriteStartObject();
        writer.WriteString("Profile", ArchiveProfileFor(source));
        writer.WriteString("ContentProfile", POIContentProfile.Id);
        writer.WritePropertyName("Checkpoint"); source[checkpointId].Snapshot.WriteTo(writer);
        writer.WritePropertyName("CheckpointCommit"); source[checkpointId].Commit.WriteTo(writer);
        writer.WritePropertyName("Commits"); writer.WriteStartArray();
        foreach (var entry in OrderedEntries(source))
            if (entry.Commit.Id != checkpointId) entry.Commit.WriteTo(writer);
        writer.WriteEndArray();
        writer.WritePropertyName("Head"); headId.Hash.WriteTo(writer);
        writer.WriteEndObject();
    }

    /// <summary>
    /// Export deterministic CBOR with native snapshots, commit maps and binary digest tuples.
    /// </summary>
    public Byte[] ToCBOR()
    {
        lock (gate) return Archive(entries, head.Id);
    }

    /// <summary>
    /// Write the exact deterministic CBOR archive to a writable stream, leaving it open and unflushed.
    /// Outer maps, commits, receipts and preflight-validated POI snapshots are emitted directly.
    /// POI and ChangeSet payloads preflight before direct output. Failed output does not change history.
    /// </summary>
    public void WriteCBOR(Stream destination)
    {
        RequireArchiveStream(destination);
        lock (gate)
        {
            if (disposed || mutating) throw new InvalidOperationException("History is disposed or a mutation callback is reentrant.");
            mutating = true;
            try { WriteArchiveCBOR(destination, entries, head.Id); }
            finally { mutating = false; }
        }
    }

    private static void RequireArchiveStream(Stream destination)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite) throw new ArgumentException("An archive destination must be writable.", nameof(destination));
    }

    private Byte[] Archive(ImmutableDictionary<RoamingNetworkCommitId, Entry> source, RoamingNetworkCommitId headId)
    {
        using var stream = new MemoryStream();
        WriteArchiveCBOR(stream, source, headId);
        return stream.ToArray();
    }

    private void WriteArchiveCBOR(Stream destination, ImmutableDictionary<RoamingNetworkCommitId, Entry> source,
                                   RoamingNetworkCommitId headId)
    {
        if (!HasCompleteAncestry) { WriteBoundaryArchive(destination, source, anchorId, headId); return; }
        using var writer = new POIArchiveCBORWriter(destination);
        writer.Map(
            ("Profile", value => value.Text(ArchiveProfileFor(source))),
            ("ContentProfile", value => value.Text(POIContentProfile.Id)),
            ("Checkpoint", value => value.POI(source[checkpointId].Snapshot)),
            ("CheckpointCommit", value => source[checkpointId].Commit.WriteArchiveCBOR(value)),
            ("Commits", value => value.Array(source.Count - 1, OrderedEntries(source).Where(entry => entry.Commit.Id != checkpointId),
                (output, entry) => entry.Commit.WriteArchiveCBOR(output))),
            ("Head", value => value.Value(headId.ToCBOR())));
        writer.Complete();
    }

    private (ETag Digest, Int64 Length) ArchiveDigest(ImmutableDictionary<RoamingNetworkCommitId, Entry> source,
                                                      RoamingNetworkCommitId headId)
    {
        using var stream = new POIArchiveHashStream();
        WriteArchiveCBOR(stream, source, headId);
        return (stream.Complete(), stream.Length);
    }

    /// <summary>
    /// Recover an in-memory archive by validating the checkpoint and replaying every retained branch.
    /// Signature verification and authorization are supplied afresh by the receiver.
    /// Local byte and container limits are checked before document materialization and replay.
    /// </summary>
    public static RoamingNetworkHistory Parse(String json,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null,
        RoamingNetworkHistoryLimits? limits = null)
    {
        using var document = JsonDocument.Parse(CheckJSONArchive(json, limits ?? new()));
        using var preparation = POICanonicalPreparation.Enter();
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
    /// Local byte and container limits are checked before document materialization and replay.
    /// </summary>
    public static RoamingNetworkHistory ParseCBOR(ReadOnlySpan<Byte> bytes,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null,
        RoamingNetworkHistoryLimits? limits = null)
    {
        return ParseCBORCore(bytes, verifyBatchSignature, verifyCommitSignature, authorizeCommit,
            authorizeSnapshotBoundary, limits ?? new(), default);
    }

    private static RoamingNetworkHistory ParseCBORCore(ReadOnlySpan<Byte> bytes,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary,
        RoamingNetworkHistoryLimits limits, POIArchiveReadProgress progress)
    {
        CheckCBORArchiveCore(bytes, limits, progress);
        var index = POIArchiveCBORIndex.Read(bytes, progress);
        using var preparation = POICanonicalPreparation.Enter();
        return ParseIndexedArchive(bytes, index, verifyBatchSignature, verifyCommitSignature,
            authorizeCommit, authorizeSnapshotBoundary, progress);
    }

    private static RoamingNetworkHistory Restore(RoamingNetworkDataSnapshot snapshot, RoamingNetworkCommit checkpoint,
        IEnumerable<RoamingNetworkCommit> commits, RoamingNetworkCommitId headId,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit)
    {
        return RestoreCore(snapshot, checkpoint, commits, headId, verifyBatchSignature,
            verifyCommitSignature, authorizeCommit, default);
    }

    private static RoamingNetworkHistory RestoreCore(RoamingNetworkDataSnapshot snapshot, RoamingNetworkCommit checkpoint,
        IEnumerable<RoamingNetworkCommit> commits, RoamingNetworkCommitId headId,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit, POIArchiveReadProgress progress)
    {
        using var preparation = POICanonicalPreparation.Enter();
        progress.Check(POIArchiveReadStage.RootState);
        var history = new RoamingNetworkHistory(RoamingNetwork.ParseSnapshot(snapshot), verifyBatchSignature, verifyCommitSignature, authorizeCommit);
        try
        {
            progress.Check(POIArchiveReadStage.RootState);
            if (history.checkpointId != checkpoint.Id || checkpoint.Kind != RoamingNetworkCommitKind.Checkpoint)
                throw new ArgumentException("Checkpoint commit does not match the archived static checkpoint.");
            progress.Check(POIArchiveReadStage.Replay);
            if (!history.TryStoreCommit(checkpoint, out var rootResult)) throw new ArgumentException(rootResult.Error);
            var seen = new HashSet<RoamingNetworkCommitId> { checkpoint.Id };
            foreach (var commit in commits)
            {
                progress.Check(POIArchiveReadStage.Replay, seen.Count);
                if (!seen.Add(commit.Id)) throw new ArgumentException("Archive contains duplicate commit identities.");
                if (!history.TryStoreCommit(commit, out var result)) throw new ArgumentException(result.Error);
            }
            progress.Check(POIArchiveReadStage.HeadState);
            if (!history.entries.TryGetValue(headId, out var headEntry)) throw new ArgumentException("Archive head is not retained.");
            history.head = new(headEntry.Commit, ReferenceEquals(history.head.Snapshot, headEntry.Snapshot)
                ? history.head.Network : RoamingNetwork.ParseSnapshot(headEntry.Snapshot));
            progress.Check(POIArchiveReadStage.HeadState);
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
    /// Check known file length before mapping and local container limits before decoding and replay.
    /// Release the mapped input before returning the writer; cancellation applies only to recovery.
    /// Failure releases the lease and leaves the archive unchanged.
    /// </summary>
    public static RoamingNetworkHistory Open(String path,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null,
        RoamingNetworkHistoryLimits? limits = null, CancellationToken cancellationToken = default)
    {
        limits ??= new();
        cancellationToken.ThrowIfCancellationRequested();
        var resolved = Path.GetFullPath(path);
        var lease = AcquireLease(resolved);
        RoamingNetworkHistory? history = null;
        try
        {
            using (var input = new POIArchiveMappedFile(resolved, limits))
                history = RestoreCapturedInput(input.Bytes, limits, verifyBatchSignature, verifyCommitSignature,
                    authorizeCommit, authorizeSnapshotBoundary, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            history.archivePath = resolved;
            history.archiveLease = lease;
            return history;
        }
        catch { history?.Dispose(); lease.Dispose(); throw; }
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
        Persist(stream => WriteArchiveCBOR(stream, source, headId));
    }

    private void Persist(Action<Stream> writeArchive)
    {
        if (archivePath is null) return;
        var temporary = archivePath + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            ArchiveWriteObserver?.Invoke(ArchiveWriteStage.BeforeTemporaryWrite);
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                writeArchive(file);
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
