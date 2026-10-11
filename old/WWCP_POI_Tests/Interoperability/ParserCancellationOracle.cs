/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using org.GraphDefined.Vanaheimr.Illias;

using cloud.charging.open.protocols.WWCP.POI;

namespace WWCP_POI_Tests.Interoperability;

/// <summary>
/// Frozen preceding syntax/canonical algorithm. Actual Styx scalar/key equality remains a shared helper.
/// The caller performs the existing local limit scan before building this index.
/// </summary>
internal sealed class ParserCancellationIndexOracle
{
    internal readonly record struct Part(Int32 Offset, Int32 Length);

    private readonly Dictionary<String, Part> fields = new(StringComparer.Ordinal);
    internal Part[] Commits { get; private set; } = [];
    internal Part[] Receipts { get; private set; } = [];

    internal static ParserCancellationIndexOracle Read(ReadOnlySpan<Byte> bytes)
    {
        var result = new ParserCancellationIndexOracle();
        var reader = new CBORReader(bytes);
        reader.ReadStartMap();
        String? duplicate = null;
        while (reader.PeekState() != CBORReaderState.EndMap)
        {
            var name = reader.ReadTextString();
            var start = reader.Position;
            if (name == "Commits") result.Commits = ReadParts(ref reader);
            else if (name == "RetentionReceipts") result.Receipts = ReadParts(ref reader);
            else ValidateValue(ref reader);
            if (!result.fields.TryAdd(name, new(start, reader.Position - start))) duplicate ??= name;
        }
        reader.ReadEndMap();
        if (duplicate is not null) ThrowDuplicate(CBORValue.FromText(duplicate));
        if (reader.BytesRemaining != 0) throw new CBORException("Unexpected trailing archive bytes.");
        return result;
    }

    internal void RequireFields(params String[] expected)
    {
        if (!fields.Keys.ToHashSet(StringComparer.Ordinal).SetEquals(expected))
            throw new ArgumentException("The CBOR map must contain exactly: " + String.Join(", ", expected));
    }

    internal ReadOnlySpan<Byte> Slice(ReadOnlySpan<Byte> bytes, String name)
        => Slice(bytes, fields[name]);

    internal CBORValue Value(ReadOnlySpan<Byte> bytes, String name)
        => fields.TryGetValue(name, out var part) ? CBORValue.Parse(Slice(bytes, part)) :
            throw new InvalidOperationException("Sequence contains no matching element");

    internal static ReadOnlySpan<Byte> Slice(ReadOnlySpan<Byte> bytes, Part part)
        => bytes.Slice(part.Offset, part.Length);

    internal static void RequireCanonical(ReadOnlySpan<Byte> bytes)
    {
        // WriterOptions.Canonical does not require preferred bignums. ReaderOptions.Canonical does.
        // Match the existing writer/re-encoding contract, including its map and floating-point rules.
        var reader = new CBORReader(bytes, new CBORReaderOptions { RequireDeterministic = true });
        try { ValidateValue(ref reader); }
        catch (CBORException error)
        { throw new ArgumentException("Bootstrap requires the deterministic CBOR archive representation.", error); }
        if (reader.BytesRemaining != 0) throw new ArgumentException("Bootstrap requires the deterministic CBOR archive representation.");
    }

    private static Part[] ReadParts(ref CBORReader reader)
    {
        var count = reader.ReadStartArray();
        var parts = new List<Part>(count.HasValue ? Math.Min(count.Value, 128) : 0);
        while (reader.PeekState() != CBORReaderState.EndArray)
        {
            var start = reader.Position;
            ValidateValue(ref reader);
            parts.Add(new(start, reader.Position - start));
        }
        reader.ReadEndArray();
        return parts.ToArray();
    }

