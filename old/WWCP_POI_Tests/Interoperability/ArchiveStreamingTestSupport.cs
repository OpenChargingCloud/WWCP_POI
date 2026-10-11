/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

internal static class ArchiveStreamingTestSupport
{
    // Each destination deliberately refuses all read/seek/length/position operations and Flush.
    // Write still accepts every byte or throws; splitting simulates different sink boundaries.
    internal sealed class Destination(Int32 segmentBytes = Int32.MaxValue, Int32? failAfter = null,
                                      Boolean writable = true, Byte[]? prefix = null) : Stream
    {
        private readonly MemoryStream bytes = new(prefix?.Length ?? 0);
        private Boolean closed;
        private Boolean initialized;
        internal Action? OnFirstWrite { get; set; }
        internal Int32 WriteCalls { get; private set; }
        internal Int32 FlushCalls { get; private set; }
        internal Int32 DisposeCalls { get; private set; }
        internal Byte[] Bytes => bytes.ToArray();

        public override void Write(ReadOnlySpan<Byte> buffer)
        {
            if (!CanWrite) throw new InvalidOperationException("Unwritable test destination.");
            if (!initialized)
            {
                initialized = true;
                if (prefix is not null) bytes.Write(prefix);
                OnFirstWrite?.Invoke();
            }
            WriteCalls++;
            var accepted = failAfter is { } maximum ? Math.Min(buffer.Length, maximum - checked((Int32)bytes.Length)) : buffer.Length;
            for (var offset = 0; offset < accepted;)
            {
                var count = Math.Min(segmentBytes, accepted - offset);
                bytes.Write(buffer.Slice(offset, count)); offset += count;
            }
            if (accepted != buffer.Length) throw new IOException("Injected partial destination failure.");
        }

        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => Write(buffer.AsSpan(offset, count));
        public override Boolean CanWrite => writable && !closed;
        public override Boolean CanRead => false;
        public override Boolean CanSeek => false;
        public override Int64 Length => throw new NotSupportedException();
        public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override void Flush() { FlushCalls++; throw new InvalidOperationException("Caller owns destination flushing."); }
        public override Int32 Read(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 value) => throw new NotSupportedException();
        protected override void Dispose(Boolean disposing)
        {
            if (disposing) { DisposeCalls++; closed = true; bytes.Dispose(); }
            base.Dispose(disposing);
        }
    }

    internal sealed class Fixture : IDisposable
    {
        internal ArchiveDirectory Directory { get; } = new();
        internal RoamingNetworkHistory History { get; private set; }
        internal String ArchivePath => Directory.ArchivePath;

        internal Fixture(Int32 profile, Boolean persistent = false)
        {
            History = InteropFixture.History();
            try
            {
                Store(History, Sign(History.Head.Commit));
                Branches("before");
                if (profile >= 2)
                {
                    var snapshot = Snapshot(History); Publish(History, snapshot);
                    Branches("after");
                    if (profile == 3)
                    {
                        var full = History;
                        var boundary = RoamingNetworkHistory.FromSnapshot(full.CheckpointId, snapshot, AcceptChain(full), VerifyCommit, VerifyBatch);
                        try
                        {
                            foreach (var commit in full.Commits.Where(commit => commit.Revision > snapshot.Revision)) Store(boundary, commit);
                            Assert.That(boundary.TryAdoptHead(snapshot.Id, full.Head.Id, out var adopted, adopt: true), Is.True, adopted.Error);
                        }
                        catch { boundary.Dispose(); throw; }
                        History = boundary; full.Dispose();
                    }
                    if (profile == 4)
                    {
                        Prune(History, Plan(History, snapshot), Path.Combine(Directory.DirectoryPath, "cold-1.cbor"));
                        // Keep a large retained value after pruning the earlier padded batches.
                        var next = Sign(History.PrepareSnapshot(History.Head.Id, InteropFixture.Time.AddDays(11),
                            ImmutableDictionary<String, String>.Empty.Add("en", new String('s', 24000))
                                .Add("de", new String('t', 24000)).Add("fr", new String('u', 24000))));
                        Publish(History, next);
                        Publish(History, Prepare(History, next.Id, "tail", Rename("Pruned tail")));
                        Prune(History, Plan(History, next), Path.Combine(Directory.DirectoryPath, "cold-2.cbor"));
                        Assert.That(History.RetentionReceipts, Has.Length.EqualTo(2));
                    }
                }
                Assert.That(Value(History.ToJSON()).GetProperty("Profile").GetString(), Is.EqualTo("wwcp-poi-history-v" + profile));
                if (persistent)
                {
                    File.WriteAllBytes(ArchivePath, History.ToCBOR());
                    History.Dispose();
                    History = RoamingNetworkHistory.Open(ArchivePath, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
                }
                Status(History, EvseTarget, "charging"); Status(History, PoolMeter, "error");
            }
            catch { History.Dispose(); Directory.Dispose(); throw; }
        }

        private void Branches(String name)
        {
            var root = History.Head.Id;
            var batch = Sign(Batch(History.GetSnapshot(root), name + "-left", Power(name == "before" ? "150 kW" : "200 kW"))
                .WithDescription(ImmutableDictionary<String, String>.Empty.Add("en", new String('x', 24000) + " snowman ☃\n").Add("de", "Änderung"))
                .WithMetadata(ImmutableDictionary<String, JsonElement>.Empty.Add("wire", Value("{\"decimal\":1.0,\"exponent\":1e0,\"negativeZero\":-0,\"null\":null,\"reading\":\"250 kW\"}"))));
            var left = Sign(History.PrepareCommit(root, batch)); Publish(History, left);
            var right = Prepare(History, root, name + "-right", Rename(name + " station ☃")); Store(History, right);
            Publish(History, Merge(History, left, right, name + "-merge"));
        }

        public void Dispose() { History.Dispose(); Directory.Dispose(); }
    }

