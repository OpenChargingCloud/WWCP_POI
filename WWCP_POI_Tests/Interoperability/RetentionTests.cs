/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text.Json.Nodes;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class RetentionTests
{
    [Test]
    public void Review_and_default_execution_preview_bind_exact_history_without_writes_or_approval_callbacks()
    {
        using var history = History(); var snapshot = LinearHistory(history); using var directory = new ArchiveDirectory();
        var before = new Observation(history); var plan = Plan(history, snapshot);
        Assert.That(plan.RetainedCommits, Is.EquivalentTo(new[] { snapshot.Id, history.Head.Id }));
        Assert.That(plan.PrunedCommits, Has.Length.EqualTo(2));
        Assert.That(plan.SourceArchiveETag, Is.EqualTo(ETag.Compute(ETagFormat.CBOR, history.ToCBOR())));
        Assert.That(history.GetRetentionSnapshots(history.Head.Id).Select(commit => commit.Id), Is.EqualTo(new[] { snapshot.Id }));
        Assert.That(history.GetRetentionSnapshots(snapshot.Parents[0]), Is.Empty);
        var called = false;
        Assert.That(history.TryExecuteRetention(plan, directory.ArchivePath, _ => { called = true; return false; },
            out var result, authorizeRetention: _ => { called = true; return false; }), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.Planned));
        Assert.That(result.Blockers, Is.Empty); Assert.That(called, Is.False);
        Assert.That(Directory.EnumerateFiles(directory.DirectoryPath), Is.Empty); before.AssertUnchanged(history);
    }

    [TestCase("unpublished")]
    [TestCase("base")]
    [TestCase("merge")]
    public void Old_branches_explicit_bases_and_crossing_merge_parents_block_pruning(String kind)
    {
        using var history = History(); var root = history.Head.Id; var snapshot = LinearHistory(history);
        RoamingNetworkCommit? branch = null;
        if (kind != "base")
        {
            branch = Prepare(history, root, "old-branch", RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "physicalReference", null, Value("\"old-right\"")));
            Store(history, branch);
            if (kind == "merge") Publish(history, Merge(history, history.Head.Commit, branch));
        }
        var before = new Observation(history);
        Assert.That(history.TryPlanRetention(snapshot.Id, InteropFixture.Time, out var absent, out var result,
            protectedCommits: kind == "base" ? [root] : null), Is.False);
        Assert.That(absent, Is.Null); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.Blocked));
        Assert.That(result.Blockers, Is.Not.Empty); Assert.That(result.Blockers.Any(item => item.RequiredHistory.Contains(root)), Is.True);
        if (kind == "unpublished") Assert.That(result.Blockers.Select(item => item.Tip), Does.Contain(branch!.Id));
        before.AssertUnchanged(history);
        if (kind == "merge")
        {
            var later = Snapshot(history, 11); Publish(history, later);
            Assert.That(Plan(history, later).RetainedCommits, Is.EqualTo(new[] { later.Id }));
        }
    }

    [TestCase("head-release")]
    [TestCase("non-frontier")]
    [TestCase("unknown-release")]
    [TestCase("protected-release")]
    [TestCase("old-cutoff")]
    [TestCase("unknown-base")]
    [TestCase("unsigned")]
    [TestCase("not-snapshot")]
    public void Invalid_boundaries_cutoffs_and_branch_releases_do_not_create_a_plan(String kind)
    {
        using var history = History(); var snapshot = LinearHistory(history);
        var unknown = new RoamingNetworkCommitId(ETag.Compute(ETagFormat.JSON, [41]));
        var selected = snapshot.Id; var cutoff = (RoamingNetworkCommitId?) null;
        RoamingNetworkCommitId[] released = [], protectedIds = [];
        if (kind == "head-release") released = [history.Head.Id];
        if (kind == "non-frontier") released = [snapshot.Id];
        if (kind == "unknown-release") released = [unknown];
        if (kind == "protected-release") { released = [history.Head.Id]; protectedIds = [history.Head.Id]; }
        if (kind == "old-cutoff") cutoff = snapshot.Parents[0];
        if (kind == "unknown-base") protectedIds = [unknown];
        if (kind == "unsigned") { var unsigned = history.PrepareSnapshot(history.Head.Id, InteropFixture.Time); Publish(history, unsigned); selected = unsigned.Id; }
        if (kind == "not-snapshot") selected = snapshot.Parents[0];
        var before = new Observation(history);
        Assert.That(history.TryPlanRetention(selected, InteropFixture.Time, out var plan, out var result,
            cutoffCommit: cutoff, archiveOnlyTips: released, protectedCommits: protectedIds), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.InvalidInput));
        Assert.That(plan, Is.Null); before.AssertUnchanged(history);
    }

    [Test]
    public void Released_unpublished_work_is_archived_and_pruning_preserves_the_exact_live_head_and_latest_runtime()
    {
        using var history = History(); var root = history.Head.Id; var snapshot = LinearHistory(history);
        var unpublished = Prepare(history, root, "unpublished", Power("200 kW")); Store(history, unpublished);
        var plan = Plan(history, snapshot, [unpublished.Id]); var head = history.Head; var archive = history.ToCBOR();
        Status(history, EvseTarget, "charging"); Status(history, PoolMeter, "error");
        head.Network.EVSEs.Single().MaxPowerRealTime = new(InteropFixture.Time.AddDays(2), Watt.Parse("75 kW"));
        var runtime = head.Network.ToJSONSnapshot().ToString(); using var directory = new ArchiveDirectory();
        var result = Prune(history, plan, directory.ArchivePath);
        Assert.That(history.Head, Is.SameAs(head)); Assert.That(history.Head.Network, Is.SameAs(head.Network));
        Assert.That(history.Head.Network.ToJSONSnapshot().ToString(), Is.EqualTo(runtime));
        Assert.That(history.AnchorId, Is.EqualTo(snapshot.Id)); Assert.That(history.CheckpointId, Is.EqualTo(root));
        Assert.That(history.Commits.Select(commit => commit.Id), Is.EquivalentTo(plan.RetainedCommits));
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(archive));
        Assert.That(result.Receipt!.PrunedCommits, Does.Contain(unpublished.Id));
        Assert.That(history.LookupCommit(unpublished.Id).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Archived));
        using var cold = RoamingNetworkHistory.ReadColdArchive(directory.ArchivePath, result.Receipt.SourceArchiveETag, VerifyBatch, VerifyCommit);
        Assert.That(cold.GetCommit(unpublished.Id).ToCBOR(), Is.EqualTo(unpublished.ToCBOR()));
        Assert.That(cold.Head.Id, Is.EqualTo(head.Id)); Assert.That(cold.ToCBOR(), Is.EqualTo(archive));
        Assert.That(cold.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
    }

    [TestCase("head")]
    [TestCase("branch")]
    [TestCase("head-peers")]
    [TestCase("root-peers")]
    public void Changed_head_inventory_or_peer_only_envelopes_invalidate_review_before_any_write(String kind)
    {
        using var history = History();
        Publish(history, Prepare(history, history.Head.Id, "before", Power("150 kW")));
        var signedRoot = Snapshot(history); var root = signedRoot.WithSignatures([signedRoot.Signatures[0]]); Publish(history, root);
        var signedHead = Prepare(history, history.Head.Id, "after", Rename("After"));
        Publish(history, signedHead.WithSignatures([signedHead.Signatures[0]]));
        var plan = Plan(history, root);
        if (kind == "head") Publish(history, Prepare(history, history.Head.Id, "advance", Power("175 kW")));
        if (kind == "branch") Store(history, Prepare(history, root.Id, "new-branch", Power("175 kW")));
        if (kind == "head-peers") Store(history, signedHead);
        if (kind == "root-peers") Store(history, signedRoot);
        var before = new Observation(history); using var directory = new ArchiveDirectory();
        Assert.That(history.TryExecuteRetention(plan, directory.ArchivePath, AcceptChain(history), out var result, prune: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(kind == "head" ? RoamingNetworkRetentionOutcome.HeadConflict : RoamingNetworkRetentionOutcome.InventoryChanged));
        Assert.That(Directory.EnumerateFiles(directory.DirectoryPath), Is.Empty); before.AssertUnchanged(history);
    }

    [TestCase("boundary")]
    [TestCase("retention")]
    [TestCase("exception")]
    [TestCase("reentry")]
    public void Approval_rejection_exception_and_reentry_leave_history_and_disk_unchanged(String kind)
    {
        using var history = History(); var snapshot = LinearHistory(history); var plan = Plan(history, snapshot);
        var before = new Observation(history); using var directory = new ArchiveDirectory();
        Boolean Boundary(RoamingNetworkSnapshotBoundary boundary)
        {
            if (kind == "exception") throw new InvalidOperationException("Injected approval exception");
            if (kind == "reentry")
            {
                Assert.That(history.TryExecuteRetention(plan, directory.ArchivePath, _ => true, out var nested, prune: true), Is.False);
                Assert.That(nested.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.Unavailable));
                Assert.That(() => history.Dispose(), Throws.TypeOf<InvalidOperationException>());
                return false;
            }
            return kind != "boundary";
        }
        Assert.That(history.TryExecuteRetention(plan, directory.ArchivePath, Boundary, out var result, prune: true,
            authorizeRetention: _ => kind != "retention"), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(kind == "exception" ? RoamingNetworkRetentionOutcome.InvalidInput : RoamingNetworkRetentionOutcome.Unauthorized));
        before.AssertUnchanged(history); Assert.That(Directory.EnumerateFiles(directory.DirectoryPath), Is.Empty);
    }

    [TestCase("BeforeTemporaryWrite")]
    [TestCase("TemporaryFileFlushed")]
    public void Active_write_failure_keeps_full_history_and_durable_cold_archive_then_retries_the_same_plan(String stage)
    {
        using var directory = new ArchiveDirectory(); var coldPath = Path.Combine(directory.DirectoryPath, "cold.cbor");
        using var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit);
        var snapshot = LinearHistory(history); var plan = Plan(history, snapshot);
        Status(history, EvseTarget, "charging"); var before = new Observation(history); var disk = File.ReadAllBytes(directory.ArchivePath);
        history.ArchiveWriteObserver = reached => { if (reached.ToString() == stage) throw new IOException("Injected retention failure"); };
        Assert.That(history.TryExecuteRetention(plan, coldPath, AcceptChain(history), out var result, prune: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.PersistenceFailure)); before.AssertUnchanged(history);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(disk)); Assert.That(File.ReadAllBytes(coldPath), Is.EqualTo(disk));
        Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        history.ArchiveWriteObserver = null; var head = history.Head; Prune(history, plan, coldPath);
        Assert.That(history.Head, Is.SameAs(head)); var expected = history.ToCBOR(); var policy = AcceptChain(history);
        history.Dispose();
        using var recovered = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: policy);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(expected)); Assert.That(recovered.RetentionReceipts, Has.Length.EqualTo(1));
        Assert.That(recovered.Head.Id, Is.EqualTo(head.Id));
        Publish(recovered, Prepare(recovered, recovered.Head.Id, "after-recovery", Power("175 kW")));
    }

    [TestCase("same-active")]
    [TestCase("active-lease")]
    [TestCase("mismatch")]
    [TestCase("leased")]
    [TestCase("directory")]
    public void Invalid_cold_destinations_and_competing_leases_cannot_delete_active_history(String kind)
    {
        using var directory = new ArchiveDirectory();
        using var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit);
        var snapshot = LinearHistory(history); var plan = Plan(history, snapshot);
        var path = Path.Combine(directory.DirectoryPath, "cold.cbor");
        if (kind == "same-active") path = directory.ArchivePath;
        if (kind == "active-lease") path = directory.ArchivePath + ".lock";
        if (kind == "directory") path = directory.DirectoryPath;
        if (kind == "mismatch") File.WriteAllBytes(path, [1, 2, 3]);
        using var lease = kind == "leased" ? new FileStream(path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read) : null;
        var before = new Observation(history); var disk = File.ReadAllBytes(directory.ArchivePath);
        Assert.That(history.TryExecuteRetention(plan, path, AcceptChain(history), out var result, prune: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.PersistenceFailure)); before.AssertUnchanged(history);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(disk));
        if (kind == "mismatch") Assert.That(File.ReadAllBytes(path), Is.EqualTo(new Byte[] { 1, 2, 3 }));
        Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Repeated_pruning_preserves_receipts_and_links_digest_checked_cold_archives(Boolean cbor)
    {
        using var history = History(); var originalRoot = history.Head.Id; var first = LinearHistory(history);
        using var directory = new ArchiveDirectory(); var firstPath = Path.Combine(directory.DirectoryPath, "first.cbor");
        Prune(history, Plan(history, first), firstPath);
        Publish(history, Prepare(history, history.Head.Id, "between", Power("175 kW")));
        var second = Snapshot(history, 11); Publish(history, second);
        Publish(history, Prepare(history, history.Head.Id, "later", Rename("Later")));
        var secondArchive = history.ToCBOR(); var secondPath = Path.Combine(directory.DirectoryPath, "second.cbor");
        Prune(history, Plan(history, second), secondPath);
        Assert.That(history.RetentionReceipts, Has.Length.EqualTo(2));
        Assert.That(File.ReadAllBytes(secondPath), Is.EqualTo(secondArchive));
        using var recovered = Restore(history, cbor);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(history.ToCBOR()));
        Assert.That(recovered.LookupCommit(originalRoot).Receipt!.Id, Is.EqualTo(history.RetentionReceipts[0].Id));
        Assert.That(recovered.LookupCommit(first.Id).Receipt!.Id, Is.EqualTo(history.RetentionReceipts[1].Id));
        using var coldSecond = RoamingNetworkHistory.ReadColdArchive(secondPath, history.RetentionReceipts[1].SourceArchiveETag,
            VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AcceptChain(history));
        Assert.That(coldSecond.GetCommit(first.Id).Id, Is.EqualTo(first.Id));
        Assert.That(coldSecond.LookupCommit(originalRoot).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Archived));
        using var coldFirst = RoamingNetworkHistory.ReadColdArchive(firstPath, coldSecond.LookupCommit(originalRoot).Receipt!.SourceArchiveETag, VerifyBatch, VerifyCommit);
        Assert.That(coldFirst.GetCommit(originalRoot).Kind, Is.EqualTo(RoamingNetworkCommitKind.Checkpoint));
        foreach (var receipt in history.RetentionReceipts)
        {
            var decoded = cbor ? RoamingNetworkRetentionReceipt.ParseCBOR(receipt.ToCBOR()) : RoamingNetworkRetentionReceipt.Parse(receipt.ToJSON());
            Assert.That(decoded.Id, Is.EqualTo(receipt.Id)); Assert.That(decoded.ToCBOR(), Is.EqualTo(receipt.ToCBOR()));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Pruning_catalog_survives_resumable_bootstrap_and_known_archived_tips_differ_from_unknown(Boolean cbor)
    {
        using var history = History(); var oldRoot = history.Head.Id; var snapshot = LinearHistory(history);
        using var cold = new ArchiveDirectory(); Prune(history, Plan(history, snapshot), cold.ArchivePath);
        var before = new Observation(history); using var staging = new ArchiveDirectory();
        var source = history.CreateBootstrap(256); var policy = AcceptChain(history);
        var manifest = cbor ? RoamingNetworkBootstrapManifest.ParseCBOR(source.Manifest.ToCBOR()) : RoamingNetworkBootstrapManifest.Parse(source.Manifest.ToJSON());
        Assert.That(manifest.WireProfile, Is.EqualTo(RoamingNetworkBootstrapManifest.RetentionProfile));
        Assert.That(manifest.ArchiveProfile, Is.EqualTo(RoamingNetworkHistory.RetentionArchiveProfile));
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, manifest); Finish(receiver, source, cbor);
        Assert.That(receiver.TryActivate(manifest.Id, out var activated, out var result, activate: true,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: policy), Is.True, result.Error);
        using var replica = activated!; Assert.That(replica.ToCBOR(), Is.EqualTo(history.ToCBOR()));
        Assert.That(replica.LookupCommit(oldRoot).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Archived));
        var unknown = new RoamingNetworkCommitId(ETag.Compute(ETagFormat.JSON, [12]));
        Assert.That(replica.LookupCommit(unknown).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Unknown));
        Assert.That(history.TryCreateCommitPack(replica.GetReplicationState(), oldRoot, out _, out var report), Is.False);
        Assert.That(report.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.SnapshotRequired));
        Assert.That(report.RetentionReceipt!.SourceArchiveETag, Is.EqualTo(history.RetentionReceipts[0].SourceArchiveETag));
        Assert.That(report.ProposedBoundary!.Anchor, Is.EqualTo(snapshot.Id));
        Assert.That(history.TryCreateCommitPack(replica.GetReplicationState(), unknown, out _, out report), Is.False);
        Assert.That(report.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.UnknownTip)); Assert.That(report.RetentionReceipt, Is.Null);
        before.AssertUnchanged(history);
    }

    [Test]
    public void Archived_suffix_branch_can_be_reimported_without_losing_historical_receipts()
    {
        using var history = History(); var snapshot = LinearHistory(history);
        var branch = Prepare(history, snapshot.Id, "archived-suffix", Power("175 kW")); Store(history, branch);
        using var directory = new ArchiveDirectory(); Prune(history, Plan(history, snapshot, [branch.Id]), directory.ArchivePath);
        Assert.That(history.LookupCommit(branch.Id).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Archived));
        var head = history.Head.Id; Store(history, branch);
        Assert.That(history.LookupCommit(branch.Id).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Retained));
        Assert.That(history.RetentionReceipts[0].PrunedCommits, Does.Contain(branch.Id)); Assert.That(history.Head.Id, Is.EqualTo(head));
    }

    [Test]
    public void Receipt_tampering_bad_archive_digests_and_revoked_cold_signatures_are_rejected()
    {
        using var history = History(); var snapshot = LinearHistory(history); using var directory = new ArchiveDirectory();
        var receipt = Prune(history, Plan(history, snapshot), directory.ArchivePath).Receipt!;
        var json = JsonNode.Parse(receipt.ToJSON())!; json["CreatedAt"] = InteropFixture.Time.ToString("O");
        Assert.That(() => RoamingNetworkRetentionReceipt.Parse(json.ToJsonString()), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkHistory.ReadColdArchive(directory.ArchivePath, ETag.Compute(ETagFormat.CBOR, [4]), VerifyBatch, VerifyCommit), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkHistory.ReadColdArchive(directory.ArchivePath, receipt.SourceArchiveETag,
            VerifyBatch, (_, _) => false), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit), Throws.ArgumentException);
    }

    [Test]
    public void Imports_requiring_recorded_archived_parents_report_cold_history_without_retaining_a_partial_page()
    {
        using var history = History(); var oldRoot = history.Head.Id; var snapshot = LinearHistory(history);
        using var directory = new ArchiveDirectory(); Prune(history, Plan(history, snapshot), directory.ArchivePath);
        var batch = Sign(Batch(history.Head.Snapshot, "external-parent", Power("175 kW")));
        var commit = Sign(RoamingNetworkCommit.Create(history.Head.Commit, batch, [oldRoot]));
        var pack = new RoamingNetworkCommitPack(history.GetCommit(snapshot.Id), commit.Id, true, [commit], history.CheckpointId);
        var before = new Observation(history);
        Assert.That(history.TryImportCommitPack(pack, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.HistoryRequired));
        Assert.That(result.MissingCommits, Does.Contain(oldRoot));
        Assert.That(result.RetentionReceipt!.Id, Is.EqualTo(history.RetentionReceipts[0].Id)); before.AssertUnchanged(history);
    }

    [Test]
    public void Exporting_a_later_snapshot_does_not_mark_excluded_retained_branches_as_pruned()
    {
        using var sender = History(); var snapshot = LinearHistory(sender); using var cold = new ArchiveDirectory();
        Prune(sender, Plan(sender, snapshot), cold.ArchivePath);
        var hidden = Prepare(sender, snapshot.Id, "hidden-suffix", Power("175 kW")); Store(sender, hidden);
        var later = Snapshot(sender, 11); Publish(sender, later);
        var before = new Observation(sender); var source = sender.CreateSnapshotBootstrap(later.Id, chunkBytes: 256);
        using var staging = new ArchiveDirectory(); using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest);
        Finish(receiver, source, true);
        Assert.That(receiver.TryActivate(source.Manifest.Id, out var activated, out var result, activate: true,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: AcceptChain(sender)), Is.True, result.Error);
        using var replica = activated!;
        Assert.That(replica.AnchorId, Is.EqualTo(later.Id)); Assert.That(replica.RetentionReceipts, Has.Length.EqualTo(1));
        Assert.That(replica.LookupCommit(hidden.Id).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Unknown));
        Assert.That(sender.LookupCommit(hidden.Id).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Retained)); before.AssertUnchanged(sender);
        Assert.That(replica.TryPlanRetention(later.Id, InteropFixture.Time, out var absent, out var nothing), Is.False);
        Assert.That(nothing.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.NothingToPrune)); Assert.That(absent, Is.Null);
        // A later export root need not equal the preceding receipt's root; a further local event is valid.
        var branch = Prepare(replica, later.Id, "released-child", Power("200 kW")); Store(replica, branch);
        using var secondCold = new ArchiveDirectory(); Prune(replica, Plan(replica, later, [branch.Id]), secondCold.ArchivePath);
        Assert.That(replica.RetentionReceipts, Has.Length.EqualTo(2));
        Assert.That(replica.RetentionReceipts[1].BeforeAnchor, Is.EqualTo(later.Id));
    }

    [Test]
    public async Task Competing_executions_install_one_reviewed_plan_and_preserve_the_live_head()
    {
        using var history = History(); var snapshot = LinearHistory(history); var plan = Plan(history, snapshot);
        using var directory = new ArchiveDirectory(); var head = history.Head; using var barrier = new Barrier(3);
        Task<RoamingNetworkRetentionResult> Run(String name) => Task.Run(() => {
            Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
            history.TryExecuteRetention(plan, Path.Combine(directory.DirectoryPath, name), AcceptChain(history), out var result, prune: true);
            return result;
        });
        var first = Run("first.cbor"); var second = Run("second.cbor");
        Assert.That(barrier.SignalAndWait(TimeSpan.FromSeconds(10)), Is.True);
        var outcomes = await Task.WhenAll(first, second);
        Assert.That(outcomes.Count(result => result.Outcome == RoamingNetworkRetentionOutcome.Pruned), Is.EqualTo(1));
        Assert.That(outcomes.Count(result => result.Outcome == RoamingNetworkRetentionOutcome.InventoryChanged), Is.EqualTo(1));
        Assert.That(history.RetentionReceipts, Has.Length.EqualTo(1)); Assert.That(history.Head, Is.SameAs(head));
    }

    [Test]
    public async Task Runtime_delivery_waits_for_retention_and_updates_the_same_live_head_after_installation()
    {
        using var history = History(); var snapshot = LinearHistory(history); var plan = Plan(history, snapshot);
        using var directory = new ArchiveDirectory(); var head = history.Head;
        using var entered = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var pruning = Task.Run(() => {
            history.TryExecuteRetention(plan, directory.ArchivePath, _ => {
                entered.Set(); if (!release.Wait(TimeSpan.FromSeconds(10))) throw new TimeoutException(); return true;
            }, out var result, prune: true); return result;
        });
        Task? delivery = null;
        try
        {
            Assert.That(entered.Wait(TimeSpan.FromSeconds(10)), Is.True);
            delivery = Task.Run(() => Status(history, EvseTarget, "charging"));
            await Task.Delay(100); Assert.That(delivery.IsCompleted, Is.False);
        }
        finally { release.Set(); }
        var result = await pruning; if (delivery is not null) await delivery;
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.Pruned));
        Assert.That(history.Head, Is.SameAs(head));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
    }
}
