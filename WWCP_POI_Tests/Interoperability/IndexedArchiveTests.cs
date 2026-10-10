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
public sealed class IndexedArchiveTests
{
    private static readonly String[] Names = ["history", "snapshot-complete-history", "snapshot-boundary-history", "pruned-history"];
    private static Byte[] Bytes(Int32 profile) => File.ReadAllBytes(FilePath(Names[profile - 1] + ".cbor"));
    private static CBORValue Replace(CBORValue map, String name, CBORValue value)
        => CBORValue.FromMap(map.AsMap().Select(field => new KeyValuePair<CBORValue, CBORValue>(field.Key,
            RoamingNetworkCommit.Text(field.Key) == name ? value : field.Value)));

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Indexed_ranges_match_every_original_commit_receipt_and_root_field(Int32 profile)
    {
        var bytes = Bytes(profile); var tree = CBORValue.Parse(bytes); var index = POIArchiveCBORIndex.Read(bytes);
        foreach (var field in tree.AsMap())
            Assert.That(index.Value(bytes, RoamingNetworkCommit.Text(field.Key)), Is.EqualTo(field.Value));
        var commits = tree.AsMap().Single(field => RoamingNetworkCommit.Text(field.Key) == "Commits").Value.AsArray();
        Assert.That(index.Commits.Length, Is.EqualTo(commits.Count));
        for (var i = 0; i < commits.Count; i++)
            Assert.That(CBORValue.Parse(POIArchiveCBORIndex.Slice(bytes, index.Commits[i])), Is.EqualTo(commits[i]));
        var receipts = profile == 4 ? tree.AsMap().Single(field => RoamingNetworkCommit.Text(field.Key) == "RetentionReceipts").Value.AsArray() : [];
        Assert.That(index.Receipts.Length, Is.EqualTo(receipts.Count));
        for (var i = 0; i < receipts.Count; i++)
            Assert.That(CBORValue.Parse(POIArchiveCBORIndex.Slice(bytes, index.Receipts[i])), Is.EqualTo(receipts[i]));
        POIArchiveCBORIndex.RequireCanonical(bytes);
    }

