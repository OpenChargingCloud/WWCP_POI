/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Security.Cryptography;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// The outcome of staging or explicitly activating a bootstrap archive.
/// </summary>
public enum RoamingNetworkBootstrapOutcome
{
    Accepted,
    AlreadyStored,
    ActivationAvailable,
    Activated,
    Incomplete,
    OutOfOrder,
    ManifestMismatch,
    InvalidData,
    PersistenceFailure,
    Unavailable
}

/// <summary>
/// The fixed manifest and next required fragment; failure never returns an activated history.
/// </summary>
public sealed record RoamingNetworkBootstrapResult(RoamingNetworkBootstrapOutcome Outcome,
    RoamingNetworkBootstrapManifest Manifest, Int32 NextChunk, String? Error = null);

/// <summary>
/// Disk staging with an exclusive writer lease, atomic fragment receipts and restartable progress.
/// Complete validation and explicit activation produce a separate new history with fresh runtime.
/// </summary>
public sealed class RoamingNetworkBootstrapReceiver : IDisposable
{
    private readonly Object gate = new();
    private readonly String directory;
    private readonly FileStream lease;
    private readonly RoamingNetworkBootstrapLimits limits;
    private Int32 nextChunk;
    private Boolean operating;
    private Boolean disposed;

    /// <summary>
    /// The immutable manifest binding this staging directory to one frozen transfer.
    /// </summary>
    public RoamingNetworkBootstrapManifest Manifest { get; }

    /// <summary>
    /// The next required ordered fragment, or ChunkCount when all fragments are stored.
    /// Resume requests must also name Manifest.Id.
    /// </summary>
    public Int32 NextChunk { get { lock (gate) return nextChunk; } }

    private RoamingNetworkBootstrapReceiver(String directory, FileStream lease,
        RoamingNetworkBootstrapManifest manifest, RoamingNetworkBootstrapLimits limits)
    {
        this.directory = directory; this.lease = lease; this.limits = limits; Manifest = manifest;
    }

