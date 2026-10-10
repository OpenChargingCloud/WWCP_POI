/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Reflection;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ParserCancellationTests
{
    private static readonly String[] Names = ["history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history"];
    private static Byte[] Bytes(Int32 profile) => File.ReadAllBytes(FilePath(Names[profile - 1] + ".cbor"));
    private delegate void Limits(ReadOnlySpan<Byte> bytes, RoamingNetworkHistoryLimits limits, POIArchiveReadProgress progress);
    private static readonly Limits Scan = typeof(RoamingNetworkHistory).GetMethod("CheckCBORArchiveCore", BindingFlags.NonPublic | BindingFlags.Static)!
        .CreateDelegate<Limits>();

    private sealed class Workspace : IDisposable
    {
        internal String DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), "wwcp-poi-parser-cancel-" + Guid.NewGuid().ToString("N"));
        internal String Orphan => Path.Combine(DirectoryPath, "wwcp-poi-input.cbor.tmp-unrelated");
        internal Workspace() { Directory.CreateDirectory(DirectoryPath); File.WriteAllText(Orphan, "keep"); }
        internal void Clean()
        {
            Assert.That(Directory.GetFiles(DirectoryPath, "wwcp-poi-input.cbor.tmp-*"), Is.EqualTo(new[] { Orphan }));
            Assert.That(File.ReadAllText(Orphan), Is.EqualTo("keep"));
            using var lease = new FileStream(Path.Combine(DirectoryPath, "wwcp-poi-input.cbor.lock"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
        }
        public void Dispose() => Directory.Delete(DirectoryPath, true);
    }

    private static RoamingNetworkHistory Restore(Byte[] bytes, CancellationToken token,
        Action<POIArchiveReadStage, Int32>? observer, RoamingNetworkBootstrapManifest? manifest = null,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? commitTrust = null)
        => RoamingNetworkHistory.RestoreCapturedInput(bytes, new(), VerifyBatch, commitTrust ?? VerifyCommit, null, _ => true, token, manifest, observer);

    private static IEnumerable<TestCaseData> CancellationCases()
    {
        for (var profile = 1; profile <= 4; profile++)
        foreach (var bootstrap in new[] { false, true })
        foreach (var file in new[] { false, true })
        {
            var stages = new List<POIArchiveReadStage> { POIArchiveReadStage.Limits, POIArchiveReadStage.Index,
                POIArchiveReadStage.CommitModel, POIArchiveReadStage.RootState, POIArchiveReadStage.Replay, POIArchiveReadStage.HeadState };
            if (bootstrap) stages.Add(POIArchiveReadStage.Canonical);
            if (profile >= 3) stages.Add(POIArchiveReadStage.SuffixCapture);
            if (profile == 4) stages.Add(POIArchiveReadStage.ReceiptModel);
            foreach (var stage in stages) yield return new(profile, bootstrap, file, stage.ToString());
        }
    }

    [TestCaseSource(nameof(CancellationCases))]
    public void Exact_parser_boundary_cancellation_releases_owned_input_and_private_state_then_retries(
        Int32 profile, Boolean bootstrap, Boolean file, String stageName)
    {
        var stage = Enum.Parse<POIArchiveReadStage>(stageName); var bytes = Bytes(profile); var original = (Byte[])bytes.Clone();
        using var source = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        var manifest = bootstrap ? source.CreateBootstrap().Manifest : null;
        using var work = new Workspace(); using var cancellation = new CancellationTokenSource();
        var observed = 0; var calls = 0; var threshold = stage is POIArchiveReadStage.Index or POIArchiveReadStage.Canonical ? 20 : 2;
        POICanonicalPreparation? context = null;
        void Observe(POIArchiveReadStage reached, Int32 position)
        {
            context ??= POICanonicalPreparation.Current;
            if (reached != stage) return;
            if (++observed == threshold) cancellation.Cancel();
        }
        var error = Assert.Catch<OperationCanceledException>(() => {
            using var spool = new POIArchiveInputSpool(new(file ? 0 : Int32.MaxValue, work.DirectoryPath), new(), false);
            spool.Append(bytes);
            using var history = spool.Read(input => RoamingNetworkHistory.RestoreCapturedInput(input, new(), VerifyBatch,
                (commit, peer) => { calls++; return VerifyCommit(commit, peer); }, null, _ => true, cancellation.Token, manifest, Observe));
        });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token)); Assert.That(observed, Is.EqualTo(threshold));
        Assert.That(bytes, Is.EqualTo(original)); Assert.That(source.ToCBOR(), Is.EqualTo(original)); work.Clean();
        Assert.That(POICanonicalPreparation.Current, Is.Null);
        if (context is not null) { Assert.That(context.Entries, Is.Zero); Assert.That(context.Bytes, Is.Zero); }
        if (stage is POIArchiveReadStage.Limits or POIArchiveReadStage.Index or POIArchiveReadStage.Canonical or
            POIArchiveReadStage.CommitModel or POIArchiveReadStage.ReceiptModel or POIArchiveReadStage.SuffixCapture ||
            stage == POIArchiveReadStage.RootState && profile <= 2) Assert.That(calls, Is.Zero);
        else Assert.That(calls, Is.GreaterThan(0));
        calls = 0;
        using var retry = Restore(bytes, default, null, manifest, (commit, peer) => { calls++; return VerifyCommit(commit, peer); });
        Assert.That(retry.ToCBOR(), Is.EqualTo(original)); Assert.That(calls, Is.GreaterThan(0));
        Assert.That(retry.Head.Network, Is.Not.SameAs(source.Head.Network)); Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    [Test, Combinatorial]
    public void Successful_reads_release_the_token_before_later_publication(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean bootstrap)
    {
        var bytes = Bytes(profile); using var source = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        using var cancellation = new CancellationTokenSource(); var observed = new HashSet<POIArchiveReadStage>();
        using var history = Restore(bytes, cancellation.Token, (stage, position) => observed.Add(stage), bootstrap ? source.CreateBootstrap().Manifest : null);
        Assert.That(history.ToCBOR(), Is.EqualTo(bytes)); Assert.That(observed, Does.Contain(POIArchiveReadStage.Index));
        if (bootstrap) Assert.That(observed, Does.Contain(POIArchiveReadStage.Canonical));
        cancellation.Cancel(); var head = history.Head;
        var batch = Sign(Batch(head.Snapshot, "after-parser-token", Power("175 kW")));
        var commit = Sign(history.PrepareCommit(head.Id, batch));
        Assert.That(history.TryPublish(head.Id, commit, out var result), Is.True, result.Error);
        Assert.That(POICanonicalPreparation.Current, Is.Null);
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Prepared_indexes_and_slices_match_the_frozen_preceding_algorithm(Int32 profile)
    {
        var bytes = Bytes(profile); using var token = new CancellationTokenSource();
        var expected = ParserCancellationIndexOracle.Read(bytes); var actual = POIArchiveCBORIndex.Read(bytes, new(token.Token));
        Assert.That(actual.Commits.Select(part => (part.Offset, part.Length)), Is.EqualTo(expected.Commits.Select(part => (part.Offset, part.Length))));
        Assert.That(actual.Receipts.Select(part => (part.Offset, part.Length)), Is.EqualTo(expected.Receipts.Select(part => (part.Offset, part.Length))));
        Assert.That(actual.Value(bytes, "Profile"), Is.EqualTo(expected.Value(bytes, "Profile")));
        ParserCancellationIndexOracle.RequireCanonical(bytes); POIArchiveCBORIndex.RequireCanonical(bytes, new(token.Token));
        ParserCancellationLimitsOracle.CheckCBORArchive(bytes, new()); Scan(bytes, new(), new(token.Token));
    }

    [Test, Combinatorial]
    public void Cancellation_after_a_suffix_model_preserves_eager_and_lazy_trust_order(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean bootstrap, [Values(false, true)] Boolean file)
    {
        var bytes = Bytes(profile); var part = POIArchiveCBORIndex.Read(bytes).Commits[^1];
        using var source = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        var manifest = bootstrap ? source.CreateBootstrap().Manifest : null;
        using var work = new Workspace(); using var cancellation = new CancellationTokenSource(); var calls = 0;
        POICanonicalPreparation? context = null;
        var error = Assert.Catch<OperationCanceledException>(() => {
            using var spool = new POIArchiveInputSpool(new(file ? 0 : Int32.MaxValue, work.DirectoryPath), new(), false);
            spool.Append(bytes);
            using var history = spool.Read(input => RoamingNetworkHistory.RestoreCapturedInput(input, new(), VerifyBatch,
                (commit, peer) => { calls++; return VerifyCommit(commit, peer); }, null, _ => true, cancellation.Token, manifest,
                (stage, position) => {
                    context ??= POICanonicalPreparation.Current;
                    if (stage == POIArchiveReadStage.CommitModel && position == part.Offset + part.Length) cancellation.Cancel();
                }));
        });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token));
        Assert.That(calls, profile <= 2 ? Is.Zero : Is.GreaterThan(0));
        work.Clean(); Assert.That(context, Is.Not.Null); Assert.That(context!.Entries, Is.Zero); Assert.That(context.Bytes, Is.Zero);
        Assert.That(POICanonicalPreparation.Current, Is.Null); Assert.That(source.ToCBOR(), Is.EqualTo(bytes));
        using var retry = Restore(bytes, default, null, manifest); Assert.That(retry.ToCBOR(), Is.EqualTo(bytes));
    }

    [Test, Combinatorial]
    public void Active_and_default_tokens_preserve_exact_archive_budget_violations(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean active)
    {
        var bytes = Bytes(profile); var count = POIArchiveCBORIndex.Read(bytes).Commits.Length + 1;
        using var cancellation = new CancellationTokenSource(); var progress = new POIArchiveReadProgress(active ? cancellation.Token : default);
        foreach (var limits in new[] { new RoamingNetworkHistoryLimits(maxArchiveBytes: bytes.Length - 1),
                                      new RoamingNetworkHistoryLimits(maxCommits: count - 1) })
        {
            var expected = Assert.Throws<RoamingNetworkHistoryLimitException>(() => ParserCancellationLimitsOracle.CheckCBORArchive(bytes, limits))!;
            var actual = Assert.Throws<RoamingNetworkHistoryLimitException>(() => Scan(bytes, limits, progress))!;
            Assert.That(actual.Violation, Is.EqualTo(expected.Violation)); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        }
        Scan(bytes, new(maxArchiveBytes: bytes.Length, maxCommits: count), progress);
    }

    [Test, Combinatorial]
    public void Catalog_budget_errors_preserve_definite_and_indefinite_container_counts(
        [Values(false, true)] Boolean indefinite, [Values(false, true)] Boolean active)
    {
        using var cancellation = new CancellationTokenSource(); var progress = new POIArchiveReadProgress(active ? cancellation.Token : default);
        var receipts = Convert.FromHexString("A171526574656E74696F6E5265636569707473" + (indefinite ? "9FA0A0FF" : "82A0A0"));
        var ids = Convert.FromHexString("A171526574656E74696F6E526563656970747381A16D5072756E6564436F6D6D697473" +
            (indefinite ? "9FF6F6FF" : "82F6F6"));
        foreach (var bytes in new[] { receipts, ids })
        {
            var limits = new RoamingNetworkHistoryLimits(maxRetentionReceipts: 1, maxCatalogCommitIds: 1);
            var expected = Assert.Throws<RoamingNetworkHistoryLimitException>(() => ParserCancellationLimitsOracle.CheckCBORArchive(bytes, limits))!;
            var actual = Assert.Throws<RoamingNetworkHistoryLimitException>(() => Scan(bytes, limits, progress))!;
            Assert.That(actual.Violation, Is.EqualTo(expected.Violation)); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        }
    }

    [Test, Combinatorial]
    public void Ordinary_parser_observer_failure_releases_private_state_and_owned_input(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean file)
    {
        var bytes = Bytes(profile); using var work = new Workspace(); using var cancellation = new CancellationTokenSource();
        var expected = new InvalidOperationException("parser observer failure"); POICanonicalPreparation? context = null;
        var actual = Assert.Throws<InvalidOperationException>(() => {
            using var spool = new POIArchiveInputSpool(new(file ? 0 : Int32.MaxValue, work.DirectoryPath), new(), false);
            spool.Append(bytes);
            using var history = spool.Read(input => Restore(input.ToArray(), cancellation.Token, (stage, position) => {
                context ??= POICanonicalPreparation.Current;
                if (stage == POIArchiveReadStage.HeadState) throw expected;
            }));
        });
        Assert.That(actual, Is.SameAs(expected)); Assert.That(cancellation.IsCancellationRequested, Is.False);
        work.Clean(); Assert.That(context, Is.Not.Null); Assert.That(context!.Entries, Is.Zero); Assert.That(context.Bytes, Is.Zero);
        Assert.That(POICanonicalPreparation.Current, Is.Null);
        using var retry = Restore(bytes, default, null); Assert.That(retry.ToCBOR(), Is.EqualTo(bytes));
    }

    private static IEnumerable<TestCaseData> Diagnostics()
    {
        var values = new List<Byte[]>();
        foreach (var profile in new[] { 1, 2, 3, 4 })
        {
            var bytes = Bytes(profile); values.Add(bytes[..^1]); values.Add([..bytes, 0xff]);
        }
        foreach (var hex in new[] { "A2617801617802", "A16178A2616101616102", "A16178A2810101810102", "BF61788101", "A161787F61FFFF", "A3617801617802617963", "A167436F6D6D69747301", "A1617861FF" })
            values.Add(Convert.FromHexString(hex));
        values.Add(Convert.FromHexString("A16178" + String.Concat(Enumerable.Repeat("D800", 65)) + "F6"));
        foreach (var bytes in values)
        foreach (var operation in new[] { "limits", "index", "canonical" })
        foreach (var active in new[] { false, true }) yield return new(bytes, operation, active);
    }

    [TestCaseSource(nameof(Diagnostics))]
    public void Uncancelled_checks_preserve_exact_syntax_limit_and_canonical_errors(Byte[] bytes, String operation, Boolean active)
    {
        using var cancellation = new CancellationTokenSource(); var progress = new POIArchiveReadProgress(active ? cancellation.Token : default);
        void Before()
        {
            if (operation == "limits") ParserCancellationLimitsOracle.CheckCBORArchive(bytes, new());
            else if (operation == "index") ParserCancellationIndexOracle.Read(bytes);
            else ParserCancellationIndexOracle.RequireCanonical(bytes);
        }
        void After()
        {
            if (operation == "limits") Scan(bytes, new(), progress);
            else if (operation == "index") POIArchiveCBORIndex.Read(bytes, progress);
            else POIArchiveCBORIndex.RequireCanonical(bytes, progress);
        }
        Exception? expected = null; try { Before(); } catch (Exception error) { expected = error; }
        if (expected is null) Assert.DoesNotThrow(After);
        else { var actual = Assert.Catch(After)!; Assert.That(actual.GetType(), Is.EqualTo(expected.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message)); }
    }

    [TestCase("limits")] [TestCase("index")] [TestCase("canonical")]
    public void An_already_cancelled_check_preserves_the_original_token(String operation)
    {
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel(); var progress = new POIArchiveReadProgress(cancellation.Token);
        var error = Assert.Catch<OperationCanceledException>(() => {
            if (operation == "limits") Scan(Bytes(1), new(), progress);
            else if (operation == "index") POIArchiveCBORIndex.Read(Bytes(1), progress);
            else POIArchiveCBORIndex.RequireCanonical(Bytes(1), progress);
        });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token));
    }

    [Test]
    public void Independent_and_reentrant_reads_have_no_ambient_token_or_observer()
    {
        var bytes = Bytes(1); using var cancellation = new CancellationTokenSource(); var nested = false;
        using var result = Restore(bytes, cancellation.Token, (stage, position) => {
            if (nested || stage != POIArchiveReadStage.Index) return;
            nested = true; using var unrelated = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit);
            Assert.That(unrelated.ToCBOR(), Is.EqualTo(bytes));
        });
        Assert.That(nested, Is.True); cancellation.Cancel();
        Task.WaitAll(Enumerable.Range(0, 8).Select(reader => Task.Run(() => {
            using var independent = Restore(bytes, default, null); Assert.That(independent.ToCBOR(), Is.EqualTo(bytes));
        })).ToArray());
        Assert.That(POICanonicalPreparation.Current, Is.Null);
    }
}
