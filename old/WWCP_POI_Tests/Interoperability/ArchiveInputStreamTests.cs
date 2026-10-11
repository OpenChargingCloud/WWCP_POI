/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ArchiveInputStreamTests
{
    private static readonly String[] Names = ["history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history"];
    private static Byte[] Bytes(Int32 profile = 1) => File.ReadAllBytes(FilePath(Names[profile - 1] + ".cbor"));

    private sealed class Workspace : IDisposable
    {
        internal String Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "wwcp-poi-input-test-" + Guid.NewGuid().ToString("N"));
        internal Workspace() { Directory.CreateDirectory(Path); File.WriteAllText(System.IO.Path.Combine(Path, "unrelated"), "keep"); }
        internal String[] Spools => Directory.GetFiles(Path, "wwcp-poi-input.cbor.tmp-*");
        internal void Clean() { Assert.That(Spools, Is.Empty); Assert.That(File.ReadAllText(System.IO.Path.Combine(Path, "unrelated")), Is.EqualTo("keep")); }
        public void Dispose() => Directory.Delete(Path, true);
    }

    // Both read APIs refuse every seek/length/position/flush operation and the other read mode.
    private sealed class Input(Byte[] bytes, Boolean asynchronous, Int32 segment = 113, Int32 initialOffset = 0,
        Boolean readable = true) : Stream
    {
        internal Int32 Consumed { get; private set; } = initialOffset;
        internal Int32 Reads { get; private set; }
        internal Int32 DisposeCalls { get; private set; }
        internal Boolean Yield { get; set; }
        internal TaskCompletionSource? ReadEntered { get; set; }
        internal TaskCompletionSource? ContinueRead { get; set; }
        internal Action<Int32>? OnRead { get; set; }
        public override Boolean CanRead => readable && DisposeCalls == 0;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => throw new NotSupportedException();
        public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        private Int32 Take(Span<Byte> destination)
        {
            Reads++; OnRead?.Invoke(Reads);
            var count = Math.Min(Math.Min(destination.Length, segment), bytes.Length - Consumed);
            bytes.AsSpan(Consumed, count).CopyTo(destination); Consumed += count; return count;
        }
        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count)
        { if (asynchronous) throw new InvalidOperationException("Synchronous source read."); return Take(buffer.AsSpan(offset, count)); }
        public override async ValueTask<Int32> ReadAsync(Memory<Byte> buffer, CancellationToken token = default)
        {
            if (!asynchronous) throw new InvalidOperationException("Asynchronous source read.");
            if (Yield) await Task.Yield();
            if (Reads == 2 && ReadEntered is not null)
            {
                ReadEntered.TrySetResult();
                await ContinueRead!.Task.WaitAsync(token).ConfigureAwait(false);
            }
            token.ThrowIfCancellationRequested(); return Take(buffer.Span);
        }
        public override void Flush() => throw new NotSupportedException();
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 value) => throw new NotSupportedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
        protected override void Dispose(Boolean disposing) { if (disposing) DisposeCalls++; base.Dispose(disposing); }
    }

    private static Task<RoamingNetworkHistory> Read(Input input, Boolean asynchronous, Workspace work, Int32 threshold = 0,
        RoamingNetworkHistoryLimits? limits = null, CancellationToken token = default,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? commitTrust = null,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? boundary = null)
    {
        var options = new RoamingNetworkArchiveReadOptions(threshold, work.Path);
        return asynchronous
            ? RoamingNetworkHistory.ParseCBORAsync(input, VerifyBatch, commitTrust ?? VerifyCommit, authorizeSnapshotBoundary: boundary ?? (_ => true),
                limits: limits, readOptions: options, cancellationToken: token)
            : Task.FromResult(RoamingNetworkHistory.ParseCBOR(input, VerifyBatch, commitTrust ?? VerifyCommit, authorizeSnapshotBoundary: boundary ?? (_ => true),
                limits: limits, readOptions: options, cancellationToken: token));
    }

    private static IEnumerable<TestCaseData> ValidInputs()
    {
        for (var profile = 1; profile <= 4; profile++)
            foreach (var asynchronous in new[] { false, true })
                foreach (var mode in new[] { 0, 1, 2 })
                    foreach (var segment in new[] { 1, 113 }) yield return new(profile, asynchronous, mode, segment);
    }

    [TestCaseSource(nameof(ValidInputs))]
    public async Task All_profiles_short_reads_and_exact_memory_boundaries_preserve_bytes(Int32 profile, Boolean asynchronous, Int32 mode, Int32 segment)
    {
        using var work = new Workspace(); var bytes = Bytes(profile); using var input = new Input(bytes, asynchronous, segment);
        var threshold = mode == 0 ? 0 : mode == 1 ? bytes.Length : bytes.Length - 1;
        var sawFile = false; var calls = 0;
        using var history = await Read(input, asynchronous, work, threshold, new(maxArchiveBytes: bytes.Length),
            commitTrust: (commit, peer) => { calls++; sawFile |= work.Spools.Length == 1; return VerifyCommit(commit, peer); });
        Assert.That(input.Consumed, Is.EqualTo(bytes.Length)); Assert.That(input.DisposeCalls, Is.Zero);
        Assert.That(calls, Is.GreaterThan(0)); Assert.That(sawFile, Is.EqualTo(mode != 1)); work.Clean();
        Assert.That(history.ToCBOR(), Is.EqualTo(bytes));
        Assert.That(history.Head.Network.EVSEs.First().Status.Value.ToString(), Is.EqualTo("available"));
    }

    [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
    [TestCase(3, false)] [TestCase(3, true)] [TestCase(4, false)] [TestCase(4, true)]
    public async Task Reading_starts_at_the_current_source_position(Int32 profile, Boolean asynchronous)
    {
        using var work = new Workspace(); var bytes = Bytes(profile); var prefixed = new Byte[7].Concat(bytes).ToArray();
        using var input = new Input(prefixed, asynchronous, initialOffset: 7);
        using var history = await Read(input, asynchronous, work); work.Clean();
        Assert.That(history.ToCBOR(), Is.EqualTo(bytes)); Assert.That(input.DisposeCalls, Is.Zero);
    }

    private static IEnumerable<TestCaseData> MalformedInputs()
    {
        for (var profile = 1; profile <= 4; profile++)
            foreach (var asynchronous in new[] { false, true })
                foreach (var kind in new[] { "truncated", "trailing", "duplicate" }) yield return new(profile, asynchronous, kind);
    }

    [TestCaseSource(nameof(MalformedInputs))]
    public async Task Complete_input_failures_precede_trust_and_allow_a_fresh_retry(Int32 profile, Boolean asynchronous, String kind)
    {
        using var work = new Workspace(); var bytes = Bytes(profile); var tree = CBORValue.Parse(bytes);
        var invalid = kind == "truncated" ? bytes[..^1] : kind == "trailing" ? bytes.Append((Byte)0).ToArray() :
            CBORValue.FromMap(tree.AsMap().Append(tree.AsMap()[^1])).ToByteArray();
        using var input = new Input(invalid, asynchronous); var calls = 0;
        var expected = Assert.Catch(() => { using var history = RoamingNetworkHistory.ParseCBOR(invalid, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true); });
        var actual = Assert.CatchAsync(async () => { using var history = await Read(input, asynchronous, work,
            commitTrust: (commit, peer) => { calls++; return VerifyCommit(commit, peer); }); });
        Assert.That(actual!.GetType(), Is.EqualTo(expected!.GetType())); Assert.That(actual.Message, Is.EqualTo(expected.Message));
        Assert.That(calls, Is.Zero); Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
        using var retry = new Input(bytes, asynchronous); using var restored = await Read(retry, asynchronous, work);
        Assert.That(restored.ToCBOR(), Is.EqualTo(bytes)); work.Clean();
    }

    [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
    [TestCase(3, false)] [TestCase(3, true)] [TestCase(4, false)] [TestCase(4, true)]
    public void Byte_limit_reports_only_the_first_excess_byte_without_draining_input(Int32 profile, Boolean asynchronous)
    {
        using var work = new Workspace(); var bytes = Bytes(profile); using var input = new Input(bytes.Concat(new Byte[100]).ToArray(), asynchronous);
        var calls = 0; var maximum = bytes.Length - 1;
        var error = Assert.ThrowsAsync<RoamingNetworkHistoryLimitException>(async () => { using var history = await Read(input, asynchronous, work,
            limits: new(maxArchiveBytes: maximum), commitTrust: (commit, peer) => { calls++; return VerifyCommit(commit, peer); }); });
        Assert.That(error!.Violation, Is.EqualTo(new RoamingNetworkHistoryLimitViolation(RoamingNetworkHistoryLimitKind.ArchiveBytes, maximum, maximum + 1L)));
        Assert.That(input.Consumed, Is.EqualTo(maximum + 1)); Assert.That(calls, Is.Zero);
        Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
    }

    [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
    [TestCase(3, false)] [TestCase(3, true)] [TestCase(4, false)] [TestCase(4, true)]
    public void Inventory_limits_reject_before_trust_after_capture(Int32 profile, Boolean asynchronous)
    {
        using var work = new Workspace(); using var input = new Input(Bytes(profile), asynchronous); var calls = 0;
        var error = Assert.ThrowsAsync<RoamingNetworkHistoryLimitException>(async () => { using var history = await Read(input, asynchronous, work,
            limits: new(maxCommits: 1), commitTrust: (commit, peer) => { calls++; return VerifyCommit(commit, peer); }); });
        Assert.That(error!.Violation.Kind, Is.EqualTo(RoamingNetworkHistoryLimitKind.RetainedCommits));
        Assert.That(calls, Is.Zero); Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
    }

    [TestCase(false)] [TestCase(true)]
    public void Already_cancelled_input_does_not_read_or_create_a_file(Boolean asynchronous)
    {
        using var work = new Workspace(); using var input = new Input(Bytes(), asynchronous); using var cancellation = new CancellationTokenSource();
        cancellation.Cancel(); var error = Assert.ThrowsAsync<OperationCanceledException>(async () => { using var history = await Read(input, asynchronous, work, token: cancellation.Token); });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token)); Assert.That(input.Reads, Is.Zero);
        Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
    }

    [TestCase(false)] [TestCase(true)]
    public void Cancellation_during_capture_deletes_only_owned_files(Boolean asynchronous)
    {
        using var work = new Workspace(); using var cancellation = new CancellationTokenSource(); using var input = new Input(Bytes(), asynchronous) { Yield = asynchronous };
        input.OnRead = count => { if (count == 3) { Assert.That(work.Spools, Has.Length.EqualTo(1)); cancellation.Cancel(); } };
        var error = Assert.ThrowsAsync<OperationCanceledException>(async () => { using var history = await Read(input, asynchronous, work, token: cancellation.Token); });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token)); Assert.That(input.Reads, Is.EqualTo(3));
        Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
    }

    [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
    [TestCase(3, false)] [TestCase(3, true)] [TestCase(4, false)] [TestCase(4, true)]
    public async Task Cancellation_in_a_peer_callback_rejects_and_retry_is_exact(Int32 profile, Boolean asynchronous)
    {
        using var work = new Workspace(); using var cancellation = new CancellationTokenSource(); var bytes = Bytes(profile);
        using var input = new Input(bytes, asynchronous); var calls = 0;
        var error = Assert.ThrowsAsync<OperationCanceledException>(async () => { using var history = await Read(input, asynchronous, work, token: cancellation.Token,
            commitTrust: (commit, peer) => { calls++; cancellation.Cancel(); return VerifyCommit(commit, peer); }); });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token)); Assert.That(calls, Is.EqualTo(1)); work.Clean();
        using var retry = new Input(bytes, asynchronous); using var history = await Read(retry, asynchronous, work);
        Assert.That(history.ToCBOR(), Is.EqualTo(bytes)); work.Clean();
    }

    [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
    [TestCase(3, false)] [TestCase(3, true)] [TestCase(4, false)] [TestCase(4, true)]
    public async Task Read_cancellation_token_does_not_poison_the_returned_history(Int32 profile, Boolean asynchronous)
    {
        using var work = new Workspace(); using var input = new Input(Bytes(profile), asynchronous); using var cancellation = new CancellationTokenSource();
        using var history = await Read(input, asynchronous, work, token: cancellation.Token); cancellation.Cancel();
        var batch = Sign(Batch(history.Head.Snapshot, "after-stream-read", RoamingNetworkChange.UpdateProperty("ChargingStation", "DE*ABC*S1", "name", null, Value("{\"en\":\"After stream\"}"))));
        var commit = Sign(history.PrepareCommit(history.Head.Id, batch));
        Assert.That(history.TryStoreCommit(commit, out var result), Is.True, result.Error); work.Clean();
    }

    [TestCase(false)] [TestCase(true)]
    public void Source_IO_failure_after_spilling_releases_the_file_and_leaves_input_open(Boolean asynchronous)
    {
        using var work = new Workspace(); using var input = new Input(Bytes(), asynchronous);
        input.OnRead = count => { if (count == 3) { Assert.That(work.Spools, Has.Length.EqualTo(1)); throw new IOException("Injected source failure."); } };
        Assert.ThrowsAsync<IOException>(async () => { using var history = await Read(input, asynchronous, work); });
        Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
    }

    [TestCase(false)] [TestCase(true)]
    public async Task Missing_spool_directory_is_needed_only_when_crossing_the_memory_limit(Boolean asynchronous)
    {
        using var work = new Workspace(); using var input = new Input(Bytes(), asynchronous);
        var missing = System.IO.Path.Combine(work.Path, "missing"); var options = new RoamingNetworkArchiveReadOptions(0, missing);
        Assert.ThrowsAsync<DirectoryNotFoundException>(async () => {
            using var history = asynchronous ? await RoamingNetworkHistory.ParseCBORAsync(input, readOptions: options) : RoamingNetworkHistory.ParseCBOR(input, readOptions: options);
        });
        Assert.That(Directory.Exists(missing), Is.False); Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
        using var retry = new Input(Bytes(), asynchronous); options = new(Bytes().Length, missing);
        using var restored = asynchronous ? await RoamingNetworkHistory.ParseCBORAsync(retry, VerifyBatch, VerifyCommit, readOptions: options) : RoamingNetworkHistory.ParseCBOR(retry, VerifyBatch, VerifyCommit, readOptions: options);
        Assert.That(restored.ToCBOR(), Is.EqualTo(Bytes())); Assert.That(Directory.Exists(missing), Is.False);
    }

    [TestCase(false)] [TestCase(true)]
    public void Unreadable_source_is_rejected_before_reading(Boolean asynchronous)
    {
        using var work = new Workspace(); using var input = new Input(Bytes(), asynchronous, readable: false);
        Assert.ThrowsAsync<ArgumentException>(async () => { using var history = await Read(input, asynchronous, work); });
        Assert.That(input.Reads, Is.Zero); Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
    }

    [TestCase(-1)]
    public void Negative_memory_threshold_is_rejected(Int32 maximum) => Assert.Throws<ArgumentOutOfRangeException>(() => new RoamingNetworkArchiveReadOptions(maximum));

    [TestCase(0)] [TestCase(1)] [TestCase(255)] [TestCase(256)] [TestCase(500)] [TestCase(4096)]
    public void Capture_capacity_never_exceeds_the_selected_threshold(Int32 maximum)
    {
        using var work = new Workspace();
        using (var spool = new POIArchiveInputSpool(new(maximum, work.Path), new(), false))
            for (var i = 0; i <= maximum; i++) { spool.Append(new Byte[1]); Assert.That(spool.MemoryCapacity, Is.LessThanOrEqualTo(maximum)); }
        work.Clean();
    }

    [TestCase(0, false)] [TestCase(0, true)]
    [TestCase(1, false)] [TestCase(1, true)]
    [TestCase(2, false)] [TestCase(2, true)]
    public void Owned_capture_file_is_released_at_every_fault_seam(Int32 stage, Boolean asynchronous)
    {
        using var work = new Workspace();
        Assert.ThrowsAsync<IOException>(async () => {
            await using var spool = new POIArchiveInputSpool(new(0, work.Path), new(), asynchronous,
                (current, path) => { if ((Int32) current == stage) throw new IOException("Injected spool failure."); });
            if (asynchronous) await spool.AppendAsync(Bytes(), default); else spool.Append(Bytes());
            using var history = spool.Read(bytes => RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit));
        });
        work.Clean();
    }

    [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
    [TestCase(3, false)] [TestCase(3, true)] [TestCase(4, false)] [TestCase(4, true)]
    public async Task Trust_callbacks_cannot_mutate_the_private_stream_capture(Int32 profile, Boolean asynchronous)
    {
        using var work = new Workspace(); var original = Bytes(profile); var mutable = original.ToArray();
        using var input = new Input(mutable, asynchronous);
        using var history = await Read(input, asynchronous, work, original.Length,
            commitTrust: (commit, peer) => { Array.Fill(mutable, (Byte)0xff); return VerifyCommit(commit, peer); });
        Assert.That(mutable, Is.Not.EqualTo(original)); Assert.That(history.ToCBOR(), Is.EqualTo(original)); work.Clean();
    }

    [TestCase(1, false)] [TestCase(1, true)] [TestCase(2, false)] [TestCase(2, true)]
    [TestCase(3, false)] [TestCase(3, true)] [TestCase(4, false)] [TestCase(4, true)]
    public void Missing_signature_verifiers_are_not_replaced_by_cancellation_wrappers(Int32 profile, Boolean asynchronous)
    {
        using var work = new Workspace(); using var input = new Input(Bytes(profile), asynchronous);
        Assert.CatchAsync<ArgumentException>(async () => {
            using var history = asynchronous
                ? await RoamingNetworkHistory.ParseCBORAsync(input, authorizeSnapshotBoundary: _ => true, readOptions: new(0, work.Path))
                : RoamingNetworkHistory.ParseCBOR(input, authorizeSnapshotBoundary: _ => true, readOptions: new(0, work.Path));
        });
        Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
    }

    [TestCase(3, false)] [TestCase(3, true)] [TestCase(4, false)] [TestCase(4, true)]
    public void Boundary_authority_remains_required(Int32 profile, Boolean asynchronous)
    {
        using var work = new Workspace(); using var input = new Input(Bytes(profile), asynchronous); var calls = 0;
        Boolean Verify(RoamingNetworkCommit commit, RoamingNetworkChangeSetSignature peer) { calls++; return VerifyCommit(commit, peer); }
        Assert.CatchAsync<ArgumentException>(async () => {
            using var history = asynchronous
                ? await RoamingNetworkHistory.ParseCBORAsync(input, VerifyBatch, Verify, readOptions: new(0, work.Path))
                : RoamingNetworkHistory.ParseCBOR(input, VerifyBatch, Verify, readOptions: new(0, work.Path));
        });
        Assert.That(calls, Is.Zero); work.Clean();
    }

    [Test]
    public async Task Pending_asynchronous_source_read_observes_cancellation_and_cleans_the_spool()
    {
        using var work = new Workspace(); using var cancellation = new CancellationTokenSource();
        using var input = new Input(Bytes(), true) {
            ReadEntered = new(TaskCreationOptions.RunContinuationsAsynchronously),
            ContinueRead = new(TaskCreationOptions.RunContinuationsAsynchronously)
        };
        var pending = Read(input, true, work, token: cancellation.Token);
        await input.ReadEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.That(work.Spools, Has.Length.EqualTo(1)); cancellation.Cancel();
        var error = Assert.CatchAsync<OperationCanceledException>(async () => { using var history = await pending; });
        Assert.That(error!.CancellationToken, Is.EqualTo(cancellation.Token)); Assert.That(input.DisposeCalls, Is.Zero); work.Clean();
    }

    [TestCase(false)] [TestCase(true)]
    public void Private_spool_also_enforces_its_byte_budget(Boolean asynchronous)
    {
        using var work = new Workspace();
        Assert.ThrowsAsync<RoamingNetworkHistoryLimitException>(async () => {
            await using var spool = new POIArchiveInputSpool(new(0, work.Path), new(maxArchiveBytes: 2), asynchronous);
            if (asynchronous) { await spool.AppendAsync(new Byte[2], default); await spool.AppendAsync(new Byte[1], default); }
            else { spool.Append(new Byte[2]); spool.Append(new Byte[1]); }
        });
        work.Clean();
    }

    [Test]
    public void Concurrent_input_captures_exclude_maintenance_and_release_both_leases()
    {
        using var work = new Workspace(); var options = new RoamingNetworkArchiveReadOptions(0, work.Path);
        using (var first = new POIArchiveInputSpool(options, new(), false))
        using (var second = new POIArchiveInputSpool(options, new(), false))
        {
            first.Append(new Byte[1]); second.Append(new Byte[1]); Assert.That(work.Spools, Has.Length.EqualTo(2));
            Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(options.TemporaryArchivePath, out _, out var result), Is.False);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkArchiveCleanupOutcome.PersistenceFailure));
        }
        work.Clean();
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(options.TemporaryArchivePath, out var plan, out var inspection), Is.True, inspection.Error);
        Assert.That(plan!.TemporaryFiles, Is.Empty); Assert.That(File.Exists(options.TemporaryArchivePath + ".lock"), Is.True);
    }

    [Test]
    public void Existing_spool_orphans_need_explicit_review_and_are_not_removed_by_recovery()
    {
        using var work = new Workspace(); var options = new RoamingNetworkArchiveReadOptions(0, work.Path);
        var orphan = options.TemporaryArchivePath + ".tmp-" + Guid.NewGuid().ToString("N"); File.WriteAllBytes(orphan, [1, 2, 3]);
        using (var input = new Input(Bytes(), false))
        using (var history = RoamingNetworkHistory.ParseCBOR(input, VerifyBatch, VerifyCommit, readOptions: options))
            Assert.That(history.ToCBOR(), Is.EqualTo(Bytes()));
        Assert.That(File.ReadAllBytes(orphan), Is.EqualTo(new Byte[] { 1, 2, 3 }));
        Assert.That(RoamingNetworkArchiveMaintenance.TryInspectArchive(options.TemporaryArchivePath, out var plan, out var inspected), Is.True, inspected.Error);
        Assert.That(plan!.TemporaryFiles, Has.Length.EqualTo(1));
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var preview), Is.True, preview.Error); Assert.That(File.Exists(orphan), Is.True);
        Assert.That(RoamingNetworkArchiveMaintenance.TryExecute(plan, out var cleaned, cleanup: true), Is.True, cleaned.Error); work.Clean();
    }
}
