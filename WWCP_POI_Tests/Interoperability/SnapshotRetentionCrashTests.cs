/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class SnapshotRetentionCrashTests
{
    private sealed record Policy(RoamingNetworkCommitId Checkpoint, RoamingNetworkCommitId BeforeAnchor,
        RoamingNetworkCommitId Target)
    {
        internal Boolean Accept(RoamingNetworkSnapshotBoundary boundary)
            => boundary.Checkpoint == Checkpoint && (boundary.Anchor == BeforeAnchor || boundary.Anchor == Target);

        internal void Save(String path)
            => File.WriteAllLines(path + ".pins", [Checkpoint.ToString(), BeforeAnchor.ToString(), Target.ToString()]);

        internal static Policy Read(String path)
        {
            var ids = File.ReadAllLines(path + ".pins").Select(RoamingNetworkCommitId.Parse).ToArray();
            return new(ids[0], ids[1], ids[2]);
        }
    }

    private static void Runtime(RoamingNetworkHistory history, String status)
    {
        Status(history, EvseTarget, status);
        var evse = history.Head.Network.EVSEs.Single();
        evse.MaxPowerRealTime = new(InteropFixture.Time.AddDays(2), Watt.Parse("75 kW"));
        evse.MaxPowerPrognoses.Add(new Timestamped<Watt>(InteropFixture.Time.AddDays(3), Watt.Parse("50 kW")));
    }

    private static void FreshRuntime(RoamingNetworkHistory history)
    {
        var evse = history.Head.Network.EVSEs.Single();
        Assert.That(evse.Status.Value.ToString(), Is.Not.EqualTo("charging"));
        Assert.That(evse.Status.Value.ToString(), Is.Not.EqualTo("reserved"));
        Assert.That(evse.MaxPowerRealTime, Is.Null); Assert.That(evse.MaxPowerPrognoses, Is.Empty);
    }

    private static void Peers(RoamingNetworkHistory history)
    {
        foreach (var commit in history.Commits)
        {
            Assert.That(commit.Signatures.Select(peer => peer.KeyId), Is.EqualTo(new[] { "fixture-alice", "fixture-bob" }));
            foreach (var peer in commit.Signatures) Assert.That(VerifyCommit(commit, peer), Is.True);
            if (commit.ChangeSet is not { } batch) continue;
            Assert.That(batch.Signatures.Select(peer => peer.KeyId), Is.EqualTo(new[] { "fixture-alice", "fixture-bob" }));
            foreach (var peer in batch.Signatures) Assert.That(VerifyBatch(batch, peer), Is.True);
        }
    }

    private static String ColdPath(String path) => Path.Combine(Path.GetDirectoryName(path)!, "cold.cbor");

    private static void FlushFile(String path, Byte[] bytes)
    {
        using var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        file.Write(bytes); file.Flush(flushToDisk: true);
    }

    private static void Orphans(String path, Boolean expected, Byte[] bytes)
    {
        var files = Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".tmp-*");
        Assert.That(files, Has.Length.EqualTo(expected ? 1 : 0));
        foreach (var file in files) Assert.That(File.ReadAllBytes(file), Is.EqualTo(bytes));
    }

    private static void RejectRevokedTrust(String path, Policy policy, Boolean partial)
    {
        var before = File.ReadAllBytes(path);
        Assert.That(() => RoamingNetworkHistory.Open(path, VerifyBatch, (_, _) => false,
            authorizeSnapshotBoundary: policy.Accept), Throws.ArgumentException);
        if (partial)
        {
            Assert.That(() => RoamingNetworkHistory.Open(path, VerifyBatch, VerifyCommit), Throws.ArgumentException);
            Assert.That(() => RoamingNetworkHistory.Open(path, VerifyBatch, VerifyCommit,
                authorizeSnapshotBoundary: _ => false), Throws.ArgumentException);
        }
        Assert.That(File.ReadAllBytes(path), Is.EqualTo(before));
        // Every failed opening released its lease, independently of the subsequent successful replay.
        using var lease = new FileStream(path + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    private static IEnumerable<TestCaseData> SnapshotStages()
    {
        foreach (var boundary in new[] { false, true })
            foreach (var stage in Enum.GetNames<ArchiveWriteStage>()) yield return new(stage, boundary);
    }

    [TestCaseSource(nameof(SnapshotStages))]
    public async Task Signed_snapshot_exit_recovers_exact_old_or_new_bytes_and_retries_without_duplicate_publication(
        String stage, Boolean previousBoundary)
    {
        using var directory = new ArchiveDirectory();
        Byte[] before, after; RoamingNetworkCommit candidate; RoamingNetworkCommitId previousHead; Policy policy;
        Int32 previousReceipts;
        using (var initial = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit))
        {
            if (previousBoundary)
            {
                var first = LinearHistory(initial);
                Prune(initial, Plan(initial, first), Path.Combine(directory.DirectoryPath, "previous.cbor"));
            }
            else
            {
                Store(initial, Sign(initial.Head.Commit));
                Publish(initial, Prepare(initial, initial.Head.Id, "before-snapshot", Power("150 kW")));
            }
            Store(initial, Prepare(initial, initial.Head.Id, "unpublished", Rename("Unpublished branch")));
            Runtime(initial, "charging"); previousHead = initial.Head.Id; previousReceipts = initial.RetentionReceipts.Length;
            candidate = Snapshot(initial, 30); policy = new(initial.CheckpointId, initial.AnchorId, candidate.Id); policy.Save(directory.ArchivePath);
            File.WriteAllBytes(directory.ArchivePath + ".candidate.cbor", candidate.ToCBOR());
            before = File.ReadAllBytes(directory.ArchivePath);
            using var oracle = RoamingNetworkHistory.ParseCBOR(before, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: policy.Accept);
            Publish(oracle, candidate); after = oracle.ToCBOR();
        }
        await ArchiveCrashTestSupport.Run(directory.ArchivePath, stage, "Snapshot");
        var replaced = stage == nameof(ArchiveWriteStage.ArchiveReplaced);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(replaced ? after : before));
        Orphans(directory.ArchivePath, stage == nameof(ArchiveWriteStage.TemporaryFileFlushed), after);
        RejectRevokedTrust(directory.ArchivePath, policy, previousBoundary);
        using var recovered = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: policy.Accept);
        Assert.That(recovered.Head.Id, Is.EqualTo(replaced ? candidate.Id : previousHead));
        Assert.That(recovered.AnchorId, Is.EqualTo(policy.BeforeAnchor)); Assert.That(recovered.CheckpointId, Is.EqualTo(policy.Checkpoint));
        Assert.That(recovered.HasCompleteAncestry, Is.EqualTo(!previousBoundary));
        Assert.That(recovered.RetentionReceipts, Has.Length.EqualTo(previousReceipts)); FreshRuntime(recovered); Peers(recovered);
        var revision = recovered.Head.Snapshot.Revision; var lastBatch = recovered.Head.Snapshot.AppliedChangeSetId;
        var etags = recovered.Head.Snapshot.ETags;
        Assert.That(recovered.TryPublish(previousHead, candidate, out var result), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(replaced ? RoamingNetworkHistoryOutcome.AlreadyPublished : RoamingNetworkHistoryOutcome.Published));
        Assert.That(recovered.Head.Id, Is.EqualTo(candidate.Id)); Assert.That(recovered.Head.Commit.ToCBOR(), Is.EqualTo(candidate.ToCBOR()));
        Assert.That(recovered.Head.Snapshot.Revision, Is.EqualTo(replaced ? revision : revision + 1));
        Assert.That(recovered.Head.Snapshot.AppliedChangeSetId, Is.EqualTo(lastBatch)); Assert.That(recovered.Head.Snapshot.ETags, Is.EqualTo(etags));
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(after));
        Publish(recovered, Prepare(recovered, recovered.Head.Id, "snapshot-crash-continued", Power("200 kW")));
        var continued = recovered.ToCBOR(); var head = recovered.Head.Id; recovered.Dispose();
        using var again = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: policy.Accept);
        Assert.That(again.Head.Id, Is.EqualTo(head)); Assert.That(again.ToCBOR(), Is.EqualTo(continued)); Peers(again);
    }

    private static IEnumerable<TestCaseData> RetentionStages()
    {
        foreach (var boundary in new[] { false, true })
        {
            foreach (var stage in Enum.GetNames<RetentionWriteStage>()) yield return new(stage, boundary, false);
            foreach (var stage in Enum.GetNames<ArchiveWriteStage>()) yield return new(stage, boundary, false);
            yield return new(nameof(RetentionWriteStage.ColdArchiveVerified), boundary, true);
        }
    }

    [TestCaseSource(nameof(RetentionStages))]
    public async Task Retention_exit_recovers_root_catalog_and_cold_digest_then_identifies_or_retries_the_reviewed_operation(
        String stage, Boolean previousBoundary, Boolean existingCold)
    {
        using var directory = new ArchiveDirectory(); using var oracleDirectory = new ArchiveDirectory();
        Byte[] before, after; RoamingNetworkRetentionPlan plan; RoamingNetworkRetentionReceipt receipt;
        RoamingNetworkCommit root; RoamingNetworkDataSnapshot state; Policy policy; Int32 oldReceipts;
        using (var initial = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit))
        {
            var target = LinearHistory(initial);
            if (previousBoundary)
            {
                Prune(initial, Plan(initial, target), Path.Combine(directory.DirectoryPath, "previous.cbor"));
                Publish(initial, Prepare(initial, initial.Head.Id, "between", Power("175 kW")));
                target = Snapshot(initial, 11); Publish(initial, target);
                Publish(initial, Prepare(initial, initial.Head.Id, "later", Rename("Later")));
            }
            var released = Prepare(initial, initial.AnchorId, "released", Rename("Cold-only branch")); Store(initial, released);
            Runtime(initial, "charging"); state = initial.Head.Snapshot; root = target; oldReceipts = initial.RetentionReceipts.Length;
            plan = Plan(initial, target, [released.Id]); policy = new(initial.CheckpointId, initial.AnchorId, target.Id); policy.Save(directory.ArchivePath);
            File.WriteAllText(directory.ArchivePath + ".plan-id", plan.Id.ToString());
            File.WriteAllLines(directory.ArchivePath + ".released", plan.ArchiveOnlyTips.Select(id => id.ToString()));
            before = File.ReadAllBytes(directory.ArchivePath);
            using var oracle = RoamingNetworkHistory.ParseCBOR(before, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: policy.Accept);
            receipt = Prune(oracle, plan, oracleDirectory.ArchivePath).Receipt!; after = oracle.ToCBOR();
        }
        var coldPath = ColdPath(directory.ArchivePath);
        if (existingCold) FlushFile(coldPath, before);
        await ArchiveCrashTestSupport.Run(directory.ArchivePath, stage, "Retention");
        var replaced = stage == nameof(ArchiveWriteStage.ArchiveReplaced);
        var coldPublished = existingCold || stage is not (nameof(RetentionWriteStage.BeforeColdTemporaryWrite) or nameof(RetentionWriteStage.ColdTemporaryFileFlushed));
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(replaced ? after : before));
        Assert.That(File.Exists(coldPath), Is.EqualTo(coldPublished));
        Orphans(coldPath, stage == nameof(RetentionWriteStage.ColdTemporaryFileFlushed), before);
        Orphans(directory.ArchivePath, stage == nameof(ArchiveWriteStage.TemporaryFileFlushed), after);
        RejectRevokedTrust(directory.ArchivePath, policy, previousBoundary || replaced);
        using var recovered = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: policy.Accept);
        Assert.That(recovered.Head.Id, Is.EqualTo(plan.ExpectedHead)); Assert.That(recovered.CheckpointId, Is.EqualTo(policy.Checkpoint));
        Assert.That(recovered.AnchorId, Is.EqualTo(replaced ? plan.Boundary.Anchor : plan.BeforeAnchor));
        Assert.That(recovered.HasCompleteAncestry, Is.EqualTo(!previousBoundary && !replaced));
        Assert.That(recovered.Head.Snapshot.Revision, Is.EqualTo(state.Revision));
        Assert.That(recovered.Head.Snapshot.AppliedChangeSetId, Is.EqualTo(state.AppliedChangeSetId));
        Assert.That(recovered.Head.Snapshot.ETags, Is.EqualTo(state.ETags));
        Assert.That(recovered.GetCommit(root.Id).ToCBOR(), Is.EqualTo(root.ToCBOR()));
        Assert.That(recovered.RetentionReceipts, Has.Length.EqualTo(oldReceipts + (replaced ? 1 : 0)));
        FreshRuntime(recovered); Peers(recovered);
        if (coldPublished)
        {
            Assert.That(File.ReadAllBytes(coldPath), Is.EqualTo(before));
            Assert.That(ETag.Compute(ETagFormat.CBOR, File.ReadAllBytes(coldPath)), Is.EqualTo(plan.SourceArchiveETag));
            using var cold = RoamingNetworkHistory.ReadColdArchive(coldPath, plan.SourceArchiveETag, VerifyBatch, VerifyCommit,
                authorizeSnapshotBoundary: policy.Accept);
            Assert.That(cold.AnchorId, Is.EqualTo(plan.BeforeAnchor)); Assert.That(cold.Head.Id, Is.EqualTo(plan.ExpectedHead));
            Assert.That(cold.RetentionReceipts, Has.Length.EqualTo(oldReceipts)); FreshRuntime(cold); Peers(cold);
            foreach (var id in plan.PrunedCommits) Assert.That(cold.LookupCommit(id).Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Retained));
            if (previousBoundary)
            {
                using var previous = RoamingNetworkHistory.ReadColdArchive(Path.Combine(directory.DirectoryPath, "previous.cbor"),
                    cold.RetentionReceipts[0].SourceArchiveETag, VerifyBatch, VerifyCommit);
                Assert.That(previous.CheckpointId, Is.EqualTo(policy.Checkpoint)); Assert.That(previous.HasCompleteAncestry, Is.True); Peers(previous);
            }
        }
        Runtime(recovered, "reserved"); var sameHead = recovered.Head; var runtime = sameHead.Network.ToJSONSnapshot().ToString();
        if (replaced)
        {
            Assert.That(recovered.RetentionReceipts[^1].PlanId, Is.EqualTo(plan.Id));
            Assert.That(recovered.RetentionReceipts[^1].ToCBOR(), Is.EqualTo(receipt.ToCBOR()));
            var unchanged = new Observation(recovered);
            Assert.That(recovered.TryExecuteRetention(plan, coldPath, policy.Accept, out var duplicate, prune: true), Is.False);
            Assert.That(duplicate.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.InventoryChanged)); unchanged.AssertUnchanged(recovered);
        }
        else
        {
            Assert.That(recovered.TryExecuteRetention(plan, coldPath, policy.Accept, out var retry, prune: true), Is.True, retry.Error);
            Assert.That(retry.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.Pruned));
            Assert.That(retry.Receipt!.ToCBOR(), Is.EqualTo(receipt.ToCBOR()));
        }
        Assert.That(recovered.Head, Is.SameAs(sameHead)); Assert.That(recovered.Head.Network, Is.SameAs(sameHead.Network));
        Assert.That(recovered.Head.Network.ToJSONSnapshot().ToString(), Is.EqualTo(runtime));
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(after)); Assert.That(File.ReadAllBytes(coldPath), Is.EqualTo(before));
        Assert.That(recovered.Commits.Select(commit => commit.Id), Is.EquivalentTo(plan.RetainedCommits));
        foreach (var id in plan.PrunedCommits)
        {
            var lookup = recovered.LookupCommit(id); Assert.That(lookup.Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Archived));
            Assert.That(lookup.Receipt!.Id, Is.EqualTo(receipt.Id));
        }
        Publish(recovered, Prepare(recovered, recovered.Head.Id, "retention-crash-continued", Power("200 kW")));
        var continued = recovered.ToCBOR(); var head = recovered.Head.Id; recovered.Dispose();
        using var again = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: policy.Accept);
        Assert.That(again.Head.Id, Is.EqualTo(head)); Assert.That(again.AnchorId, Is.EqualTo(root.Id));
        Assert.That(again.ToCBOR(), Is.EqualTo(continued)); Assert.That(again.RetentionReceipts[^1].Id, Is.EqualTo(receipt.Id)); Peers(again);
    }

    [TestCase("BeforeColdTemporaryWrite")]
    [TestCase("ColdTemporaryFileFlushed")]
    [TestCase("ColdArchivePublished")]
    [TestCase("ColdArchiveVerified")]
    public void Cold_stage_exception_keeps_active_state_cleans_own_temporary_file_and_releases_cold_handles(String stage)
    {
        using var directory = new ArchiveDirectory(); var coldPath = ColdPath(directory.ArchivePath);
        using var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit);
        var snapshot = LinearHistory(history); var plan = Plan(history, snapshot); Runtime(history, "charging");
        var before = new Observation(history); var bytes = File.ReadAllBytes(directory.ArchivePath);
        history.RetentionWriteObserver = reached => {
            if (reached.ToString() != stage) return;
            Assert.That(history.TryExecuteRetention(plan, coldPath, AcceptChain(history), out var nested, prune: true), Is.False);
            Assert.That(nested.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.Unavailable));
            Assert.That(() => history.Dispose(), Throws.InvalidOperationException);
            throw new IOException("Injected cold-stage failure");
        };
        Assert.That(history.TryExecuteRetention(plan, coldPath, AcceptChain(history), out var result, prune: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkRetentionOutcome.PersistenceFailure));
        Assert.That(result.Error, Is.EqualTo("Injected cold-stage failure")); before.AssertUnchanged(history);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(bytes)); Assert.That(Directory.GetFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        var published = stage is "ColdArchivePublished" or "ColdArchiveVerified";
        Assert.That(File.Exists(coldPath), Is.EqualTo(published));
        if (published)
        {
            Assert.That(File.ReadAllBytes(coldPath), Is.EqualTo(bytes));
            using var cold = new FileStream(coldPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        using (var lease = new FileStream(coldPath + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        history.RetentionWriteObserver = null; Prune(history, plan, coldPath);
        Assert.That(File.ReadAllBytes(coldPath), Is.EqualTo(bytes));
    }

    internal static void RunWorker(String path, String operation, String stage)
    {
        var policy = Policy.Read(path);
        using var history = RoamingNetworkHistory.Open(path, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: policy.Accept);
        Runtime(history, "reserved");
        history.ArchiveWriteObserver = reached => { if (reached.ToString() == stage) ArchiveCrashTestSupport.Exit(path, stage); };
        if (operation == "Snapshot")
        {
            var candidate = RoamingNetworkCommit.ParseCBOR(File.ReadAllBytes(path + ".candidate.cbor"));
            Assert.That(candidate.Id, Is.EqualTo(policy.Target));
            history.TryPublish(candidate.Parents[0], candidate, out var result);
            Assert.Fail("Snapshot exit stage was not reached: " + result.Error);
        }
        else if (operation == "Retention")
        {
            var released = File.ReadAllLines(path + ".released").Select(RoamingNetworkCommitId.Parse).ToArray();
            var plan = Plan(history, history.GetCommit(policy.Target), released);
            Assert.That(plan.Id, Is.EqualTo(ETag.Parse(File.ReadAllText(path + ".plan-id"))));
            history.RetentionWriteObserver = reached => { if (reached.ToString() == stage) ArchiveCrashTestSupport.Exit(path, stage); };
            history.TryExecuteRetention(plan, ColdPath(path), policy.Accept, out var result, prune: true);
            Assert.Fail("Retention exit stage was not reached: " + result.Error);
        }
        else Assert.Fail("Unknown crash operation: " + operation);
    }
}
