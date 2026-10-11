/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Reflection;
using cloud.charging.open.protocols.WWCP.POI;
using Org.BouncyCastle.Crypto.Parameters;
using org.GraphDefined.Vanaheimr.Illias;

namespace WWCP_POI_Benchmarks;

// Sixteen copies per sample; transport, independent full-constructor oracle and trust are outside measurement.
internal sealed class SignatureCopyProbe
{
    internal static readonly String[] Operations = ["checkpoint-copy-signatures", "commit-copy-signatures",
        "snapshot-copy-signatures", "commit-copy-batch-peers", "commit-sign"];
    private static readonly Ed25519PrivateKeyParameters Key = new(Convert.FromHexString(
        "000102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f"), 0);
    private readonly Shape shape;
    private readonly RoamingNetworkCommit original;
    private readonly RoamingNetworkChangeSet batch;
    private readonly ImmutableArray<RoamingNetworkChangeSetSignature> signatures;
    private readonly String operation;
    private readonly String expectedJSON;
    private readonly Byte[] expectedCBOR;
    private RoamingNetworkCommit latest;
    private readonly RoamingNetworkDataSnapshot source;
    private readonly OperationResult result;

    internal SignatureCopyProbe(Shape shape, String operation)
    {
        this.shape = shape; this.operation = operation;
        var batches = new ChangeSetBatchProbe(shape, "changeset-batch-cbor");
        source = batches.Source;
        var root = RoamingNetworkCommit.CreateCheckpoint(source);
        batch = batches.Batch;
        original = operation switch {
            "checkpoint-copy-signatures" => root,
            "snapshot-copy-signatures" => RoamingNetworkCommit.CreateSnapshot(root, source,
                DateTimeOffset.Parse("2026-01-02T00:00:00Z")),
            _ => RoamingNetworkCommit.Create(root, batch.WithoutSignatures())
        };
        var signed = original;
        for (var i = 0; i < shape.Branches; i++) signed = signed.Sign(Key, "benchmark-" + i, COSEAlgorithm.Ed25519);
        signatures = signed.Signatures;
        var expectedSignatures = operation == "commit-copy-batch-peers" ? original.Signatures :
            operation == "commit-sign" ? original.Sign(Key, "benchmark-added", COSEAlgorithm.Ed25519).Signatures : signatures;
        var constructor = typeof(RoamingNetworkCommit).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)
            .Single(value => value.GetParameters().Length == 9);
        var expected = (RoamingNetworkCommit) constructor.Invoke([original.RoamingNetworkId, original.Revision,
            original.Parents, original.StateETags, original.AppliedChangeSetId,
            operation == "commit-copy-batch-peers" ? batch : original.ChangeSet,
            expectedSignatures, original.Id, original.Snapshot]);
        expectedJSON = expected.ToJSON(); expectedCBOR = expected.ToCBOR(); latest = original;
        result = new(expectedCBOR.Length, 0, original.Id.ToString());
        Execute(); Verify();
    }

    private OperationResult Execute()
    {
        for (var i = 0; i < 16; i++)
            latest = operation switch {
                "commit-copy-batch-peers" => original.WithChangeSet(batch),
                "commit-sign" => original.Sign(Key, "benchmark-added", COSEAlgorithm.Ed25519),
                _ => original.WithSignatures(signatures)
            };
        return result;
    }

    private void Verify()
    {
        if (latest.Id != original.Id || latest.ToJSON() != expectedJSON || !latest.ToCBOR().AsSpan().SequenceEqual(expectedCBOR))
            throw new InvalidOperationException("Copy differs from full-constructor identity or peer transport.");
        foreach (var peer in latest.Signatures)
            if (!latest.VerifySignature(peer, Key.GeneratePublicKey(), peer.KeyId, out var error))
                throw new InvalidOperationException(error);
        foreach (var peer in latest.ChangeSet?.Signatures ?? [])
            if (!latest.ChangeSet!.VerifySignature(peer, Key.GeneratePublicKey(), peer.KeyId, out var error))
                throw new InvalidOperationException(error);
    }

    internal RunResult Run(Int32 warmups, Int32 samples)
    {
        for (var i = 0; i < warmups; i++) { Execute(); Verify(); }
        var measured = Enumerable.Range(0, samples).Select(_ => {
            var sample = Measurement.Run(Execute); Verify(); return sample;
        }).ToArray();
        return new(shape, operation, source.Entities.Count, 1, 0, 0, String.Join("|", source.ETags),
            ETag.Compute(ETagFormat.CBOR, expectedCBOR).ToString(), measured);
    }
}
