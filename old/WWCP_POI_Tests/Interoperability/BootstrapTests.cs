/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class BootstrapTests
{
    private static RoamingNetworkHistory BranchedHistory()
    {
        var history = History(); Store(history, Sign(history.Head.Commit));
        var root = history.Head.Id;
        var left = Prepare(history, root, "left", Power("150 kW")); Publish(history, left);
        var right = Prepare(history, root, "right", Rename("Remote")); Store(history, right);
        var hidden = Prepare(history, root, "hidden", Power("200 kW")); Store(history, hidden);
        Publish(history, Merge(history, left, right));
        return history;
    }

    private static RoamingNetworkBootstrapChunk Transport(RoamingNetworkBootstrapChunk chunk, Boolean cbor)
        => cbor ? RoamingNetworkBootstrapChunk.ParseCBOR(chunk.ToCBOR()) : RoamingNetworkBootstrapChunk.Parse(chunk.ToJSON());

    private static void Finish(RoamingNetworkBootstrapReceiver receiver, RoamingNetworkBootstrapSource source, Boolean cbor = true)
    {
        while (receiver.NextChunk < source.Manifest.ChunkCount)
            Assert.That(receiver.TryAcceptChunk(Transport(source.CreateChunk(receiver.NextChunk), cbor), out var receipt), Is.True, receipt.Error);
    }

    private static Boolean Activate(RoamingNetworkBootstrapReceiver receiver, out RoamingNetworkHistory? history,
        out RoamingNetworkBootstrapResult result, Boolean activate = false, String? path = null,
        Func<RoamingNetworkCommit, Boolean>? authorize = null)
        => receiver.TryActivate(receiver.Manifest.Id, out history, out result, activate, path, VerifyBatch, VerifyCommit, authorize);

    [TestCase(false)]
    [TestCase(true)]
    public void A_frozen_multi_page_bootstrap_resumes_then_explicitly_activates_original_signed_history(Boolean cbor)
    {
        using var sender = BranchedHistory(); using var staging = new ArchiveDirectory();
        var local = Network();
        using var unrelated = History(local.ApplyChangeSet(Batch(local.DataSnapshot, "different-checkpoint", Rename("Local"))));
        Status(unrelated, EvseTarget, "charging");
        var untouched = new Observation(unrelated);
        Status(sender, EvseTarget, "reserved");
        sender.Head.Network.EVSEs.Single().MaxPowerRealTime = new(InteropFixture.Time.AddDays(2), Watt.Parse("75 kW"));
        sender.Head.Network.EVSEs.Single().MaxPowerPrognoses.Add(new Timestamped<Watt>(InteropFixture.Time.AddDays(3), Watt.Parse("50 kW")));
        var source = sender.CreateBootstrap(256); var manifest = source.Manifest;
        var expected = sender.ToCBOR(); var expectedJSON = sender.ToJSON();
        Assert.That(manifest.ChunkCount, Is.GreaterThan(2));
        manifest = cbor ? RoamingNetworkBootstrapManifest.ParseCBOR(manifest.ToCBOR()) : RoamingNetworkBootstrapManifest.Parse(manifest.ToJSON());
        Assert.That(manifest.Id, Is.EqualTo(source.Manifest.Id));
        using (var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, manifest))
        {
            Assert.That(receiver.TryAcceptChunk(Transport(source.CreateChunk(0), cbor), out var result), Is.True, result.Error);
            Assert.That(Activate(receiver, out var absent, out result, activate: true), Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.Incomplete)); Assert.That(absent, Is.Null);
        }
        // The sender advances after the session was frozen; resumed bytes still describe the earlier head.
        Publish(sender, Prepare(sender, sender.Head.Id, "later", Power("175 kW")));
        using var resumed = RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, manifest.Id);
        Assert.That(resumed.NextChunk, Is.EqualTo(1));
        Assert.That(resumed.TryAcceptChunk(Transport(source.CreateChunk(0), cbor), out var duplicate), Is.True);
        Assert.That(duplicate.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.AlreadyStored));
        Finish(resumed, source, cbor);
        Assert.That(Activate(resumed, out var previewHistory, out var preview), Is.True, preview.Error);
        Assert.That(preview.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.ActivationAvailable)); Assert.That(previewHistory, Is.Null);
        untouched.AssertUnchanged(unrelated);
        Assert.That(Activate(resumed, out var activated, out var result2, activate: true), Is.True, result2.Error);
        using var replica = activated!;
        Assert.That(replica.ToCBOR(), Is.EqualTo(expected));
        Assert.That(CanonicalJSON.Serialize(Value(replica.ToJSON())), Is.EqualTo(CanonicalJSON.Serialize(Value(expectedJSON))));
        Assert.That(replica.GetReplicationState().Checkpoint, Is.Not.EqualTo(unrelated.GetReplicationState().Checkpoint));
        Assert.That(replica.Head.Id, Is.EqualTo(manifest.Head));
        Assert.That(replica.Commits, Has.Length.EqualTo(manifest.CommitCount));
        Assert.That(replica.Head.Commit.Signatures, Has.Length.EqualTo(2));
        Assert.That(replica.Head.Commit.ChangeSet!.Signatures, Has.Length.EqualTo(2));
        Assert.That(replica.Commits.Any(commit => commit.ChangeSet?.Id == "hidden"), Is.True);
        Assert.That(replica.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
        Assert.That(replica.Head.Network.EVSEs.Single().MaxPowerRealTime, Is.Null);
        Assert.That(replica.Head.Network.EVSEs.Single().MaxPowerPrognoses, Is.Empty);
        Assert.That(sender.TryCreateCommitPack(replica.GetReplicationState(), sender.Head.Id, out var pack, out var export), Is.True, export.Error);
        Assert.That(replica.TryImportCommitPack(ReplicationTestSupport.Transport(pack!, cbor), out var imported), Is.True, imported.Error);
        Assert.That(replica.TryAdoptHead(manifest.Head, sender.Head.Id, out var adopted, adopt: true), Is.True, adopted.Error);
        Assert.That(replica.Head.Id, Is.EqualTo(sender.Head.Id)); untouched.AssertUnchanged(unrelated);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Corrupt_wrong_manifest_out_of_order_and_wrong_length_fragments_do_not_advance_progress(Boolean cbor)
    {
        using var sender = BranchedHistory(); using var directory = new ArchiveDirectory();
        var source = sender.CreateBootstrap(256);
        using var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, source.Manifest);
        var original = source.CreateChunk(0); var corrupt = original.Data.ToArray(); corrupt[0] ^= 1;
        var other = ETag.Compute(ETagFormat.JSON, Encoding.UTF8.GetBytes("other"));
        foreach (var chunk in new[] {
            new RoamingNetworkBootstrapChunk(other, 0, original.Data.AsSpan()),
            new RoamingNetworkBootstrapChunk(source.Manifest.Id, 0, corrupt),
            new RoamingNetworkBootstrapChunk(source.Manifest.Id, 0, original.Data.AsSpan()[..^1]),
            source.CreateChunk(1), new RoamingNetworkBootstrapChunk(source.Manifest.Id, source.Manifest.ChunkCount, corrupt)
        })
        {
            Assert.That(receiver.TryAcceptChunk(Transport(chunk, cbor), out _), Is.False);
            Assert.That(receiver.NextChunk, Is.Zero); Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.chunk"), Is.Empty);
        }
        Finish(receiver, source, cbor);
        Assert.That(receiver.TryActivate(other, out var absent, out var result, activate: true), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.ManifestMismatch)); Assert.That(absent, Is.Null);
        Assert.That(Activate(receiver, out absent, out result), Is.True, result.Error); Assert.That(absent, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Raw_and_encoded_bounds_are_exact_and_manifests_cannot_enlarge_local_limits(Boolean cbor)
    {
        using var sender = History(); var bytes = sender.ToCBOR();
        var probe = sender.CreateBootstrap(bytes.Length); var chunk = probe.CreateChunk(0);
        var wire = Math.Max(chunk.ToCBOR().Length, Encoding.UTF8.GetByteCount(chunk.ToJSON()));
        var manifestBytes = Math.Max(probe.Manifest.ToCBOR().Length, Encoding.UTF8.GetByteCount(probe.Manifest.ToJSON()));
        var limits = new RoamingNetworkBootstrapLimits(bytes.Length, bytes.Length, 1, 1, wire, manifestBytes);
        var source = sender.CreateBootstrap(bytes.Length, limits);
        var decoded = cbor ? RoamingNetworkBootstrapChunk.ParseCBOR(chunk.ToCBOR(), limits) : RoamingNetworkBootstrapChunk.Parse(chunk.ToJSON(), limits);
        var manifest = cbor ? RoamingNetworkBootstrapManifest.ParseCBOR(source.Manifest.ToCBOR(), limits) : RoamingNetworkBootstrapManifest.Parse(source.Manifest.ToJSON(), limits);
        using var directory = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, manifest, limits);
        Assert.That(receiver.TryAcceptChunk(decoded, out var result), Is.True, result.Error);
        Assert.That(Activate(receiver, out var absent, out result), Is.True, result.Error); Assert.That(absent, Is.Null);
        Assert.That(() => sender.CreateBootstrap(bytes.Length, new(bytes.Length - 1)), Throws.ArgumentException);
        Assert.That(() => sender.CreateBootstrap(bytes.Length, new(maxChunkBytes: bytes.Length - 1)), Throws.ArgumentException);
        Assert.That(() => sender.CreateBootstrap(bytes.Length, new(maxWireBytes: wire - 1)), Throws.ArgumentException);
        Assert.That(() => sender.CreateBootstrap(bytes.Length, new(maxManifestBytes: manifestBytes - 1)), Throws.ArgumentException);
        Assert.That(() => sender.CreateBootstrap(1, new(maxChunks: 1)), Throws.ArgumentException);
        var rawBound = cbor ? chunk.ToCBOR().Length : Encoding.UTF8.GetByteCount(chunk.ToJSON());
        Assert.That(() => { if (cbor) RoamingNetworkBootstrapChunk.ParseCBOR(chunk.ToCBOR(), new(maxWireBytes: rawBound - 1));
                          else RoamingNetworkBootstrapChunk.Parse(chunk.ToJSON(), new(maxWireBytes: rawBound - 1)); }, Throws.ArgumentException);
        Assert.That(() => { if (cbor) RoamingNetworkBootstrapManifest.ParseCBOR(manifest.ToCBOR(), new(maxChunks: 1, maxArchiveBytes: bytes.Length - 1));
                          else RoamingNetworkBootstrapManifest.Parse(manifest.ToJSON(), new(maxArchiveBytes: bytes.Length - 1)); }, Throws.ArgumentException);
        using var branched = BranchedHistory();
        var excessive = Assert.Throws<RoamingNetworkHistoryLimitException>(() => branched.CreateBootstrap(limits: new(maxCommits: 1)))!;
        Assert.That(excessive.Violation, Is.EqualTo(new RoamingNetworkHistoryLimitViolation(RoamingNetworkHistoryLimitKind.RetainedCommits, 1, 5)));
        Assert.That(() => new RoamingNetworkBootstrapLimits(maxArchiveBytes: 0), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase("BeforeTemporaryWrite")]
    [TestCase("TemporaryFileFlushed")]
    public void Failed_fragment_receipts_roll_back_then_resume_from_verified_disk(String stage)
    {
        using var sender = History(); using var directory = new ArchiveDirectory(); var source = sender.CreateBootstrap(256);
        using (var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, source.Manifest))
        {
            receiver.WriteObserver = reached => { if (reached.ToString() == stage) throw new IOException("Injected bootstrap write failure"); };
            Assert.That(receiver.TryAcceptChunk(source.CreateChunk(0), out var result), Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.PersistenceFailure)); Assert.That(receiver.NextChunk, Is.Zero);
            Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.chunk"), Is.Empty);
            Assert.That(Directory.EnumerateFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        }
        using var reopened = RoamingNetworkBootstrapReceiver.Open(directory.DirectoryPath, source.Manifest.Id);
        Finish(reopened, source); Assert.That(Activate(reopened, out var replica, out var activation, true), Is.True, activation.Error);
        replica!.Dispose();
    }

    [TestCase("digest")]
    [TestCase("length")]
    [TestCase("gap")]
    [TestCase("manifest")]
    public void Resume_revalidates_acknowledged_files_and_rejects_disk_corruption(String kind)
    {
        using var sender = History(); using var directory = new ArchiveDirectory(); var source = sender.CreateBootstrap(256);
        using (var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, source.Manifest)) Finish(receiver, source);
        var first = Path.Combine(directory.DirectoryPath, "00000000.chunk");
        if (kind == "digest") { var bytes = File.ReadAllBytes(first); bytes[0] ^= 1; File.WriteAllBytes(first, bytes); }
        if (kind == "length") File.AppendAllText(first, "extra");
        if (kind == "gap") File.Delete(first);
        var expected = kind == "manifest" ? ETag.Compute(ETagFormat.JSON, [1]) : source.Manifest.Id;
        Assert.That(() => RoamingNetworkBootstrapReceiver.Open(directory.DirectoryPath, expected), Throws.ArgumentException);
    }

    [Test]
    public void Orphan_temporary_writes_are_not_progress_and_writer_leases_prevent_parallel_staging()
    {
        using var sender = History(); using var directory = new ArchiveDirectory(); var source = sender.CreateBootstrap(256);
        using (var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, source.Manifest))
        {
            Assert.That(() => RoamingNetworkBootstrapReceiver.Open(directory.DirectoryPath, source.Manifest.Id), Throws.TypeOf<IOException>());
            Assert.That(() => RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, source.Manifest), Throws.TypeOf<IOException>());
            Assert.That(receiver.TryAcceptChunk(source.CreateChunk(0), out _), Is.True);
        }
        File.WriteAllBytes(Path.Combine(directory.DirectoryPath, "00000001.chunk.tmp-interrupted"), [1, 2, 3]);
        using var reopened = RoamingNetworkBootstrapReceiver.Open(directory.DirectoryPath, source.Manifest.Id);
        Assert.That(reopened.NextChunk, Is.EqualTo(1)); Finish(reopened, source);
        Assert.That(Activate(reopened, out var replica, out var result, true), Is.True, result.Error); replica!.Dispose();
    }

    [Test]
    public void Preview_does_not_persist_and_activation_uses_fresh_trust_including_unpublished_branches()
    {
        using var sender = BranchedHistory(); using var directory = new ArchiveDirectory(); using var archive = new ArchiveDirectory();
        var source = sender.CreateBootstrap(512); using var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, source.Manifest);
        Finish(receiver, source);
        Assert.That(Activate(receiver, out var absent, out var result, path: archive.ArchivePath), Is.True, result.Error);
        Assert.That(absent, Is.Null); Assert.That(File.Exists(archive.ArchivePath), Is.False);
        Assert.That(Activate(receiver, out absent, out result, true, archive.ArchivePath, commit => commit.ChangeSet?.Id != "hidden"), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.InvalidData)); Assert.That(absent, Is.Null);
        Assert.That(File.Exists(archive.ArchivePath), Is.False);
        Assert.That(receiver.TryActivate(source.Manifest.Id, out absent, out result, activate: true), Is.False); // Signed peers require verifiers.
        Assert.That(absent, Is.Null);
        Assert.That(receiver.TryActivate(source.Manifest.Id, out absent, out result, activate: true,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeBootstrap: _ => false), Is.False);
        Assert.That(absent, Is.Null);
        Assert.That(Activate(receiver, out var activated, out result, true, archive.ArchivePath), Is.True, result.Error);
        using (activated!) Assert.That(File.ReadAllBytes(archive.ArchivePath), Is.EqualTo(sender.ToCBOR()));
        using var recovered = RoamingNetworkHistory.Open(archive.ArchivePath, VerifyBatch, VerifyCommit);
        Assert.That(recovered.Head.Id, Is.EqualTo(source.Manifest.Head)); Assert.That(recovered.ToCBOR(), Is.EqualTo(sender.ToCBOR()));
        Publish(recovered, Prepare(recovered, recovered.Head.Id, "continued", Power("175 kW")));
    }

    [Test]
    public void Existing_archives_and_failed_activation_paths_are_preserved_and_can_be_retried()
    {
        using var sender = BranchedHistory(); using var staging = new ArchiveDirectory(); using var archive = new ArchiveDirectory();
        using var current = RoamingNetworkHistory.CreatePersistent(archive.ArchivePath, Network(), VerifyBatch, VerifyCommit);
        Status(current, EvseTarget, "charging"); var unchanged = new Observation(current); var before = File.ReadAllBytes(archive.ArchivePath);
        var source = sender.CreateBootstrap(512); using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest);
        Finish(receiver, source);
        Assert.That(Activate(receiver, out var absent, out var result, true, archive.ArchivePath), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.PersistenceFailure)); Assert.That(absent, Is.Null);
        unchanged.AssertUnchanged(current); Assert.That(File.ReadAllBytes(archive.ArchivePath), Is.EqualTo(before));
        current.Dispose();
        Assert.That(Activate(receiver, out absent, out result, true, archive.ArchivePath), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.PersistenceFailure)); Assert.That(absent, Is.Null);
        Assert.That(File.ReadAllBytes(archive.ArchivePath), Is.EqualTo(before));
        var blocker = Path.Combine(archive.DirectoryPath, "blocker"); File.WriteAllText(blocker, "existing file");
        Assert.That(Activate(receiver, out absent, out result, true, Path.Combine(blocker, "history.cbor")), Is.False);
        Assert.That(absent, Is.Null); Assert.That(File.ReadAllText(blocker), Is.EqualTo("existing file"));
        var fresh = Path.Combine(archive.DirectoryPath, "new.cbor");
        Assert.That(Activate(receiver, out var activated, out result, true, fresh), Is.True, result.Error); activated!.Dispose();
        Assert.That(File.ReadAllBytes(fresh), Is.EqualTo(sender.ToCBOR()));
    }

    [Test]
    public void Reentrant_callbacks_and_disposed_receivers_cannot_accept_or_activate()
    {
        using var sender = History(); using var directory = new ArchiveDirectory(); var source = sender.CreateBootstrap(512);
        var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, source.Manifest); Finish(receiver, source);
        Assert.That(receiver.TryActivate(source.Manifest.Id, out var replica, out var result, activate: true, authorizeBootstrap: _ => {
            Assert.That(receiver.TryAcceptChunk(source.CreateChunk(0), out var reentry), Is.False);
            Assert.That(reentry.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.Unavailable));
            Assert.That(() => receiver.Dispose(), Throws.InvalidOperationException); return true;
        }), Is.True, result.Error);
        replica!.Dispose(); receiver.Dispose();
        Assert.That(receiver.TryAcceptChunk(source.CreateChunk(0), out result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.Unavailable));
        Assert.That(Activate(receiver, out var absent, out result, true), Is.False); Assert.That(absent, Is.Null);
        sender.Dispose(); Assert.That(() => sender.CreateBootstrap(), Throws.InvalidOperationException);
    }

    private static CBORValue Replace(CBORValue map, String name, CBORValue value)
        => CBORValue.FromMap(map.AsMap().Select(pair => new KeyValuePair<CBORValue, CBORValue>(pair.Key,
            pair.Key.TryGetText(out var key) && key == name ? value : pair.Value)));

    private static RoamingNetworkBootstrapManifest ManifestFor(Byte[] bytes, RoamingNetworkBootstrapManifest original,
        Int32? count = null, RoamingNetworkCommitId? checkpoint = null, RoamingNetworkCommitId? head = null, ETag? digest = null)
        => new(checkpoint ?? original.Checkpoint, head ?? original.Head, count ?? original.CommitCount, bytes.Length, 256,
            digest ?? ETag.Compute(ETagFormat.CBOR, bytes), Enumerable.Range(0, (bytes.Length + 255) / 256)
                .Select(index => ImmutableArray.CreateRange(SHA256.HashData(bytes.AsSpan(index * 256, Math.Min(256, bytes.Length - index * 256))))).ToImmutableArray());

    [TestCase("ArchiveDigest")]
    [TestCase("Head")]
    [TestCase("Checkpoint")]
    [TestCase("Count")]
    [TestCase("Profile")]
    [TestCase("ContentProfile")]
    [TestCase("Snapshot")]
    [TestCase("MissingParent")]
    [TestCase("DuplicateCommit")]
    [TestCase("CommitIdentity")]
    [TestCase("CommitSignature")]
    [TestCase("BatchSignature")]
    [TestCase("ResultETags")]
    [TestCase("OperationPrecondition")]
    [TestCase("BatchId")]
    public void Complete_self_consistent_fragment_receipts_cannot_bypass_archive_or_history_validation(String fault)
    {
        using var sender = BranchedHistory(); using var directory = new ArchiveDirectory(); var source = sender.CreateBootstrap(256);
        var map = CBORValue.Parse(sender.ToCBOR()); var fields = map.AsMap().ToDictionary(pair => pair.Key.AsText(), pair => pair.Value);
        var commits = fields["Commits"].AsArray().ToArray(); var count = source.Manifest.CommitCount;
        if (fault == "Profile") map = Replace(map, "Profile", CBORValue.FromText("unknown"));
        if (fault == "ContentProfile") map = Replace(map, "ContentProfile", CBORValue.FromText("unknown"));
        if (fault == "Snapshot") map = Replace(map, "Checkpoint", Replace(fields["Checkpoint"], "name", CBORValue.FromText("tampered")));
        if (fault == "MissingParent")
        {
            map = Replace(map, "Commits", CBORValue.FromArray(commits.Where(commit => RoamingNetworkCommit.ParseCBORValue(commit).Id != sender.Head.Commit.Parents[0])));
            count--;
        }
        if (fault == "DuplicateCommit") { map = Replace(map, "Commits", CBORValue.FromArray(commits.Append(commits[0]))); count++; }
        if (fault == "CommitIdentity") { commits[0] = Replace(commits[0], "Revision", CBORValue.FromInt64(100)); map = Replace(map, "Commits", CBORValue.FromArray(commits)); }
        if (fault is "CommitSignature" or "BatchSignature")
        {
            var commit = RoamingNetworkCommit.ParseCBORValue(commits[^1]);
            commit = fault == "CommitSignature"
                ? commit.WithSignature(new("Ed25519", "fixture-alice", Convert.ToBase64String(new Byte[64]), RoamingNetworkCommit.SigningProfile))
                : commit.WithChangeSet(commit.ChangeSet!.WithSignature(new("Ed25519", "fixture-alice", Convert.ToBase64String(new Byte[64]))));
            commits[^1] = commit.ToCBORValue(); map = Replace(map, "Commits", CBORValue.FromArray(commits));
        }
        RoamingNetworkCommitId? overrideHead = null;
        if (fault is "ResultETags" or "OperationPrecondition" or "BatchId")
        {
            var parent = sender.Head.Commit;
            var valid = Batch(sender.Head.Snapshot, "invalid-next", Power("175 kW"));
            var batch = fault switch {
                "ResultETags" => new RoamingNetworkChangeSet(valid.Id, valid.RoamingNetworkId, valid.BaseRevision,
                    valid.CreatedAt, valid.Changes, valid.BeforeETags, valid.BeforeETags),
                "OperationPrecondition" => new RoamingNetworkChangeSet(valid.Id, valid.RoamingNetworkId, valid.BaseRevision,
                    valid.CreatedAt, [RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Value("\"777 kW\""), Value("\"175 kW\""))],
                    valid.BeforeETags, valid.AfterETags),
                _ => new RoamingNetworkChangeSet("left", valid.RoamingNetworkId, valid.BaseRevision,
                    valid.CreatedAt, valid.Changes, valid.BeforeETags, valid.AfterETags)
            };
            var invalid = Sign(RoamingNetworkCommit.Create(parent, Sign(batch)));
            overrideHead = invalid.Id;
            map = Replace(Replace(map, "Commits", CBORValue.FromArray(commits.Append(invalid.ToCBORValue()))), "Head", invalid.Id.ToCBOR()); count++;
        }
        var bytes = map.ToByteArray(CBORWriterOptions.Canonical);
        var otherId = new RoamingNetworkCommitId(ETag.Compute(ETagFormat.JSON, [1]));
        var manifest = ManifestFor(bytes, source.Manifest, fault == "Count" ? count + 1 : count,
            checkpoint: fault == "Checkpoint" ? otherId : null, head: fault == "Head" ? otherId : overrideHead,
            digest: fault == "ArchiveDigest" ? ETag.Compute(ETagFormat.CBOR, [1]) : null);
        using var receiver = RoamingNetworkBootstrapReceiver.Create(directory.DirectoryPath, manifest);
        for (var index = 0; index < manifest.ChunkCount; index++)
            Assert.That(receiver.TryAcceptChunk(new(manifest.Id, index, bytes.AsSpan(index * 256, Math.Min(256, bytes.Length - index * 256))), out var result), Is.True, result.Error);
        Assert.That(Activate(receiver, out var absent, out var rejected, true), Is.False);
        Assert.That(rejected.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.InvalidData)); Assert.That(absent, Is.Null);
    }

    [Test]
    public void Wire_parsers_reject_unknown_profiles_duplicates_trailing_bytes_and_noncanonical_base64()
    {
        using var history = History(); var source = history.CreateBootstrap(512); var manifest = source.Manifest; var chunk = source.CreateChunk(0);
        Assert.That(() => RoamingNetworkBootstrapManifest.Parse(manifest.ToJSON().Replace(RoamingNetworkBootstrapManifest.Profile, "unknown")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkBootstrapManifest.Parse(manifest.ToJSON().Replace(POIContentProfile.Id, "unknown")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkBootstrapManifest.Parse(manifest.ToJSON().Replace("\"CommitCount\":1", "\"CommitCount\":1,\"CommitCount\":1")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkBootstrapManifest.Parse(manifest.ToJSON().Replace("\"CommitCount\":1", "\"CommitCount\":2")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkBootstrapChunk.Parse(chunk.ToJSON().Replace(RoamingNetworkBootstrapChunk.Profile, "unknown")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkBootstrapChunk.Parse(chunk.ToJSON().Replace("\"Index\":0", "\"Index\":0,\"Index\":0")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkBootstrapChunk.Parse(chunk.ToJSON() + "{}"), Throws.InstanceOf<System.Text.Json.JsonException>());
        Assert.That(() => RoamingNetworkBootstrapChunk.ParseCBOR(chunk.ToCBOR().Concat(new Byte[] { 0 }).ToArray()), Throws.Exception);
        Assert.That(() => RoamingNetworkBootstrapManifest.ParseCBOR(manifest.ToCBOR().Concat(new Byte[] { 0 }).ToArray()), Throws.Exception);
        Assert.That(() => RoamingNetworkBootstrapChunk.Parse(chunk.ToJSON().Replace("\"Data\":\"", "\"Data\":\" ")), Throws.ArgumentException);
    }
}
