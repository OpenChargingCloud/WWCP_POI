using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// Bounded digest pairs belonging to one complete immutable snapshot context.
/// Admission preserves the traversal prefix; saturation never evicts useful ancestor entries.
/// </summary>
internal sealed class POIChildETagCache
{
    internal const Int32 DefaultMaxEntries = 4096;
    internal const Int64 DefaultMaxPayloadBytes = 1048576;
    internal const Int32 DefaultMaxKeyCharacters = 1024;

    private readonly Object gate = new();
    private readonly Dictionary<(String Kind, String Path), ImmutableArray<ETag>> entries = [];
    private readonly Int32 maxEntries;
    private readonly Int64 maxPayloadBytes;
    private readonly Int32 maxKeyCharacters;
    private Int64 payloadBytes;
    private Int64 hits;
    private Int64 misses;
    private Int64 rejected;

    /// <summary>
    /// Logical retained payload counts UTF-16 key characters and the two 32-byte digests.
    /// Object headers, array/dictionary capacity and synchronization overhead are additional.
    /// </summary>
    internal readonly record struct State(Int32 Entries, Int64 PayloadBytes, Int64 Hits, Int64 Misses, Int64 Rejected);

    internal POIChildETagCache(Int32 maxEntries = DefaultMaxEntries,
                              Int64 maxPayloadBytes = DefaultMaxPayloadBytes,
                              Int32 maxKeyCharacters = DefaultMaxKeyCharacters)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(maxEntries);
        ArgumentOutOfRangeException.ThrowIfNegative(maxPayloadBytes);
        ArgumentOutOfRangeException.ThrowIfNegative(maxKeyCharacters);
        this.maxEntries = maxEntries;
        this.maxPayloadBytes = maxPayloadBytes;
        this.maxKeyCharacters = maxKeyCharacters;
    }

    internal State Statistics
    {
        get { lock (gate) return new(entries.Count, payloadBytes, hits, misses, rejected); }
    }

    internal Boolean TryGet(String kind, String path, out ImmutableArray<ETag> tags)
    {
        lock (gate)
        {
            if (entries.TryGetValue((kind, path), out tags)) { hits++; return true; }
            misses++; return false;
        }
    }

    /// <summary>
    /// Admit only successful immutable pairs. Callers compute outside the lock; concurrent
    /// misses may repeat work, but only one pair is retained and failures cannot poison entries.
    /// </summary>
    internal void Add(String kind, String path, ImmutableArray<ETag> tags)
    {
        ETag.ValidatePair(tags, "Child ETags");
        var characters = (Int64) kind.Length + path.Length;
        var bytes = 64 + 2 * characters;
        lock (gate)
        {
            if (entries.ContainsKey((kind, path))) return;
            if (entries.Count >= maxEntries || characters > maxKeyCharacters || bytes > maxPayloadBytes - payloadBytes)
            {
                rejected++; return;
            }
            entries.Add((kind, path), tags);
            payloadBytes += bytes;
        }
    }
}
