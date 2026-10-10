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

// Existing recovery contracts to preserve when a future reader accepts segmented input.
[TestFixture]
public sealed class ArchiveRecoveryContractTests
{
    private static readonly String[] Names = ["history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history"];
    private sealed class Trust
    {
        internal Int32 BatchCalls, CommitCalls, BoundaryCalls;
        internal Boolean Batch(RoamingNetworkChangeSet value, RoamingNetworkChangeSetSignature peer)
        { BatchCalls++; return VerifyBatch(value, peer); }
        internal Boolean Commit(RoamingNetworkCommit value, RoamingNetworkChangeSetSignature peer)
        { CommitCalls++; return VerifyCommit(value, peer); }
        internal Boolean Boundary(RoamingNetworkSnapshotBoundary _) { BoundaryCalls++; return true; }
        internal Int32 Calls => BatchCalls + CommitCalls + BoundaryCalls;
    }

    private static Byte[] Bytes(Int32 profile) => File.ReadAllBytes(FilePath(Names[profile - 1] + ".cbor"));
    private static RoamingNetworkHistory Recover(Byte[] bytes, Trust trust, RoamingNetworkHistoryLimits? limits = null)
        => RoamingNetworkHistory.ParseCBOR(bytes, trust.Batch, trust.Commit, authorizeSnapshotBoundary: trust.Boundary, limits: limits);

    private static CBORValue Replace(CBORValue map, String name, CBORValue value)
        => CBORValue.FromMap(map.AsMap().Select(field => new KeyValuePair<CBORValue, CBORValue>(field.Key,
            RoamingNetworkCommit.Text(field.Key) == name ? value : field.Value)));