    [Test, Combinatorial]
    public void Duplicate_nested_keys_are_rejected_before_trust(
        [Values(1, 2, 3, 4)] Int32 profile, [Values(0, 1, 2, 3, 4, 5)] Int32 kind)
    {
        var key = kind switch {
            0 => CBORValue.FromText("key"), 1 => CBORValue.FromUInt64(UInt64.MaxValue),
            2 => CBORValue.FromBytes([0, 1]), 3 => CBORValue.FromArray([CBORValue.FromText("x")]),
            4 => CBORValue.FromMap([new(CBORValue.FromText("x"), CBORValue.Null)]),
            _ => CBORValue.Tagged(new CBORTag(2000), CBORValue.FromText("x")) };
        var duplicate = CBORValue.FromMap([new(key, CBORValue.Null), new(key, CBORValue.True)]);
        var root = CBORValue.Parse(Bytes(profile));
        var invalid = CBORValue.FromMap(root.AsMap().Append(new(CBORValue.FromText("Opaque"), duplicate))).ToByteArray();
        var calls = 0;
        Assert.Throws<CBORException>(() => RoamingNetworkHistory.ParseCBOR(invalid,
            (batch, peer) => { calls++; return VerifyBatch(batch, peer); },
            (commit, peer) => { calls++; return VerifyCommit(commit, peer); },
            authorizeSnapshotBoundary: _ => { calls++; return true; }));
        Assert.That(calls, Is.Zero);
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Duplicate_late_commit_fields_reject_before_root_trust(Int32 profile)
    {
        var root = CBORValue.Parse(Bytes(profile));
        var commits = root.AsMap().Single(field => RoamingNetworkCommit.Text(field.Key) == "Commits").Value.AsArray();
        var last = commits[^1]; var first = last.AsMap()[0];
        var duplicate = CBORValue.FromMap(last.AsMap().Append(first));
        var invalid = Replace(root, "Commits", CBORValue.FromArray(commits.Take(commits.Count - 1).Append(duplicate))).ToByteArray();
        var calls = 0;
        Assert.Throws<CBORException>(() => RoamingNetworkHistory.ParseCBOR(invalid, VerifyBatch,
            (commit, peer) => { calls++; return VerifyCommit(commit, peer); }, authorizeSnapshotBoundary: _ => true));
        Assert.That(calls, Is.Zero);
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Callback_mutation_of_input_cannot_change_validated_suffix_models(Int32 profile)
    {
        var original = Bytes(profile); var input = original.ToArray(); var calls = 0;
        using var restored = RoamingNetworkHistory.ParseCBOR(input, VerifyBatch,
            (commit, peer) => { calls++; Array.Fill(input, (Byte)0xff); return VerifyCommit(commit, peer); },
            authorizeSnapshotBoundary: _ => true);
        Assert.That(calls, Is.GreaterThan(0));
        Assert.That(input, Is.Not.EqualTo(original));
        Assert.That(restored.ToCBOR(), Is.EqualTo(original));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Bootstrap_callbacks_cannot_mutate_pending_input_commits(Int32 profile)
    {
        var original = Bytes(profile);
        using var source = RoamingNetworkHistory.ParseCBOR(original, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        var manifest = source.CreateBootstrap().Manifest;
        var input = original.ToArray(); var calls = 0;
        using var restored = RoamingNetworkHistory.RestoreBootstrap(input, manifest, VerifyBatch,
            (commit, peer) => { calls++; Array.Fill(input, (Byte)0xff); return VerifyCommit(commit, peer); },
            null, _ => true, new());
        Assert.That(calls, Is.GreaterThan(0)); Assert.That(restored.ToCBOR(), Is.EqualTo(original));
    }

    [TestCase(1)] [TestCase(2)] [TestCase(3)] [TestCase(4)]
    public void Bootstrap_rejects_noncanonical_root_order_before_trust(Int32 profile)
    {
        var original = Bytes(profile);
        using var source = RoamingNetworkHistory.ParseCBOR(original, VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: _ => true);
        var manifest = source.CreateBootstrap().Manifest;
        var reversed = CBORValue.FromMap(CBORValue.Parse(original).AsMap().Reverse()).ToByteArray();
        Assert.That(reversed, Is.Not.EqualTo(original)); var calls = 0;
        Assert.Throws<ArgumentException>(() => RoamingNetworkHistory.RestoreBootstrap(reversed, manifest,
            VerifyBatch, (commit, peer) => { calls++; return VerifyCommit(commit, peer); }, null, _ => true, new()));
        Assert.That(calls, Is.Zero);
    }

    private static IEnumerable<TestCaseData> RawValues()
    {
        Byte[][] items = [
            [0x01], [0x18, 0x01], [0x1b, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff],
            [0x3b, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff], [0xf4], [0xf6], [0xf7], [0xe1],
            [0xf9, 0x3e, 0x00], [0xfa, 0x3f, 0xc0, 0x00, 0x00],
            [0xf9, 0x7e, 0x01], [0xf9, 0x7e, 0x00], [0xf9, 0x80, 0x00],
            [0x61, 0xff], [0x7f, 0x61, 0xc3, 0x61, 0xa9, 0xff],
            [0x7f, 0x61, 0x61, 0x61, 0x62, 0xff], [0x5f, 0x41, 0x00, 0xff],
            [0xc2, 0x42, 0x00, 0x01], [0xd9, 0x07, 0xd0, 0xf6], [0xbf, 0x61, 0x61, 0xf6, 0xff],
            [0xa2, 0x01, 0xf6, 0x18, 0x01, 0xf6], [0xa2, 0x61, 0x62, 0xf6, 0x61, 0x61, 0xf6],
            [0xa2, 0x81, 0x01, 0xf6, 0x81, 0x01, 0xf6], [0x9f, 0x01, 0xff], [0xf8, 0x18], [0xff]
        ];
        for (var i = 0; i < items.Length; i++) yield return new TestCaseData(items[i]).SetName($"Styx_value_and_canonical_oracle_{i}");
        foreach (var depth in new[] { 62, 63, 64, 65 })
        {
            yield return new TestCaseData(Enumerable.Repeat((Byte)0x81, depth - 1).Append((Byte)0x80).ToArray()).SetName($"Empty_array_depth_{depth}");
            yield return new TestCaseData(Enumerable.Repeat((Byte)0xc0, depth).Append((Byte)0xf6).ToArray()).SetName($"Tag_depth_{depth}");
        }
    }

    [TestCaseSource(nameof(RawValues))]
    public void Value_validation_and_canonical_checks_match_the_actual_styx_tree_oracle(Byte[] raw)
    {
        Byte[] bytes = [0xa1, 0x61, 0x78, .. raw];
        CBORValue tree;
        try { tree = CBORValue.Parse(bytes); }
        catch (CBORException)
        { Assert.Throws<CBORException>(() => POIArchiveCBORIndex.Read(bytes)); return; }
        var index = POIArchiveCBORIndex.Read(bytes);
        Assert.That(index.Value(bytes, "x"), Is.EqualTo(tree.AsMap()[0].Value));
        var canonical = tree.ToByteArray(CBORWriterOptions.Canonical).AsSpan().SequenceEqual(bytes);
        if (canonical) Assert.DoesNotThrow(() => POIArchiveCBORIndex.RequireCanonical(bytes));
        else Assert.Throws<ArgumentException>(() => POIArchiveCBORIndex.RequireCanonical(bytes));
    }
}
