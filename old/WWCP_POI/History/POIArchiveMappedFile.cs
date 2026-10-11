/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.IO.MemoryMappedFiles;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Scoped read-only file input. No span may escape its owner or survive disposal.
/// Existing archives are held with read sharing only; private spools retain their own file lifetime.
/// </summary>
internal sealed unsafe class POIArchiveMappedFile : IDisposable
{
    private readonly FileStream? ownedFile;
    private MemoryMappedFile? mapping;
    private MemoryMappedViewAccessor? view;
    private Byte* pointer;
    private Boolean acquired;
    private Boolean disposed;
    internal Int32 Length { get; }

    internal ReadOnlySpan<Byte> Bytes
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return Length == 0 ? ReadOnlySpan<Byte>.Empty : new(pointer + view!.PointerOffset, Length);
        }
    }

    internal POIArchiveMappedFile(String path, RoamingNetworkHistoryLimits limits)
        : this(new FileStream(Path.GetFullPath(path), FileMode.Open, FileAccess.Read, FileShare.Read), limits, true) { }

    internal POIArchiveMappedFile(FileStream file, RoamingNetworkHistoryLimits limits)
        : this(file, limits, false) { }

    private POIArchiveMappedFile(FileStream file, RoamingNetworkHistoryLimits limits, Boolean ownFile)
    {
        ownedFile = ownFile ? file : null;
        try
        {
            var length = file.Length;
            limits.Require(RoamingNetworkHistoryLimitKind.ArchiveBytes, length);
            Length = checked((Int32) length);
            if (Length == 0) return;
            mapping = MemoryMappedFile.CreateFromFile(file, null, Length, MemoryMappedFileAccess.Read,
                HandleInheritability.None, leaveOpen: true);
            view = mapping.CreateViewAccessor(0, Length, MemoryMappedFileAccess.Read);
            view.SafeMemoryMappedViewHandle.AcquirePointer(ref pointer);
            acquired = true;
        }
        catch { Dispose(); throw; }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (acquired) { view!.SafeMemoryMappedViewHandle.ReleasePointer(); acquired = false; pointer = null; }
        }
        finally
        {
            try { view?.Dispose(); }
            finally { try { mapping?.Dispose(); } finally { ownedFile?.Dispose(); } }
        }
    }
}
