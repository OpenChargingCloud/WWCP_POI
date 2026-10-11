/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Reflection;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Benchmarks;

internal sealed record ChildETagState(Int32 Entries, Int64 PayloadBytes, Int64 Hits, Int64 Misses, Int64 Rejected);
internal sealed record ChildETagRetention(Int64 ManagedBeforeBytes, Int64 ManagedAfterBytes,
    Int64 ManagedDeltaBytes, ChildETagState State);

// Fresh immutable maps and their root hashes are prepared outside the measured window.
// Cold samples never inherit a preceding warmup's child cache. Warm samples share one map.
internal sealed class ChildETagProbe
{
    internal static readonly String[] Operations = ["child-etags-json-cold", "child-etags-json-warm",
        "child-etags-cbor-cold", "child-etags-cbor-warm"];
    private readonly JObject input;
    private readonly Byte[] expected;
    private readonly OperationResult result;
    private readonly Boolean cold;
    private readonly Boolean cbor;
    private RoamingNetworkDataSnapshot snapshot;
    private JObject? latestJSON;
    private Byte[]? latestCBOR;
    private static readonly PropertyInfo? CacheProperty = typeof(RoamingNetworkDataSnapshot)
        .GetProperty("ChildETags", BindingFlags.Instance | BindingFlags.NonPublic);

    internal ChildETagRetention Retention { get; }
    internal ChildETagState State => Describe(snapshot);

    internal ChildETagProbe(String operation, RoamingNetworkDataSnapshot source)
    {
        cold = operation.EndsWith("-cold", StringComparison.Ordinal);
        cbor = operation.Contains("-cbor-", StringComparison.Ordinal);
        input = source.ToJSON();
        expected = cbor ? source.ToCBOR(IncludeVersionMetadata: true) :
                         CanonicalJSON.ToUTF8Bytes(source.ToJSONWithETags(IncludeVersionMetadata: true));
        result = new(expected.Length, 0, ETag.Compute(cbor ? ETagFormat.CBOR : ETagFormat.JSON, expected).ToString());
        // Approximate retained managed heap delta: keep the same parsed map and precomputed
        // root pair alive, discard the export document, and collect before/after first export.
        // This is separate from per-operation allocation, not an object-size guarantee.
        snapshot = Fresh();
        _ = Describe(snapshot); // Initialize reflection/empty context before the first collection.
        var before = CollectedBytes();
        Populate(snapshot);
        var after = CollectedBytes();
        Retention = new(before, after, after - before, Describe(snapshot));
        GC.KeepAlive(snapshot);
        snapshot = Fresh();
        if (!cold) Populate(snapshot);
    }

    internal void Prepare()
    {
        latestJSON = null; latestCBOR = null;
        if (cold) snapshot = Fresh();
    }

    internal OperationResult Run()
    {
        if (cbor)
        {
            latestCBOR = snapshot.ToCBOR(IncludeVersionMetadata: true);
            return new(latestCBOR.Length, 0, ETag.Compute(ETagFormat.CBOR, latestCBOR).ToString());
        }
        latestJSON = snapshot.ToJSONWithETags(IncludeVersionMetadata: true);
        return result; // Match the existing document-stage consumption outside measurement.
    }

    internal void Verify()
    {
        var bytes = cbor ? latestCBOR! : CanonicalJSON.ToUTF8Bytes(latestJSON!);
        if (!bytes.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException("Child-cache export bytes changed.");
    }

    private RoamingNetworkDataSnapshot Fresh()
    {
        var value = RoamingNetworkDataSnapshot.Parse(input);
        _ = value.ETags; // Root hash computation is explicitly excluded in both cold and warm calls.
        return value;
    }

    private static void Populate(RoamingNetworkDataSnapshot value) => _ = value.ToJSONWithETags(IncludeVersionMetadata: true);
    private static Int64 CollectedBytes()
    {
        GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
        GC.WaitForPendingFinalizers();
        return GC.GetTotalMemory(forceFullCollection: true);
    }

    private static ChildETagState Describe(RoamingNetworkDataSnapshot value)
    {
        // Old production builds have no cache. Reflection remains outside timed calls.
        if (CacheProperty is null) return new(0, 0, 0, 0, 0);
        var cache = CacheProperty.GetValue(value)!;
        var state = cache.GetType().GetProperty("Statistics", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(cache)!;
        Int64 Read(String name) => Convert.ToInt64(state.GetType().GetProperty(name)!.GetValue(state));
        return new((Int32) Read("Entries"), Read("PayloadBytes"), Read("Hits"), Read("Misses"), Read("Rejected"));
    }
}
