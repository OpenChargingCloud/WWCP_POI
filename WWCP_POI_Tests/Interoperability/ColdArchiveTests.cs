/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Text.Json;
using System.Text.Json.Nodes;
using cloud.charging.open.protocols.WWCP.POI;
using NUnit.Framework;
using org.GraphDefined.Vanaheimr.Illias;
using static WWCP_POI_Tests.Interoperability.InteropFixture;
using static WWCP_POI_Tests.Interoperability.ReplicationTestSupport;
using static WWCP_POI_Tests.Interoperability.SnapshotTestSupport;

namespace WWCP_POI_Tests.Interoperability;

[TestFixture]
public sealed class ColdArchiveTests
{
    private static readonly String[] Profiles = ["Complete", "Boundary", "Repeated"];
    private static RoamingNetworkCommitId Unknown => new(ETag.Compute(ETagFormat.JSON, [99]));

    private sealed class Scenario : IDisposable
    {
        internal readonly ArchiveDirectory Cold = new();
        internal readonly ArchiveDirectory FirstCold = new();
        internal readonly ArchiveDirectory SecondCold = new();
        internal readonly RoamingNetworkHistory Active;
        internal readonly RoamingNetworkRetentionReceipt Receipt;
        internal readonly Byte[] Bytes;
        internal readonly RoamingNetworkCommitId PrunedId;
        internal readonly RoamingNetworkCommitId Root;
        internal readonly Observation Before;

        internal Scenario(String profile)
        {
            Active = History(); Root = Active.CheckpointId; var snapshot = LinearHistory(Active);
            if (profile == "Boundary")
            {
                Active.Dispose();
                Active = RoamingNetworkHistory.FromSnapshot(Root, snapshot, boundary => boundary.Checkpoint == Root,
                    VerifyCommit, VerifyBatch);
            }
            if (profile == "Repeated")
            {
                Prune(Active, Plan(Active, snapshot), FirstCold.ArchivePath);
                Publish(Active, Prepare(Active, Active.Head.Id, "between-first-second", Power("175 kW")));
                snapshot = Snapshot(Active, 11); Publish(Active, snapshot);
                Publish(Active, Prepare(Active, Active.Head.Id, "after-second", Rename("Second")));
                Prune(Active, Plan(Active, snapshot), SecondCold.ArchivePath);
            }
            if (profile != "Complete")
            {
                Publish(Active, Prepare(Active, Active.Head.Id, "before-final", Power("200 kW")));
                snapshot = Snapshot(Active, 12); Publish(Active, snapshot);
                Publish(Active, Prepare(Active, Active.Head.Id, "after-final", Rename("Final")));
            }
            // Preserve an unpublished signed branch in the cold archive as well.
            var released = Prepare(Active, Active.AnchorId, "cold-branch", Rename("Cold branch")); Store(Active, released);
            PrunedId = released.Id; Bytes = Active.ToCBOR();
            Receipt = Prune(Active, Plan(Active, snapshot, [released.Id]), Cold.ArchivePath).Receipt!;
            Status(Active, EvseTarget, "charging"); Before = new(Active);
        }

        internal Boolean Read(RoamingNetworkColdArchiveCatalog catalog, out RoamingNetworkHistory? history,
            out RoamingNetworkColdArchiveResult result, RoamingNetworkCommitId? requested = null, RoamingNetworkHistoryLimits? limits = null)
            => RoamingNetworkHistory.TryReadColdArchive(Receipt, catalog, out history, out result, VerifyBatch, VerifyCommit,
                authorizeCommit: commit => commit.Signatures.Length == 2, authorizeSnapshotBoundary: AcceptChain(Active),
                limits: limits, requestedCommit: requested, authorizeReceipt: receipt => receipt.Id == Receipt.Id);