    private static void ValidateValue(ref CBORReader reader)
    {
        switch (reader.PeekState())
        {
            case CBORReaderState.StartArray:
                reader.ReadStartArray();
                while (reader.PeekState() != CBORReaderState.EndArray) ValidateValue(ref reader);
                reader.ReadEndArray();
                break;

            case CBORReaderState.StartMap:
                reader.ReadStartMap();
                var keys = new HashSet<CBORValue>();
                CBORValue? duplicate = null;
                while (reader.PeekState() != CBORReaderState.EndMap)
                {
                    // Use the actual value model's equality, including structured/non-text keys.
                    var key = CBORValue.ReadFrom(ref reader);
                    ValidateValue(ref reader);
                    if (!keys.Add(key)) duplicate ??= key;
                }
                reader.ReadEndMap();
                // As in CBORValue.ReadFrom, malformed later children precede this map's duplicates.
                if (duplicate is { } repeated) ThrowDuplicate(repeated);
                break;

            case CBORReaderState.Tag:
                reader.ReadTag();
                ValidateValue(ref reader);
                break;

            default:
                // Actual Styx scalar readers preserve strict UTF-8, indefinite string chunks,
                // simple-value/float rules and full integer range. No scalar is retained.
                _ = CBORValue.ReadFrom(ref reader);
                break;
        }
    }

    private static void ThrowDuplicate(CBORValue key)
        => throw new CBORException($"Duplicate CBOR map key {key.ToDiagnosticString()}!");
}

// Frozen preceding byte/count/depth preflight, including original SkipValue calls.
internal static class ParserCancellationLimitsOracle
{
    internal static void CheckCBORArchive(ReadOnlySpan<Byte> bytes, RoamingNetworkHistoryLimits limits)
    {
        limits.Require(RoamingNetworkHistoryLimitKind.ArchiveBytes, bytes.Length);
        var reader = new CBORReader(bytes);
        reader.ReadStartMap();
        Int64 commits = 1, receipts = 0, ids = 0;
        while (reader.PeekState() != CBORReaderState.EndMap)
        {
            switch (reader.ReadTextString())
            {
                case "Commits":
                    CountCBORArray(ref reader, limits, RoamingNetworkHistoryLimitKind.RetainedCommits, ref commits);
                    break;
                case "RetentionReceipts":
                    StartCBORArray(ref reader, limits, RoamingNetworkHistoryLimitKind.RetentionReceipts, receipts);
                    while (reader.PeekState() != CBORReaderState.EndArray)
                    {
                        limits.Require(RoamingNetworkHistoryLimitKind.RetentionReceipts, ++receipts);
                        reader.ReadStartMap();
                        while (reader.PeekState() != CBORReaderState.EndMap)
                        {
                            var name = reader.ReadTextString();
                            if (name is "PrunedCommits" or "ArchiveOnlyTips")
                                CountCBORArray(ref reader, limits, RoamingNetworkHistoryLimitKind.CatalogCommitIds, ref ids);
                            else reader.SkipValue();
                        }
                        reader.ReadEndMap();
                    }
                    reader.ReadEndArray();
                    break;
                default:
                    reader.SkipValue();
                    break;
            }
        }
        reader.ReadEndMap();
        if (reader.BytesRemaining != 0) throw new ArgumentException("Trailing archive data.");
    }

    private static void StartCBORArray(ref CBORReader reader, RoamingNetworkHistoryLimits limits,
        RoamingNetworkHistoryLimitKind kind, Int64 count)
    {
        if (reader.ReadStartArray() is Int32 length) limits.Require(kind, count + length);
    }

    private static void CountCBORArray(ref CBORReader reader, RoamingNetworkHistoryLimits limits,
        RoamingNetworkHistoryLimitKind kind, ref Int64 count)
    {
        StartCBORArray(ref reader, limits, kind, count);
        while (reader.PeekState() != CBORReaderState.EndArray)
        {
            limits.Require(kind, ++count);
            reader.SkipValue();
        }
        reader.ReadEndArray();
    }

}
