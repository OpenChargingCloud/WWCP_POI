/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Buffers;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;
using static WWCP_POI_Tests.Interoperability.ArchiveStreamingTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class DirectArchivePOICBORTests
{
    [Test, Combinatorial]
    public void All_archive_profiles_preserve_exact_bytes_peers_and_local_runtime(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(1, 7, 16384)] Int32 segment)
    {
        using var fixture = new Fixture(profile);
        var history = fixture.History; var before = new Observation(history);
        var expected = ReferenceCBOR(history);
        using var destination = new Destination(segment);
        history.WriteCBOR(destination);
        Assert.That(destination.Bytes, Is.EqualTo(expected));
        Assert.That(history.ToCBOR(), Is.EqualTo(expected));
        Assert.That(destination.FlushCalls, Is.Zero); Assert.That(destination.DisposeCalls, Is.Zero);
        before.AssertUnchanged(history);
        using var restored = RoamingNetworkHistory.ParseCBOR(destination.Bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Assert.That(restored.Head.Id, Is.EqualTo(history.Head.Id));
        Assert.That(restored.Head.Snapshot.ETags, Is.EqualTo(history.Head.Snapshot.ETags));
        foreach (var commit in restored.Commits)
            foreach (var peer in commit.Signatures) Assert.That(VerifyCommit(commit, peer), Is.True);
        Assert.That(restored.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
    }

    [Test, Combinatorial]
    public void Preflight_matches_standalone_writer_and_remaining_SkipValue_depth(
        [Values(0, 1, 4)] Int32 envelopeDepth, [Values(59, 60, 61, 62, 63, 64)] Int32 count,
        [Values("0", "1.10", "18446744073709551616", "[]", "{}", "measurement")] String scalar)
    {
        var json = scalar == "measurement" ? "{\"alt\":\"1.10 m\"}" : scalar;
        for (var index = 0; index < count; index++) json = "{\"geoLocation\":" + json + "}";
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 128 });
        Byte[]? reference = null;
        try
        {
            var encoded = new ArrayBufferWriter<Byte>(); new POICBORWriter(encoded).Write(document.RootElement, "GeoLocation");
            var bytes = encoded.WrittenSpan.ToArray();
            var reader = new CBORReader(bytes, new CBORReaderOptions { MaxDepth = 64 - envelopeDepth });
            reader.SkipValue(); Assert.That(reader.BytesRemaining, Is.Zero);
            reference = bytes;
        }
        catch (CBORException) { }
        var output = new ArrayBufferWriter<Byte>();
        void Direct()
        {
            new POICBORPreflight(64 - envelopeDepth).Validate(document.RootElement, "GeoLocation");
            new POICBORWriter(output).Write(document.RootElement, "GeoLocation");
        }
        if (reference is null)
        {
            Assert.Throws<CBORException>(Direct);
            Assert.That(output.WrittenCount, Is.Zero, "The rejected payload must not begin emission.");
        }
        else { Direct(); Assert.That(output.WrittenSpan.ToArray(), Is.EqualTo(reference)); }
    }

    [TestCase("{\"first\":1,\"first\":2}")]
    [TestCase("{\"value\":1e65537}")]
    [TestCase("{\"value\":1e-65537}")]
    [TestCase("{\"maxPower\":\"invalid\"}")]
    [TestCase("{\"ETags\":[]}")]
    public void Invalid_values_fail_before_payload_output_and_valid_retry_succeeds(String json)
    {
        using var document = JsonDocument.Parse(json);
        var output = new ArrayBufferWriter<Byte>();
        Assert.Catch<Exception>(() => {
            new POICBORPreflight(60).Validate(document.RootElement, nameof(EVSE));
            new POICBORWriter(output).Write(document.RootElement, nameof(EVSE));
        });
        Assert.That(output.WrittenCount, Is.Zero);
        using var valid = JsonDocument.Parse("{\"maxPower\":\"250 kW\",\"customData\":{\"ETags\":[],\"power\":\"250 kW\"}}");
        new POICBORPreflight(60).Validate(valid.RootElement, nameof(EVSE));
        new POICBORWriter(output).Write(valid.RootElement, nameof(EVSE));
        var expected = new ArrayBufferWriter<Byte>(); new POICBORWriter(expected).Write(valid.RootElement, nameof(EVSE));
        Assert.That(output.WrittenSpan.ToArray(), Is.EqualTo(expected.WrittenSpan.ToArray()));
    }

    [Test, Combinatorial]
    public void Real_snapshot_preflight_preserves_the_previous_partial_output_boundary(
        [Values(1, 3, 4)] Int32 envelopeDepth, [Values(58, 59, 60)] Int32 arrays)
    {
        var source = DeepNetwork(arrays).DataSnapshot;
        var encoded = source.ToCBOR(IncludeVersionMetadata: true);
        using var oldOutput = new Destination(); using var directOutput = new Destination();
        Exception? previous = null;
        try { Envelope(oldOutput, envelopeDepth, writer => writer.Encoded(encoded)); }
        catch (CBORException error) { previous = error; }
        if (previous is null) Envelope(directOutput, envelopeDepth, writer => writer.POI(source));
        else Assert.Throws<CBORException>(() => Envelope(directOutput, envelopeDepth, writer => writer.POI(source)));
        Assert.That(directOutput.Bytes, Is.EqualTo(oldOutput.Bytes));
        Assert.That(directOutput.FlushCalls, Is.Zero); Assert.That(directOutput.DisposeCalls, Is.Zero);
        Assert.That(source.ToCBOR(IncludeVersionMetadata: true), Is.EqualTo(encoded));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Real_snapshot_state_depth_failure_is_atomic_and_retry_preserves_runtime(Boolean publish)
    {
        using var directory = new ArchiveDirectory();
        using (var initial = History(DeepNetwork(59)))
        {
            Store(initial, Sign(initial.Head.Commit));
            Publish(initial, Prepare(initial, initial.Head.Id, "prefix", Rename(new String('p', 64000))));
            File.WriteAllBytes(directory.ArchivePath, initial.ToCBOR());
        }
        using var history = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit);
        Status(history, EvseTarget, "charging");
        var before = new Observation(history); var disk = File.ReadAllBytes(directory.ArchivePath);
        var candidate = Snapshot(history, 27);
        Assert.That(() => candidate.ToCBOR(), Throws.Nothing);
        var stages = new List<ArchiveWriteStage>(); history.ArchiveWriteObserver = stages.Add;
        var success = publish ? history.TryPublish(history.Head.Id, candidate, out var result) : history.TryStoreCommit(candidate, out result);
        Assert.That(success, Is.False); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.PersistenceFailure));
        Assert.That(result.Error, Does.Contain("depth"));
        Assert.That(stages, Is.EqualTo(new[] { ArchiveWriteStage.BeforeTemporaryWrite }));
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(disk));
        Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        before.AssertUnchanged(history); history.ArchiveWriteObserver = null;
        var retry = Prepare(history, history.Head.Id, "retry", Rename("Recovered direct state")); Publish(history, retry);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(ReferenceCBOR(history)));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Destination_failure_keeps_history_and_supports_exact_retry(Int32 profile)
    {
        using var fixture = new Fixture(profile); var history = fixture.History; var before = new Observation(history);
        using var failed = new Destination(7, failAfter: 20000);
        Assert.Throws<IOException>(() => history.WriteCBOR(failed));
        Assert.That(failed.Bytes.Length, Is.EqualTo(20000));
        Assert.That(failed.FlushCalls, Is.Zero); Assert.That(failed.DisposeCalls, Is.Zero); before.AssertUnchanged(history);
        using var retry = new Destination(1); history.WriteCBOR(retry);
        Assert.That(retry.Bytes, Is.EqualTo(ReferenceCBOR(history)));
    }

    private static void Envelope(Destination destination, Int32 depth, Action<POIArchiveCBORWriter> payload)
    {
        using var writer = new POIArchiveCBORWriter(destination);
        void Nest(POIArchiveCBORWriter value, Int32 remaining)
        {
            if (remaining == 0) payload(value);
            else value.Map(("a", item => item.Text(new String('p', 64000))), ("payload", item => Nest(item, remaining - 1)));
        }
        Nest(writer, depth); writer.Complete();
    }

    private static RoamingNetwork DeepNetwork(Int32 arrays)
    {
        var json = Network().DataSnapshot.ToJSON(); json.Remove("ETags");
        var deep = new String('[', arrays) + "0" + new String(']', arrays);
        json["customData"] = JObject.Parse("{\"deep\":" + deep + "}");
        return RoamingNetwork.Parse(json);
    }
}