        internal void Verify(RoamingNetworkHistory recovered)
        {
            Assert.That(recovered.ToCBOR(), Is.EqualTo(Bytes)); Assert.That(recovered.Head.Id, Is.EqualTo(Receipt.Head));
            Assert.That(recovered.CheckpointId, Is.EqualTo(Receipt.Checkpoint)); Assert.That(recovered.AnchorId, Is.EqualTo(Receipt.BeforeAnchor));
            Assert.That(recovered.GetCommit(PrunedId).Id, Is.EqualTo(PrunedId));
            Assert.That(recovered.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("available"));
            Assert.That(recovered.Head.Network.EVSEs.Single().MaxPowerRealTime, Is.Null);
            Assert.That(recovered.Head.Network.EVSEs.Single().MaxPowerPrognoses, Is.Empty);
            foreach (var commit in recovered.Commits)
            {
                Assert.That(commit.Signatures.Select(peer => peer.KeyId), Is.EqualTo(new[] { "fixture-alice", "fixture-bob" }));
                foreach (var peer in commit.Signatures) Assert.That(VerifyCommit(commit, peer), Is.True);
            }
            Before.AssertUnchanged(Active);
        }

        public void Dispose() { Active.Dispose(); Cold.Dispose(); FirstCold.Dispose(); SecondCold.Dispose(); }
    }

    private static RoamingNetworkColdArchiveCatalog Catalog(ETag hash, params String[] paths)
        => new(paths.Select(path => new RoamingNetworkColdArchiveLocation(hash, path)));

    private static RoamingNetworkRetentionReceipt Edit(RoamingNetworkRetentionReceipt receipt, Action<JsonObject> edit)
    {
        var json = JsonNode.Parse(receipt.ToJSON())!.AsObject(); json.Remove("Id"); edit(json);
        using var document = JsonDocument.Parse(json.ToJsonString());
        json["Id"] = JsonNode.Parse(ETag.Compute(ETagFormat.JSON, CanonicalJSON.ToUTF8Bytes(document)).ToJSON().ToString());
        return RoamingNetworkRetentionReceipt.Parse(json.ToJsonString());
    }

    [Test]
    public void Catalog_freezes_normalized_ordered_locations_deduplicates_paths_and_never_reads_or_creates_files()
    {
        using var directory = new ArchiveDirectory(); var hash = ETag.Compute(ETagFormat.CBOR, [1]);
        var first = new RoamingNetworkColdArchiveLocation(hash, Path.Combine(directory.DirectoryPath, ".", "z.cbor"));
        var second = new RoamingNetworkColdArchiveLocation(hash, Path.Combine(directory.DirectoryPath, "a.cbor"));
        var supplied = new List<RoamingNetworkColdArchiveLocation> { first, first, second };
        if (OperatingSystem.IsWindows()) supplied.Add(new(hash, first.Path.ToUpperInvariant()));
        var catalog = new RoamingNetworkColdArchiveCatalog(supplied, maxLocations: supplied.Count); supplied.Clear();
        Assert.That(catalog.Locations, Is.EqualTo(new[] { first, second }));
        Assert.That(catalog.GetCandidates(hash), Is.EqualTo(new[] { first.Path, second.Path }));
        Assert.That(first.Path, Is.EqualTo(Path.Combine(directory.DirectoryPath, "z.cbor")));
        Assert.That(catalog.GetCandidates(ETag.Compute(ETagFormat.CBOR, [2])), Is.Empty);
        Assert.That(Directory.GetFiles(directory.DirectoryPath), Is.Empty);
        var next = new RoamingNetworkColdArchiveCatalog([.. catalog.Locations, new(hash, directory.ArchivePath)]);
        Assert.That(next.Locations, Has.Length.EqualTo(3)); Assert.That(catalog.Locations, Has.Length.EqualTo(2));
        foreach (var type in new[] { typeof(RoamingNetworkColdArchiveCatalog), typeof(RoamingNetworkColdArchiveLocation) })
            Assert.That(type.GetProperties().Where(property => property.SetMethod?.IsPublic == true), Is.Empty);
    }

