/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ArchiveLimitsTests
{
    private static IEnumerable<TestCaseData> Archives()
    {
        foreach (var name in new[] { "history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history" })
            foreach (var cbor in new[] { false, true }) yield return new(name, cbor);
    }

    private sealed class TrustSpy
    {
        internal Int32 Calls;
        internal Boolean Batch(RoamingNetworkChangeSet batch, RoamingNetworkChangeSetSignature signature)
        { Calls++; return VerifyBatch(batch, signature); }
        internal Boolean Commit(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature signature)
        { Calls++; return VerifyCommit(commit, signature); }
        internal Boolean Boundary(RoamingNetworkSnapshotBoundary boundary) { Calls++; return true; }
    }

    private static RoamingNetworkHistoryLimits ExactLimits(String name, Boolean cbor)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(FilePath(name + ".json")));
        var root = document.RootElement;
        var receipts = root.TryGetProperty("RetentionReceipts", out var catalog) ? catalog.GetArrayLength() : 0;
        var ids = receipts == 0 ? 0 : catalog.EnumerateArray().Sum(receipt =>
            receipt.GetProperty("PrunedCommits").GetArrayLength() + receipt.GetProperty("ArchiveOnlyTips").GetArrayLength());
        return new(File.ReadAllBytes(FilePath(name + (cbor ? ".cbor" : ".json"))).Length,
            root.GetProperty("Commits").GetArrayLength() + 1, Math.Max(1, receipts), Math.Max(1, ids));
    }

    private static RoamingNetworkHistory Decode(String name, Boolean cbor, RoamingNetworkHistoryLimits limits, TrustSpy spy)
        => cbor ? RoamingNetworkHistory.ParseCBOR(File.ReadAllBytes(FilePath(name + ".cbor")), spy.Batch, spy.Commit,
                      authorizeSnapshotBoundary: spy.Boundary, limits: limits) :
                  RoamingNetworkHistory.Parse(File.ReadAllText(FilePath(name + ".json")), spy.Batch, spy.Commit,
                      authorizeSnapshotBoundary: spy.Boundary, limits: limits);

    private static void Violation(TestDelegate action, RoamingNetworkHistoryLimitKind kind, Int64 maximum, Int64 observed)
    {
        var exception = Assert.Throws<RoamingNetworkHistoryLimitException>(action)!;
        Assert.That(exception.Violation, Is.EqualTo(new RoamingNetworkHistoryLimitViolation(kind, maximum, observed)));
    }

    [TestCaseSource(nameof(Archives))]
    public void All_profiles_accept_exact_encoded_bytes_and_item_limits(String name, Boolean cbor)
    {
        var spy = new TrustSpy(); using var recovered = Decode(name, cbor, ExactLimits(name, cbor), spy);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(File.ReadAllBytes(FilePath(name + ".cbor"))));
        Assert.That(spy.Calls, Is.GreaterThan(0));
    }

    [TestCaseSource(nameof(Archives))]
    public void All_profiles_reject_one_excess_byte_before_any_trust_callback(String name, Boolean cbor)
    {
        var exact = ExactLimits(name, cbor); var spy = new TrustSpy();
        Violation(() => Decode(name, cbor, new(maxArchiveBytes: exact.MaxArchiveBytes - 1), spy),
            RoamingNetworkHistoryLimitKind.ArchiveBytes, exact.MaxArchiveBytes - 1, exact.MaxArchiveBytes);
        Assert.That(spy.Calls, Is.Zero);
    }

    [TestCaseSource(nameof(Archives))]
    public void All_profiles_count_the_root_and_reject_excess_commits_before_replay(String name, Boolean cbor)
    {
        var exact = ExactLimits(name, cbor); var spy = new TrustSpy();
        Violation(() => Decode(name, cbor, new(maxCommits: exact.MaxCommits - 1), spy),
            RoamingNetworkHistoryLimitKind.RetainedCommits, exact.MaxCommits - 1, exact.MaxCommits);
        Assert.That(spy.Calls, Is.Zero);
    }

    [Test]
    public void JSON_byte_limit_measures_UTF8_and_counts_escaped_archive_keys()
    {
        const String json = "{\"説明\":\"🔌\",\"\\u0043ommits\":[null,null]}";
        var bytes = Encoding.UTF8.GetByteCount(json);
        Assert.That(bytes, Is.GreaterThan(json.Length));
        Violation(() => RoamingNetworkHistory.Parse(json, limits: new(maxArchiveBytes: bytes - 1)),
            RoamingNetworkHistoryLimitKind.ArchiveBytes, bytes - 1, bytes);
        Violation(() => RoamingNetworkHistory.Parse(json, limits: new(maxArchiveBytes: bytes, maxCommits: 2)),
            RoamingNetworkHistoryLimitKind.RetainedCommits, 2, 3);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CBOR_commit_limits_cover_definite_and_indefinite_containers(Boolean indefinite)
    {
        var bytes = new Byte[] { 0xA1, 0x67, 0x43, 0x6F, 0x6D, 0x6D, 0x69, 0x74, 0x73,
            indefinite ? (Byte) 0x9F : (Byte) 0x82, 0xF6, 0xF6 };
        if (indefinite) bytes = [.. bytes, 0xFF];
        Violation(() => RoamingNetworkHistory.ParseCBOR(bytes, limits: new(maxCommits: 2)),
            RoamingNetworkHistoryLimitKind.RetainedCommits, 2, 3);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void File_byte_limits_reject_large_lengths_before_allocating_or_hashing(Boolean coldRead)
    {
        using var directory = new ArchiveDirectory();
        var digest = ETag.Compute(ETagFormat.CBOR, new Byte[] { 0 });
        const Int64 length = (Int64) Int32.MaxValue + 1;
        using (var file = new FileStream(directory.ArchivePath, FileMode.CreateNew, FileAccess.Write, FileShare.None)) file.SetLength(length);
        Violation(() => {
            using var ignored = coldRead ? RoamingNetworkHistory.ReadColdArchive(directory.ArchivePath, digest, limits: new(maxArchiveBytes: 1024)) :
                                          RoamingNetworkHistory.Open(directory.ArchivePath, limits: new(maxArchiveBytes: 1024));
        }, RoamingNetworkHistoryLimitKind.ArchiveBytes, 1024, length);
        Assert.That(new FileInfo(directory.ArchivePath).Length, Is.EqualTo(length));
        if (!coldRead)
        {
            using var lease = new FileStream(directory.ArchivePath + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Corrupt_file_recovery_releases_the_lease_and_never_replaces_input(Boolean coldRead)
    {
        using var directory = new ArchiveDirectory(); var bytes = File.ReadAllBytes(FilePath("pruned-history.cbor"));
        bytes = bytes[..^1]; File.WriteAllBytes(directory.ArchivePath, bytes);
        var digest = ETag.Compute(ETagFormat.CBOR, bytes); var spy = new TrustSpy();
        Assert.That(() => {
            using var ignored = coldRead ? RoamingNetworkHistory.ReadColdArchive(directory.ArchivePath, digest, spy.Batch, spy.Commit,
                authorizeSnapshotBoundary: spy.Boundary) : RoamingNetworkHistory.Open(directory.ArchivePath, spy.Batch, spy.Commit,
                authorizeSnapshotBoundary: spy.Boundary);
        }, Throws.Exception);
        Assert.That(spy.Calls, Is.Zero); Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(bytes));
        if (!coldRead)
        {
            using var lease = new FileStream(directory.ArchivePath + ".lock", FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
    }

    private static CBORValue Replace(CBORValue map, String name, CBORValue value)
        => CBORValue.FromMap(map.AsMap().Select(pair => new KeyValuePair<CBORValue, CBORValue>(pair.Key,
            pair.Key.AsText() == name ? value : pair.Value)));

    [TestCase(false)]
    [TestCase(true)]
    public void Small_retained_suffix_cannot_hide_a_large_catalog_or_duplicate_ID_entries(Boolean cbor)
    {
        var limits = new RoamingNetworkHistoryLimits(maxCommits: 2, maxRetentionReceipts: 1, maxCatalogCommitIds: 16);
        var spy = new TrustSpy();
        if (cbor)
        {
            var map = CBORValue.Parse(File.ReadAllBytes(FilePath("pruned-history.cbor")));
            var receipt = map.AsMap().Single(pair => pair.Key.AsText() == "RetentionReceipts").Value.AsArray()[0];
            var id = receipt.AsMap().Single(pair => pair.Key.AsText() == "PrunedCommits").Value.AsArray()[0];
            receipt = Replace(receipt, "PrunedCommits", CBORValue.FromArray(Enumerable.Repeat(id, 1000)));
            map = Replace(map, "RetentionReceipts", CBORValue.FromArray([receipt]));
            Violation(() => RoamingNetworkHistory.ParseCBOR(map.ToByteArray(CBORWriterOptions.Canonical), spy.Batch, spy.Commit,
                authorizeSnapshotBoundary: spy.Boundary, limits: limits), RoamingNetworkHistoryLimitKind.CatalogCommitIds, 16, 1000);
        }
        else
        {
            var node = JsonNode.Parse(File.ReadAllText(FilePath("pruned-history.json")))!;
            var receipt = node["RetentionReceipts"]![0]!; var id = receipt["PrunedCommits"]![0]!;
            receipt["PrunedCommits"] = new JsonArray(Enumerable.Range(0, 1000).Select(_ => id.DeepClone()).ToArray());
            Violation(() => RoamingNetworkHistory.Parse(node.ToJsonString(), spy.Batch, spy.Commit,
                authorizeSnapshotBoundary: spy.Boundary, limits: limits), RoamingNetworkHistoryLimitKind.CatalogCommitIds, 16, 17);
        }
        Assert.That(spy.Calls, Is.Zero);
    }

    private static RoamingNetworkHistory CatalogHistory(ArchiveDirectory cold)
    {
        var history = History(); var first = LinearHistory(history);
        var released = Prepare(history, history.CheckpointId, "released", Rename("Cold branch")); Store(history, released);
        Prune(history, Plan(history, first, [released.Id]), Path.Combine(cold.DirectoryPath, "first.cbor"));
        Publish(history, Prepare(history, history.Head.Id, "between", Power("175 kW")));
        var second = Snapshot(history, 11); Publish(history, second);
        Publish(history, Prepare(history, history.Head.Id, "later", Rename("Later")));
        Prune(history, Plan(history, second), Path.Combine(cold.DirectoryPath, "second.cbor"));
        return history;
    }

    [TestCase(false, RoamingNetworkHistoryLimitKind.RetentionReceipts)]
    [TestCase(true, RoamingNetworkHistoryLimitKind.RetentionReceipts)]
    [TestCase(false, RoamingNetworkHistoryLimitKind.CatalogCommitIds)]
    [TestCase(true, RoamingNetworkHistoryLimitKind.CatalogCommitIds)]
    public void Receipt_and_catalog_limits_are_aggregate_and_exact_limits_preserve_repeated_bookkeeping(Boolean cbor,
        RoamingNetworkHistoryLimitKind kind)
    {
        using var cold = new ArchiveDirectory(); using var history = CatalogHistory(cold);
        var ids = history.RetentionReceipts.Sum(receipt => receipt.PrunedCommits.Length + receipt.ArchiveOnlyTips.Length);
        var maximum = kind == RoamingNetworkHistoryLimitKind.RetentionReceipts ? 1 : ids - 1;
        var exact = new RoamingNetworkHistoryLimits(maxCommits: 2, maxRetentionReceipts: 2, maxCatalogCommitIds: ids);
        using var recovered = cbor ? RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), VerifyBatch, VerifyCommit,
            authorizeSnapshotBoundary: AcceptChain(history), limits: exact) : RoamingNetworkHistory.Parse(history.ToJSON(), VerifyBatch, VerifyCommit,
            authorizeSnapshotBoundary: AcceptChain(history), limits: exact);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(history.ToCBOR()));
        Assert.That(ids, Is.GreaterThan(history.RetentionReceipts.SelectMany(receipt => receipt.PrunedCommits.Concat(receipt.ArchiveOnlyTips)).Distinct().Count()));
        var limits = new RoamingNetworkHistoryLimits(maxCommits: 2,
            maxRetentionReceipts: kind == RoamingNetworkHistoryLimitKind.RetentionReceipts ? maximum : 2,
            maxCatalogCommitIds: kind == RoamingNetworkHistoryLimitKind.CatalogCommitIds ? maximum : ids);
        var spy = new TrustSpy(); var before = new Observation(history);
        Violation(() => {
            using var ignored = cbor ? RoamingNetworkHistory.ParseCBOR(history.ToCBOR(), spy.Batch, spy.Commit,
                authorizeSnapshotBoundary: spy.Boundary, limits: limits) : RoamingNetworkHistory.Parse(history.ToJSON(), spy.Batch, spy.Commit,
                authorizeSnapshotBoundary: spy.Boundary, limits: limits);
        }, kind, maximum, maximum + 1);
        Assert.That(spy.Calls, Is.Zero); before.AssertUnchanged(history);
    }

    [TestCase(false, RoamingNetworkHistoryLimitKind.ArchiveBytes)]
    [TestCase(true, RoamingNetworkHistoryLimitKind.ArchiveBytes)]
    [TestCase(false, RoamingNetworkHistoryLimitKind.RetainedCommits)]
    [TestCase(true, RoamingNetworkHistoryLimitKind.RetainedCommits)]
    [TestCase(false, RoamingNetworkHistoryLimitKind.RetentionReceipts)]
    [TestCase(true, RoamingNetworkHistoryLimitKind.RetentionReceipts)]
    [TestCase(false, RoamingNetworkHistoryLimitKind.CatalogCommitIds)]
    [TestCase(true, RoamingNetworkHistoryLimitKind.CatalogCommitIds)]
    public void File_recovery_rejections_keep_archive_and_runtime_and_release_the_writer_lease(Boolean coldRead,
        RoamingNetworkHistoryLimitKind kind)
    {
        using var cold = new ArchiveDirectory(); using var history = CatalogHistory(cold); using var directory = new ArchiveDirectory();
        Status(history, EvseTarget, "charging"); var before = new Observation(history); var bytes = history.ToCBOR();
        File.WriteAllBytes(directory.ArchivePath, bytes); var digest = ETag.Compute(ETagFormat.CBOR, bytes);
        var ids = history.RetentionReceipts.Sum(receipt => receipt.PrunedCommits.Length + receipt.ArchiveOnlyTips.Length);
        var maximum = kind switch {
            RoamingNetworkHistoryLimitKind.ArchiveBytes => bytes.Length - 1,
            RoamingNetworkHistoryLimitKind.RetainedCommits => 1,
            RoamingNetworkHistoryLimitKind.RetentionReceipts => 1,
            _ => ids - 1
        };
        var limits = new RoamingNetworkHistoryLimits(
            maxArchiveBytes: kind == RoamingNetworkHistoryLimitKind.ArchiveBytes ? maximum : bytes.Length,
            maxCommits: kind == RoamingNetworkHistoryLimitKind.RetainedCommits ? maximum : 2,
            maxRetentionReceipts: kind == RoamingNetworkHistoryLimitKind.RetentionReceipts ? maximum : 2,
            maxCatalogCommitIds: kind == RoamingNetworkHistoryLimitKind.CatalogCommitIds ? maximum : ids);
        var spy = new TrustSpy();
        Violation(() => {
            using var ignored = coldRead ? RoamingNetworkHistory.ReadColdArchive(directory.ArchivePath, digest, spy.Batch, spy.Commit,
                authorizeSnapshotBoundary: spy.Boundary, limits: limits) : RoamingNetworkHistory.Open(directory.ArchivePath, spy.Batch, spy.Commit,
                authorizeSnapshotBoundary: spy.Boundary, limits: limits);
        }, kind, maximum, maximum + 1);
        Assert.That(spy.Calls, Is.Zero); before.AssertUnchanged(history);
        Assert.That(File.ReadAllBytes(directory.ArchivePath), Is.EqualTo(bytes));
        Assert.That(Directory.GetFiles(directory.DirectoryPath, "*.tmp-*"), Is.Empty);
        if (coldRead) Assert.That(File.Exists(directory.ArchivePath + ".lock"), Is.False);
        var exact = new RoamingNetworkHistoryLimits(bytes.Length, 2, 2, ids);
        using var recovered = coldRead ? RoamingNetworkHistory.ReadColdArchive(directory.ArchivePath, digest, VerifyBatch, VerifyCommit,
            authorizeSnapshotBoundary: AcceptChain(history), limits: exact) : RoamingNetworkHistory.Open(directory.ArchivePath, VerifyBatch, VerifyCommit,
            authorizeSnapshotBoundary: AcceptChain(history), limits: exact);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(bytes));
    }

    private static RoamingNetworkBootstrapManifest Manifest(Byte[] bytes, RoamingNetworkHistory history, Int32 count)
        => new(history.CheckpointId, history.Head.Id, count, bytes.Length, 256, ETag.Compute(ETagFormat.CBOR, bytes),
            Enumerable.Range(0, (bytes.Length + 255) / 256).Select(index => ImmutableArray.CreateRange(
                SHA256.HashData(bytes.AsSpan(index * 256, Math.Min(256, bytes.Length - index * 256))))).ToImmutableArray(),
            history.HasCompleteAncestry ? RoamingNetworkHistory.SnapshotArchiveProfile : RoamingNetworkHistory.RetentionArchiveProfile, history.AnchorId);

    [TestCase(RoamingNetworkHistoryLimitKind.RetainedCommits, false)]
    [TestCase(RoamingNetworkHistoryLimitKind.RetainedCommits, true)]
    [TestCase(RoamingNetworkHistoryLimitKind.RetentionReceipts, false)]
    [TestCase(RoamingNetworkHistoryLimitKind.RetentionReceipts, true)]
    [TestCase(RoamingNetworkHistoryLimitKind.CatalogCommitIds, false)]
    [TestCase(RoamingNetworkHistoryLimitKind.CatalogCommitIds, true)]
    public void Bootstrap_checks_actual_archive_counts_and_keeps_staging_and_active_state_on_rejection(
        RoamingNetworkHistoryLimitKind kind, Boolean activate)
    {
        using var cold = new ArchiveDirectory(); using var history = CatalogHistory(cold);
        using var staging = new ArchiveDirectory(); using var active = new ArchiveDirectory();
        Status(history, EvseTarget, "charging"); var before = new Observation(history);
        var bytes = history.ToCBOR(); var ids = history.RetentionReceipts.Sum(receipt => receipt.PrunedCommits.Length + receipt.ArchiveOnlyTips.Length);
        var limits = new RoamingNetworkBootstrapLimits(maxCommits: kind == RoamingNetworkHistoryLimitKind.RetainedCommits ? 1 : 2,
            maxRetentionReceipts: kind == RoamingNetworkHistoryLimitKind.RetentionReceipts ? 1 : 2,
            maxCatalogCommitIds: kind == RoamingNetworkHistoryLimitKind.CatalogCommitIds ? ids - 1 : ids);
        // A dishonest low manifest count fits staging limits, but the actual archive has two commits.
        var manifest = Manifest(bytes, history, kind == RoamingNetworkHistoryLimitKind.RetainedCommits ? 1 : 2);
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, manifest, limits);
        for (var index = 0; index < manifest.ChunkCount; index++)
        {
            var length = Math.Min(256, bytes.Length - index * 256);
            var chunk = new RoamingNetworkBootstrapChunk(manifest.Id, index, bytes.AsSpan(index * 256, length));
            Assert.That(receiver.TryAcceptChunk(chunk, out var receipt), Is.True, receipt.Error);
        }
        File.WriteAllBytes(active.ArchivePath, [1, 2, 3]);
        var staged = Directory.GetFiles(staging.DirectoryPath).Where(path => Path.GetFileName(path) != "bootstrap.lock")
            .ToDictionary(path => Path.GetFileName(path), File.ReadAllBytes);
        var spy = new TrustSpy();
        Assert.That(receiver.TryActivate(manifest.Id, out var absent, out var result, activate, active.ArchivePath,
            spy.Batch, spy.Commit, authorizeSnapshotBoundary: spy.Boundary), Is.False);
        Assert.That(absent, Is.Null); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkBootstrapOutcome.LimitExceeded));
        var maximum = kind == RoamingNetworkHistoryLimitKind.CatalogCommitIds ? ids - 1 : 1;
        Assert.That(result.LimitViolation, Is.EqualTo(new RoamingNetworkHistoryLimitViolation(kind, maximum, maximum + 1)));
        Assert.That(result.NextChunk, Is.EqualTo(manifest.ChunkCount)); Assert.That(spy.Calls, Is.Zero);
        Assert.That(File.ReadAllBytes(active.ArchivePath), Is.EqualTo(new Byte[] { 1, 2, 3 }));
        Assert.That(File.Exists(active.ArchivePath + ".lock"), Is.False); before.AssertUnchanged(history);
        foreach (var path in Directory.GetFiles(staging.DirectoryPath).Where(path => Path.GetFileName(path) != "bootstrap.lock"))
            Assert.That(File.ReadAllBytes(path), Is.EqualTo(staged[Path.GetFileName(path)]));
        Assert.That(Directory.GetFiles(staging.DirectoryPath), Has.Length.EqualTo(staged.Count + 1));
    }

    [Test]
    public void Bootstrap_exact_archive_and_catalog_limits_reopen_and_activate_with_fresh_runtime()
    {
        using var cold = new ArchiveDirectory(); using var sender = CatalogHistory(cold);
        using var staging = new ArchiveDirectory(); using var archive = new ArchiveDirectory();
        Status(sender, EvseTarget, "charging"); var bytes = sender.ToCBOR();
        var ids = sender.RetentionReceipts.Sum(receipt => receipt.PrunedCommits.Length + receipt.ArchiveOnlyTips.Length);
        var limits = new RoamingNetworkBootstrapLimits(maxArchiveBytes: bytes.Length, maxCommits: 2, maxRetentionReceipts: 2, maxCatalogCommitIds: ids);
        var source = sender.CreateBootstrap(256, limits);
        using (var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, source.Manifest, limits)) Finish(receiver, source, true);
        using var resumed = RoamingNetworkBootstrapReceiver.Open(staging.DirectoryPath, source.Manifest.Id, limits);
        Assert.That(resumed.TryActivate(source.Manifest.Id, out var replica, out var result, true, archive.ArchivePath,
            VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AcceptChain(sender)), Is.True, result.Error);
        using (replica!)
        {
            Assert.That(replica!.ToCBOR(), Is.EqualTo(bytes)); Assert.That(result.LimitViolation, Is.Null);
            Assert.That(replica.Head.Network, Is.Not.SameAs(sender.Head.Network));
            Assert.That(replica.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.Not.EqualTo("charging"));
        }
        Assert.That(File.ReadAllBytes(archive.ArchivePath), Is.EqualTo(bytes));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Bootstrap_source_checks_catalog_limits_before_freezing_an_archive(Boolean snapshotExport)
    {
        using var cold = new ArchiveDirectory(); using var history = CatalogHistory(cold); var before = new Observation(history);
        var limits = new RoamingNetworkBootstrapLimits(maxRetentionReceipts: 1);
        Violation(() => {
            if (snapshotExport) history.CreateSnapshotBootstrap(history.AnchorId, limits: limits);
            else history.CreateBootstrap(limits: limits);
        }, RoamingNetworkHistoryLimitKind.RetentionReceipts, 1, 2);
        before.AssertUnchanged(history);
    }

    [TestCase("bytes")]
    [TestCase("commits")]
    [TestCase("receipts")]
    [TestCase("ids")]
    public void Local_archive_limits_must_be_positive(String setting)
    {
        Assert.That(() => new RoamingNetworkHistoryLimits(setting == "bytes" ? 0 : 1, setting == "commits" ? 0 : 1,
            setting == "receipts" ? 0 : 1, setting == "ids" ? 0 : 1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => new RoamingNetworkBootstrapLimits(maxRetentionReceipts: setting == "receipts" ? 0 : 1,
            maxCatalogCommitIds: setting == "ids" ? 0 : 1, maxArchiveBytes: setting == "bytes" ? 0 : 1,
            maxCommits: setting == "commits" ? 0 : 1), Throws.TypeOf<ArgumentOutOfRangeException>());
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Malformed_or_truncated_containers_are_rejected_before_trust(Boolean cbor)
    {
        var spy = new TrustSpy();
        if (cbor)
        {
            // An impossible 2^64-1 array length is rejected without allocating its declared size.
            var malformed = new Byte[] { 0xA1, 0x67, 0x43, 0x6F, 0x6D, 0x6D, 0x69, 0x74, 0x73, 0x9B,
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
            Assert.That(() => RoamingNetworkHistory.ParseCBOR(malformed, spy.Batch, spy.Commit), Throws.Exception);
            var bytes = File.ReadAllBytes(FilePath("pruned-history.cbor"));
            Assert.That(() => RoamingNetworkHistory.ParseCBOR(bytes.AsSpan(0, bytes.Length - 1), spy.Batch, spy.Commit), Throws.Exception);
        }
        else
        {
            Assert.That(() => RoamingNetworkHistory.Parse("{\"Commits\":[", spy.Batch, spy.Commit), Throws.InstanceOf<JsonException>());
            Assert.That(() => RoamingNetworkHistory.Parse("{\"RetentionReceipts\":[{\"PrunedCommits\":null}]}", spy.Batch, spy.Commit), Throws.TypeOf<JsonException>());
        }
        Assert.That(spy.Calls, Is.Zero);
    }
}
