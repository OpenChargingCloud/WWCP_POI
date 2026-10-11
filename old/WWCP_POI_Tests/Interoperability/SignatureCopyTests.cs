/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class SignatureCopyTests
{
    private static readonly ConstructorInfo FullConstructor = typeof(RoamingNetworkCommit)
        .GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance).Single(value => value.GetParameters().Length == 9);
    private static RoamingNetworkChangeSet RichBatch()
        => Batch(Network().DataSnapshot, "signature-copy", Power("250 kW"))
            .WithDescription("de", "Mehr Leistung 🔌").WithDescription("en", "Power <>&")
            .WithMetadata("tokens", Value("{\"z\":null,\"reading\":\"250 kW\",\"decimal\":1.0,\"exponent\":1e0,\"negativeZero\":-0}"));
    private static RoamingNetworkCommit Commit(Int32 kind)
    {
        var state = Network().DataSnapshot; var root = RoamingNetworkCommit.CreateCheckpoint(state);
        return kind switch { 0 => root, 1 => RoamingNetworkCommit.Create(root, RichBatch()),
            _ => RoamingNetworkCommit.CreateSnapshot(root, state, InteropFixture.Time,
                ImmutableDictionary<String, String>.Empty.Add("de", "Snapshot 🔌"),
                ImmutableDictionary<String, JsonElement>.Empty.Add("reading", Value("\"250 kW\""))) };
    }
    // The preceding full constructor remains the fallback and independently recomputes/validates the ID.
    private static RoamingNetworkCommit OriginalCopy(RoamingNetworkCommit source,
        ImmutableArray<RoamingNetworkChangeSetSignature> peers, RoamingNetworkChangeSet? batch)
    {
        try { return (RoamingNetworkCommit) FullConstructor.Invoke([source.RoamingNetworkId, source.Revision,
            source.Parents, source.StateETags, source.AppliedChangeSetId, batch, peers, source.Id, source.Snapshot]); }
        catch (TargetInvocationException error) when (error.InnerException is not null)
        { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
    }
    private static void Exact(RoamingNetworkCommit actual, RoamingNetworkCommit expected)
    {
        Assert.That(actual.Id, Is.EqualTo(expected.Id));
        Assert.That(actual.ToJSON(), Is.EqualTo(expected.ToJSON()));
        Assert.That(actual.ToCBOR(), Is.EqualTo(expected.ToCBOR()));
        Assert.That(actual.GetIdentityBytes(), Is.EqualTo(CanonicalPreparationOracle.Identity(expected)));
        foreach (var algorithm in new[] { COSEAlgorithm.Ed25519, COSEAlgorithm.ES256, COSEAlgorithm.ES384 })
            Assert.That(actual.GetSigningBytes(algorithm, "🔌ä<>&\u2028"),
                Is.EqualTo(CanonicalPreparationOracle.Signing(expected, algorithm, "🔌ä<>&\u2028")));
    }

    [Test, Combinatorial]
    public void Every_kind_and_peer_array_matches_the_preceding_constructor(
        [Values(0, 1, 2)] Int32 kind, [Values(0, 1, 2, 3, 4)] Int32 peers, [Values(false, true)] Boolean scoped)
    {
        var source = Sign(Commit(kind)); var originals = source.Signatures;
        var selected = peers switch { 0 => default, 1 => ImmutableArray<RoamingNetworkChangeSetSignature>.Empty,
            2 => [originals[1]], 3 => [originals[1], originals[0]], _ => [originals[0], originals[0], originals[1]] };
        var expected = OriginalCopy(source, selected, source.ChangeSet);
        using var scope = scoped ? POICanonicalPreparation.Enter() : default;
        var copy = source.WithSignatures(selected);
        Exact(copy, expected);
        Assert.That(copy, Is.Not.SameAs(source)); Assert.That(source.Signatures, Is.EqualTo(originals));
        Assert.That(copy.ChangeSet, Is.SameAs(source.ChangeSet)); Assert.That(copy.Snapshot, Is.SameAs(source.Snapshot));
        Assert.That(copy.Parents, Is.EqualTo(source.Parents)); Assert.That(copy.StateETags, Is.EqualTo(source.StateETags));
        Array.Fill(copy.GetIdentityBytes(), (Byte)0xff); Array.Fill(copy.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"), (Byte)0xff);
        Exact(copy, expected); Exact(source, OriginalCopy(source, originals, source.ChangeSet));
        Assert.That(RoamingNetworkCommit.Parse(copy.ToJSON()).ToCBOR(), Is.EqualTo(copy.ToCBOR()));
        // Native CBOR already normalizes object key order. Compare both preceding/current
        // transport routes, rather than requiring the original JSON insertion order on return.
        Exact(RoamingNetworkCommit.ParseCBOR(copy.ToCBOR()), RoamingNetworkCommit.ParseCBOR(expected.ToCBOR()));
    }

    [Test, Combinatorial]
    public void Signing_matches_original_crypto_and_every_copy_resolves_current_keys(
        [Values(0, 1, 2)] Int32 kind, [Values(false, true)] Boolean scoped)
    {
        var source = Commit(kind); using var scope = scoped ? POICanonicalPreparation.Enter() : default;
        var signed = Sign(source);
        Exact(signed, OriginalCopy(source, signed.Signatures, source.ChangeSet));
        Assert.That(signed.Signatures[0].Value, Is.EqualTo(Convert.ToBase64String(COSEAlgorithm.Ed25519.Sign(
            CanonicalPreparationOracle.Signing(source, COSEAlgorithm.Ed25519, "fixture-alice"), Alice))));
        var calls = 0;
        Assert.That(signed.VerifySignatures(peer => { calls++; return PublicKey(peer.KeyId); }, out var error), Is.True, error);
        Assert.That(calls, Is.EqualTo(2)); calls = 0;
        Assert.That(signed.WithSignatures(signed.Signatures).VerifySignatures(_ => { calls++; return null; }, out _), Is.False);
        Assert.That(calls, Is.EqualTo(1)); Assert.That(source.Signatures, Is.Empty);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void Invalid_new_peer_arrays_keep_exact_errors_and_are_not_admitted(Int32 kind)
    {
        using var scope = POICanonicalPreparation.Enter(); var source = Commit(kind);
        var count = POICanonicalPreparation.Current!.Entries; var bytes = POICanonicalPreparation.Current.Bytes;
        ImmutableArray<RoamingNetworkChangeSetSignature> invalid = [null!];
        var expected = Assert.Catch(() => OriginalCopy(source, invalid, source.ChangeSet))!;
        var actual = Assert.Catch(() => source.WithSignatures(invalid))!;
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        Assert.Throws<ArgumentNullException>(() => source.WithSignature(null!));
        Assert.Throws<ArgumentNullException>(() => source.WithChangeSet(null!));
        Assert.That(POICanonicalPreparation.Current.Entries, Is.EqualTo(count)); Assert.That(POICanonicalPreparation.Current.Bytes, Is.EqualTo(bytes));
        Exact(source.WithSignatures(default), OriginalCopy(source, default, source.ChangeSet));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void Copy_without_a_prepared_source_does_not_prepare_or_retain_unsigned_bytes(Int32 kind)
    {
        var source = Commit(kind);
        using var scope = POICanonicalPreparation.Enter(); var copy = source.WithSignatures([]);
        Assert.That(POICanonicalPreparation.Current!.Entries, Is.Zero);
        _ = copy.GetIdentityBytes(); Assert.That(POICanonicalPreparation.Current.Entries, Is.EqualTo(1));
        var next = copy.WithSignatures([]); Assert.That(POICanonicalPreparation.Current.Entries, Is.EqualTo(2));
        Assert.That(next.GetIdentityBytes(), Is.EqualTo(copy.GetIdentityBytes()));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void Copy_aliases_share_one_private_content_and_preserve_entry_bounds_and_release(Int32 kind)
    {
        var source = Commit(kind);
        POICanonicalPreparation context;
        using (POICanonicalPreparation.Enter())
        {
            _ = source.GetIdentityBytes(); context = POICanonicalPreparation.Current!;
            var values = (System.Collections.IDictionary) typeof(POICanonicalPreparation)
                .GetField("values", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(context)!;
            var content = values[source]; var length = source.GetIdentityBytes().Length;
            for (var i = 0; i < 80; i++)
            {
                var copy = source.WithSignatures([]);
                if (context.Entries < POICanonicalPreparation.MaxEntries || values.Contains(copy))
                    Assert.That(values[copy], Is.SameAs(content));
                Assert.That(copy.GetIdentityBytes(), Is.EqualTo(source.GetIdentityBytes()));
                Assert.That(context.Entries, Is.LessThanOrEqualTo(64)); Assert.That(context.Bytes, Is.EqualTo(context.Entries * length));
            }
            Assert.That(context.Entries, Is.EqualTo(64));
        }
        Assert.That(context.Entries, Is.Zero); Assert.That(context.Bytes, Is.Zero); Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    [TestCase(200000)] [TestCase(1100000)]
    public void Aliases_respect_total_and_individual_byte_limits(Int32 size)
    {
        var source = Commit(1);
        var batch = source.ChangeSet!.WithMetadata("large", new String('a', size));
        using var scope = POICanonicalPreparation.Enter();
        var root = RoamingNetworkCommit.CreateCheckpoint(Network().DataSnapshot);
        var original = RoamingNetworkCommit.Create(root, batch); var length = original.GetIdentityBytes().Length;
        var initialCount = POICanonicalPreparation.Current!.Entries;
        var initialBytes = POICanonicalPreparation.Current.Bytes;
        for (var i = 0; i < 80; i++)
        {
            var copy = original.WithSignatures([]); Assert.That(copy.Id, Is.EqualTo(original.Id));
            Assert.That(POICanonicalPreparation.Current.Entries, Is.LessThanOrEqualTo(64));
            Assert.That(POICanonicalPreparation.Current.Bytes, Is.LessThanOrEqualTo(4 * 1024 * 1024));
        }
        Assert.That(POICanonicalPreparation.Current.Entries, Is.EqualTo(length > POICanonicalPreparation.MaxValueBytes ? initialCount :
            initialCount + (POICanonicalPreparation.MaxTotalBytes - initialBytes) / length));
    }

    [Test, Combinatorial]
    public void Batch_peer_replacements_preserve_exact_identity_and_original_record_equality(
        [Values(0, 1, 2, 3)] Int32 mode, [Values(false, true)] Boolean scoped)
    {
        var original = Commit(1); var batch = original.ChangeSet!; var equal = batch with { }; var hash = batch.GetHashCode();
        var replacement = mode switch { 0 => batch, 1 => Sign(batch), 2 => batch.WithoutSignatures(),
            _ => JsonSerializer.Deserialize<RoamingNetworkChangeSet>(JsonSerializer.Serialize(Sign(batch)))! };
        using var scope = scoped ? POICanonicalPreparation.Enter() : default;
        Exact(original.WithChangeSet(replacement), OriginalCopy(original, original.Signatures, replacement));
        Assert.That(original.WithChangeSet(replacement).ChangeSet, Is.SameAs(replacement));
        Assert.That(batch, Is.EqualTo(equal)); Assert.That(batch.GetHashCode(), Is.EqualTo(hash));
        Assert.That(batch.Signatures, Is.Empty); Assert.That(batch.Description, Is.SameAs(equal.Description));
    }

    [TestCase(0)] [TestCase(2)]
    public void Checkpoints_and_snapshots_cannot_gain_a_batch(Int32 kind)
    {
        var error = Assert.Throws<InvalidOperationException>(() => Commit(kind).WithChangeSet(RichBatch()));
        Assert.That(error!.Message, Is.EqualTo("This commit has no batch."));
    }

    [Test, Combinatorial]
    public void Every_unsigned_field_change_uses_full_validation_and_keeps_exact_errors(
        [Range(0, 8)] Int32 field, [Values(false, true)] Boolean scoped)
    {
        var source = Commit(1); var batch = source.ChangeSet!;
        var replacement = field switch {
            0 => batch.WithDescription("de", "Changed"), 1 => batch.WithMetadata("tokens", Value("{\"decimal\":1}")),
            2 => new("other", batch.RoamingNetworkId, batch.BaseRevision, batch.CreatedAt, batch.Changes, batch.BeforeETags, batch.AfterETags),
            3 => new(batch.Id, "other", batch.BaseRevision, batch.CreatedAt, batch.Changes, batch.BeforeETags, batch.AfterETags),
            4 => new(batch.Id, batch.RoamingNetworkId, batch.BaseRevision + 1, batch.CreatedAt, batch.Changes, batch.BeforeETags, batch.AfterETags),
            5 => new(batch.Id, batch.RoamingNetworkId, batch.BaseRevision, batch.CreatedAt.AddSeconds(1), batch.Changes, batch.BeforeETags, batch.AfterETags),
            6 => new(batch.Id, batch.RoamingNetworkId, batch.BaseRevision, batch.CreatedAt, [Power("100 kW")], batch.BeforeETags, batch.AfterETags),
            7 => new(batch.Id, batch.RoamingNetworkId, batch.BaseRevision, batch.CreatedAt, batch.Changes,
                [ETag.Compute(ETagFormat.JSON, [1]), batch.BeforeETags[1]], batch.AfterETags),
            _ => new(batch.Id, batch.RoamingNetworkId, batch.BaseRevision, batch.CreatedAt, batch.Changes, batch.BeforeETags,
                [ETag.Compute(ETagFormat.JSON, [1]), batch.AfterETags[1]])
        };
        var expected = Assert.Catch(() => OriginalCopy(source, source.Signatures, replacement))!;
        using var scope = scoped ? POICanonicalPreparation.Enter() : default;
        var actual = Assert.Catch(() => source.WithChangeSet(replacement))!;
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        if (scoped) Assert.That(POICanonicalPreparation.Current!.Entries, Is.Zero);
    }

    [Test]
    public void Canonical_equivalent_metadata_and_detached_operation_arrays_keep_the_full_fallback()
    {
        var source = Commit(1); var batch = source.ChangeSet!;
        var reordered = batch.WithMetadata("tokens", Value("{\"negativeZero\":-0, \"exponent\":1e0, \"decimal\":1.0, \"reading\":\"250 kW\", \"z\":null}"));
        using var scope = POICanonicalPreparation.Enter();
        Exact(source.WithChangeSet(reordered), OriginalCopy(source, source.Signatures, reordered));
        var parsed = JsonSerializer.Deserialize<RoamingNetworkChangeSet>(JsonSerializer.Serialize(batch))!;
        Exact(source.WithChangeSet(parsed), OriginalCopy(source, source.Signatures, parsed));
    }

    [Test, Combinatorial]
    public void Copies_preserve_signing_depth_errors([Values(58, 59, 60, 61)] Int32 depth, [Values(false, true)] Boolean scoped)
    {
        using var document = JsonDocument.Parse(new String('[', depth) + "1.0" + new String(']', depth), new() { MaxDepth = 2048 });
        var root = Commit(0); var batch = RichBatch().WithMetadata("deep", document.RootElement);
        var source = RoamingNetworkCommit.Create(root, batch);
        using var scope = scoped ? POICanonicalPreparation.Enter() : default;
        var copy = source.WithChangeSet(batch.WithSignatures([])).WithSignatures([]);
        Byte[]? expected = null; Exception? error = null;
        try { expected = CanonicalPreparationOracle.Signing(source, COSEAlgorithm.Ed25519, "fixture-alice"); }
        catch (Exception failure) { error = failure; }
        if (error is null) Assert.That(copy.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"), Is.EqualTo(expected));
        else
        {
            var actual = Assert.Catch(() => copy.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"))!;
            Assert.That(actual.GetType(), Is.EqualTo(error.GetType())); Assert.That(actual.Message, Is.EqualTo(error.Message));
        }
    }

    [TestCase("{\"x\":1,\"x\":2}")] [TestCase("{\"nested\":[{\"x\":1,\"x\":2}]}")]
    public void Invalid_new_operation_arrays_keep_preceding_validation(String text)
    {
        var source = Commit(1); var batch = source.ChangeSet!;
        var changed = new RoamingNetworkChangeSet(batch.Id, batch.RoamingNetworkId, batch.BaseRevision, batch.CreatedAt,
            [RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "customData", null, Value(text))], batch.BeforeETags, batch.AfterETags);
        var expected = Assert.Catch(() => OriginalCopy(source, [], changed))!;
        using var scope = POICanonicalPreparation.Enter();
        var actual = Assert.Catch(() => source.WithChangeSet(changed))!;
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        Assert.That(POICanonicalPreparation.Current!.Entries, Is.Zero);
    }

    [TestCase(false)] [TestCase(true)]
    public void Changed_declared_identity_is_rechecked_during_transport(Boolean cbor)
    {
        var copy = Sign(Commit(1)).WithSignatures([]);
        var value = System.Text.Json.Nodes.JsonNode.Parse(copy.ToJSON())!;
        value["Id"] = System.Text.Json.Nodes.JsonNode.Parse(JsonSerializer.Serialize(Commit(0).Id));
        using var scope = POICanonicalPreparation.Enter();
        if (!cbor) Assert.Throws<ArgumentException>(() => RoamingNetworkCommit.Parse(value.ToJsonString()));
        else
        {
            var original = CBORValue.Parse(copy.ToCBOR());
            var changed = CBORValue.FromMap(original.AsMap().Select(item => new KeyValuePair<CBORValue, CBORValue>(item.Key,
                RoamingNetworkCommit.Text(item.Key) == "Id" ? Commit(0).Id.ToCBOR() : item.Value)));
            var count = POICanonicalPreparation.Current!.Entries;
            Assert.Throws<ArgumentException>(() => RoamingNetworkCommit.ParseCBOR(changed.ToByteArray(CBORWriterOptions.Canonical)));
            Assert.That(POICanonicalPreparation.Current.Entries, Is.EqualTo(count));
        }
    }

    [Test, Combinatorial]
    public void All_archive_profiles_keep_original_copy_bytes_peers_and_fresh_recovery(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean cbor)
    {
        var name = new[] { "history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history" }[profile - 1];
        var bytes = File.ReadAllBytes(FilePath(name + ".cbor"));
        using var original = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        using (POICanonicalPreparation.Enter())
            foreach (var commit in original.Commits)
            {
                var copy = commit.WithSignatures(commit.Signatures);
                if (copy.ChangeSet is { } batch) copy = copy.WithChangeSet(batch.WithSignatures(batch.Signatures));
                Exact(copy, commit); Assert.That(copy.VerifySignatures(peer => PublicKey(peer.KeyId), out var error), Is.True, error);
            }
        var calls = 0;
        Boolean Trust(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer) { calls++; return VerifyCommit(commit, peer); }
        using var recovered = cbor ? RoamingNetworkHistory.ParseCBOR(original.ToCBOR(), VerifyBatch, Trust, authorizeSnapshotBoundary: _ => true) :
            RoamingNetworkHistory.Parse(original.ToJSON(), VerifyBatch, Trust, authorizeSnapshotBoundary: _ => true);
        Assert.That(calls, Is.GreaterThan(0)); Assert.That(recovered.ToCBOR(), Is.EqualTo(bytes));
        Assert.That(recovered.Head.Network, Is.Not.SameAs(original.Head.Network));
        Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    [Test]
    public void Copy_preparation_does_not_flow_to_other_threads()
    {
        using var scope = POICanonicalPreparation.Enter(); var source = Commit(1); var parent = POICanonicalPreparation.Current;
        Task.WaitAll(Enumerable.Range(0, 8).Select(reader => Task.Run(() => {
            Assert.That(POICanonicalPreparation.Current, Is.Null);
            using (POICanonicalPreparation.Enter())
            {
                var copy = source.WithSignatures([]); Assert.That(POICanonicalPreparation.Current!.Entries, Is.Zero);
                _ = copy.GetIdentityBytes(); Assert.That(POICanonicalPreparation.Current.Entries, Is.EqualTo(1));
            }
            Assert.That(POICanonicalPreparation.Current, Is.Null);
        })).ToArray());
        Assert.That(POICanonicalPreparation.Current, Is.SameAs(parent));
    }

    [Test, Combinatorial]
    public void Duplicate_delivery_and_pack_peer_unions_recheck_trust_and_reject_atomically(
        [Values(0, 1, 2)] Int32 delivery, [Values(false, true)] Boolean batchRejection)
    {
        var reject = false; var calls = new List<String>();
        using var history = new RoamingNetworkHistory(Network(),
            (batch, peer) => { calls.Add("batch:" + peer.KeyId); return (!reject || !batchRejection || peer.KeyId != "fixture-bob") && VerifyBatch(batch, peer); },
            (commit, peer) => { calls.Add("commit:" + peer.KeyId); return (!reject || batchRejection || peer.KeyId != "fixture-bob") && VerifyCommit(commit, peer); });
        var root = history.Head.Commit;
        var batch = Batch(history.Head.Snapshot, "duplicate-copy", Power("175 kW"));
        var source = history.PrepareCommit(root.Id, batch.Sign(Alice, "fixture-alice", COSEAlgorithm.Ed25519))
            .Sign(Alice, "fixture-alice", COSEAlgorithm.Ed25519);
        Assert.That(history.TryPublish(root.Id, source, out var stored), Is.True, stored.Error);
        var incoming = source.WithChangeSet(batch.Sign(Bob, "fixture-bob", COSEAlgorithm.Ed25519))
            .WithSignatures([]).Sign(Bob, "fixture-bob", COSEAlgorithm.Ed25519);
        Boolean Deliver() => delivery switch {
            0 => history.TryStoreCommit(incoming, out _), 1 => history.TryPublish(root.Id, incoming, out _),
            _ => history.TryImportCommitPack(new(root, incoming.Id, true, [incoming]), out _)
        };
        history.ApplyRuntimeUpdate(new(history.Head.Network.Id.ToString(), POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
            POIRuntimeStatusKind.Status, new("charging", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        var before = history.ToCBOR(); var head = history.Head; var snapshot = head.Snapshot;
        reject = true; calls.Clear(); Assert.That(Deliver(), Is.False);
        Assert.That(history.ToCBOR(), Is.EqualTo(before)); Assert.That(history.Head, Is.SameAs(head));
        Assert.That(calls, Does.Contain((batchRejection ? "batch:" : "commit:") + "fixture-bob"));
        reject = false; calls.Clear(); Assert.That(Deliver(), Is.True);
        Assert.That(calls, Does.Contain("commit:fixture-alice")); Assert.That(calls, Does.Contain("commit:fixture-bob"));
        Assert.That(calls, Does.Contain("batch:fixture-alice")); Assert.That(calls, Does.Contain("batch:fixture-bob"));
        Assert.That(history.Head.Commit.Signatures.Select(peer => peer.KeyId), Is.EqualTo(new[] { "fixture-alice", "fixture-bob" }));
        Assert.That(history.Head.Commit.ChangeSet!.Signatures.Select(peer => peer.KeyId), Is.EqualTo(new[] { "fixture-alice", "fixture-bob" }));
        Assert.That(history.Head.Snapshot, Is.SameAs(snapshot)); Assert.That(history.Head.Network, Is.SameAs(head.Network));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        var merged = history.ToCBOR(); Assert.That(Deliver(), Is.True); Assert.That(history.ToCBOR(), Is.EqualTo(merged));
        using var recovered = RoamingNetworkHistory.ParseCBOR(merged, VerifyBatch, VerifyCommit);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(merged)); Assert.That(recovered.Head.Network, Is.Not.SameAs(history.Head.Network));
        Assert.That(recovered.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
    }
}