    [TestCase("default")]
    [TestCase("json")]
    [TestCase("empty-path")]
    [TestCase("blank-path")]
    [TestCase("null-path")]
    [TestCase("null-locations")]
    [TestCase("null-location")]
    [TestCase("zero-budget")]
    [TestCase("negative-budget")]
    public void Catalog_rejects_invalid_claims_and_configuration(String fault)
    {
        var hash = ETag.Compute(ETagFormat.CBOR, [1]);
        Assert.That(() => {
            switch (fault)
            {
                case "default": new RoamingNetworkColdArchiveLocation(default, "missing.cbor"); break;
                case "json": new RoamingNetworkColdArchiveLocation(ETag.Compute(ETagFormat.JSON, [1]), "missing.cbor"); break;
                case "empty-path": new RoamingNetworkColdArchiveLocation(hash, ""); break;
                case "blank-path": new RoamingNetworkColdArchiveLocation(hash, " "); break;
                case "null-path": new RoamingNetworkColdArchiveLocation(hash, null!); break;
                case "null-locations": new RoamingNetworkColdArchiveCatalog(null!); break;
                case "null-location": new RoamingNetworkColdArchiveCatalog([null!]); break;
                case "zero-budget": new RoamingNetworkColdArchiveCatalog([], 0); break;
                case "negative-budget": new RoamingNetworkColdArchiveCatalog([], -1); break;
            }
        }, Throws.InstanceOf<ArgumentException>());
    }

    [Test]
    public void Catalog_counts_duplicate_claims_and_stops_enumerating_at_the_first_excess_location()
    {
        var location = new RoamingNetworkColdArchiveLocation(ETag.Compute(ETagFormat.CBOR, [1]), "missing.cbor"); var visited = 0;
        IEnumerable<RoamingNetworkColdArchiveLocation> Endless()
        {
            while (true) { visited++; yield return location; }
        }
        Assert.That(() => new RoamingNetworkColdArchiveCatalog(Endless(), 2), Throws.ArgumentException);
        Assert.That(visited, Is.EqualTo(3));
        Assert.That(() => new RoamingNetworkColdArchiveCatalog([]).GetCandidates(default), Throws.ArgumentException);
        Assert.That(() => new RoamingNetworkColdArchiveCatalog([]).GetCandidates(ETag.Compute(ETagFormat.JSON, [1])), Throws.ArgumentException);
    }

    private static IEnumerable<TestCaseData> MovedCases()
    {
        foreach (var profile in Profiles) foreach (var cbor in new[] { false, true }) yield return new(profile, cbor);
    }

