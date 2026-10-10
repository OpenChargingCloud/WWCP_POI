/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using Org.BouncyCastle.Crypto.Signers;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class SnapshotRetentionReferenceTests
{
    private const String ManifestFile = "snapshot-retention.vectors.json";
    private static readonly (String File, String Field)[] Artifacts = [
        ("snapshot-commit.json", "snapshotJSON"), ("snapshot-commit.cbor", "snapshotCBORHex"),
        ("snapshot-complete-history.json", "completeJSON"), ("snapshot-complete-history.cbor", "completeCBORHex"),
        ("snapshot-boundary-history.json", "boundaryJSON"), ("snapshot-boundary-history.cbor", "boundaryCBORHex"),
        ("snapshot-suffix-page.json", "pageJSON"), ("snapshot-suffix-page.cbor", "pageCBORHex"),
        ("retention-plan.json", "planJSON"),
        ("retention-receipt.json", "receiptJSON"), ("retention-receipt.cbor", "receiptCBORHex"),
        ("pruned-history.json", "prunedJSON"), ("pruned-history.cbor", "prunedCBORHex"),
        ("pruned-bootstrap-manifest.json", "manifestJSON"), ("pruned-bootstrap-manifest.cbor", "manifestCBORHex")
    ];

    private static SortedDictionary<String, String> BuildVectors()
    {
        using var history = History(); var snapshot = LinearHistory(history);
        using var boundary = RoamingNetworkHistory.FromSnapshot(history.CheckpointId, snapshot, AcceptChain(history), VerifyCommit, VerifyBatch);
        Assert.That(history.TryCreateCommitPack(boundary.GetReplicationState(), history.Head.Id, out var page, out var report), Is.True, report.Error);
        Store(boundary, history.Head.Commit);
        Assert.That(boundary.TryAdoptHead(snapshot.Id, history.Head.Id, out var adopted, adopt: true), Is.True, adopted.Error);
        var completeJSON = history.ToJSON(); var completeCBOR = history.ToCBOR(); var plan = Plan(history, snapshot);
        using var directory = new ArchiveDirectory(); var receipt = Prune(history, plan, directory.ArchivePath).Receipt!;
        var manifest = history.CreateBootstrap(256).Manifest;
        return new(StringComparer.Ordinal) {
            ["contentProfile"] = POIContentProfile.Id,
            ["checkpointId"] = history.CheckpointId.ToString(), ["headId"] = history.Head.Id.ToString(),
            ["snapshotId"] = snapshot.Id.ToString(), ["snapshotIdentityHex"] = Hex(snapshot.GetIdentityBytes()),
            ["snapshotSigningAliceHex"] = Hex(snapshot.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice")),
            ["snapshotSigningBobHex"] = Hex(snapshot.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-bob")),
            ["snapshotSignatureAliceBase64"] = snapshot.Signatures[0].Value,
            ["snapshotSignatureBobBase64"] = snapshot.Signatures[1].Value,
            ["snapshotJSON"] = CanonicalJSON.Serialize(Value(snapshot.ToJSON())), ["snapshotCBORHex"] = Hex(snapshot.ToCBOR()),
            ["completeJSON"] = CanonicalJSON.Serialize(Value(completeJSON)), ["completeCBORHex"] = Hex(completeCBOR),
            ["boundaryJSON"] = CanonicalJSON.Serialize(Value(boundary.ToJSON())), ["boundaryCBORHex"] = Hex(boundary.ToCBOR()),
            ["pageJSON"] = CanonicalJSON.Serialize(Value(page!.ToJSON())), ["pageCBORHex"] = Hex(page.ToCBOR()),
            ["announcementJSON"] = CanonicalJSON.Serialize(Value(boundary.GetReplicationState().ToJSON())),
            ["announcementCBORHex"] = Hex(boundary.GetReplicationState().ToCBOR()),
            ["planId"] = plan.Id.ToString(), ["planJSON"] = CanonicalJSON.Serialize(Value(plan.ToJSON())),
            ["sourceArchiveETag"] = plan.SourceArchiveETag.ToString(),
            ["receiptId"] = receipt.Id.ToString(), ["receiptJSON"] = CanonicalJSON.Serialize(Value(receipt.ToJSON())),
            ["receiptCBORHex"] = Hex(receipt.ToCBOR()),
            ["prunedJSON"] = CanonicalJSON.Serialize(Value(history.ToJSON())), ["prunedCBORHex"] = Hex(history.ToCBOR()),
            ["manifestId"] = manifest.Id.ToString(), ["manifestArchiveETag"] = manifest.ArchiveETag.ToString(),
            ["manifestJSON"] = CanonicalJSON.Serialize(Value(manifest.ToJSON())), ["manifestCBORHex"] = Hex(manifest.ToCBOR())
        };
    }

    [TestCase("en-US")]
    [TestCase("de-DE")]
    [TestCase("ar-EG")]
    public void Fixed_snapshot_retention_references_match_all_bytes_and_identities_across_cultures(String culture)
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var expected = JsonSerializer.Deserialize<SortedDictionary<String, String>>(File.ReadAllText(FilePath(ManifestFile)))!;
            var actual = BuildVectors(); Assert.That(actual.Keys, Is.EqualTo(expected.Keys));
            foreach (var pair in expected) Assert.That(actual[pair.Key], Is.EqualTo(pair.Value), pair.Key);
        }
        finally { CultureInfo.CurrentCulture = original; }
    }

    [Test]
    public void Reference_artifacts_match_manifest_and_recover_complete_boundary_and_pruned_histories()
    {
        var vectors = JsonSerializer.Deserialize<Dictionary<String, String>>(File.ReadAllText(FilePath(ManifestFile)))!;
        foreach (var (file, field) in Artifacts) Assert.That(File.ReadAllBytes(FilePath(file)), Is.EqualTo(ArtifactBytes(field, vectors[field])), file);
        var checkpoint = RoamingNetworkCommitId.Parse(vectors["checkpointId"]);
        Boolean Accept(RoamingNetworkSnapshotBoundary boundary) => boundary.Checkpoint == checkpoint && boundary.Anchor.ToString() == vectors["snapshotId"];
        foreach (var key in new[] { "complete", "boundary", "pruned" })
        {
            using var json = RoamingNetworkHistory.Parse(vectors[key + "JSON"], VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: Accept);
            using var cbor = RoamingNetworkHistory.ParseCBOR(Convert.FromHexString(vectors[key + "CBORHex"]), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: Accept);
            Assert.That(json.Head.Id.ToString(), Is.EqualTo(vectors["headId"])); Assert.That(cbor.ToCBOR(), Is.EqualTo(json.ToCBOR()));
            Assert.That(json.HasCompleteAncestry, Is.EqualTo(key == "complete"));
            if (key == "pruned") Assert.That(json.RetentionReceipts[0].Id.ToString(), Is.EqualTo(vectors["receiptId"]));
        }
    }

    [Test]
    public void Reference_digests_and_snapshot_signatures_verify_directly_against_the_published_preimages()
    {
        var vectors = JsonSerializer.Deserialize<Dictionary<String, String>>(File.ReadAllText(FilePath(ManifestFile)))!;
        var snapshot = RoamingNetworkCommit.Parse(vectors["snapshotJSON"]);
        Assert.That(snapshot.GetIdentityBytes(), Is.EqualTo(Convert.FromHexString(vectors["snapshotIdentityHex"])));
        Assert.That(snapshot.Id.Hash.Digest.ToArray(), Is.EqualTo(SHA256.HashData(Convert.FromHexString(vectors["snapshotIdentityHex"]))));
        foreach (var name in new[] { "Alice", "Bob" })
        {
            var message = Convert.FromHexString(vectors["snapshotSigning" + name + "Hex"]);
            var signer = new Ed25519Signer(); signer.Init(false, name == "Alice" ? Alice.GeneratePublicKey() : Bob.GeneratePublicKey());
            signer.BlockUpdate(message, 0, message.Length);
            Assert.That(signer.VerifySignature(Convert.FromBase64String(vectors["snapshotSignature" + name + "Base64"])), Is.True);
        }
        foreach (var kind in new[] { "plan", "receipt", "manifest" })
        {
            using var json = JsonDocument.Parse(vectors[kind + "JSON"]);
            var withoutId = json.RootElement.EnumerateObject().Where(field => field.Name != "Id").ToDictionary(field => field.Name, field => field.Value.Clone());
            var canonical = CanonicalJSON.ToUTF8Bytes(JsonSerializer.SerializeToElement(withoutId));
            Assert.That(ETag.Parse(vectors[kind + "Id"]).Digest.ToArray(), Is.EqualTo(SHA256.HashData(canonical)), kind);
        }
        Assert.That(ETag.Parse(vectors["sourceArchiveETag"]), Is.EqualTo(ETag.Compute(ETagFormat.CBOR, Convert.FromHexString(vectors["completeCBORHex"]))));
        Assert.That(ETag.Parse(vectors["manifestArchiveETag"]), Is.EqualTo(ETag.Compute(ETagFormat.CBOR, Convert.FromHexString(vectors["prunedCBORHex"]))));
    }

    [Test, Explicit("Generate reviewed snapshot/retention regression references deliberately; ordinary tests never rewrite them.")]
    public void GenerateSnapshotRetentionVectors()
    {
        var directory = Path.GetFullPath("../../../../docs/interoperability", TestContext.CurrentContext.TestDirectory);
        Assert.That(File.Exists(Path.Combine(directory, "snapshot.input.json")), Is.True);
        var vectors = BuildVectors();
        File.WriteAllText(Path.Combine(directory, ManifestFile), JsonSerializer.Serialize(vectors, new JsonSerializerOptions {
            WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }) + "\n", new UTF8Encoding(false));
        foreach (var (file, field) in Artifacts) File.WriteAllBytes(Path.Combine(directory, file), ArtifactBytes(field, vectors[field]));
    }
}
