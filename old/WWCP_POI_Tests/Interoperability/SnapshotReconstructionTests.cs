/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class SnapshotReconstructionTests
{
    private static readonly String[] Archives = ["history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history"];
    private static Byte[] Bytes(Int32 profile) => File.ReadAllBytes(FilePath(Archives[profile - 1] + ".cbor"));
    private static RoamingNetwork Original(RoamingNetworkDataSnapshot snapshot) => RoamingNetwork.Parse(snapshot.ToJSON());
    private static JObject At(JObject value, Int32 depth)
    {
        foreach (var field in new[] { "chargingStationOperators", "chargingPools", "chargingStations", "EVSEs", "socketOutlets" }.Take(depth))
            value = (JObject)value[field]![0]!;
        return value;
    }
    private static void Same(RoamingNetwork expected, RoamingNetwork actual)
    {
        Assert.That(actual.DataSnapshot.Revision, Is.EqualTo(expected.DataSnapshot.Revision));
        Assert.That(actual.DataSnapshot.AppliedChangeSetId, Is.EqualTo(expected.DataSnapshot.AppliedChangeSetId));
        Assert.That(actual.ToCanonicalJSON(), Is.EqualTo(expected.ToCanonicalJSON()));
        Assert.That(actual.ToCanonicalCBOR(), Is.EqualTo(expected.ToCanonicalCBOR()));
        Assert.That(actual.ETags, Is.EqualTo(expected.ETags));
        Assert.That(actual.DataSnapshot.Entities.Keys, Is.EquivalentTo(expected.DataSnapshot.Entities.Keys));
        foreach (var (key, value) in expected.DataSnapshot.Entities)
        {
            var entity = actual.DataSnapshot.Entities[key];
            Assert.That(entity.Parent, Is.EqualTo(value.Parent)); Assert.That(entity.Children, Is.EquivalentTo(value.Children));
            foreach (var (field, property) in value.Properties)
                Assert.That(entity.Properties[field].GetRawText(), Is.EqualTo(property.GetRawText()), key + "/" + field);
        }
        Assert.That(actual.EVSEs.Single().ChargingStation, Is.SameAs(actual.ChargingStations.Single()));
        Assert.That(actual.ChargingPools.Single().GridConnectionPoint!.GridOperator, Is.SameAs(actual.GridOperators.Single()));
    }

    [Test, Combinatorial]
    public void Exact_snapshots_share_static_maps_and_preserve_the_preceding_full_model(
        [Values(0L, 17L, Int64.MaxValue)] Int64 revision, [Values(false, true)] Boolean metadata)
    {
        var source = Network().DataSnapshot;
        var snapshot = RoamingNetworkDataSnapshot.Capture(source.GetDocument(), revision, metadata ? "reconstruction-🔌" : null);
        var expected = Original(snapshot); var actual = RoamingNetwork.ParseSnapshot(snapshot);
        Same(expected, actual); Assert.That(actual.DataSnapshot, Is.SameAs(snapshot));
        Assert.That(actual.DataSnapshot.Entities, Is.SameAs(snapshot.Entities));
        Assert.That(actual, Is.Not.SameAs(expected)); Assert.That(actual.EVSEs.Single(), Is.Not.SameAs(expected.EVSEs.Single()));
        var exported = actual.DataSnapshot.ToJSON(); exported.RemoveAll(); Same(expected, actual);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Incomplete_metadata_takes_the_existing_completion_and_capture_path(Int32 depth)
    {
        var source = Network().DataSnapshot.GetDocument(); At(source, depth).Remove("created");
        var snapshot = RoamingNetworkDataSnapshot.Capture(source, 7, "incomplete");
        var before = snapshot.ToCanonicalJSON(); var actual = RoamingNetwork.ParseSnapshot(snapshot);
        Assert.That(actual.DataSnapshot, Is.Not.SameAs(snapshot));
        Assert.That(actual.DataSnapshot.Revision, Is.EqualTo(7));
        Assert.That(actual.DataSnapshot.AppliedChangeSetId, Is.EqualTo("incomplete"));
        Assert.That(At(actual.DataSnapshot.GetDocument(), depth)["created"]!.Type, Is.EqualTo(JTokenType.String));
        Assert.That(snapshot.ToCanonicalJSON(), Is.EqualTo(before));
    }

    [Test, Combinatorial]
    public void Domain_errors_preserve_the_preceding_type_message_and_allow_an_independent_retry(
        [Values(0, 1, 2, 3, 4)] Int32 depth, [Values(false, true)] Boolean missing)
    {
        var document = Network().DataSnapshot.GetDocument();
        if (missing) At(document, depth).Remove("name"); else At(document, depth)["name"] = false;
        var snapshot = RoamingNetworkDataSnapshot.Capture(document, 0); var bytes = snapshot.ToCanonicalJSON();
        Exception? expected = null; RoamingNetwork? preceding = null;
        try { preceding = Original(snapshot); } catch (Exception error) { expected = error; }
        if (expected is null) Same(preceding!, RoamingNetwork.ParseSnapshot(snapshot));
        else
        {
            var actual = Assert.Catch(() => RoamingNetwork.ParseSnapshot(snapshot))!;
            Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        }
        Assert.That(snapshot.ToCanonicalJSON(), Is.EqualTo(bytes));
        Same(Network(), RoamingNetwork.ParseSnapshot(Network().DataSnapshot));
    }

    public static IEnumerable<TestCaseData> EntityCases()
    {
        var snapshot = Network().DataSnapshot;
        foreach (var type in snapshot.Entities.Keys.Select(key => key.Type).Distinct().Order())
            foreach (var missing in new[] { false, true }) yield return new TestCaseData(type, missing);
    }

    [TestCaseSource(nameof(EntityCases))]
    public void Any_changed_or_missing_owned_identity_disables_reuse(InfrastructureEntityType type, Boolean missing)
    {
        var snapshot = Network().DataSnapshot; var document = snapshot.GetDocument(); JObject? selected = null;
        POIRepresentation.Visit(document, nameof(RoamingNetwork), "", (node, kind, _) => { if (kind == type.ToString() && selected is null) selected = node; });
        var field = InfrastructureChangeSchema.IdField(type);
        if (missing) selected!.Remove(field); else selected![field] = "changed";
        Assert.That(snapshot.MatchesCompletedDocument(document, snapshot.Revision, snapshot.AppliedChangeSetId), Is.False);
        Assert.That(snapshot.MatchesCompletedDocument(snapshot.GetDocument(), snapshot.Revision, snapshot.AppliedChangeSetId), Is.True);
    }

    [TestCase("1.0")] [TestCase("1e0")] [TestCase("-0")] [TestCase("12345678901234567890123456789")]
    public void Numeric_tokens_customer_arrays_and_SI_strings_match_the_preceding_capture(String number)
    {
        var document = Network().DataSnapshot.GetDocument();
        document["customData"] = InfrastructureJson.ReadObject("{\"number\":" + number + ",\"array\":[1,2,3],\"ETags\":[\"customer\"],\"contentProfile\":\"customer\",\"reading\":\"250 kW\"}");
        var snapshot = RoamingNetworkDataSnapshot.Capture(document, 0);
        var actual = RoamingNetwork.ParseSnapshot(snapshot); Same(Original(snapshot), actual);
        Assert.That(actual.DataSnapshot, Is.SameAs(snapshot));
        var changed = snapshot.GetDocument(); ((JArray)changed["customData"]!["array"]!).RemoveAt(1);
        Assert.That(snapshot.MatchesCompletedDocument(changed, 0, null), Is.False);
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Reuse_rejects_extra_missing_duplicate_children_wrong_versions_and_changed_lexemes(Int32 variant)
    {
        var snapshot = Network().DataSnapshot; var document = snapshot.GetDocument();
        if (variant == 0) document["unrecognized"] = true;
        if (variant == 1) document.Remove("chargingStationOperators");
        if (variant == 2) ((JArray)document["chargingStationOperators"]!).Add(document["chargingStationOperators"]![0]!.DeepClone());
        if (variant == 3) Assert.That(snapshot.MatchesCompletedDocument(document, 9, "other"), Is.False);
        if (variant == 4)
        {
            ((JObject)document["customData"]!).Remove("decimal");
            document["customData"]!["decimal"] = InfrastructureJson.ReadObject("{\"value\":1.2500}")["value"];
        }
        if (variant != 3) Assert.That(snapshot.MatchesCompletedDocument(document, 0, null), Is.False);
    }

    [Test, Combinatorial]
    public void Every_archive_route_preserves_all_branches_peers_and_separate_runtime(
        [Values(1, 2, 3, 4)] Int32 profile, [Values("json", "cbor", "bootstrap")] String route)
    {
        var bytes = Bytes(profile); var calls = 0; var batchCalls = 0;
        Boolean Trust(RoamingNetworkCommit value, RoamingNetworkChangeSetSignature peer) { calls++; return VerifyCommit(value, peer); }
        Boolean BatchTrust(RoamingNetworkChangeSet value, RoamingNetworkChangeSetSignature peer) { batchCalls++; return VerifyBatch(value, peer); }
        using var source = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        source.ApplyRuntimeUpdate(new(source.Head.Network.Id.ToString(), POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
            POIRuntimeStatusKind.Status, new("charging", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        using var actual = route switch {
            "json" => RoamingNetworkHistory.Parse(source.ToJSON(), BatchTrust, Trust, authorizeSnapshotBoundary: _ => true),
            "bootstrap" => RoamingNetworkHistory.RestoreBootstrap(bytes, source.CreateBootstrap().Manifest, BatchTrust, Trust, null, _ => true, new()),
            _ => RoamingNetworkHistory.ParseCBOR(bytes, BatchTrust, Trust, authorizeSnapshotBoundary: _ => true)
        };
        Assert.That(actual.ToCBOR(), Is.EqualTo(bytes));
        Assert.That(calls, Is.EqualTo(source.Commits.Sum(commit => commit.Signatures.Length) + source.LookupCommit(source.AnchorId).Commit!.Signatures.Length));
        Assert.That(batchCalls, Is.EqualTo(source.Commits.Sum(commit => commit.ChangeSet?.Signatures.Length ?? 0)));
        foreach (var commit in source.Commits)
            Assert.That(actual.GetSnapshot(commit.Id).ToCanonicalJSON(), Is.EqualTo(source.GetSnapshot(commit.Id).ToCanonicalJSON()));
        Same(Original(actual.GetSnapshot(actual.Head.Id)), actual.Head.Network);
        Assert.That(actual.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
        Assert.That(source.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        calls = 0; Assert.Catch(() => { using var rejected = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, (_, _) => { calls++; return false; }, authorizeSnapshotBoundary: _ => true); });
        Assert.That(calls, Is.EqualTo(1));
    }

    [TestCase(false)] [TestCase(true)]
    public void Anchor_only_heads_keep_the_freshly_validated_root_and_original_peer_envelopes(Boolean boundary)
    {
        using var source = History(); var root = Sign(source.Head.Commit);
        Assert.That(source.TryStoreCommit(root, out _), Is.True);
        var snapshot = Sign(RoamingNetworkCommit.CreateSnapshot(root, source.Head.Snapshot, InteropFixture.Time));
        using var anchored = boundary ? RoamingNetworkHistory.FromSnapshot(root.Id, snapshot, _ => true, VerifyCommit, VerifyBatch) : null;
        var bytes = (anchored ?? source).ToCBOR(); var calls = 0;
        using var first = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, (value, peer) => { calls++; return VerifyCommit(value, peer); }, authorizeSnapshotBoundary: _ => true);
        using var second = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Assert.That(first.ToCBOR(), Is.EqualTo(bytes)); Assert.That(calls, Is.EqualTo(boundary ? 2 : 4));
        Assert.That(first.Head.Snapshot, Is.SameAs(first.GetSnapshot(first.AnchorId)));
        Assert.That(first.Head.Network, Is.Not.SameAs(second.Head.Network)); Assert.That(first.Head.Network.EVSEs.Single(), Is.Not.SameAs(second.Head.Network.EVSEs.Single()));
        first.ApplyRuntimeUpdate(new(first.Head.Network.Id.ToString(), POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
            POIRuntimeStatusKind.Status, new("charging", InteropFixture.Time), mode: POIRuntimeUpdateMode.ReplaceHistory));
        Assert.That(second.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
    }

    [Test]
    public async Task Independent_concurrent_models_share_only_the_immutable_snapshot()
    {
        var snapshot = Network().DataSnapshot; var original = snapshot.ToCanonicalCBOR();
        var values = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => RoamingNetwork.ParseSnapshot(snapshot))));
        Assert.That(values.Distinct(ReferenceEqualityComparer.Instance).Count(), Is.EqualTo(8));
        Assert.That(values.Select(value => value.EVSEs.Single()).Distinct(ReferenceEqualityComparer.Instance).Count(), Is.EqualTo(8));
        foreach (var value in values) { Assert.That(value.DataSnapshot, Is.SameAs(snapshot)); Assert.That(value.ToCanonicalCBOR(), Is.EqualTo(original)); }
    }

    [TestCase(false)] [TestCase(true)]
    public void Boundary_reconstruction_keeps_domain_failure_before_trust_and_completion_failure_after_trust(Boolean incomplete)
    {
        using var source = History(); var document = source.Head.Snapshot.GetDocument();
        if (incomplete) document.Remove("created"); else document["name"] = false;
        var snapshot = RoamingNetworkDataSnapshot.Capture(document, 0);
        var parent = RoamingNetworkCommit.CreateCheckpoint(snapshot);
        var commit = Sign(RoamingNetworkCommit.CreateSnapshot(parent, snapshot, InteropFixture.Time));
        var calls = 0; var boundaries = 0;
        var error = Assert.Catch(() => { using var rejected = RoamingNetworkHistory.FromSnapshot(parent.Id, commit,
            _ => { boundaries++; return true; }, (value, peer) => { calls++; return VerifyCommit(value, peer); }); })!;
        if (incomplete) { Assert.That(calls, Is.EqualTo(2)); Assert.That(boundaries, Is.EqualTo(1)); }
        else
        {
            var expected = Assert.Catch(() => Original(snapshot))!;
            Assert.That(error.GetType(), Is.EqualTo(expected.GetType())); Assert.That(error.Message, Is.EqualTo(expected.Message));
            Assert.That(calls, Is.Zero); Assert.That(boundaries, Is.Zero);
        }
        using var retry = RoamingNetworkHistory.FromSnapshot(source.CheckpointId,
            Sign(RoamingNetworkCommit.CreateSnapshot(source.Head.Commit, source.Head.Snapshot, InteropFixture.Time)), _ => true, VerifyCommit);
        Assert.That(retry.Head.Snapshot.ETags, Is.EqualTo(source.Head.Snapshot.ETags));
    }

    [TestCase(false)] [TestCase(true)]
    public void Changed_bookkeeping_or_property_spellings_retain_exact_preceding_capture(Boolean escaped)
    {
        var source = Network().DataSnapshot;
        var change = escaped ? RoamingNetworkChange.UpdateProperty("RoamingNetwork", source.Root.Id, "description", null,
            Value("{ \"en\": \"Escaped \\u003Cvalue\\u003E\" }")) : Power("175 kW");
        var snapshot = source.ApplyChangeSet(Batch(source, "reconstruction-normalization", change));
        var expected = Original(snapshot); var actual = RoamingNetwork.ParseSnapshot(snapshot);
        Same(expected, actual);
        if (snapshot.Entities.Any(item => item.Value.Properties.Any(property =>
            property.Value.GetRawText() != expected.DataSnapshot.Entities[item.Key].Properties[property.Key].GetRawText())))
            Assert.That(actual.DataSnapshot, Is.Not.SameAs(snapshot));
    }

    [TestCase(0)] [TestCase(55)] [TestCase(58)] [TestCase(60)]
    public void Deep_content_preserves_the_preceding_export_identity_and_model_failure_order(Int32 depth)
    {
        JToken value = JValue.CreateNull();
        for (var index = 0; index < depth; index++) value = new JArray(value);
        var document = Network().DataSnapshot.GetDocument(); document["customData"] = new JObject { ["deep"] = value };
        var snapshot = RoamingNetworkDataSnapshot.Capture(document, 0);
        RoamingNetwork? expected = null; Exception? failure = null;
        try { expected = Original(snapshot); } catch (Exception error) { failure = error; }
        if (failure is null) Same(expected!, RoamingNetwork.ParseSnapshot(snapshot));
        else
        {
            var actual = Assert.Catch(() => RoamingNetwork.ParseSnapshot(snapshot))!;
            Assert.That(actual.GetType(), Is.EqualTo(failure.GetType())); Assert.That(actual.Message, Is.EqualTo(failure.Message));
        }
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Cancelled_verification_releases_private_history_and_retry_uses_fresh_runtime(Int32 profile)
    {
        var bytes = Bytes(profile); using var input = new MemoryStream(bytes); using var cancel = new CancellationTokenSource();
        var calls = 0;
        var error = Assert.Throws<OperationCanceledException>(() => { using var rejected = RoamingNetworkHistory.ParseCBOR(input, VerifyBatch,
            (value, peer) => { calls++; cancel.Cancel(); return VerifyCommit(value, peer); }, authorizeSnapshotBoundary: _ => true, cancellationToken: cancel.Token); })!;
        Assert.That(error.CancellationToken, Is.EqualTo(cancel.Token)); Assert.That(calls, Is.EqualTo(1)); Assert.That(input.CanRead, Is.True);
        using var retry = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Assert.That(retry.ToCBOR(), Is.EqualTo(bytes)); Assert.That(retry.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
    }

    [Test]
    public void A_rejected_peer_preserves_atomic_publication_and_retry_rechecks_all_peers()
    {
        var reject = true; var calls = 0;
        using var history = new RoamingNetworkHistory(RoamingNetwork.ParseSnapshot(Network().DataSnapshot), VerifyBatch,
            (value, peer) => { calls++; return (!reject || peer.KeyId != "fixture-bob") && VerifyCommit(value, peer); });
        var head = history.Head; var bytes = history.ToCBOR();
        var commit = Sign(history.PrepareCommit(head.Id, Sign(Batch(head.Snapshot, "reconstruction-publish", Power("175 kW")))));
        Assert.That(history.TryPublish(head.Id, commit, out _), Is.False); Assert.That(calls, Is.EqualTo(2));
        Assert.That(history.Head, Is.SameAs(head)); Assert.That(history.ToCBOR(), Is.EqualTo(bytes));
        reject = false; calls = 0; Assert.That(history.TryPublish(head.Id, commit, out _), Is.True); Assert.That(calls, Is.EqualTo(2));
    }
}
