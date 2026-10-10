/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Globalization;
using System.Text;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Newtonsoft.Json.Linq;
using Org.BouncyCastle.Crypto.Parameters;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Benchmarks;

internal sealed record Shape(String Name, Int32 EVSEs, Int32 Changes, Int32 Branches,
    Int32 SnapshotEvery, Int32 Retentions, Int32 Locations);

internal sealed class Workload : IDisposable
{
    private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-01-01T00:00:00Z", CultureInfo.InvariantCulture);
    // Public, reproducible benchmark seed. This key establishes no production authority.
    private static readonly Ed25519PrivateKeyParameters Key = new(Convert.FromHexString(
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);
    private static readonly Ed25519PublicKeyParameters PublicKey = Key.GeneratePublicKey();
    private readonly String directory = Path.Combine(Path.GetTempPath(), "wwcp-poi-benchmark-" + Guid.NewGuid().ToString("N"));
    private readonly List<String> ownedDirectories = [];
    private readonly Shape shape;
    private Int32 ordinal;
    private RoamingNetworkChangeSet? batch;
    private RoamingNetworkRetentionReceipt? receipt;
    private RoamingNetworkColdArchiveCatalog? coldCatalog;
    private String? json;
    private Byte[]? cbor;
    private RoamingNetworkBootstrapSource? bootstrap;
    private RoamingNetworkHistory? persistent;
    private EncodingProbe? encoding;
    private ChildETagProbe? childETags;
    internal ChildETagRetention? ChildETagRetention => childETags?.Retention;
    internal ChildETagState? ChildETagState => childETags?.State;

    internal RoamingNetworkHistory History { get; } = null!;
    internal RoamingNetworkDataSnapshot Snapshot => History.Head.Snapshot;
    internal Int32 EntityCount => Snapshot.Entities.Count;
    internal Int32 CommitCount => History.Commits.Length;
    internal Int32 ReceiptCount => History.RetentionReceipts.Length;
    internal Int32 CatalogIdCount => History.RetentionReceipts.Sum(value => value.PrunedCommits.Length + value.ArchiveOnlyTips.Length);
    internal String StaticIdentity => String.Join("|", Snapshot.ETags);
    internal String ArchiveIdentity => ETag.Compute(ETagFormat.CBOR, cbor ??= History.ToCBOR()).ToString();

    internal static Boolean VerifyBatch(RoamingNetworkChangeSet value, RoamingNetworkChangeSetSignature peer)
        => value.VerifySignature(peer, PublicKey, "benchmark", out _);

    internal static Boolean VerifyCommit(RoamingNetworkCommit value, RoamingNetworkChangeSetSignature peer)
        => value.VerifySignature(peer, PublicKey, "benchmark", out _);

    private Boolean AuthorizeBoundary(RoamingNetworkSnapshotBoundary boundary) => boundary.Checkpoint == History.CheckpointId;

    internal Workload(Shape shape, String operation)
    {
        this.shape = shape;
        Directory.CreateDirectory(directory); ownedDirectories.Add(directory);
        try
        {
            History = new(Network(shape.EVSEs), VerifyBatch, VerifyCommit);
            if (operation == "catalog-create") return;
            Require(History.TryStoreCommit(Sign(History.Head.Commit), out var stored), stored.Error);
            for (var round = 0; round < shape.Retentions; round++)
            {
                for (var step = 0; step < 4; step++) PublishChange();
                var anchor = PublishSnapshot();
                PublishChange();
                Require(History.TryPlanRetention(anchor.Id, At(), out var plan, out var review, cutoffCommit: anchor.Id), review.Error);
                var path = Path.Combine(directory, $"cold-{round}.cbor");
                Require(History.TryExecuteRetention(plan!, path, AuthorizeBoundary, out var pruned, prune: true), pruned.Error);
                receipt = pruned.Receipt;
                coldCatalog = new([new(receipt!.SourceArchiveETag, path)]);
            }
            for (var step = 1; step <= shape.Changes; step++)
            {
                PublishChange();
                if (shape.SnapshotEvery > 0 && step % shape.SnapshotEvery == 0) PublishSnapshot();
            }
            var parent = History.Head.Id;
            for (var branch = 0; branch < shape.Branches; branch++)
            {
                var envelope = Prepare(parent);
                Require(History.TryStoreCommit(envelope, out stored), stored.Error);
            }
            var network = History.Head.Network;
            // Materialize and deliver nondefault local status before every runtime benchmark.
            Require(network.EVSEs.Count() == shape.EVSEs, "Unexpected workload hierarchy size.");
            History.ApplyRuntimeUpdate(new("benchmark-network", POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
                POIRuntimeStatusKind.Status, new("charging", Time.AddDays(30)), mode: POIRuntimeUpdateMode.ReplaceHistory));
            batch = Snapshot.CreateChangeSet("measured-update", Time.AddDays(31), [Change("230 kW")]);
            if (ChildETagProbe.Operations.Contains(operation)) childETags = new(operation, Snapshot);
            if (EncodingProbe.Operations.Contains(operation))
                encoding = new(operation, Snapshot, batch.Sign(Key, "benchmark", COSEAlgorithm.Ed25519));
            if (operation is "replay-json") json = History.ToJSON();
            if (operation is "replay-cbor" or "persist-rewrite" or "bootstrap-transfer") cbor = History.ToCBOR();
            if (operation is "persist-rewrite")
            {
                var path = Path.Combine(directory, "active.cbor"); File.WriteAllBytes(path, cbor!);
                persistent = RoamingNetworkHistory.Open(path, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AuthorizeBoundary);
            }
            if (operation is "bootstrap-transfer") bootstrap = History.CreateBootstrap();
            if (operation is "cold-read" && receipt is null) throw new ArgumentException("cold-read requires at least one retention round.");
        }
        catch { Dispose(); throw; }
    }

    internal void PrepareSample() => childETags?.Prepare();
    internal void VerifyOutput() { encoding?.VerifyOutput(); childETags?.Verify(); }

    internal Func<OperationResult> Operation(String operation) => (childETags is null ? null : (Func<OperationResult>) childETags.Run) ?? encoding?.Action ?? (operation switch {
        "hash-json" => () => { var bytes = Snapshot.ToCanonicalJSON(); return new(bytes.Length, 0, ETag.Compute(ETagFormat.JSON, bytes).ToString()); },
        "hash-cbor" => () => { var bytes = Snapshot.ToCanonicalCBOR(); return new(bytes.Length, 0, ETag.Compute(ETagFormat.CBOR, bytes).ToString()); },
        "etag-recompute" => () => new(0, 0, String.Join("|", POIRepresentation.GetETags(Snapshot))),
        "static-update" => () => { var state = Snapshot.ApplyChangeSet(batch!); return new(0, 0, String.Join("|", state.ETags)); },
        "runtime-capture" => () => { var version = History.Head.Network.ApplyChangeSet(batch!); return new(0, 0, String.Join("|", version.ETags)); },
        "runtime-materialize" => () => {
            var version = History.Head.Network.ApplyChangeSet(batch!);
            Require(version.EVSEs.Count() == shape.EVSEs && version.EVSEs.Single(value => value.Id.ToString() == "DE*ABC*E1").Status.Value.ToString() == "charging", "Runtime was not preserved.");
            Require(History.Head.Network.EVSEs.Single(value => value.Id.ToString() == "DE*ABC*E1").Status.Value.ToString() == "charging", "Source runtime changed.");
            return new(0, 0, String.Join("|", version.ETags)); },
        "archive-json" => () => { var text = History.ToJSON(); return new(Encoding.UTF8.GetByteCount(text), 0, ETag.Compute(ETagFormat.JSON, Encoding.UTF8.GetBytes(text)).ToString()); },
        "archive-cbor" => () => { var bytes = History.ToCBOR(); return new(bytes.Length, 0, ETag.Compute(ETagFormat.CBOR, bytes).ToString()); },
        "archive-json-stream" => () => { using var stream = new DigestStream(); History.WriteJSON(stream); return stream.Complete(ETagFormat.JSON); },
        "archive-cbor-stream" => () => { using var stream = new DigestStream(); History.WriteCBOR(stream); return stream.Complete(ETagFormat.CBOR); },
        "replay-json" => () => { using var restored = RoamingNetworkHistory.Parse(json!, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AuthorizeBoundary); Check(restored); return new(Encoding.UTF8.GetByteCount(json!), 0, restored.Head.Id.ToString()); },
        "replay-cbor" => () => { using var restored = RoamingNetworkHistory.ParseCBOR(cbor!, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AuthorizeBoundary); Check(restored); return new(cbor!.Length, 0, restored.Head.Id.ToString()); },
        "snapshot-prepare" => () => { var value = History.PrepareSnapshot(History.Head.Id, Time.AddDays(32)); return new(0, 0, value.Id.ToString()); },
        "persist-rewrite" => () => {
            // Exact duplicate publication still exercises signature checks and a full durable archive rewrite.
            var value = persistent!.Head.Commit;
            Require(persistent.TryPublish(value.Parents[0], value, out var result), result.Error);
            return new(0, new FileInfo(Path.Combine(directory, "active.cbor")).Length, persistent.Head.Id.ToString()); },
        "bootstrap-export" => () => {
            var source = History.CreateBootstrap(); Int64 bytes = source.Manifest.ToCBOR().Length;
            for (var i = 0; i < source.Manifest.ChunkCount; i++) bytes += source.CreateChunk(i).ToCBOR().Length;
            return new(bytes, 0, source.Manifest.Id.ToString()); },
        "bootstrap-transfer" => () => BootstrapTransfer(),
        "cold-read" => () => {
            Require(RoamingNetworkHistory.TryReadColdArchive(receipt!, coldCatalog!, out var restored, out var result,
                VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AuthorizeBoundary), result.Error);
            using (restored!) { Require(restored!.Head.Id == receipt!.Head, "Wrong cold head."); return new(new FileInfo(result.ResolvedPath!).Length, 0, restored.Head.Id.ToString()); } },
        "catalog-create" => () => {
            var digest = ETag.Compute(ETagFormat.CBOR, [1]);
            var entries = Enumerable.Range(0, shape.Locations).Select(i => new RoamingNetworkColdArchiveLocation(digest, Path.Combine(directory, $"location-{i:D8}.cbor")));
            var catalog = new RoamingNetworkColdArchiveCatalog(entries, shape.Locations);
            var candidates = catalog.GetCandidates(digest);
            Require(candidates.Length == shape.Locations && candidates[0].EndsWith("location-00000000.cbor", StringComparison.Ordinal), "Catalog order or membership changed.");
            return new(0, 0, $"ordered-locations:{candidates.Length}"); },
        _ => throw new ArgumentException("Unknown operation: " + operation)
    });

    private OperationResult BootstrapTransfer()
    {
        var path = Path.Combine(directory, "staging-" + ownedDirectories.Count);
        ownedDirectories.Add(path);
        Int64 written;
        using (var receiver = RoamingNetworkBootstrapReceiver.Create(path, bootstrap!.Manifest))
        {
            for (var i = 0; i < bootstrap.Manifest.ChunkCount; i++)
            {
                var chunk = RoamingNetworkBootstrapChunk.ParseCBOR(bootstrap.CreateChunk(i).ToCBOR());
                Require(receiver.TryAcceptChunk(chunk, out var accepted), accepted.Error);
            }
            Require(receiver.TryActivate(bootstrap.Manifest.Id, out var restored, out var activated, activate: true,
                verifyBatchSignature: VerifyBatch, verifyCommitSignature: VerifyCommit, authorizeSnapshotBoundary: AuthorizeBoundary), activated.Error);
            using (restored!) Check(restored!);
            written = Directory.EnumerateFiles(path).Sum(file => new FileInfo(file).Length);
        }
        // Cleanup is outside the measured call; each sample receives a fresh dedicated staging folder.
        return new(bootstrap.Manifest.ArchiveBytes, written, bootstrap.Manifest.Id.ToString());
    }

    private void Check(RoamingNetworkHistory restored)
    {
        Require(restored.Head.Id == History.Head.Id && restored.AnchorId == History.AnchorId && restored.CheckpointId == History.CheckpointId,
            "Replay identity mismatch.");
        Require(restored.Head.Snapshot.ETags.SequenceEqual(Snapshot.ETags) && restored.Commits.Length == CommitCount &&
            restored.RetentionReceipts.Length == ReceiptCount, "Replay inventory mismatch.");
        Require(restored.Head.Network.EVSEs.Single(value => value.Id.ToString() == "DE*ABC*E1").Status.Value.ToString() == "available",
            "Static recovery inherited live runtime.");
    }

    private static RoamingNetworkChange Change(String power)
        => RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", null, JsonSerializer.SerializeToElement(power));

    private DateTimeOffset At() => Time.AddSeconds(++ordinal);

    private RoamingNetworkCommit Prepare(RoamingNetworkCommitId parent)
    {
        var timestamp = At();
        var change = History.GetSnapshot(parent).CreateChangeSet("change-" + ordinal, timestamp, [Change((100 + ordinal) + " kW")])
            .Sign(Key, "benchmark", COSEAlgorithm.Ed25519);
        return Sign(History.PrepareCommit(parent, change));
    }

    private static RoamingNetworkCommit Sign(RoamingNetworkCommit value) => value.Sign(Key, "benchmark", COSEAlgorithm.Ed25519);

    private void PublishChange()
    {
        var commit = Prepare(History.Head.Id);
        Require(History.TryPublish(commit.Parents[0], commit, out var result), result.Error);
    }

    private RoamingNetworkCommit PublishSnapshot()
    {
        var commit = Sign(History.PrepareSnapshot(History.Head.Id, At()));
        Require(History.TryPublish(commit.Parents[0], commit, out var result), result.Error); return commit;
    }

    internal static RoamingNetwork Network(Int32 evses)
    {
        JObject Node(String id) => new() { ["@id"] = id, ["name"] = new JObject { ["en"] = "Benchmark" },
            ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" };
        var stations = new JArray();
        for (var start = 0; start < evses; start += 4)
        {
            var station = Node($"DE*ABC*S{start / 4 + 1}");
            station["name"] = new JObject { ["de"] = "Ladestation", ["en"] = "Charging station" };
            station["energyMeters"] = new JArray(new JObject { ["id"] = "meter-" + start, ["role"] = "grid", ["created"] = "2026-01-01T00:00:00Z", ["lastChange"] = "2026-01-01T00:00:00Z" });
            var children = new JArray();
            for (var index = start; index < Math.Min(start + 4, evses); index++)
            {
                var evse = Node("DE*ABC*E" + (index + 1));
                evse["currentType"] = new JArray("DC"); evse["maxPower"] = "100 kW";
                evse["socketOutlets"] = new JArray(new JObject { ["@id"] = "1", ["type"] = "CCS", ["lockable"] = false });
                children.Add(evse);
            }
            station["EVSEs"] = children; stations.Add(station);
        }
        var pool = Node("DE*ABC*P1"); pool["chargingStations"] = stations;
        var owner = Node("DE*ABC"); owner["chargingPools"] = new JArray(pool);
        var root = Node("benchmark-network"); root["chargingStationOperators"] = new JArray(owner);
        return RoamingNetwork.Parse(root);
    }

    private static void Require(Boolean condition, String? error)
    {
        if (!condition) throw new InvalidOperationException(error ?? "Benchmark invariant failed.");
    }

    public void Dispose()
    {
        encoding?.Dispose(); persistent?.Dispose(); History?.Dispose();
        // Only direct files in freshly allocated, explicitly owned folders. No recursive deletion.
        foreach (var path in ownedDirectories.AsEnumerable().Reverse())
        {
            foreach (var file in Directory.EnumerateFiles(path)) File.Delete(file);
            Directory.Delete(path);
        }
    }
}
