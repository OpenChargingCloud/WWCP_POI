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
public sealed class BootstrapCrashTests
{
    private static readonly String[] Profiles = ["Complete", "Snapshots", "Boundary", "Pruned"];

    private static RoamingNetworkBootstrapSource Source(String profile, ArchiveDirectory cold)
    {
        using var sender = History();
        if (profile == "Complete")
        {
            Store(sender, Sign(sender.Head.Commit)); var root = sender.Head.Id;
            var left = Prepare(sender, root, "left", Power("150 kW")); Publish(sender, left);
            var right = Prepare(sender, root, "right", Rename("Remote")); Store(sender, right);
            Store(sender, Prepare(sender, root, "hidden", Power("200 kW")));
            Publish(sender, Merge(sender, left, right));
            Status(sender, EvseTarget, "charging");
            return sender.CreateBootstrap(512);
        }
        var snapshot = LinearHistory(sender);
        if (profile == "Pruned")
        {
            var released = Prepare(sender, sender.CheckpointId, "released", Rename("Cold-only branch")); Store(sender, released);
            Prune(sender, Plan(sender, snapshot, [released.Id]), cold.ArchivePath);
        }
        Store(sender, Prepare(sender, sender.Head.Id, "hidden", Power("200 kW")));
        Status(sender, EvseTarget, "charging");
        return profile == "Boundary" ? sender.CreateSnapshotBootstrap(snapshot.Id, chunkBytes: 512) : sender.CreateBootstrap(512);
    }

    private static Boolean Boundary(RoamingNetworkBootstrapManifest manifest, RoamingNetworkSnapshotBoundary boundary)
        => boundary.Checkpoint == manifest.Checkpoint && boundary.Anchor == manifest.Anchor;

    private static Boolean Activate(RoamingNetworkBootstrapReceiver receiver, String path, out RoamingNetworkHistory? history,
        out RoamingNetworkBootstrapResult result, Boolean recover = false)
        => receiver.TryActivate(receiver.Manifest.Id, out history, out result, activate: true, archivePath: path,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit,
            authorizeCommit: commit => commit.Signatures.Length == 2,
            authorizeBootstrap: manifest => manifest.Id == receiver.Manifest.Id,
            authorizeSnapshotBoundary: boundary => Boundary(receiver.Manifest, boundary), recoverExistingArchive: recover);

    private static Byte[] Bytes(RoamingNetworkBootstrapSource source)
    {
        using var stream = new MemoryStream();
        for (var index = 0; index < source.Manifest.ChunkCount; index++) stream.Write(source.CreateChunk(index).Data.AsSpan());
        return stream.ToArray();
    }

    private static void Save(String control, RoamingNetworkBootstrapSource source, String staging, String archive)
    {
        File.WriteAllBytes(control + ".manifest", source.Manifest.ToCBOR());
        File.WriteAllText(control + ".expected-id", source.Manifest.Id.ToString());
        File.WriteAllLines(control + ".paths", [staging, archive]);
        Assert.That(source.Manifest.ChunkCount, Is.GreaterThan(2));
        File.WriteAllBytes(control + ".fragment", source.CreateChunk(1).Data.ToArray());
    }

    private static Dictionary<String, Byte[]> Inventory(String directory)
        => Directory.GetFiles(directory).Where(path => !path.EndsWith(".lock", StringComparison.Ordinal))
            .ToDictionary(path => Path.GetFileName(path), File.ReadAllBytes);

    private static void Unchanged(String directory, Dictionary<String, Byte[]> expected)
    {
        var actual = Inventory(directory); Assert.That(actual.Keys, Is.EquivalentTo(expected.Keys));
        foreach (var (name, bytes) in expected) Assert.That(actual[name], Is.EqualTo(bytes), name);
    }

