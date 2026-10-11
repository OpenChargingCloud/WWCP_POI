/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using Org.BouncyCastle.Crypto.Parameters;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Tests.Interoperability;

internal static class InteropFixture
{
    internal static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-02-01T00:00:00Z");
    // Published fixture keys only. Never use these seeds outside reference tests.
    internal static readonly Ed25519PrivateKeyParameters Alice = new(Convert.FromHexString("000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);
    internal static readonly Ed25519PrivateKeyParameters Bob = new(Convert.FromHexString("202122232425262728292a2b2c2d2e2f303132333435363738393a3b3c3d3e3f"), 0);
    internal static String FilePath(String file) => Path.Combine(TestContext.CurrentContext.TestDirectory, "InteropVectors", file);
    internal static JsonElement Value(String value) => JsonSerializer.Deserialize<JsonElement>(value);
    internal static RoamingNetwork Network() => RoamingNetwork.Parse(File.ReadAllText(FilePath("snapshot.input.json")));
    internal static RoamingNetworkChange Power(String value) => RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", null, Value(JsonSerializer.Serialize(value)));
    internal static RoamingNetworkChangeSet Batch(RoamingNetworkDataSnapshot source, String id, params RoamingNetworkChange[] changes)
        => source.CreateChangeSet(id, Time, [.. changes]);
    internal static RoamingNetworkChangeSet Sign(RoamingNetworkChangeSet batch)
        => batch.Sign(Alice, "fixture-alice", COSEAlgorithm.Ed25519).Sign(Bob, "fixture-bob", COSEAlgorithm.Ed25519);
    internal static RoamingNetworkCommit Sign(RoamingNetworkCommit commit)
        => commit.Sign(Alice, "fixture-alice", COSEAlgorithm.Ed25519).Sign(Bob, "fixture-bob", COSEAlgorithm.Ed25519);
    internal static Ed25519PublicKeyParameters? PublicKey(String id) => id switch {
        "fixture-alice" => Alice.GeneratePublicKey(), "fixture-bob" => Bob.GeneratePublicKey(), _ => null
    };
    internal static Boolean VerifyBatch(RoamingNetworkChangeSet batch, RoamingNetworkChangeSetSignature signature)
        => PublicKey(signature.KeyId) is { } key && batch.VerifySignature(signature, key, signature.KeyId, out _);
    internal static Boolean VerifyCommit(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature signature)
        => PublicKey(signature.KeyId) is { } key && commit.VerifySignature(signature, key, signature.KeyId, out _);
    internal static RoamingNetworkHistory History(RoamingNetwork? network = null)
        => new(network ?? Network(), VerifyBatch, VerifyCommit);

    internal static SortedDictionary<String, String> Vectors()
    {
        var source = Network().DataSnapshot;
        using var history = History();
        var checkpoint = Sign(history.Head.Commit);
        Assert.That(history.TryStoreCommit(checkpoint, out var checkpointResult), Is.True, checkpointResult.Error);
        var batch = Sign(Batch(source, "vector-change", Power("150000 W"))
            .WithDescription(ImmutableDictionary<String, String>.Empty.Add("de", "Leistung erhöhen").Add("en", "Increase power"))
            .WithMetadata(ImmutableDictionary<String, JsonElement>.Empty.Add("case", Value("{\"id\":\"ACME-42\",\"decimal\":1.0,\"exponent\":1e0,\"negativeZero\":-0,\"null\":null,\"reading\":\"150000 W\"}"))));
        var commit = Sign(history.PrepareCommit(checkpoint.Id, batch));
        var right = Sign(history.PrepareCommit(checkpoint.Id, Sign(Batch(source, "vector-right",
            RoamingNetworkChange.UpdateProperty("ChargingStation", "DE*ABC*S1", "name", null, Value("{\"en\":\"New station\"}"))))));
        Assert.That(history.TryStoreCommit(commit, out var stored), Is.True, stored.Error);
        Assert.That(history.TryStoreCommit(right, out stored), Is.True, stored.Error);
        Assert.That(history.TryMerge(commit.Id, right.Id, out var merge, out var report, merge: true,
            mergedChangeSetId: "vector-merge", createdAt: Time.AddDays(1)), Is.True, report.Message);
        merge = Sign(merge!.WithChangeSet(Sign(merge.ChangeSet!)));
        var conflict = Sign(history.PrepareCommit(checkpoint.Id, Sign(Batch(source, "vector-conflict", Power("200 kW")))));
        Assert.That(history.TryStoreCommit(conflict, out stored), Is.True, stored.Error);
        Assert.That(history.TryMerge(commit.Id, conflict.Id, out var resolved, out report, merge: true,
            mergedChangeSetId: "vector-resolved", createdAt: Time.AddDays(1),
            resolveConflict: _ => RoamingNetworkMergeResolution.Custom(Value("\"175000 W\""))), Is.True, report.Message);
        resolved = Sign(resolved!.WithChangeSet(Sign(resolved.ChangeSet!)));
        Assert.That(history.TryStoreCommit(resolved, out stored), Is.True, stored.Error);
        Assert.That(history.TryPublish(checkpoint.Id, commit, out stored), Is.True, stored.Error);
        Assert.That(history.TryPublish(commit.Id, merge, out stored), Is.True, stored.Error);
        return new(StringComparer.Ordinal) {
            ["contentProfile"] = POIContentProfile.Id,
            ["alicePublicKeyHex"] = Hex(Alice.GeneratePublicKey().GetEncoded()),
            ["bobPublicKeyHex"] = Hex(Bob.GeneratePublicKey().GetEncoded()),
            ["staticCanonicalJSONHex"] = Hex(source.ToCanonicalJSON()),
            ["staticCanonicalCBORHex"] = Hex(source.ToCanonicalCBOR()),
            ["jsonETag"] = source.ETags[0].ToString(), ["cborETag"] = source.ETags[1].ToString(),
            ["snapshotJSON"] = source.ToJSON().ToString(Newtonsoft.Json.Formatting.None),
            ["snapshotCBORHex"] = Hex(source.ToCBOR(IncludeVersionMetadata: true)),
            ["checkpointId"] = checkpoint.Id.ToString(), ["checkpointIdentityHex"] = Hex(checkpoint.GetIdentityBytes()),
            ["changeSetJSON"] = CanonicalJSON.Serialize(Value(JsonSerializer.Serialize(batch))), ["changeSetCBORHex"] = Hex(batch.ToCBOR()),
            ["batchSigningAliceHex"] = Hex(batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice")),
            ["batchSignatureAliceBase64"] = batch.Signatures[0].Value,
            ["batchSignatureBobBase64"] = batch.Signatures[1].Value,
            ["commitId"] = commit.Id.ToString(), ["commitIdentityHex"] = Hex(commit.GetIdentityBytes()),
            ["commitJSON"] = CanonicalJSON.Serialize(Value(commit.ToJSON())), ["commitCBORHex"] = Hex(commit.ToCBOR()),
            ["commitSigningAliceHex"] = Hex(commit.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice")),
            ["commitSignatureAliceBase64"] = commit.Signatures[0].Value,
            ["commitSignatureBobBase64"] = commit.Signatures[1].Value,
            ["mergeId"] = merge.Id.ToString(), ["mergeJSON"] = CanonicalJSON.Serialize(Value(merge.ToJSON())), ["mergeCBORHex"] = Hex(merge.ToCBOR()),
            ["resolvedMergeId"] = resolved.Id.ToString(), ["resolvedMergeJSON"] = CanonicalJSON.Serialize(Value(resolved.ToJSON())), ["resolvedMergeCBORHex"] = Hex(resolved.ToCBOR()),
            ["historyJSON"] = CanonicalJSON.Serialize(Value(history.ToJSON())), ["historyCBORHex"] = Hex(history.ToCBOR())
        };
    }
    internal static readonly (String File, String Field)[] ArtifactFields = [
        ("snapshot.canonical.json", "staticCanonicalJSONHex"), ("snapshot.canonical.cbor", "staticCanonicalCBORHex"),
        ("snapshot.json", "snapshotJSON"), ("snapshot.cbor", "snapshotCBORHex"),
        ("changeset.json", "changeSetJSON"), ("changeset.cbor", "changeSetCBORHex"),
        ("commit.json", "commitJSON"), ("commit.cbor", "commitCBORHex"),
        ("merge.json", "mergeJSON"), ("merge.cbor", "mergeCBORHex"),
        ("resolved-merge.json", "resolvedMergeJSON"), ("resolved-merge.cbor", "resolvedMergeCBORHex"),
        ("history.json", "historyJSON"), ("history.cbor", "historyCBORHex")
    ];
    internal static Byte[] ArtifactBytes(String field, String value)
        => field.EndsWith("Hex", StringComparison.Ordinal) ? Convert.FromHexString(value) : Encoding.UTF8.GetBytes(value);
    internal static String Hex(Byte[] bytes) => Convert.ToHexStringLower(bytes);
}