    internal static void Write(RoamingNetworkHistory history, Stream destination, Boolean cbor)
    {
        if (cbor) history.WriteCBOR(destination); else history.WriteJSON(destination);
    }

    internal static Byte[] Bytes(RoamingNetworkHistory history, Boolean cbor)
        => cbor ? history.ToCBOR() : Encoding.UTF8.GetBytes(history.ToJSON());

    // Independent outer-envelope reference: retain the old whole-map codec and unchanged
    // standalone commit/receipt/snapshot codecs instead of using the new archive adapter.
    internal static Byte[] ReferenceCBOR(RoamingNetworkHistory history)
    {
        var commits = history.Commits;
        var fields = new List<(String Key, CBORValue Value)> {
            ("Profile", CBORValue.FromText(Value(history.ToJSON()).GetProperty("Profile").GetString()!)),
            ("ContentProfile", CBORValue.FromText(POIContentProfile.Id)),
            ("Commits", CBORValue.FromArray(commits.Where(commit => commit.Id != history.AnchorId).Select(commit => CBORValue.Parse(commit.ToCBOR())))),
            ("Head", history.Head.Id.ToCBOR())
        };
        if (history.HasCompleteAncestry)
        {
            fields.Add(("Checkpoint", CBORValue.Parse(history.GetSnapshot(history.CheckpointId).ToCBOR(IncludeVersionMetadata: true))));
            fields.Add(("CheckpointCommit", CBORValue.Parse(commits.Single(commit => commit.Id == history.CheckpointId).ToCBOR())));
        }
        else
        {
            fields.Add(("CheckpointId", history.CheckpointId.ToCBOR()));
            fields.Add(("SnapshotCommit", CBORValue.Parse(commits.Single(commit => commit.Id == history.AnchorId).ToCBOR())));
            if (!history.RetentionReceipts.IsEmpty)
                fields.Add(("RetentionReceipts", CBORValue.FromArray(history.RetentionReceipts.Select(receipt => CBORValue.Parse(receipt.ToCBOR())))));
        }
        return RoamingNetworkCommit.Map(fields.ToArray()).ToByteArray(CBORWriterOptions.Canonical);
    }

    internal static Byte[] ReferenceJSON(RoamingNetworkHistory history)
    {
        using var bytes = new MemoryStream();
        using (var writer = new Utf8JsonWriter(bytes))
        {
            writer.WriteStartObject();
            writer.WriteString("Profile", Value(history.ToJSON()).GetProperty("Profile").GetString());
            writer.WriteString("ContentProfile", POIContentProfile.Id);
            if (history.HasCompleteAncestry)
            {
                writer.WritePropertyName("Checkpoint"); history.GetSnapshot(history.CheckpointId).WriteTo(writer);
                writer.WritePropertyName("CheckpointCommit"); history.Commits.Single(commit => commit.Id == history.CheckpointId).WriteTo(writer);
            }
            else
            {
                writer.WritePropertyName("CheckpointId"); history.CheckpointId.Hash.WriteTo(writer);
                writer.WritePropertyName("SnapshotCommit"); history.Commits.Single(commit => commit.Id == history.AnchorId).WriteTo(writer);
            }
            writer.WritePropertyName("Commits"); writer.WriteStartArray();
            foreach (var commit in history.Commits.Where(commit => commit.Id != history.AnchorId)) commit.WriteTo(writer);
            writer.WriteEndArray(); writer.WritePropertyName("Head"); history.Head.Id.Hash.WriteTo(writer);
            if (!history.RetentionReceipts.IsEmpty)
            {
                writer.WritePropertyName("RetentionReceipts"); writer.WriteStartArray();
                foreach (var receipt in history.RetentionReceipts) receipt.WriteTo(writer);
                writer.WriteEndArray();
            }
            writer.WriteEndObject();
        }
        return bytes.ToArray();
    }
}
