/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using EntityMap = System.Collections.Immutable.ImmutableDictionary<cloud.charging.open.protocols.WWCP.POI.InfrastructureEntityKey, cloud.charging.open.protocols.WWCP.POI.InfrastructureEntitySnapshot>;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ModelPreparationTests
{
    private static JObject Input() => InfrastructureJson.ReadObject(File.ReadAllText(FilePath("snapshot.input.json")));
    private static String Text(JToken value) => value.ToString(Formatting.None);
    private static JObject At(JObject value, Int32 depth)
    {
        foreach (var field in new[] { "chargingStationOperators", "chargingPools", "chargingStations", "EVSEs", "socketOutlets" }.Take(depth))
            value = (JObject)value[field]![0]!;
        return value;
    }

    private static void SameEntities(EntityMap expected, EntityMap actual)
    {
        Assert.That(actual.Keys, Is.EquivalentTo(expected.Keys));
        foreach (var (key, entity) in expected)
        {
            var result = actual[key];
            Assert.That(result.Parent, Is.EqualTo(entity.Parent), key.ToString());
            Assert.That(result.Children, Is.EquivalentTo(entity.Children), key.ToString());
            Assert.That(result.Properties.Keys, Is.EquivalentTo(entity.Properties.Keys), key.ToString());
            foreach (var (field, value) in entity.Properties)
                Assert.That(result.Properties[field].GetRawText(), Is.EqualTo(value.GetRawText()), key + "/" + field);
        }
    }

    private static (InfrastructureEntityKey Root, EntityMap Entities) ReferenceImport(JObject input)
    {
        var entities = EntityMap.Empty;
        var root = SnapshotImportOracle.Import(input, InfrastructureEntityType.RoamingNetwork, null, ref entities, null);
        return (root, entities);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)]
    public void Capture_matches_previous_import_and_owns_no_caller_nodes(Int32 variant)
    {
        var input = variant == 2 ? Network().DataSnapshot.ToJSON() : Input();
        if (variant == 1)
        {
            input["revision"] = 42; input["appliedChangeSetId"] = "caller";
            for (var depth = 0; depth < 5; depth++)
            {
                At(input, depth)["status"] = "charging";
                At(input, depth)["adminStatus"] = "outOfService";
                At(input, depth)["created"] = "2026-01-01T02:00:00.1234567+02:00";
            }
            At(input, 4)["maxPower"] = "100 kW";
            At(input, 4)["maxPowerRealTime"] = "50 kW";
        }
        var original = Text(input); var reference = ReferenceImport(input);
        var captured = RoamingNetworkDataSnapshot.Capture(input, 17, "captured");
        Assert.That(captured.Root, Is.EqualTo(reference.Root)); SameEntities(reference.Entities, captured.Entities);
        Assert.That(captured.Revision, Is.EqualTo(17)); Assert.That(captured.AppliedChangeSetId, Is.EqualTo("captured"));
        var json = captured.ToCanonicalJSON(); var cbor = captured.ToCanonicalCBOR(); var tags = captured.ETags;
        Assert.That(Text(input), Is.EqualTo(original));
        input.RemoveAll(); input["@id"] = "changed";
        Assert.That(captured.ToCanonicalJSON(), Is.EqualTo(json)); Assert.That(captured.ToCanonicalCBOR(), Is.EqualTo(cbor));
        Assert.That(captured.ETags, Is.EqualTo(tags));
        var exported = captured.ToJSON(); exported.RemoveAll();
        Assert.That(captured.ToCanonicalJSON(), Is.EqualTo(json));
    }

    [Test, Combinatorial]
    public void Failed_import_has_the_previous_error_and_leaves_every_input_node_unchanged(
        [Values(0, 1, 2, 3, 4, 5)] Int32 depth, [Values("unknown", "missing", "wrong-array")] String failure)
    {
        var input = Input(); var selected = At(input, depth);
        if (failure == "unknown") selected["unknown-static-field"] = new JObject { ["number"] = 1 };
        else if (failure == "missing") selected.Remove("@id");
        else
        {
            var type = (InfrastructureEntityType)Enum.Parse(typeof(InfrastructureEntityType),
                new[] { "RoamingNetwork", "ChargingStationOperator", "ChargingPool", "ChargingStation", "EVSE", "ChargingConnector" }[depth]);
            selected[InfrastructureChangeSchema.Relations.FirstOrDefault(item => item.Value.Parent == type).Value.Field ?? "unknown-static-field"] = true;
        }
        var before = Text(input);
        var expected = Assert.Catch(() => ReferenceImport(input))!;
        var actual = Assert.Catch(() => RoamingNetworkDataSnapshot.Capture(input, 0))!;
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        Assert.That(Text(input), Is.EqualTo(before));
        Assert.That(RoamingNetworkDataSnapshot.Capture(Input(), 0).ETags, Is.EqualTo(Network().ETags));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
    public void Duplicate_descendants_preserve_reservation_errors_and_allow_a_fresh_retry(Int32 depth)
    {
        var input = Input(); var child = At(input, depth); var array = (JArray)child.Parent!;
        array.Add(child.DeepClone()); var before = Text(input);
        var expected = Assert.Catch(() => ReferenceImport(input))!;
        var actual = Assert.Catch(() => RoamingNetworkDataSnapshot.Capture(input, 0))!;
        Assert.That(actual.Message, Is.EqualTo(expected.Message)); Assert.That(Text(input), Is.EqualTo(before));
        array.RemoveAt(array.Count - 1); SameEntities(ReferenceImport(input).Entities, RoamingNetworkDataSnapshot.Capture(input, 0).Entities);
    }

    public static IEnumerable<TestCaseData> PreparationCases()
    {
        var input = InfrastructureJson.ReadObject(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "InteropVectors", "snapshot.input.json")));
        var kinds = new HashSet<String>(StringComparer.Ordinal);
        POIRepresentation.Visit(input, nameof(RoamingNetwork), "", (_, kind, _) => kinds.Add(kind));
        foreach (var kind in kinds.Order(StringComparer.Ordinal))
            foreach (var owned in new[] { false, true }) yield return new TestCaseData(kind, owned);
    }

    [TestCaseSource(nameof(PreparationCases))]
    public void Completing_private_children_matches_the_previous_algorithm_and_detaches_both_inputs(String kind, Boolean owned)
    {
        var input = Input(); JObject? selected = null;
        POIRepresentation.Visit(input, nameof(RoamingNetwork), "", (value, type, _) => { if (type == kind && selected is null) selected = value; });
        var source = (JObject)selected!.DeepClone(); var projection = (JObject)source.DeepClone();
        source["customData"] = new JObject { ["status"] = "customer", ["ETags"] = new JArray("literal"), ["reading"] = "250 kW" };
        source["created"] = null; source["roamingNetworkId"] = "supplied-owner";
        projection["created"] = "2026-01-01T00:00:00.1234567Z";
        var sourceBefore = Text(source); var projectionBefore = Text(projection);
        var expected = SnapshotPreparationOracle.CompleteImport(source, projection, kind, owned);
        var actual = POISnapshotRepresentation.CompleteImport(source, projection, kind, owned);
        Assert.That(Text(actual), Is.EqualTo(Text(expected)));
        Assert.That(Text(source), Is.EqualTo(sourceBefore)); Assert.That(Text(projection), Is.EqualTo(projectionBefore));
        actual.RemoveAll(); Assert.That(Text(source), Is.EqualTo(sourceBefore)); Assert.That(Text(projection), Is.EqualTo(projectionBefore));
    }

    [TestCase(false)] [TestCase(true)]
    public void Projection_fallback_and_child_order_match_previous_copies(Boolean reverse)
    {
        var projection = Input(); var source = (JObject)projection.DeepClone();
        source["chargingStationOperators"] = new JArray();
        source.Remove("transparencySoftwareCertificates");
        if (reverse) source["transparencySoftware"] = new JArray(((JArray)source["transparencySoftware"]!).Reverse().Select(item => item.DeepClone()));
        var original = Text(source); var projected = Text(projection);
        var expected = SnapshotPreparationOracle.CompleteImport(source, projection, nameof(RoamingNetwork));
        var actual = POISnapshotRepresentation.CompleteImport(source, projection, nameof(RoamingNetwork));
        Assert.That(Text(actual), Is.EqualTo(Text(expected)));
        Assert.That(Text(source), Is.EqualTo(original)); Assert.That(Text(projection), Is.EqualTo(projected));
        At(actual, 4)["maxPower"] = "999 kW";
        Assert.That(Text(projection), Is.EqualTo(projected));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
    public void Late_projection_failure_keeps_both_borrowed_trees_unchanged(Int32 depth)
    {
        var source = Input(); var projection = Input(); At(projection, depth)["@id"] = new JObject { ["invalid"] = true };
        var before = Text(source); var projected = Text(projection);
        var expected = Assert.Catch(() => SnapshotPreparationOracle.CompleteImport(source, projection, nameof(RoamingNetwork)))!;
        var actual = Assert.Catch(() => POISnapshotRepresentation.CompleteImport(source, projection, nameof(RoamingNetwork)))!;
        Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        Assert.That(Text(source), Is.EqualTo(before)); Assert.That(Text(projection), Is.EqualTo(projected));
        Assert.That(Text(POISnapshotRepresentation.CompleteImport(source, Input(), nameof(RoamingNetwork))),
            Is.EqualTo(Text(SnapshotPreparationOracle.CompleteImport(source, Input(), nameof(RoamingNetwork)))));
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)] [TestCase(5)]
    public void Declared_identifiers_are_checked_without_modifying_caller_nodes(Int32 depth)
    {
        var source = Network().DataSnapshot.ToJSON();
        if (depth == 5) At(source, depth)["lockable"] = true;
        else At(source, depth)["created"] = "2025-01-01T00:00:00Z";
        var before = Text(source);
        RoamingNetwork? network = null;
        var error = Assert.Catch(() => network = POIRepresentation.ParseJSON(source, json => RoamingNetwork.Parse(json)))!;
        Assert.That(network, Is.Null); Assert.That(error.Message, Does.Contain("ETags")); Assert.That(Text(source), Is.EqualTo(before));
        Assert.That(POIRepresentation.ParseJSON(Network().DataSnapshot.ToJSON(), json => RoamingNetwork.Parse(json)).ETags, Is.EqualTo(Network().ETags));
    }

    [TestCase("duplicate")] [TestCase("unknown")]
    public void A_late_failed_add_is_atomic_and_the_valid_signed_retry_succeeds(String failure)
    {
        var network = Network(); var source = network.DataSnapshot; var before = source.ToCanonicalCBOR();
        var pool = (JObject)At(Input(), 2).DeepClone(); pool["@id"] = "DE*ABC*P2";
        pool["chargingStations"]![0]!["@id"] = "DE*ABC*S2"; pool["chargingStations"]![0]!["EVSEs"]![0]!["@id"] = "DE*ABC*E2";
        var valid = Text(pool);
        if (failure == "duplicate") ((JArray)pool["chargingStations"]![0]!["EVSEs"]![0]!["socketOutlets"]!).Add(pool["chargingStations"]![0]!["EVSEs"]![0]!["socketOutlets"]![0]!.DeepClone());
        else pool["chargingStations"]![0]!["EVSEs"]![0]!["socketOutlets"]![0]!["unknown-static-field"] = true;
        var operation = RoamingNetworkChange.Add("ChargingPool", "DE*ABC*P2", Value(Text(pool)), "ChargingStationOperator", "DE*ABC");
        var batch = Sign(new RoamingNetworkChangeSet("failed-add", source.Root.Id, source.Revision, Time, [operation], source.ETags, source.ETags));
        var signed = batch.ToCBOR(); Assert.Catch(() => source.ApplyChangeSet(batch, VerifyBatch));
        Assert.That(source.ToCanonicalCBOR(), Is.EqualTo(before)); Assert.That(batch.ToCBOR(), Is.EqualTo(signed));
        var retry = Sign(Batch(source, "valid-retry", RoamingNetworkChange.Add("ChargingPool", "DE*ABC*P2", Value(valid), "ChargingStationOperator", "DE*ABC")));
        Assert.That(network.ApplyChangeSet(retry, VerifyBatch).ChargingPools.Count(), Is.EqualTo(2));
        Assert.That(source.ToCanonicalCBOR(), Is.EqualTo(before));
    }

    [Test]
    public async Task Independent_concurrent_parsers_leave_a_shared_input_unchanged()
    {
        var source = Input(); var before = Text(source); var expected = Network().ToCanonicalCBOR();
        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => RoamingNetwork.Parse(source).ToCanonicalCBOR())));
        foreach (var result in results) Assert.That(result, Is.EqualTo(expected));
        Assert.That(Text(source), Is.EqualTo(before));
    }

    [TestCase(false)] [TestCase(true)]
    public void Bound_representations_match_previous_strings_and_return_detached_values(Boolean runtime)
    {
        var input = Input();
        if (runtime) At(input, 4)["status"] = new JObject { ["value"] = "charging", ["timestamp"] = "2026-02-01T00:00:00Z" };
        var before = Text(input); var network = RoamingNetwork.Parse(input);
        var document = network.DataSnapshot.ToJSON(); var documentBefore = Text(document);
        SnapshotPreparationOracle.Bind(network, document); POISnapshotRepresentation.Bind(network, document);
        var children = new List<IImmutablePOI>();
        void Visit(IImmutablePOI value)
        {
            children.Add(value);
            var method = typeof(SnapshotPreparationOracle).GetMethod("Children", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic)!;
            foreach (var (_, child) in (IEnumerable<(String, IImmutablePOI)>)method.Invoke(null, [value])!) Visit(child);
        }
        Visit(network);
        foreach (var child in children)
        {
            var expected = SnapshotPreparationOracle.Get(child); var actual = POISnapshotRepresentation.Get(child);
            Assert.That(actual?.ToString(Formatting.None), Is.EqualTo(expected?.ToString(Formatting.None)), child.GetType().Name);
            if (actual is not null) { var value = Text(actual); actual.RemoveAll(); Assert.That(Text(POISnapshotRepresentation.Get(child)!), Is.EqualTo(value)); }
        }
        Assert.That(Text(input), Is.EqualTo(before)); Assert.That(Text(document), Is.EqualTo(documentBefore));
        document.RemoveAll(); Assert.That(network.ETags, Is.EqualTo(Network().ETags));
    }

    [TestCase(false)] [TestCase(true)]
    public void A_signed_added_subtree_preserves_payload_and_atomic_source(Boolean cbor)
    {
        var network = Network(); var source = network.DataSnapshot; var sourceBytes = source.ToCanonicalCBOR();
        var pool = (JObject)At(Input(), 2).DeepClone(); pool["@id"] = "DE*ABC*P2";
        pool["chargingStations"]![0]!["@id"] = "DE*ABC*S2"; pool["chargingStations"]![0]!["EVSEs"]![0]!["@id"] = "DE*ABC*E2";
        var payload = Text(pool);
        var operation = RoamingNetworkChange.Add("ChargingPool", "DE*ABC*P2", Value(payload), "ChargingStationOperator", "DE*ABC");
        var batch = Sign(Batch(source, "prepared-add", operation).WithMetadata(
            ImmutableDictionary<String, System.Text.Json.JsonElement>.Empty.Add("tokens", Value("{\"decimal\":1.0,\"exponent\":1e0,\"negativeZero\":-0,\"reading\":\"250 kW\"}"))));
        var signed = batch.ToCBOR(); var recovered = cbor ? RoamingNetworkChangeSet.ParseCBOR(signed) : batch;
        var changed = network.ApplyChangeSet(recovered, VerifyBatch);
        Assert.That(changed.ChargingPools.Count(), Is.EqualTo(2)); Assert.That(changed.EVSEs.Count(), Is.EqualTo(2));
        Assert.That(source.ToCanonicalCBOR(), Is.EqualTo(sourceBytes)); Assert.That(Text(pool), Is.EqualTo(payload));
        Assert.That(batch.ToCBOR(), Is.EqualTo(signed)); Assert.That(batch.VerifySignatures(peer => PublicKey(peer.KeyId), out var error), Is.True, error);
        var expected = source.Entities;
        SnapshotImportOracle.Import(InfrastructureJson.ReadObject(recovered.Changes[0].NewValue!.Value.GetRawText()), InfrastructureEntityType.ChargingPool,
            new InfrastructureEntityKey(InfrastructureEntityType.ChargingStationOperator, "DE*ABC"), ref expected, Time);
        SameEntities(expected.Where(item => !source.Entities.ContainsKey(item.Key)).ToImmutableDictionary(),
            changed.DataSnapshot.Entities.Where(item => !source.Entities.ContainsKey(item.Key)).ToImmutableDictionary());
    }

    [Test, Combinatorial]
    public void Recovery_preserves_original_peers_all_branch_states_and_fresh_runtime(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean cbor)
    {
        var names = new[] { "history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history" };
        var bytes = File.ReadAllBytes(FilePath(names[profile - 1] + ".cbor"));
        using var source = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        source.ApplyRuntimeUpdate(new(source.Head.Network.Id.ToString(), POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
            POIRuntimeStatusKind.Status, new("charging", Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        var commitCalls = 0; var batchCalls = 0;
        Boolean Commit(RoamingNetworkCommit value, RoamingNetworkChangeSetSignature peer) { commitCalls++; return VerifyCommit(value, peer); }
        Boolean BatchTrust(RoamingNetworkChangeSet value, RoamingNetworkChangeSetSignature peer) { batchCalls++; return VerifyBatch(value, peer); }
        using var restored = cbor ? RoamingNetworkHistory.ParseCBOR(bytes, BatchTrust, Commit, authorizeSnapshotBoundary: _ => true)
                                  : RoamingNetworkHistory.Parse(source.ToJSON(), BatchTrust, Commit, authorizeSnapshotBoundary: _ => true);
        Assert.That(restored.ToCBOR(), Is.EqualTo(bytes)); Assert.That(commitCalls, Is.GreaterThan(0));
        Assert.That(batchCalls, Is.EqualTo(source.Commits.Sum(value => value.ChangeSet?.Signatures.Length ?? 0)));
        foreach (var commit in source.Commits)
            Assert.That(restored.GetSnapshot(commit.Id).ToCanonicalJSON(), Is.EqualTo(source.GetSnapshot(commit.Id).ToCanonicalJSON()));
        Assert.That(restored.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
        Assert.That(source.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        var retryCalls = 0;
        Assert.Catch(() => { using var rejected = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, (_, _) => { retryCalls++; return false; }, authorizeSnapshotBoundary: _ => true); });
        Assert.That(retryCalls, Is.EqualTo(1));
    }
}
