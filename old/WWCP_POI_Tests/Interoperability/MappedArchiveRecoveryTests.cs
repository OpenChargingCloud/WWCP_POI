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
public sealed class MappedArchiveRecoveryTests
{
    private static readonly String[] Names = ["history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history"];
    private static Byte[] Bytes(Int32 profile) => File.ReadAllBytes(FilePath(Names[profile - 1] + ".cbor"));
    private static RoamingNetworkHistory Restore(Byte[] bytes)
        => RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);

    private static void Clean(ArchiveDirectory work)
        => Assert.That(Directory.GetFiles(work.DirectoryPath, "wwcp-poi-input.cbor.tmp-*"), Is.Empty);

    private static void Replica(RoamingNetworkHistory history, Byte[] bytes)
    {
        Assert.That(history.ToCBOR(), Is.EqualTo(bytes));
        Assert.That(history.Head.Network.EVSEs.First().Status.Value.ToString(), Is.EqualTo("available"));
        foreach (var commit in history.Commits)
        {
            Assert.That(commit.Signatures, Has.Length.EqualTo(2));
            foreach (var signature in commit.Signatures) Assert.That(VerifyCommit(commit, signature), Is.True);
            if (commit.ChangeSet is not { } batch) continue;
            foreach (var signature in batch.Signatures) Assert.That(VerifyBatch(batch, signature), Is.True);
        }
    }

    private static void Followup(RoamingNetworkHistory history, CancellationTokenSource? cancellation = null)
    {
        cancellation?.Cancel();
        Publish(history, Prepare(history, history.Head.Id, "mapped-followup", Power("225 kW")));
    }

    [Test, Combinatorial]
    public void Open_and_direct_cold_read_release_input_handles_and_preserve_original_peers(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean persistent)
    {
        using var work = new ArchiveDirectory(); var bytes = Bytes(profile); File.WriteAllBytes(work.ArchivePath, bytes);
        using var cancellation = new CancellationTokenSource();
        using var history = persistent
            ? RoamingNetworkHistory.Open(work.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true,
                limits: new(maxArchiveBytes: bytes.Length), cancellationToken: cancellation.Token)
            : RoamingNetworkHistory.ReadColdArchive(work.ArchivePath, ETag.Compute(ETagFormat.CBOR, bytes), VerifyBatch, VerifyCommit,
                authorizeSnapshotBoundary: _ => true, limits: new(maxArchiveBytes: bytes.Length), cancellationToken: cancellation.Token);
        Replica(history, bytes); Clean(work);
        using (var file = new FileStream(work.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        if (persistent) Assert.That(() => RoamingNetworkHistory.Open(work.ArchivePath), Throws.TypeOf<IOException>());
        Followup(history, cancellation);
        if (persistent) Assert.That(File.ReadAllBytes(work.ArchivePath), Is.EqualTo(history.ToCBOR()));
        else Assert.That(File.ReadAllBytes(work.ArchivePath), Is.EqualTo(bytes));
    }

    [Test, Combinatorial]
    public void Known_length_limits_precede_malformed_bytes_and_release_the_file_and_writer_lease(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean persistent)
    {
        using var work = new ArchiveDirectory(); var bytes = Bytes(profile); bytes[^1] = 0xff;
        File.WriteAllBytes(work.ArchivePath, bytes); var calls = 0;
        Boolean Trust(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature signature) { calls++; return true; }
        var error = Assert.Throws<RoamingNetworkHistoryLimitException>(() => {
            using var history = persistent
                ? RoamingNetworkHistory.Open(work.ArchivePath, verifyCommitSignature: Trust, limits: new(maxArchiveBytes: 1))
                : RoamingNetworkHistory.ReadColdArchive(work.ArchivePath, ETag.Compute(ETagFormat.CBOR, bytes),
                    verifyCommitSignature: Trust, limits: new(maxArchiveBytes: 1));
        });
        Assert.That(error!.Violation, Is.EqualTo(new RoamingNetworkHistoryLimitViolation(RoamingNetworkHistoryLimitKind.ArchiveBytes, 1, bytes.Length)));
        Assert.That(calls, Is.Zero);
        using (var file = new FileStream(work.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        File.WriteAllBytes(work.ArchivePath, Bytes(profile));
        using var retry = RoamingNetworkHistory.Open(work.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Replica(retry, Bytes(profile));
    }

    [Test, Combinatorial]
    public void Invalid_file_syntax_matches_span_failure_before_trust(
        [Values(1, 2, 3, 4)] Int32 profile, [Values("empty", "trailing", "truncated", "duplicate")] String fault)
    {
        using var work = new ArchiveDirectory(); var bytes = Bytes(profile); var tree = CBORValue.Parse(bytes);
        var invalid = fault switch {
            "empty" => [], "trailing" => bytes.Append((Byte)0).ToArray(), "truncated" => bytes[..^1],
            _ => CBORValue.FromMap(tree.AsMap().Append(tree.AsMap()[^1])).ToByteArray()
        };
        File.WriteAllBytes(work.ArchivePath, invalid); var calls = 0;
        var expected = Assert.Catch(() => { using var history = Restore(invalid); });
        var actual = Assert.Catch(() => { using var history = RoamingNetworkHistory.Open(work.ArchivePath, VerifyBatch,
            (commit, peer) => { calls++; return VerifyCommit(commit, peer); }, authorizeSnapshotBoundary: _ => true); });
        Assert.That(actual!.GetType(), Is.EqualTo(expected!.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        Assert.That(calls, Is.Zero); Assert.That(File.ReadAllBytes(work.ArchivePath), Is.EqualTo(invalid));
        File.WriteAllBytes(work.ArchivePath, bytes);
        using var retry = RoamingNetworkHistory.Open(work.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Replica(retry, bytes);
    }

    [Test, Combinatorial]
    public void File_recovery_cancellation_releases_input_and_does_not_poison_retry(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean persistent,
        [Values("before", "commit", "batch")] String stage)
    {
        using var work = new ArchiveDirectory(); var bytes = Bytes(profile); File.WriteAllBytes(work.ArchivePath, bytes);
        using var cancellation = new CancellationTokenSource(); if (stage == "before") cancellation.Cancel();
        Boolean Commit(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer)
        { if (stage == "commit") cancellation.Cancel(); return VerifyCommit(commit, peer); }
        Boolean Batch(RoamingNetworkChangeSet batch, RoamingNetworkChangeSetSignature peer)
        { if (stage == "batch") cancellation.Cancel(); return VerifyBatch(batch, peer); }
        var error = Assert.Catch<OperationCanceledException>(() => {
            using var history = persistent
                ? RoamingNetworkHistory.Open(work.ArchivePath, Batch, Commit, authorizeSnapshotBoundary: _ => true, cancellationToken: cancellation.Token)
                : RoamingNetworkHistory.ReadColdArchive(work.ArchivePath, ETag.Compute(ETagFormat.CBOR, bytes), Batch, Commit,
                    authorizeSnapshotBoundary: _ => true, cancellationToken: cancellation.Token);
        });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token)); Assert.That(File.ReadAllBytes(work.ArchivePath), Is.EqualTo(bytes));
        using (var file = new FileStream(work.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        using var retry = RoamingNetworkHistory.Open(work.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Followup(retry);
    }

    [TestCase(3)] [TestCase(4)]
    public void Boundary_cancellation_is_observed_after_root_signatures_before_suffix_and_releases_the_archive(Int32 profile)
    {
        using var work = new ArchiveDirectory(); File.WriteAllBytes(work.ArchivePath, Bytes(profile));
        using var cancellation = new CancellationTokenSource(); var calls = 0;
        Assert.Catch<OperationCanceledException>(() => {
            using var history = RoamingNetworkHistory.Open(work.ArchivePath, VerifyBatch, (commit, peer) => { calls++; return true; },
                authorizeSnapshotBoundary: _ => { cancellation.Cancel(); return true; }, cancellationToken: cancellation.Token);
        });
        Assert.That(calls, Is.EqualTo(2));
        using var file = new FileStream(work.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [TestCase(false)] [TestCase(true)]
    public void Catalog_cancellation_reports_current_candidate_and_never_falls_back(Boolean beforeMatch)
    {
        using var source = History(); var snapshot = LinearHistory(source);
        using var cold = new ArchiveDirectory(); var receipt = Prune(source, Plan(source, snapshot), cold.ArchivePath).Receipt!;
        using var second = new ArchiveDirectory(); File.Copy(cold.ArchivePath, second.ArchivePath);
        var catalog = new RoamingNetworkColdArchiveCatalog([new(receipt.SourceArchiveETag, cold.ArchivePath), new(receipt.SourceArchiveETag, second.ArchivePath)]);
        using var cancellation = new CancellationTokenSource(); var calls = 0;
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(receipt, catalog, out var absent, out var result, VerifyBatch,
            (commit, peer) => { calls++; cancellation.Cancel(); return true; },
            authorizeReceipt: _ => { if (beforeMatch) cancellation.Cancel(); return true; }, cancellationToken: cancellation.Token), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkColdArchiveOutcome.Cancelled)); Assert.That(absent, Is.Null);
        Assert.That(result.Candidates, Has.Length.EqualTo(beforeMatch ? 0 : 1)); Assert.That(calls, Is.EqualTo(beforeMatch ? 0 : 1));
        if (!beforeMatch) Assert.That(result.Candidates[0].Outcome, Is.EqualTo(RoamingNetworkColdArchiveCandidateOutcome.DigestMatched));
        using (var file = new FileStream(cold.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(receipt, catalog, out var retry, out result, VerifyBatch, VerifyCommit), Is.True, result.Error);
        using (retry!) { Assert.That(result.Candidates, Has.Length.EqualTo(1)); }
    }

    [Test, Combinatorial]
    public void Bootstrap_capture_modes_preview_activate_and_recover_exact_bytes(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean inMemory,
        [Values("preview", "memory", "persist", "recover")] String mode)
    {
        var bytes = Bytes(profile); using var source = Restore(bytes); using var staging = new ArchiveDirectory();
        using var work = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        var transfer = source.CreateBootstrap(512); using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, transfer.Manifest);
        Finish(receiver, transfer, true); if (mode == "recover") File.WriteAllBytes(destination.ArchivePath, bytes);
        using var cancellation = new CancellationTokenSource(); var mapped = false;
        Boolean Trust(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer)
        { mapped |= Directory.GetFiles(work.DirectoryPath, "wwcp-poi-input.cbor.tmp-*").Length == 1; return VerifyCommit(commit, peer); }
        receiver.ActivationWriteObserver = reached => {
            Clean(work);
            Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(new RoamingNetworkArchiveReadOptions(0, work.DirectoryPath).TemporaryArchivePath,
                out _, out var result), Is.True, result.Error);
        };
        Assert.That(receiver.TryActivate(transfer.Manifest.Id, out var history, out var result, activate: mode != "preview",
            archivePath: mode is "persist" or "recover" ? destination.ArchivePath : null,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: Trust, authorizeSnapshotBoundary: _ => true,
            recoverExistingArchive: mode == "recover", readOptions: new(inMemory ? bytes.Length : 0, work.DirectoryPath),
            cancellationToken: cancellation.Token), Is.True, result.Error);
        Assert.That(mapped, Is.EqualTo(!inMemory)); Clean(work);
        Assert.That(result.Outcome, Is.EqualTo(mode == "preview" ? RoamingNetworkBootstrapOutcome.ActivationAvailable :
            mode == "recover" ? RoamingNetworkBootstrapOutcome.ActivationRecovered : RoamingNetworkBootstrapOutcome.Activated));
        if (mode == "preview") { Assert.That(history, Is.Null); return; }
        using (history!)
        {
            Replica(history!, bytes);
            if (mode is "persist" or "recover")
            {
                Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(bytes));
                using (var file = new FileStream(destination.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
            }
            Followup(history!, cancellation);
            if (mode is "persist" or "recover") Assert.That(File.ReadAllBytes(destination.ArchivePath), Is.EqualTo(history!.ToCBOR()));
        }
    }

    [Test, Combinatorial]
    public void Bootstrap_capture_failure_or_cancellation_cleans_only_owned_files_and_allows_retry(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean cancel,
        [Values("FileCreated", "BeforeWrite", "BeforeMap", "Trust")] String stage)
    {
        using var source = Restore(Bytes(profile)); var transfer = source.CreateBootstrap(512);
        using var staging = new ArchiveDirectory(); using var work = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, transfer.Manifest); Finish(receiver, transfer, true);
        var staged = Directory.GetFiles(staging.DirectoryPath).Where(path => !path.EndsWith(".lock", StringComparison.Ordinal)).ToDictionary(path => Path.GetFileName(path)!, File.ReadAllBytes);
        var orphan = Path.Combine(work.DirectoryPath, "wwcp-poi-input.cbor.tmp-" + Guid.NewGuid().ToString("N")); File.WriteAllBytes(orphan, [42]);
        using var cancellation = new CancellationTokenSource();
        void Fault() { if (cancel) cancellation.Cancel(); else throw new IOException("Injected input capture failure"); }
        receiver.InputObserver = (reached, _) => { if (reached.ToString() == stage) Fault(); };
        Assert.That(receiver.TryActivate(transfer.Manifest.Id, out var absent, out var result, activate: true, archivePath: destination.ArchivePath,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: (commit, peer) => { if (stage == "Trust") Fault(); return VerifyCommit(commit, peer); },
            authorizeSnapshotBoundary: _ => true, readOptions: new(0, work.DirectoryPath), cancellationToken: cancellation.Token), Is.False);
        Assert.That(absent, Is.Null);
        // Complete replay wraps callback errors; snapshot-root verification propagates I/O directly.
        Assert.That(result.Outcome, Is.EqualTo(cancel ? RoamingNetworkBootstrapOutcome.Cancelled : stage == "Trust" && profile < 3 ?
            RoamingNetworkBootstrapOutcome.InvalidData : RoamingNetworkBootstrapOutcome.PersistenceFailure));
        Assert.That(File.Exists(destination.ArchivePath), Is.False); Assert.That(File.ReadAllBytes(orphan), Is.EqualTo(new Byte[] { 42 }));
        Assert.That(Directory.GetFiles(work.DirectoryPath, "*.tmp-*"), Is.EqualTo(new[] { orphan }));
        foreach (var (name, bytes) in staged) Assert.That(File.ReadAllBytes(Path.Combine(staging.DirectoryPath, name!)), Is.EqualTo(bytes));
        receiver.InputObserver = null;
        Assert.That(receiver.TryActivate(transfer.Manifest.Id, out var retry, out result, activate: true, archivePath: destination.ArchivePath,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: _ => true,
            readOptions: new(0, work.DirectoryPath)), Is.True, result.Error);
        using (retry!) { Replica(retry!, Bytes(profile)); }
        Assert.That(Directory.GetFiles(work.DirectoryPath, "*.tmp-*"), Is.EqualTo(new[] { orphan }));
    }

    [Test, Combinatorial]
    public void Bootstrap_cancellation_at_manifest_and_batch_boundaries_preserves_staging_for_retry(
        [Values(1, 2, 3, 4)] Int32 profile, [Values("before", "manifest", "batch")] String stage)
    {
        using var source = Restore(Bytes(profile)); var transfer = source.CreateBootstrap(512);
        using var staging = new ArchiveDirectory(); using var work = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, transfer.Manifest); Finish(receiver, transfer, true);
        using var cancellation = new CancellationTokenSource(); if (stage == "before") cancellation.Cancel();
        var commitCalls = 0;
        Assert.That(receiver.TryActivate(transfer.Manifest.Id, out var absent, out var result, activate: true,
            verifyBatchSignature: (batch, peer) => { if (stage == "batch") cancellation.Cancel(); return VerifyBatch(batch, peer); },
            verifyCommitSignature: (commit, peer) => { commitCalls++; return VerifyCommit(commit, peer); },
            authorizeBootstrap: _ => { if (stage == "manifest") cancellation.Cancel(); return true; },
            authorizeSnapshotBoundary: _ => true, readOptions: new(0, work.DirectoryPath), cancellationToken: cancellation.Token), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.Cancelled)); Assert.That(absent, Is.Null); Clean(work);
        if (stage != "batch") Assert.That(commitCalls, Is.Zero);
        Assert.That(receiver.TryActivate(transfer.Manifest.Id, out var retry, out result, activate: true,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: _ => true,
            readOptions: new(0, work.DirectoryPath)), Is.True, result.Error);
        using (retry!) { Replica(retry!, Bytes(profile)); } Clean(work);
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Cancellation_after_installation_starts_does_not_hide_durable_activation(Int32 profile)
    {
        using var source = Restore(Bytes(profile)); var transfer = source.CreateBootstrap(512);
        using var staging = new ArchiveDirectory(); using var work = new ArchiveDirectory(); using var destination = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, transfer.Manifest); Finish(receiver, transfer, true);
        using var cancellation = new CancellationTokenSource(); receiver.ActivationWriteObserver = reached => { Clean(work); cancellation.Cancel(); };
        Assert.That(receiver.TryActivate(transfer.Manifest.Id, out var history, out var result, activate: true, archivePath: destination.ArchivePath,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: _ => true,
            readOptions: new(0, work.DirectoryPath), cancellationToken: cancellation.Token), Is.True, result.Error);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.Activated)); using (history!) { Replica(history!, Bytes(profile)); Followup(history!); }
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Mapped_input_blocks_conflicting_writes_until_trust_finishes(Int32 profile)
    {
        if (!OperatingSystem.IsWindows()) Assert.Ignore("Windows file sharing contract.");
        using var work = new ArchiveDirectory(); File.WriteAllBytes(work.ArchivePath, Bytes(profile)); var attempts = 0;
        using (var history = RoamingNetworkHistory.ReadColdArchive(work.ArchivePath, ETag.Compute(ETagFormat.CBOR, Bytes(profile)), VerifyBatch,
            (commit, peer) => { attempts++; Assert.That(() => File.WriteAllBytes(work.ArchivePath, [0]), Throws.TypeOf<IOException>()); return VerifyCommit(commit, peer); },
            authorizeSnapshotBoundary: _ => true)) Replica(history, Bytes(profile));
        Assert.That(attempts, Is.GreaterThan(0));
        using var file = new FileStream(work.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
    }

    [Test]
    public void Scoped_mapping_owns_existing_files_but_borrows_spool_handles_and_rejects_use_after_disposal()
    {
        using var work = new ArchiveDirectory(); File.WriteAllBytes(work.ArchivePath, [1, 2, 3]);
        var input = new POIArchiveMappedFile(work.ArchivePath, new()); Assert.That(input.Bytes.ToArray(), Is.EqualTo(new Byte[] { 1, 2, 3 }));
        input.Dispose(); input.Dispose(); Assert.That(() => input.Bytes.ToArray(), Throws.TypeOf<ObjectDisposedException>());
        using var file = new FileStream(work.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        using (var borrowed = new POIArchiveMappedFile(file, new())) Assert.That(borrowed.Length, Is.EqualTo(3));
        file.Position = 0; Assert.That(file.ReadByte(), Is.EqualTo(1));
    }

    [Test, Combinatorial]
    public async Task Process_exit_during_mapping_releases_capture_files_and_writer_leases_for_retry(
        [Values(1, 2, 3, 4)] Int32 profile, [Values("MappedOpen", "MappedCold", "MappedBootstrap")] String operation)
    {
        using var control = new ArchiveDirectory(); using var staging = new ArchiveDirectory(); using var work = new ArchiveDirectory();
        File.WriteAllBytes(control.ArchivePath, Bytes(profile)); using var source = Restore(Bytes(profile)); var transfer = source.CreateBootstrap(512);
        File.WriteAllBytes(control.ArchivePath + ".manifest", transfer.Manifest.ToCBOR());
        File.WriteAllLines(control.ArchivePath + ".paths", [staging.DirectoryPath, work.DirectoryPath]);
        using (var initial = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, transfer.Manifest)) Finish(initial, transfer, true);
        await ArchiveCrashTestSupport.Run(control.ArchivePath, "MappedTrust", operation);
        Clean(work); Assert.That(File.ReadAllBytes(control.ArchivePath), Is.EqualTo(Bytes(profile)));
        using (var history = RoamingNetworkHistory.Open(control.ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true)) Replica(history, Bytes(profile));
        using var receiver = RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, transfer.Manifest.Id);
        Assert.That(receiver.TryActivate(transfer.Manifest.Id, out var retry, out var result, activate: true,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: _ => true,
            readOptions: new(0, work.DirectoryPath)), Is.True, result.Error);
        using (retry!) { Replica(retry!, Bytes(profile)); } Clean(work);
    }

    internal static void RunWorker(String path, String operation, String stage)
    {
        Boolean Trust(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer)
        { ArchiveCrashTestSupport.Exit(path, stage); return true; }
        if (operation == "MappedOpen")
        { using var history = RoamingNetworkHistory.Open(path, VerifyBatch, Trust, authorizeSnapshotBoundary: _ => true); }
        else if (operation == "MappedCold")
        { using var history = RoamingNetworkHistory.ReadColdArchive(path, ETag.Compute(ETagFormat.CBOR, File.ReadAllBytes(path)), VerifyBatch, Trust, authorizeSnapshotBoundary: _ => true); }
        else
        {
            var paths = File.ReadAllLines(path + ".paths"); var manifest = RoamingNetworkBootstrapManifest.ParseCBOR(File.ReadAllBytes(path + ".manifest"));
            using var receiver = RoamingNetworkBootstrapReceiver.Open(paths[0], manifest.Id);
            receiver.TryActivate(manifest.Id, out var history, out _, activate: true, verifyBatchSignature: VerifyBatch, verifyCommitSignature: Trust,
                authorizeSnapshotBoundary: _ => true, readOptions: new(0, paths[1])); history?.Dispose();
        }
        Assert.Fail("The mapped recovery exit point was not reached.");
    }
}