    private static void Orphans(String path, Boolean expected, Byte[] bytes)
    {
        var files = Directory.GetFiles(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".tmp-*");
        Assert.That(files, Has.Length.EqualTo(expected ? 1 : 0));
        foreach (var file in files) Assert.That(File.ReadAllBytes(file), Is.EqualTo(bytes));
    }

    private static void Replica(RoamingNetworkHistory history, RoamingNetworkBootstrapSource source)
    {
        Assert.That(history.ToCBOR(), Is.EqualTo(Bytes(source))); Assert.That(history.Head.Id, Is.EqualTo(source.Manifest.Head));
        Assert.That(history.CheckpointId, Is.EqualTo(source.Manifest.Checkpoint)); Assert.That(history.AnchorId, Is.EqualTo(source.Manifest.Anchor));
        Assert.That(history.Commits, Has.Length.EqualTo(source.Manifest.CommitCount));
        Assert.That(history.HasCompleteAncestry, Is.EqualTo(source.Manifest.Checkpoint == source.Manifest.Anchor));
        Assert.That(history.RetentionReceipts.Length, Is.EqualTo(source.Manifest.ArchiveProfile == RoamingNetworkHistory.RetentionArchiveProfile ? 1 : 0));
        foreach (var commit in history.Commits)
        {
            Assert.That(commit.Signatures.Select(peer => peer.KeyId), Is.EqualTo(new[] { "fixture-alice", "fixture-bob" }));
            foreach (var peer in commit.Signatures) Assert.That(VerifyCommit(commit, peer), Is.True);
            if (commit.ChangeSet is not { } batch) continue;
            Assert.That(batch.Signatures, Has.Length.EqualTo(2));
            foreach (var peer in batch.Signatures) Assert.That(VerifyBatch(batch, peer), Is.True);
        }
        var evse = history.Head.Network.EVSEs.Single();
        Assert.That(evse.Status.Value.ToString(), Is.EqualTo("available"));
        Assert.That(evse.MaxPowerRealTime, Is.Null); Assert.That(evse.MaxPowerPrognoses, Is.Empty);
    }

    private static void Continue(RoamingNetworkHistory replica, RoamingNetworkBootstrapSource source, String path)
    {
        Status(replica, EvseTarget, "reserved");
        Publish(replica, Prepare(replica, replica.Head.Id, "bootstrap-crash-followup", Power("225 kW")));
        var bytes = replica.ToCBOR(); var head = replica.Head.Id; replica.Dispose();
        using var reopened = RoamingNetworkHistory.Open(path, VerifyBatch, VerifyCommit,
            authorizeSnapshotBoundary: boundary => Boundary(source.Manifest, boundary));
        Assert.That(reopened.Head.Id, Is.EqualTo(head)); Assert.That(reopened.ToCBOR(), Is.EqualTo(bytes));
        Assert.That(reopened.AnchorId, Is.EqualTo(source.Manifest.Anchor));
        Assert.That(reopened.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
        foreach (var peer in reopened.Head.Commit.Signatures) Assert.That(VerifyCommit(reopened.Head.Commit, peer), Is.True);
    }

    private static IEnumerable<TestCaseData> ReceiptStages()
    {
        foreach (var profile in Profiles)
            foreach (var stage in Enum.GetNames<RoamingNetworkBootstrapReceiver.BootstrapWriteStage>()) yield return new(profile, stage);
    }

    [TestCaseSource(nameof(ReceiptStages))]
    public async Task Manifest_exit_reopens_only_a_published_manifest_and_preserves_unacknowledged_temporary_data(String profile, String stage)
    {
        using var cold = new ArchiveDirectory(); var source = Source(profile, cold);
        using var control = new ArchiveDirectory(); using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var fresh = new ArchiveDirectory();
        Save(control.ArchivePath, source, staging.DirectoryPath, destination.ArchivePath);
        await ArchiveCrashTestSupport.Run(control.ArchivePath, stage, "BootstrapManifest");
        var installed = stage == nameof(RoamingNetworkBootstrapReceiver.BootstrapWriteStage.ReceiptInstalled);
        var temporary = stage == nameof(RoamingNetworkBootstrapReceiver.BootstrapWriteStage.TemporaryFileFlushed);
        var manifestPath = Path.Combine(staging.DirectoryPath, "manifest.cbor");
        Assert.That(File.Exists(manifestPath), Is.EqualTo(installed));
        Assert.That(Directory.GetFiles(staging.DirectoryPath, "*.chunk"), Is.Empty);
        Orphans(manifestPath, temporary, source.Manifest.ToCBOR());
        Assert.That(File.Exists(destination.ArchivePath), Is.False);
        if (!installed)
        {
            Assert.That(() => RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, source.Manifest.Id), Throws.TypeOf<FileNotFoundException>());
            using var lease = new FileStream(Path.Combine(staging.DirectoryPath, "bootstrap.lock"), FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        var original = Inventory(staging.DirectoryPath);
        if (temporary)
            Assert.That(() => RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest), Throws.TypeOf<IOException>());
        // An unpublished manifest with orphan data has no resumable transfer; choose another empty directory explicitly.
        using var receiver = installed ? RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, source.Manifest.Id) :
            RoamingNetworkBootstrapReceiver.Create(temporary ? fresh.DirectoryPath : staging.DirectoryPath, source.Manifest);
        Assert.That(receiver.NextChunk, Is.Zero); Assert.That(receiver.Manifest.Id, Is.EqualTo(source.Manifest.Id));
        if (temporary) Unchanged(staging.DirectoryPath, original);
        Finish(receiver, source, true);
        Assert.That(Activate(receiver, destination.ArchivePath, out var replica, out var result), Is.True, result.Error);
        using (replica!) { Replica(replica!, source); Continue(replica!, source, destination.ArchivePath); }
        if (temporary) Unchanged(staging.DirectoryPath, original);
    }

