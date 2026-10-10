/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using cloud.charging.open.protocols.WWCP.POI;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Benchmarks;

internal sealed record RecoveryInventory(String Profile, Int32 InputCBORBytes, Int32 InputJSONBytes,
    Int32 CommitPeers, Int32 BatchPeers, Int32 Snapshots, Int32 MaxBatchOperations);
internal sealed record ReplayRetention(Int64 ManagedDeltaBytes, Int32 RetainedCommits, Int32 RetainedSnapshots);

// Stage access is bound once during setup. No production API or alternative replay algorithm.
internal sealed class ArchiveRecoveryProbe : IDisposable
{
    internal static readonly String[] Operations = ["read-cbor-limits", "read-cbor-tree", "read-cbor-model",
        "read-signatures", "read-restore", "read-cbor", "read-json"];
    internal static readonly String[] StreamOperations = ["read-cbor-input-memory", "read-cbor-input-spool", "read-cbor-input-token"];
    internal static readonly String[] FileOperations = ["read-cbor-file-array", "read-cbor-file-mapped"];
    internal static readonly String[] AllOperations = [.. Operations, .. StreamOperations, .. FileOperations];

    private sealed class Input(Byte[] bytes) : Stream
    {
        private Int32 offset;
        public override Boolean CanRead => true;
        public override Boolean CanSeek => false;
        public override Boolean CanWrite => false;
        public override Int64 Length => throw new NotSupportedException();
        public override Int64 Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override Int32 Read(Byte[] buffer, Int32 start, Int32 count)
        {
            var length = Math.Min(Math.Min(count, 16 * 1024), bytes.Length - offset);
            bytes.AsSpan(offset, length).CopyTo(buffer.AsSpan(start, length)); offset += length; return length;
        }
        public override void Flush() => throw new NotSupportedException();
        public override Int64 Seek(Int64 offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(Int64 length) => throw new NotSupportedException();
        public override void Write(Byte[] buffer, Int32 offset, Int32 count) => throw new NotSupportedException();
    }

    private readonly CancellationTokenSource readCancellation = new();
    private readonly RoamingNetworkArchiveReadOptions memoryInput = new(Int32.MaxValue);
    private readonly RoamingNetworkArchiveReadOptions fileInput = new(0);

    private delegate void Scan(ReadOnlySpan<Byte> bytes, RoamingNetworkHistoryLimits limits);
    private delegate RoamingNetworkHistory RestoreComplete(RoamingNetworkDataSnapshot snapshot,
        RoamingNetworkCommit root, IEnumerable<RoamingNetworkCommit> commits, RoamingNetworkCommitId head,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? batchTrust,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? commitTrust,
        Func<RoamingNetworkCommit, Boolean>? authorize);
    private delegate RoamingNetworkHistory RestorePartial(RoamingNetworkCommitId checkpoint,
        RoamingNetworkCommit root, IEnumerable<RoamingNetworkCommit> commits, RoamingNetworkCommitId head,
        Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean>? batchTrust,
        Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean>? commitTrust,
        Func<RoamingNetworkCommit, Boolean>? authorize,
        Func<RoamingNetworkSnapshotBoundary, Boolean>? boundary,
        ImmutableArray<RoamingNetworkRetentionReceipt> receipts);
    private sealed record Model(String Profile, RoamingNetworkCommitId CheckpointId, RoamingNetworkCommit Root,
        ImmutableArray<RoamingNetworkCommit> Commits, RoamingNetworkCommitId Head,
        RoamingNetworkDataSnapshot? Checkpoint, ImmutableArray<RoamingNetworkRetentionReceipt> Receipts);

