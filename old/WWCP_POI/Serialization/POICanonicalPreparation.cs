/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text.Json;
using org.GraphDefined.Vanaheimr.Illias;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Reuse successful unsigned canonical content within synchronous recovery or peer verification.
/// The outer scope releases all entries; keys and trust decisions are never retained.
/// </summary>
internal sealed class POICanonicalPreparation
{
    internal const Int32 MaxEntries = 64;
    internal const Int32 MaxValueBytes = 1024 * 1024;
    internal const Int32 MaxTotalBytes = 4 * 1024 * 1024;
    [ThreadStatic] private static POICanonicalPreparation? current;
    private readonly Dictionary<Object, Content> values = new(ReferenceEqualityComparer.Instance);
    internal static POICanonicalPreparation? Current => current;
    internal Int32 Entries => values.Count;
    internal Int32 Bytes { get; private set; }

    internal readonly struct Scope(POICanonicalPreparation? owner) : IDisposable
    {
        public void Dispose()
        {
            if (owner is null || !ReferenceEquals(current, owner)) return;
            current = null;
            owner.values.Clear(); owner.Bytes = 0;
        }
    }

    internal static Scope Enter()
    {
        if (current is not null) return new(null);
        current = new(); return new(current);
    }

    internal sealed class Content(Byte[] bytes, Int32 depth)
    {
        internal ReadOnlySpan<Byte> Span => bytes;
        internal Int32 Depth => depth;
        internal Byte[] Copy() => (Byte[]) bytes.Clone();
    }

    internal static Content Capture(Action<Utf8JsonWriter> write)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream)) write(writer);
        using var document = JsonDocument.Parse(stream.ToArray());
        var bytes = CanonicalJSON.ToUTF8Bytes(document);
        var reader = new Utf8JsonReader(bytes);
        var depth = 0;
        while (reader.Read())
            if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                depth = Math.Max(depth, reader.CurrentDepth + 1);
        return new(bytes, depth);
    }

    internal static Content Get(Object owner, Action<Utf8JsonWriter> write)
    {
        if (current is { } context && context.values.TryGetValue(owner, out var found)) return found;
        var content = Capture(write);
        Admit(owner, content);
        return content;
    }

    // Constructor callers admit only after checking the declared content identity.
    internal static void Admit(Object owner, Content content)
    {
        if (current is not { } context || context.values.ContainsKey(owner) || context.values.Count >= MaxEntries ||
            content.Span.Length > MaxValueBytes || content.Span.Length > MaxTotalBytes - context.Bytes) return;
        context.values.Add(owner, content); context.Bytes += content.Span.Length;
    }

    // Share only an existing validated immutable payload within the current scope. Admission
    // counts each alias conservatively against the same limits; no preparation is forced here.
    internal static void Share(Object source, Object copy)
    {
        if (current is { } context && context.values.TryGetValue(source, out var content)) Admit(copy, content);
    }

    internal static Byte[] SigningBytes(Object owner, Action<Utf8JsonWriter> writeContent,
        Action<Utf8JsonWriter> writeHeader, String field, Func<Byte[]> original)
    {
        // Individual calls cannot reuse unsigned bytes; keep their preceding complete factory.
        if (current is null) return original();
        Content content;
        try { content = Get(owner, writeContent); }
        catch (Exception error) when (error is JsonException or ArgumentException or InvalidOperationException or FormatException)
        {
            // Preserve the original envelope's validation order and diagnostics for invalid inputs.
            return original();
        }
        // The signature envelope adds one container to the fixed JsonDocument depth contract.
        if (content.Depth >= 64) return original();
        var header = Capture(writeHeader);
        var reader = new Utf8JsonReader(header.Span);
        while (reader.Read())
        {
            if (reader.TokenType != JsonTokenType.PropertyName || reader.CurrentDepth != 1 || reader.GetString() != field) continue;
            if (!reader.Read() || reader.TokenType != JsonTokenType.Null)
                throw new InvalidOperationException("The canonical signature header requires a null payload placeholder.");
            var start = checked((Int32) reader.TokenStartIndex);
            var end = checked((Int32) reader.BytesConsumed);
            var result = new Byte[checked(header.Span.Length - (end - start) + content.Span.Length)];
            header.Span[..start].CopyTo(result);
            content.Span.CopyTo(result.AsSpan(start));
            header.Span[end..].CopyTo(result.AsSpan(start + content.Span.Length));
            return result;
        }
        throw new InvalidOperationException("The canonical signature header is missing its payload placeholder.");
    }
}
