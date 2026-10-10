using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Org.BouncyCastle.Crypto.Parameters;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Benchmarks;

// Prepared histories contain one genuinely signed ordered batch; every output is consumed.
internal sealed class ArchiveBatchProbe : IDisposable
{
    internal static readonly String[] Operations = ["batch-archive-cbor", "batch-archive-cbor-stream", "batch-archive-control-json"];
    private static readonly Ed25519PrivateKeyParameters Key = new(Convert.FromHexString(
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);
    private readonly Shape shape;
    private readonly RoamingNetworkHistory history;
    internal RoamingNetworkHistory History => history;
    private readonly RoamingNetworkChangeSet batch;
    private readonly OperationResult cborIdentity;
    private readonly OperationResult jsonIdentity;

    internal ArchiveBatchProbe(Shape shape)
    {
        this.shape = shape;
        var probe = new ChangeSetBatchProbe(shape, "changeset-batch-cbor");
        batch = probe.Batch;
        history = new(RoamingNetwork.Parse(probe.Source.ToJSON()), VerifyBatch, VerifyCommit);
        try
        {
            if (!history.Head.Snapshot.ETags.SequenceEqual(probe.Source.ETags)) throw new InvalidOperationException("Source changed.");
            var checkpoint = Sign(history.Head.Commit);
            if (!history.TryStoreCommit(checkpoint, out var stored)) throw new InvalidOperationException(stored.Error);
            var parent = history.Head.Id;
            var commit = Sign(history.PrepareCommit(parent, batch));
            if (!history.TryPublish(parent, commit, out var published)) throw new InvalidOperationException(published.Error);
            var bytes = history.ToCBOR(); cborIdentity = Identity(bytes, ETagFormat.CBOR);
            jsonIdentity = Identity(JSON(), ETagFormat.JSON);
            using var restored = RoamingNetworkHistory.ParseCBOR(bytes, VerifyBatch, VerifyCommit);
            if (restored.Head.Id != history.Head.Id || !restored.Head.Snapshot.ETags.SequenceEqual(batch.AfterETags) ||
                restored.Head.Commit.Signatures.Length != shape.Branches || restored.Head.Commit.ChangeSet!.Signatures.Length != shape.Branches)
                throw new InvalidOperationException("Signed archive recovery changed.");
        }
        catch { history.Dispose(); throw; }
    }

    internal static Boolean VerifyBatch(RoamingNetworkChangeSet value, RoamingNetworkChangeSetSignature peer)
        => value.VerifySignature(peer, Key.GeneratePublicKey(), peer.KeyId, out _);
    internal static Boolean VerifyCommit(RoamingNetworkCommit value, RoamingNetworkChangeSetSignature peer)
        => value.VerifySignature(peer, Key.GeneratePublicKey(), peer.KeyId, out _);
    private RoamingNetworkCommit Sign(RoamingNetworkCommit value)
    {
        for (var peer = 0; peer < shape.Branches; peer++) value = value.Sign(Key, "benchmark-" + peer, COSEAlgorithm.Ed25519);
        return value;
    }

    private Byte[] JSON()
    {
        using var document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(batch));
        return CanonicalJSON.ToUTF8Bytes(document.RootElement);
    }
    private static OperationResult Identity(Byte[] bytes, ETagFormat format)
        => new(bytes.Length, 0, ETag.Compute(format, bytes).ToString());

    private OperationResult Encode(String operation)
    {
        if (operation == "batch-archive-control-json") return Identity(JSON(), ETagFormat.JSON);
        if (operation == "batch-archive-cbor") return Identity(history.ToCBOR(), ETagFormat.CBOR);
        using var sink = new DigestStream(); history.WriteCBOR(sink); return sink.Complete(ETagFormat.CBOR);
    }

    internal RunResult Run(String operation, Int32 warmups, Int32 samples)
    {
        var expected = operation == "batch-archive-control-json" ? jsonIdentity : cborIdentity;
        for (var index = 0; index < warmups; index++)
            if (Encode(operation) != expected) throw new InvalidOperationException("Warmup output changed.");
        var measured = Enumerable.Range(0, samples).Select(_ => {
            var result = Measurement.Run(() => Encode(operation));
            if (result.Result != expected) throw new InvalidOperationException("Measured output changed.");
            return result;
        }).ToArray();
        return new(shape, operation, history.Head.Snapshot.Entities.Count, history.Commits.Length, 0, 0,
            String.Join("|", history.Head.Snapshot.ETags), cborIdentity.Identity, measured);
    }

    public void Dispose() => history.Dispose();
}
