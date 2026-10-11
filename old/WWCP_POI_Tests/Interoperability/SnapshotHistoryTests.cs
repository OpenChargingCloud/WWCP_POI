/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class SnapshotHistoryTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Snapshot_preparation_shares_static_maps_and_advances_only_history_bookkeeping(Boolean afterBatch)
    {
        using var history = History();
        if (afterBatch) Publish(history, Prepare(history, history.Head.Id, "before", Power("150 kW")));
        var before = history.Head; var observation = new Observation(history);
        var snapshot = history.PrepareSnapshot(before.Id, InteropFixture.Time.AddDays(10));
        observation.AssertUnchanged(history);
        Assert.That(snapshot.Kind, Is.EqualTo(RoamingNetworkCommitKind.Snapshot));
        Assert.That(snapshot.Profile, Is.EqualTo(RoamingNetworkCommit.SnapshotIdentityProfile));
        Assert.That(snapshot.SignatureProfile, Is.EqualTo(RoamingNetworkCommit.SnapshotSigningProfile));
        Assert.That(snapshot.ChangeSet, Is.Null); Assert.That(snapshot.Parents, Is.EqualTo(new[] { before.Id }));
        Assert.That(snapshot.Revision, Is.EqualTo(before.Snapshot.Revision + 1));
        Assert.That(snapshot.AppliedChangeSetId, Is.EqualTo(before.Snapshot.AppliedChangeSetId));
        Assert.That(snapshot.StateETags, Is.EqualTo(before.Snapshot.ETags));
        Assert.That(snapshot.Snapshot!.State.ToCanonicalJSON(), Is.EqualTo(before.Snapshot.ToCanonicalJSON()));
        Assert.That(snapshot.Snapshot.State.Entities, Is.SameAs(before.Snapshot.Entities));
        Assert.That(snapshot.Snapshot.State.References, Is.SameAs(before.Snapshot.References));
        Assert.That(snapshot.Signatures, Is.Empty);
        Assert.That(snapshot.Id.Hash.Digest.ToArray(), Is.EqualTo(SHA256.HashData(snapshot.GetIdentityBytes())));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Snapshot_transport_preserves_native_state_metadata_number_spelling_and_peer_signatures(Boolean cbor)
    {
        using var history = History(); var snapshot = Snapshot(history);
        var decoded = CommitTransport(snapshot, cbor);
        Assert.That(decoded.Id, Is.EqualTo(snapshot.Id));
        Assert.That(decoded.ToCBOR(), Is.EqualTo(snapshot.ToCBOR()));
        Assert.That(decoded.GetIdentityBytes(), Is.EqualTo(snapshot.GetIdentityBytes()));
        Assert.That(CanonicalJSON.Serialize(decoded.Snapshot!.Metadata["case"]),
            Is.EqualTo(CanonicalJSON.Serialize(snapshot.Snapshot!.Metadata["case"])));
        foreach (var field in new[] { "decimal", "exponent", "negativeZero" })
            Assert.That(decoded.Snapshot.Metadata["case"].GetProperty(field).GetRawText(),
                Is.EqualTo(snapshot.Snapshot.Metadata["case"].GetProperty(field).GetRawText()));
        Assert.That(decoded.Signatures, Has.Length.EqualTo(2));
        Assert.That(decoded.VerifySignatures(signature => PublicKey(signature.KeyId), out var error), Is.True, error);
        Assert.That(decoded.WithSignatures([]).Id, Is.EqualTo(decoded.Id));
        Assert.That(decoded.WithSignatures([]).GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice"),
            Is.EqualTo(snapshot.GetSigningBytes(COSEAlgorithm.Ed25519, "fixture-alice")));
        var map = CBORValue.Parse(snapshot.ToCBOR()).AsMap();
        var payload = map.Single(pair => pair.Key.AsText() == "Snapshot").Value;
        Assert.That(payload.AsMap().Single(pair => pair.Key.AsText() == "State").Value.Kind, Is.EqualTo(CBORValueKind.Map));
        Publish(history, decoded);
    }

    [Test]
    public void Snapshot_metadata_is_detached_and_timestamp_offsets_normalize_without_changing_identity()
    {
        using var history = History();
        using var metadata = JsonDocument.Parse("{\"case\":1.0}");
        var data = ImmutableDictionary<String, JsonElement>.Empty.Add("task", metadata.RootElement);
        var offset = InteropFixture.Time.ToOffset(TimeSpan.FromHours(2));
        var first = history.PrepareSnapshot(history.Head.Id, offset, metadata: data);
        metadata.Dispose();
        var second = history.PrepareSnapshot(history.Head.Id, InteropFixture.Time,
            metadata: ImmutableDictionary<String, JsonElement>.Empty.Add("task", Value("{\"case\":1.0}")));
        Assert.That(first.Id, Is.EqualTo(second.Id));
        Assert.That(first.Snapshot!.CreatedAt.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(first.Snapshot.Metadata["task"].GetProperty("case").GetRawText(), Is.EqualTo("1.0"));
        Assert.That(history.PrepareSnapshot(history.Head.Id, InteropFixture.Time.AddTicks(1), metadata: second.Snapshot!.Metadata).Id,
            Is.Not.EqualTo(first.Id));
    }

    [TestCase("Profile")]
    [TestCase("Description")]
    [TestCase("Revision")]
    [TestCase("Parent")]
    [TestCase("UnknownField")]
    public void Snapshot_JSON_rejects_changed_identity_content_and_unknown_fields(String kind)
    {
        using var history = History(); var snapshot = Snapshot(history); var json = JsonNode.Parse(snapshot.ToJSON())!;
        if (kind == "Profile") json["Profile"] = RoamingNetworkCommit.IdentityProfile;
        if (kind == "Description") json["Snapshot"]!["Description"]!["en"] = "Altered";
        if (kind == "Revision") json["Revision"] = snapshot.Revision + 1;
        if (kind == "Parent") json["Parents"]![0] = JsonNode.Parse(System.Text.Json.JsonSerializer.Serialize(snapshot.Id));
        if (kind == "UnknownField") json["Extra"] = true;
        Assert.That(() => RoamingNetworkCommit.Parse(json.ToJsonString()), Throws.Exception);
    }

    [Test]
    public void Snapshot_signatures_do_not_authorize_modified_metadata_or_a_wrong_signature_profile()
    {
        using var history = History(); var original = Snapshot(history);
        var changed = history.PrepareSnapshot(history.Head.Id, original.CreatedAt!.Value,
            original.Snapshot!.Description, original.Snapshot.Metadata.SetItem("case", Value("{\"decimal\":2.0}")))
            .WithSignatures(original.Signatures);
        var before = new Observation(history);
        Assert.That(history.TryPublish(history.Head.Id, changed, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.InvalidCommit)); before.AssertUnchanged(history);
        var signature = original.Signatures[0];
        var invalid = original.WithSignatures([new(signature.Algorithm, signature.KeyId, signature.Value, RoamingNetworkCommit.SigningProfile)]);
        Assert.That(history.TryPublish(history.Head.Id, invalid, out result), Is.False); before.AssertUnchanged(history);
    }

    [Test]
    public void Snapshot_publication_captures_independent_runtime_and_the_next_batch_uses_the_new_revision()
    {
        using var history = History(); var before = history.Head; var priorEVSE = before.Network.EVSEs.Single();
        var snapshot = Snapshot(history);
        Status(history, EvseTarget, "charging"); Status(history, PoolMeter, "error");
        priorEVSE.MaxPowerRealTime = new(InteropFixture.Time.AddDays(2), Watt.Parse("75 kW"));
        priorEVSE.MaxPowerPrognoses.Add(new Timestamped<Watt>(InteropFixture.Time.AddDays(3), Watt.Parse("50 kW")));
        var statuses = priorEVSE.StatusSchedule().ToArray();
        Publish(history, snapshot); var current = history.Head.Network.EVSEs.Single();
        Assert.That(current, Is.Not.SameAs(priorEVSE));
        Assert.That(current.StatusSchedule(), Is.EqualTo(statuses));
        Assert.That(current.MaxPowerRealTime, Is.EqualTo(priorEVSE.MaxPowerRealTime));
        Assert.That(current.MaxPowerPrognoses, Is.EquivalentTo(priorEVSE.MaxPowerPrognoses));
        Assert.That(history.Head.Network.ChargingPools.Single().EnergyMeters.Single().Status.Value.ToString(), Is.EqualTo("error"));
        Status(history, EvseTarget, "reserved", 4);
        Assert.That(priorEVSE.Status.Value.ToString(), Is.EqualTo("charging"));
        Assert.That(snapshot.ToJSON(), Does.Not.Contain("MaxPowerRealTime"));
        Publish(history, Prepare(history, history.Head.Id, "next", Power("175 kW")));
        Assert.That(history.Head.Snapshot.Revision, Is.EqualTo(snapshot.Revision + 1));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Complete_archives_replay_snapshots_and_keep_original_signed_branches(Boolean cbor)
    {
        using var history = History(); var root = history.Head.Id; LinearHistory(history);
        var hidden = Prepare(history, root, "hidden", Rename("Unpublished")); Store(history, hidden);
        using var restored = Restore(history, cbor);
        Assert.That(restored.HasCompleteAncestry, Is.True);
        Assert.That(restored.ToCBOR(), Is.EqualTo(history.ToCBOR()));
        Assert.That(restored.Head.Id, Is.EqualTo(history.Head.Id));
        Assert.That(restored.GetCommit(hidden.Id).Signatures, Has.Length.EqualTo(2));
        Publish(restored, Prepare(restored, restored.Head.Id, "continued", Power("200 kW")));
    }

    [Test]
    public void Competing_snapshots_obey_expected_head_and_stale_preparation_does_not_mutate()
    {
        using var history = History(); var root = history.Head.Id;
        var first = Snapshot(history, 10); var second = Snapshot(history, 11);
        Publish(history, first); var before = new Observation(history);
        Assert.That(history.TryPublish(root, second, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.HeadConflict)); before.AssertUnchanged(history);
        Assert.That(() => history.PrepareSnapshot(root, InteropFixture.Time), Throws.ArgumentException); before.AssertUnchanged(history);
        Assert.That(history.TryPublish(root, first, out result), Is.True);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.AlreadyPublished));
    }

    [TestCase("BeforeTemporaryWrite")]
    [TestCase("TemporaryFileFlushed")]
    public void Snapshot_write_failures_preserve_disk_memory_and_runtime_then_retry(String stage)
    {
        using var directory = new ArchiveDirectory();
        using var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit);
        var snapshot = Snapshot(history); Status(history, EvseTarget, "charging");
        var before = new Observation(history); var bytes = File.ReadAllBytes(directory.ArchivePath);
        history.ArchiveWriteObserver = reached => { if (reached.ToString() == stage) throw new IOException("Injected snapshot failure"); };
        Assert.That(history.TryPublish(history.Head.Id, snapshot, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.PersistenceFailure)); before.AssertUnchanged(history);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(bytes));
        Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        history.ArchiveWriteObserver = null; Publish(history, snapshot);
        history.Dispose(); using var recovered = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit);
        Assert.That(recovered.Head.Id, Is.EqualTo(snapshot.Id));
    }

    [Test]
    public void Snapshot_factory_rejects_mismatched_source_and_revision_overflow()
    {
        using var history = History(); var other = history.Head.Snapshot.ApplyChangeSet(Batch(history.Head.Snapshot, "other", Power("150 kW")));
        Assert.That(() => RoamingNetworkCommit.CreateSnapshot(history.Head.Commit, other, InteropFixture.Time), Throws.ArgumentException);
        var json = Newtonsoft.Json.Linq.JObject.Parse(history.Head.Snapshot.ToJSON().ToString());
        json["revision"] = Int64.MaxValue;
        json.Remove("ETags");
        var state = RoamingNetworkDataSnapshot.Parse(json.ToString()); var parent = RoamingNetworkCommit.CreateCheckpoint(state);
        Assert.That(() => RoamingNetworkCommit.CreateSnapshot(parent, state, InteropFixture.Time), Throws.TypeOf<OverflowException>());
    }

    [Test]
    public async Task Concurrent_snapshot_publications_accept_one_expected_head_and_keep_delivered_runtime()
    {
        using var history = History(); var root = history.Head.Id;
        var first = Snapshot(history, 10); var second = Snapshot(history, 11);
        Status(history, EvseTarget, "charging"); using var barrier = new Barrier(3);
        Task<RoamingNetworkHistoryResult> Run(RoamingNetworkCommit commit) => Task.Run(() => {
            Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
            history.TryPublish(root, commit, out var result); return result;
        });
        var a = Run(first); var b = Run(second);
        Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
        var results = await Task.WhenAll(a, b);
        Assert.That(results.Count(result => result.Outcome == RoamingNetworkHistoryOutcome.Published), Is.EqualTo(1));
        Assert.That(results.Count(result => result.Outcome == RoamingNetworkHistoryOutcome.HeadConflict), Is.EqualTo(1));
        Assert.That(history.Commits, Has.Length.EqualTo(2));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
    }
}
