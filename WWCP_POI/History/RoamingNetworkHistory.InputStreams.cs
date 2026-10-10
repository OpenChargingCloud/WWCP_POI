/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    /// <summary>
    /// Capture and recover a borrowed readable CBOR stream, leaving it open and never seeking it.
    /// Byte limits reject at the first excess byte; full syntax validation still precedes trust.
    /// Cancellation is observed at reads and internal syntax/model/replay boundaries; a blocking Read or individual parser call is not interrupted.
    /// </summary>
    public static RoamingNetworkHistory ParseCBOR(Stream source,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null,
        RoamingNetworkHistoryLimits? limits = null, RoamingNetworkArchiveReadOptions? readOptions = null,
        CancellationToken cancellationToken = default)
    {
        RequireInput(source); limits ??= new(); readOptions ??= new();
        cancellationToken.ThrowIfCancellationRequested();
        using var spool = new POIArchiveInputSpool(readOptions, limits, false);
        var buffer = new Byte[(Int32) Math.Min(POIArchiveInputSpool.BufferBytes, (Int64) limits.MaxArchiveBytes + 1)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var count = source.Read(buffer, 0, InputRequest(spool.Length, buffer.Length, limits));
            cancellationToken.ThrowIfCancellationRequested();
            RequireInputCount(count, InputRequest(spool.Length, buffer.Length, limits));
            if (count == 0) break;
            limits.Require(RoamingNetworkHistoryLimitKind.ArchiveBytes, (Int64) spool.Length + count);
            spool.Append(buffer.AsSpan(0, count));
        }
        return spool.Read(bytes => RestoreCapturedInput(bytes, limits, verifyBatchSignature, verifyCommitSignature,
            authorizeCommit, authorizeSnapshotBoundary, cancellationToken));
    }

    /// <summary>
    /// Asynchronously capture a borrowed CBOR stream and recover its static history with fresh local runtime.
    /// The source owns its lifetime; temporary files, buffers and mapped views belong to this call.
    /// Parsing and replay are synchronous, with cooperative checks in owned loops and around individual parser/model calls.
    /// </summary>
    public static async Task<RoamingNetworkHistory> ParseCBORAsync(Stream source,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature = null,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary = null,
        RoamingNetworkHistoryLimits? limits = null, RoamingNetworkArchiveReadOptions? readOptions = null,
        CancellationToken cancellationToken = default)
    {
        RequireInput(source); limits ??= new(); readOptions ??= new();
        cancellationToken.ThrowIfCancellationRequested();
        await using var spool = new POIArchiveInputSpool(readOptions, limits, true);
        var buffer = new Byte[(Int32) Math.Min(POIArchiveInputSpool.BufferBytes, (Int64) limits.MaxArchiveBytes + 1)];
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = InputRequest(spool.Length, buffer.Length, limits);
            var count = await source.ReadAsync(buffer.AsMemory(0, request), cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            RequireInputCount(count, request);
            if (count == 0) break;
            limits.Require(RoamingNetworkHistoryLimitKind.ArchiveBytes, (Int64) spool.Length + count);
            await spool.AppendAsync(buffer.AsMemory(0, count), cancellationToken).ConfigureAwait(false);
        }
        await spool.FlushAsync(cancellationToken).ConfigureAwait(false);
        return spool.Read(bytes => RestoreCapturedInput(bytes, limits, verifyBatchSignature, verifyCommitSignature,
            authorizeCommit, authorizeSnapshotBoundary, cancellationToken));
    }

    private static void RequireInput(Stream source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead) throw new ArgumentException("The archive input stream must be readable.", nameof(source));
    }

    private static Int32 InputRequest(Int32 length, Int32 bufferLength, RoamingNetworkHistoryLimits limits)
        => (Int32) Math.Min(bufferLength, (Int64) limits.MaxArchiveBytes - length + 1);

    private static void RequireInputCount(Int32 count, Int32 requested)
    {
        if (count < 0 || count > requested) throw new IOException("The input stream returned an invalid read count.");
    }

    internal static RoamingNetworkHistory RestoreCapturedInput(ReadOnlySpan<Byte> bytes, RoamingNetworkHistoryLimits limits,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? verifyBatchSignature,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? verifyCommitSignature,
        Func<RoamingNetworkCommit, Boolean>? authorizeCommit,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? authorizeSnapshotBoundary, CancellationToken token,
        RoamingNetworkBootstrapManifest? manifest = null,
        Action<POIArchiveReadStage, Int32>? parseObserver = null)
    {
        Boolean BatchTrust(RoamingNetworkChangeSet batch, RoamingNetworkChangeSetSignature peer)
        { token.ThrowIfCancellationRequested(); var valid = verifyBatchSignature!(batch, peer); token.ThrowIfCancellationRequested(); return valid; }
        Boolean CommitTrust(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer)
        { token.ThrowIfCancellationRequested(); var valid = verifyCommitSignature!(commit, peer); token.ThrowIfCancellationRequested(); return valid; }
        Boolean CommitAuthority(RoamingNetworkCommit commit)
        { token.ThrowIfCancellationRequested(); var valid = authorizeCommit?.Invoke(commit) ?? true; token.ThrowIfCancellationRequested(); return valid; }
        Boolean BoundaryAuthority(RoamingNetworkSnapshotBoundary boundary)
        { token.ThrowIfCancellationRequested(); var valid = authorizeSnapshotBoundary!(boundary); token.ThrowIfCancellationRequested(); return valid; }
        RoamingNetworkHistory? history = null;
        try
        {
            token.ThrowIfCancellationRequested();
            var progress = new POIArchiveReadProgress(token, parseObserver);
            history = manifest is null
                ? ParseCBORCore(bytes, verifyBatchSignature is null ? null : BatchTrust,
                    verifyCommitSignature is null ? null : CommitTrust,
                    CommitAuthority, authorizeSnapshotBoundary is null ? null : BoundaryAuthority, limits, progress)
                : RestoreBootstrap(bytes, manifest, verifyBatchSignature is null ? null : BatchTrust,
                    verifyCommitSignature is null ? null : CommitTrust,
                    CommitAuthority, authorizeSnapshotBoundary is null ? null : BoundaryAuthority, limits, progress);
            token.ThrowIfCancellationRequested();
            var result = history; history = null; return result;
        }
        catch (Exception) when (token.IsCancellationRequested) { throw new OperationCanceledException(token); }
        finally
        {
            // The recovered history retains its trust delegates. Cancellation belongs only to this read.
            // Clear the captured token before returning so later commits keep the caller's original policy.
            token = default;
            history?.Dispose();
        }
    }
}
