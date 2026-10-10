using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using cloud.charging.open.protocols.WWCP.POI;
using Org.BouncyCastle.Crypto.Parameters;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Benchmarks;

// Fresh workers measure complete signed batches; construction, signing and verification are setup.
internal sealed class ChangeSetBatchProbe
{
    internal static readonly String[] Operations = ["changeset-batch-json", "changeset-batch-cbor"];
    private static readonly DateTimeOffset Time = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
    private static readonly Ed25519PrivateKeyParameters Key = new(Convert.FromHexString(
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);
    private readonly RoamingNetworkChangeSet batch;
    internal RoamingNetworkChangeSet Batch => batch;
    internal RoamingNetworkDataSnapshot Source => source;
    private readonly RoamingNetworkDataSnapshot source;
    private readonly Byte[] expected;
    private readonly Boolean cbor;
    private Byte[] latest = [];
    private readonly Shape shape;

    internal ChangeSetBatchProbe(Shape shape, String operation)
    {
        this.shape = shape; cbor = operation == "changeset-batch-cbor";
        source = Workload.Network(shape.EVSEs).DataSnapshot;
        JsonElement Value(String json) => JsonSerializer.Deserialize<JsonElement>(json);
        var changes = Enumerable.Range(0, shape.Changes).Select(index => {
            var id = "DE*ABC*E" + (index % shape.EVSEs + 1);
            return (index % 4) switch {
                0 => RoamingNetworkChange.UpdateProperty("EVSE", id, "maxPower", null, Value("\"150 kW\"")),
                1 => RoamingNetworkChange.UpdateProperty("EVSE", id, "name", null, Value("{\"de\":\"Aktualisiert\",\"en\":\"Updated\"}")),
                2 => RoamingNetworkChange.UpdateProperty("EVSE", id, "customData", null,
                    Value("{\"decimal\":1.10,\"exponent\":1e0,\"negativeZero\":-0,\"ETags\":[[\"customer\"]],\"reading\":\"250 kW\",\"nested\":[true,null,{\"a/~\":123456789012345678901234567890}]}")),
                _ => RoamingNetworkChange.UpdateProperty("EVSE", id, "maxCurrent", null, Value("\"200 A\""))
            };
        }).ToImmutableArray();
        batch = source.CreateChangeSet("batch-probe", Time, changes)
            .WithDescription("de", "Ladepunkte aktualisieren").WithDescription("en", "Update charging points")
            .WithMetadata("ticket", "ACME-4711").WithMetadata("numbers", Value("[1.0,1e0,-0,1e-300,1e300]"));
        for (var peer = 0; peer < shape.Branches; peer++) batch = batch.Sign(Key, "benchmark-" + peer, COSEAlgorithm.Ed25519);
        expected = cbor ? Reference(batch) : JSON();
        VerifyBytes(cbor ? batch.ToCBOR() : JSON());
        var recovered = RoamingNetworkChangeSet.ParseCBOR(batch.ToCBOR());
        foreach (var signature in recovered.Signatures)
        {
            if (!recovered.VerifySignature(signature, Key.GeneratePublicKey(), signature.KeyId, out var error))
                throw new InvalidOperationException(error);
            if (!batch.GetSigningBytes(COSEAlgorithm.Ed25519, signature.KeyId).AsSpan().SequenceEqual(
                  recovered.GetSigningBytes(COSEAlgorithm.Ed25519, signature.KeyId)))
                throw new InvalidOperationException("Signing bytes changed after transport.");
        }
        if (!source.ApplyChangeSet(recovered, (value, signature) =>
            value.VerifySignature(signature, Key.GeneratePublicKey(), signature.KeyId, out _)).ETags.SequenceEqual(batch.AfterETags))
            throw new InvalidOperationException("Recovered batch produces a different result.");
    }

    private OperationResult Encode()
    {
        latest = cbor ? batch.ToCBOR() : JSON();
        return new(latest.Length, 0, ETag.Compute(cbor ? ETagFormat.CBOR : ETagFormat.JSON, latest).ToString());
    }

    private void VerifyBytes(Byte[] bytes)
    {
        if (!bytes.AsSpan().SequenceEqual(expected)) throw new InvalidOperationException("Previous-tree batch output changed.");
    }

    private Byte[] JSON()
    {
        using var document = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(batch));
        return CanonicalJSON.ToUTF8Bytes(document.RootElement);
    }

    internal RunResult Run(String operation, Int32 warmups, Int32 samples)
    {
        var identity = Encode(); VerifyBytes(latest);
        for (var i = 1; i < warmups; i++) { if (Encode() != identity) throw new InvalidOperationException(); VerifyBytes(latest); }
        var measured = Enumerable.Range(0, samples).Select(_ => {
            var sample = Measurement.Run(Encode); VerifyBytes(latest);
            if (sample.Result != identity) throw new InvalidOperationException("Nondeterministic batch output.");
            return sample;
        }).ToArray();
        // ArchiveIdentity binds the complete signed batch CBOR in this suite; no history is constructed.
        return new(shape, operation, source.Entities.Count, 0, 0, 0, String.Join("|", source.ETags),
            ETag.Compute(ETagFormat.CBOR, batch.ToCBOR()).ToString(), measured);
    }

    private static Byte[] Reference(RoamingNetworkChangeSet value)
    {
        using var doc = JsonDocument.Parse(JsonSerializer.SerializeToUtf8Bytes(value));
        var validate = Bind<Action<JsonElement>>("ValidateSigningJSON"); validate(doc.RootElement);
        var paths = typeof(RoamingNetworkChangeSet).GetMethod("MeasurementPaths", BindingFlags.Instance | BindingFlags.NonPublic)!
            .CreateDelegate<Func<HashSet<String>>>(value)();
        var encode = Bind<Func<JsonElement, String, HashSet<String>, CBORValue>>("EncodeTransportJSON");
        var convert = Bind<Func<CBORValue, Boolean, CBORValue>>("ConvertTransportETags");
        return convert(encode(doc.RootElement, "", paths), true).ToByteArray(CBORWriterOptions.Canonical);
    }

    private static T Bind<T>(String name) where T : Delegate
        => typeof(RoamingNetworkChangeSet).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!.CreateDelegate<T>();
}
