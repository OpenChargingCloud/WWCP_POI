/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Security.Cryptography;

namespace cloud.charging.open.protocols.WWCP.POI;

// Hash/count an archive without retaining its bytes; optionally forward them to a borrowed file.
internal sealed class POIArchiveHashStream(Stream? destination = null) : Stream
{
    private readonly IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    private Int64 length;
    private Boolean complete;

    internal ETag Complete()
    {
        if (complete) throw new InvalidOperationException("The archive digest has already been finalized.");
        complete = true;
        return new(ETagFormat.CBOR, ETagHashAlgorithm.SHA256, hash.GetHashAndReset());
    }

    public override void Write(ReadOnlySpan<Byte> buffer)
    {
        if (complete) throw new InvalidOperationException("The archive digest has already been finalized.");
        var next = checked(length + buffer.Length);
        destination?.Write(buffer);
        hash.AppendData(buffer);
        length = next;
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
