using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class ChildETagCacheTests
{
    [Test, Combinatorial]
    public void Cold_and_warm_export_match_uncached_reference(
        [Values(false, true)] Boolean runtime, [Values(false, true)] Boolean version,
        [Values(ETagDigestEncoding.HEX, ETagDigestEncoding.Base64)] ETagDigestEncoding encoding)
    {
        var network = Network(); var snapshot = network.DataSnapshot;
        var expected = CanonicalJSON.ToUTF8Bytes(Reference(network, runtime, encoding, version));
        Assert.That(snapshot.ChildETags.Statistics.Entries, Is.Zero);
        var cold = network.ToJSONWithETags(runtime, encoding, version);
        Assert.That(CanonicalJSON.ToUTF8Bytes(cold), Is.EqualTo(expected));
        var initial = snapshot.ChildETags.Statistics;
        Assert.That(initial.Entries, Is.Positive); Assert.That(initial.Misses, Is.Positive);
        // Caller mutations of a returned transport DOM cannot alter cached digests or future views.
        cold.RemoveAll();
        var warm = network.ToJSONWithETags(runtime, encoding, version);
        Assert.That(CanonicalJSON.ToUTF8Bytes(warm), Is.EqualTo(expected));
        Assert.That(snapshot.ChildETags.Statistics.Misses, Is.EqualTo(initial.Misses));
        Assert.That(snapshot.ChildETags.Statistics.Hits, Is.Positive);
        Assert.That(network.ToCBOR(runtime, version), Is.EqualTo(Encode(Reference(network, runtime, ETagDigestEncoding.HEX, version), nameof(RoamingNetwork), null)));
    }

    [Test]
    public void Runtime_updates_remain_fresh_and_do_not_change_static_pairs()
    {
        var network = Network(); var snapshot = network.DataSnapshot;
        var staticBytes = CanonicalJSON.ToUTF8Bytes(network.ToJSONWithETags());
        var before = CanonicalJSON.ToUTF8Bytes(network.ToJSONWithETags(IncludeRuntime: true));
        network.ApplyRuntimeUpdate(new(network.Id.ToString(), POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
            POIRuntimeStatusKind.Status, new("charging", InteropFixture.Time.AddDays(1)), mode: POIRuntimeUpdateMode.ReplaceHistory));
        var after = CanonicalJSON.ToUTF8Bytes(network.ToJSONWithETags(IncludeRuntime: true));
        Assert.That(after, Is.Not.EqualTo(before));
        Assert.That(after, Is.EqualTo(CanonicalJSON.ToUTF8Bytes(Reference(network, true, ETagDigestEncoding.HEX, false))));
        Assert.That(CanonicalJSON.ToUTF8Bytes(network.ToJSONWithETags()), Is.EqualTo(staticBytes));
        Assert.That(network.DataSnapshot, Is.SameAs(snapshot));
    }

    [Test]
    public void Snapshot_only_revisions_share_context_but_changed_descendants_do_not()
    {
        var source = Network().DataSnapshot;
        _ = source.ToJSONWithETags();
        var revision = source.AdvanceSnapshotRevision().AdvanceSnapshotRevision();
        Assert.That(revision.Entities, Is.SameAs(source.Entities));
        Assert.That(revision.ChildETags, Is.SameAs(source.ChildETags));
        Assert.That(revision.ETags, Is.EqualTo(source.ETags));
        Assert.That(revision.Revision, Is.EqualTo(source.Revision + 2));
        Assert.That(revision.ToCBOR(IncludeVersionMetadata: true), Is.EqualTo(Encode(Reference(revision, false, ETagDigestEncoding.HEX, true), nameof(RoamingNetwork), null)));
        var next = source.ApplyChangeSet(Batch(source, "child-cache-update", Power("222 kW")));
        Assert.That(next.GetEntity(InfrastructureEntityType.ChargingStation, "DE*ABC*S1").Children,
                    Is.EqualTo(source.GetEntity(InfrastructureEntityType.ChargingStation, "DE*ABC*S1").Children),
                    "Stable owner identities and child keys do not identify descendant contents.");
        Assert.That(next.ChildETags, Is.Not.SameAs(source.ChildETags));
        Assert.That(next.ChildETags.Statistics.Entries, Is.Zero);
        Assert.That(next.ToCBOR(), Is.EqualTo(Encode(Reference(next, false, ETagDigestEncoding.HEX, false), nameof(RoamingNetwork), null)));
        Assert.That(next.ETags, Is.Not.EqualTo(source.ETags));
    }

    [Test]
    public void Static_snapshot_runtime_option_still_fails_after_warming()
    {
        var snapshot = Network().DataSnapshot; _ = snapshot.ToJSONWithETags();
        Assert.Throws<ArgumentException>(() => snapshot.ToJSONWithETags(IncludeRuntime: true));
        Assert.Throws<ArgumentException>(() => snapshot.ToCBOR(IncludeRuntime: true));
    }

    [TestCase(0, 1024, 1024, 0)]
    [TestCase(1, 1024, 1024, 1)]
    [TestCase(8, 72, 1024, 1)]
    [TestCase(8, 71, 1024, 0)]
    [TestCase(8, 1024, 3, 0)]
    public void Admission_obeys_entry_payload_and_key_bounds(Int32 count, Int64 bytes, Int32 characters, Int32 expected)
    {
        var cache = new POIChildETagCache(count, bytes, characters);
        cache.Add("K", "/aa", Pair(1)); // 4 UTF-16 characters + 64 digest bytes = 72 logical bytes.
        cache.Add("K", "/bb", Pair(2));
        var state = cache.Statistics;
        Assert.That(state.Entries, Is.EqualTo(expected)); Assert.That(state.PayloadBytes, Is.EqualTo(72 * expected));
        Assert.That(state.Rejected, Is.EqualTo(2 - expected));
        Assert.That(cache.TryGet("K", "/aa", out var retained), Is.EqualTo(expected > 0));
        if (expected > 0) Assert.That(retained, Is.EqualTo(Pair(1)));
        Assert.That(cache.TryGet("K", "/bb", out _), Is.False, "Saturation must preserve the admitted ancestor prefix.");
    }

    [Test]
    public void Schema_kind_and_path_identify_entries_and_duplicates_do_not_grow_them()
    {
        var cache = new POIChildETagCache();
        cache.Add("EVSE", "/a/0", Pair(1)); cache.Add("EVSE", "/a/1", Pair(2));
        cache.Add("ChargingCable", "/a/0", Pair(3));
        var before = cache.Statistics;
        cache.Add("EVSE", "/a/0", Pair(1));
        Assert.That(cache.Statistics, Is.EqualTo(before));
        Assert.That(cache.TryGet("EVSE", "/a/0", out var first), Is.True); Assert.That(first, Is.EqualTo(Pair(1)));
        Assert.That(cache.TryGet("EVSE", "/a/1", out var second), Is.True); Assert.That(second, Is.EqualTo(Pair(2)));
        Assert.That(cache.TryGet("ChargingCable", "/a/0", out var third), Is.True); Assert.That(third, Is.EqualTo(Pair(3)));
    }

    [Test]
    public void Invalid_pair_is_not_retained_and_a_valid_retry_succeeds()
    {
        var cache = new POIChildETagCache();
        Assert.Throws<ArgumentException>(() => cache.Add("EVSE", "/a/0", default));
        Assert.That(cache.Statistics.Entries, Is.Zero);
        cache.Add("EVSE", "/a/0", Pair(1));
        Assert.That(cache.TryGet("EVSE", "/a/0", out var pair), Is.True); Assert.That(pair, Is.EqualTo(Pair(1)));
    }

    [Test]
    public void Concurrent_cold_exports_are_exact_and_admit_one_pair_per_path()
    {
        var source = Network().DataSnapshot;
        var expected = CanonicalJSON.ToUTF8Bytes(Reference(source, false, ETagDigestEncoding.HEX, true));
        var expectedBase64 = CanonicalJSON.ToUTF8Bytes(Reference(source, false, ETagDigestEncoding.Base64, true));
        _ = source.ETags;
        using var start = new ManualResetEventSlim();
        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() => {
            start.Wait();
            for (var round = 0; round < 4; round++)
            {
                var base64 = i % 2 == 0;
                var bytes = CanonicalJSON.ToUTF8Bytes(source.ToJSONWithETags(DigestEncoding: base64 ? ETagDigestEncoding.Base64 : ETagDigestEncoding.HEX, IncludeVersionMetadata: true));
                Assert.That(bytes, Is.EqualTo(base64 ? expectedBase64 : expected));
            }
        })).ToArray();
        start.Set(); Task.WaitAll(tasks);
        var state = source.ChildETags.Statistics;
        Assert.That(state.Entries, Is.Positive); Assert.That(state.Hits, Is.Positive);
        Assert.That(state.Entries, Is.LessThanOrEqualTo(POIChildETagCache.DefaultMaxEntries));
        Assert.That(state.PayloadBytes, Is.LessThanOrEqualTo(POIChildETagCache.DefaultMaxPayloadBytes));
        Assert.That(state.Rejected, Is.Zero);
        var fresh = Network().DataSnapshot; _ = fresh.ToJSONWithETags();
        Assert.That(state.Entries, Is.EqualTo(fresh.ChildETags.Statistics.Entries));
        Assert.That(state.PayloadBytes, Is.EqualTo(fresh.ChildETags.Statistics.PayloadBytes));
    }

    [Test]
    public void Cache_lifetime_ends_with_its_snapshot_context()
    {
        var (snapshot, cache) = ReleasedContext();
        for (var attempt = 0; attempt < 3 && (snapshot.IsAlive || cache.IsAlive); attempt++)
        {
            GC.Collect(2, GCCollectionMode.Forced, blocking: true, compacting: true);
            GC.WaitForPendingFinalizers(); GC.Collect(2, GCCollectionMode.Forced, blocking: true);
        }
        Assert.That(snapshot.IsAlive, Is.False); Assert.That(cache.IsAlive, Is.False);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference Snapshot, WeakReference Cache) ReleasedContext()
    {
        var value = Network().DataSnapshot; _ = value.ToJSONWithETags();
        return (new(value), new(value.ChildETags));
    }

    private static ImmutableArray<ETag> Pair(Byte seed)
        => [ETag.Compute(ETagFormat.JSON, [seed]), ETag.Compute(ETagFormat.CBOR, [seed])];

    // Pre-cache export algorithm: freshly hash every declared child from its static projection.
    // These unchanged private value codecs never read the child cache.
    private static readonly Func<IImmutablePOI, Boolean, JObject> Document = Bind<Func<IImmutablePOI, Boolean, JObject>>("Document");
    private static readonly Action<JObject, String, Boolean, Boolean> Prepare = Bind<Action<JObject, String, Boolean, Boolean>>("Prepare");
    private static readonly Func<JObject, String, Byte[]?, Byte[]> Encode = Bind<Func<JObject, String, Byte[]?, Byte[]>>("Encode");
    private static readonly HashSet<String> TaggedKinds = (HashSet<String>) typeof(POIRepresentation)
        .GetField("TaggedKinds", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
    private static T Bind<T>(String name) where T : Delegate
        => typeof(POIRepresentation).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<T>();

    private static JObject Reference(IImmutablePOI value, Boolean runtime, ETagDigestEncoding encoding, Boolean version)
    {
        var json = Document(value, runtime); Prepare(json, nameof(RoamingNetwork), runtime, version);
        POIRepresentation.Visit(json, nameof(RoamingNetwork), "", (node, kind, path) => {
            if (path.Length > 0 && !TaggedKinds.Contains(kind)) return;
            var content = (JObject) node.DeepClone(); Prepare(content, kind, false, false);
            var canonical = CanonicalJSON.ToUTF8Bytes(content);
            var tags = path.Length == 0 ? value.ETags :
                ImmutableArray.Create(ETag.Compute(ETagFormat.JSON, canonical), ETag.Compute(ETagFormat.CBOR, Encode(content, kind, canonical)));
            node[POIContentProfile.PropertyName] = POIContentProfile.Id;
            node["ETags"] = new JArray(tags.Select(tag => tag.ToJSON(encoding)));
        });
        return json;
    }
}
