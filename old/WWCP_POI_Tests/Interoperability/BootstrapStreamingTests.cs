/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Security.Cryptography;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;
using static WWCP_POI_Tests.Interoperability.ArchiveStreamingTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class BootstrapStreamingTests
{
    [Test, Combinatorial]
    public void Frozen_full_and_tail_fragments_match_archive_digests_and_survive_sender_changes(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean exactMultiple)
    {
        using var fixture = new Fixture(profile); var history = fixture.History;
        var expected = ReferenceCBOR(history);
        var chunkBytes = exactMultiple ? expected.Length / 2 : 257;
        // The exact-multiple case uses the full archive when its length is odd.
        if (exactMultiple && expected.Length % 2 != 0) chunkBytes = expected.Length;
        if (!exactMultiple)
            while (expected.Length % chunkBytes == 0) chunkBytes++;
        var source = history.CreateBootstrap(chunkBytes);
        var manifest = source.Manifest;
        Assert.That(manifest.ArchiveETag, Is.EqualTo(ETag.Compute(ETagFormat.CBOR, expected)));
        Assert.That(manifest.ArchiveBytes, Is.EqualTo(expected.Length));
        var assembled = new List<Byte>();
        for (var index = 0; index < manifest.ChunkCount; index++)
        {
            var chunk = source.CreateChunk(index);
            Assert.That(chunk.Data.Length, Is.EqualTo(manifest.Length(index)));
            Assert.That(chunk.Data.ToArray(), Is.EqualTo(expected.AsSpan(index * chunkBytes, manifest.Length(index)).ToArray()));
            Assert.That(SHA256.HashData(chunk.Data.AsSpan()), Is.EqualTo(manifest.ChunkDigests[index].ToArray()));
            assembled.AddRange(chunk.Data);
            var detached = chunk.Data.ToArray(); detached[0] ^= 0xFF;
            Assert.That(source.CreateChunk(index).Data, Is.EqualTo(chunk.Data));
        }
        Assert.That(assembled.ToArray(), Is.EqualTo(expected));
        Assert.That(manifest.Length(manifest.ChunkCount - 1), exactMultiple ? Is.EqualTo(chunkBytes) : Is.LessThan(chunkBytes));
        Assert.That(() => source.CreateChunk(-1), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => source.CreateChunk(manifest.ChunkCount), Throws.TypeOf<ArgumentOutOfRangeException>());
        Publish(history, Prepare(history, history.Head.Id, "after-freeze", Rename("After freeze")));
        if (profile == 4)
        {
            var snapshot = Snapshot(history, 23); Publish(history, snapshot);
            Prune(history, Plan(history, snapshot), Path.Combine(fixture.Directory.DirectoryPath, "cold-3.cbor"));
            Assert.That(history.RetentionReceipts, Has.Length.EqualTo(3));
        }
        history.Dispose();
        using var destination = new Destination(5); source.WriteArchive(destination);
        Assert.That(destination.Bytes, Is.EqualTo(expected));
        Assert.That(source.Manifest, Is.SameAs(manifest));
        Assert.That(destination.FlushCalls, Is.Zero); Assert.That(destination.DisposeCalls, Is.Zero);
        using var staging = new ArchiveDirectory();
        using var receiver = RoamingNetworkBootstrapReceiver.Create(staging.DirectoryPath, manifest);
        SnapshotTestSupport.Finish(receiver, source, cbor: true);
        Assert.That(receiver.TryActivate(manifest.Id, out var activated, out var result, activate: true,
            verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: _ => true), Is.True, result.Error);
        using var recovered = activated!;
        Assert.That(recovered.ToCBOR(), Is.EqualTo(expected));
        Assert.That(recovered.Head.Id, Is.EqualTo(manifest.Head));
    }

    [Test, Combinatorial]
    public void Frozen_output_failures_and_invalid_destinations_do_not_damage_the_session(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(0, 1, 2)] Int32 position)
    {
        using var fixture = new Fixture(profile); var expected = fixture.History.ToCBOR();
        var source = fixture.History.CreateBootstrap(257); var manifest = source.Manifest;
        var limit = position == 0 ? 0 : position == 1 ? 258 : expected.Length - 1;
        using var broken = new Destination(3, failAfter: limit);
        Assert.That(() => source.WriteArchive(broken), Throws.TypeOf<IOException>());
        Assert.That(broken.Bytes, Is.EqualTo(expected[..limit]));
        Assert.That(broken.DisposeCalls, Is.Zero); Assert.That(broken.FlushCalls, Is.Zero);
        Assert.That(() => source.WriteArchive(null!), Throws.TypeOf<ArgumentNullException>());
        using var readonlyStream = new Destination(writable: false);
        Assert.That(() => source.WriteArchive(readonlyStream), Throws.ArgumentException);
        Assert.That(readonlyStream.WriteCalls, Is.Zero);
        using var retry = new Destination(7); source.WriteArchive(retry);
        Assert.That(retry.Bytes, Is.EqualTo(expected)); Assert.That(source.Manifest, Is.SameAs(manifest));
    }

    [Test, Combinatorial]
    public void Source_limits_accept_exact_budgets_and_reject_one_less_without_changing_history(
        [Values(1, 2, 3, 4)] Int32 profile, [Values("bytes", "chunks", "wire", "manifest", "commits")] String kind)
    {
        using var fixture = new Fixture(profile); var history = fixture.History;
        var before = new Observation(history); var source = history.CreateBootstrap(257); var manifest = source.Manifest;
        var wireBytes = Enumerable.Range(0, manifest.ChunkCount).Max(index => Size(source.CreateChunk(index)));
        var manifestBytes = Math.Max(System.Text.Encoding.UTF8.GetByteCount(manifest.ToJSON()), manifest.ToCBOR().Length);
        RoamingNetworkBootstrapLimits Limits(Boolean excessive) => new(
            maxArchiveBytes: manifest.ArchiveBytes - (excessive && kind == "bytes" ? 1 : 0),
            maxChunkBytes: 257, maxChunks: manifest.ChunkCount - (excessive && kind == "chunks" ? 1 : 0),
            maxCommits: manifest.CommitCount - (excessive && kind == "commits" ? 1 : 0),
            maxWireBytes: wireBytes - (excessive && kind == "wire" ? 1 : 0),
            maxManifestBytes: manifestBytes - (excessive && kind == "manifest" ? 1 : 0));
        var exact = history.CreateBootstrap(257, Limits(false));
        Assert.That(exact.Manifest.Id, Is.EqualTo(manifest.Id));
        Assert.That(() => history.CreateBootstrap(257, Limits(true)), Throws.InstanceOf<ArgumentException>());
        before.AssertUnchanged(history);
    }

    private static Int32 Size(RoamingNetworkBootstrapChunk chunk)
        => Math.Max(System.Text.Encoding.UTF8.GetByteCount(chunk.ToJSON()), chunk.ToCBOR().Length);

    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    public void Frozen_stream_callbacks_can_advance_the_sender_and_repeat_original_output(Int32 profile)
    {
        using var fixture = new Fixture(profile); var history = fixture.History;
        var source = history.CreateBootstrap(257); var expected = history.ToCBOR();
        using var destination = new Destination();
        destination.OnFirstWrite = () => {
            Publish(history, Prepare(history, history.Head.Id, "inside-frozen-output", Rename("Sender advanced")));
            Status(history, EvseTarget, "reserved");
            using var repeated = new Destination(7); source.WriteArchive(repeated);
            Assert.That(repeated.Bytes, Is.EqualTo(expected));
        };
        source.WriteArchive(destination);
        Assert.That(destination.Bytes, Is.EqualTo(expected));
        Assert.That(history.Head.Id, Is.Not.EqualTo(source.Manifest.Head));
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
        Assert.That(destination.DisposeCalls, Is.Zero); Assert.That(destination.FlushCalls, Is.Zero);
    }

    [Test, Combinatorial]
    public void Fragment_capture_handles_writes_crossing_full_tail_and_total_capacity_boundaries(
        [Values(1, 4, 100)] Int32 writeBytes, [Values(15, 16, 20)] Int32 length)
    {
        var expected = Enumerable.Range(0, length).Select(index => (Byte)index).ToArray();
        using var capture = new POIBootstrapArchiveCapture(5, length, (length + 4) / 5);
        for (var offset = 0; offset < expected.Length; offset += writeBytes)
            capture.Write(expected.AsSpan(offset, Math.Min(writeBytes, expected.Length - offset)));
        Assert.That(() => capture.Write([99]), Throws.ArgumentException);
        var frozen = capture.Complete();
        Assert.That(frozen.Length, Is.EqualTo(length));
        Assert.That(frozen.Digest, Is.EqualTo(ETag.Compute(ETagFormat.CBOR, expected)));
        Assert.That(frozen.Chunks.SelectMany(chunk => chunk).ToArray(), Is.EqualTo(expected));
        for (var index = 0; index < frozen.Chunks.Length; index++)
            Assert.That(SHA256.HashData(frozen.Chunks[index]), Is.EqualTo(frozen.ChunkDigests[index].ToArray()));
        Assert.That(frozen.Chunks[^1].Length, Is.EqualTo((length - 1) % 5 + 1));
        Assert.That(() => capture.Write([0]), Throws.TypeOf<InvalidOperationException>());
        Assert.That(() => capture.Complete(), Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Rejected_capture_write_is_atomic_and_can_continue_within_chunk_count_capacity()
    {
        using var capture = new POIBootstrapArchiveCapture(5, 100, 2);
        capture.Write([1, 2, 3, 4, 5, 6]);
        Assert.That(() => capture.Write([7, 8, 9, 10, 11]), Throws.ArgumentException);
        Assert.That(capture.Length, Is.EqualTo(6));
        capture.Write([7, 8, 9, 10]);
        var frozen = capture.Complete();
        Byte[] expected = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        Assert.That(frozen.Chunks.SelectMany(chunk => chunk), Is.EqualTo(expected));
        Assert.That(frozen.Digest, Is.EqualTo(ETag.Compute(ETagFormat.CBOR, expected)));
    }
}
