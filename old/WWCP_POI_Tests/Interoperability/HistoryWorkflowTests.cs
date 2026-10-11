/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class HistoryWorkflowTests
{
    private static RoamingNetworkRuntimeUpdate Runtime(String status) => new("interop-network",
        POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"), POIRuntimeStatusKind.Status,
        new(status, Time.AddDays(2)), mode: POIRuntimeUpdateMode.ReplaceHistory);

    [TestCase(false)]
    [TestCase(true)]
    public void Two_replicas_exchange_signed_branches_merge_reload_and_continue(Boolean cbor)
    {
        using var alice = History(); using var bob = History();
        alice.ApplyRuntimeUpdate(Runtime("available")); bob.ApplyRuntimeUpdate(Runtime("charging"));
        var checkpoint = alice.Head.Id;
        Assert.That(bob.Head.Id, Is.EqualTo(checkpoint));
        Assert.That(bob.Head.Snapshot.ETags, Is.EqualTo(alice.Head.Snapshot.ETags));
        Assert.That(bob.Head.Network.EVSEs.Single().Status.Value, Is.Not.EqualTo(alice.Head.Network.EVSEs.Single().Status.Value));
        var left = Sign(alice.PrepareCommit(checkpoint, Sign(Batch(alice.Head.Snapshot, "alice", Power("150 kW")))));
        var right = Sign(bob.PrepareCommit(checkpoint, Sign(Batch(bob.Head.Snapshot, "bob",
            RoamingNetworkChange.UpdateProperty("ChargingStation", "DE*ABC*S1", "name", null, Value("{\"en\":\"Renamed\"}"))))));
        Assert.That(alice.TryPublish(checkpoint, left, out var result), Is.True, result.Error);
        Assert.That(bob.TryPublish(checkpoint, right, out result), Is.True, result.Error);
        var receivedRight = cbor ? RoamingNetworkCommit.ParseCBOR(right.ToCBOR()) : RoamingNetworkCommit.Parse(right.ToJSON());
        Assert.That(alice.TryStoreCommit(receivedRight, out result), Is.True, result.Error);
        Assert.That(bob.TryStoreCommit(left, out result), Is.True, result.Error);
        Assert.That(alice.TryMerge(left.Id, right.Id, out var preview, out var report), Is.True, report.Message);
        Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.MergeAvailable));
        Assert.That(preview, Is.Null); Assert.That(alice.Head.Id, Is.EqualTo(left.Id));
        Assert.That(alice.TryMerge(left.Id, right.Id, out var merge, out report, merge: true,
            mergedChangeSetId: "merged", createdAt: Time.AddDays(1)), Is.True, report.Message);
        Assert.That(merge!.Signatures, Is.Empty); Assert.That(merge.ChangeSet!.Signatures, Is.Empty);
        Assert.That(merge.Parents, Is.EqualTo(new[] {left.Id, right.Id}));
        var signedMerge = Sign(merge.WithChangeSet(Sign(merge.ChangeSet)));
        Assert.That(alice.TryPublish(left.Id, signedMerge, out result), Is.True, result.Error);
        Assert.That(alice.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
        Assert.That(bob.TryStoreCommit(signedMerge, out result), Is.True, result.Error);
        // A remote integration against left cannot be directly published over right.
        Assert.That(bob.TryPublish(right.Id, signedMerge, out result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.HeadConflict));
        Assert.That(bob.Head.Id, Is.EqualTo(right.Id));
        // Explicit full archive bootstrap selects the published remote integration head.
        using var restored = cbor ? RoamingNetworkHistory.ParseCBOR(alice.ToCBOR(), VerifyBatch, VerifyCommit) :
                                    RoamingNetworkHistory.Parse(alice.ToJSON(), VerifyBatch, VerifyCommit);
        Assert.That(restored.Head.Id, Is.EqualTo(alice.Head.Id));
        Assert.That(restored.Head.Snapshot.ETags, Is.EqualTo(alice.Head.Snapshot.ETags));
        Assert.That(restored.Commits.Select(commit => commit.Id), Is.EquivalentTo(alice.Commits.Select(commit => commit.Id)));
        restored.ApplyRuntimeUpdate(Runtime("charging"));
        Assert.That(restored.Head.Id, Is.EqualTo(alice.Head.Id));
        var next = Sign(alice.PrepareCommit(alice.Head.Id, Sign(Batch(alice.Head.Snapshot, "after-reload", Power("175 kW")))));
        Assert.That(alice.TryPublish(alice.Head.Id, next, out result), Is.True, result.Error);
        Assert.That(restored.TryPublish(restored.Head.Id, RoamingNetworkCommit.ParseCBOR(next.ToCBOR()), out result), Is.True, result.Error);
        Assert.That(restored.Head.Id, Is.EqualTo(alice.Head.Id));
        Assert.That(restored.TryPublish(checkpoint, signedMerge, out result), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.AlreadyPublished));
        Assert.That(restored.Head.Id, Is.EqualTo(next.Id));
        Assert.That(restored.Head.Snapshot.Revision, Is.EqualTo(3));
    }

    [Test]
    public void Conflicting_writes_require_a_choice_and_record_signed_audit_metadata()
    {
        using var history = History(); var root = history.Head.Id;
        var left = history.PrepareCommit(root, Batch(history.Head.Snapshot, "left", Power("150 kW")));
        var right = history.PrepareCommit(root, Batch(history.Head.Snapshot, "right", Power("200 kW")));
        Assert.That(history.TryStoreCommit(left, out var result), Is.True, result.Error);
        Assert.That(history.TryStoreCommit(right, out result), Is.True, result.Error);
        Assert.That(history.TryMerge(left.Id, right.Id, out var merge, out var report, merge: true, mergedChangeSetId: "merge"), Is.False);
        Assert.That(merge, Is.Null); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.Conflicts));
        var conflict = report.Conflicts.Single();
        Assert.That(conflict.BaseValue!.Value.GetString(), Is.EqualTo("100 kW"));
        Assert.That(conflict.LeftValue!.Value.GetString(), Is.EqualTo("150 kW"));
        Assert.That(conflict.RightValue!.Value.GetString(), Is.EqualTo("200 kW"));
        Assert.That(history.TryMerge(left.Id, right.Id, out merge, out report, merge: true, mergedChangeSetId: "resolved",
            createdAt: Time.AddDays(1), resolveConflict: _ => RoamingNetworkMergeResolution.Custom(Value("\"175000 W\""))), Is.True, report.Message);
        var batch = merge!.ChangeSet!;
        var audit = batch.Metadata[RoamingNetworkHistory.MergeMetadataProperty];
        Assert.That(audit.GetProperty("Resolutions")[0].GetProperty("Choice").GetString(), Is.EqualTo("Custom"));
        Assert.That(audit.GetProperty("Resolutions")[0].GetProperty("Value").GetString(), Is.EqualTo("175000 W"));
        Assert.That(audit.GetProperty("Resolutions")[0].GetProperty("Sequence").GetInt32(), Is.Zero);
        Assert.That(history.TryStoreCommit(Sign(merge.WithChangeSet(Sign(batch))), out result), Is.True, result.Error);
        Assert.That(history.GetSnapshot(merge.Id).GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties["maxPower"].GetString(), Is.EqualTo("175 kW"));
        Assert.That(history.Head.Id, Is.EqualTo(root));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Property_absence_and_JSON_null_are_distinct_merge_choices(Boolean remove)
    {
        using var history = History(); var root = history.Head.Id;
        var operation = remove ? RoamingNetworkChange.RemoveProperty("EVSE", "DE*ABC*E1", "physicalReference", Value("null")) :
                                 RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "physicalReference", Value("null"), Value("\"left\""));
        var left = history.PrepareCommit(root, Batch(history.Head.Snapshot, "left", operation));
        var right = history.PrepareCommit(root, Batch(history.Head.Snapshot, "right",
            RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "physicalReference", Value("null"), Value("\"right\""))));
        history.TryStoreCommit(left, out _); history.TryStoreCommit(right, out _);
        Assert.That(history.TryMerge(left.Id, right.Id, out var merged, out var report, merge: true, mergedChangeSetId: "resolved",
            resolveConflict: _ => remove ? RoamingNetworkMergeResolution.UseLeft : RoamingNetworkMergeResolution.UseBase), Is.True, report.Message);
        Assert.That(history.TryStoreCommit(merged!, out var result), Is.True, result.Error);
        var properties = history.GetSnapshot(merged!.Id).GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E1").Properties;
        Assert.That(properties.ContainsKey("physicalReference"), Is.EqualTo(!remove));
        if (!remove) Assert.That(properties["physicalReference"].ValueKind, Is.EqualTo(JsonValueKind.Null));
    }

    [Test]
    public void Disjoint_identified_meter_edits_merge_and_export_both_results()
    {
        using var history = History(); var root = history.Head.Id;
        var left = history.PrepareCommit(root, Batch(history.Head.Snapshot, "pool-meter",
            RoamingNetworkChange.UpdateElementProperty("ChargingPool", "DE*ABC*P1", [new("energyMeters", "pool-meter")], "description", null, Value("{\"en\":\"Grid meter\"}"))));
        var right = history.PrepareCommit(root, Batch(history.Head.Snapshot, "station-meter",
            RoamingNetworkChange.UpdateElementProperty("ChargingStation", "DE*ABC*S1", [new("energyMeters", "station-meter")], "description", null, Value("{\"en\":\"PV meter\"}"))));
        history.TryStoreCommit(left, out _); history.TryStoreCommit(right, out _);
        Assert.That(history.TryMerge(left.Id, right.Id, out var merge, out var report, merge: true, mergedChangeSetId: "meters"), Is.True, report.Message);
        Assert.That(history.TryStoreCommit(merge!, out var result), Is.True, result.Error);
        var json = history.GetSnapshot(merge!.Id).ToJSON().ToString();
        Assert.That(json, Does.Contain("Grid meter").And.Contain("PV meter"));
    }

    [Test]
    public void Missing_parent_conflicting_batch_id_and_signature_policy_are_enforced()
    {
        using var history = History(); var root = history.Head.Id;
        var left = Sign(history.PrepareCommit(root, Sign(Batch(history.Head.Snapshot, "same-id", Power("150 kW")))));
        var right = Sign(history.PrepareCommit(root, Sign(Batch(history.Head.Snapshot, "same-id", Power("200 kW")))));
        var next = RoamingNetworkCommit.Create(left, Batch(history.Head.Snapshot.ApplyChangeSet(left.ChangeSet!, VerifyBatch), "later", Power("175 kW")));
        Assert.That(history.TryStoreCommit(next, out var result), Is.False); Assert.That(result.Error, Does.Contain("Missing parent"));
        Assert.That(history.TryStoreCommit(left, out result), Is.True, result.Error);
        Assert.That(history.TryStoreCommit(right, out result), Is.False); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.ChangeSetIdConflict));
        using var unsignedOnly = new RoamingNetworkHistory(Network());
        Assert.That(unsignedOnly.TryStoreCommit(left, out result), Is.False); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.InvalidCommit));
        using var requireTwo = new RoamingNetworkHistory(Network(), VerifyBatch, VerifyCommit,
            commit => commit.Signatures.Select(signature => signature.KeyId).Distinct().Count() >= 2);
        Assert.That(requireTwo.TryStoreCommit(left.WithSignatures([left.Signatures[0]]), out result), Is.False);
        Assert.That(requireTwo.TryStoreCommit(left, out result), Is.True, result.Error);
        var tampered = left.WithSignature(new("Ed25519", "fixture-alice", Convert.ToBase64String(new Byte[64]), RoamingNetworkCommit.SigningProfile));
        Assert.That(history.TryStoreCommit(tampered, out result), Is.False);
    }

    [Test]
    public async Task Competing_publications_install_exactly_one_expected_head()
    {
        using var history = History(); var root = history.Head.Id;
        var left = history.PrepareCommit(root, Batch(history.Head.Snapshot, "race-left", Power("150 kW")));
        var right = history.PrepareCommit(root, Batch(history.Head.Snapshot, "race-right", Power("200 kW")));
        using var barrier = new Barrier(3);
        Task<RoamingNetworkHistoryResult> Publish(RoamingNetworkCommit commit) => Task.Run(() => {
            Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
            history.TryPublish(root, commit, out var result); return result;
        });
        var first = Publish(left); var second = Publish(right);
        Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
        var results = await Task.WhenAll(first, second);
        Assert.That(results.Select(result => result.Outcome), Is.EquivalentTo(new[] {RoamingNetworkHistoryOutcome.Published, RoamingNetworkHistoryOutcome.HeadConflict}));
        Assert.That(history.Head.Snapshot.Revision, Is.EqualTo(1)); Assert.That(history.Commits, Has.Length.EqualTo(2));
    }

    [Test]
    public void Corrupt_archive_identity_or_checkpoint_is_rejected()
    {
        using var history = History();
        var commit = history.PrepareCommit(history.Head.Id, Batch(history.Head.Snapshot, "archived", Power("150 kW")));
        history.TryPublish(history.Head.Id, commit, out _);
        var archive = JObject.Parse(history.ToJSON()); archive["Checkpoint"]!["name"] = new JObject(new JProperty("en", "tampered"));
        Assert.That(() => RoamingNetworkHistory.Parse(archive.ToString()), Throws.ArgumentException);
        archive = JObject.Parse(history.ToJSON()); archive["Commits"]![0]!["Revision"] = 99;
        Assert.That(() => RoamingNetworkHistory.Parse(archive.ToString()), Throws.ArgumentException);
    }
}
