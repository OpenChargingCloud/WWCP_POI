/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;
using static WWCP_POI_Tests.Interoperability.ArchiveStreamingTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class ArchiveStreamingFailureTests
{
    [Test, Combinatorial]
    public void Mid_encoding_depth_failure_preserves_active_file_inventory_runtime_and_retry(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean publish)
    {
        using var fixture = new Fixture(profile, persistent: true); var history = fixture.History;
        if (profile == 4)
            Publish(history, Prepare(history, history.Head.Id, "encoding-prefix", Rename(new String('n', 64000))));
        var before = new Observation(history); var disk = File.ReadAllBytes(fixture.ArchivePath);
        // 60 arrays fit the standalone snapshot commit's depth. The enclosing archive contributes
        // two more containers. Earlier retained payloads force output before the real codec error.
        var deep = Value(new String('[', 60) + "0" + new String(']', 60));
        var candidate = history.PrepareSnapshot(history.Head.Id, InteropFixture.Time.AddDays(24),
            ImmutableDictionary<String, String>.Empty.Add("en", new String('p', 64000)),
            ImmutableDictionary<String, JsonElement>.Empty.Add("deep", deep));
        Assert.That(() => candidate.ToCBOR(), Throws.Nothing, "Standalone canonical CBOR is valid.");
        var stages = new List<ArchiveWriteStage>(); history.ArchiveWriteObserver = stages.Add;
        var success = publish ? history.TryPublish(history.Head.Id, candidate, out var result) : history.TryStoreCommit(candidate, out result);
        Assert.That(success, Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.PersistenceFailure));
        Assert.That(result.Error, Does.Contain("depth"));
        Assert.That(stages, Is.EqualTo(new[] { ArchiveWriteStage.BeforeTemporaryWrite }));
        Assert.That(File.ReadAllBytes(fixture.ArchivePath), Is.EqualTo(disk));
        Assert.That(System.IO.Directory.EnumerateFiles(fixture.Directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        before.AssertUnchanged(history); history.ArchiveWriteObserver = null;

        // The same candidate in a nonpersistent history reaches the actual caller-owned output.
        using var memory = RoamingNetworkHistory.ParseCBOR(disk, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Store(memory, candidate);
        var head = memory.Head; using var partial = new Destination();
        Assert.That(() => memory.WriteCBOR(partial), Throws.TypeOf<CBORException>());
        Assert.That(partial.Bytes.Length, Is.GreaterThan(0));
        Assert.That(partial.FlushCalls, Is.Zero); Assert.That(partial.DisposeCalls, Is.Zero);
        Assert.That(memory.Head, Is.SameAs(head));

        var valid = Prepare(history, history.Head.Id, "after-encoder-error", Rename("Recovered encoder"));
        Publish(history, valid);
        Assert.That(File.ReadAllBytes(fixture.ArchivePath), Is.EqualTo(ReferenceCBOR(history)));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        history.Dispose();
        using var reopened = RoamingNetworkHistory.Open(fixture.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Assert.That(reopened.Head.Id, Is.EqualTo(valid.Id));
        Assert.That(reopened.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
    }

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Incremental_retention_review_and_cold_active_output_match_the_whole_value_codec(Int32 profile)
    {
        using var fixture = new Fixture(profile, persistent: true); var history = fixture.History;
        var snapshot = Snapshot(history, 25); Publish(history, snapshot);
        var source = ReferenceCBOR(history); var plan = Plan(history, snapshot);
        Assert.That(plan.SourceArchiveETag, Is.EqualTo(ETag.Compute(ETagFormat.CBOR, source)));
        var head = history.Head; var runtime = head.Network.ToJSONSnapshot().ToString();
        var path = Path.Combine(fixture.Directory.DirectoryPath, "reviewed-cold.cbor");
        var coldStages = new List<RetentionWriteStage>(); var activeStages = new List<ArchiveWriteStage>();
        history.RetentionWriteObserver = stage => {
            coldStages.Add(stage);
            if (stage == RetentionWriteStage.ColdArchiveVerified)
            {
                // The retained verification handle has read/write access. A cooperating reader
                // must permit that existing access while requesting only read access itself.
                using var reader = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var bytes = new MemoryStream(); reader.CopyTo(bytes);
                Assert.That(bytes.ToArray(), Is.EqualTo(source));
                Assert.That(() => { using var exclusive = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None); },
                    Throws.TypeOf<IOException>(), "The verified cold handle remains held.");
            }
        };
        history.ArchiveWriteObserver = activeStages.Add;
        var result = Prune(history, plan, path);
        Assert.That(coldStages, Is.EqualTo(new[] { RetentionWriteStage.BeforeColdTemporaryWrite,
            RetentionWriteStage.ColdTemporaryFileFlushed, RetentionWriteStage.ColdArchivePublished, RetentionWriteStage.ColdArchiveVerified }));
        Assert.That(activeStages, Is.EqualTo(new[] { ArchiveWriteStage.BeforeTemporaryWrite,
            ArchiveWriteStage.TemporaryFileFlushed, ArchiveWriteStage.ArchiveReplaced }));
        Assert.That(result.Receipt!.SourceArchiveETag, Is.EqualTo(plan.SourceArchiveETag));
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(source));
        Assert.That(File.ReadAllBytes(fixture.ArchivePath), Is.EqualTo(ReferenceCBOR(history)));
        Assert.That(history.Head, Is.SameAs(head)); Assert.That(head.Network.ToJSONSnapshot().ToString(), Is.EqualTo(runtime));
        Assert.That(System.IO.Directory.EnumerateFiles(fixture.Directory.DirectoryPath, "*.tmp-*"), Is.Empty);
    }

    [TestCase(0)]
    [TestCase(23)]
    [TestCase(24)]
    [TestCase(255)]
    [TestCase(256)]
    [TestCase(65535)]
    [TestCase(65536)]
    public void Definite_array_counts_match_Styx_at_every_integer_width_boundary(Int32 count)
    {
        var values = Enumerable.Range(0, count).Select(value => CBORValue.FromInt64(value)).ToArray();
        using var destination = new Destination(7);
        using (var writer = new POIArchiveCBORWriter(destination))
        {
            writer.Array(count, values, (output, value) => output.Value(value)); writer.Complete();
        }
        Assert.That(destination.Bytes, Is.EqualTo(CBORValue.FromArray(values).ToByteArray(CBORWriterOptions.Canonical)));
        Assert.That(destination.FlushCalls, Is.Zero); Assert.That(destination.DisposeCalls, Is.Zero);
    }

    [Test]
    public void Map_sorting_keeps_encoded_key_order_and_rejects_duplicate_keys_before_output()
    {
        using var destination = new Destination();
        using (var writer = new POIArchiveCBORWriter(destination))
        {
            writer.Map(("aaa", value => value.Text("long")), ("z", value => value.Text("short")), ("ä", value => value.Text("unicode")));
            writer.Complete();
        }
        var reference = RoamingNetworkCommit.Map(("aaa", CBORValue.FromText("long")),
            ("z", CBORValue.FromText("short")), ("ä", CBORValue.FromText("unicode"))).ToByteArray(CBORWriterOptions.Canonical);
        Assert.That(destination.Bytes, Is.EqualTo(reference));
        using var duplicate = new Destination();
        using (var writer = new POIArchiveCBORWriter(duplicate))
            Assert.That(() => writer.Map(("a", output => output.Text("one")), ("a", output => output.Text("two"))), Throws.ArgumentException);
        Assert.That(duplicate.WriteCalls, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Combined_depth_allows_64_and_rejects_65_after_partial_output(Boolean encoded)
    {
        static CBORValue Nested(Int32 depth)
        {
            var value = CBORValue.FromInt64(0);
            for (var index = 0; index < depth; index++) value = CBORValue.FromArray([value]);
            return value;
        }
        using var valid = new Destination();
        using (var writer = new POIArchiveCBORWriter(valid))
        {
            writer.Map(("value", output => {
                if (encoded) output.Encoded(Nested(63).ToByteArray(CBORWriterOptions.Canonical)); else output.Value(Nested(63));
            }));
            writer.Complete();
        }
        Assert.That(valid.Bytes, Is.EqualTo(RoamingNetworkCommit.Map(("value", Nested(63))).ToByteArray(CBORWriterOptions.Canonical)));
        using var invalid = new Destination();
        using (var writer = new POIArchiveCBORWriter(invalid))
            Assert.That(() => writer.Map(("big", output => output.Text(new String('x', 64000))), ("deep", output => {
                if (encoded) output.Encoded(Nested(64).ToByteArray(CBORWriterOptions.Canonical)); else output.Value(Nested(64));
            })), Throws.TypeOf<CBORException>());
        Assert.That(invalid.Bytes.Length, Is.GreaterThan(0));
        Assert.That(invalid.Bytes.Length, Is.LessThan(64000), "Disposal must not emit the pending buffer after a codec failure.");
        Assert.That(invalid.FlushCalls, Is.Zero); Assert.That(invalid.DisposeCalls, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Incorrect_declared_counts_and_trailing_encoded_values_are_rejected(Boolean excessive)
    {
        using var destination = new Destination();
        using (var writer = new POIArchiveCBORWriter(destination))
            Assert.That(() => writer.Array(excessive ? 0 : 2, new[] { 1 }, (output, value) => output.Value(CBORValue.FromInt64(value))), Throws.ArgumentException);
        Assert.That(destination.WriteCalls, Is.Zero);
        using var trailing = new Destination();
        using (var writer = new POIArchiveCBORWriter(trailing))
            Assert.That(() => writer.Encoded([0, 1]), Throws.ArgumentException);
        Assert.That(trailing.WriteCalls, Is.Zero);
    }

    [Test]
    public void Hash_stream_forwards_exact_bytes_borrows_destination_and_rejects_writes_after_completion()
    {
        using var destination = new Destination(1);
        using (var hashing = new POIArchiveHashStream(destination))
        {
            hashing.Write([1, 2]); hashing.Write([3, 4, 5]);
            Assert.That(hashing.Length, Is.EqualTo(5));
            Assert.That(hashing.Complete().Digest.ToArray(), Is.EqualTo(SHA256.HashData(new Byte[] { 1, 2, 3, 4, 5 })));
            Assert.That(() => hashing.Write([6]), Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => hashing.Complete(), Throws.TypeOf<InvalidOperationException>());
        }
        Assert.That(destination.Bytes, Is.EqualTo(new Byte[] { 1, 2, 3, 4, 5 }));
        Assert.That(destination.DisposeCalls, Is.Zero); Assert.That(destination.FlushCalls, Is.Zero);
    }
}