    [Test, Combinatorial]
    public void Every_truncated_input_is_rejected_before_any_trust_callback(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(0, 1, 2, 3, 4)] Int32 cut)
    {
        var bytes = Bytes(profile); var length = cut switch { 0 => 0, 1 => 1, 2 => 2, 3 => bytes.Length / 2, _ => bytes.Length - 1 };
        var trust = new Trust();
        Assert.That(() => Recover(bytes[..length], trust), Throws.Exception);
        Assert.That(trust.Calls, Is.Zero);
        using var retry = Recover(bytes, trust); Assert.That(retry.ToCBOR(), Is.EqualTo(bytes));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void A_complete_archive_with_trailing_data_is_rejected_before_trust(Int32 profile)
    {
        var trust = new Trust(); var bytes = Bytes(profile);
        Assert.That(() => Recover([.. bytes, 0xf6], trust), Throws.Exception);
        Assert.That(trust.Calls, Is.Zero);
        using var retry = Recover(bytes, trust); Assert.That(retry.ToCBOR(), Is.EqualTo(bytes));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Arbitrary_root_field_order_recovers_the_same_signed_archive(Int32 profile)
    {
        var bytes = Bytes(profile);
        var reverse = CBORValue.FromMap(CBORValue.Parse(bytes).AsMap().Reverse()).ToByteArray(new CBORWriterOptions());
        Assert.That(reverse, Is.Not.EqualTo(bytes));
        using var recovered = Recover(reverse, new()); Assert.That(recovered.ToCBOR(), Is.EqualTo(bytes));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void An_indefinite_root_map_is_accepted_and_canonicalized_on_export(Int32 profile)
    {
        var bytes = Bytes(profile); Assert.That(bytes[0], Is.AnyOf((Byte)0xa6, (Byte)0xa7));
        using var recovered = Recover([0xbf, .. bytes[1..], 0xff], new());
        Assert.That(recovered.ToCBOR(), Is.EqualTo(bytes));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void The_local_byte_limit_precedes_even_a_malformed_last_byte(Int32 profile)
    {
        var bytes = Bytes(profile); bytes[^1] = 0xff; var trust = new Trust();
        var error = Assert.Throws<RoamingNetworkHistoryLimitException>(() => Recover(bytes, trust, new(maxArchiveBytes: bytes.Length - 1)))!;
        Assert.That(error.Violation, Is.EqualTo(new RoamingNetworkHistoryLimitViolation(
            RoamingNetworkHistoryLimitKind.ArchiveBytes, bytes.Length - 1, bytes.Length)));
        Assert.That(trust.Calls, Is.Zero);
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Unknown_root_fields_are_rejected_before_trust(Int32 profile)
    {
        var value = CBORValue.Parse(Bytes(profile)); var trust = new Trust();
        var invalid = CBORValue.FromMap(value.AsMap().Append(new(CBORValue.FromText("Unknown"), CBORValue.Null)));
        Assert.That(() => Recover(invalid.ToByteArray(CBORWriterOptions.Canonical), trust), Throws.Exception);
        Assert.That(trust.Calls, Is.Zero);
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void An_unknown_head_fails_after_callbacks_without_returning_a_history(Int32 profile)
    {
        var bytes = Bytes(profile); var trust = new Trust(); RoamingNetworkHistory? result = null;
        var invalid = Replace(CBORValue.Parse(bytes), "Head", ETag.Compute(ETagFormat.JSON, [42]).ToCBOR());
        Assert.That(() => result = Recover(invalid.ToByteArray(CBORWriterOptions.Canonical), trust), Throws.Exception);
        Assert.That(result, Is.Null); Assert.That(trust.Calls, Is.GreaterThan(0));
        using var retry = Recover(bytes, new()); Assert.That(retry.ToCBOR(), Is.EqualTo(bytes));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Every_root_peer_must_verify_even_when_another_peer_is_valid(Int32 profile)
    {
        var bytes = Bytes(profile); var value = CBORValue.Parse(bytes);
        var name = profile <= 2 ? "CheckpointCommit" : "SnapshotCommit";
        var root = value.AsMap().Single(field => RoamingNetworkCommit.Text(field.Key) == name).Value;
        var peers = root.AsMap().Single(field => RoamingNetworkCommit.Text(field.Key) == "Signatures").Value.AsArray();
        Assert.That(peers.Count, Is.EqualTo(2));
        var invalidPeer = Replace(peers[1], "Value", CBORValue.FromText(Convert.ToBase64String(new Byte[64])));
        var invalid = Replace(value, name, Replace(root, "Signatures", CBORValue.FromArray([peers[0], invalidPeer])));
        var trust = new Trust();
        Assert.That(() => Recover(invalid.ToByteArray(CBORWriterOptions.Canonical), trust), Throws.Exception);
        Assert.That(trust.CommitCalls, Is.EqualTo(2)); Assert.That(trust.BatchCalls, Is.Zero);
        using var retry = Recover(bytes, new()); Assert.That(retry.ToCBOR(), Is.EqualTo(bytes));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Late_model_errors_preserve_the_current_profile_specific_callback_phase(Int32 profile)
    {
        var value = CBORValue.Parse(Bytes(profile)); var trust = new Trust();
        var commits = value.AsMap().Single(field => RoamingNetworkCommit.Text(field.Key) == "Commits").Value.AsArray();
        var invalid = Replace(value, "Commits", CBORValue.FromArray(commits.Append(CBORValue.FromText("not a commit"))));
        Assert.That(() => Recover(invalid.ToByteArray(CBORWriterOptions.Canonical), trust), Throws.Exception);
        // Complete profiles decode all models before Restore; boundary profiles decode their suffix lazily.
        if (profile <= 2) Assert.That(trust.Calls, Is.Zero);
        else Assert.That(trust.Calls, Is.GreaterThan(0));
    }

    [TestCase(3)] [TestCase(4)]
    public void Boundary_recovery_requires_an_explicit_policy_before_callbacks(Int32 profile)
    {
        var trust = new Trust();
        Assert.That(() => RoamingNetworkHistory.ParseCBOR(Bytes(profile), trust.Batch, trust.Commit), Throws.Exception);
        Assert.That(trust.Calls, Is.Zero);
    }

    [TestCase(3)] [TestCase(4)]
    public void Boundary_recovery_requires_a_commit_verifier_before_callbacks(Int32 profile)
    {
        var trust = new Trust();
        Assert.That(() => RoamingNetworkHistory.ParseCBOR(Bytes(profile), trust.Batch, authorizeSnapshotBoundary: trust.Boundary), Throws.Exception);
        Assert.That(trust.Calls, Is.Zero);
    }

    [TestCase(3)] [TestCase(4)]
    public void A_rejected_boundary_is_not_returned_as_a_recovered_history(Int32 profile)
    {
        var trust = new Trust();
        Assert.That(() => RoamingNetworkHistory.ParseCBOR(Bytes(profile), trust.Batch, trust.Commit,
            authorizeSnapshotBoundary: _ => false), Throws.Exception);
        Assert.That(trust.BatchCalls, Is.Zero);
    }
}
