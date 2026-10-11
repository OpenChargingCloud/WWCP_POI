/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class InteroperabilityTests
{
    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("fr-FR")]
    public void Published_vectors_match_exact_bytes_digests_identities_and_signatures(String culture)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var expected = JsonSerializer.Deserialize<SortedDictionary<String, String>>(File.ReadAllText(FilePath("vectors.json")))!;
            var actual = Vectors();
            Assert.That(actual.Keys, Is.EquivalentTo(expected.Keys));
            foreach (var field in expected) Assert.That(actual[field.Key], Is.EqualTo(field.Value), field.Key);
            var snapshot = Network().DataSnapshot;
            Assert.That(Hex(SHA256.HashData(snapshot.ToCanonicalJSON())), Is.EqualTo(Hex(snapshot.ETags[0].Digest.ToArray())));
            Assert.That(Hex(SHA256.HashData(snapshot.ToCanonicalCBOR())), Is.EqualTo(Hex(snapshot.ETags[1].Digest.ToArray())));
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Test, Explicit("Regenerate reviewed reference files deliberately; ordinary tests never rewrite expectations.")]
    public void GenerateReferenceVectors()
    {
        var directory = Path.GetFullPath("../../../../docs/interoperability", TestContext.CurrentContext.TestDirectory);
        Assert.That(File.Exists(Path.Combine(directory, "snapshot.input.json")), Is.True);
        var vectors = Vectors();
        File.WriteAllText(Path.Combine(directory, "vectors.json"), JsonSerializer.Serialize(vectors, new JsonSerializerOptions {
            WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }) + "\n");
        foreach (var (file, field) in ArtifactFields) File.WriteAllBytes(Path.Combine(directory, file), ArtifactBytes(field, vectors[field]));
    }

    [Test]
    public void Published_artifacts_match_the_frozen_manifest_and_recover_signed_history()
    {
        var expected = JsonSerializer.Deserialize<Dictionary<String, String>>(File.ReadAllText(FilePath("vectors.json")))!;
        foreach (var (file, field) in ArtifactFields)
            Assert.That(File.ReadAllBytes(FilePath(file)), Is.EqualTo(ArtifactBytes(field, expected[field])), file);
        using var json = RoamingNetworkHistory.Parse(expected["historyJSON"], VerifyBatch, VerifyCommit);
        using var cbor = RoamingNetworkHistory.ParseCBOR(Convert.FromHexString(expected["historyCBORHex"]), VerifyBatch, VerifyCommit);
        Assert.That(json.Head.Id.ToString(), Is.EqualTo(expected["mergeId"]));
        Assert.That(cbor.Head.Id, Is.EqualTo(json.Head.Id));
        Assert.That(cbor.Commits, Has.Length.EqualTo(6));
        Assert.That(cbor.Commits.All(commit => commit.Signatures.Length == 2), Is.True);
    }

    [Test]
    public void Published_signed_documents_are_verified_after_each_transport()
    {
        var expected = JsonSerializer.Deserialize<Dictionary<String, String>>(File.ReadAllText(FilePath("vectors.json")))!;
        foreach (var batch in new[] {JsonSerializer.Deserialize<RoamingNetworkChangeSet>(expected["changeSetJSON"])!, RoamingNetworkChangeSet.ParseCBOR(Convert.FromHexString(expected["changeSetCBORHex"]))})
        {
            Assert.That(batch.Signatures, Has.Length.EqualTo(2));
            Assert.That(batch.VerifySignatures(signature => PublicKey(signature.KeyId), out var error), Is.True, error);
            Assert.That(Network().DataSnapshot.ApplyChangeSet(batch, VerifyBatch).ETags, Is.EqualTo(batch.AfterETags));
        }
        foreach (var commit in new[] {RoamingNetworkCommit.Parse(expected["commitJSON"]), RoamingNetworkCommit.ParseCBOR(Convert.FromHexString(expected["commitCBORHex"]))})
        {
            Assert.That(commit.VerifySignatures(signature => PublicKey(signature.KeyId), out var error), Is.True, error);
            using var history = History();
            Assert.That(history.TryPublish(history.Head.Id, commit, out var result), Is.True, result.Error);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Versioned_snapshot_roundtrip_retains_presence_and_can_continue(Boolean cbor)
    {
        var source = Network().DataSnapshot;
        var next = source.ApplyChangeSet(Batch(source, "initial", Power("150 kW")));
        var restored = cbor ? RoamingNetworkDataSnapshot.ParseCBOR(next.ToCBOR(IncludeVersionMetadata: true)) : RoamingNetworkDataSnapshot.Parse(next.ToJSON());
        Assert.That(restored.Revision, Is.EqualTo(next.Revision));
        Assert.That(restored.AppliedChangeSetId, Is.EqualTo(next.AppliedChangeSetId));
        Assert.That(restored.ETags, Is.EqualTo(next.ETags));
        Assert.That(restored.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["physicalReference"].ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(restored.ApplyChangeSet(Batch(restored, "continued", Power("175 kW"))).Revision, Is.EqualTo(2));
    }

    [Test]
    public void Equivalent_units_offsets_and_property_order_share_state_identifiers()
    {
        var source = Network();
        var json = source.DataSnapshot.ToJSON();
        var evse = (JObject)json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!;
        evse["maxPower"] = "0.1 MW";
        json["lastChange"] = "2026-01-02T11:00:00+01:00";
        var reordered = new JObject(json.Properties().Reverse().Select(property => new JProperty(property.Name, property.Value.DeepClone())));
        Assert.That(RoamingNetworkDataSnapshot.Parse(reordered).ETags, Is.EqualTo(source.ETags));
        Assert.That((String)source.DataSnapshot.ToJSON()["customData"]!["reading"]!, Is.EqualTo("250 kW"));
    }

    [Test]
    public void Schema_profile_is_validated_and_customer_profile_remains_content()
    {
        var source = Network().DataSnapshot;
        var json = source.ToJSON();
        Assert.That((String)json[POIContentProfile.PropertyName]!, Is.EqualTo(POIContentProfile.Id));
        Assert.That(source.Entities[source.Root].Properties.ContainsKey(POIContentProfile.PropertyName), Is.False);
        json[POIContentProfile.PropertyName] = "wwcp-poi-static-v999";
        Assert.That(() => RoamingNetworkDataSnapshot.Parse(json), Throws.ArgumentException);
        json = source.ToJSON(); json.Remove(POIContentProfile.PropertyName);
        Assert.That(() => RoamingNetworkDataSnapshot.Parse(json), Throws.ArgumentException);
        json = source.ToJSON(); json["customData"]!["contentProfile"] = "different customer content";
        Assert.That(() => RoamingNetworkDataSnapshot.Parse(json), Throws.ArgumentException);
    }

    [Test]
    public void Commit_profile_is_part_of_identity_and_is_rejected_when_unsupported()
    {
        using var history = History();
        using var identity = JsonDocument.Parse(history.Head.Commit.GetIdentityBytes());
        Assert.That(identity.RootElement.GetProperty("ContentProfile").GetString(), Is.EqualTo(POIContentProfile.Id));
        var commit = JObject.Parse(history.Head.Commit.ToJSON()); commit["ContentProfile"] = "other";
        Assert.That(() => RoamingNetworkCommit.Parse(commit.ToString()), Throws.ArgumentException);
        var archive = JObject.Parse(history.ToJSON()); archive["ContentProfile"] = "other";
        Assert.That(() => RoamingNetworkHistory.Parse(archive.ToString()), Throws.ArgumentException);
    }

    [Test]
    public void State_and_commit_digest_tuples_keep_their_formats_in_CBOR()
    {
        var source = Network();
        var map = CBORValue.Parse(source.ToCBOR()).AsMap().ToDictionary(entry => entry.Key.AsText(), entry => entry.Value);
        var tags = map["ETags"].AsArray();
        Assert.That(tags[0].AsArray()[0].AsText(), Is.EqualTo("json"));
        Assert.That(tags[1].AsArray()[0].AsText(), Is.EqualTo("cbor"));
        foreach (var tag in tags) Assert.That(tag.AsArray()[2].Kind, Is.EqualTo(CBORValueKind.ByteString));
        using var history = History();
        var commit = CBORValue.Parse(history.Head.Commit.ToCBOR()).AsMap().ToDictionary(entry => entry.Key.AsText(), entry => entry.Value);
        Assert.That(commit["Id"].AsArray()[0].AsText(), Is.EqualTo("json"));
        Assert.That(ETag.Parse(source.ETags[0].ToJSON(ETagDigestEncoding.Base64)), Is.EqualTo(source.ETags[0]));
    }

    [TestCase("1.0")]
    [TestCase("1e0")]
    [TestCase("-0")]
    [TestCase("1234567890123456789012345678901234567890")]
    public void Signed_number_spelling_and_customer_SI_text_survive_CBOR(String number)
    {
        var source = Network().DataSnapshot;
        var batch = Sign(Batch(source, "number").WithMetadata(ImmutableDictionary<String, JsonElement>.Empty
            .Add("value", Value(number)).Add("customer", Value("{\"ETags\":[\"json\",\"sha256\",\"hex\",\"raw\"],\"reading\":\"100000 W\"}"))));
        var restored = RoamingNetworkChangeSet.ParseCBOR(batch.ToCBOR());
        Assert.That(restored.Metadata["value"].GetRawText(), Is.EqualTo(number));
        Assert.That(restored.Metadata["customer"].GetRawText(), Is.EqualTo(batch.Metadata["customer"].GetRawText()));
        Assert.That(restored.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"), Is.EqualTo(batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice")));
        Assert.That(restored.VerifySignatures(signature => PublicKey(signature.KeyId), out var error), Is.True, error);
    }

    [Test]
    public void Optional_operation_presence_and_metrological_spelling_survive_CBOR()
    {
        var source = Network().DataSnapshot;
        var batch = Sign(Batch(source, "optional", Power("150000 W"),
            RoamingNetworkChange.RemoveProperty("EVSE", "DE*ABC*E1", "physicalReference", Value("null"))));
        var restored = RoamingNetworkChangeSet.ParseCBOR(batch.ToCBOR());
        Assert.That(restored.Changes[0].NewValue!.Value.GetString(), Is.EqualTo("150000 W"));
        Assert.That(restored.Changes[1].NewValue.HasValue, Is.False);
        Assert.That(restored.Changes[1].OldValue!.Value.ValueKind, Is.EqualTo(JsonValueKind.Null));
        Assert.That(source.ApplyChangeSet(restored, VerifyBatch).GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties.ContainsKey("physicalReference"), Is.False);
    }

    [TestCase("BeforeETags", 0)]
    [TestCase("BeforeETags", 1)]
    [TestCase("AfterETags", 0)]
    [TestCase("AfterETags", 1)]
    public void Either_wrong_state_digest_rejects_the_entire_batch(String field, Int32 index)
    {
        var source = Network().DataSnapshot;
        var batch = Batch(source, "wrong", Power("150 kW"));
        var tags = (field == "BeforeETags" ? batch.BeforeETags : batch.AfterETags).SetItem(index, ETag.Compute((ETagFormat)index, [1,2,3]));
        var changed = new RoamingNetworkChangeSet(batch.Id, batch.RoamingNetworkId, batch.BaseRevision, batch.CreatedAt, batch.Changes,
            field == "BeforeETags" ? tags : batch.BeforeETags, field == "AfterETags" ? tags : batch.AfterETags);
        var before = source.ToCanonicalJSON();
        Assert.That(() => source.ApplyChangeSet(changed), Throws.TypeOf<RoamingNetworkChangeSetException>());
        Assert.That(source.ToCanonicalJSON(), Is.EqualTo(before));
        Assert.That(source.Revision, Is.Zero);
    }

    [Test]
    public void Additional_peers_preserve_identity_and_existing_signatures()
    {
        using var history = History();
        var batch = Batch(history.Head.Snapshot, "peers", Power("150 kW")).Sign(Alice, "fixture-alice", COSEAlgorithm.Ed25519);
        var commit = history.PrepareCommit(history.Head.Id, batch).Sign(Alice, "fixture-alice", COSEAlgorithm.Ed25519);
        var extra = commit.WithChangeSet(batch.Sign(Bob, "fixture-bob", COSEAlgorithm.Ed25519)).Sign(Bob, "fixture-bob", COSEAlgorithm.Ed25519);
        Assert.That(extra.Id, Is.EqualTo(commit.Id));
        Assert.That(extra.VerifySignature(commit.Signatures[0], Alice.GeneratePublicKey(), "fixture-alice", out var error), Is.True, error);
        Assert.That(history.TryPublish(history.Head.Id, commit, out var result), Is.True, result.Error);
        Assert.That(history.TryPublish(default, extra, out result), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.AlreadyPublished));
        Assert.That(history.Head.Commit.Signatures, Has.Length.EqualTo(2));
        Assert.That(history.Head.Commit.ChangeSet!.Signatures, Has.Length.EqualTo(2));
        Assert.That(history.Head.Snapshot.Revision, Is.EqualTo(1));
    }

    [TestCase("Id")]
    [TestCase("Description")]
    [TestCase("Metadata")]
    public void Altered_signed_headers_and_metadata_fail_verification(String field)
    {
        var batch = Sign(Batch(Network().DataSnapshot, "signed", Power("150 kW")));
        var json = JObject.Parse(JsonSerializer.Serialize(batch));
        json[field] = field == "Id" ? new JValue("changed-id") : new JObject(new JProperty("changed", "content"));
        var changed = JsonSerializer.Deserialize<RoamingNetworkChangeSet>(json.ToString())!;
        Assert.That(changed.VerifySignatures(signature => PublicKey(signature.KeyId), out var error), Is.False);
        Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void Changing_ancestry_invalidates_commit_peers_without_changing_batch_peers()
    {
        using var history = History(); var root = history.Head.Commit;
        var batch = Sign(Batch(history.Head.Snapshot, "ancestry", Power("150 kW")));
        var original = Sign(RoamingNetworkCommit.Create(root, batch));
        var other = RoamingNetworkCommit.Create(root, Batch(history.Head.Snapshot, "other", Power("200 kW")));
        var changed = RoamingNetworkCommit.Create(root, batch, [other.Id]).WithSignatures(original.Signatures);
        Assert.That(changed.Id, Is.Not.EqualTo(original.Id));
        Assert.That(changed.ChangeSet!.VerifySignatures(signature => PublicKey(signature.KeyId), out _), Is.True);
        Assert.That(changed.VerifySignatures(signature => PublicKey(signature.KeyId), out _), Is.False);
    }

    [TestCase("a2617801617802")]
    [TestCase("a10100")]
    [TestCase("f7")]
    [TestCase("d9010040")]
    public void Malformed_or_unsupported_CBOR_returns_no_partial_value(String hex)
    {
        var bytes = Convert.FromHexString(hex);
        Assert.That(RoamingNetworkDataSnapshot.TryParseCBOR(bytes, out var snapshot, out var error), Is.False);
        Assert.That(snapshot, Is.Null); Assert.That(error, Is.Not.Empty);
        Assert.That(RoamingNetworkChangeSet.TryParseCBOR(bytes, out var batch, out error), Is.False);
        Assert.That(batch, Is.Null); Assert.That(error, Is.Not.Empty);
    }

    [Test]
    public void Trailing_CBOR_data_is_rejected_for_every_complete_transport()
    {
        using var history = History();
        var snapshot = history.Head.Snapshot;
        var batch = Batch(snapshot, "trailing", Power("150 kW"));
        var commit = history.PrepareCommit(history.Head.Id, batch);
        Assert.That(() => RoamingNetworkDataSnapshot.ParseCBOR([.. snapshot.ToCBOR(), 0]), Throws.Exception);
        Assert.That(() => RoamingNetworkChangeSet.ParseCBOR([.. batch.ToCBOR(), 0]), Throws.Exception);
        Assert.That(() => RoamingNetworkCommit.ParseCBOR([.. commit.ToCBOR(), 0]), Throws.Exception);
        Assert.That(() => RoamingNetworkHistory.ParseCBOR([.. history.ToCBOR(), 0]), Throws.Exception);
    }

    [TestCase("150000")]
    [TestCase("\"150000\"")]
    [TestCase("\"150 Hz\"")]
    public void Quantity_import_requires_a_unit_and_the_correct_dimension(String quantity)
    {
        var json = Network().DataSnapshot.ToJSON();
        json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!["EVSEs"]![0]!["maxPower"] = JToken.Parse(quantity);
        Assert.That(() => RoamingNetworkDataSnapshot.Parse(json), Throws.ArgumentException);
    }

    [Test]
    public void JSON_import_rejects_duplicate_keys_and_trailing_values()
    {
        Assert.That(() => RoamingNetworkDataSnapshot.Parse("{\"@id\":\"a\",\"@id\":\"b\"}"), Throws.Exception);
        Assert.That(() => RoamingNetworkDataSnapshot.Parse("{\"@id\":\"a\"} {}"), Throws.Exception);
    }

    [Test]
    public void Runtime_and_version_transport_options_do_not_change_static_content()
    {
        using var history = History();
        var root = history.Head.Id; var source = history.Head.Network; var tags = source.ETags;
        var staticBytes = source.ToCanonicalJSON();
        history.ApplyRuntimeUpdate(new("interop-network", POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
            POIRuntimeStatusKind.Status, new("charging", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        Assert.That(source.ToCanonicalJSON(), Is.EqualTo(staticBytes));
        Assert.That(history.Head.Id, Is.EqualTo(root)); Assert.That(source.ETags, Is.EqualTo(tags));
        Assert.That(source.ToJSONWithETags(IncludeRuntime: true)["revision"], Is.Null);
        Assert.That(source.ToJSONWithETags(IncludeVersionMetadata: true)["revision"]!.Value<Int64>(), Is.Zero);
        var changed = source.DataSnapshot.ToJSON();
        changed["revision"] = 42; changed["appliedChangeSetId"] = "bookkeeping";
        Assert.That(RoamingNetworkDataSnapshot.Parse(changed).ETags, Is.EqualTo(tags));
        Assert.That(() => source.DataSnapshot.ToCBOR(IncludeRuntime: true), Throws.ArgumentException);
    }

    [Test]
    public void Validated_inherited_station_views_are_removed_from_owned_EVSE_storage()
    {
        var json = JObject.Parse(File.ReadAllText(FilePath("snapshot.input.json")));
        var station = (JObject)json["chargingStationOperators"]![0]!["chargingPools"]![0]!["chargingStations"]![0]!;
        station["openingTimes"] = new JObject(new JProperty("24/7", true));
        var expected = RoamingNetwork.Parse(json).DataSnapshot;
        station["EVSEs"]![0]!["openingTimes"] = station["openingTimes"]!.DeepClone();
        var actual = RoamingNetwork.Parse(json).DataSnapshot;
        Assert.That(actual.ETags, Is.EqualTo(expected.ETags));
        Assert.That(actual.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties.ContainsKey("openingTimes"), Is.False);
    }
}
