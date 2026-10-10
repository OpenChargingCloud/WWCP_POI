/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Buffers;
using System.Collections.Immutable;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.ArchiveStreamingTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class DirectArchiveChangeSetTests
{
    [Test, Combinatorial]
    public void Signed_scalar_preflight_matches_buffered_writer_and_actual_remaining_reader(
        [Values(0, 1, 4)] Int32 envelopeDepth, [Values(59, 60, 61, 62, 63, 64)] Int32 count,
        [Values("0", "1.10", "1e0", "-0", "\"150 kW\"", "\"4.50 m\"", "[]", "{}")] String scalar)
    {
        var json = scalar;
        for (var index = 0; index < count; index++) json = "{\"value\":" + json + "}";
        using var document = JsonDocument.Parse(json, new JsonDocumentOptions { MaxDepth = 128 });
        var path = String.Concat(Enumerable.Repeat("/value", count));
        HashSet<String> paths = scalar.StartsWith('"') ? [path] : [];
        Byte[]? reference = null;
        try
        {
            var buffered = new ArrayBufferWriter<Byte>(); new POICBORWriter(buffered).WriteChangeSet(document.RootElement, paths);
            var reader = new CBORReader(buffered.WrittenSpan.ToArray(), new CBORReaderOptions { MaxDepth = 64 - envelopeDepth });
            reader.SkipValue(); reference = buffered.WrittenSpan.ToArray();
        }
        catch (CBORException) { }
        var cache = new POIChangeSetScalarCache();
        using var leaf = JsonDocument.Parse(scalar);
        if (leaf.RootElement.ValueKind is JsonValueKind.Number or JsonValueKind.String)
            cache.Read(leaf.RootElement, "/warm", ["/warm"]);
        var output = new ArrayBufferWriter<Byte>();
        void Encode()
        {
            new POICBORPreflight(64 - envelopeDepth).ValidateChangeSet(document.RootElement, paths, cache);
            new POICBORWriter(output).WriteChangeSet(document.RootElement, paths, cache);
        }
        if (reference is null) { Assert.Throws<CBORException>(Encode); Assert.That(output.WrittenCount, Is.Zero); }
        else { Encode(); Assert.That(output.WrittenSpan.ToArray(), Is.EqualTo(reference)); }
    }

    [Test, Combinatorial]
    public void Header_native_tuples_match_reader_budgets_and_ignore_nested_customer_declarations(
        [Values(0, 1, 2, 3, 60, 64)] Int32 budget, [Values(false, true)] Boolean base64)
    {
        var encoding = base64 ? ETagDigestEncoding.Base64 : ETagDigestEncoding.HEX;
        var tags = new JArray(ETag.Compute(ETagFormat.JSON, [1]).ToJSON(encoding), ETag.Compute(ETagFormat.CBOR, [2]).ToJSON(encoding));
        using var doc = JsonDocument.Parse(new JObject(new JProperty("BeforeETags", tags),
            new JProperty("AfterETags", tags.DeepClone()), new JProperty("Metadata", JObject.Parse("{\"BeforeETags\":[],\"ETags\":[],\"maxPower\":\"invalid\"}"))).ToString());
        var expected = new ArrayBufferWriter<Byte>(); new POICBORWriter(expected).WriteChangeSet(doc.RootElement, []);
        Boolean allowed;
        try { var reader = new CBORReader(expected.WrittenSpan.ToArray(), new CBORReaderOptions { MaxDepth = budget }); reader.SkipValue(); allowed = true; }
        catch (CBORException) { allowed = false; }
        var actual = new ArrayBufferWriter<Byte>();
        void Encode() { new POICBORPreflight(budget).ValidateChangeSet(doc.RootElement, []); new POICBORWriter(actual).WriteChangeSet(doc.RootElement, []); }
        if (allowed) { Encode(); Assert.That(actual.WrittenSpan.ToArray(), Is.EqualTo(expected.WrittenSpan.ToArray())); }
        else { Assert.Throws<CBORException>(Encode); Assert.That(actual.WrittenCount, Is.Zero); }
    }

    [Test, Combinatorial]
    public void All_profiles_preserve_long_signed_batches_bytes_runtime_and_original_peers(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(1, 7, 16384)] Int32 segment)
    {
        using var fixture = new Fixture(profile); var history = fixture.History;
        var batch = Sign(Batch(history.Head.Snapshot, "archive-long", LongChanges(512))
            .WithDescription("de", "Ladepunkte aktualisieren").WithDescription("en", "Update charging points")
            .WithMetadata("numbers", Value("[1.0,1e0,-0,1e300,1e-300]")));
        Publish(history, Sign(history.PrepareCommit(history.Head.Id, batch))); Status(history, EvseTarget, "charging");
        var before = new Observation(history); var expected = ReferenceCBOR(history);
        using var output = new Destination(segment); history.WriteCBOR(output);
        Assert.That(output.Bytes, Is.EqualTo(expected)); Assert.That(history.ToCBOR(), Is.EqualTo(expected));
        Assert.That(output.FlushCalls, Is.Zero); Assert.That(output.DisposeCalls, Is.Zero); before.AssertUnchanged(history);
        using var restored = RoamingNetworkHistory.ParseCBOR(output.Bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Assert.That(restored.Head.Id, Is.EqualTo(history.Head.Id)); Assert.That(restored.Head.Snapshot.ETags, Is.EqualTo(batch.AfterETags));
        var recovered = restored.Head.Commit.ChangeSet!;
        Assert.That(recovered.ToCBOR(), Is.EqualTo(batch.ToCBOR()));
        foreach (var peer in recovered.Signatures) Assert.That(VerifyBatch(recovered, peer), Is.True);
        Assert.That(restored.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Failure_inside_a_batch_preserves_borrowed_stream_history_and_exact_retry(Int32 profile)
    {
        using var fixture = new Fixture(profile); var history = fixture.History;
        var batch = Sign(Batch(history.Head.Snapshot, "archive-failure", LongChanges(512)));
        Publish(history, Sign(history.PrepareCommit(history.Head.Id, batch)));
        var before = new Observation(history); var expected = ReferenceCBOR(history);
        var batchBytes = batch.ToCBOR(); var offset = expected.AsSpan().IndexOf(batchBytes);
        Assert.That(offset, Is.GreaterThanOrEqualTo(0)); var accepted = offset + batchBytes.Length / 2;
        using var failed = new Destination(7, failAfter: accepted);
        Assert.Throws<IOException>(() => history.WriteCBOR(failed));
        Assert.That(failed.Bytes, Is.EqualTo(expected[..accepted]));
        Assert.That(failed.FlushCalls, Is.Zero); Assert.That(failed.DisposeCalls, Is.Zero); before.AssertUnchanged(history);
        using var retry = new Destination(1); history.WriteCBOR(retry); Assert.That(retry.Bytes, Is.EqualTo(expected));
    }

    [Test, Combinatorial]
    public void Real_batch_preflight_preserves_the_previous_failed_archive_prefix(
        [Values(1, 3, 4)] Int32 envelopeDepth, [Values(57, 58, 59, 60)] Int32 arrays)
    {
        var batch = Sign(Unapplied().WithMetadata("deep", Value(new String('[', arrays) + "1e0" + new String(']', arrays))));
        var bytes = batch.ToCBOR(); using var previous = new Destination(); using var direct = new Destination();
        Exception? error = null;
        try { Envelope(previous, envelopeDepth, value => value.Encoded(bytes)); }
        catch (CBORException failure) { error = failure; }
        if (error is null) Envelope(direct, envelopeDepth, value => value.ChangeSet(batch));
        else Assert.Throws<CBORException>(() => Envelope(direct, envelopeDepth, value => value.ChangeSet(batch)));
        Assert.That(direct.Bytes, Is.EqualTo(previous.Bytes));
        Assert.That(direct.FlushCalls, Is.Zero); Assert.That(direct.DisposeCalls, Is.Zero);
        Assert.That(batch.ToCBOR(), Is.EqualTo(bytes));
    }

    [Test, Combinatorial]
    public void Real_tag_depth_failure_is_atomic_at_every_profile_and_allows_signed_retry(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean publish)
    {
        using var fixture = new Fixture(profile, persistent: true); var history = fixture.History;
        Publish(history, Prepare(history, history.Head.Id, "prefix", Rename(new String('n', 64000))));
        Status(history, EvseTarget, "charging");
        var before = new Observation(history); var disk = File.ReadAllBytes(fixture.ArchivePath);
        var batch = Sign(Batch(history.Head.Snapshot, "archive-depth", Power("150 kW"))
            .WithMetadata("deep", Value(new String('[', 60) + "1e0" + new String(']', 60))));
        Assert.That(() => batch.ToCBOR(), Throws.Nothing);
        var candidate = Sign(history.PrepareCommit(history.Head.Id, batch)); Assert.That(() => candidate.ToCBOR(), Throws.Nothing);
        var stages = new List<ArchiveWriteStage>(); history.ArchiveWriteObserver = stages.Add;
        var success = publish ? history.TryPublish(history.Head.Id, candidate, out var result) : history.TryStoreCommit(candidate, out result);
        Assert.That(success, Is.False); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.PersistenceFailure));
        Assert.That(result.Error, Does.Contain("depth"));
        Assert.That(stages, Is.EqualTo(new[] { ArchiveWriteStage.BeforeTemporaryWrite }));
        Assert.That(File.ReadAllBytes(fixture.ArchivePath), Is.EqualTo(disk)); before.AssertUnchanged(history);
        Assert.That(Directory.EnumerateFiles(fixture.Directory.DirectoryPath, "*.tmp-*"), Is.Empty); history.ArchiveWriteObserver = null;
        Publish(history, Prepare(history, history.Head.Id, "retry", Power("175 kW")));
        Assert.That(File.ReadAllBytes(fixture.ArchivePath), Is.EqualTo(ReferenceCBOR(history)));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
    }

    [TestCase("{\"x\":1,\"x\":2}")]
    [TestCase("{\"x\":1,\"\\u0078\":2}")]
    [TestCase("{\"nested\":{\"x\":1,\"x\":2}}")]
    public void Invalid_signed_content_emits_no_payload_and_valid_retry_succeeds(String json)
    {
        var batch = Unapplied(RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null, Value(json)));
        var output = new ArrayBufferWriter<Byte>(); Assert.Throws<ArgumentException>(() => batch.WriteArchiveCBOR(output, 60));
        Assert.That(output.WrittenCount, Is.Zero);
        var valid = Sign(Unapplied(Power("150 kW"))); valid.WriteArchiveCBOR(output, 60);
        Assert.That(output.WrittenSpan.ToArray(), Is.EqualTo(valid.ToCBOR()));
    }

    [Test]
    public void Exact_number_tokens_and_reading_strings_have_separate_cache_identities()
    {
        var cache = new POIChangeSetScalarCache();
        foreach (var json in new[] { "1", "1.0", "1e0", "-0", "0", "\"1\"", "\"150 kW\"", "\"4.50 m\"", "\"invalid\"" })
        {
            var value = Value(json); var expected = RoamingNetworkChangeSet.EncodeTransportScalar(value, "/reading", ["/reading"]);
            for (var index = 0; index < 2; index++)
                Assert.That(cache.Read(value, "/reading", ["/reading"]).ToByteArray(CBORWriterOptions.Canonical), Is.EqualTo(expected.ToByteArray(CBORWriterOptions.Canonical)));
        }
        Assert.That(cache.Entries, Is.EqualTo(9));
    }

    [TestCase("true")] [TestCase("\"150 kW\"")]
    public void Unsupported_or_customer_occurrences_cannot_use_the_scalar_cache(String json)
    {
        var cache = new POIChangeSetScalarCache(); cache.Read(Value("\"150 kW\""), "/reading", ["/reading"]);
        Assert.Throws<ArgumentException>(() => cache.Read(Value(json), "/customer", ["/reading"]));
        Assert.That(cache.Entries, Is.EqualTo(1));
    }

    [Test]
    public void Entry_saturation_preserves_exact_overflow_values_and_admitted_hits()
    {
        var cache = new POIChangeSetScalarCache();
        for (var index = 0; index < 70; index++)
        {
            var value = Value(index.ToString(System.Globalization.CultureInfo.InvariantCulture));
            Assert.That(cache.Read(value, "", []).ToByteArray(CBORWriterOptions.Canonical),
                        Is.EqualTo(RoamingNetworkChangeSet.EncodeTransportScalar(value, "", []).ToByteArray(CBORWriterOptions.Canonical)));
        }
        Assert.That(cache.Entries, Is.EqualTo(64)); var retained = cache.TextCharacters;
        cache.Read(Value("0"), "", []); cache.Read(Value("69"), "", []);
        Assert.That(cache.TextCharacters, Is.EqualTo(retained)); Assert.That(new POIChangeSetScalarCache().Entries, Is.Zero);
    }

    [TestCase(256)] [TestCase(257)]
    public void Text_and_total_text_admission_limits_preserve_large_integer_bytes(Int32 length)
    {
        var cache = new POIChangeSetScalarCache();
        for (var index = 1; index <= 34; index++)
        {
            var prefix = (index + 10).ToString(System.Globalization.CultureInfo.InvariantCulture);
            var value = Value(prefix + new String('1', length - prefix.Length));
            Assert.That(cache.Read(value, "", []).ToByteArray(CBORWriterOptions.Canonical),
                        Is.EqualTo(RoamingNetworkChangeSet.EncodeTransportScalar(value, "", []).ToByteArray(CBORWriterOptions.Canonical)));
        }
        Assert.That(cache.Entries, Is.EqualTo(length == 256 ? 32 : 0));
        Assert.That(cache.TextCharacters, Is.EqualTo(length == 256 ? 8192 : 0));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(4)]
    public void Long_direct_payload_preserves_every_equal_peer_and_signing_preimage(Int32 peers)
    {
        var batch = Unapplied(LongChanges(2048)).WithMetadata("numbers", Value("[1.0,1e0,-0,1e65537,1e-65537]"));
        for (var index = 0; index < peers; index++) batch = batch.Sign(Alice, "fixture-" + index, COSEAlgorithm.Ed25519);
        var actual = new ArrayBufferWriter<Byte>(); batch.WriteArchiveCBOR(actual, 61);
        Assert.That(actual.WrittenSpan.ToArray(), Is.EqualTo(batch.ToCBOR()));
        var restored = RoamingNetworkChangeSet.ParseCBOR(actual.WrittenSpan);
        Assert.That(restored.Signatures, Is.EqualTo(batch.Signatures));
        foreach (var peer in restored.Signatures)
        {
            Assert.That(restored.VerifySignature(peer, Alice.GeneratePublicKey(), peer.KeyId, out _), Is.True);
            Assert.That(restored.GetSigningBytes(COSEAlgorithm.Ed25519, peer.KeyId), Is.EqualTo(batch.GetSigningBytes(COSEAlgorithm.Ed25519, peer.KeyId)));
        }
    }

    [TestCase("Add")] [TestCase("Remove")] [TestCase("UpdateProperty")] [TestCase("RemoveProperty")]
    [TestCase("AddElement")] [TestCase("RemoveElement")] [TestCase("ReplaceElement")]
    [TestCase("UpdateElementProperty")] [TestCase("RemoveElementProperty")]
    public void All_operation_kinds_keep_their_scoped_reading_paths(String kind)
    {
        ImmutableArray<POIElementPathSegment> cable = [new("cable")]; var value = Value("{\"length\":\"4.50 m\"}");
        var entity = Value("{\"@id\":\"DE*ABC*E2\",\"maxPower\":\"150 kW\"}");
        var change = kind switch {
            "Add" => RoamingNetworkChange.Add("EVSE", "DE*ABC*E2", entity, "ChargingStation", "DE*ABC*S1"),
            "Remove" => RoamingNetworkChange.Remove("EVSE", "DE*ABC*E2", entity, "ChargingStation", "DE*ABC*S1"),
            "UpdateProperty" => Power("150 kW"),
            "RemoveProperty" => RoamingNetworkChange.RemoveProperty("EVSE", "DE*ABC*E1", "maxPower", Value("\"150 kW\"")),
            "AddElement" => RoamingNetworkChange.AddElement("ChargingConnector", "1", cable, value, "EVSE", "DE*ABC*E1"),
            "RemoveElement" => RoamingNetworkChange.RemoveElement("ChargingConnector", "1", cable, value, "EVSE", "DE*ABC*E1"),
            "ReplaceElement" => RoamingNetworkChange.ReplaceElement("ChargingConnector", "1", cable, value, value, "EVSE", "DE*ABC*E1"),
            "UpdateElementProperty" => RoamingNetworkChange.UpdateElementProperty("ChargingConnector", "1", cable, "length", Value("\"4.50 m\""), Value("\"5 m\""), "EVSE", "DE*ABC*E1"),
            _ => RoamingNetworkChange.RemoveElementProperty("ChargingConnector", "1", cable, "length", Value("\"4.50 m\""), "EVSE", "DE*ABC*E1")
        };
        var batch = Sign(Unapplied(change)); var output = new ArrayBufferWriter<Byte>(); batch.WriteArchiveCBOR(output, 61);
        Assert.That(output.WrittenSpan.ToArray(), Is.EqualTo(batch.ToCBOR()));
    }

    private static RoamingNetworkChange[] LongChanges(Int32 count)
        => Enumerable.Range(0, count).Select(index => index % 2 == 0 ? Power("150 kW") :
            RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null,
                Value("{\"decimal\":1.10,\"exponent\":1e0,\"negativeZero\":-0,\"ETags\":[[\"customer\"]],\"reading\":\"150 kW\",\"a/~\":[null,true,123456789012345678901234567890]}"))).ToArray();

    private static void Envelope(Destination output, Int32 depth, Action<POIArchiveCBORWriter> payload)
    {
        using var writer = new POIArchiveCBORWriter(output);
        void Nest(POIArchiveCBORWriter value, Int32 remaining)
        {
            if (remaining == 0) payload(value);
            else value.Map(("a", item => item.Text(new String('p', 64000))), ("payload", item => Nest(item, remaining - 1)));
        }
        Nest(writer, depth); writer.Complete();
    }

    private static RoamingNetworkChangeSet Unapplied(params RoamingNetworkChange[] changes)
    {
        var source = Network().DataSnapshot;
        return new("archive-transport", source.Root.Id, source.Revision, InteropFixture.Time, [.. changes], source.ETags, source.ETags);
    }
}
