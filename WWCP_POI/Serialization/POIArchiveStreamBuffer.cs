/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Buffers;

namespace cloud.charging.open.protocols.WWCP.POI;

// A borrowed stream adapter for JSON and CBOR writers. Flush only the encoder buffer;
// destination lifetime and durable flushing belong to the caller. Large values can grow it.
internal sealed class POIArchiveStreamBuffer(Stream stream) : IBufferWriter<Byte>, IDisposable
{
    private Byte[] buffer = ArrayPool<Byte>.Shared.Rent(16 * 1024);
    private Int32 written;

    public Memory<Byte> GetMemory(Int32 sizeHint = 0) { Ensure(sizeHint); return buffer.AsMemory(written); }
    public Span<Byte> GetSpan(Int32 sizeHint = 0) { Ensure(sizeHint); return buffer.AsSpan(written); }

    public void Advance(Int32 count)
    {
        if (count < 0 || count > buffer.Length - written) throw new ArgumentOutOfRangeException(nameof(count));
        written += count;
    }

    private void Ensure(Int32 sizeHint)
    {
        ObjectDisposedException.ThrowIf(buffer.Length == 0, this);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);
        sizeHint = Math.Max(1, sizeHint);
        if (sizeHint <= buffer.Length - written) return;
        Flush();
        if (sizeHint <= buffer.Length) return;
        var replacement = ArrayPool<Byte>.Shared.Rent(sizeHint);
        ArrayPool<Byte>.Shared.Return(buffer);
        buffer = replacement;
    }

    internal void WriteParts(ReadOnlySpan<Byte> bytes)
    {
        if (bytes.Length > buffer.Length)
        {
            Flush();
            stream.Write(bytes);
            return;
        }
        bytes.CopyTo(GetSpan(bytes.Length));
        Advance(bytes.Length);
    }

    internal void Flush()
    {
        if (written == 0) return;
        stream.Write(buffer.AsSpan(0, written));
        written = 0;
    }

    public void Dispose()
    {
        // Flush only after successful encoding. Never close or flush the caller's stream.
        if (buffer.Length == 0) return;
        ArrayPool<Byte>.Shared.Return(buffer);
        buffer = [];
    }
}
