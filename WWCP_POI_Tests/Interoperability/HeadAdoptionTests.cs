/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class HeadAdoptionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Received_merge_is_previewed_then_adopted_over_its_second_parent_with_original_signatures_and_local_runtime(Boolean cbor)
    {
        using var sender = History(); using var receiver = History();
        var root = sender.Head.Id;
        var left = Prepare(sender, root, "left", Power("150 kW"));
        var right = Prepare(receiver, root, "right", Rename("Receiver"));
        Publish(sender, left); Publish(receiver, right); Store(sender, right);
        var merge = Merge(sender, left, right); Publish(sender, merge);
        Status(sender, EvseTarget, "available");
        Status(receiver, EvseTarget, "charging"); Status(receiver, PoolMeter, "error"); Status(receiver, StationMeter, "reserved");
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), merge.Id, out var outgoing, out var export,
            maxCommits: 1), Is.True, export.Error);
        while (true)
        {
            var page = Transport(outgoing!, cbor);
            Assert.That(receiver.TryImportCommitPack(page, out var import), Is.True, import.Error);
            if (page.Complete) break;
            Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), merge.Id, out outgoing, out export,
                maxCommits: 1), Is.True, export.Error);
        }
        Assert.That(receiver.TryPublish(right.Id, merge, out var publication), Is.False);
        Assert.That(publication.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.HeadConflict));
        var before = new Observation(receiver);
        Assert.That(receiver.TryAdoptHead(right.Id, merge.Id, out var preview), Is.True, preview.Error);
        Assert.That(preview.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.AdoptionAvailable));
        Assert.That(preview.RuntimeTransfer, Is.EqualTo(RoamingNetworkRuntimeTransfer.ConservativeBranchContinuity));
        before.AssertUnchanged(receiver);
        // Runtime delivered after preview must be captured by the actual adoption.
        Status(receiver, EvseTarget, "reserved", 3);
        var priorNetwork = receiver.Head.Network;
        var priorEVSE = priorNetwork.EVSEs.Single();
        priorEVSE.MaxPowerRealTime = new(InteropFixture.Time.AddDays(3), Watt.Parse("90 kW"));
        priorEVSE.MaxPowerPrognoses.Add(new Timestamped<Watt>(InteropFixture.Time.AddDays(4), Watt.Parse("80 kW")));
        var localStatus = priorEVSE.StatusSchedule().ToImmutableArray();
        var localAdmin = priorEVSE.AdminStatusSchedule().ToImmutableArray();
        var measurement = priorEVSE.MaxPowerRealTime;
        var forecast = priorEVSE.MaxPowerPrognoses.ToImmutableArray();
        var retainedCount = receiver.Commits.Length;
        Assert.That(receiver.TryAdoptHead(right.Id, merge.Id, out var adopted, adopt: true), Is.True, adopted.Error);
        Assert.That(adopted.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.Adopted));
        Assert.That(receiver.Head.Id, Is.EqualTo(sender.Head.Id));
        Assert.That(receiver.Head.Snapshot.ETags, Is.EqualTo(sender.Head.Snapshot.ETags));
        Assert.That(receiver.Head.Snapshot.Revision, Is.EqualTo(merge.Revision));
        Assert.That(receiver.Head.Snapshot.AppliedChangeSetId, Is.EqualTo(merge.AppliedChangeSetId));
        Assert.That(receiver.Commits, Has.Length.EqualTo(retainedCount));
        Assert.That(receiver.Head.Commit.GetIdentityBytes(), Is.EqualTo(merge.GetIdentityBytes()));
        Assert.That(receiver.Head.Commit.Signatures, Is.EqualTo(merge.Signatures));
        Assert.That(receiver.Head.Commit.ChangeSet!.Signatures, Is.EqualTo(merge.ChangeSet!.Signatures));
        // Mutating the old version before the new lazy hierarchy is read cannot alter captured values.
        priorEVSE.SetStatus(new Timestamped<EVSEStatusType>(InteropFixture.Time.AddDays(5), EVSEStatusType.OutOfService));
        priorEVSE.MaxPowerRealTime = new(InteropFixture.Time.AddDays(5), Watt.Parse("70 kW"));
        priorEVSE.MaxPowerPrognoses.Clear();
        var current = receiver.Head.Network.EVSEs.Single();
        Assert.That(current, Is.Not.SameAs(priorEVSE));
        Assert.That(current.StatusSchedule(), Is.EqualTo(localStatus));
        Assert.That(current.AdminStatusSchedule(), Is.EqualTo(localAdmin));
        Assert.That(current.MaxPowerRealTime, Is.EqualTo(measurement));
        Assert.That(current.MaxPowerPrognoses, Is.EquivalentTo(forecast));
        Assert.That(receiver.Head.Network.ChargingPools.Single().EnergyMeters.Single().Status.Value.ToString(), Is.EqualTo("error"));
        Assert.That(receiver.Head.Network.ChargingStations.Single().EnergyMeters.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
        Status(receiver, EvseTarget, "charging", 6);
        Assert.That(priorEVSE.Status.Value, Is.EqualTo(EVSEStatusType.OutOfService));
        Assert.That(receiver.TryAdoptHead(root, merge.Id, out adopted, adopt: true), Is.True);
        Assert.That(adopted.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.AlreadyCurrent));
        Assert.That(receiver.TryAdoptHead(merge.Id, right.Id, out adopted, adopt: true), Is.True);
        Assert.That(adopted.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.AlreadyIntegrated));
        Assert.That(receiver.Head.Id, Is.EqualTo(merge.Id));
        var next = Prepare(sender, merge.Id, "continue", Power("175 kW")); Publish(sender, next);
        Assert.That(receiver.TryPublish(merge.Id, next, out publication), Is.True, publication.Error);
        Assert.That(receiver.Head.Id, Is.EqualTo(sender.Head.Id));
        Assert.That(receiver.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
    }

    [TestCase("RecreatedMeter")]
    [TestCase("ClearedMeterSlot")]
    [TestCase("NewMeter")]
    [TestCase("RecreatedEVSE")]
    public void Secondary_parent_adoption_resets_unproved_lifetimes_and_keeps_unrelated_runtime(String kind)
    {
        using var history = History();
        var root = history.Head.Id; var source = history.Head.Snapshot;
        var meters = source.GetEntity(InfrastructureEntityType.ChargingPool, "DE*ABC*P1").Properties["energyMeters"];
        var meter = meters.EnumerateArray().Single();
        var operations = kind switch {
            "RecreatedMeter" => new[] {
                RoamingNetworkChange.RemoveElement("ChargingPool", "DE*ABC*P1", [new("energyMeters", "pool-meter")]),
                RoamingNetworkChange.AddElement("ChargingPool", "DE*ABC*P1", [new("energyMeters", "pool-meter")], meter)
            },
            "ClearedMeterSlot" => new[] {
                RoamingNetworkChange.UpdateProperty("ChargingPool", "DE*ABC*P1", "energyMeters", null, Value("[]")),
                RoamingNetworkChange.UpdateProperty("ChargingPool", "DE*ABC*P1", "energyMeters", null, meters)
            },
            "NewMeter" => new[] {
                RoamingNetworkChange.AddElement("ChargingPool", "DE*ABC*P1", [new("energyMeters", "new-meter")], Value("{\"id\":\"new-meter\",\"role\":\"pv\"}"))
            },
            _ => new[] {
                RoamingNetworkChange.Remove("EVSE", "DE*ABC*E1"),
                RoamingNetworkChange.Add("EVSE", "DE*ABC*E1", Value(source.GetEntityJSON(InfrastructureEntityType.EVSE, "DE*ABC*E1").ToString()),
                    "ChargingStation", "DE*ABC*S1")
            }
        };
        var left = Prepare(history, root, "left", Power("150 kW")); Store(history, left);
        var right = Prepare(history, root, "right", operations); Publish(history, right);
        Status(history, EvseTarget, "charging"); Status(history, PoolMeter, "error"); Status(history, StationMeter, "reserved");
        if (kind == "NewMeter") Status(history, POIRuntimeTarget.Meter(InfrastructureEntityType.ChargingPool, "DE*ABC*P1", "new-meter"), "error");
        var merge = Merge(history, left, right); Store(history, merge);
        var old = history.Head.Network;
        Assert.That(history.TryAdoptHead(right.Id, merge.Id, out var result, adopt: true), Is.True, result.Error);
        var current = history.Head.Network;
        var defaultRuntime = RoamingNetwork.Parse(history.GetSnapshot(merge.Id).ToJSON());
        Assert.That(current.ChargingStations.Single().EnergyMeters.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
        if (kind == "RecreatedEVSE")
        {
            Assert.That(current.EVSEs.Single().Status.Value, Is.EqualTo(defaultRuntime.EVSEs.Single().Status.Value));
            Assert.That(current.ChargingPools.Single().EnergyMeters.Single().Status.Value.ToString(), Is.EqualTo("error"));
        }
        else
        {
            Assert.That(current.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
            var id = kind == "NewMeter" ? "new-meter" : "pool-meter";
            Assert.That(current.ChargingPools.Single().EnergyMeters.Single(item => item.Id.ToString() == id).Status.Value,
                Is.EqualTo(defaultRuntime.ChargingPools.Single().EnergyMeters.Single(item => item.Id.ToString() == id).Status.Value));
            Assert.That(old.ChargingPools.Single().EnergyMeters.Single(item => item.Id.ToString() == id).Status.Value.ToString(), Is.EqualTo("error"));
            if (kind == "NewMeter")
                Assert.That(current.ChargingPools.Single().EnergyMeters.Single(item => item.Id.ToString() == "pool-meter").Status.Value.ToString(), Is.EqualTo("error"));
        }
        Assert.That(history.Head.Snapshot.ETags, Is.EqualTo(merge.StateETags));
    }

    [Test]
    public void Independently_added_meters_with_equal_ids_do_not_share_a_runtime_lifetime()
    {
        using var history = History(); var root = history.Head.Id;
        var add = RoamingNetworkChange.AddElement("ChargingPool", "DE*ABC*P1", [new("energyMeters", "same-new-id")],
            Value("{\"id\":\"same-new-id\",\"role\":\"pv\"}"));
        var left = Prepare(history, root, "left", add); Store(history, left);
        var right = Prepare(history, root, "right", add); Publish(history, right);
        Status(history, PoolMeter, "reserved");
        Status(history, POIRuntimeTarget.Meter(InfrastructureEntityType.ChargingPool, "DE*ABC*P1", "same-new-id"), "error");
        var merge = Merge(history, left, right); Store(history, merge);
        Assert.That(history.TryAdoptHead(right.Id, merge.Id, out var result, adopt: true), Is.True, result.Error);
        var meters = history.Head.Network.ChargingPools.Single().EnergyMeters;
        Assert.That(meters.Single(meter => meter.Id.ToString() == "pool-meter").Status.Value.ToString(), Is.EqualTo("reserved"));
        Assert.That(meters.Single(meter => meter.Id.ToString() == "same-new-id").Status.Value.ToString(), Is.Not.EqualTo("error"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Connection_point_children_keep_runtime_only_when_the_connection_point_identity_survives(Boolean replaceIdentity)
    {
        var json = Newtonsoft.Json.Linq.JObject.Parse(File.ReadAllText(FilePath("snapshot.input.json")));
        var point = json["chargingStationOperators"]![0]!["chargingPools"]![0]!["gridConnectionPoint"]!;
        point["id"] = "point-a";
        point["energyMeter"] = Newtonsoft.Json.Linq.JObject.Parse("{\"id\":\"connection-meter\",\"created\":\"2026-01-01T00:00:00Z\",\"lastChange\":\"2026-01-01T00:00:00Z\"}");
        using var history = History(RoamingNetwork.Parse(json)); var root = history.Head.Id;
        var left = Prepare(history, root, "left", Power("150 kW")); Store(history, left);
        var replacement = point.DeepClone(); replacement["id"] = "point-b";
        var edit = replaceIdentity ? RoamingNetworkChange.UpdateProperty("ChargingPool", "DE*ABC*P1", "gridConnectionPoint",
            null, Value(replacement.ToString())) :
            RoamingNetworkChange.UpdateElementProperty("ChargingPool", "DE*ABC*P1", [new("gridConnectionPoint", "point-a")], "nominalVoltage", null, Value("\"450 V\""));
        var right = Prepare(history, root, "right", edit); Publish(history, right);
        Status(history, POIRuntimeTarget.ConnectionMeter("DE*ABC*P1", "connection-meter"), "error");
        Status(history, POIRuntimeTarget.ConnectionGridOperator("DE*ABC*P1", "DE*GRD"), "Offline");
        Status(history, PoolMeter, "reserved");
        var merge = Merge(history, left, right); Store(history, merge);
        Assert.That(history.TryAdoptHead(right.Id, merge.Id, out var result, adopt: true), Is.True, result.Error);
        var current = history.Head.Network.ChargingPools.Single();
        Assert.That(current.EnergyMeters.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
        Assert.That(current.GridConnectionPoint!.EnergyMeter!.Status.Value.ToString() == "error", Is.EqualTo(!replaceIdentity));
        Assert.That(current.GridConnectionPoint.GridOperator.Status.Value == GridOperatorStatusTypes.Offline, Is.EqualTo(!replaceIdentity));
    }

    [Test]
    public void First_parent_fast_forward_replays_intermediate_remove_readd_lifetimes()
    {
        using var history = History(); var root = history.Head.Id;
        Status(history, EvseTarget, "charging"); Status(history, PoolMeter, "error"); Status(history, StationMeter, "reserved");
        var source = history.Head.Snapshot;
        var first = Prepare(history, root, "remove", RoamingNetworkChange.RemoveElement("ChargingPool", "DE*ABC*P1", [new("energyMeters", "pool-meter")]));
        Store(history, first);
        var meter = source.GetEntity(InfrastructureEntityType.ChargingPool, "DE*ABC*P1").Properties["energyMeters"].EnumerateArray().Single();
        var second = Prepare(history, first.Id, "restore", RoamingNetworkChange.AddElement("ChargingPool", "DE*ABC*P1", [new("energyMeters", "pool-meter")], meter));
        Store(history, second);
        var before = new Observation(history);
        Assert.That(history.TryAdoptHead(root, second.Id, out var preview), Is.True);
        Assert.That(preview.RuntimeTransfer, Is.EqualTo(RoamingNetworkRuntimeTransfer.FirstParentReplay));
        before.AssertUnchanged(history);
        Assert.That(history.TryAdoptHead(root, second.Id, out var result, adopt: true), Is.True, result.Error);
        Assert.That(history.Head.Network.ChargingPools.Single().EnergyMeters.Single().Status.Value.ToString(), Is.Not.EqualTo("error"));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        Assert.That(history.Head.Network.ChargingStations.Single().EnergyMeters.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
        Assert.That(history.Head.Snapshot.ETags, Is.EqualTo(second.StateETags));
    }

    [Test]
    public void Divergence_unknown_tip_and_stale_expected_head_never_change_history()
    {
        using var history = History(); var root = history.Head.Id;
        var left = Prepare(history, root, "left", Power("150 kW")); Publish(history, left);
        var right = Prepare(history, root, "right", Rename("Right")); Store(history, right);
        var next = Prepare(history, left.Id, "next", Power("175 kW")); Store(history, next);
        var before = new Observation(history);
        Assert.That(history.TryAdoptHead(left.Id, default, out var result, adopt: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.UnknownTip)); before.AssertUnchanged(history);
        Assert.That(history.TryAdoptHead(left.Id, right.Id, out result, adopt: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.Diverged)); before.AssertUnchanged(history);
        Assert.That(history.TryAdoptHead(root, next.Id, out result, adopt: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.HeadConflict)); before.AssertUnchanged(history);
        Assert.That(history.TryAdoptHead(left.Id, root, out result, adopt: true), Is.True);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.AlreadyIntegrated)); before.AssertUnchanged(history);
    }

    [Test]
    public void Secondary_parent_adoption_uses_ancestry_even_when_the_revision_number_decreases()
    {
        using var history = History(); var root = history.Head.Id;
        var left = Prepare(history, root, "left", Power("150 kW")); Store(history, left);
        var right = Prepare(history, root, "right", Rename("Right 1")); Publish(history, right);
        for (var i = 2; i <= 4; i++) { right = Prepare(history, right.Id, "right-" + i, Rename("Right " + i)); Publish(history, right); }
        var merge = Merge(history, left, right); Store(history, merge);
        Assert.That(history.Head.Snapshot.Revision, Is.EqualTo(4)); Assert.That(merge.Revision, Is.EqualTo(2));
        Status(history, EvseTarget, "charging");
        Assert.That(history.TryAdoptHead(right.Id, merge.Id, out var result, adopt: true), Is.True, result.Error);
        Assert.That(history.Head.Snapshot.Revision, Is.EqualTo(2)); Assert.That(history.Head.Id, Is.EqualTo(merge.Id));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
    }

    [Test]
    public void Adoption_rechecks_revoked_trust_and_rejects_reentrant_mutations()
    {
        var accepted = true; var probe = false;
        RoamingNetworkHistory? history = null;
        using var owner = new RoamingNetworkHistory(Network(), VerifyBatch, VerifyCommit, commit => {
            if (probe)
            {
                Assert.That(history!.TryAdoptHead(history.Head.Id, history.Head.Id, out var nested, adopt: true), Is.False);
                Assert.That(nested.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.Unavailable));
                Assert.That(history.TryImportCommitPack(new(history.Head.Commit, history.Head.Id, true, []), out var import), Is.False);
                Assert.That(import.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.Unavailable));
            }
            return accepted;
        });
        history = owner; var root = history.Head.Id;
        var target = Prepare(history, root, "target", Power("150 kW")); Store(history, target);
        var before = new Observation(history); accepted = false;
        Assert.That(history.TryAdoptHead(root, target.Id, out var result, adopt: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.InvalidInput)); before.AssertUnchanged(history);
        accepted = true; probe = true;
        Assert.That(history.TryAdoptHead(root, target.Id, out result, adopt: true), Is.True, result.Error);
        Assert.That(history.Head.Id, Is.EqualTo(target.Id));
    }

    [Test]
    public async Task Competing_adoptions_install_exactly_one_expected_head()
    {
        using var history = History(); var root = history.Head.Id;
        var left = Prepare(history, root, "left", Power("150 kW")); Store(history, left);
        var right = Prepare(history, root, "right", Power("175 kW")); Store(history, right);
        using var barrier = new Barrier(3);
        Task<RoamingNetworkHeadAdoptionResult> Adopt(RoamingNetworkCommit target) => Task.Run(() => {
            Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
            history.TryAdoptHead(root, target.Id, out var result, adopt: true); return result;
        });
        var first = Adopt(left); var second = Adopt(right);
        Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
        var results = await Task.WhenAll(first, second);
        Assert.That(results.Select(result => result.Outcome), Is.EquivalentTo(new[] { RoamingNetworkHeadAdoptionOutcome.Adopted, RoamingNetworkHeadAdoptionOutcome.HeadConflict }));
        Assert.That(history.Commits, Has.Length.EqualTo(3));
        Assert.That(history.Head.Snapshot.ETags, Is.EqualTo(history.Head.Commit.StateETags));
    }

    [Test]
    public void Revoking_an_additional_parent_blocks_adoption_of_an_otherwise_authorized_merge()
    {
        var allowRight = true;
        using var history = new RoamingNetworkHistory(Network(), VerifyBatch, VerifyCommit,
            commit => allowRight || commit.ChangeSet?.Id != "right");
        var root = history.Head.Id;
        var left = Prepare(history, root, "left", Power("150 kW")); Publish(history, left);
        var right = Prepare(history, root, "right", Rename("Right")); Store(history, right);
        var merge = Merge(history, left, right); Store(history, merge);
        Assert.That(history.TryAdoptHead(left.Id, merge.Id, out var preview), Is.True, preview.Error);
        var before = new Observation(history); allowRight = false;
        Assert.That(history.TryAdoptHead(left.Id, merge.Id, out var rejected, adopt: true), Is.False);
        Assert.That(rejected.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.InvalidInput));
        before.AssertUnchanged(history);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Runtime_delivery_waits_for_adoption_and_targets_the_new_head(Boolean secondaryParent)
    {
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var armed = false; RoamingNetworkCommitId targetId = default;
        using var history = new RoamingNetworkHistory(Network(), VerifyBatch, VerifyCommit, commit => {
            if (armed && commit.Id == targetId)
            {
                armed = false;
                entered.Set();
                if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Adoption gate was not released.");
            }
            return true;
        });
        var root = history.Head.Id;
        var left = Prepare(history, root, "left", Power("150 kW")); Store(history, left);
        var right = Prepare(history, root, "right", Rename("Right")); Publish(history, right);
        var target = secondaryParent ? Merge(history, left, right) : Prepare(history, right.Id, "forward", Power("175 kW"));
        Store(history, target); targetId = target.Id;
        Status(history, EvseTarget, "available");
        var old = history.Head.Network;
        armed = true;
        var adoption = Task.Run(() => { history.TryAdoptHead(right.Id, target.Id, out var result, adopt: true); return result; });
        Task? runtime = null;
        try
        {
            Assert.That(entered.Wait(TimeSpan.FromSeconds(10)), Is.True);
            var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            runtime = Task.Run(() => { started.SetResult(); Status(history, EvseTarget, "charging", 3); });
            await started.Task.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.That(runtime.IsCompleted, Is.False);
        }
        finally { release.Set(); }
        var adopted = await adoption.WaitAsync(TimeSpan.FromSeconds(10));
        if (runtime is not null) await runtime.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.That(adopted.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.Adopted), adopted.Error);
        Assert.That(history.Head.Id, Is.EqualTo(target.Id));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Timestamp, Is.EqualTo(InteropFixture.Time.AddDays(3)));
        Assert.That(old.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
        Assert.That(history.Head.Snapshot.ETags, Is.EqualTo(target.StateETags));
    }
}