    /// <summary>
    /// Create staging in a dedicated empty directory. Existing transfers require Open.
    /// The manifest must come from the application's selected sender or trust policy.
    /// </summary>
    public static RoamingNetworkBootstrapReceiver Create(String directory, RoamingNetworkBootstrapManifest manifest,
        RoamingNetworkBootstrapLimits? limits = null)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        limits ??= new(); manifest.RequireLimits(limits);
        var resolved = Path.GetFullPath(directory);
        Directory.CreateDirectory(resolved);
        var lease = AcquireLease(resolved);
        try
        {
            if (Directory.EnumerateFileSystemEntries(resolved).Any(path => Path.GetFileName(path) != "bootstrap.lock"))
                throw new IOException("Bootstrap staging requires a dedicated empty directory; open an existing transfer to resume.");
            var receiver = new RoamingNetworkBootstrapReceiver(resolved, lease, manifest, limits);
            receiver.WriteAtomic(Path.Combine(resolved, "manifest.cbor"), manifest.ToCBOR());
            return receiver;
        }
        catch { lease.Dispose(); throw; }
    }

    /// <summary>
    /// Reopen a transfer only against its independently retained manifest identity.
    /// Recompute every completed fragment digest before reporting resumed progress.
    /// Orphan temporary files from interrupted writes are ignored and never acknowledged.
    /// </summary>
    public static RoamingNetworkBootstrapReceiver Open(String directory, ETag expectedManifest,
        RoamingNetworkBootstrapLimits? limits = null)
    {
        limits ??= new();
        var resolved = Path.GetFullPath(directory);
        var lease = AcquireLease(resolved);
        try
        {
            var path = Path.Combine(resolved, "manifest.cbor");
            var manifest = RoamingNetworkBootstrapManifest.ParseCBOR(ReadBounded(path, limits.MaxManifestBytes), limits);
            if (manifest.Id != expectedManifest) throw new ArgumentException("Staging manifest differs from the expected transfer.");
            var receiver = new RoamingNetworkBootstrapReceiver(resolved, lease, manifest, limits);
            receiver.Scan();
            return receiver;
        }
        catch { lease.Dispose(); throw; }
    }

    /// <summary>
    /// Accept only the next fragment or a byte-identical verified duplicate.
    /// The receipt is installed after Flush(true); invalid bytes and future positions are rejected.
    /// </summary>
    public Boolean TryAcceptChunk(RoamingNetworkBootstrapChunk chunk, out RoamingNetworkBootstrapResult result)
    {
        lock (gate)
        {
            if (!Begin(out result)) return false;
            try
            {
                ArgumentNullException.ThrowIfNull(chunk);
                if (chunk.Manifest != Manifest.Id) return Fail(RoamingNetworkBootstrapOutcome.ManifestMismatch, "Fragment belongs to another manifest.", out result);
                chunk.RequireLimits(limits);
                if (chunk.Index >= Manifest.ChunkCount) return Fail(RoamingNetworkBootstrapOutcome.InvalidData, "Fragment index exceeds the manifest.", out result);
                Validate(chunk.Index, chunk.Data.AsSpan());
                if (chunk.Index > nextChunk) return Fail(RoamingNetworkBootstrapOutcome.OutOfOrder, "Request NextChunk before sending a later fragment.", out result);
                if (chunk.Index < nextChunk)
                {
                    var stored = ReadBounded(ChunkPath(chunk.Index), Manifest.Length(chunk.Index));
                    Validate(chunk.Index, stored);
                    if (!stored.AsSpan().SequenceEqual(chunk.Data.AsSpan())) throw new ArgumentException("Duplicate fragment differs from its stored receipt.");
                    result = Result(RoamingNetworkBootstrapOutcome.AlreadyStored); return true;
                }
                WriteAtomic(ChunkPath(chunk.Index), chunk.Data.AsSpan());
                nextChunk++;
                result = Result(RoamingNetworkBootstrapOutcome.Accepted); return true;
            }
            catch (IOException exception) { return Fail(RoamingNetworkBootstrapOutcome.PersistenceFailure, exception.Message, out result); }
            catch (UnauthorizedAccessException exception) { return Fail(RoamingNetworkBootstrapOutcome.PersistenceFailure, exception.Message, out result); }
            catch (Exception exception) { return Fail(RoamingNetworkBootstrapOutcome.InvalidData, exception.Message, out result); }
            finally { operating = false; }
        }
    }

    /// <summary>
    /// Verify all staged bytes, the archive digest, profiles, original identities, signatures,
    /// authorization, every branch and state transition. The default is a validation preview.
    /// activate=true returns a new history, optionally persisted to a new archive path.
    /// Every call obtains current trust policy afresh; no validated preview is cached as authority.
    /// </summary>
    public Boolean TryActivate(ETag expectedManifest, out RoamingNetworkHistory? history,
        out RoamingNetworkBootstrapResult result, Boolean activate = false, String? archivePath = null,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkBootstrapManifest, Boolean>? authorizeBootstrap = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null)
    {
        lock (gate)
        {
            history = null;
            if (!Begin(out result)) return false;
            RoamingNetworkHistory? validated = null;
            try
            {
                if (expectedManifest != Manifest.Id) return Fail(RoamingNetworkBootstrapOutcome.ManifestMismatch, "Activation requires the expected manifest identity.", out result);
                if (authorizeBootstrap is not null && !authorizeBootstrap(Manifest))
                    return Fail(RoamingNetworkBootstrapOutcome.InvalidData, "The bootstrap manifest was not authorized.", out result);
                Scan();
                if (nextChunk != Manifest.ChunkCount) return Fail(RoamingNetworkBootstrapOutcome.Incomplete, "Transfer is incomplete; request NextChunk to resume.", out result);
                var bytes = new Byte[Manifest.ArchiveBytes];
                for (var index = 0; index < Manifest.ChunkCount; index++)
                {
                    var data = ReadBounded(ChunkPath(index), Manifest.Length(index));
                    Validate(index, data);
                    data.CopyTo(bytes, index * Manifest.ChunkBytes);
                }
                if (ETag.Compute(ETagFormat.CBOR, bytes) != Manifest.ArchiveETag)
                    throw new ArgumentException("Complete archive digest differs from the manifest.");
                validated = RoamingNetworkHistory.RestoreBootstrap(bytes, Manifest, verifyBatchSignature, verifyCommitSignature, authorizeCommit, authorizeSnapshotBoundary);
                if (!activate)
                {
                    result = Result(RoamingNetworkBootstrapOutcome.ActivationAvailable); return true;
                }
                if (archivePath is not null) validated.PersistBootstrap(archivePath);
                history = validated; validated = null;
                result = Result(RoamingNetworkBootstrapOutcome.Activated); return true;
            }
            catch (IOException exception) { return Fail(RoamingNetworkBootstrapOutcome.PersistenceFailure, exception.Message, out result); }
            catch (UnauthorizedAccessException exception) { return Fail(RoamingNetworkBootstrapOutcome.PersistenceFailure, exception.Message, out result); }
            catch (Exception exception) { return Fail(RoamingNetworkBootstrapOutcome.InvalidData, exception.Message, out result); }
            finally { validated?.Dispose(); operating = false; }
        }
    }

    private void Scan()
    {
        var prefix = 0;
        for (var index = 0; index < Manifest.ChunkCount; index++)
        {
            if (!File.Exists(ChunkPath(index))) continue;
            if (index != prefix) throw new ArgumentException("Stored fragments contain a gap; staging is not a valid ordered prefix.");
            Validate(index, ReadBounded(ChunkPath(index), Manifest.Length(index)));
            prefix++;
        }
        nextChunk = prefix;
    }

    private void Validate(Int32 index, ReadOnlySpan<Byte> bytes)
    {
        if (bytes.Length != Manifest.Length(index) || !SHA256.HashData(bytes).AsSpan().SequenceEqual(Manifest.ChunkDigests[index].AsSpan()))
            throw new ArgumentException("Fragment length or SHA-256 digest differs from the manifest.");
    }

    private static Byte[] ReadBounded(String path, Int32 maxBytes)
    {
        using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (file.Length > maxBytes) throw new ArgumentException("Staged file exceeds its declared byte limit.");
        var bytes = new Byte[checked((Int32) file.Length)];
        file.ReadExactly(bytes);
        return bytes;
    }

    private void WriteAtomic(String path, ReadOnlySpan<Byte> bytes)
    {
        var temporary = path + ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            WriteObserver?.Invoke(BootstrapWriteStage.BeforeTemporaryWrite);
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                file.Write(bytes); file.Flush(flushToDisk: true);
            }
            WriteObserver?.Invoke(BootstrapWriteStage.TemporaryFileFlushed);
            File.Move(temporary, path, overwrite: false);
            // Receipt exists durably before in-memory progress advances.
        }
        finally
        {
            try { File.Delete(temporary); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private String ChunkPath(Int32 index) => Path.Combine(directory, index.ToString("D8", System.Globalization.CultureInfo.InvariantCulture) + ".chunk");
    private static FileStream AcquireLease(String directory)
        => new(Path.Combine(directory, "bootstrap.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);

    private Boolean Begin(out RoamingNetworkBootstrapResult result)
    {
        if (disposed || operating) return Fail(RoamingNetworkBootstrapOutcome.Unavailable, "Receiver is disposed or a callback is reentrant.", out result);
        operating = true; result = Result(RoamingNetworkBootstrapOutcome.Unavailable); return true;
    }

    private RoamingNetworkBootstrapResult Result(RoamingNetworkBootstrapOutcome outcome, String? error = null)
        => new(outcome, Manifest, nextChunk, error);

    private Boolean Fail(RoamingNetworkBootstrapOutcome outcome, String error, out RoamingNetworkBootstrapResult result)
    {
        result = Result(outcome, error); return false;
    }

    /// <summary>
    /// Release the staging lease without deleting acknowledged fragments or progress.
    /// </summary>
    public void Dispose()
    {
        lock (gate)
        {
            if (operating) throw new InvalidOperationException("A bootstrap callback cannot dispose its active receiver.");
            if (!disposed) { disposed = true; lease.Dispose(); }
        }
    }

    // A test observer may fail before a receipt is installed, never after it becomes acknowledged.
    internal enum BootstrapWriteStage { BeforeTemporaryWrite, TemporaryFileFlushed }
    internal Action<BootstrapWriteStage>? WriteObserver { get; set; }
}
