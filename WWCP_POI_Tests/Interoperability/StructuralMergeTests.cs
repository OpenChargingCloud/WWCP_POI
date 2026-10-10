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
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class StructuralMergeTests
{
    private const String Pool = "DE*ABC*P1";
    private const String Station = "DE*ABC*S1";
    private const String Evse = "DE*ABC*E1";
    private const String Tariff1 = "DE*ABC*T1";
    private const String Tariff2 = "DE*ABC*T2";

    private static JsonElement Document(JObject json) => Value(json.ToString(Newtonsoft.Json.Formatting.None));
    private static RoamingNetworkMergeResolution Choice(String value) => value switch {
        "Base" => RoamingNetworkMergeResolution.UseBase, "Left" => RoamingNetworkMergeResolution.UseLeft,
        "Right" => RoamingNetworkMergeResolution.UseRight, "Remove" => RoamingNetworkMergeResolution.Remove,
        _ => throw new ArgumentException(value)
    };

    private static RoamingNetworkCommit Resolved(RoamingNetworkHistory history, RoamingNetworkCommit left, RoamingNetworkCommit right,
        Func<RoamingNetworkMergeConflict, RoamingNetworkMergeResolution?> resolver, out RoamingNetworkMergeResult report, String id = "resolved")
    {
        Assert.That(history.TryMerge(left.Id, right.Id, out var merged, out report, merge: true,
            mergedChangeSetId: id, createdAt: InteropFixture.Time.AddDays(1), resolveConflict: resolver), Is.True, report.Message);
        Assert.That(merged!.Signatures, Is.Empty); Assert.That(merged.ChangeSet!.Signatures, Is.Empty);
        return Sign(merged.WithChangeSet(Sign(merged.ChangeSet)));
    }

    [TestCase("ChargingPool", Pool, false)]
    [TestCase("ChargingPool", Pool, true)]
    [TestCase("ChargingStation", Station, false)]
    [TestCase("ChargingStation", Station, true)]
    [TestCase("EVSE", Evse, false)]
    [TestCase("EVSE", Evse, true)]
    public void Deleting_an_owner_conflicts_with_a_descendant_edit_once_at_the_owner(String type, String id, Boolean swap)
    {
        using var history = History(); var root = history.Head.Id;
        var removed = Prepare(history, root, "removed", RoamingNetworkChange.Remove(type, id));
        var edited = Prepare(history, root, "edited", Power("150 kW"));
        var left = swap ? edited : removed; var right = swap ? removed : edited;
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out var absent, out var report, merge: true, mergedChangeSetId: "merge"), Is.False);
        Assert.That(absent, Is.Null); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.Conflicts));
        var conflict = report.Conflicts.Single(); Assert.That(conflict.Kind, Is.EqualTo(RoamingNetworkMergeConflictKind.DeleteModify));
        Assert.That(conflict.Entity, Is.EqualTo(new InfrastructureEntityKey(Enum.Parse<InfrastructureEntityType>(type), id)));
        Assert.That(conflict.PropertyName, Is.Null); Assert.That(conflict.ElementPath, Is.Empty); Assert.That(conflict.RelatedEntity, Is.Null);
        Assert.That(conflict.BaseValue!.Value.GetProperty("Document").ValueKind, Is.EqualTo(JsonValueKind.Object));
        Assert.That(swap ? conflict.RightValue : conflict.LeftValue, Is.Null);
        Assert.That(report.AfterETags, Is.Empty); before.AssertUnchanged(history);
    }

    [TestCase("Base")]
    [TestCase("Left")]
    [TestCase("Right")]
    [TestCase("Remove")]
    public void A_delete_modify_resolution_selects_the_whole_subtree_and_preserves_signed_audit(String choice)
    {
        using var history = History(); var root = history.Head.Id;
        var left = Prepare(history, root, "remove", RoamingNetworkChange.Remove("ChargingPool", Pool));
        var right = Prepare(history, root, "edit", Power("150 kW")); Publish(history, left); Store(history, right);
        var before = new Observation(history);
        var merged = Resolved(history, left, right, _ => Choice(choice), out var report);
        before.AssertUnchanged(history); Assert.That(report.Conflicts.Single().Resolution!.Choice.ToString(), Is.EqualTo(choice));
        Publish(history, merged);
        var hasPool = choice is "Base" or "Right";
        Assert.That(history.Head.Snapshot.Entities.Keys.Any(key => key.Type == InfrastructureEntityType.ChargingPool), Is.EqualTo(hasPool));
        if (hasPool) Assert.That(history.Head.Snapshot.GetEntity(InfrastructureEntityType.EVSE, Evse).Properties["maxPower"].GetString(),
            Is.EqualTo(choice == "Base" ? "100 kW" : "150 kW"));
        var audit = merged.ChangeSet!.Metadata[RoamingNetworkHistory.MergeMetadataProperty].GetProperty("Resolutions");
        Assert.That(audit.GetArrayLength(), Is.EqualTo(1)); Assert.That(audit[0].GetProperty("Kind").GetString(), Is.EqualTo("DeleteModify"));
        using var recovered = RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit);
        Assert.That(recovered.Head.Id, Is.EqualTo(merged.Id)); Assert.That(recovered.Head.Snapshot.ETags, Is.EqualTo(report.AfterETags));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Nested_meter_deletion_and_recreation_have_distinct_structural_conflicts(Boolean recreate)
    {
        using var history = History(); var root = history.Head.Id;
        var meter = JObject.Parse(history.Head.Snapshot.GetEntity(InfrastructureEntityType.ChargingPool, Pool).Properties["energyMeters"].EnumerateArray().Single().GetRawText());
        meter["created"] = InteropFixture.Time.ToString("O"); meter["role"] = "storage";
        var changes = new List<RoamingNetworkChange> { RoamingNetworkChange.RemoveElement("ChargingPool", Pool, [new("energyMeters", "pool-meter")]) };
        if (recreate) changes.Add(RoamingNetworkChange.AddElement("ChargingPool", Pool, [new("energyMeters", "pool-meter")], Document(meter)));
        var left = Prepare(history, root, "left", [.. changes]);
        var right = Prepare(history, root, "right", RoamingNetworkChange.UpdateElementProperty("ChargingPool", Pool,
            [new("energyMeters", "pool-meter")], "role", null, Value("\"pv\"")));
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out _, out var report), Is.False);
        var conflict = report.Conflicts.Single();
        Assert.That(conflict.Kind, Is.EqualTo(recreate ? RoamingNetworkMergeConflictKind.ReplaceModify : RoamingNetworkMergeConflictKind.DeleteModify));
        Assert.That(conflict.ElementPath, Is.EqualTo(new[] { new POIElementPathSegment("energyMeters", "pool-meter") }));
        Assert.That(conflict.PropertyName, Is.Null); Assert.That(conflict.Path, Does.Contain("/energyMeters/@id=")); before.AssertUnchanged(history);
        var resolved = Resolved(history, left, right, _ => RoamingNetworkMergeResolution.UseRight, out report);
        Publish(history, resolved);
        var current = history.Head.Snapshot.GetEntity(InfrastructureEntityType.ChargingPool, Pool).Properties["energyMeters"].EnumerateArray().Single();
        Assert.That(current.GetProperty("role").GetString(), Is.EqualTo("pv"));
        Assert.That(current.GetProperty("created").GetString(), Is.EqualTo(conflict.RightValue!.Value.GetProperty("created").GetString()));
    }

    [Test]
    public void A_recreated_graph_entity_does_not_absorb_an_edit_to_its_prior_creation()
    {
        using var history = History(); var root = history.Head.Id;
        var document = history.Head.Snapshot.GetEntityJSON(InfrastructureEntityType.EVSE, Evse); document["created"] = InteropFixture.Time.ToString("O");
        var left = Prepare(history, root, "recreated", RoamingNetworkChange.Remove("EVSE", Evse),
            RoamingNetworkChange.Add("EVSE", Evse, Document(document), "ChargingStation", Station));
        var right = Prepare(history, root, "edited", Power("150 kW")); Store(history, left); Store(history, right);
        var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out _, out var report), Is.False);
        Assert.That(report.Conflicts.Single().Kind, Is.EqualTo(RoamingNetworkMergeConflictKind.ReplaceModify)); before.AssertUnchanged(history);
        var merged = Resolved(history, left, right, _ => RoamingNetworkMergeResolution.UseLeft, out report); Store(history, merged);
        Assert.That(history.GetSnapshot(merged.Id).GetEntity(InfrastructureEntityType.EVSE, Evse).Properties["created"].GetString(),
            Is.EqualTo(history.GetSnapshot(left.Id).GetEntity(InfrastructureEntityType.EVSE, Evse).Properties["created"].GetString()));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Equal_additions_coalesce_and_different_additions_require_an_explicit_choice(Boolean equal)
    {
        using var history = History(); var root = history.Head.Id;
        var document = history.Head.Snapshot.GetEntityJSON(InfrastructureEntityType.EVSE, Evse); document["@id"] = "DE*ABC*E2";
        var left = Prepare(history, root, "left", RoamingNetworkChange.Add("EVSE", "DE*ABC*E2", Document(document), "ChargingStation", Station));
        if (!equal) document["maxPower"] = "200 kW";
        var right = Prepare(history, root, "right", RoamingNetworkChange.Add("EVSE", "DE*ABC*E2", Document(document), "ChargingStation", Station));
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out _, out var preview), Is.EqualTo(equal));
        if (equal) Assert.That(preview.Conflicts, Is.Empty);
        else Assert.That(preview.Conflicts.Single().Kind, Is.EqualTo(RoamingNetworkMergeConflictKind.AddAdd));
        before.AssertUnchanged(history);
        var merged = Resolved(history, left, right, _ => RoamingNetworkMergeResolution.UseRight, out _); Publish(history, merged);
        Assert.That(history.Head.Snapshot.Entities.Keys.Count(key => key.Type == InfrastructureEntityType.EVSE), Is.EqualTo(2));
        Assert.That(history.Head.Snapshot.GetEntity(InfrastructureEntityType.EVSE, "DE*ABC*E2").Properties["maxPower"].GetString(),
            Is.EqualTo(equal ? "100 kW" : "200 kW"));
    }

    private static RoamingNetworkHistory WithReferences()
    {
        var network = Network();
        var seed = Batch(network.DataSnapshot, "seed-references",
            RoamingNetworkChange.Add("ChargingTariff", Tariff1, Value("""{"@id":"DE*ABC*T1","currency":"EUR","elements":[{"priceComponents":[{"type":"FLAT","price":1}]}]}"""), "ChargingStationOperator", "DE*ABC"),
            RoamingNetworkChange.Add("ChargingTariff", Tariff2, Value("""{"@id":"DE*ABC*T2","currency":"EUR","elements":[{"priceComponents":[{"type":"FLAT","price":2}]}]}"""), "ChargingStationOperator", "DE*ABC"));
        return History(network.ApplyChangeSet(seed));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Deleted_tariffs_report_every_consumer_and_target_in_deterministic_order(Boolean swap)
    {
        using var history = WithReferences(); var root = history.Head.Id;
        var removed = Prepare(history, root, "deleted", RoamingNetworkChange.Remove("ChargingTariff", Tariff1), RoamingNetworkChange.Remove("ChargingTariff", Tariff2));
        var referenced = Prepare(history, root, "referenced", RoamingNetworkChange.UpdateProperty("EVSE", Evse, "tariffIds", null, Value("[\"DE*ABC*T2\",\"DE*ABC*T1\"]")),
            RoamingNetworkChange.UpdateProperty("ChargingConnector", "1", "tariffIds", null, Value("[\"DE*ABC*T1\"]"), "EVSE", Evse));
        var left = swap ? referenced : removed; var right = swap ? removed : referenced;
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out var absent, out var report, merge: true, mergedChangeSetId: "merge"), Is.False);
        Assert.That(absent, Is.Null); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.Conflicts));
        Assert.That(report.Conflicts, Has.Length.EqualTo(3)); Assert.That(report.AfterETags, Is.Empty);
        Assert.That(report.Conflicts.All(conflict => conflict.Kind == RoamingNetworkMergeConflictKind.Reference && conflict.PropertyName == "tariffIds"), Is.True);
        Assert.That(report.Conflicts.Where(conflict => conflict.Entity!.Value.Type == InfrastructureEntityType.EVSE).Select(conflict => conflict.RelatedEntity!.Value.Id),
            Is.EqualTo(new[] { Tariff1, Tariff2 }));
        var connector = report.Conflicts.Single(conflict => conflict.Entity!.Value.Type == InfrastructureEntityType.ChargingConnector);
        Assert.That(connector.Entity!.Value.Scope, Is.EqualTo(Evse)); Assert.That(connector.Path, Does.Contain("/EVSE="));
        Assert.That(connector.RelatedEntity, Is.EqualTo(new InfrastructureEntityKey(InfrastructureEntityType.ChargingTariff, Tariff1)));
        before.AssertUnchanged(history);
        var calls = new List<RoamingNetworkMergeConflict>();
        var merged = Resolved(history, left, right, conflict => {
            calls.Add(conflict); return swap ? RoamingNetworkMergeResolution.UseRight : RoamingNetworkMergeResolution.UseLeft;
        }, out report);
        // Selecting the EVSE subtree removes both its own and connector references in one decision.
        Assert.That(calls, Has.Count.EqualTo(1)); Assert.That(report.Conflicts, Has.Length.EqualTo(1));
        Assert.That(report.Conflicts.Single().RelatedEntity!.Value.Id, Is.EqualTo(Tariff1));
        before.AssertUnchanged(history); Publish(history, merged);
        Assert.That(history.Head.Snapshot.Entities.Keys.Any(key => key.Type == InfrastructureEntityType.ChargingTariff), Is.False);
        var decision = merged.ChangeSet!.Metadata[RoamingNetworkHistory.MergeMetadataProperty].GetProperty("Resolutions")[0];
        Assert.That(decision.GetProperty("RelatedEntity").GetProperty("EntityId").GetString(), Is.EqualTo(Tariff1));
        using var recovered = RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit);
        Assert.That(recovered.Head.Id, Is.EqualTo(merged.Id));
    }

    [Test]
    public void Keeping_an_invalid_reference_does_not_loop_or_weaken_reference_validation()
    {
        using var history = WithReferences(); var root = history.Head.Id;
        var left = Prepare(history, root, "delete", RoamingNetworkChange.Remove("ChargingTariff", Tariff1));
        var right = Prepare(history, root, "reference", RoamingNetworkChange.UpdateProperty("EVSE", Evse, "tariffIds", null, Value("[\"DE*ABC*T1\"]")));
        Publish(history, left); Store(history, right); var before = new Observation(history); var calls = 0;
        Assert.That(history.TryMerge(left.Id, right.Id, out var absent, out var report, merge: true, mergedChangeSetId: "invalid",
            resolveConflict: _ => { calls++; return RoamingNetworkMergeResolution.UseRight; }), Is.False);
        Assert.That(absent, Is.Null); Assert.That(calls, Is.EqualTo(1)); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.Conflicts));
        Assert.That(report.Conflicts, Has.Length.EqualTo(2)); Assert.That(report.Conflicts.Count(conflict => conflict.Resolution is null), Is.EqualTo(1));
        Assert.That(report.AfterETags, Is.Empty); before.AssertUnchanged(history);
    }

    [Test]
    public void Admission_lists_allow_future_ids_but_active_group_members_conflict_with_target_deletion()
    {
        var network = Network(); var groupId = EVSEGroup_Id.Parse(ChargingStationOperator_Id.Parse("DE*ABC"), "fast").ToString();
        var group = Value(JsonSerializer.Serialize(new Dictionary<String, Object> {
            ["@id"] = groupId, ["name"] = new { en = "Fast" }, ["EVSEIds"] = Array.Empty<String>(), ["allowedMemberIds"] = new[] { Evse }
        }));
        using var history = History(network.ApplyChangeSet(Batch(network.DataSnapshot, "group",
            RoamingNetworkChange.Add("EVSEGroup", groupId, group, "ChargingStationOperator", "DE*ABC"))));
        var root = history.Head.Id;
        var left = Prepare(history, root, "delete", RoamingNetworkChange.Remove("EVSE", Evse));
        var right = Prepare(history, root, "active-member", RoamingNetworkChange.AddElement("EVSEGroup", groupId, [new("EVSEIds", Evse)], Value(JsonSerializer.Serialize(Evse))));
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out _, out var report), Is.False);
        var conflict = report.Conflicts.Single(); Assert.That(conflict.Kind, Is.EqualTo(RoamingNetworkMergeConflictKind.Reference));
        Assert.That(conflict.PropertyName, Is.EqualTo("EVSEIds")); Assert.That(conflict.RelatedEntity, Is.EqualTo(new InfrastructureEntityKey(InfrastructureEntityType.EVSE, Evse)));
        before.AssertUnchanged(history);
        var merged = Resolved(history, left, right, _ => RoamingNetworkMergeResolution.UseLeft, out _); Publish(history, merged);
        var stored = history.Head.Snapshot.GetEntity(InfrastructureEntityType.EVSEGroup, groupId).Properties;
        Assert.That(stored["EVSEIds"].GetArrayLength(), Is.Zero); Assert.That(stored["allowedMemberIds"].GetArrayLength(), Is.EqualTo(1));
    }

    [Test]
    public void Referenced_registry_grid_operator_cannot_be_removed_and_edits_reach_connection_points()
    {
        using var history = History(); var root = history.Head.Id;
        Assert.Throws<RoamingNetworkChangeSetException>(() =>
            Prepare(history, root, "remove-registry", RoamingNetworkChange.Remove("GridOperator", "DE*GRD")));
        var edit = Prepare(history, root, "edit-registry", RoamingNetworkChange.UpdateProperty("GridOperator", "DE*GRD",
            "name", null, Value("{\"en\":\"Shared operator\"}")));
        Publish(history, edit);
        var network = history.Head.Network;
        var op = network.GridOperators.Single();
        Assert.That(network.ChargingPools.Single().GridConnectionPoint!.GridOperator, Is.SameAs(op));
        Assert.That(op.Name.ToJSON()["en"]!.Value<String>(), Is.EqualTo("Shared operator"));
    }

    [Test]
    public void A_criss_cross_history_requires_one_best_ancestor_and_binds_the_choice_into_the_merge_identity()
    {
        using var history = History(); var root = history.Head.Id;
        var a = Prepare(history, root, "a", Power("150 kW")); var b = Prepare(history, root, "b", Rename("B")); Store(history, a); Store(history, b);
        var ab = Resolved(history, a, b, _ => null, out _, "ab"); Store(history, ab);
        var ba = Resolved(history, b, a, _ => null, out _, "ba"); Store(history, ba);
        var before = new Observation(history);
        Assert.That(history.TryMerge(ab.Id, ba.Id, out var absent, out var ambiguous), Is.False);
        Assert.That(absent, Is.Null); Assert.That(ambiguous.Status, Is.EqualTo(RoamingNetworkMergeStatus.AmbiguousAncestor));
        Assert.That(ambiguous.AncestorCandidates, Is.EquivalentTo(new[] { a.Id, b.Id })); Assert.That(ambiguous.Ancestor, Is.Null);
        Assert.That(history.TryMerge(ab.Id, ba.Id, out absent, out var invalid, commonAncestor: root), Is.False);
        Assert.That(invalid.Status, Is.EqualTo(RoamingNetworkMergeStatus.InvalidInput));
        Assert.That(history.TryMerge(ab.Id, ba.Id, out var first, out var selectedA, merge: true, mergedChangeSetId: "chosen", createdAt: InteropFixture.Time.AddDays(2), commonAncestor: a.Id), Is.True, selectedA.Message);
        Assert.That(history.TryMerge(ab.Id, ba.Id, out var same, out _, merge: true, mergedChangeSetId: "chosen", createdAt: InteropFixture.Time.AddDays(2), commonAncestor: a.Id), Is.True);
        Assert.That(first!.Id, Is.EqualTo(same!.Id));
        Assert.That(history.TryMerge(ab.Id, ba.Id, out var second, out var selectedB, merge: true, mergedChangeSetId: "chosen", createdAt: InteropFixture.Time.AddDays(2), commonAncestor: b.Id), Is.True, selectedB.Message);
        Assert.That(first.Id, Is.Not.EqualTo(second!.Id)); Assert.That(selectedA.AfterETags, Is.EqualTo(selectedB.AfterETags));
        Assert.That(first.Parents, Is.EqualTo(new[] { ab.Id, ba.Id })); before.AssertUnchanged(history);
        Store(history, Sign(first.WithChangeSet(Sign(first.ChangeSet!))));
    }

    private static RoamingNetworkHistory WithParking()
    {
        var network = Network();
        return History(network.ApplyChangeSet(Batch(network.DataSnapshot, "seed-parking",
            RoamingNetworkChange.Add("ParkingOperator", "park-a", Value("""{"id":"park-a","name":{"en":"A"}}"""), "RoamingNetwork", "interop-network"),
            RoamingNetworkChange.Add("ParkingOperator", "park-b", Value("""{"id":"park-b","name":{"en":"B"}}"""), "RoamingNetwork", "interop-network"),
            RoamingNetworkChange.Add("ParkingOperator", "park-c", Value("""{"id":"park-c","name":{"en":"C"}}"""), "RoamingNetwork", "interop-network"),
            RoamingNetworkChange.Add("ParkingSensor", "sensor-a", Value("""{"@id":"sensor-a","name":{"en":"Sensor"}}"""), "ParkingOperator", "park-a"),
            RoamingNetworkChange.Add("ParkingSpace", "space-a", Value("""{"@id":"space-a","name":{"en":"Space"}}"""), "ParkingOperator", "park-a"))));
    }

    private static RoamingNetworkCommit MoveSensor(RoamingNetworkHistory history, RoamingNetworkCommitId root, String id, String destination)
        => Prepare(history, root, id, RoamingNetworkChange.Remove("ParkingSensor", "sensor-a"),
            RoamingNetworkChange.Add("ParkingSensor", "sensor-a", Document(history.GetSnapshot(root).GetEntityJSON(InfrastructureEntityType.ParkingSensor, "sensor-a")), "ParkingOperator", destination));

    [TestCase("Base", "park-a")]
    [TestCase("Left", "park-b")]
    [TestCase("Right", "park-c")]
    public void Different_owner_moves_require_a_parent_choice_without_mixing_subtrees(String choice, String expectedOwner)
    {
        using var history = WithParking(); var root = history.Head.Id;
        var left = MoveSensor(history, root, "left", "park-b"); var right = MoveSensor(history, root, "right", "park-c");
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out _, out var preview), Is.False);
        var conflict = preview.Conflicts.Single(); Assert.That(conflict.Kind, Is.EqualTo(RoamingNetworkMergeConflictKind.Ownership));
        Assert.That(conflict.PropertyName, Is.EqualTo("$parent"));
        Assert.That(conflict.LeftValue!.Value.GetProperty("EntityId").GetString(), Is.EqualTo("park-b"));
        Assert.That(conflict.RightValue!.Value.GetProperty("EntityId").GetString(), Is.EqualTo("park-c"));
        var merged = Resolved(history, left, right, _ => Choice(choice), out _); before.AssertUnchanged(history); Publish(history, merged);
        Assert.That(history.Head.Snapshot.GetEntity(InfrastructureEntityType.ParkingSensor, "sensor-a").Parent,
            Is.EqualTo(new InfrastructureEntityKey(InfrastructureEntityType.ParkingOperator, expectedOwner)));
        Assert.That(history.Head.Snapshot.Entities.Keys.Count(key => key.Type == InfrastructureEntityType.ParkingSensor), Is.EqualTo(1));
    }

    [Test]
    public void A_target_move_and_new_reference_conflict_when_their_combination_crosses_operator_scope()
    {
        using var history = WithParking(); var root = history.Head.Id;
        var left = MoveSensor(history, root, "move", "park-b");
        var right = Prepare(history, root, "reference", RoamingNetworkChange.UpdateProperty("ParkingSpace", "space-a", "sensors", null, Value("[\"sensor-a\"]")));
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out _, out var report), Is.False);
        var conflict = report.Conflicts.Single(); Assert.That(conflict.Kind, Is.EqualTo(RoamingNetworkMergeConflictKind.Reference));
        Assert.That(conflict.Entity, Is.EqualTo(new InfrastructureEntityKey(InfrastructureEntityType.ParkingSpace, "space-a")));
        Assert.That(conflict.PropertyName, Is.EqualTo("sensors")); Assert.That(conflict.Message, Does.Contain("different parking operator"));
        Assert.That(conflict.RelatedEntity, Is.EqualTo(new InfrastructureEntityKey(InfrastructureEntityType.ParkingSensor, "sensor-a")));
        before.AssertUnchanged(history);
        var merged = Resolved(history, left, right, _ => RoamingNetworkMergeResolution.UseLeft, out _); Publish(history, merged);
        Assert.That(history.Head.Snapshot.GetEntity(InfrastructureEntityType.ParkingSensor, "sensor-a").Parent!.Value.Id, Is.EqualTo("park-b"));
        Assert.That(history.Head.Snapshot.GetEntity(InfrastructureEntityType.ParkingSpace, "space-a").Properties.ContainsKey("sensors"), Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Connector_delete_modify_conflicts_are_scoped_to_their_EVSE(Boolean sameScope)
    {
        var network = Network(); var document = network.DataSnapshot.GetEntityJSON(InfrastructureEntityType.EVSE, Evse); document["@id"] = "DE*ABC*E2";
        using var history = History(network.ApplyChangeSet(Batch(network.DataSnapshot, "seed-connector",
            RoamingNetworkChange.Add("EVSE", "DE*ABC*E2", Document(document), "ChargingStation", Station))));
        var root = history.Head.Id;
        var left = Prepare(history, root, "delete", RoamingNetworkChange.Remove("ChargingConnector", "1", oldValue: null, parentEntityType: "EVSE", parentEntityId: Evse));
        var right = Prepare(history, root, "edit", RoamingNetworkChange.UpdateProperty("ChargingConnector", "1", "lockable", Value("false"), Value("true"), "EVSE", sameScope ? Evse : "DE*ABC*E2"));
        Publish(history, left); Store(history, right);
        Assert.That(history.TryMerge(left.Id, right.Id, out _, out var report), Is.EqualTo(!sameScope));
        if (sameScope)
        {
            var conflict = report.Conflicts.Single(); Assert.That(conflict.Kind, Is.EqualTo(RoamingNetworkMergeConflictKind.DeleteModify));
            Assert.That(conflict.Entity!.Value.Scope, Is.EqualTo(Evse)); Assert.That(conflict.Path, Does.Contain("/EVSE="));
        }
        else Assert.That(report.Conflicts, Is.Empty);
        var merged = Resolved(history, left, right, _ => RoamingNetworkMergeResolution.UseRight, out _); Publish(history, merged);
        Assert.That(history.Head.Snapshot.Entities.Keys.Count(key => key.Type == InfrastructureEntityType.ChargingConnector), Is.EqualTo(sameScope ? 2 : 1));
        Assert.That(history.Head.Snapshot.GetEntity(InfrastructureEntityType.ChargingConnector, "1", sameScope ? Evse : "DE*ABC*E2").Properties["lockable"].GetBoolean(), Is.True);
    }

    [TestCase("Graph")]
    [TestCase("Reference")]
    [TestCase("Nested")]
    [TestCase("Ownership")]
    public void Unsupported_resolution_shapes_cannot_bypass_identity_ownership_or_graph_validation(String category)
    {
        using var history = category == "Reference" ? WithReferences() : category == "Ownership" ? WithParking() : History();
        var root = history.Head.Id;
        var left = category switch {
            "Reference" => Prepare(history, root, "left", RoamingNetworkChange.Remove("ChargingTariff", Tariff1)),
            "Nested" => Prepare(history, root, "left", RoamingNetworkChange.RemoveElement("ChargingPool", Pool, [new("energyMeters", "pool-meter")])),
            "Ownership" => MoveSensor(history, root, "left", "park-b"),
            _ => Prepare(history, root, "left", RoamingNetworkChange.Remove("EVSE", Evse))
        };
        var right = category switch {
            "Reference" => Prepare(history, root, "right", RoamingNetworkChange.UpdateProperty("EVSE", Evse, "tariffIds", null, Value("[\"DE*ABC*T1\"]"))),
            "Nested" => Prepare(history, root, "right", RoamingNetworkChange.UpdateElementProperty("ChargingPool", Pool, [new("energyMeters", "pool-meter")], "role", null, Value("\"pv\""))),
            "Ownership" => MoveSensor(history, root, "right", "park-c"),
            _ => Prepare(history, root, "right", Power("150 kW"))
        };
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out var absent, out var report, merge: true, mergedChangeSetId: "invalid",
            resolveConflict: _ => category == "Ownership" ? RoamingNetworkMergeResolution.Remove :
                RoamingNetworkMergeResolution.Custom(Value(category == "Nested" ? "{\"id\":\"wrong-meter\",\"role\":\"pv\"}" : "{}"))), Is.False);
        Assert.That(absent, Is.Null); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.InvalidInput));
        Assert.That(report.AfterETags, Is.Empty); before.AssertUnchanged(history);
    }

    [Test]
    public void A_referenced_replacement_prepares_explicit_detachment_and_restoration_without_publication()
    {
        var network = Network(); var id = EVSEGroup_Id.Parse(ChargingStationOperator_Id.Parse("DE*ABC"), "active").ToString();
        var group = Value(JsonSerializer.Serialize(new Dictionary<String, Object> {
            ["@id"] = id, ["name"] = new { en = "Active" }, ["EVSEIds"] = new[] { Evse }, ["allowedMemberIds"] = new[] { Evse }
        }));
        using var history = History(network.ApplyChangeSet(Batch(network.DataSnapshot, "seed-active",
            RoamingNetworkChange.Add("EVSEGroup", id, group, "ChargingStationOperator", "DE*ABC"))));
        var root = history.Head.Id; var replacement = history.Head.Snapshot.GetEntityJSON(InfrastructureEntityType.EVSE, Evse);
        replacement["created"] = InteropFixture.Time.AddDays(1).ToString("O");
        var left = Prepare(history, root, "left", Rename("Left"));
        var right = Prepare(history, root, "recreate",
            RoamingNetworkChange.RemoveElement("EVSEGroup", id, [new("EVSEIds", Evse)]), RoamingNetworkChange.Remove("EVSE", Evse),
            RoamingNetworkChange.Add("EVSE", Evse, Document(replacement), "ChargingStation", Station),
            RoamingNetworkChange.AddElement("EVSEGroup", id, [new("EVSEIds", Evse)], Value(JsonSerializer.Serialize(Evse))));
        Publish(history, left); Store(history, right); var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out var prepared, out var report, merge: true, mergedChangeSetId: "reference-plan"), Is.True, report.Message);
        Assert.That(prepared, Is.Not.Null); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.Prepared));
        Assert.That(report.ReferenceTransitions.Single().Consumer, Is.EqualTo(new InfrastructureEntityKey(InfrastructureEntityType.EVSEGroup, id)));
        Assert.That(report.AfterETags, Is.Not.Empty); before.AssertUnchanged(history);
    }

    [TestCase("Return")]
    [TestCase("Throw")]
    public void Resolver_reentry_and_exceptions_cannot_retain_publish_or_dispose_the_history(String mode)
    {
        using var history = History(); var root = history.Head.Id;
        var left = Prepare(history, root, "left", Power("150 kW")); var right = Prepare(history, root, "right", Power("200 kW")); Publish(history, left); Store(history, right);
        var unused = Prepare(history, left.Id, "unused", Rename("Unused")); var before = new Observation(history);
        var success = history.TryMerge(left.Id, right.Id, out var merged, out var report, merge: true, mergedChangeSetId: "resolved", resolveConflict: conflict => {
            Assert.That(history.TryMerge(left.Id, right.Id, out _, out var reentry), Is.False); Assert.That(reentry.Status, Is.EqualTo(RoamingNetworkMergeStatus.Unavailable));
            Assert.That(history.TryStoreCommit(unused, out var retained), Is.False); Assert.That(retained.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.Unavailable));
            Assert.That(history.TryPublish(left.Id, unused, out var published), Is.False); Assert.That(published.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.Unavailable));
            Assert.That(() => history.Dispose(), Throws.InvalidOperationException);
            if (mode == "Throw") throw new InvalidOperationException("Injected resolver failure");
            return RoamingNetworkMergeResolution.UseLeft;
        });
        Assert.That(success, Is.EqualTo(mode == "Return")); before.AssertUnchanged(history);
        if (mode == "Throw") { Assert.That(merged, Is.Null); Assert.That(report.Status, Is.EqualTo(RoamingNetworkMergeStatus.InvalidInput)); }
        var retry = Resolved(history, left, right, _ => RoamingNetworkMergeResolution.UseRight, out _); Publish(history, retry);
    }
}
