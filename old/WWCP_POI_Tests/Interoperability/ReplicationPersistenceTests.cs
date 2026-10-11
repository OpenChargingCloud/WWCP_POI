/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ReplicationPersistenceTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void A_multi_commit_page_is_persisted_once_before_retention_and_recovers_without_publication(Boolean cbor)
    {
        using var directory = new ArchiveDirectory(); using var sender = History();
        var first = Prepare(sender, sender.Head.Id, "first", Power("150 kW")); Store(sender, first);
        var second = Prepare(sender, first.Id, "second", Rename("Second")); Store(sender, second);
        var page = Transport(new(Sign(sender.Head.Commit), second.Id, true, [first, second]), cbor);
        using (var receiver = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit))
        {
            var root = receiver.Head.Id; var before = File.ReadAllBytes(directory.ArchivePath);
            Status(receiver, EvseTarget, "charging");
            var network = receiver.Head.Network;
            var stages = new List<(ArchiveWriteStage Stage, RoamingNetworkCommitId Head, Int32 Count, Byte[] Disk)>();
            receiver.ArchiveWriteObserver = stage => stages.Add((stage, receiver.Head.Id, receiver.Commits.Length, File.ReadAllBytes(directory.ArchivePath)));
            Assert.That(receiver.TryImportCommitPack(page, out var result), Is.True, result.Error);
            Assert.That(stages.Select(item => item.Stage), Is.EqualTo(new[] {
                ArchiveWriteStage.BeforeTemporaryWrite, ArchiveWriteStage.TemporaryFileFlushed, ArchiveWriteStage.ArchiveReplaced
            }));
            Assert.That(stages.All(item => item.Head == root && item.Count == 1), Is.True);
            Assert.That(stages[0].Disk, Is.EqualTo(before)); Assert.That(stages[1].Disk, Is.EqualTo(before));
            Assert.That(stages[2].Disk, Is.EqualTo(receiver.ToCBOR()));
            Assert.That(result.StoredCount, Is.EqualTo(2)); Assert.That(receiver.Head.Id, Is.EqualTo(root));
            Assert.That(receiver.Head.Network, Is.SameAs(network));
            Assert.That(network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        }
        using var recovered = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit);
        Assert.That(recovered.Head.Id, Is.EqualTo(sender.Head.Id)); // Sender retained but did not publish either transition.
        Assert.That(recovered.Commits, Has.Length.EqualTo(3));
        Assert.That(recovered.GetSnapshot(second.Id).ETags, Is.EqualTo(second.StateETags));
        Assert.That(recovered.TryAdoptHead(recovered.Head.Id, second.Id, out var adopted, adopt: true), Is.True, adopted.Error);
        var next = Prepare(recovered, second.Id, "continued", Power("175 kW")); Publish(recovered, next);
        Assert.That(recovered.Head.Id, Is.EqualTo(next.Id));
    }

    [TestCase("BeforeTemporaryWrite", false)]
    [TestCase("BeforeTemporaryWrite", true)]
    [TestCase("TemporaryFileFlushed", false)]
    [TestCase("TemporaryFileFlushed", true)]
    public void Failed_page_persistence_keeps_memory_disk_peers_and_runtime_unchanged(String stageName, Boolean cbor)
    {
        using var directory = new ArchiveDirectory(); using var sender = History();
        var first = Prepare(sender, sender.Head.Id, "first", Power("150 kW")); Store(sender, first);
        var second = Prepare(sender, first.Id, "second", Rename("Second"));
        var page = Transport(new(Sign(sender.Head.Commit), second.Id, true, [first, second]), cbor);
        using var receiver = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit);
        Status(receiver, EvseTarget, "charging");
        var before = new Observation(receiver); var disk = File.ReadAllBytes(directory.ArchivePath);
        var stage = Enum.Parse<ArchiveWriteStage>(stageName);
        receiver.ArchiveWriteObserver = reached => { if (reached == stage) throw new IOException("Injected page persistence failure"); };
        Assert.That(receiver.TryImportCommitPack(page, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.PersistenceFailure));
        before.AssertUnchanged(receiver);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(disk));
        Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        Assert.That(receiver.Head.Commit.Signatures, Is.Empty);
        receiver.ArchiveWriteObserver = null;
        Assert.That(receiver.TryImportCommitPack(page, out result), Is.True, result.Error);
        Assert.That(result.StoredCount, Is.EqualTo(2));
        Assert.That(receiver.Head.Commit.Signatures, Has.Length.EqualTo(2));
        Assert.That(receiver.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
    }

    [TestCase("BeforeTemporaryWrite", false)]
    [TestCase("BeforeTemporaryWrite", true)]
    [TestCase("TemporaryFileFlushed", false)]
    [TestCase("TemporaryFileFlushed", true)]
    public void Failed_adoption_preserves_disk_and_runtime_then_retry_recovers_the_original_signed_head(String stageName, Boolean secondaryParent)
    {
        using var directory = new ArchiveDirectory();
        RoamingNetworkCommit target;
        using (var history = RoamingNetworkHistory.CreatePersistent(directory.ArchivePath, Network(), VerifyBatch, VerifyCommit))
        {
            var root = history.Head.Id;
            Store(history, Sign(history.Head.Commit));
            var left = Prepare(history, root, "left", Power("150 kW")); Store(history, left);
            var right = Prepare(history, root, "right", Rename("Right")); Publish(history, right);
            target = secondaryParent ? Merge(history, left, right) : Prepare(history, right.Id, "forward", Power("175 kW"));
            Store(history, target);
            Status(history, EvseTarget, "charging"); Status(history, PoolMeter, "error");
            var before = new Observation(history); var disk = File.ReadAllBytes(directory.ArchivePath);
            var stage = Enum.Parse<ArchiveWriteStage>(stageName);
            history.ArchiveWriteObserver = reached => { if (reached == stage) throw new IOException("Injected adoption persistence failure"); };
            Assert.That(history.TryAdoptHead(right.Id, target.Id, out var result, adopt: true), Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.PersistenceFailure));
            Assert.That(result.RuntimeTransfer, Is.EqualTo(secondaryParent ? RoamingNetworkRuntimeTransfer.ConservativeBranchContinuity : RoamingNetworkRuntimeTransfer.FirstParentReplay));
            before.AssertUnchanged(history);
            Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(disk));
            Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
            history.ArchiveWriteObserver = null;
            Status(history, EvseTarget, "reserved", 3);
            var observed = new List<(ArchiveWriteStage Stage, RoamingNetworkCommitId Head)>();
            history.ArchiveWriteObserver = reached => observed.Add((reached, history.Head.Id));
            Assert.That(history.TryAdoptHead(right.Id, target.Id, out result, adopt: true), Is.True, result.Error);
            Assert.That(observed.Select(item => item.Stage), Is.EqualTo(new[] { ArchiveWriteStage.BeforeTemporaryWrite, ArchiveWriteStage.TemporaryFileFlushed, ArchiveWriteStage.ArchiveReplaced }));
            Assert.That(observed.All(item => item.Head == right.Id), Is.True);
            Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
            Assert.That(history.Head.Network.ChargingPools.Single().EnergyMeters.Single().Status.Value.ToString(), Is.EqualTo("error"));
            Assert.That(history.Head.Id, Is.EqualTo(target.Id));
        }
        using (var recovered = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit))
        {
            Assert.That(recovered.Head.Id, Is.EqualTo(target.Id));
            Assert.That(recovered.Head.Snapshot.ETags, Is.EqualTo(target.StateETags));
            Assert.That(recovered.Head.Commit.GetIdentityBytes(), Is.EqualTo(target.GetIdentityBytes()));
            Assert.That(recovered.Head.Commit.Signatures, Is.EqualTo(target.Signatures));
            Assert.That(recovered.Head.Commit.ChangeSet!.Signatures, Is.EqualTo(target.ChangeSet!.Signatures));
            Assert.That(recovered.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.Not.EqualTo("reserved"));
            Assert.That(recovered.Head.Network.ChargingPools.Single().EnergyMeters.Single().Status.Value.ToString(), Is.Not.EqualTo("error"));
            Assert.That(recovered.TryAdoptHead(target.Parents[0], target.Id, out var duplicate, adopt: true), Is.True);
            Assert.That(duplicate.Outcome, Is.EqualTo(RoamingNetworkHeadAdoptionOutcome.AlreadyCurrent));
            var continued = Prepare(recovered, target.Id, "after-recovery", Power("200 kW")); Publish(recovered, continued);
        }
        using var again = RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit);
        Assert.That(again.Head.Snapshot.AppliedChangeSetId, Is.EqualTo("after-recovery"));
        Assert.That(again.Head.Snapshot.Revision, Is.EqualTo(target.Revision + 1));
    }
}
