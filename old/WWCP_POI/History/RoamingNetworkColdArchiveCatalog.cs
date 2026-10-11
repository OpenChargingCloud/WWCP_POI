/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;

namespace cloud.charging.open.protocols.WWCP.POI;

/// <summary>
/// An immutable local location claim for exact CBOR archive bytes, without establishing trust.
/// </summary>
public sealed record RoamingNetworkColdArchiveLocation
{
    /// <summary>
    /// The expected CBOR archive digest, independent of the local filename.
    /// </summary>
    public ETag ArchiveETag { get; }

    /// <summary>
    /// The absolute local path resolved when constructing this location.
    /// </summary>
    public String Path { get; }

    /// <summary>
    /// Capture a valid CBOR digest and local path without accessing the file.
    /// </summary>
    public RoamingNetworkColdArchiveLocation(ETag archiveETag, String path)
    {
        if (!archiveETag.IsValid || archiveETag.Format != ETagFormat.CBOR)
            throw new ArgumentException("A valid CBOR archive digest is required.", nameof(archiveETag));
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArchiveETag = archiveETag; Path = System.IO.Path.GetFullPath(path);
    }
}

/// <summary>
/// An immutable, explicitly supplied local digest-to-path catalog in caller-selected candidate order.
/// Catalog entries are location claims; reading always verifies bytes and current trust independently.
/// </summary>
public sealed class RoamingNetworkColdArchiveCatalog
{
    private readonly ImmutableDictionary<ETag, ImmutableArray<String>> candidates;

    /// <summary>
    /// Unique local digest/path claims in their original registration order.
    /// Windows paths compare without case; other platforms use ordinal path comparison.
    /// </summary>
    public ImmutableArray<RoamingNetworkColdArchiveLocation> Locations { get; }

    /// <summary>
    /// Maximum supplied location count, including duplicates, before deduplication.
    /// </summary>
    public Int32 MaxLocations { get; }

    /// <summary>
    /// Freeze bounded location claims without scanning directories or reading files.
    /// Construct a new catalog to register moved files; historical receipts remain unchanged.
    /// </summary>
    public RoamingNetworkColdArchiveCatalog(IEnumerable<RoamingNetworkColdArchiveLocation> locations, Int32 maxLocations = 4096)
    {
        ArgumentNullException.ThrowIfNull(locations);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxLocations, 1);
        MaxLocations = maxLocations;
        var comparer = OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
        var paths = new Dictionary<ETag, List<String>>();
        var registered = new Dictionary<ETag, HashSet<String>>();
        var unique = ImmutableArray.CreateBuilder<RoamingNetworkColdArchiveLocation>();
        Int64 count = 0;
        foreach (var location in locations)
        {
            if (++count > maxLocations) throw new ArgumentException("Cold archive catalog exceeds its local location budget.", nameof(locations));
            ArgumentNullException.ThrowIfNull(location);
            if (!paths.TryGetValue(location.ArchiveETag, out var values))
            {
                paths.Add(location.ArchiveETag, values = []);
                registered.Add(location.ArchiveETag, new(comparer));
            }
            if (!registered[location.ArchiveETag].Add(location.Path)) continue;
            values.Add(location.Path); unique.Add(location);
        }
        Locations = unique.ToImmutable();
        candidates = paths.ToImmutableDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray());
    }

    /// <summary>
    /// Return ordered local path candidates, or an initialized empty array for an unknown digest.
    /// </summary>
    public ImmutableArray<String> GetCandidates(ETag archiveETag)
    {
        if (!archiveETag.IsValid || archiveETag.Format != ETagFormat.CBOR)
            throw new ArgumentException("A valid CBOR archive digest is required.", nameof(archiveETag));
        return candidates.GetValueOrDefault(archiveETag, []);
    }
}