    private static readonly Scan Check = Bind<Scan>(typeof(RoamingNetworkHistory), "CheckCBORArchive");
    private static readonly RestoreComplete Complete = Bind<RestoreComplete>(typeof(RoamingNetworkHistory), "Restore");
    private static readonly RestorePartial Partial = Bind<RestorePartial>(typeof(RoamingNetworkHistory), "RestoreBoundary");
    private static readonly Func<CBORValue, RoamingNetworkCommit> ParseCommit = Bind<Func<CBORValue, RoamingNetworkCommit>>(typeof(RoamingNetworkCommit), "ParseCBORValue");
    private static readonly Func<CBORValue, RoamingNetworkRetentionReceipt> ParseReceipt = Bind<Func<CBORValue, RoamingNetworkRetentionReceipt>>(typeof(RoamingNetworkRetentionReceipt), "ParseCBORValue");
    private static readonly Func<CBORValue, String[], Dictionary<String, CBORValue>> Fields = Bind<Func<CBORValue, String[], Dictionary<String, CBORValue>>>(typeof(RoamingNetworkCommit), "Fields");
    private static readonly Func<CBORValue, String> Text = Bind<Func<CBORValue, String>>(typeof(RoamingNetworkCommit), "Text");
    private static readonly Func<Boolean, String[]> BoundaryFields = Bind<Func<Boolean, String[]>>(typeof(RoamingNetworkHistory), "BoundaryFields");
    private static readonly Action<String?, IEnumerable<RoamingNetworkCommit>> RequireProfile = Bind<Action<String?, IEnumerable<RoamingNetworkCommit>>>(typeof(RoamingNetworkHistory), "RequireArchiveProfile");

    private readonly Shape shape;
    private readonly String operation;
    private readonly IDisposable owner;
    private readonly RoamingNetworkHistory source;
    private readonly RoamingNetworkHistory? boundarySource;
    private readonly Func<RoamingNetworkChangeSet, RoamingNetworkChangeSetSignature, Boolean> batchTrust;
    private readonly Func<RoamingNetworkCommit, RoamingNetworkChangeSetSignature, Boolean> commitTrust;
    private readonly Byte[] bytes;
    private readonly String json;
    private readonly String archiveIdentity;
    private readonly String staticIdentity;
    private readonly RoamingNetworkHistoryLimits limits = new();
    private readonly CBORValue? inputTree;
    private readonly Model? inputModel;
    private readonly String modelIdentity;
    private readonly RecoveryInventory inventory;
    private readonly DomainRecoveryFixture? domain;
    private CBORValue? latestTree;
    private Model? latestModel;
    private RoamingNetworkHistory? latestHistory;
    private Int32 latestSignatures;
    private String? inputDirectory;
    private String? inputPath;
    private FileStream? inputLease;

