/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.ArchiveStreamingTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture, NonParallelizable]
public sealed class ArchiveStreamingTests
{
    [Test, Combinatorial]
    public void Nonseekable_output_preserves_old_codec_bytes_and_borrowed_destination(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean cbor, [Values(1, 7, 65536)] Int32 segmentBytes)
    {
        using var fixture = new Fixture(profile); var history = fixture.History;
        var before = new Observation(history);
        var expected = cbor ? ReferenceCBOR(history) : ReferenceJSON(history);
        Assert.That(Bytes(history, cbor), Is.EqualTo(expected));
        Byte[] prefix = [0xFA, 0xCE];
        using var destination = new Destination(segmentBytes, prefix: prefix);
        Write(history, destination, cbor);
        Assert.That(destination.Bytes, Is.EqualTo(prefix.Concat(expected).ToArray()));
        Assert.That(destination.WriteCalls, Is.GreaterThan(1), "The archive must be emitted in segments.");
        Assert.That(destination.FlushCalls, Is.Zero); Assert.That(destination.DisposeCalls, Is.Zero);
        Assert.That(destination.CanWrite, Is.True);
        before.AssertUnchanged(history);
        using var recovered = cbor ? RoamingNetworkHistory.ParseCBOR(expected, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true) :
            RoamingNetworkHistory.Parse(Encoding.UTF8.GetString(expected), VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        Assert.That(recovered.ToCBOR(), Is.EqualTo(history.ToCBOR()));
        Assert.That(recovered.Head.Id, Is.EqualTo(history.Head.Id));
        Assert.That(recovered.Head.Commit.Signatures, Has.Length.EqualTo(2));
        Assert.That(recovered.Commits.Where(commit => commit.ChangeSet is not null).All(commit => commit.ChangeSet!.Signatures.Length == 2), Is.True);
        Assert.That(recovered.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
    }

    [Test, Combinatorial]
    public void Fixed_archive_references_survive_direct_stream_export(
        [Values("history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history")] String name,
        [Values(false, true)] Boolean cbor)
    {
        var expectedCBOR = File.ReadAllBytes(FilePath(name + ".cbor"));
        using var history = RoamingNetworkHistory.ParseCBOR(expectedCBOR, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        using var destination = new Destination(3);
        Write(history, destination, cbor);
        if (cbor) Assert.That(destination.Bytes, Is.EqualTo(expectedCBOR));
        else Assert.That(CanonicalJSON.Serialize(Value(Encoding.UTF8.GetString(destination.Bytes))),
            Is.EqualTo(File.ReadAllText(FilePath(name + ".json"))));
        Assert.That(destination.FlushCalls, Is.Zero); Assert.That(destination.DisposeCalls, Is.Zero);
    }

    [Test, Combinatorial]
    public void Partial_write_failure_preserves_history_and_releases_the_export_guard(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean cbor, [Values(0, 1, 2)] Int32 position)
    {
        using var fixture = new Fixture(profile); var history = fixture.History;
        var expected = Bytes(history, cbor); var before = new Observation(history);
        var limit = position == 0 ? 0 : position == 1 ? expected.Length / 2 : expected.Length - 1;
        using var broken = new Destination(7, failAfter: limit);
        Assert.That(() => Write(history, broken, cbor), Throws.TypeOf<IOException>());
        Assert.That(broken.Bytes, Is.EqualTo(expected[..limit]));
        Assert.That(broken.FlushCalls, Is.Zero); Assert.That(broken.DisposeCalls, Is.Zero);
        before.AssertUnchanged(history);
        using var retry = new Destination(13); Write(history, retry, cbor);
        Assert.That(retry.Bytes, Is.EqualTo(expected));
        Publish(history, Prepare(history, history.Head.Id, "after-failed-export", Rename("Export retry")));
        Status(history, EvseTarget, "reserved");
        Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
    }

    [Test, Combinatorial]
    public void Stream_callbacks_cannot_reenter_publication_runtime_export_or_disposal(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(false, true)] Boolean cbor)
    {
        using var fixture = new Fixture(profile); var history = fixture.History;
        var before = new Observation(history); var expected = Bytes(history, cbor);
        var candidate = Prepare(history, history.Head.Id, "after-stream", Rename("After stream"));
        using var destination = new Destination();
        var invoked = false;
        destination.OnFirstWrite = () => {
            invoked = true;
            Assert.That(history.TryPublish(history.Head.Id, candidate, out var rejected), Is.False);
            Assert.That(rejected.Outcome, Is.EqualTo(RoamingNetworkHistoryOutcome.Unavailable));
            Assert.That(() => Status(history, EvseTarget, "reserved"), Throws.TypeOf<InvalidOperationException>());
            Assert.That(history.Dispose, Throws.TypeOf<InvalidOperationException>());
            Assert.That(() => history.CreateBootstrap(), Throws.TypeOf<InvalidOperationException>());
            using var nested = new Destination();
            Assert.That(() => Write(history, nested, !cbor), Throws.TypeOf<InvalidOperationException>());
            Assert.That(nested.Bytes, Is.Empty);
            Assert.That(Bytes(history, cbor), Is.EqualTo(expected), "Existing memory exports remain readable in callbacks.");
        };
        Write(history, destination, cbor);
        Assert.That(invoked, Is.True); Assert.That(destination.Bytes, Is.EqualTo(expected));
        before.AssertUnchanged(history);
        Publish(history, candidate); Status(history, EvseTarget, "reserved");
    }

    [Test, Combinatorial]
    public async Task Gated_publication_and_runtime_delivery_wait_for_complete_export(
        [Values(false, true)] Boolean cbor, [Values(false, true)] Boolean runtime)
    {
        using var fixture = new Fixture(2); var history = fixture.History;
        var expected = Bytes(history, cbor);
        var candidate = Prepare(history, history.Head.Id, "concurrent-export", Rename("Concurrent"));
        using var reached = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        using var started = new ManualResetEventSlim(); using var destination = new Destination();
        destination.OnFirstWrite = () => { reached.Set(); Assert.That(release.Wait(TimeSpan.FromSeconds(15)), Is.True); };
        var export = Task.Run(() => Write(history, destination, cbor));
        Task? mutation = null;
        try
        {
            Assert.That(reached.Wait(TimeSpan.FromSeconds(15)), Is.True);
            mutation = Task.Run(() => {
                started.Set();
                if (runtime) Status(history, EvseTarget, "reserved"); else Publish(history, candidate);
            });
            Assert.That(started.Wait(TimeSpan.FromSeconds(15)), Is.True);
            Assert.That(mutation.Wait(TimeSpan.FromMilliseconds(100)), Is.False, "The destination still holds the history gate.");
        }
        finally
        {
            release.Set();
            await export.WaitAsync(TimeSpan.FromSeconds(20));
            if (mutation is not null) await mutation.WaitAsync(TimeSpan.FromSeconds(20));
        }
        Assert.That(destination.Bytes, Is.EqualTo(expected));
        if (runtime) Assert.That(history.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
        else Assert.That(history.Head.Id, Is.EqualTo(candidate.Id));
        Assert.That(destination.DisposeCalls, Is.Zero); Assert.That(destination.FlushCalls, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Invalid_destinations_and_disposed_histories_reject_without_output(Boolean cbor)
    {
        using var history = History(); var before = new Observation(history);
        Assert.That(() => Write(history, null!, cbor), Throws.TypeOf<ArgumentNullException>());
        using var readonlyStream = new Destination(writable: false);
        Assert.That(() => Write(history, readonlyStream, cbor), Throws.ArgumentException);
        Assert.That(readonlyStream.WriteCalls, Is.Zero); before.AssertUnchanged(history);
        history.Dispose(); using var writable = new Destination();
        Assert.That(() => Write(history, writable, cbor), Throws.TypeOf<InvalidOperationException>());
        Assert.That(writable.WriteCalls, Is.Zero);
    }
}
