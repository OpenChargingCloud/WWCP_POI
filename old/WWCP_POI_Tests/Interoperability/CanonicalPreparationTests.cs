/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class CanonicalPreparationTests
{
    private static readonly String[] Archives = ["history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history"];
    private static Byte[] Bytes(Int32 profile) => File.ReadAllBytes(FilePath(Archives[profile - 1] + ".cbor"));
    private static RoamingNetworkChangeSet RichBatch()
        => Batch(Network().DataSnapshot, "canonical-preparation", Power("250 kW"))
            .WithDescription("de", "Mehr Leistung 🔌\u2028\u2029").WithDescription("en", "Increase power <>&")
            .WithMetadata("tokens", Value("{\"z\":null,\"reading\":\"250 kW\",\"decimal\":1.0,\"exponent\":1e0,\"negativeZero\":-0,\"nested\":[true,{\"ä\":\"温度\"}]}"));
    private static RoamingNetworkCommit Commit(Int32 kind)
    {
        var source = Network().DataSnapshot; var root = RoamingNetworkCommit.CreateCheckpoint(source);
        return kind switch { 0 => root, 1 => RoamingNetworkCommit.Create(root, RichBatch()),
            _ => RoamingNetworkCommit.CreateSnapshot(root, source, InteropFixture.Time,
                ImmutableDictionary<String, String>.Empty.Add("de", "Vollständiger Stand 🔌"),
                ImmutableDictionary<String, JsonElement>.Empty.Add("tokens", Value("{\"decimal\":1.0,\"reading\":\"250 kW\"}"))) };
    }
    private static COSEAlgorithm Algorithm(Int32 kind) => kind switch { 0 => COSEAlgorithm.Ed25519, 1 => COSEAlgorithm.ES256, _ => COSEAlgorithm.ES384 };
    private static readonly String[] Keys = ["fixture-alice", "🔌温度ä<>&\u2028\u2029", "\"Commit\":null,\"ChangeSet\":null", "a\u0000b\\c\nd", "\ud800"];

    [Test, Combinatorial]
    public void All_payloads_algorithms_and_header_strings_match_preceding_canonical_bytes(
        [Values(0, 1, 2)] Int32 kind, [Values(0, 1, 2)] Int32 algorithm, [Range(0, 4)] Int32 key)
    {
        var commit = Commit(kind); var batch = RichBatch(); var selected = Algorithm(algorithm);
        var expected = CanonicalPreparationOracle.Signing(commit, selected, Keys[key]);
        var batchExpected = CanonicalPreparationOracle.Signing(batch, selected, Keys[key]);
        using (POICanonicalPreparation.Enter())
        {
            Assert.That(commit.GetIdentityBytes(), Is.EqualTo(CanonicalPreparationOracle.Identity(commit)));
            Assert.That(commit.GetSigningBytes(selected, Keys[key]), Is.EqualTo(expected));
            Assert.That(batch.GetSigningBytes(selected, Keys[key]), Is.EqualTo(batchExpected));
            var retained = POICanonicalPreparation.Current!; var count = retained.Entries; var bytes = retained.Bytes;
            Array.Fill(commit.GetIdentityBytes(), (Byte)0xff); Array.Fill(commit.GetSigningBytes(selected, Keys[key]), (Byte)0xff);
            Array.Fill(batch.GetSigningBytes(selected, Keys[key]), (Byte)0xff);
            Assert.That(commit.GetSigningBytes(selected, Keys[key]), Is.EqualTo(expected));
            Assert.That(batch.GetSigningBytes(selected, Keys[key]), Is.EqualTo(batchExpected));
            Assert.That(retained.Entries, Is.EqualTo(count)); Assert.That(retained.Bytes, Is.EqualTo(bytes));
        }
        Assert.That(POICanonicalPreparation.Current, Is.Null);
        Assert.That(commit.GetSigningBytes(selected, Keys[key]), Is.EqualTo(expected));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void Original_deterministic_signatures_IDs_and_unsigned_copy_checks_remain_exact(Int32 kind)
    {
        var original = Commit(kind); var batch = RichBatch(); var equal = batch with { }; var hash = batch.GetHashCode();
        using (POICanonicalPreparation.Enter())
        {
            var signed = Sign(original);
            Assert.That(signed.Id, Is.EqualTo(original.Id));
            Assert.That(signed.GetIdentityBytes(), Is.EqualTo(CanonicalPreparationOracle.Identity(original)));
            Assert.That(signed.Signatures[0].Value, Is.EqualTo(Convert.ToBase64String(COSEAlgorithm.Ed25519.Sign(
                CanonicalPreparationOracle.Signing(original, COSEAlgorithm.Ed25519, "fixture-alice"), Alice))));
            Assert.That(signed.VerifySignatures(peer => PublicKey(peer.KeyId), out var error), Is.True, error);
            _ = batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice");
            Assert.That(batch, Is.EqualTo(equal)); Assert.That(batch.GetHashCode(), Is.EqualTo(hash));
            var changed = batch.WithMetadata("different", Value("1.0"));
            Assert.That(changed.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"),
                Is.Not.EqualTo(batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice")));
            if (kind == 1)
            {
                Assert.That(signed.WithChangeSet(Sign(original.ChangeSet!)).Id, Is.EqualTo(original.Id));
                Assert.Throws<ArgumentException>(() => signed.WithChangeSet(original.ChangeSet!.WithMetadata("different", Value("1.0"))));
            }
            var value = System.Text.Json.Nodes.JsonNode.Parse(original.ToJSON())!;
            value["Revision"] = original.Revision + 1;
            var count = POICanonicalPreparation.Current!.Entries;
            Assert.Throws<ArgumentException>(() => RoamingNetworkCommit.Parse(value.ToJsonString()));
            Assert.That(POICanonicalPreparation.Current.Entries, Is.EqualTo(count));
        }
        Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    [TestCase(59)] [TestCase(60)] [TestCase(61)]
    public void Commit_signing_keeps_the_envelope_depth_limit_after_successful_identity_preparation(Int32 depth)
    {
        var source = Network().DataSnapshot; var root = RoamingNetworkCommit.CreateCheckpoint(source);
        var batch = RichBatch().WithMetadata("deep", Deep(depth));
        using var scope = POICanonicalPreparation.Enter();
        var value = RoamingNetworkCommit.Create(root, batch);
        Assert.That(value.GetIdentityBytes(), Is.EqualTo(CanonicalPreparationOracle.Identity(value)));
        Byte[]? expected = null; Exception? failure = null;
        try { expected = CanonicalPreparationOracle.Signing(value, COSEAlgorithm.Ed25519, "fixture-alice"); }
        catch (Exception error) { failure = error; }
        if (failure is null) Assert.That(value.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"), Is.EqualTo(expected));
        else
        {
            var actual = Assert.Catch(() => value.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"))!;
            Assert.That(actual.GetType(), Is.EqualTo(failure.GetType())); Assert.That(actual.Message, Is.EqualTo(failure.Message));
        }
    }

    private static JsonElement Deep(Int32 depth)
    {
        using var document = JsonDocument.Parse(new String('[', depth) + "1.0" + new String(']', depth), new() { MaxDepth = 2048 });
        return document.RootElement.Clone();
    }

    [Test, Combinatorial]
    public void Deep_signing_inputs_keep_the_exact_existing_envelope_errors(
        [Values(0, 58, 59, 60, 61, 62, 63, 64, 65, 1000)] Int32 depth, [Values(false, true)] Boolean scoped)
    {
        var batch = RichBatch().WithMetadata("deep", Deep(depth));
        using var scope = scoped ? POICanonicalPreparation.Enter() : default;
        Byte[]? expected = null; Exception? failure = null;
        try { expected = CanonicalPreparationOracle.Signing(batch, COSEAlgorithm.Ed25519, "fixture-alice"); }
        catch (Exception error) { failure = error; }
        if (failure is null) Assert.That(batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"), Is.EqualTo(expected));
        else
        {
            var actual = Assert.Catch(() => batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"))!;
            Assert.That(actual.GetType(), Is.EqualTo(failure.GetType())); Assert.That(actual.Message, Is.EqualTo(failure.Message));
        }
    }

    [TestCase("{\"x\":1,\"x\":2}")] [TestCase("{\"nested\":[{\"x\":1,\"x\":2}]}")]
    public void Invalid_operation_values_are_not_cached_and_keep_the_original_error(String text)
    {
        var source = Network().DataSnapshot;
        var batch = new RoamingNetworkChangeSet("invalid", source.Root.Id, source.Revision, InteropFixture.Time,
            [RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null, Value(text))], source.ETags, source.ETags);
        using var scope = POICanonicalPreparation.Enter();
        var expected = Assert.Catch(() => CanonicalPreparationOracle.Signing(batch, COSEAlgorithm.Ed25519, "fixture-alice"))!;
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var actual = Assert.Catch(() => batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"))!;
            Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
            Assert.That(POICanonicalPreparation.Current!.Entries, Is.Zero);
        }
    }

    [TestCase("1e9999")] [TestCase("-0")] [TestCase("1.0")] [TestCase("1e0")] [TestCase("18446744073709551616")]
    public void Canonical_signing_keeps_valid_number_tokens_without_numeric_conversion(String text)
    {
        var source = Network().DataSnapshot;
        var batch = new RoamingNetworkChangeSet("numeric", source.Root.Id, source.Revision, InteropFixture.Time,
            [RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null, Value(text))], source.ETags, source.ETags);
        using var scope = POICanonicalPreparation.Enter();
        Assert.That(batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"),
            Is.EqualTo(CanonicalPreparationOracle.Signing(batch, COSEAlgorithm.Ed25519, "fixture-alice")));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void Algorithm_and_key_guards_precede_preparation(Int32 invalid)
    {
        var algorithm = invalid == 0 ? COSEAlgorithm.SHA256 : COSEAlgorithm.Ed25519;
        var key = invalid == 1 ? "" : invalid == 2 ? " " : "fixture-alice";
        var commit = Commit(0); var batch = RichBatch(); using var scope = POICanonicalPreparation.Enter();
        Assert.Throws<ArgumentException>(() => commit.GetSigningBytes(algorithm, key));
        Assert.Throws<ArgumentException>(() => batch.GetSigningBytes(algorithm, key));
        Assert.That(POICanonicalPreparation.Current!.Entries, Is.Zero);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void Admission_bounds_fall_back_to_fresh_preparation_without_rejecting_content(Int32 bound)
    {
        using var scope = POICanonicalPreparation.Enter(); var context = POICanonicalPreparation.Current!;
        var size = bound == 0 ? 2 : bound == 1 ? POICanonicalPreparation.MaxValueBytes : POICanonicalPreparation.MaxValueBytes + 1;
        var expectedCount = bound == 0 ? POICanonicalPreparation.MaxEntries : bound == 1 ? 4 : 0;
        var owners = Enumerable.Range(0, bound == 0 ? 70 : 6).Select(_ => new Object()).ToArray(); var calls = 0;
        void Write(Utf8JsonWriter writer) { calls++; writer.WriteStringValue(new String('a', size - 2)); }
        foreach (var owner in owners) Assert.That(POICanonicalPreparation.Get(owner, Write).Span.Length, Is.EqualTo(size));
        Assert.That(context.Entries, Is.EqualTo(expectedCount)); Assert.That(context.Bytes, Is.EqualTo(expectedCount * size));
        var before = calls;
        foreach (var owner in owners) _ = POICanonicalPreparation.Get(owner, Write);
        Assert.That(calls - before, Is.EqualTo(owners.Length - expectedCount));
        Assert.That(context.Bytes, Is.LessThanOrEqualTo(POICanonicalPreparation.MaxTotalBytes));
    }

    [Test]
    public void Nested_and_copied_scopes_release_their_own_context_only()
    {
        Assert.That(POICanonicalPreparation.Current, Is.Null);
        var outer = POICanonicalPreparation.Enter(); var copy = outer; var first = POICanonicalPreparation.Current!;
        using (POICanonicalPreparation.Enter()) { _ = Commit(0).GetIdentityBytes(); Assert.That(POICanonicalPreparation.Current, Is.SameAs(first)); }
        Assert.That(first.Entries, Is.EqualTo(1)); outer.Dispose(); Assert.That(first.Entries, Is.Zero); Assert.That(first.Bytes, Is.Zero);
        using var next = POICanonicalPreparation.Enter(); var second = POICanonicalPreparation.Current;
        copy.Dispose(); Assert.That(POICanonicalPreparation.Current, Is.SameAs(second));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference ReleasedOwner()
    {
        using var scope = POICanonicalPreparation.Enter(); var owner = new Object();
        _ = POICanonicalPreparation.Get(owner, writer => writer.WriteNumberValue(1));
        return new(owner);
    }

    [Test]
    public void Scope_completion_releases_owner_references()
    {
        var owner = ReleasedOwner(); GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        Assert.That(owner.IsAlive, Is.False); Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    [Test]
    public void Preparation_does_not_flow_to_other_threads_and_parallel_readers_are_independent()
    {
        var batch = RichBatch(); var expected = CanonicalPreparationOracle.Signing(batch, COSEAlgorithm.Ed25519, "fixture-alice");
        using var scope = POICanonicalPreparation.Enter(); var parent = POICanonicalPreparation.Current;
        var readers = Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
        {
            Assert.That(POICanonicalPreparation.Current, Is.Null);
            using (POICanonicalPreparation.Enter())
            {
                Assert.That(POICanonicalPreparation.Current, Is.Not.SameAs(parent));
                Assert.That(batch.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"), Is.EqualTo(expected));
                Assert.That(POICanonicalPreparation.Current!.Entries, Is.EqualTo(1));
            }
            Assert.That(POICanonicalPreparation.Current, Is.Null);
        })).ToArray();
        Task.WaitAll(readers); Assert.That(POICanonicalPreparation.Current, Is.SameAs(parent));
    }

    [TestCase(false)] [TestCase(true)]
    public void Every_peer_resolves_a_fresh_key_and_failed_verification_releases_preparation(Boolean commit)
    {
        var batch = Sign(RichBatch()); var value = Sign(Commit(1)); var calls = 0; POICanonicalPreparation? context = null;
        Org.BouncyCastle.Crypto.AsymmetricKeyParameter? Resolve(RoamingNetworkChangeSetSignature peer)
        {
            context ??= POICanonicalPreparation.Current; calls++;
            Assert.That(POICanonicalPreparation.Current, Is.SameAs(context));
            return PublicKey(peer.KeyId);
        }
        Assert.That(commit ? value.VerifySignatures(Resolve, out _) : batch.VerifySignatures(Resolve, out _), Is.True);
        Assert.That(calls, Is.EqualTo(2)); Assert.That(context, Is.Not.Null);
        Assert.That(context!.Entries, Is.Zero); Assert.That(context.Bytes, Is.Zero); Assert.That(POICanonicalPreparation.Current, Is.Null);
        calls = 0;
        Assert.That(commit ? value.VerifySignatures(_ => { calls++; return null; }, out _) : batch.VerifySignatures(_ => { calls++; return null; }, out _), Is.False);
        Assert.That(calls, Is.EqualTo(1)); Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    [Test, Combinatorial]
    public void Recovery_scopes_cover_original_peers_and_release_all_bytes_and_preserve_local_runtime(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean cbor)
    {
        var bytes = Bytes(profile); POICanonicalPreparation? context = null; var commitCalls = 0; var batchCalls = 0;
        Boolean TrustCommit(RoamingNetworkCommit value, RoamingNetworkChangeSetSignature peer)
        {
            context ??= POICanonicalPreparation.Current; Assert.That(POICanonicalPreparation.Current, Is.SameAs(context));
            commitCalls++; var result = VerifyCommit(value, peer);
            Assert.That(value.GetSigningBytes(COSEAlgorithm.Ed25519, peer.KeyId), Is.EqualTo(CanonicalPreparationOracle.Signing(value, COSEAlgorithm.Ed25519, peer.KeyId)));
            Assert.That(context!.Entries, Is.GreaterThan(0)); return result;
        }
        Boolean TrustBatch(RoamingNetworkChangeSet value, RoamingNetworkChangeSetSignature peer)
        { batchCalls++; Assert.That(POICanonicalPreparation.Current, Is.SameAs(context)); return VerifyBatch(value, peer); }
        using var source = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        source.ApplyRuntimeUpdate(new(source.Head.Network.Id.ToString(), POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
            POIRuntimeStatusKind.Status, new("charging", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        using var restored = cbor ? RoamingNetworkHistory.ParseCBOR(bytes, TrustBatch, TrustCommit, authorizeSnapshotBoundary: _ => true) :
            RoamingNetworkHistory.Parse(source.ToJSON(), TrustBatch, TrustCommit, authorizeSnapshotBoundary: _ => true);
        Assert.That(restored.ToCBOR(), Is.EqualTo(bytes)); Assert.That(commitCalls, Is.GreaterThan(0));
        Assert.That(batchCalls, Is.EqualTo(source.Commits.Sum(value => value.ChangeSet?.Signatures.Length ?? 0)));
        foreach (var value in source.Commits) Assert.That(restored.GetSnapshot(value.Id).ToCanonicalCBOR(), Is.EqualTo(source.GetSnapshot(value.Id).ToCanonicalCBOR()));
        Assert.That(restored.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
        Assert.That(source.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        Assert.That(context!.Bytes, Is.Zero); Assert.That(context.Entries, Is.Zero); Assert.That(POICanonicalPreparation.Current, Is.Null);
        var rejected = 0;
        Assert.Catch(() => { using var result = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, (_, _) => { rejected++; return false; }, authorizeSnapshotBoundary: _ => true); });
        Assert.That(rejected, Is.EqualTo(1)); Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    private static CBORValue Replace(CBORValue map, String field, CBORValue value)
        => CBORValue.FromMap(map.AsMap().Select(item => new KeyValuePair<CBORValue, CBORValue>(item.Key,
            RoamingNetworkCommit.Text(item.Key) == field ? value : item.Value)));

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Late_model_errors_preserve_eager_or_lazy_trust_and_release_preparation(Int32 profile)
    {
        var value = CBORValue.Parse(Bytes(profile)); var calls = 0;
        var commits = value.AsMap().Single(item => RoamingNetworkCommit.Text(item.Key) == "Commits").Value.AsArray();
        var bad = Replace(value, "Commits", CBORValue.FromArray(commits.Append(CBORValue.FromText("invalid model"))));
        Assert.Catch(() => { using var history = RoamingNetworkHistory.ParseCBOR(bad.ToByteArray(CBORWriterOptions.Canonical), VerifyBatch,
            (commit, peer) => { calls++; return VerifyCommit(commit, peer); }, authorizeSnapshotBoundary: _ => true); });
        Assert.That(calls, profile <= 2 ? Is.Zero : Is.GreaterThan(0)); Assert.That(POICanonicalPreparation.Current, Is.Null);
        calls = 0; var bytes = Bytes(profile);
        Assert.Catch(() => { using var history = RoamingNetworkHistory.ParseCBOR([.. bytes, 0xff], VerifyBatch,
            (commit, peer) => { calls++; return VerifyCommit(commit, peer); }, authorizeSnapshotBoundary: _ => true); });
        Assert.That(calls, Is.Zero); Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Bootstrap_and_cancelled_recovery_release_preparation_before_retry(Int32 profile)
    {
        var bytes = Bytes(profile);
        using var source = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        POICanonicalPreparation? context = null;
        using var recovered = RoamingNetworkHistory.RestoreBootstrap(bytes, source.CreateBootstrap().Manifest, VerifyBatch,
            (commit, peer) => { context ??= POICanonicalPreparation.Current; return VerifyCommit(commit, peer); }, null, _ => true, new());
        Assert.That(recovered.ToCBOR(), Is.EqualTo(bytes)); Assert.That(context, Is.Not.Null);
        Assert.That(context!.Entries, Is.Zero); Assert.That(POICanonicalPreparation.Current, Is.Null);
        using var input = new MemoryStream(bytes); using var cancel = new CancellationTokenSource(); var calls = 0;
        var error = Assert.Throws<OperationCanceledException>(() =>
        {
            using var history = RoamingNetworkHistory.ParseCBOR(input, VerifyBatch,
                (commit, peer) => { calls++; cancel.Cancel(); return VerifyCommit(commit, peer); },
                authorizeSnapshotBoundary: _ => true, cancellationToken: cancel.Token);
        });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancel.Token)); Assert.That(calls, Is.EqualTo(1));
        Assert.That(POICanonicalPreparation.Current, Is.Null); Assert.That(input.CanRead, Is.True);
        using var retry = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Assert.That(retry.ToCBOR(), Is.EqualTo(bytes));
    }

    [Test]
    public void Rejected_peer_keeps_publication_atomic_and_a_retry_verifies_every_original_peer()
    {
        var reject = true; var calls = 0;
        using var history = new RoamingNetworkHistory(Network(), VerifyBatch, (value, peer) =>
        { calls++; return (!reject || peer.KeyId != "fixture-bob") && VerifyCommit(value, peer); });
        var head = history.Head; var original = history.ToCBOR();
        var commit = Sign(history.PrepareCommit(head.Id, Sign(Batch(head.Snapshot, "atomic-preparation", Power("175 kW")))));
        calls = 0; Assert.That(history.TryPublish(head.Id, commit, out _), Is.False);
        Assert.That(calls, Is.EqualTo(2)); Assert.That(history.Head, Is.SameAs(head)); Assert.That(history.ToCBOR(), Is.EqualTo(original));
        Assert.That(POICanonicalPreparation.Current, Is.Null);
        reject = false; calls = 0; Assert.That(history.TryPublish(head.Id, commit, out var result), Is.True, result.Error);
        Assert.That(calls, Is.EqualTo(2)); Assert.That(history.Head.Id, Is.EqualTo(commit.Id)); Assert.That(POICanonicalPreparation.Current, Is.Null);
    }
}