    internal ArchiveRecoveryProbe(Shape shape, String operation, DomainRecoveryFixture? domain = null)
    {
        this.shape = shape; this.operation = operation; this.domain = domain;
        if (domain is not null)
        {
            owner = domain; source = domain.History;
            batchTrust = DomainRecoveryFixture.VerifyBatch; commitTrust = DomainRecoveryFixture.VerifyCommit;
        }
        else if (shape.Name.StartsWith("recover-batch-", StringComparison.Ordinal))
        {
            var batch = new ArchiveBatchProbe(shape); owner = batch; source = batch.History;
            batchTrust = ArchiveBatchProbe.VerifyBatch; commitTrust = ArchiveBatchProbe.VerifyCommit;
        }
        else
        {
            var workload = new Workload(shape, "replay-cbor"); owner = workload; source = workload.History;
            batchTrust = Workload.VerifyBatch; commitTrust = Workload.VerifyCommit;
        }
        try
        {
            if (shape.Name == "recover-boundary-16")
            {
                var root = source.Commits.Where(value => value.Kind == RoamingNetworkCommitKind.Snapshot).MinBy(value => value.Revision)!;
                boundarySource = RoamingNetworkHistory.FromSnapshot(source.CheckpointId, root, Boundary, commitTrust, batchTrust);
                foreach (var commit in source.Commits.Where(value => value.Revision > root.Revision).OrderBy(value => value.Revision).ThenBy(value => value.Id.ToString(), StringComparer.Ordinal))
                    if (!boundarySource.TryStoreCommit(commit, out var stored)) throw new InvalidOperationException(stored.Error);
                if (!boundarySource.TryAdoptHead(root.Id, source.Head.Id, out var adopted, adopt: true)) throw new InvalidOperationException(adopted.Error);
                source = boundarySource;
            }
            source.ApplyRuntimeUpdate(new("benchmark-network", POIRuntimeTarget.Entity(InfrastructureEntityType.EVSE, "DE*ABC*E1"),
                POIRuntimeStatusKind.Status, new("charging", DateTimeOffset.Parse("2026-06-01T00:00:00Z")), mode: POIRuntimeUpdateMode.ReplaceHistory));
            bytes = source.ToCBOR(); json = source.ToJSON();
            archiveIdentity = ETag.Compute(ETagFormat.CBOR, bytes).ToString(); staticIdentity = String.Join("|", source.Head.Snapshot.ETags);
            Check(bytes, limits); var tree = CBORValue.Parse(bytes); var model = DecodeModel(tree);
            modelIdentity = Fingerprint(model);
            inputTree = operation == "read-cbor-model" ? tree : (CBORValue?) null;
            inputModel = operation is "read-restore" or "read-signatures" ? model : null;
            inventory = new(model.Profile, bytes.Length, Encoding.UTF8.GetByteCount(json),
                source.Commits.Sum(value => value.Signatures.Length), source.Commits.Sum(value => value.ChangeSet?.Signatures.Length ?? 0),
                source.Commits.Count(value => value.Kind == RoamingNetworkCommitKind.Snapshot),
                source.Commits.Max(value => value.ChangeSet?.Changes.Length ?? 0));
            using var direct = Recover(model); VerifyRecovered(direct, true);
            using var cbor = RoamingNetworkHistory.ParseCBOR(bytes, batchTrust, commitTrust, authorizeSnapshotBoundary: Boundary); VerifyRecovered(cbor, true);
            using var text = RoamingNetworkHistory.Parse(json, batchTrust, commitTrust, authorizeSnapshotBoundary: Boundary); VerifyRecovered(text, true);
            VerifySignatures(model);
            if (FileOperations.Contains(operation))
            {
                inputDirectory = Path.Combine(Path.GetTempPath(), "wwcp-poi-mapped-probe-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(inputDirectory);
                inputPath = Path.Combine(inputDirectory, "history.cbor"); File.WriteAllBytes(inputPath, bytes);
            }
        }
        catch { Dispose(); throw; }
    }

    private Boolean Boundary(RoamingNetworkSnapshotBoundary value) => value.Checkpoint == source.CheckpointId;

    // Mirror only envelope composition; actual model parsers, profile/field checks and replay are bound above.
    // Boundary production recovery enumerates suffix models lazily during replay. This model-only probe is eager.
    private static Model DecodeModel(CBORValue value)
    {
        var profile = Text(value.AsMap().Single(field => Text(field.Key) == "Profile").Value);
        if (profile is RoamingNetworkHistory.BoundaryArchiveProfile or RoamingNetworkHistory.RetentionArchiveProfile)
        {
            var hasReceipts = profile == RoamingNetworkHistory.RetentionArchiveProfile;
            var fields = Fields(value, BoundaryFields(hasReceipts)); POIContentProfile.Require(Text(fields["ContentProfile"]));
            var receipts = hasReceipts ? fields["RetentionReceipts"].AsArray().Select(ParseReceipt).ToImmutableArray() : [];
            if (hasReceipts && receipts.IsEmpty) throw new ArgumentException("A retention archive requires receipts.");
            return new(profile, new(ETag.Parse(fields["CheckpointId"])), ParseCommit(fields["SnapshotCommit"]),
                fields["Commits"].AsArray().Select(ParseCommit).ToImmutableArray(), new(ETag.Parse(fields["Head"])), null, receipts);
        }
        var complete = Fields(value, ["Profile", "ContentProfile", "Checkpoint", "CheckpointCommit", "Commits", "Head"]);
        if (profile is not (RoamingNetworkHistory.ArchiveProfile or RoamingNetworkHistory.SnapshotArchiveProfile)) throw new ArgumentException("Unknown archive profile.");
        POIContentProfile.Require(Text(complete["ContentProfile"]));
        var commits = complete["Commits"].AsArray().Select(ParseCommit).ToImmutableArray(); RequireProfile(profile, commits);
        var checkpoint = RoamingNetworkDataSnapshot.ParseCBOR(complete["Checkpoint"].ToByteArray(CBORWriterOptions.Canonical));
        var root = ParseCommit(complete["CheckpointCommit"]);
        return new(profile, root.Id, root, commits, new(ETag.Parse(complete["Head"])), checkpoint, []);
    }

    private RoamingNetworkHistory Recover(Model model)
        => model.Checkpoint is { } checkpoint ? Complete(checkpoint, model.Root, model.Commits, model.Head, batchTrust, commitTrust, null) :
            Partial(model.CheckpointId, model.Root, model.Commits, model.Head, batchTrust, commitTrust, null, Boundary, model.Receipts);

    private Int32 VerifySignatures(Model model)
    {
        var count = 0;
        foreach (var commit in model.Commits.Prepend(model.Root))
        {
            foreach (var peer in commit.Signatures)
            { if (!commitTrust(commit, peer)) throw new InvalidOperationException("Invalid commit signature."); count++; }
            if (commit.ChangeSet is { } batch)
                foreach (var peer in batch.Signatures)
                { if (!batchTrust(batch, peer)) throw new InvalidOperationException("Invalid batch signature."); count++; }
        }
        return count;
    }

    private static String Fingerprint(Model model)
    {
        using var output = new MemoryStream(); using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
        writer.Write(model.Profile); writer.Write(model.CheckpointId.ToString()); writer.Write(model.Head.ToString());
        void Part(Byte[] value) { writer.Write(value.Length); writer.Write(value); }
        Part(model.Checkpoint?.ToCBOR(IncludeVersionMetadata: true) ?? []); Part(model.Root.ToCBOR());
        writer.Write(model.Commits.Length); foreach (var commit in model.Commits) Part(commit.ToCBOR());
        writer.Write(model.Receipts.Length); foreach (var receipt in model.Receipts) Part(receipt.ToCBOR());
        writer.Flush(); return "model:sha256:hex:" + Convert.ToHexStringLower(SHA256.HashData(output.GetBuffer().AsSpan(0, checked((Int32)output.Length))));
    }

    private void VerifyRecovered(RoamingNetworkHistory value, Boolean export)
    {
        if (value.Head.Id != source.Head.Id || value.AnchorId != source.AnchorId || value.CheckpointId != source.CheckpointId ||
            value.Commits.Length != source.Commits.Length || !value.Head.Snapshot.ETags.SequenceEqual(source.Head.Snapshot.ETags) ||
            !value.RetentionReceipts.Select(item => item.Id).SequenceEqual(source.RetentionReceipts.Select(item => item.Id)))
            throw new InvalidOperationException("Recovered identity/inventory changed.");
        foreach (var commit in value.Commits)
            if (!value.GetSnapshot(commit.Id).ETags.SequenceEqual(source.GetSnapshot(commit.Id).ETags)) throw new InvalidOperationException("Branch state changed.");
        if (value.Head.Network.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1").Status.Value.ToString() != "available" ||
            source.Head.Network.EVSEs.Single(item => item.Id.ToString() == "DE*ABC*E1").Status.Value.ToString() != "charging")
            throw new InvalidOperationException("Local runtime leaked into recovery or source changed.");
        if (export && !value.ToCBOR().AsSpan().SequenceEqual(bytes)) throw new InvalidOperationException("Recovery changed exact archive bytes/peers.");
        domain?.CheckRecovered(value);
    }

    private OperationResult Action()
    {
        switch (operation)
        {
            case "read-cbor-limits": Check(bytes, limits); return new(bytes.Length, 0, archiveIdentity);
            case "read-cbor-tree": latestTree = CBORValue.Parse(bytes); return new(0, 0, archiveIdentity);
            case "read-cbor-model": latestModel = DecodeModel(inputTree!.Value); return new(0, 0, modelIdentity);
            case "read-signatures": latestSignatures = VerifySignatures(inputModel!); return new(0, 0, $"peers:{latestSignatures}:{source.Head.Id}");
            case "read-restore": latestHistory = Recover(inputModel!); break;
            case "read-cbor": latestHistory = RoamingNetworkHistory.ParseCBOR(bytes, batchTrust, commitTrust, authorizeSnapshotBoundary: Boundary); break;
            case "read-cbor-input-memory":
            case "read-cbor-input-spool":
            case "read-cbor-input-token":
                using (var input = new Input(bytes))
                    latestHistory = RoamingNetworkHistory.ParseCBOR(input, batchTrust, commitTrust, authorizeSnapshotBoundary: Boundary,
                        limits: limits, readOptions: operation == "read-cbor-input-spool" ? fileInput : memoryInput, cancellationToken: operation == "read-cbor-input-token" ? readCancellation.Token : default);
                break;
            case "read-cbor-file-array":
                // Previous Open input algorithm as a same-build control. Hold the writer lease until
                // post-measurement verification/disposal, just like the mapped persistent history.
                inputLease = new FileStream(inputPath + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
                Byte[] captured;
                using (var file = new FileStream(inputPath!, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (file.Length > limits.MaxArchiveBytes) throw new InvalidOperationException("Benchmark fixture exceeds its archive budget.");
                    captured = new Byte[checked((Int32) file.Length)]; file.ReadExactly(captured);
                    if (file.ReadByte() != -1) throw new IOException("Archive length changed while reading.");
                }
                latestHistory = RoamingNetworkHistory.ParseCBOR(captured, batchTrust, commitTrust, authorizeSnapshotBoundary: Boundary, limits: limits);
                break;
            case "read-cbor-file-mapped":
                latestHistory = RoamingNetworkHistory.Open(inputPath!, batchTrust, commitTrust, authorizeSnapshotBoundary: Boundary, limits: limits);
                break;
            case "read-json": latestHistory = RoamingNetworkHistory.Parse(json, batchTrust, commitTrust, authorizeSnapshotBoundary: Boundary); break;
            default: throw new ArgumentException("Unknown recovery operation.");
        }
        return new(operation == "read-cbor" || StreamOperations.Contains(operation) || FileOperations.Contains(operation) ? bytes.Length : operation == "read-json" ? inventory.InputJSONBytes : 0, 0, latestHistory.Head.Id.ToString());
    }

    private void Verify()
    {
        try
        {
            if (latestTree is { } tree && !tree.ToByteArray(CBORWriterOptions.Canonical).AsSpan().SequenceEqual(bytes)) throw new InvalidOperationException("Tree output changed.");
            if (latestModel is { } model && Fingerprint(model) != modelIdentity) throw new InvalidOperationException("Model bytes changed.");
            if (operation == "read-signatures" && latestSignatures != inventory.CommitPeers + inventory.BatchPeers) throw new InvalidOperationException("Peer count changed.");
            if (latestHistory is { } history) VerifyRecovered(history, true);
        }
        finally { latestHistory?.Dispose(); inputLease?.Dispose(); inputLease = null; latestHistory = null; latestTree = null; latestModel = null; }
    }

    private ReplayRetention Retention()
    {
        GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); var before = GC.GetTotalMemory(true);
        using var restored = RoamingNetworkHistory.ParseCBOR(bytes, batchTrust, commitTrust, authorizeSnapshotBoundary: Boundary);
        VerifyRecovered(restored, false);
        GC.Collect(2, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); var after = GC.GetTotalMemory(true);
        GC.KeepAlive(restored); GC.KeepAlive(source);
        return new(after - before, restored.Commits.Length, inventory.Snapshots);
    }

    internal RunResult Run(Int32 warmups, Int32 samples)
    {
        OperationResult? expected = null;
        for (var index = 0; index < warmups; index++)
        {
            var result = Action(); Verify(); expected ??= result;
            if (result != expected) throw new InvalidOperationException("Warmup result changed.");
        }
        var measured = Enumerable.Range(0, samples).Select(_ => {
            var result = Measurement.Run(Action); Verify();
            if (result.Result != expected) throw new InvalidOperationException("Measured result changed."); return result;
        }).ToArray();
        return new(shape, operation, source.Head.Snapshot.Entities.Count, source.Commits.Length, source.RetentionReceipts.Length,
            source.RetentionReceipts.Sum(value => value.PrunedCommits.Length + value.ArchiveOnlyTips.Length), staticIdentity, archiveIdentity, measured,
            RecoveryInventory: inventory, ReplayRetention: operation == "read-cbor" ? Retention() : null,
            DomainInventory: domain?.Inventory);
    }

    private static T Bind<T>(Type owner, String name) where T : Delegate
        => (owner.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(owner.FullName, name)).CreateDelegate<T>();

    public void Dispose()
    {
        try { latestHistory?.Dispose(); inputLease?.Dispose(); boundarySource?.Dispose(); owner.Dispose(); }
        finally
        {
            readCancellation.Dispose();
            if (inputPath is not null) { File.Delete(inputPath); File.Delete(inputPath + ".lock"); }
            if (inputDirectory is not null) Directory.Delete(inputDirectory);
        }
    }
}
