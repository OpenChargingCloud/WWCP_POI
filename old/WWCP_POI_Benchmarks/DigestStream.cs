/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Security.Cryptography;
using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Benchmarks;

// Non-seekable borrowed output: hash/count bytes without retaining an archive or doing disk I/O.
internal sealed class DigestStream : Stream
{
    private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private Int64 count;
    private Boolean disposed;
    internal OperationResult Complete(ETagFormat format)
    {
        if (disposed) throw new InvalidOperationException("The encoder closed its borrowed stream.");
        var prefix = format == ETagFormat.JSON ? "json" : "cbor";
        return new(count, 0, prefix + ":sha256:hex:" + Convert.ToHexStringLower(hash.GetHashAndReset()));
    }

    public override void Write(Byte[] buffer, Int32 offset, Int32 length) => Write(buffer.AsSpan(offset, length));
    public override void Write(ReadOnlySpan<Byte> bytes)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        hash.AppendData(bytes); count = checked(count + bytes.Length);
    }

    public override Boolean CanRead => false;
    public override Boolean CanSeek => false;
    public override Boolean CanWrite => !disposed;
    public override Int64 Length => throw new NotSupportedException();
    public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => throw new InvalidOperationException("The encoder flushed its borrowed stream.");
    public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(Int64 value) => throw new NotSupportedException();
    protected override void Dispose(Boolean disposing)
    {
        if (disposing && !disposed) { disposed = true; hash.Dispose(); }
        base.Dispose(disposing);
    }
}
