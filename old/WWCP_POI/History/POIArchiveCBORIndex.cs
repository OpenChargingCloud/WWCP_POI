/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Validated offsets into a borrowed archive span. No value or input buffer is retained.
/// The caller performs the existing local limit scan before building this index.
/// </summary>
internal sealed class POIArchiveCBORIndex
{
    internal readonly record struct Part(Int32 Offset, Int32 Length);

    private readonly Dictionary<String, Part> fields = new(StringComparer.Ordinal);
    internal Part[] Commits { get; private set; } = [];
    internal Part[] Receipts { get; private set; } = [];

    internal static POIArchiveCBORIndex Read(ReadOnlySpan<Byte> bytes, POIArchiveReadProgress progress = default)
    {
        progress.Check(POIArchiveReadStage.Index);
        var result = new POIArchiveCBORIndex();
        var reader = new CBORReader(bytes);
        reader.ReadStartMap();
        String? duplicate = null;
        while (reader.PeekState() != CBORReaderState.EndMap)
        {
            progress.Check(POIArchiveReadStage.Index, reader.Position);
            var name = reader.ReadTextString();
            var start = reader.Position;
            if (name == "Commits") result.Commits = ReadParts(ref reader, progress);
            else if (name == "RetentionReceipts") result.Receipts = ReadParts(ref reader, progress);
            else ValidateValue(ref reader, progress, POIArchiveReadStage.Index);
            if (!result.fields.TryAdd(name, new(start, reader.Position - start))) duplicate ??= name;
        }
        reader.ReadEndMap();
        progress.Check(POIArchiveReadStage.Index, reader.Position);
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

    internal static void RequireCanonical(ReadOnlySpan<Byte> bytes, POIArchiveReadProgress progress = default)
    {
        // WriterOptions.Canonical does not require preferred bignums. ReaderOptions.Canonical does.
        // Match the existing writer/re-encoding contract, including its map and floating-point rules.
        var reader = new CBORReader(bytes, new CBORReaderOptions { RequireDeterministic = true });
        progress.Check(POIArchiveReadStage.Canonical);
        try { ValidateValue(ref reader, progress, POIArchiveReadStage.Canonical); }
        catch (CBORException error)
        { throw new ArgumentException("Bootstrap requires the deterministic CBOR archive representation.", error); }
        progress.Check(POIArchiveReadStage.Canonical, reader.Position);
        if (reader.BytesRemaining != 0) throw new ArgumentException("Bootstrap requires the deterministic CBOR archive representation.");
    }

    private static Part[] ReadParts(ref CBORReader reader, POIArchiveReadProgress progress)
    {
        var count = reader.ReadStartArray();
        var parts = new List<Part>(count.HasValue ? Math.Min(count.Value, 128) : 0);
        while (reader.PeekState() != CBORReaderState.EndArray)
        {
            var start = reader.Position;
            ValidateValue(ref reader, progress, POIArchiveReadStage.Index);
            parts.Add(new(start, reader.Position - start));
        }
        reader.ReadEndArray();
        return parts.ToArray();
    }

    private static void ValidateValue(ref CBORReader reader, POIArchiveReadProgress progress, POIArchiveReadStage stage)
    {
        progress.Check(stage, reader.Position);
        switch (reader.PeekState())
        {
            case CBORReaderState.StartArray:
                reader.ReadStartArray();
                while (reader.PeekState() != CBORReaderState.EndArray) ValidateValue(ref reader, progress, stage);
                reader.ReadEndArray();
                break;

            case CBORReaderState.StartMap:
                reader.ReadStartMap();
                var keys = new HashSet<CBORValue>();
                CBORValue? duplicate = null;
                while (reader.PeekState() != CBORReaderState.EndMap)
                {
                    // Use the actual value model's equality, including structured/non-text keys.
                    progress.Check(stage, reader.Position);
                    var key = CBORValue.ReadFrom(ref reader);
                    ValidateValue(ref reader, progress, stage);
                    if (!keys.Add(key)) duplicate ??= key;
                }
                reader.ReadEndMap();
                // As in CBORValue.ReadFrom, malformed later children precede this map's duplicates.
                if (duplicate is { } repeated) ThrowDuplicate(repeated);
                break;

            case CBORReaderState.Tag:
                reader.ReadTag();
                ValidateValue(ref reader, progress, stage);
                break;

            default:
                // Actual Styx scalar readers preserve strict UTF-8, indefinite string chunks,
                // simple-value/float rules and full integer range. No scalar is retained.
                _ = CBORValue.ReadFrom(ref reader);
                break;
        }
        progress.Check(stage, reader.Position);
    }

    private static void ThrowDuplicate(CBORValue key)
        => throw new CBORException($"Duplicate CBOR map key {key.ToDiagnosticString()}!");
}

/// <summary>
/// Synchronous commit cursor for models or encoded ranges.
/// Lazy suffix bytes are privately frozen before callbacks can mutate the caller's input.
/// Model enumeration begins only on the first move, after root authorization in boundary restore.
/// </summary>
internal ref struct POIArchiveCommitCursor
{
    private readonly IEnumerable<RoamingNetworkCommit>? models;
    private IEnumerator<RoamingNetworkCommit>? enumerator;
    private readonly ReadOnlySpan<Byte> bytes;
    private readonly POIArchiveCBORIndex.Part[]? parts;
    private readonly Int32 baseOffset;
    private Int32 position;
    private readonly POIArchiveReadProgress progress;

    internal POIArchiveCommitCursor(IEnumerable<RoamingNetworkCommit> models)
    { this.models = models; enumerator = null; bytes = default; parts = null; baseOffset = 0; position = 0; progress = default; }

    internal POIArchiveCommitCursor(ReadOnlySpan<Byte> bytes, POIArchiveCBORIndex.Part[] parts, POIArchiveReadProgress progress = default)
    {
        this.progress = progress; progress.Check(POIArchiveReadStage.SuffixCapture);
        models = null; enumerator = null; this.parts = parts; position = 0;
        baseOffset = parts.Length == 0 ? 0 : parts[0].Offset;
        this.bytes = parts.Length == 0 ? [] : bytes.Slice(baseOffset,
            parts[^1].Offset + parts[^1].Length - baseOffset).ToArray();
        progress.Check(POIArchiveReadStage.SuffixCapture, this.bytes.Length);
    }

    internal Boolean MoveNext(out RoamingNetworkCommit commit)
    {
        if (models is not null)
        {
            enumerator ??= models.GetEnumerator();
            if (enumerator.MoveNext()) { commit = enumerator.Current; return true; }
        }
        else if (position < parts!.Length)
        {
            var part = parts[position++];
            progress.Check(POIArchiveReadStage.CommitModel, part.Offset);
            commit = RoamingNetworkCommit.ParseCBOR(bytes.Slice(part.Offset - baseOffset, part.Length));
            progress.Check(POIArchiveReadStage.CommitModel, part.Offset + part.Length);
            return true;
        }
        commit = null!;
        return false;
    }

    internal void Dispose() => enumerator?.Dispose();
}
