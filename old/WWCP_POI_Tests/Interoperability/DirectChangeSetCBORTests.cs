using System.Buffers;
using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class DirectChangeSetCBORTests
{
    [TestCase("0")] [TestCase("-0")] [TestCase("1.0")] [TestCase("1.10")]
    [TestCase("1e0")] [TestCase("1E+0")] [TestCase("1e-300")] [TestCase("1e300")]
    [TestCase("9223372036854775807")] [TestCase("18446744073709551615")]
    [TestCase("18446744073709551616")] [TestCase("-9223372036854775809")]
    [TestCase("123456789012345678901234567890.12345")]
    [TestCase("-0.00")] [TestCase("0.0000000000000000000000000001")]
    public void Number_spelling_signing_bytes_and_two_peers_survive(String number)
    {
        var source = Network().DataSnapshot;
        var change = RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null, Value("{\"number\":" + number + "}"));
        var batch = Sign(Unapplied(change).WithMetadata("number", Value(number)));
        var recovered = Check(batch);
        Assert.That(recovered.Changes[0].NewValue!.Value.GetProperty("number").GetRawText(), Is.EqualTo(number));
        Assert.That(recovered.Metadata["number"].GetRawText(), Is.EqualTo(number));
    }

    [TestCase("150 kW")] [TestCase("150.00 kW")] [TestCase("0.150 MW")]
    [TestCase(" 150 kW ")] [TestCase("150kW")] [TestCase("1e3 W")] [TestCase("not-a-reading")]
    [TestCase("(230.00 ±0.12) V, k=2, p=0.95, dist=normal")]
    public void Native_reading_requires_exact_recoverable_signed_spelling(String text)
    {
        var batch = Unapplied(RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Text(text), Text(text)));
        var recovered = Check(Sign(batch));
        Assert.That(recovered.Changes[0].OldValue!.Value.GetString(), Is.EqualTo(text));
        Assert.That(recovered.Changes[0].NewValue!.Value.GetString(), Is.EqualTo(text));
        var change = Field(CBORValue.Parse(batch.ToCBOR()), "Changes").AsArray()[0];
        var expected = MetrologicalValue.TryParse(text, out var reading, out _) && reading.ToString() == text;
        Assert.That(Field(change, "NewValue").Kind == CBORValueKind.Tagged, Is.EqualTo(expected));
    }

    [TestCase("Add")] [TestCase("Remove")] [TestCase("UpdateProperty")] [TestCase("RemoveProperty")]
    [TestCase("AddElement")] [TestCase("RemoveElement")] [TestCase("ReplaceElement")]
    [TestCase("UpdateElementProperty")] [TestCase("RemoveElementProperty")]
    public void All_operation_shapes_preserve_payload_presence_paths_and_native_readings(String kind)
    {
        var entity = Value("{\"@id\":\"DE*ABC*E2\",\"maxPower\":\"150 kW\",\"geoLocation\":{\"alt\":\"150 m\"},\"socketOutlets\":[{\"@id\":\"1\",\"cable\":{\"length\":\"4.50 m\"}}],\"customData\":{\"reading\":\"150 kW\",\"ETags\":[1,2]}}");
        ImmutableArray<POIElementPathSegment> cable = [new("cable")];
        var value = Value("{\"length\":\"4.50 m\",\"resistance\":\"0.02 Ω\"}");
        var operation = kind switch {
            "Add" => RoamingNetworkChange.Add("EVSE", "DE*ABC*E2", entity, "ChargingStation", "DE*ABC*S1"),
            "Remove" => RoamingNetworkChange.Remove("EVSE", "DE*ABC*E2", entity, "ChargingStation", "DE*ABC*S1"),
            "UpdateProperty" => RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Text("100 kW"), Text("150 kW")),
            "RemoveProperty" => RoamingNetworkChange.RemoveProperty("EVSE", "DE*ABC*E1", "maxPower", Text("100 kW")),
            "AddElement" => RoamingNetworkChange.AddElement("ChargingConnector", "1", cable, value, "EVSE", "DE*ABC*E1"),
            "RemoveElement" => RoamingNetworkChange.RemoveElement("ChargingConnector", "1", cable, value, "EVSE", "DE*ABC*E1"),
            "ReplaceElement" => RoamingNetworkChange.ReplaceElement("ChargingConnector", "1", cable, value, value, "EVSE", "DE*ABC*E1"),
            "UpdateElementProperty" => RoamingNetworkChange.UpdateElementProperty("ChargingConnector", "1", cable, "length", Text("4.50 m"), Text("5 m"), "EVSE", "DE*ABC*E1"),
            _ => RoamingNetworkChange.RemoveElementProperty("ChargingConnector", "1", cable, "length", Text("4.50 m"), "EVSE", "DE*ABC*E1")
        };
        var recovered = Check(Sign(Unapplied(operation)));
        var actual = recovered.Changes[0];
        Assert.That(actual.ElementPath, Is.EqualTo(operation.ElementPath));
        Assert.That(actual.ParentEntityType, Is.EqualTo(operation.ParentEntityType));
        Assert.That(actual.ParentEntityId, Is.EqualTo(operation.ParentEntityId));
        Assert.That(actual.OldValue?.GetRawText(), Is.EqualTo(operation.OldValue is { } old ? ReorderedJSON(old) : null));
        Assert.That(actual.NewValue?.GetRawText(), Is.EqualTo(operation.NewValue is { } next ? ReorderedJSON(next) : null));
    }

    [Test]
    public void Customer_ETags_and_readings_are_ordinary_values_but_header_digests_are_binary()
    {
        var payload = Value("{\"ETags\":[[\"json\",\"sha256\",\"hex\",\"customer\"]],\"BeforeETags\":[1,2],\"maxPower\":\"150 kW\",\"a/~\":[1e0,-0],\"nested\":[[{\"geoLocation\":{\"alt\":\"unknown\"}}]]}");
        var batch = Sign(Unapplied(RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", Value("null"), payload))
            .WithDescription("de", "250 kW").WithMetadata("BeforeETags", payload));
        var recovered = Check(batch);
        Assert.That(recovered.Changes[0].OldValue!.Value.ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(recovered.Changes[0].NewValue!.Value.GetProperty("maxPower").GetString(), Is.EqualTo("150 kW"));
        var cbor = CBORValue.Parse(batch.ToCBOR());
        foreach (var name in new[] { "BeforeETags", "AfterETags" })
            foreach (var tag in Field(cbor, name).AsArray())
                Assert.That(tag.AsArray()[2].Kind, Is.EqualTo(CBORValueKind.ByteString));
    }

    [TestCase(0)] [TestCase(23)] [TestCase(24)] [TestCase(255)] [TestCase(256)]
    public void Nested_arrays_and_wide_maps_preserve_counts_and_key_order(Int32 count)
    {
        var payload = "{\"array\":[" + String.Join(",", Enumerable.Repeat("1e0", count)) + "],\"map\":{" +
            String.Join(",", Enumerable.Range(0, count).Reverse().Select(i => JsonSerializer.Serialize("é/~" + i) + ":1.10")) + "}}";
        Check(Sign(Unapplied(RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null, Value(payload)))));
    }

    [TestCase(57)] [TestCase(58)] [TestCase(59)] [TestCase(60)] [TestCase(61)]
    public void Container_decimal_and_embedded_tag_depth_limits_match_previous_codec(Int32 depth)
    {
        foreach (var scalar in new[] { "0", "1.10", "1e0" })
        {
            var batch = Unapplied(RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null,
                Value(new String('[', depth) + scalar + new String(']', depth))));
            Byte[]? expected = null; try { expected = Reference(batch); } catch (Exception) { }
            if (expected is null) Assert.Catch<Exception>(() => batch.ToCBOR());
            else Assert.That(batch.ToCBOR(), Is.EqualTo(expected));
        }
    }

    [Test]
    public void Duplicate_signed_payload_is_rejected_before_writer_output_and_valid_retry_succeeds()
    {
        var batch = Unapplied(RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null, Value("{\"x\":1,\"x\":2}")));
        Assert.Throws<ArgumentException>(() => batch.ToCBOR());
        Assert.Throws<ArgumentException>(() => Reference(batch));
        Check(Sign(Unapplied(Power("150 kW"))));
    }

    [TestCase("{}")] [TestCase("[]")]
    public void Scalar_codec_rejects_complete_containers(String json)
        => Assert.Throws<ArgumentException>(() => RoamingNetworkChangeSet.EncodeTransportScalar(Value(json), "", []));

    [Test]
    public void Signed_ordered_batch_produces_the_expected_immutable_result()
    {
        var source = Network().DataSnapshot;
        var batch = Sign(Batch(source, "direct-apply", Power("150 kW"), Power("200 kW"))
            .WithDescription("de", "Mehr Leistung").WithDescription("en", "More power")
            .WithMetadata("numbers", Value("[1.0,1e0,-0,1e300,1e-300]")));
        var restored = Check(batch);
        Assert.That(source.ApplyChangeSet(restored, VerifyBatch).ETags, Is.EqualTo(batch.AfterETags));
        Assert.That(source.ApplyChangeSet(restored, VerifyBatch).ETags, Is.Not.EqualTo(source.ETags));
    }

    [TestCase(1, false)] [TestCase(1, true)] [TestCase(4, false)] [TestCase(4, true)]
    public void Real_ChangeSet_tag_depth_failure_preserves_disk_head_runtime_and_allows_retry(Int32 profile, Boolean publish)
    {
        using var fixture = new ArchiveStreamingTestSupport.Fixture(profile, persistent: true);
        var history = fixture.History;
        var prefix = ReplicationTestSupport.Prepare(history, history.Head.Id, "direct-prefix",
            ReplicationTestSupport.Rename(new String('n', 64000)));
        ReplicationTestSupport.Publish(history, prefix);
        var before = new ReplicationTestSupport.Observation(history);
        var disk = File.ReadAllBytes(fixture.ArchivePath);
        var batch = Sign(Batch(history.Head.Snapshot, "direct-depth", Power("150 kW"))
            .WithMetadata("deep", Value(new String('[', 60) + "1e0" + new String(']', 60))));
        Assert.That(batch.ToCBOR(), Is.EqualTo(Reference(batch)));
        var candidate = history.PrepareCommit(history.Head.Id, batch);
        Assert.That(() => candidate.ToCBOR(), Throws.Nothing);
        var success = publish ? history.TryPublish(history.Head.Id, candidate, out var result) : history.TryStoreCommit(candidate, out result);
        Assert.That(success, Is.False); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.PersistenceFailure));
        Assert.That(result.Error, Does.Contain("depth"));
        Assert.That(File.ReadAllBytes(fixture.ArchivePath), Is.EqualTo(disk)); before.AssertUnchanged(history);
        Assert.That(Directory.EnumerateFiles(fixture.Directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        var retry = ReplicationTestSupport.Prepare(history, history.Head.Id, "direct-retry", Power("175 kW"));
        ReplicationTestSupport.Publish(history, retry);
        Assert.That(File.ReadAllBytes(fixture.ArchivePath), Is.EqualTo(ArchiveStreamingTestSupport.ReferenceCBOR(history)));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(4)]
    public void Equal_peer_array_order_and_empty_operations_match_previous_codec(Int32 peers)
    {
        var batch = Unapplied().WithMetadata("wide", Value("{\"z\":-0,\"ä\":1e0,\"aaa\":1.0}"));
        for (var i = 0; i < peers; i++) batch = batch.Sign(Alice, "fixture-alice-" + i, COSEAlgorithm.Ed25519);
        Assert.That(batch.Signatures.Length, Is.EqualTo(peers));
        Check(batch);
    }

    private static JsonElement Text(String text) => JsonSerializer.SerializeToElement(text);
    private static CBORValue Field(CBORValue value, String name) => value.AsMap().Single(entry => entry.Key.AsText() == name).Value;
    private static RoamingNetworkChangeSet Unapplied(params RoamingNetworkChange[] changes)
    {
        var source = Network().DataSnapshot;
        return new("direct-transport", source.Root.Id, source.Revision, InteropFixture.Time, [.. changes], source.ETags, source.ETags);
    }

    // Compare parsed JSON semantically with exact numeric tokens, irrespective of map wire order.
    private static String ReorderedJSON(JsonElement value)
    {
        var tree = ScalarTree(value, "", []);
        return Decode(CBORValue.Parse(tree.ToByteArray(CBORWriterOptions.Canonical))).GetRawText();
    }

    private static RoamingNetworkChangeSet Check(RoamingNetworkChangeSet batch)
    {
        var bytes = batch.ToCBOR(); Assert.That(bytes, Is.EqualTo(Reference(batch)));
        var recovered = RoamingNetworkChangeSet.ParseCBOR(bytes);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(bytes)); Assert.That(recovered.Signatures, Is.EqualTo(batch.Signatures));
        foreach (var signature in batch.Signatures)
        {
            Assert.That(recovered.GetSigningBytes(COSEAlgorithm.Ed25519, signature.KeyId), Is.EqualTo(batch.GetSigningBytes(COSEAlgorithm.Ed25519, signature.KeyId)));
            Assert.That(recovered.VerifySignature(signature, signature.KeyId == "fixture-bob" ? Bob.GeneratePublicKey() : Alice.GeneratePublicKey(),
                        signature.KeyId, out _), Is.True);
        }
        return recovered;
    }

    private static readonly Func<JsonElement, String, HashSet<String>, CBORValue> ScalarTree = Bind<Func<JsonElement, String, HashSet<String>, CBORValue>>("EncodeTransportJSON");
    private static readonly Func<CBORValue, Boolean, CBORValue> ConvertTags = Bind<Func<CBORValue, Boolean, CBORValue>>("ConvertTransportETags");
    private static readonly Action<JsonElement> Validate = Bind<Action<JsonElement>>("ValidateSigningJSON");
    private static readonly Func<CBORValue, JsonElement> Decode = Bind<Func<CBORValue, JsonElement>>("DecodeApplicationJSON");
    private static T Bind<T>(String method) where T : Delegate
        => typeof(RoamingNetworkChangeSet).GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<T>();
    private static Byte[] Reference(RoamingNetworkChangeSet batch)
    {
        using var document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(batch)); Validate(document.RootElement);
        var paths = typeof(RoamingNetworkChangeSet).GetMethod("MeasurementPaths", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Func<HashSet<String>>>(batch)();
        return ConvertTags(ScalarTree(document.RootElement, "", paths), true).ToByteArray(CBORWriterOptions.Canonical);
    }
}
