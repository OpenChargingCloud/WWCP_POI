/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class SnapshotBoundaryTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Boundary_recovery_retains_original_root_and_chain_then_rechecks_fresh_policy(Boolean cbor)
    {
        using var sender = History(); var snapshot = LinearHistory(sender);
        Status(sender, EvseTarget, "charging");
        using var replica = RoamingNetworkHistory.FromSnapshot(sender.CheckpointId, snapshot, AcceptChain(sender), VerifyCommit, VerifyBatch);
        Assert.That(replica.CheckpointId, Is.EqualTo(sender.CheckpointId));
        Assert.That(replica.AnchorId, Is.EqualTo(snapshot.Id)); Assert.That(replica.HasCompleteAncestry, Is.False);
        Assert.That(replica.GetCommit(snapshot.Id).Parents, Is.EqualTo(snapshot.Parents));
        Assert.That(replica.GetCommit(snapshot.Id).ToCBOR(), Is.EqualTo(snapshot.ToCBOR()));
        Assert.That(replica.LookupCommit(snapshot.Parents[0]).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Unknown));
        Assert.That(replica.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
        Assert.That(sender.TryCreateCommitPack(replica.GetReplicationState(), sender.Head.Id, out var pack, out var export), Is.True, export.Error);
        Assert.That(replica.TryImportCommitPack(PageTransport(pack!, replica, cbor), out var imported), Is.True, imported.Error);
        Assert.That(replica.TryAdoptHead(snapshot.Id, sender.Head.Id, out var adopted, adopt: true), Is.True, adopted.Error);
        using var recovered = Restore(replica, cbor);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(replica.ToCBOR()));
        Assert.That(recovered.Head.Id, Is.EqualTo(sender.Head.Id));
        Assert.That(() => { if (cbor) RoamingNetworkHistory.ParseCBOR(replica.ToCBOR(), VerifyBatch, VerifyCommit);
                           else RoamingNetworkHistory.Parse(replica.ToJSON(), VerifyBatch, VerifyCommit); }, Throws.ArgumentException);
        Assert.That(() => { if (cbor) RoamingNetworkHistory.ParseCBOR(replica.ToCBOR(), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => false);
                           else RoamingNetworkHistory.Parse(replica.ToJSON(), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => false); }, Throws.ArgumentException);
    }

    [TestCase("unsigned")]
    [TestCase("verifier")]
    [TestCase("policy")]
    [TestCase("wrong-chain")]
    [TestCase("rollback")]
    public void Boundary_creation_requires_original_signed_root_and_explicit_chain_and_rollback_authority(String kind)
    {
        using var history = History(); var snapshot = LinearHistory(history);
        var checkpoint = kind == "wrong-chain" ? new RoamingNetworkCommitId(ETag.Compute(ETagFormat.JSON, [42])) : history.CheckpointId;
        var root = kind == "unsigned" ? snapshot.WithSignatures([]) : snapshot;
        Func<RoamingNetworkSnapshotBoundary, Boolean> policy = kind == "policy" ? _ => false : AcceptChain(history);
        if (kind == "rollback") policy = boundary => boundary.SnapshotCommit.Revision > snapshot.Revision;
        Assert.That(() => RoamingNetworkHistory.FromSnapshot(checkpoint, root, policy,
            kind == "verifier" ? (_, _) => false : VerifyCommit, VerifyBatch), Throws.ArgumentException);
    }

    [Test]
    public void Revoked_boundary_blocks_static_mutations_but_local_runtime_delivery_stays_available()
    {
        using var sender = History(); var snapshot = LinearHistory(sender); var allowed = true;
        using var replica = RoamingNetworkHistory.FromSnapshot(sender.CheckpointId, snapshot,
            boundary => allowed && boundary.Anchor == snapshot.Id, VerifyCommit, VerifyBatch);
        allowed = false; var original = new Observation(replica);
        Assert.That(replica.TryPublish(snapshot.Id, sender.Head.Commit, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.InvalidCommit)); original.AssertUnchanged(replica);
        Assert.That(() => replica.PrepareSnapshot(snapshot.Id, InteropFixture.Time), Throws.ArgumentException);
        Status(replica, EvseTarget, "charging");
        Assert.That(replica.Head.Id, Is.EqualTo(snapshot.Id));
        Assert.That(replica.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Incremental_pages_are_bounded_independently_of_large_anchor_state_and_keep_local_root_peers(Boolean cbor)
    {
        using var sender = History();
        // The full sender has no snapshot peers; this receiver independently signs and authorizes it.
        var unsigned = sender.PrepareSnapshot(sender.Head.Id, InteropFixture.Time.AddDays(10),
            ImmutableDictionary<String, String>.Empty.Add("en", new String('s', 16000)));
        Publish(sender, unsigned); var root = Sign(unsigned);
        using var replica = RoamingNetworkHistory.FromSnapshot(sender.CheckpointId, root, AcceptChain(sender), VerifyCommit, VerifyBatch);
        Publish(sender, Prepare(sender, sender.Head.Id, "page-one", Power("150 kW")));
        Publish(sender, Prepare(sender, sender.Head.Id, "page-two", Rename("Continued")));
        var target = sender.Head.Id; var pages = 0;
        while (replica.LookupCommit(target).Availability != RoamingNetworkCommitAvailability.Retained)
        {
            Assert.That(sender.TryCreateCommitPack(replica.GetReplicationState(), target, out var probe, out var report, maxCommits: 1), Is.True, report.Error);
            var bound = Size(probe!);
            Assert.That(bound, Is.LessThan(root.ToCBOR().Length));
            Assert.That(sender.TryCreateCommitPack(replica.GetReplicationState(), target, out var pack, out report, maxCommits: 1, maxBytes: bound), Is.True, report.Error);
            Assert.That(Size(pack!), Is.EqualTo(bound)); Assert.That(pack!.Commits, Has.Length.EqualTo(1));
            Assert.That(pack.AnchorCommit.Signatures, Is.Empty);
            Assert.That(sender.TryCreateCommitPack(replica.GetReplicationState(), target, out _, out _, maxCommits: 1, maxBytes: bound - 1), Is.False);
            Assert.That(() => { if (cbor) RoamingNetworkCommitPack.ParseCBOR(pack.ToCBOR()); else RoamingNetworkCommitPack.Parse(pack.ToJSON()); }, Throws.ArgumentException);
            var original = replica.Head;
            Assert.That(replica.TryImportCommitPack(PageTransport(pack, replica, cbor), out var imported), Is.True, imported.Error);
            Assert.That(replica.Head.Id, Is.EqualTo(original.Id)); Assert.That(replica.Head.Network, Is.SameAs(original.Network));
            Assert.That(replica.GetCommit(root.Id).Signatures, Has.Length.EqualTo(2));
            Assert.That(++pages, Is.LessThan(5));
        }
        Assert.That(pages, Is.EqualTo(2));
        Assert.That(replica.TryAdoptHead(root.Id, target, out var adopted, adopt: true), Is.True, adopted.Error);
        Assert.That(replica.Head.Snapshot.ETags, Is.EqualTo(sender.Head.Snapshot.ETags));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Snapshot_bootstrap_resumes_and_activation_rechecks_authority_before_original_suffix_replay(Boolean cbor)
    {
        using var sender = History(); var snapshot = LinearHistory(sender); using var staging = new ArchiveDirectory();
        var source = sender.CreateSnapshotBootstrap(snapshot.Id, chunkBytes: 256); var policy = AcceptChain(sender);
        var manifest = cbor ? RoamingNetworkBootstrapManifest.ParseCBOR(source.Manifest.ToCBOR()) : RoamingNetworkBootstrapManifest.Parse(source.Manifest.ToJSON());
        Assert.That(manifest.WireProfile, Is.EqualTo(RoamingNetworkBootstrapManifest.BoundaryProfile));
        using (var first = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, manifest))
            Assert.That(first.TryAcceptChunk(source.CreateChunk(0), out var accepted), Is.True, accepted.Error);
        using var resumed = RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, manifest.Id);
        Assert.That(resumed.NextChunk, Is.EqualTo(1)); Finish(resumed, source, cbor);
        Assert.That(resumed.TryActivate(manifest.Id, out var absent, out var result, verifyBatchSignature: VerifyBatch,
            verifyCommitSignature: VerifyCommit), Is.False); Assert.That(absent, Is.Null);
        Assert.That(resumed.TryActivate(manifest.Id, out absent, out result, verifyBatchSignature: VerifyBatch,
            verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: policy), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.ActivationAvailable)); Assert.That(absent, Is.Null);
        Assert.That(resumed.TryActivate(manifest.Id, out absent, out result, activate: true, verifyBatchSignature: VerifyBatch,
            verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: _ => false), Is.False); Assert.That(absent, Is.Null);
        Assert.That(resumed.TryActivate(manifest.Id, out var activated, out result, activate: true, verifyBatchSignature: VerifyBatch,
            verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: policy), Is.True, result.Error);
        using var replica = activated!;
        Assert.That(replica.Head.Id, Is.EqualTo(sender.Head.Id)); Assert.That(replica.AnchorId, Is.EqualTo(snapshot.Id));
        var archive = Enumerable.Range(0, manifest.ChunkCount).SelectMany(index => source.CreateChunk(index).Data).ToArray();
        Assert.That(replica.ToCBOR(), Is.EqualTo(archive));
    }

    [Test]
    public void Old_merge_parent_blocks_snapshot_export_but_a_later_snapshot_covers_the_merged_DAG()
    {
        using var history = History(); var root = history.Head.Id;
        var right = Prepare(history, root, "old-right", Rename("Right")); Store(history, right);
        Publish(history, Prepare(history, root, "left", Power("150 kW")));
        var snapshot = Snapshot(history); Publish(history, snapshot);
        var merged = Merge(history, snapshot, right); Publish(history, merged);
        var before = new Observation(history);
        Assert.That(() => history.CreateSnapshotBootstrap(snapshot.Id), Throws.ArgumentException); before.AssertUnchanged(history);
        var receiver = new RoamingNetworkReplicationState(history.CheckpointId, snapshot.Id, [snapshot.Id], snapshot.Id);
        Assert.That(history.TryCreateCommitPack(receiver, merged.Id, out _, out var report), Is.False);
        Assert.That(report.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.HistoryRequired)); Assert.That(report.ProposedBoundary, Is.Null);
        var later = Snapshot(history, 11); Publish(history, later);
        Assert.That(history.CreateSnapshotBootstrap(later.Id).Manifest.Head, Is.EqualTo(later.Id));
    }

    [Test]
    public void Suffix_merges_preserve_runtime_and_report_unavailable_earlier_bases()
    {
        using var full = History(); var snapshot = LinearHistory(full);
        using var history = RoamingNetworkHistory.FromSnapshot(full.CheckpointId, snapshot, AcceptChain(full), VerifyCommit, VerifyBatch);
        var left = Prepare(history, snapshot.Id, "left", Power("175 kW")); Publish(history, left);
        var right = Prepare(history, snapshot.Id, "right", Rename("Right")); Store(history, right);
        Status(history, EvseTarget, "charging"); Publish(history, Merge(history, left, right));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        var before = new Observation(history);
        Assert.That(history.TryMerge(left.Id, right.Id, out _, out var result, commonAncestor: snapshot.Parents[0]), Is.False);
        Assert.That(result.Status, Is.EqualTo(RoamingNetworkMergeStatus.HistoryRequired));
        Assert.That(result.MissingCommits, Does.Contain(snapshot.Parents[0])); before.AssertUnchanged(history);
    }
}
