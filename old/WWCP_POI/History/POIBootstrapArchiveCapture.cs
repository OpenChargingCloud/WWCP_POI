/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Security.Cryptography;

namespace cloud.charging.open.protocols.WWCP.POI;

// Own fixed-size fragments and hash them as they are emitted. No complete contiguous archive
// is allocated. All arrays remain private to the completed immutable bootstrap source.
internal sealed class POIBootstrapArchiveCapture(Int32 chunkBytes, Int32 maxArchiveBytes, Int32 maxChunks) : Stream
{
    private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private readonly List<Byte[]> chunks = [];
    private readonly List<ImmutableArray<Byte>> digests = [];
    private Byte[]? current;
    private Int32 used;
    private Int32 length;
    private Boolean complete;

    public override void Write(ReadOnlySpan<Byte> buffer)
    {
        if (complete) throw new InvalidOperationException("The frozen archive has already been finalized.");
        if ((Int64)length + buffer.Length > Math.Min((Int64)maxArchiveBytes, (Int64)chunkBytes * maxChunks))
            throw new ArgumentException("Frozen archive exceeds local bootstrap limits.");
        hash.AppendData(buffer);
        length += buffer.Length;
        while (!buffer.IsEmpty)
        {
            current ??= new Byte[Math.Min(chunkBytes, maxArchiveBytes - (length - buffer.Length))];
            var count = Math.Min(buffer.Length, current.Length - used);
            buffer[..count].CopyTo(current.AsSpan(used));
            used += count;
            buffer = buffer[count..];
            if (used == current.Length) FinishChunk();
        }
    }

    private void FinishChunk()
    {
        if (current is null || used == 0) return;
        if (used != current.Length) System.Array.Resize(ref current, used);
        chunks.Add(current);
        digests.Add(ImmutableArray.CreateRange(SHA256.HashData(current)));
        current = null;
        used = 0;
    }

    internal (ImmutableArray<Byte[]> Chunks, ImmutableArray<ImmutableArray<Byte>> ChunkDigests, Int32 Length, ETag Digest) Complete()
    {
        if (complete) throw new InvalidOperationException("The frozen archive has already been finalized.");
        FinishChunk();
        complete = true;
        return ([.. chunks], [.. digests], length, new(ETagFormat.CBOR, ETagHashAlgorithm.SHA256, hash.GetHashAndReset()));
    }

    public override void Write(Byte[] buffer, Int32 offset, Int32 count) => Write(buffer.AsSpan(offset, count));
    public override Boolean CanRead => false;
    public override Boolean CanSeek => false;
    public override Boolean CanWrite => !complete;
    public override Int64 Length => length;
    public override Int64 Position { get => length; set => throw new NotSupportedException(); }
    public override void Flush() { }
    public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(Int64 value) => throw new NotSupportedException();
    protected override void Dispose(Boolean disposing) { if (disposing) { hash.Dispose(); complete = true; } base.Dispose(disposing); }
}