    [TestCaseSource(nameof(ReceiptStages))]
    public async Task Chunk_exit_reopens_the_verified_prefix_and_retries_without_acknowledging_an_orphan(String profile, String stage)
    {
        using var cold = new ArchiveDirectory(); var source = Source(profile, cold);
        using var control = new ArchiveDirectory(); using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var unrelated = History(); Status(unrelated, EvseTarget, "reserved"); var untouched = new Observation(unrelated);
        Save(control.ArchivePath, source, staging.DirectoryPath, destination.ArchivePath);
        using (var initial = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest))
            Assert.That(initial.TryAcceptChunk(source.CreateChunk(0), out var result), Is.True, result.Error);
        var first = File.ReadAllBytes(Path.Combine(staging.DirectoryPath, "00000000.chunk"));
        await ArchiveCrashTestSupport.Run(control.ArchivePath, stage, "BootstrapChunk");
        var installed = stage == nameof(RoamingNetworkBootstrapReceiver.BootstrapWriteStage.ReceiptInstalled);
        var path = Path.Combine(staging.DirectoryPath, "00000001.chunk");
        Assert.That(File.Exists(path), Is.EqualTo(installed));
        Orphans(path, stage == nameof(RoamingNetworkBootstrapReceiver.BootstrapWriteStage.TemporaryFileFlushed), source.CreateChunk(1).Data.ToArray());
        Assert.That(File.ReadAllBytes(Path.Combine(staging.DirectoryPath, "00000000.chunk")), Is.EqualTo(first));
        Assert.That(File.Exists(destination.ArchivePath), Is.False);
        var rejected = Inventory(staging.DirectoryPath);
        Assert.That(() => RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, ETag.Compute(ETagFormat.JSON, [1])), Throws.ArgumentException);
        Unchanged(staging.DirectoryPath, rejected);
        using var receiver = RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, source.Manifest.Id);
        Assert.That(receiver.NextChunk, Is.EqualTo(installed ? 2 : 1));
        Assert.That(receiver.TryAcceptChunk(source.CreateChunk(1), out var retry), Is.True, retry.Error);
        Assert.That(retry.Outcome, Is.EqualTo(installed ? RoamingNetworkBootstrapOutcome.AlreadyStored : RoamingNetworkBootstrapOutcome.Accepted));
        Assert.That(receiver.NextChunk, Is.EqualTo(2)); Finish(receiver, source, false);
        Assert.That(Activate(receiver, destination.ArchivePath, out var replica, out var activation), Is.True, activation.Error);
        using (replica!) { Replica(replica!, source); Continue(replica!, source, destination.ArchivePath); }
        untouched.AssertUnchanged(unrelated);
    }

    private static IEnumerable<TestCaseData> ActivationStages()
    {
        foreach (var profile in Profiles)
            foreach (var stage in Enum.GetNames<ArchiveWriteStage>()) yield return new(profile, stage);
    }

    [TestCaseSource(nameof(ActivationStages))]
    public async Task Activation_exit_retries_absent_archives_or_explicitly_recovers_exact_completed_archives(String profile, String stage)
    {
        using var cold = new ArchiveDirectory(); var source = Source(profile, cold);
        using var control = new ArchiveDirectory(); using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var unrelated = History(); Status(unrelated, EvseTarget, "charging"); var untouched = new Observation(unrelated);
        Save(control.ArchivePath, source, staging.DirectoryPath, destination.ArchivePath);
        using (var initial = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest)) Finish(initial, source, true);
        var files = Inventory(staging.DirectoryPath);
        await ArchiveCrashTestSupport.Run(control.ArchivePath, stage, "BootstrapActivation");
        var installed = stage == nameof(ArchiveWriteStage.ArchiveReplaced); var bytes = Bytes(source);
        Assert.That(File.Exists(destination.ArchivePath), Is.EqualTo(installed));
        if (installed) Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(bytes));
        Orphans(destination.ArchivePath, stage == nameof(ArchiveWriteStage.TemporaryFileFlushed), bytes);
        Unchanged(staging.DirectoryPath, files); untouched.AssertUnchanged(unrelated);
        using var receiver = RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, source.Manifest.Id);
        Assert.That(receiver.NextChunk, Is.EqualTo(source.Manifest.ChunkCount));
        if (installed)
        {
            Assert.That(Activate(receiver, destination.ArchivePath, out var absent, out var rejected), Is.False);
            Assert.That(rejected.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.PersistenceFailure)); Assert.That(absent, Is.Null);
            Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(bytes));
        }
        Assert.That(Activate(receiver, destination.ArchivePath, out var replica, out var result, recover: true), Is.True, result.Error);
        using (replica!)
        {
            Assert.That(result.Outcome, Is.EqualTo(installed ? RoamingNetworkBootstrapOutcome.ActivationRecovered : RoamingNetworkBootstrapOutcome.Activated));
            Replica(replica!, source);
            Assert.That(() => RoamingNetworkHistory.Open(destination.ArchivePath, VerifyBatch, VerifyCommit,
                authorizeSnapshotBoundary: boundary => Boundary(source.Manifest, boundary)), Throws.TypeOf<IOException>());
            Assert.That(Activate(receiver, destination.ArchivePath, out var blocked, out var leased, recover: true), Is.False);
            Assert.That(leased.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.PersistenceFailure)); Assert.That(blocked, Is.Null);
            Unchanged(staging.DirectoryPath, files); untouched.AssertUnchanged(unrelated);
            Continue(replica!, source, destination.ArchivePath);
        }
    }

    private static IEnumerable<TestCaseData> RecoveryFaults()
    {
        foreach (var profile in Profiles)
            foreach (var fault in new[] { "Different", "Advanced", "Peers", "NonCanonical", "Lease" }) yield return new(profile, fault);
    }

    private static CBORValue Replace(CBORValue map, String name, CBORValue value)
        => CBORValue.FromMap(map.AsMap().Select(pair => new KeyValuePair<CBORValue, CBORValue>(pair.Key, pair.Key.AsText() == name ? value : pair.Value)));

    [TestCaseSource(nameof(RecoveryFaults))]
    public void Recovery_requires_exact_original_bytes_and_a_free_lease_even_for_equal_content_or_newer_valid_heads(String profile, String fault)
    {
        using var cold = new ArchiveDirectory(); var source = Source(profile, cold);
        using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest); Finish(receiver, source, true);
        var original = Bytes(source); var bytes = original;
        if (fault == "Different") bytes = [1, 2, 3];
        if (fault == "NonCanonical")
        {
            Assert.That(original[0], Is.InRange(0xA0, 0xB7));
            bytes = [0xBF, .. original.Skip(1), 0xFF];
            using var same = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit,
                authorizeSnapshotBoundary: boundary => Boundary(source.Manifest, boundary));
            Assert.That(same.ToCBOR(), Is.EqualTo(original));
        }
        if (fault == "Peers")
        {
            var map = CBORValue.Parse(bytes); var rootName = source.Manifest.Anchor == source.Manifest.Checkpoint ? "CheckpointCommit" : "SnapshotCommit";
            var root = map.AsMap().Single(pair => pair.Key.AsText() == rootName).Value;
            var peers = root.AsMap().Single(pair => pair.Key.AsText() == "Signatures").Value.AsArray();
            bytes = Replace(map, rootName, Replace(root, "Signatures", CBORValue.FromArray(peers.Take(1)))).ToByteArray(CBORWriterOptions.Canonical);
            using var same = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit,
                authorizeSnapshotBoundary: boundary => Boundary(source.Manifest, boundary));
            Assert.That(same.Head.Id, Is.EqualTo(source.Manifest.Head)); Assert.That(same.AnchorId, Is.EqualTo(source.Manifest.Anchor));
        }
        if (fault == "Advanced")
        {
            using var newer = RoamingNetworkHistory.ParseCBOR(original, VerifyBatch, VerifyCommit,
                authorizeSnapshotBoundary: boundary => Boundary(source.Manifest, boundary));
            Publish(newer, Prepare(newer, newer.Head.Id, "already-advanced", Power("250 kW"))); bytes = newer.ToCBOR();
        }
        File.WriteAllBytes(destination.ArchivePath, bytes); var staged = Inventory(staging.DirectoryPath);
        using var held = fault == "Lease" ? RoamingNetworkHistory.Open(destination.ArchivePath, VerifyBatch, VerifyCommit,
            authorizeSnapshotBoundary: boundary => Boundary(source.Manifest, boundary)) : null;
        Assert.That(Activate(receiver, destination.ArchivePath, out var absent, out var result, recover: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(fault == "Lease" ? RoamingNetworkBootstrapOutcome.PersistenceFailure : RoamingNetworkBootstrapOutcome.InvalidData));
        Assert.That(absent, Is.Null); Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(bytes)); Unchanged(staging.DirectoryPath, staged);
        Assert.That(receiver.NextChunk, Is.EqualTo(source.Manifest.ChunkCount));
        held?.Dispose();
        if (File.Exists(destination.ArchivePath + ".lock"))
        {
            using var lease = new FileStream(destination.ArchivePath + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
    }

    private static IEnumerable<TestCaseData> Revocations()
    {
        foreach (var profile in Profiles)
        {
            foreach (var policy in new[] { "Batch", "Commit", "WholeCommit", "Manifest" }) yield return new(profile, policy);
            if (profile is "Boundary" or "Pruned") yield return new(profile, "Boundary");
        }
    }

    [TestCaseSource(nameof(Revocations))]
    public void Matching_existing_activation_is_rechecked_against_current_trust_and_keeps_all_disk_bytes_on_rejection(String profile, String policy)
    {
        using var cold = new ArchiveDirectory(); var source = Source(profile, cold);
        using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest); Finish(receiver, source, true);
        Assert.That(Activate(receiver, destination.ArchivePath, out var first, out var initial), Is.True, initial.Error); first!.Dispose();
        var bytes = File.ReadAllBytes(destination.ArchivePath); var staged = Inventory(staging.DirectoryPath);
        Assert.That(receiver.TryActivate(source.Manifest.Id, out var absent, out var result, activate: true, archivePath: destination.ArchivePath,
            verifyBatchSignature: (batch, signature) => policy != "Batch" && VerifyBatch(batch, signature),
            verifyCommitSignature: (commit, signature) => policy != "Commit" && VerifyCommit(commit, signature),
            authorizeCommit: _ => policy != "WholeCommit", authorizeBootstrap: _ => policy != "Manifest",
            authorizeSnapshotBoundary: boundary => policy != "Boundary" && Boundary(source.Manifest, boundary), recoverExistingArchive: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.InvalidData)); Assert.That(absent, Is.Null);
        Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(bytes)); Unchanged(staging.DirectoryPath, staged);
        receiver.ActivationWriteObserver = _ => Assert.Fail("Existing recovery must not rewrite its destination.");
        Assert.That(Activate(receiver, destination.ArchivePath, out var recovered, out result, recover: true), Is.True, result.Error);
        using (recovered!) { Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.ActivationRecovered)); Replica(recovered!, source); }
        Unchanged(staging.DirectoryPath, staged); Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(bytes));
    }

    [TestCase("Complete")]
    [TestCase("Snapshots")]
    [TestCase("Boundary")]
    [TestCase("Pruned")]
    public void Trust_revocation_during_existing_archive_replay_releases_the_acquired_lease_and_allows_a_fresh_retry(String profile)
    {
        using var cold = new ArchiveDirectory(); var source = Source(profile, cold);
        using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest); Finish(receiver, source, true);
        Assert.That(Activate(receiver, destination.ArchivePath, out var first, out var initial), Is.True, initial.Error); first!.Dispose();
        var bytes = File.ReadAllBytes(destination.ArchivePath); var staged = Inventory(staging.DirectoryPath); var sourceChecks = 0; var checkedUnderLease = false;
        Assert.That(receiver.TryActivate(source.Manifest.Id, out var absent, out var rejected, activate: true, archivePath: destination.ArchivePath,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit,
            authorizeCommit: _ => {
                try
                {
                    using var lease = new FileStream(destination.ArchivePath + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                    sourceChecks++; return true;
                }
                catch (IOException) { checkedUnderLease = true; return false; }
            }, authorizeBootstrap: manifest => manifest.Id == source.Manifest.Id,
            authorizeSnapshotBoundary: boundary => Boundary(source.Manifest, boundary), recoverExistingArchive: true), Is.False);
        Assert.That(sourceChecks, Is.GreaterThan(0)); Assert.That(checkedUnderLease, Is.True);
        Assert.That(rejected.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.InvalidData)); Assert.That(absent, Is.Null);
        Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(bytes)); Unchanged(staging.DirectoryPath, staged);
        using (var lease = new FileStream(destination.ArchivePath + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        Assert.That(Activate(receiver, destination.ArchivePath, out var recovered, out var result, recover: true), Is.True, result.Error);
        using (recovered!) { Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.ActivationRecovered)); Replica(recovered!, source); }
        Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(bytes)); Unchanged(staging.DirectoryPath, staged);
    }

    private static IEnumerable<TestCaseData> ActivationFailures()
    {
        foreach (var profile in Profiles)
            foreach (var stage in new[] { "BeforeTemporaryWrite", "TemporaryFileFlushed" }) yield return new(profile, stage);
    }

    [TestCaseSource(nameof(ActivationFailures))]
    public void Activation_IO_failure_cleans_own_temporary_data_releases_destination_lease_and_retries_from_verified_staging(String profile, String stage)
    {
        using var cold = new ArchiveDirectory(); var source = Source(profile, cold);
        using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest); Finish(receiver, source, false);
        var staged = Inventory(staging.DirectoryPath);
        receiver.ActivationWriteObserver = reached => {
            if (reached.ToString() != stage) return;
            Assert.That(Activate(receiver, destination.ArchivePath, out var nested, out var result), Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.Unavailable)); Assert.That(nested, Is.Null);
            Assert.That(() => receiver.Dispose(), Throws.InvalidOperationException);
            throw new IOException("Injected activation write failure");
        };
        Assert.That(Activate(receiver, destination.ArchivePath, out var absent, out var failed, recover: true), Is.False);
        Assert.That(failed.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.PersistenceFailure));
        Assert.That(failed.Error, Is.EqualTo("Injected activation write failure")); Assert.That(absent, Is.Null);
        Assert.That(File.Exists(destination.ArchivePath), Is.False); Assert.That(Directory.GetFiles(destination.DirectoryPath, "*.tmp-*"), Is.Empty);
        Unchanged(staging.DirectoryPath, staged);
        using (var lease = new FileStream(destination.ArchivePath + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        receiver.ActivationWriteObserver = null;
        Assert.That(Activate(receiver, destination.ArchivePath, out var replica, out var activation, recover: true), Is.True, activation.Error);
        using (replica!) { Assert.That(activation.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.Activated)); Replica(replica!, source); }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void Existing_recovery_requires_explicit_activation_and_a_destination_path(Boolean activate, Boolean path)
    {
        using var cold = new ArchiveDirectory(); var source = Source("Complete", cold);
        using var staging = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest); Finish(receiver, source, true);
        var staged = Inventory(staging.DirectoryPath);
        Assert.That(receiver.TryActivate(source.Manifest.Id, out var absent, out var result, activate, path ? destination.ArchivePath : null,
            VerifyBatch, VerifyCommit, recoverExistingArchive: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.InvalidData)); Assert.That(absent, Is.Null);
        Assert.That(File.Exists(destination.ArchivePath), Is.False); Assert.That(File.Exists(destination.ArchivePath + ".lock"), Is.False);
        Unchanged(staging.DirectoryPath, staged);
    }

    internal static void RunWorker(String control, String operation, String stage)
    {
        var manifest = RoamingNetworkBootstrapManifest.ParseCBOR(File.ReadAllBytes(control + ".manifest"));
        var expected = ETag.Parse(File.ReadAllText(control + ".expected-id")); Assert.That(manifest.Id, Is.EqualTo(expected));
        var paths = File.ReadAllLines(control + ".paths");
        if (operation == "BootstrapManifest")
        {
            using var initial = RoamingNetworkBootstrapReceiver.CreateObserved(paths[0], manifest, null,
                reached => { if (reached.ToString() == stage) ArchiveCrashTestSupport.Exit(control, stage); });
            Assert.Fail("Manifest exit stage was not reached.");
        }
        using var receiver = RoamingNetworkBootstrapReceiver.Open(paths[0], expected);
        if (operation == "BootstrapChunk")
        {
            receiver.WriteObserver = reached => { if (reached.ToString() == stage) ArchiveCrashTestSupport.Exit(control, stage); };
            var chunk = new RoamingNetworkBootstrapChunk(expected, 1, File.ReadAllBytes(control + ".fragment"));
            receiver.TryAcceptChunk(chunk, out var result); Assert.Fail("Chunk exit stage was not reached: " + result.Error);
        }
        else if (operation == "BootstrapActivation")
        {
            receiver.ActivationWriteObserver = reached => { if (reached.ToString() == stage) ArchiveCrashTestSupport.Exit(control, stage); };
            Activate(receiver, paths[1], out var history, out var result); history?.Dispose();
            Assert.Fail("Activation exit stage was not reached: " + result.Error);
        }
        else Assert.Fail("Unknown bootstrap crash operation: " + operation);
    }
}