    [TestCaseSource(nameof(MovedCases))]
    public void Moved_cold_archives_recover_original_branches_and_peers_with_fresh_runtime_and_no_writer_files(String profile, Boolean cbor)
    {
        using var scenario = new Scenario(profile); using var moved = new ArchiveDirectory();
        File.Move(scenario.Cold.ArchivePath, moved.ArchivePath); var oldReceipt = scenario.Receipt.ToCBOR();
        var receipt = cbor ? RoamingNetworkRetentionReceipt.ParseCBOR(oldReceipt) : RoamingNetworkRetentionReceipt.Parse(scenario.Receipt.ToJSON());
        var catalog = Catalog(receipt.SourceArchiveETag, scenario.Cold.ArchivePath, moved.ArchivePath);
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(receipt, catalog, out var recovered, out var result,
            VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AcceptChain(scenario.Active), requestedCommit: scenario.PrunedId), Is.True, result.Error);
        using (recovered!)
        {
            scenario.Verify(recovered!); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkColdArchiveOutcome.Recovered));
            Assert.That(result.Candidates.Select(candidate => candidate.Outcome), Is.EqualTo(new[] {
                RoamingNetworkColdArchiveCandidateOutcome.NotFound, RoamingNetworkColdArchiveCandidateOutcome.DigestMatched }));
            Assert.That(result.ResolvedPath, Is.EqualTo(moved.ArchivePath)); Assert.That(result.CommitLookup!.Commit!.Id, Is.EqualTo(scenario.PrunedId));
            Status(recovered!, EvseTarget, "reserved"); Publish(recovered!, Prepare(recovered!, recovered!.Head.Id, "cold-followup", Power("225 kW")));
            Assert.That(recovered!.Head.Network.EVSEs.Single().Status.Value.ToString(), Is.EqualTo("reserved"));
            Assert.That(File.ReadAllBytes(moved.ArchivePath), Is.EqualTo(scenario.Bytes));
        }
        scenario.Before.AssertUnchanged(scenario.Active); Assert.That(scenario.Receipt.ToCBOR(), Is.EqualTo(oldReceipt));
        Assert.That(Directory.GetFiles(moved.DirectoryPath), Is.EqualTo(new[] { moved.ArchivePath }));
        Assert.That(File.Exists(scenario.Cold.ArchivePath), Is.False);
    }

    private static IEnumerable<TestCaseData> CandidateFailures()
    {
        foreach (var profile in Profiles)
            foreach (var fault in new[] { "NotFound", "DigestMismatch", "ReadFailure", "LimitExceeded" }) yield return new(profile, fault);
    }

    [TestCaseSource(nameof(CandidateFailures))]
    public void Unusable_location_claims_are_diagnosed_before_trying_the_next_matching_copy(String profile, String fault)
    {
        using var scenario = new Scenario(profile); using var bad = new ArchiveDirectory();
        var bytes = fault == "LimitExceeded" ? new Byte[scenario.Bytes.Length + 1] : new Byte[] { 1, 2, 3 };
        if (fault != "NotFound") File.WriteAllBytes(bad.ArchivePath, bytes);
        using var held = fault == "ReadFailure" ? new FileStream(bad.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None) : null;
        Assert.That(scenario.Read(Catalog(scenario.Receipt.SourceArchiveETag, bad.ArchivePath, scenario.Cold.ArchivePath),
            out var recovered, out var result, scenario.PrunedId, new(maxArchiveBytes: scenario.Bytes.Length)), Is.True, result.Error);
        using (recovered!) { scenario.Verify(recovered!); }
        Assert.That(result.Candidates, Has.Length.EqualTo(2));
        Assert.That(result.Candidates[0].Outcome.ToString(), Is.EqualTo(fault));
        Assert.That(result.Candidates[1].Outcome, Is.EqualTo(RoamingNetworkColdArchiveCandidateOutcome.DigestMatched));
        Assert.That(result.ResolvedPath, Is.EqualTo(scenario.Cold.ArchivePath));
        if (fault == "LimitExceeded") Assert.That(result.Candidates[0].LimitViolation, Is.EqualTo(new RoamingNetworkHistoryLimitViolation(
            RoamingNetworkHistoryLimitKind.ArchiveBytes, scenario.Bytes.Length, scenario.Bytes.Length + 1)));
        if (fault == "DigestMismatch") Assert.That(result.Candidates[0].ObservedArchiveETag, Is.EqualTo(ETag.Compute(ETagFormat.CBOR, bytes)));
        held?.Dispose(); if (fault != "NotFound") Assert.That(File.ReadAllBytes(bad.ArchivePath), Is.EqualTo(bytes));
    }

    private static IEnumerable<TestCaseData> MissingCases()
    {
        foreach (var profile in Profiles)
            foreach (var fault in new[] { "Empty", "UnknownDigest", "NotFound", "DigestMismatch", "ReadFailure", "LimitExceeded" }) yield return new(profile, fault);
    }

    [TestCaseSource(nameof(MissingCases))]
    public void Unavailable_candidates_return_structured_diagnostics_without_replay_or_disk_changes(String profile, String fault)
    {
        using var scenario = new Scenario(profile); using var missing = new ArchiveDirectory();
        var bytes = fault == "LimitExceeded" ? new Byte[scenario.Bytes.Length + 1] : new Byte[] { 1, 2, 3 };
        if (fault is "DigestMismatch" or "ReadFailure" or "LimitExceeded") File.WriteAllBytes(missing.ArchivePath, bytes);
        using var held = fault == "ReadFailure" ? new FileStream(missing.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None) : null;
        var hash = fault == "UnknownDigest" ? ETag.Compute(ETagFormat.CBOR, [4]) : scenario.Receipt.SourceArchiveETag;
        var catalog = fault == "Empty" ? new RoamingNetworkColdArchiveCatalog([]) : Catalog(hash, missing.ArchivePath);
        var verified = 0;
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(scenario.Receipt, catalog, out var absent, out var result,
            verifyCommitSignature: (_, _) => { verified++; return true; }, limits: new(maxArchiveBytes: scenario.Bytes.Length)), Is.False);
        Assert.That(absent, Is.Null); Assert.That(verified, Is.Zero);
        Assert.That(result.Outcome, Is.EqualTo(fault is "Empty" or "UnknownDigest" or "NotFound"
            ? RoamingNetworkColdArchiveOutcome.ArchiveNotFound : RoamingNetworkColdArchiveOutcome.CandidatesRejected));
        Assert.That(result.Candidates.Length, Is.EqualTo(fault is "Empty" or "UnknownDigest" ? 0 : 1));
        if (result.Candidates.Length == 1) Assert.That(result.Candidates[0].Outcome.ToString(), Is.EqualTo(fault));
        scenario.Before.AssertUnchanged(scenario.Active); held?.Dispose();
        if (File.Exists(missing.ArchivePath)) Assert.That(File.ReadAllBytes(missing.ArchivePath), Is.EqualTo(bytes));
        Assert.That(File.ReadAllBytes(scenario.Cold.ArchivePath), Is.EqualTo(scenario.Bytes));
        Assert.That(Directory.GetFiles(missing.DirectoryPath).Any(path => path.EndsWith(".lock")), Is.False);
    }

    [TestCase("Complete")]
    [TestCase("Boundary")]
    [TestCase("Repeated")]
    public void Multiple_matching_copies_use_registration_order_and_newer_valid_archives_do_not_override_the_frozen_digest(String profile)
    {
        using var scenario = new Scenario(profile); using var copies = new ArchiveDirectory();
        var first = Path.Combine(copies.DirectoryPath, "z.cbor"); var second = Path.Combine(copies.DirectoryPath, "a.cbor");
        File.WriteAllBytes(first, scenario.Bytes); File.WriteAllBytes(second, scenario.Bytes);
        var catalog = Catalog(scenario.Receipt.SourceArchiveETag, first, second);
        Assert.That(scenario.Read(catalog, out var recovered, out var result), Is.True, result.Error);
        using (recovered!)
        {
            scenario.Verify(recovered!); Assert.That(result.ResolvedPath, Is.EqualTo(first)); Assert.That(result.Candidates, Has.Length.EqualTo(1));
            Publish(recovered!, Prepare(recovered!, recovered!.Head.Id, "newer-cold", Power("250 kW")));
            File.WriteAllBytes(first, recovered!.ToCBOR());
        }
        var newer = File.ReadAllBytes(first);
        Assert.That(scenario.Read(catalog, out recovered, out result), Is.True, result.Error);
        using (recovered!) { scenario.Verify(recovered!); }
        Assert.That(result.ResolvedPath, Is.EqualTo(second)); Assert.That(result.Candidates[0].Outcome, Is.EqualTo(RoamingNetworkColdArchiveCandidateOutcome.DigestMismatch));
        Assert.That(File.ReadAllBytes(first), Is.EqualTo(newer)); Assert.That(File.ReadAllBytes(second), Is.EqualTo(scenario.Bytes));
    }

    private static IEnumerable<TestCaseData> TrustCases()
    {
        foreach (var profile in Profiles)
        {
            foreach (var policy in new[] { "Batch", "Commit", "WholeCommit", "Receipt", "ReceiptException", "CommitException", "MissingVerifier" }) yield return new(profile, policy);
            if (profile != "Complete") foreach (var policy in new[] { "Boundary", "MissingBoundary" }) yield return new(profile, policy);
        }
    }

    [TestCaseSource(nameof(TrustCases))]
    public void Every_read_rechecks_current_trust_and_never_retries_another_copy_after_a_matching_digest_fails_authority(String profile, String policy)
    {
        using var scenario = new Scenario(profile); using var copy = new ArchiveDirectory(); File.WriteAllBytes(copy.ArchivePath, scenario.Bytes);
        var catalog = Catalog(scenario.Receipt.SourceArchiveETag, scenario.Cold.ArchivePath, copy.ArchivePath);
        Assert.That(scenario.Read(catalog, out var preview, out var first), Is.True, first.Error); preview!.Dispose();
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(scenario.Receipt, catalog, out var absent, out var result,
            verifyBatchSignature: (batch, peer) => policy != "Batch" && VerifyBatch(batch, peer),
            verifyCommitSignature: policy == "MissingVerifier" ? null : (commit, peer) => policy == "CommitException"
                ? throw new InvalidOperationException("Current commit policy failed") : policy != "Commit" && VerifyCommit(commit, peer),
            authorizeCommit: _ => policy != "WholeCommit",
            authorizeSnapshotBoundary: policy == "MissingBoundary" ? null : boundary => policy != "Boundary" && boundary.Checkpoint == scenario.Root,
            authorizeReceipt: _ => policy == "ReceiptException" ? throw new IOException("Current receipt policy failed") : policy != "Receipt"), Is.False);
        Assert.That(absent, Is.Null); Assert.That(result.Outcome, Is.EqualTo(policy == "Receipt"
            ? RoamingNetworkColdArchiveOutcome.Unauthorized : RoamingNetworkColdArchiveOutcome.VerificationFailed));
        Assert.That(result.Candidates.Length, Is.EqualTo(policy is "Receipt" or "ReceiptException" ? 0 : 1));
        Assert.That(File.ReadAllBytes(scenario.Cold.ArchivePath), Is.EqualTo(scenario.Bytes)); Assert.That(File.ReadAllBytes(copy.ArchivePath), Is.EqualTo(scenario.Bytes));
        Assert.That(scenario.Read(catalog, out var retried, out var retry), Is.True, retry.Error);
        using (retried!) { scenario.Verify(retried!); }
        Assert.That(Directory.GetFiles(copy.DirectoryPath), Is.EqualTo(new[] { copy.ArchivePath }));
    }

    private static IEnumerable<TestCaseData> LimitCases()
    {
        foreach (var profile in Profiles) yield return new(profile, "Commits");
        yield return new("Repeated", "Receipts"); yield return new("Repeated", "Catalog");
    }

    [TestCaseSource(nameof(LimitCases))]
    public void Actual_archive_containers_use_local_budgets_after_digest_matching_and_release_all_read_handles(String profile, String budget)
    {
        using var scenario = new Scenario(profile); var catalog = Catalog(scenario.Receipt.SourceArchiveETag, scenario.Cold.ArchivePath);
        using var original = RoamingNetworkHistory.ReadColdArchive(scenario.Cold.ArchivePath, scenario.Receipt.SourceArchiveETag,
            VerifyBatch, VerifyCommit, authorizeSnapshotBoundary: AcceptChain(scenario.Active));
        var limits = new RoamingNetworkHistoryLimits(maxCommits: budget == "Commits" ? original.Commits.Length - 1 : 100000,
            maxRetentionReceipts: budget == "Receipts" ? 1 : 4096, maxCatalogCommitIds: budget == "Catalog" ? 1 : 1000000);
        Assert.That(scenario.Read(catalog, out var absent, out var result, limits: limits), Is.False); Assert.That(absent, Is.Null);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkColdArchiveOutcome.LimitExceeded));
        Assert.That(result.LimitViolation!.Kind, Is.EqualTo(budget switch {
            "Commits" => RoamingNetworkHistoryLimitKind.RetainedCommits,
            "Receipts" => RoamingNetworkHistoryLimitKind.RetentionReceipts,
            _ => RoamingNetworkHistoryLimitKind.CatalogCommitIds }));
        Assert.That(result.Candidates.Single().Outcome, Is.EqualTo(RoamingNetworkColdArchiveCandidateOutcome.DigestMatched));
        using (var handle = new FileStream(scenario.Cold.ArchivePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        scenario.Before.AssertUnchanged(scenario.Active); Assert.That(File.ReadAllBytes(scenario.Cold.ArchivePath), Is.EqualTo(scenario.Bytes));
        var exact = new RoamingNetworkHistoryLimits(scenario.Bytes.Length, original.Commits.Length,
            Math.Max(1, original.RetentionReceipts.Length), Math.Max(1, original.RetentionReceipts.Sum(receipt => receipt.PrunedCommits.Length + receipt.ArchiveOnlyTips.Length)));
        Assert.That(scenario.Read(catalog, out var recovered, out result, limits: exact), Is.True, result.Error);
        using (recovered!) { scenario.Verify(recovered!); }
    }

    [TestCase("CheckpointId")]
    [TestCase("BeforeAnchor")]
    [TestCase("AfterAnchor")]
    [TestCase("AfterAnchorNotSnapshot")]
    [TestCase("Head")]
    [TestCase("PrunedCommits")]
    [TestCase("ArchiveOnlyTips")]
    public void A_valid_receipt_identity_cannot_invent_membership_or_roots_in_the_verified_source_archive(String field)
    {
        using var scenario = new Scenario("Complete");
        var receipt = Edit(scenario.Receipt, json => json[field == "AfterAnchorNotSnapshot" ? "AfterAnchor" : field] =
            JsonNode.Parse(field is "PrunedCommits" or "ArchiveOnlyTips" ? "[" + Unknown.Hash.ToJSON() + "]" :
                (field == "AfterAnchorNotSnapshot" ? scenario.Receipt.Head.Hash : Unknown.Hash).ToJSON().ToString()));
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(receipt, Catalog(receipt.SourceArchiveETag, scenario.Cold.ArchivePath),
            out var absent, out var result, VerifyBatch, VerifyCommit), Is.False);
        Assert.That(absent, Is.Null); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkColdArchiveOutcome.ReceiptMismatch));
        scenario.Before.AssertUnchanged(scenario.Active); Assert.That(File.ReadAllBytes(scenario.Cold.ArchivePath), Is.EqualTo(scenario.Bytes));
    }

    [Test]
    public void A_receipt_cannot_claim_an_unsigned_snapshot_as_a_completed_pruning_boundary()
    {
        using var scenario = new Scenario("Complete"); using var candidate = new ArchiveDirectory();
        using var original = RoamingNetworkHistory.ReadColdArchive(scenario.Cold.ArchivePath, scenario.Receipt.SourceArchiveETag, VerifyBatch, VerifyCommit);
        var archive = JsonNode.Parse(original.ToJSON())!;
        var anchor = JsonNode.Parse(scenario.Receipt.AfterAnchor.Hash.ToJSON().ToString());
        var snapshot = archive["Commits"]!.AsArray().Single(commit => JsonNode.DeepEquals(commit!["Id"], anchor))!;
        snapshot["Signatures"] = new JsonArray();
        using var unsigned = RoamingNetworkHistory.Parse(archive.ToJsonString(), VerifyBatch, VerifyCommit);
        var bytes = unsigned.ToCBOR(); File.WriteAllBytes(candidate.ArchivePath, bytes);
        var receipt = Edit(scenario.Receipt, json => json["SourceArchiveETag"] = JsonNode.Parse(ETag.Compute(ETagFormat.CBOR, bytes).ToJSON().ToString()));
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(receipt, Catalog(receipt.SourceArchiveETag, candidate.ArchivePath),
            out var absent, out var result, VerifyBatch, VerifyCommit), Is.False);
        Assert.That(absent, Is.Null); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkColdArchiveOutcome.ReceiptMismatch));
        Assert.That(File.ReadAllBytes(candidate.ArchivePath), Is.EqualTo(bytes)); scenario.Before.AssertUnchanged(scenario.Active);
    }

    [TestCase("Malformed")]
    [TestCase("Unsupported")]
    [TestCase("OtherChain")]
    public void Matching_digests_still_require_supported_complete_replay_and_the_receipts_original_chain(String fault)
    {
        using var scenario = new Scenario("Complete"); using var candidate = new ArchiveDirectory();
        var bytes = fault == "Malformed" ? new Byte[] { 1, 2, 3 } : CBORValue.FromMap(new[] {
            new KeyValuePair<CBORValue, CBORValue>(CBORValue.FromText("Profile"), CBORValue.FromText("unsupported")) }).ToByteArray();
        if (fault == "OtherChain")
        {
            var json = JsonNode.Parse(Network().ToJSONSnapshot().ToString())!; json["@id"] = "other-network";
            using var other = new RoamingNetworkHistory(RoamingNetwork.Parse(json.ToJsonString()), VerifyBatch, VerifyCommit);
            LinearHistory(other); bytes = other.ToCBOR();
        }
        File.WriteAllBytes(candidate.ArchivePath, bytes);
        var receipt = Edit(scenario.Receipt, json => json["SourceArchiveETag"] = JsonNode.Parse(ETag.Compute(ETagFormat.CBOR, bytes).ToJSON().ToString()));
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(receipt, Catalog(receipt.SourceArchiveETag, candidate.ArchivePath),
            out var absent, out var result, VerifyBatch, VerifyCommit), Is.False);
        Assert.That(absent, Is.Null); Assert.That(result.Outcome, Is.EqualTo(fault == "OtherChain"
            ? RoamingNetworkColdArchiveOutcome.ReceiptMismatch : RoamingNetworkColdArchiveOutcome.VerificationFailed));
        Assert.That(result.Candidates.Single().Outcome, Is.EqualTo(RoamingNetworkColdArchiveCandidateOutcome.DigestMatched));
        scenario.Before.AssertUnchanged(scenario.Active); Assert.That(File.ReadAllBytes(candidate.ArchivePath), Is.EqualTo(bytes));
    }

    [TestCase("Complete")]
    [TestCase("Boundary")]
    [TestCase("Repeated")]
    public void Unavailable_requested_commits_are_explicit_and_unknown_ids_never_invent_older_receipts(String profile)
    {
        using var scenario = new Scenario(profile); var catalog = Catalog(scenario.Receipt.SourceArchiveETag, scenario.Cold.ArchivePath);
        Assert.That(scenario.Read(catalog, out var absent, out var result, Unknown), Is.False); Assert.That(absent, Is.Null);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkColdArchiveOutcome.CommitNotRetained));
        Assert.That(result.CommitLookup!.Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Unknown)); Assert.That(result.CommitLookup.Receipt, Is.Null);
        scenario.Before.AssertUnchanged(scenario.Active);
    }

    [Test]
    public void Repeated_receipt_chains_resolve_older_moved_archives_explicitly_without_hydrating_active_ancestry()
    {
        using var scenario = new Scenario("Repeated"); using var moved = new ArchiveDirectory();
        File.Move(scenario.FirstCold.ArchivePath, moved.ArchivePath);
        var earliest = scenario.Active.LookupCommit(scenario.Root).Receipt!;
        var catalog = new RoamingNetworkColdArchiveCatalog([
            new(scenario.Receipt.SourceArchiveETag, scenario.Cold.ArchivePath), new(earliest.SourceArchiveETag, moved.ArchivePath)]);
        Assert.That(scenario.Read(catalog, out var absent, out var result, scenario.Root), Is.False); Assert.That(absent, Is.Null);
        Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkColdArchiveOutcome.CommitNotRetained));
        Assert.That(result.CommitLookup!.Availability, Is.EqualTo(RoamingNetworkCommitAvailability.Archived));
        Assert.That(result.CommitLookup.Receipt!.Id, Is.EqualTo(earliest.Id));
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(result.CommitLookup.Receipt, catalog, out var older, out var found,
            VerifyBatch, VerifyCommit, requestedCommit: scenario.Root), Is.True, found.Error);
        using (older!) { Assert.That(older!.GetCommit(scenario.Root).Id, Is.EqualTo(scenario.Root)); }
        scenario.Before.AssertUnchanged(scenario.Active);
        Assert.That(File.Exists(scenario.FirstCold.ArchivePath), Is.False); Assert.That(File.Exists(moved.ArchivePath + ".lock"), Is.False);
    }

    [TestCase("receipt")]
    [TestCase("catalog")]
    [TestCase("commit")]
    public void Invalid_recovery_inputs_return_no_history_and_do_not_access_catalog_files(String fault)
    {
        using var scenario = new Scenario("Complete");
        Assert.That(RoamingNetworkHistory.TryReadColdArchive(fault == "receipt" ? null! : scenario.Receipt,
            fault == "catalog" ? null! : Catalog(scenario.Receipt.SourceArchiveETag, scenario.Cold.ArchivePath), out var absent, out var result,
            requestedCommit: fault == "commit" ? default(RoamingNetworkCommitId) : null), Is.False);
        Assert.That(absent, Is.Null); Assert.That(result.Outcome, Is.EqualTo(RoamingNetworkColdArchiveOutcome.InvalidInput));
        Assert.That(result.Candidates, Is.Empty); scenario.Before.AssertUnchanged(scenario.Active);
    }
}
