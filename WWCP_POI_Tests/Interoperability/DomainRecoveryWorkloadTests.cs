/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Globalization;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using WWCP_POI_Benchmarks;
using static WWCP_POI_Benchmarks.DomainRecoveryFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class DomainRecoveryWorkloadTests
{
    private sealed class DirectoryLease : IDisposable
    {
        internal String PathName { get; } = Path.Combine(Path.GetTempPath(), "wwcp-poi-domain-test-" + Guid.NewGuid().ToString("N"));
        internal DirectoryLease() => Directory.CreateDirectory(PathName);
        public void Dispose()
        {
            foreach (var file in Directory.EnumerateFiles(PathName)) File.Delete(file);
            Directory.Delete(PathName);
        }
    }

    [Test, Combinatorial]
    public void Prepared_workloads_have_exact_reproducible_bytes_peers_branches_and_real_inventories(
        [Values(16, 64)] Int32 evses, [Values(1, 2, 3, 4)] Int32 profile)
    {
        using var first = new DomainRecoveryFixture(evses, profile); using var second = new DomainRecoveryFixture(evses, profile);
        Assert.That(second.History.ToCBOR(), Is.EqualTo(first.History.ToCBOR())); Assert.That(second.History.ToJSON(), Is.EqualTo(first.History.ToJSON()));
        Assert.That(second.Inventory, Is.EqualTo(first.Inventory)); Assert.That(first.Inventory.EVSEs, Is.EqualTo(evses));
        Assert.That(first.Inventory.Pools, Is.EqualTo(evses / 8)); Assert.That(first.Inventory.Stations, Is.EqualTo(evses / 4));
        Assert.That(first.Inventory.MeterSlots, Is.EqualTo(evses / 8 * 3 + evses / 4 * 2 + evses));
        Assert.That(first.Inventory.SoftwareAssignments, Is.EqualTo(first.Inventory.MeterSlots * 2));
        Assert.That(first.Inventory.ReferenceEdges, Is.GreaterThan(evses * 8));
        Assert.That(first.Inventory.TariffElements, Is.EqualTo(8)); Assert.That(first.Inventory.PriceComponents, Is.EqualTo(24));
        Assert.That(first.Inventory.Restrictions, Is.EqualTo(16)); Assert.That(first.Inventory.MergeCommits, Is.EqualTo(1));
        Assert.That(first.Inventory.ElementOperations, Is.GreaterThanOrEqualTo(4));
        Assert.That(first.History.RetentionReceipts.Length, Is.EqualTo(profile == 4 ? 2 : 0));
        Assert.That(first.History.HasCompleteAncestry, Is.EqualTo(profile <= 2));
        foreach (var commit in first.History.Commits)
        {
            Assert.That(commit.Signatures.Select(peer => peer.KeyId), Is.EquivalentTo(new[] { "domain-a", "domain-b" }));
            Assert.That(commit.Signatures.All(peer => VerifyCommit(commit, peer)), Is.True);
            if (commit.ChangeSet is { } batch) Assert.That(batch.Signatures.All(peer => VerifyBatch(batch, peer)), Is.True);
        }
    }

    [Test, Combinatorial]
    public void All_profiles_recover_full_domain_data_and_independent_runtime_through_public_inputs(
        [Values(1, 2, 3, 4)] Int32 profile, [Values("json", "cbor", "memory", "spool")] String input)
    {
        using var fixture = new DomainRecoveryFixture(16, profile); using var work = new DirectoryLease();
        var bytes = fixture.History.ToCBOR(); var etags = fixture.History.Head.Snapshot.ETags;
        using var source = new MemoryStream(bytes, writable: false);
        using var token = new CancellationTokenSource();
        using var restored = input switch {
            "json" => RoamingNetworkHistory.Parse(fixture.History.ToJSON(), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: fixture.Boundary),
            "cbor" => RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: fixture.Boundary),
            _ => RoamingNetworkHistory.ParseCBOR(source, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: fixture.Boundary,
                readOptions: new(input == "spool" ? 0 : Int32.MaxValue, work.PathName), cancellationToken: token.Token)
        };
        fixture.CheckRecovered(restored); Assert.That(restored.ToCBOR(), Is.EqualTo(bytes));
        Assert.That(source.CanRead, Is.True); Assert.That(Directory.GetFiles(work.PathName, "*.tmp-*"), Is.Empty);
        Assert.That(restored.Head.Snapshot.ETags, Is.EqualTo(etags));
        token.Cancel(); var parent = restored.Head.Id;
        var batch = Sign(restored.Head.Snapshot.CreateChangeSet("late-token", Epoch.AddDays(2),
            [Edit("TransparencySoftwareCertificate", "approval-1", "documentNumber", Text("Later publication"))]));
        var commit = Sign(restored.PrepareCommit(parent, batch));
        Assert.That(restored.TryPublish(parent, commit, out var result), Is.True, result.Error);
        Assert.That(fixture.History.ToCBOR(), Is.EqualTo(bytes)); CheckGraph(restored.Head.Network);
    }

    [Test, Combinatorial]
    public void Bootstrap_domain_activation_rejects_fresh_trust_atomically_and_retries(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean file, [Values(false, true)] Boolean deny)
    {
        using var fixture = new DomainRecoveryFixture(16, profile); using var work = new DirectoryLease(); using var capture = new DirectoryLease();
        var source = fixture.History.CreateBootstrap(4096); var path = Path.Combine(work.PathName, "active.cbor"); var calls = 0;
        using var receiver = RoamingNetworkBootstrapReceiver.Create(work.PathName, source.Manifest);
        for (var index = 0; index < source.Manifest.ChunkCount; index++)
            Assert.That(receiver.TryAcceptChunk(source.CreateChunk(index), out var accepted), Is.True, accepted.Error);
        var options = new RoamingNetworkArchiveReadOptions(file ? 0 : Int32.MaxValue, capture.PathName);
        Boolean CommitTrust(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer)
        { calls++; return !deny && VerifyCommit(commit, peer); }
        var activated = receiver.TryActivate(source.Manifest.Id, out var history, out var result, activate: true,
            archivePath: path, verifyBatchSignature: VerifyBatch, verifyCommitSignature: CommitTrust,
            authorizeSnapshotBoundary: fixture.Boundary, readOptions: options);
        if (deny)
        {
            Assert.That(activated, Is.False); Assert.That(history, Is.Null); Assert.That(File.Exists(path), Is.False);
            Assert.That(receiver.NextChunk, Is.EqualTo(source.Manifest.ChunkCount)); Assert.That(calls, Is.GreaterThan(0));
            deny = false;
            Assert.That(receiver.TryActivate(source.Manifest.Id, out history, out result, activate: true,
                archivePath: path, verifyBatchSignature: VerifyBatch, verifyCommitSignature: CommitTrust,
                authorizeSnapshotBoundary: fixture.Boundary, readOptions: options), Is.True, result.Error);
        }
        else Assert.That(activated, Is.True, result.Error);
        using (history!) { fixture.CheckRecovered(history!); Assert.That(history!.ToCBOR(), Is.EqualTo(fixture.History.ToCBOR())); }
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(fixture.History.ToCBOR()));
        Assert.That(Directory.GetFiles(capture.PathName, "*.tmp-*"), Is.Empty);
        using var lease = new FileStream(path + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [TestCase(16)] [TestCase(64)]
    public void Nested_values_and_shared_catalogs_are_detached_from_the_supplied_document(Int32 evses)
    {
        var document = CreateDocument(evses); var network = RoamingNetwork.Parse(document); var before = network.ToCanonicalCBOR();
        document["transparencySoftware"]![0]!["vendor"] = "Caller mutation";
        document["transparencySoftwareCertificates"]![0]!["issuer"] = "Caller issuer";
        document["chargingStationOperators"]![0]!["chargingTariffs"]![0]!["elements"]![0]!["priceComponents"]![0]!["price"] = 999m;
        document["parkingOperators"]![0]!["parkingSpaces"]![0]!["chargingStationIds"] = new JArray();
        Assert.That(network.ToCanonicalCBOR(), Is.EqualTo(before)); CheckGraph(network);
        var prices = network.ChargingStationOperators.Single().ChargingTariffs.First().ToJSON();
        prices["elements"]![0]!["restrictions"]![0]!["maxPower"] = "1 W";
        Assert.That(network.ToCanonicalCBOR(), Is.EqualTo(before));
    }

    [Test, Combinatorial]
    public void Invalid_reference_and_tariff_edits_leave_all_history_and_runtime_unchanged(
        [Values(1, 2, 3, 4)] Int32 profile, [Values("grid", "software", "certificate", "tariff", "parking", "restriction")] String kind)
    {
        using var fixture = new DomainRecoveryFixture(16, profile); var history = fixture.History;
        var before = history.ToCBOR(); var head = history.Head; var runtime = head.Network.ToJSONSnapshot().ToString();
        var change = kind switch {
            "grid" => RoamingNetworkChange.Remove("GridOperator", "DE*GRD"),
            "software" => RoamingNetworkChange.Remove("TransparencySoftware", "verifier-1"),
            "certificate" => RoamingNetworkChange.Remove("TransparencySoftwareCertificate", "approval-1"),
            "tariff" => RoamingNetworkChange.Remove("ChargingTariff", "DE*ABC*T2"),
            "parking" => RoamingNetworkChange.Remove("ParkingProduct", "long-stay"),
            _ => Edit("ChargingTariff", "DE*ABC*T1", "elements", DomainRecoveryFixture.Json(JArray.Parse("""[{"priceComponents":[{"type":"ENERGY","price":1,"stepSize":"0 Wh"}]}]""")))
        };
        Assert.Throws<RoamingNetworkChangeSetException>(() => fixture.Prepare(head.Id, change));
        Assert.That(history.Head, Is.SameAs(head)); Assert.That(history.ToCBOR(), Is.EqualTo(before));
        Assert.That(history.Head.Network.ToJSONSnapshot().ToString(), Is.EqualTo(runtime)); Assert.That(Describe(history), Is.EqualTo(fixture.Inventory));
    }

    [Test, Combinatorial]
    public void Invalid_extra_signature_peer_rejects_publication_atomically_before_valid_retry(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean batchPeer)
    {
        using var fixture = new DomainRecoveryFixture(16, profile); var history = fixture.History; var head = history.Head;
        var bytes = history.ToCBOR(); var good = fixture.Prepare(head.Id, Edit("TransparencySoftwareCertificate", "approval-1", "documentNumber", Text("Retry")));
        var peer = new RoamingNetworkChangeSetSignature("Ed25519", "unknown-peer", Convert.ToBase64String(new Byte[64]),
            batchPeer ? RoamingNetworkChangeSet.SigningProfile : RoamingNetworkCommit.SigningProfile);
        var bad = batchPeer ? good.WithChangeSet(good.ChangeSet!.WithSignature(peer)) : good.WithSignature(peer);
        Assert.That(history.TryPublish(head.Id, bad, out var result), Is.False);
        Assert.That(history.Head, Is.SameAs(head)); Assert.That(history.ToCBOR(), Is.EqualTo(bytes));
        Assert.That(history.TryPublish(head.Id, good, out result), Is.True, result.Error); CheckGraph(history.Head.Network);
        Assert.That(history.Head.Network.GridOperators.Single(grid => grid.Id.ToString() == "DE*GRD").Status.Value, Is.EqualTo(GridOperatorStatusTypes.Offline));
        using var restored = RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: fixture.Boundary);
        CheckGraph(restored.Head.Network); Assert.That(restored.Head.Id, Is.EqualTo(good.Id));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Prepared_merge_retains_both_branches_and_catalog_and_parking_results(Int32 profile)
    {
        using var fixture = new DomainRecoveryFixture(16, profile); var history = fixture.History;
        Assert.That(history.Head.Commit.Parents, Has.Length.EqualTo(2));
        Assert.That(history.Head.Network.GetTransparencySoftwareById(TransparencySoftware_Id.Parse("verifier-1"))!.Vendor, Is.EqualTo("Merged vendor"));
        Assert.That(history.Head.Network.ParkingProducts.Single(product => product.Id.ToString() == "short-stay").MinDuration, Is.EqualTo(TimeSpan.FromMinutes(15)));
        Assert.That(history.GetSnapshot(history.Head.Commit.Parents[0]).ETags, Is.Not.EqualTo(history.GetSnapshot(history.Head.Commit.Parents[1]).ETags));
        using var recovered = RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: fixture.Boundary);
        fixture.CheckRecovered(recovered);
    }

    [TestCase("en-US")] [TestCase("de-DE")] [TestCase("ar-EG")]
    public void Domain_values_and_identities_are_culture_independent(String culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture; using var baseline = new DomainRecoveryFixture(16, 2);
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture); using var actual = new DomainRecoveryFixture(16, 2);
            Assert.That(actual.History.ToCBOR(), Is.EqualTo(baseline.History.ToCBOR())); Assert.That(actual.Inventory, Is.EqualTo(baseline.Inventory));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }
}
