/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

namespace cloud.charging.open.protocols.WWCP.POI;

internal enum POIArchiveInputStage { FileCreated, BeforeWrite, BeforeMap }

/// <summary>
/// Private bounded input capture. File handles own deletion; mapped spans live only during synchronous replay.
/// </summary>
internal sealed class POIArchiveInputSpool : IDisposable, IAsyncDisposable
{
    internal delegate RoamingNetworkHistory Restore(ReadOnlySpan<Byte> bytes);
    internal const Int32 BufferBytes = 64 * 1024;
    private readonly RoamingNetworkArchiveReadOptions options;
    private readonly RoamingNetworkHistoryLimits limits;
    private readonly Int32 memoryLimit;
    private readonly Boolean asynchronous;
    private readonly Action<POIArchiveInputStage, String>? observer;
    private MemoryStream? memory = new();
    private FileStream? file;
    private FileStream? lease;
    private String? path;
    internal Int32 Length { get; private set; }
    internal Int32 MemoryCapacity => memory?.Capacity ?? 0;

    internal POIArchiveInputSpool(RoamingNetworkArchiveReadOptions options, RoamingNetworkHistoryLimits limits,
        Boolean asynchronous, Action<POIArchiveInputStage, String>? observer = null)
    {
        this.options = options; this.limits = limits; memoryLimit = Math.Min(options.MemoryThresholdBytes, limits.MaxArchiveBytes);
        this.asynchronous = asynchronous; this.observer = observer;
    }

    private void CreateFile()
    {
        if (!Directory.Exists(options.TemporaryDirectory)) throw new DirectoryNotFoundException("The input spool directory must exist.");
        for (var directory = new DirectoryInfo(options.TemporaryDirectory); directory is not null; directory = directory.Parent)
            if ((directory.Attributes & FileAttributes.ReparsePoint) != 0)
                throw new IOException("Input spooling does not accept linked directory ancestors.");
        var lockPath = options.TemporaryArchivePath + ".lock";
        try
        {
            if ((File.GetAttributes(lockPath) & (FileAttributes.Directory | FileAttributes.ReparsePoint | FileAttributes.Device)) != 0)
                throw new IOException("The input spool lease must be a regular file.");
        }
        catch (FileNotFoundException) { }
        // Shared reader leases permit simultaneous captures and exclude the maintenance writer lease.
        lease = new(lockPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
        path = options.TemporaryArchivePath + ".tmp-" + Guid.NewGuid().ToString("N");
        file = new(path, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, BufferBytes,
            FileOptions.DeleteOnClose | FileOptions.SequentialScan | (asynchronous ? FileOptions.Asynchronous : FileOptions.None));
        observer?.Invoke(POIArchiveInputStage.FileCreated, path);
    }

    private void Reserve(Int32 count)
    {
        var required = checked(Length + count);
        if (memory!.Capacity < required)
            memory.Capacity = (Int32) Math.Min(memoryLimit, Math.Max(required, Math.Max(256L, 2L * memory.Capacity)));
    }

    internal void Append(ReadOnlySpan<Byte> bytes)
    {
        limits.Require(RoamingNetworkHistoryLimitKind.ArchiveBytes, (Int64) Length + bytes.Length);
        if (file is null && (Int64) Length + bytes.Length > memoryLimit)
        {
            CreateFile();
            file!.Write(memory!.GetBuffer().AsSpan(0, Length));
            memory.Dispose(); memory = null;
        }
        if (file is null) { Reserve(bytes.Length); memory!.Write(bytes); }
        else { observer?.Invoke(POIArchiveInputStage.BeforeWrite, path!); file.Write(bytes); }
        Length = checked(Length + bytes.Length);
    }

    internal async ValueTask AppendAsync(ReadOnlyMemory<Byte> bytes, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        limits.Require(RoamingNetworkHistoryLimitKind.ArchiveBytes, (Int64) Length + bytes.Length);
        if (file is null && (Int64) Length + bytes.Length > memoryLimit)
        {
            CreateFile();
            await file!.WriteAsync(memory!.GetBuffer().AsMemory(0, Length), cancellationToken).ConfigureAwait(false);
            memory.Dispose(); memory = null;
        }
        if (file is null) { Reserve(bytes.Length); memory!.Write(bytes.Span); }
        else
        {
            observer?.Invoke(POIArchiveInputStage.BeforeWrite, path!);
            await file.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
        }
        Length = checked(Length + bytes.Length);
    }

    internal ValueTask FlushAsync(CancellationToken cancellationToken)
        => file is null ? ValueTask.CompletedTask : new(file.FlushAsync(cancellationToken));

    internal RoamingNetworkHistory Read(Restore restore)
    {
        if (file is null) return restore(memory!.GetBuffer().AsSpan(0, Length));
        file.Flush();
        observer?.Invoke(POIArchiveInputStage.BeforeMap, path!);
        using var input = new POIArchiveMappedFile(file, limits);
        return restore(input.Bytes);
    }

    public void Dispose() { try { file?.Dispose(); } finally { try { lease?.Dispose(); } finally { memory?.Dispose(); } } }
    public async ValueTask DisposeAsync()
    {
        try { if (file is not null) await file.DisposeAsync().ConfigureAwait(false); }
        finally { try { lease?.Dispose(); } finally { memory?.Dispose(); } }
    }
}
