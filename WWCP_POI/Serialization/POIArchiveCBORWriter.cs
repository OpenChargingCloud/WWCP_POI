/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Buffers.Binary;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

// Archive containers have known counts and text keys. Sort their encoded keys before emitting
// values, so Styx does not need a scratch buffer containing the complete outer archive map.
// Leaf values keep Styx's canonical encoding; POI payloads preflight before direct emission.
internal sealed class POIArchiveCBORWriter(Stream destination) : IDisposable
{
    private readonly POIArchiveStreamBuffer output = new(destination);
    private Int32 depth;

    internal void Map(params (String Name, Action<POIArchiveCBORWriter> Write)[] fields)
    {
        var sorted = fields.Select(field => (Key: CBORValue.FromText(field.Name).ToByteArray(CBORWriterOptions.Canonical), field.Write)).ToArray();
        System.Array.Sort(sorted, (left, right) => left.Key.AsSpan().SequenceCompareTo(right.Key));
        for (var index = 1; index < sorted.Length; index++)
            if (sorted[index - 1].Key.AsSpan().SequenceEqual(sorted[index].Key))
                throw new ArgumentException("Duplicate CBOR archive map key.");
        Enter();
        try
        {
            Head(5, sorted.Length);
            foreach (var field in sorted)
            {
                output.WriteParts(field.Key);
                field.Write(this);
            }
        }
        finally { depth--; }
    }

    internal void Array<T>(Int32 count, IEnumerable<T> values, Action<POIArchiveCBORWriter, T> write)
    {
        Enter();
        try
        {
            Head(4, count);
            var written = 0;
            foreach (var value in values)
            {
                if (written == count) throw new ArgumentException("CBOR archive array exceeds its declared count.");
                write(this, value);
                written++;
            }
            if (written != count) throw new ArgumentException("CBOR archive array differs from its declared count.");
        }
        finally { depth--; }
    }

    internal void Value(CBORValue value)
    {
        var writer = new CBORWriter(output, new CBORWriterOptions { Deterministic = true, MaxDepth = 64 - depth });
        value.WriteTo(writer);
        if (!writer.IsComplete) throw new InvalidOperationException("Incomplete CBOR archive value.");
    }

    internal void Text(String value) => Value(CBORValue.FromText(value));

    internal void Encoded(ReadOnlySpan<Byte> canonicalValue)
    {
        var reader = new CBORReader(canonicalValue, new CBORReaderOptions { MaxDepth = 64 - depth });
        reader.SkipValue();
        if (reader.BytesRemaining != 0) throw new ArgumentException("Expected one encoded CBOR archive value.");
        output.WriteParts(canonicalValue);
    }

    /// <summary>
    /// Preflight a complete static POI snapshot before writing it directly into this archive.
    /// The caller keeps ownership of the output stream and its durable installation.
    /// </summary>
    internal void POI(IImmutablePOI value)
        => POIRepresentation.WriteArchiveCBOR(value, output, 64 - depth);

    /// <summary>
    /// Preflight a signed ChangeSet before emitting its exact transport directly into this archive.
    /// The caller owns partial-output disposal, destination lifetime and durable installation.
    /// </summary>
    internal void ChangeSet(RoamingNetworkChangeSet value)
        => value.WriteArchiveCBOR(output, 64 - depth);

    private void Enter()
    {
        if (depth == 64) throw new CBORException("The maximum CBOR nesting depth of 64 was exceeded.");
        depth++;
    }

    // Only definite-length array/map heads are emitted here. Scalar/tag/float rules stay in Styx.
    private void Head(Byte major, Int32 count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        var span = output.GetSpan(5);
        var prefix = (Byte)(major << 5);
        if (count < 24) { span[0] = (Byte)(prefix | count); output.Advance(1); }
        else if (count <= Byte.MaxValue) { span[0] = (Byte)(prefix | 24); span[1] = (Byte)count; output.Advance(2); }
        else if (count <= UInt16.MaxValue) { span[0] = (Byte)(prefix | 25); BinaryPrimitives.WriteUInt16BigEndian(span[1..], (UInt16)count); output.Advance(3); }
        else { span[0] = (Byte)(prefix | 26); BinaryPrimitives.WriteUInt32BigEndian(span[1..], (UInt32)count); output.Advance(5); }
    }

    internal void Complete() => output.Flush();

    public void Dispose() => output.Dispose();

}
