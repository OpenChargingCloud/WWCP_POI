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

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ReplicationTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void Count_bounded_pages_include_all_merge_parents_keep_a_fixed_tip_and_do_not_publish(Boolean cbor)
    {
        using var sender = History(); using var receiver = History();
        var root = sender.Head.Id;
        Store(sender, Sign(sender.Head.Commit));
        var left = Prepare(sender, root, "left", Power("150 kW"));
        var right = Prepare(sender, root, "right", Rename("Right branch"));
        Store(sender, left); Store(sender, right);
        var merge = Merge(sender, left, right);
        Publish(sender, left); Publish(sender, merge);
        Status(receiver, EvseTarget, "charging");
        var localNetwork = receiver.Head.Network;
        var seen = new HashSet<RoamingNetworkCommitId> { root };
        var pages = 0;
        RoamingNetworkCommitPack page;
        do
        {
            Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), merge.Id, out var outgoing, out var export,
                maxCommits: 1), Is.True, export.Error);
            page = Transport(outgoing!, cbor);
            Assert.That(page.Commits, Has.Length.EqualTo(1));
            Assert.That(page.Tip, Is.EqualTo(merge.Id));
            Assert.That(page.Commits[0].Parents.All(seen.Contains), Is.True);
            Assert.That(receiver.TryImportCommitPack(page, out var import, maxCommits: 1), Is.True, import.Error);
            Assert.That(import.StoredCount, Is.EqualTo(1));
            Assert.That(import.DuplicateCount, Is.Zero);
            Assert.That(receiver.Head.Id, Is.EqualTo(root));
            Assert.That(receiver.Head.Network, Is.SameAs(localNetwork));
            Assert.That(localNetwork.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
            seen.Add(page.Commits[0].Id);
            if (++pages == 1)
            {
                var later = Prepare(sender, merge.Id, "new-sender-head", Power("175 kW"));
                Publish(sender, later); // The pinned transfer must still finish at merge.
            }
            Assert.That(pages, Is.LessThanOrEqualTo(3));
        }
        while (!page.Complete);
        Assert.That(pages, Is.EqualTo(3));
        Assert.That(seen, Is.EquivalentTo(new[] { root, left.Id, right.Id, merge.Id }));
        Assert.That(receiver.GetSnapshot(merge.Id).ETags, Is.EqualTo(merge.StateETags));
        Assert.That(receiver.Commits, Has.Length.EqualTo(4));
        Assert.That(receiver.Commits.Single(commit => commit.Id == root).Signatures, Has.Length.EqualTo(2));
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), merge.Id, out var empty, out _), Is.True);
        Assert.That(empty!.Complete, Is.True); Assert.That(empty.Commits, Is.Empty);
        Assert.That(receiver.TryImportCommitPack(Transport(empty, cbor), out var repeated), Is.True, repeated.Error);
        Assert.That(repeated.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.AlreadyStored));
        Assert.That(repeated.StoredCount, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Inventory_frontier_acknowledges_all_parents_and_ignores_unknown_receiver_branches(Boolean cbor)
    {
        using var sender = History(); using var receiver = History();
        var root = sender.Head.Id;
        var left = Prepare(sender, root, "left", Power("150 kW"));
        var right = Prepare(sender, root, "right", Rename("Right"));
        Store(sender, left); Store(sender, right); Store(receiver, right);
        var unknown = Prepare(receiver, root, "receiver-only", Power("200 kW")); Store(receiver, unknown);
        var merge = Merge(sender, left, right); Store(sender, merge);
        var announced = sender.GetReplicationState();
        Assert.That(announced.Head, Is.EqualTo(root));
        Assert.That(announced.KnownTips, Is.EqualTo(new[] { merge.Id }));
        var state = receiver.GetReplicationState();
        state = cbor ? RoamingNetworkReplicationState.ParseCBOR(state.ToCBOR()) : RoamingNetworkReplicationState.Parse(state.ToJSON());
        Assert.That(state.KnownTips, Is.EquivalentTo(new[] { right.Id, unknown.Id }));
        Assert.That(sender.TryCreateCommitPack(state, merge.Id, out var page, out var result), Is.True, result.Error);
        Assert.That(page!.Commits.Select(commit => commit.Id), Is.EqualTo(new[] { left.Id, merge.Id }));
        Assert.That(receiver.TryImportCommitPack(Transport(page, cbor), out result), Is.True, result.Error);
        Assert.That(receiver.GetReplicationState().KnownTips, Is.EquivalentTo(new[] { merge.Id, unknown.Id }));
        Assert.That(receiver.Head.Id, Is.EqualTo(root));
    }

    [Test]
    public void Byte_bounds_accept_the_exact_envelope_and_reject_one_byte_less()
    {
        using var sender = History(); using var receiver = History();
        var root = sender.Head.Id;
        var first = Prepare(sender, root, "first", Power("150 kW")); Store(sender, first);
        var single = new RoamingNetworkCommitPack(sender.Head.Commit, first.Id, true, [first]);
        var bound = Size(single);
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), first.Id, out var page, out var result,
            maxBytes: bound), Is.True, result.Error);
        Assert.That(Size(page!), Is.EqualTo(bound));
        Assert.That(Encoding.UTF8.GetByteCount(page!.ToJSON()), Is.LessThanOrEqualTo(bound));
        Assert.That(page.ToCBOR().Length, Is.LessThanOrEqualTo(bound));
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), first.Id, out page, out result,
            maxBytes: bound - 1), Is.False);
        Assert.That(page, Is.Null);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.CommitTooLarge));
        Assert.That(result.MissingCommits, Is.EqualTo(new[] { first.Id }));
        var before = new Observation(receiver);
        Assert.That(receiver.TryImportCommitPack(single, out result, maxBytes: bound - 1), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.CommitTooLarge));
        before.AssertUnchanged(receiver);
        Assert.That(receiver.TryImportCommitPack(single, out result, maxBytes: bound), Is.True, result.Error);
    }

    [Test]
    public void Byte_bounded_prefix_makes_progress_and_header_only_limits_are_enforced()
    {
        using var sender = History(); using var receiver = History();
        var first = Prepare(sender, sender.Head.Id, "first", Power("150 kW")); Store(sender, first);
        var second = Prepare(sender, first.Id, "second", Power("175 kW")); Store(sender, second);
        var prefix = new RoamingNetworkCommitPack(sender.Head.Commit, second.Id, false, [first]);
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), second.Id, out var page, out var result,
            maxBytes: Size(prefix)), Is.True, result.Error);
        Assert.That(page!.Complete, Is.False);
        Assert.That(page.Commits.Select(commit => commit.Id), Is.EqualTo(new[] { first.Id }));
        var empty = new RoamingNetworkCommitPack(sender.Head.Commit, sender.Head.Id, true, []);
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), sender.Head.Id, out page, out result,
            maxBytes: Size(empty)), Is.True, result.Error);
        Assert.That(page!.Commits, Is.Empty);
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), sender.Head.Id, out page, out result,
            maxBytes: Size(empty) - 1), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.CommitTooLarge));
        Assert.That(result.MissingCommits, Is.Empty);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Wire_parsers_enforce_raw_byte_and_item_counts_at_exact_boundaries(Boolean cbor)
    {
        using var history = History();
        var left = Prepare(history, history.Head.Id, "left", Power("150 kW"));
        var right = Prepare(history, history.Head.Id, "right", Rename("Right"));
        Store(history, left); Store(history, right);
        var pack = new RoamingNetworkCommitPack(history.Head.Commit, right.Id, true, [left, right]);
        var state = history.GetReplicationState();
        var size = cbor ? pack.ToCBOR().Length : Encoding.UTF8.GetByteCount(pack.ToJSON());
        var stateSize = cbor ? state.ToCBOR().Length : Encoding.UTF8.GetByteCount(state.ToJSON());
        RoamingNetworkCommitPack ParsePack(Int32 count, Int32 bytes) => cbor ?
            RoamingNetworkCommitPack.ParseCBOR(pack.ToCBOR(), count, bytes) : RoamingNetworkCommitPack.Parse(pack.ToJSON(), count, bytes);
        RoamingNetworkReplicationState ParseState(Int32 count, Int32 bytes) => cbor ?
            RoamingNetworkReplicationState.ParseCBOR(state.ToCBOR(), count, bytes) : RoamingNetworkReplicationState.Parse(state.ToJSON(), count, bytes);
        Assert.That(ParsePack(2, size).Commits, Has.Length.EqualTo(2));
        Assert.That(() => ParsePack(1, size), Throws.ArgumentException);
        Assert.That(() => ParsePack(2, size - 1), Throws.ArgumentException);
        Assert.That(ParseState(2, stateSize).KnownTips, Is.EqualTo(state.KnownTips));
        Assert.That(() => ParseState(1, stateSize), Throws.ArgumentException);
        Assert.That(() => ParseState(2, stateSize - 1), Throws.ArgumentException);
        var before = new Observation(history);
        Assert.That(history.TryImportCommitPack(pack, out var result, maxCommits: 1), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.CommitTooLarge));
        before.AssertUnchanged(history);
    }

    [TestCase(0, 1024)]
    [TestCase(-1, 1024)]
    [TestCase(1, 0)]
    [TestCase(1, -1)]
    public void Nonpositive_limits_are_rejected_without_mutation(Int32 count, Int32 bytes)
    {
        using var history = History();
        var page = new RoamingNetworkCommitPack(history.Head.Commit, history.Head.Id, true, []);
        var before = new Observation(history);
        Assert.That(history.TryCreateCommitPack(history.GetReplicationState(), history.Head.Id, out var pack, out var export,
            count, bytes), Is.False);
        Assert.That(pack, Is.Null); Assert.That(export.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.InvalidInput));
        Assert.That(history.TryImportCommitPack(page, out var import, count, bytes), Is.False);
        Assert.That(import.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.InvalidInput));
        Assert.That(() => RoamingNetworkCommitPack.Parse(page.ToJSON(), count, bytes), Throws.TypeOf<ArgumentOutOfRangeException>());
        Assert.That(() => RoamingNetworkReplicationState.Parse(history.GetReplicationState().ToJSON(), count, bytes), Throws.TypeOf<ArgumentOutOfRangeException>());
        before.AssertUnchanged(history);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Different_checkpoints_and_unknown_tips_do_not_change_either_history(Boolean cbor)
    {
        using var sender = History();
        var json = Network().DataSnapshot.ToJSON(); json["name"] = new Newtonsoft.Json.Linq.JObject(new Newtonsoft.Json.Linq.JProperty("en", "Other checkpoint"));
        using var receiver = History(RoamingNetwork.Parse(json));
        var before = new Observation(receiver);
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), sender.Head.Id, out var pack, out var export), Is.False);
        Assert.That(pack, Is.Null); Assert.That(export.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.CheckpointMismatch));
        var page = new RoamingNetworkCommitPack(Sign(sender.Head.Commit), sender.Head.Id, true, []);
        Assert.That(receiver.TryImportCommitPack(Transport(page, cbor), out var import), Is.False);
        Assert.That(import.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.CheckpointMismatch));
        before.AssertUnchanged(receiver);
        Assert.That(sender.TryCreateCommitPack(sender.GetReplicationState(), receiver.Head.Id, out _, out export), Is.False);
        Assert.That(export.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.UnknownTip));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Missing_and_out_of_order_parents_are_atomic_and_retry_can_complete(Boolean cbor)
    {
        using var sender = History(); using var receiver = History();
        var first = Prepare(sender, sender.Head.Id, "first", Power("150 kW")); Store(sender, first);
        var second = Prepare(sender, first.Id, "second", Power("175 kW")); Store(sender, second);
        var before = new Observation(receiver);
        var missing = Transport(new(Sign(sender.Head.Commit), second.Id, true, [second]), cbor);
        Assert.That(receiver.TryImportCommitPack(missing, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.MissingParents));
        Assert.That(result.MissingCommits, Is.EqualTo(new[] { first.Id }));
        before.AssertUnchanged(receiver);
        var reversed = Transport(new(Sign(sender.Head.Commit), second.Id, true, [second, first]), cbor);
        Assert.That(receiver.TryImportCommitPack(reversed, out result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.InvalidInput));
        before.AssertUnchanged(receiver);
        var falseCompletion = Transport(new(sender.Head.Commit, second.Id, true, []), cbor);
        Assert.That(receiver.TryImportCommitPack(falseCompletion, out result), Is.False);
        Assert.That(result.MissingCommits, Is.EqualTo(new[] { second.Id }));
        before.AssertUnchanged(receiver);
        Assert.That(receiver.TryImportCommitPack(Transport(new(sender.Head.Commit, second.Id, true, [first, second]), cbor), out result), Is.True, result.Error);
        Assert.That(result.StoredCount, Is.EqualTo(2));
        // Parents supplied in the message must precede children even if already retained.
        var retained = new Observation(receiver);
        Assert.That(receiver.TryImportCommitPack(reversed, out result), Is.False);
        retained.AssertUnchanged(receiver);
    }

    [TestCase("ResultETags", false)]
    [TestCase("ResultETags", true)]
    [TestCase("OperationPrecondition", false)]
    [TestCase("OperationPrecondition", true)]
    [TestCase("BatchSignature", false)]
    [TestCase("BatchSignature", true)]
    [TestCase("CommitSignature", false)]
    [TestCase("CommitSignature", true)]
    [TestCase("Authorization", false)]
    [TestCase("Authorization", true)]
    [TestCase("BatchId", false)]
    [TestCase("BatchId", true)]
    public void A_later_invalid_commit_rolls_back_new_states_peer_unions_and_batch_ids(String failure, Boolean cbor)
    {
        using var sender = History();
        using var receiver = new RoamingNetworkHistory(Network(), VerifyBatch, VerifyCommit,
            commit => failure != "Authorization" || commit.ChangeSet?.Id != "second");
        var known = Prepare(sender, sender.Head.Id, "known", Rename("Known")); Store(sender, known);
        var onePeer = known.WithSignatures([known.Signatures[0]])
            .WithChangeSet(known.ChangeSet!.WithSignatures([known.ChangeSet.Signatures[0]]));
        Store(receiver, onePeer);
        var first = Prepare(sender, known.Id, "first", Power("150 kW")); Store(sender, first);
        var source = sender.GetSnapshot(first.Id);
        var valid = Batch(source, "second", Power("175 kW"));
        var badBatch = failure switch {
            "ResultETags" => new RoamingNetworkChangeSet(valid.Id, valid.RoamingNetworkId, valid.BaseRevision,
                valid.CreatedAt, valid.Changes, valid.BeforeETags, valid.BeforeETags),
            "OperationPrecondition" => new RoamingNetworkChangeSet(valid.Id, valid.RoamingNetworkId, valid.BaseRevision,
                valid.CreatedAt, [RoamingNetworkChange.UpdateProperty("EVSE", "DE*ABC*E1", "maxPower", Value("\"777 kW\""), Value("\"175 kW\""))],
                valid.BeforeETags, valid.AfterETags),
            "BatchId" => new RoamingNetworkChangeSet("first", valid.RoamingNetworkId, valid.BaseRevision,
                valid.CreatedAt, valid.Changes, valid.BeforeETags, valid.AfterETags),
            _ => valid
        };
        badBatch = Sign(badBatch);
        if (failure == "BatchSignature") badBatch = badBatch.WithSignature(new("Ed25519", "fixture-alice", Convert.ToBase64String(new Byte[64])));
        var bad = Sign(RoamingNetworkCommit.Create(first, badBatch));
        if (failure == "CommitSignature") bad = bad.WithSignature(new("Ed25519", "fixture-alice", Convert.ToBase64String(new Byte[64]), RoamingNetworkCommit.SigningProfile));
        var page = Transport(new(Sign(sender.Head.Commit), bad.Id, true, [known, first, bad]), cbor);
        Status(receiver, EvseTarget, "charging");
        var before = new Observation(receiver);
        Assert.That(receiver.TryImportCommitPack(page, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(failure == "BatchId" ? RoamingNetworkReplicationOutcome.ChangeSetIdConflict : RoamingNetworkReplicationOutcome.InvalidInput));
        Assert.That(result.StoredCount, Is.Zero); Assert.That(result.DuplicateCount, Is.Zero);
        before.AssertUnchanged(receiver);
        Assert.That(receiver.Commits.Single(commit => commit.Id == known.Id).Signatures, Has.Length.EqualTo(1));
        Assert.That(receiver.Commits.Single(commit => commit.Id == sender.Head.Id).Signatures, Is.Empty);
        Assert.That(() => receiver.GetSnapshot(first.Id), Throws.TypeOf<KeyNotFoundException>());
        var retry = Prepare(sender, first.Id, "retry", Power("175 kW"));
        Assert.That(receiver.TryImportCommitPack(Transport(new(Sign(sender.Head.Commit), retry.Id, true, [known, first, retry]), cbor), out result), Is.True, result.Error);
        Assert.That(result.StoredCount, Is.EqualTo(2)); Assert.That(result.DuplicateCount, Is.EqualTo(1));
        Assert.That(receiver.Commits.Single(commit => commit.Id == known.Id).Signatures, Has.Length.EqualTo(2));
        Assert.That(receiver.Commits.Single(commit => commit.Id == sender.Head.Id).Signatures, Has.Length.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Repeated_pages_add_peers_idempotently_without_changing_commit_identity_or_runtime(Boolean cbor)
    {
        using var sender = History(); using var receiver = History();
        var original = Prepare(sender, sender.Head.Id, "peers", Power("150 kW"));
        var one = original.WithSignatures([original.Signatures[0]])
            .WithChangeSet(original.ChangeSet!.WithSignatures([original.ChangeSet.Signatures[0]]));
        Publish(receiver, one);
        Status(receiver, EvseTarget, "charging");
        var network = receiver.Head.Network;
        var page = Transport(new(Sign(sender.Head.Commit), original.Id, true, [original]), cbor);
        for (var delivery = 0; delivery < 2; delivery++)
        {
            Assert.That(receiver.TryImportCommitPack(page, out var result), Is.True, result.Error);
            Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.AlreadyStored));
            Assert.That(result.StoredCount, Is.Zero); Assert.That(result.DuplicateCount, Is.EqualTo(1));
            Assert.That(receiver.Head.Id, Is.EqualTo(original.Id)); Assert.That(receiver.Head.Network, Is.SameAs(network));
            Assert.That(receiver.Head.Commit.Signatures, Has.Length.EqualTo(2));
            Assert.That(receiver.Head.Commit.ChangeSet!.Signatures, Has.Length.EqualTo(2));
            Assert.That(network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("charging"));
        }
    }

    [Test]
    public void Changed_trust_rechecks_stored_ancestry_before_importing_a_descendant()
    {
        var accept = true;
        using var sender = History();
        using var receiver = new RoamingNetworkHistory(Network(), VerifyBatch, VerifyCommit, commit => accept || commit.ChangeSet?.Id != "first");
        var first = Prepare(sender, sender.Head.Id, "first", Power("150 kW")); Store(sender, first); Store(receiver, first);
        var second = Prepare(sender, first.Id, "second", Rename("Second"));
        var page = new RoamingNetworkCommitPack(sender.Head.Commit, second.Id, true, [second]);
        accept = false;
        var before = new Observation(receiver);
        Assert.That(receiver.TryImportCommitPack(page, out var result), Is.False);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.InvalidInput));
        before.AssertUnchanged(receiver);
        accept = true;
        Assert.That(receiver.TryImportCommitPack(page, out result), Is.True, result.Error);
    }

    [Test]
    public void An_indivisible_oversized_child_is_reported_after_the_valid_prefix_arrives()
    {
        using var sender = History(); using var receiver = History();
        var first = Prepare(sender, sender.Head.Id, "first", Power("150 kW")); Store(sender, first);
        var batch = Sign(Batch(sender.GetSnapshot(first.Id), "large-child", Rename("Large"))
            .WithMetadata("vendor:payload", new String('x', 8192)));
        var child = Sign(sender.PrepareCommit(first.Id, batch)); Store(sender, child);
        var bound = Size(new(sender.Head.Commit, child.Id, false, [first]));
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), child.Id, out var prefix, out var result,
            maxBytes: bound), Is.True, result.Error);
        Assert.That(prefix!.Complete, Is.False); Assert.That(prefix.Commits.Single().Id, Is.EqualTo(first.Id));
        Assert.That(receiver.TryImportCommitPack(prefix, out result, maxBytes: bound), Is.True, result.Error);
        var before = new Observation(receiver);
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), child.Id, out var oversized, out result,
            maxBytes: bound), Is.False);
        Assert.That(oversized, Is.Null); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkReplicationOutcome.CommitTooLarge));
        Assert.That(result.MissingCommits, Is.EqualTo(new[] { child.Id })); before.AssertUnchanged(receiver);
        var expanded = Size(new(sender.Head.Commit, child.Id, true, [child]));
        Assert.That(sender.TryCreateCommitPack(receiver.GetReplicationState(), child.Id, out var last, out result,
            maxBytes: expanded), Is.True, result.Error);
        Assert.That(last!.Complete, Is.True);
        Assert.That(receiver.TryImportCommitPack(last, out result, maxBytes: expanded), Is.True, result.Error);
    }

    [Test]
    public void Raw_JSON_limits_count_UTF8_bytes_instead_of_UTF16_characters()
    {
        using var sender = History();
        var batch = Sign(Batch(sender.Head.Snapshot, "unicode", Power("150 kW"))
            .WithDescription("de", "Übergrößen ⚡"));
        var commit = Sign(sender.PrepareCommit(sender.Head.Id, batch));
        var page = new RoamingNetworkCommitPack(sender.Head.Commit, commit.Id, true, [commit]);
        var json = page.ToJSON().Replace("\\u00DC", "Ü").Replace("\\u00F6", "ö").Replace("\\u00DF", "ß").Replace("\\u26A1", "⚡");
        var bytes = Encoding.UTF8.GetByteCount(json);
        Assert.That(bytes, Is.GreaterThan(json.Length));
        Assert.That(RoamingNetworkCommitPack.Parse(json, maxBytes: bytes).Commits.Single().Id, Is.EqualTo(commit.Id));
        Assert.That(() => RoamingNetworkCommitPack.Parse(json, maxBytes: bytes - 1), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkCommitPack.Parse(json, maxBytes: json.Length), Throws.ArgumentException);
    }

    [Test]
    public void Wire_contract_rejects_unknown_profiles_duplicate_fields_trailing_input_and_incomplete_empty_pages()
    {
        using var history = History();
        var page = new RoamingNetworkCommitPack(history.Head.Commit, history.Head.Id, true, []);
        var json = page.ToJSON(); var state = history.GetReplicationState().ToJSON();
        Assert.That(() => RoamingNetworkCommitPack.Parse(json.Replace(RoamingNetworkCommitPack.Profile, "unknown")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkCommitPack.Parse(json.Replace(POIContentProfile.Id, "unknown")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkCommitPack.Parse(json.Replace("\"Complete\":true", "\"Complete\":true,\"Complete\":false")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkReplicationState.Parse(state.Replace(RoamingNetworkReplicationState.Profile, "unknown")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkReplicationState.Parse(state.Replace(POIContentProfile.Id, "unknown")), Throws.ArgumentException);
        Assert.That(() => RoamingNetworkCommitPack.Parse(json + "{}"), Throws.InstanceOf<JsonException>());
        Assert.That(() => RoamingNetworkCommitPack.ParseCBOR([.. page.ToCBOR(), 0x00]), Throws.Exception);
        Assert.That(() => new RoamingNetworkCommitPack(history.Head.Commit, history.Head.Id, false, []), Throws.ArgumentException);
        Assert.That(() => new RoamingNetworkReplicationState(history.Head.Id, history.Head.Id, []), Throws.ArgumentException);
        Assert.That(() => new RoamingNetworkReplicationState(history.Head.Id, history.Head.Id, [history.Head.Id, history.Head.Id]), Throws.ArgumentException);
    }
}
