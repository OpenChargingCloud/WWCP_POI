/*
 * Copyright (c) 2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 * Licensed under the Affero GPL license, Version 3.0.
 */

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace cloud.charging.open.protocols.WWCP.POI;

public sealed partial class RoamingNetworkHistory
{
    /// <summary>
    /// Announce the original checkpoint, local replay root, published head and retained branch tips.
    /// Acknowledgment includes every retained parent path down to the declared root.
    /// </summary>
    public RoamingNetworkReplicationState GetReplicationState()
    {
        lock (gate)
        {
            var parents = entries.Values.SelectMany(entry => entry.Commit.Parents).ToHashSet();
            return new(checkpointId, head.Id, entries.Keys.Where(id => !parents.Contains(id)).ToImmutableArray(), anchorId);
        }
    }

    /// <summary>
    /// Export a bounded parent-before-child prefix of the requested tip's missing ancestry.
    /// Unknown receiver tips are ignored. Refresh the receiver announcement after importing each
    /// page, while keeping targetTip fixed. Both JSON UTF-8 and CBOR page sizes obey maxBytes.
    /// </summary>
    public Boolean TryCreateCommitPack(RoamingNetworkReplicationState receiver, RoamingNetworkCommitId targetTip,
        [NotNullWhen(true)] out RoamingNetworkCommitPack? pack, out RoamingNetworkReplicationResult result,
        Int32 maxCommits = 128, Int32 maxBytes = 1024 * 1024)
    {
        lock (gate)
        {
            pack = null;
            if (disposed || mutating)
            {
                result = new(RoamingNetworkReplicationOutcome.Unavailable, head, Error: "History is disposed or a mutation callback is reentrant.");
                return false;
            }
            try
            {
                ArgumentNullException.ThrowIfNull(receiver);
                if (maxCommits < 1 || maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxCommits), "Page limits must be positive.");
                if (receiver.Checkpoint != checkpointId)
                {
                    result = new(RoamingNetworkReplicationOutcome.CheckpointMismatch, head,
                        Error: "Incremental exchange requires the same checkpoint; explicitly bootstrap a separate history.");
                    return false;
                }
                if (!targetTip.IsValid || !entries.ContainsKey(targetTip))
                {
                    if (targetTip.IsValid && archivedCommits.TryGetValue(targetTip, out var receipt))
                    {
                        result = new(RoamingNetworkReplicationOutcome.SnapshotRequired, head,
                            MissingCommits: [targetTip], ProposedBoundary: AvailableBoundary(), RetentionReceipt: receipt,
                            Error: "The requested tip was archived by explicit retention; retrieve its cold archive or explicitly authorize a current snapshot bootstrap.");
                        return false;
                    }
                    result = new(RoamingNetworkReplicationOutcome.UnknownTip, head, Error: "The requested tip is not retained.");
                    return false;
                }
                var wireRoot = receiver.HasCompleteAncestry && !HasCompleteAncestry ? anchorId : receiver.Anchor;
                if (receiver.HasCompleteAncestry && !HasCompleteAncestry &&
                    !receiver.KnownTips.Any(tip => entries.ContainsKey(tip) && Ancestors(tip).Contains(anchorId)))
                {
                    result = new(RoamingNetworkReplicationOutcome.SnapshotRequired, head,
                        MissingCommits: [anchorId], ProposedBoundary: AvailableBoundary(),
                        Error: "The receiver has not acknowledged this replica's snapshot anchor; explicitly bootstrap or retrieve its original ancestry.");
                    return false;
                }
                if (!entries.TryGetValue(wireRoot, out var rootEntry) ||
                    (wireRoot != checkpointId && rootEntry.Commit.Kind != RoamingNetworkCommitKind.Snapshot))
                {
                    result = new(RoamingNetworkReplicationOutcome.SnapshotRequired, head,
                        MissingCommits: [wireRoot], ProposedBoundary: AvailableBoundary(),
                        RetentionReceipt: archivedCommits.GetValueOrDefault(wireRoot),
                        Error: "The receiver's replay boundary is unavailable here; explicitly authorize a compatible snapshot or retrieve full history.");
                    return false;
                }
                if (!TryAncestorsWithin(targetTip, wireRoot, out var missing, out var external))
                {
                    result = new(RoamingNetworkReplicationOutcome.HistoryRequired, head,
                        MissingCommits: external, ProposedBoundary: AvailableBoundary(),
                        Error: "The requested tip needs ancestry outside the selected snapshot boundary; retrieve an earlier boundary or complete history.");
                    return false;
                }
                var known = new HashSet<RoamingNetworkCommitId> { wireRoot };
                foreach (var tip in receiver.KnownTips)
                    if (entries.ContainsKey(tip) && TryAncestorsWithin(tip, wireRoot, out var acknowledged, out _)) known.UnionWith(acknowledged);
                missing.ExceptWith(known);
                var selected = ImmutableArray<RoamingNetworkCommit>.Empty;
                var anchor = rootEntry.Commit;
                var candidate = new RoamingNetworkCommitPack(anchor, targetTip, true, selected, checkpointId);
                if (PackSize(candidate) > maxBytes)
                {
                    result = new(RoamingNetworkReplicationOutcome.CommitTooLarge, head,
                        Error: "The replay-root envelope and page header exceed maxBytes.");
                    return false;
                }
                foreach (var entry in OrderedEntries(entries).Where(entry => missing.Contains(entry.Commit.Id)))
                {
                    if (selected.Length == maxCommits) break;
                    var next = selected.Add(entry.Commit);
                    candidate = new(anchor, targetTip, next.Length == missing.Count, next, checkpointId);
                    if (PackSize(candidate) > maxBytes)
                    {
                        if (selected.IsEmpty)
                        {
                            result = new(RoamingNetworkReplicationOutcome.CommitTooLarge, head,
                                MissingCommits: [entry.Commit.Id], Error: "The next indivisible commit and page header exceed maxBytes.");
                            return false;
                        }
                        break;
                    }
                    selected = next;
                }
                pack = new(anchor, targetTip, selected.Length == missing.Count, selected, checkpointId);
                result = new(RoamingNetworkReplicationOutcome.PackAvailable, head);
                return true;
            }
            catch (Exception exception)
            {
                result = new(RoamingNetworkReplicationOutcome.InvalidInput, head, Error: exception.Message);
                return false;
            }
        }
    }

    private static Int32 PackSize(RoamingNetworkCommitPack pack)
        => Math.Max(Encoding.UTF8.GetByteCount(pack.ToJSON()), pack.ToCBOR().Length);

    /// <summary>
    /// Validate and retain an entire incoming page atomically, including peer-envelope unions.
    /// Missing parents, trust, replay, batch-ID or persistence failures retain nothing from the page.
    /// This method never selects a remote head or copies remote runtime data.
    /// </summary>
    public Boolean TryImportCommitPack(RoamingNetworkCommitPack pack, out RoamingNetworkReplicationResult result,
                                       Int32 maxCommits = 128, Int32 maxBytes = 1024 * 1024)
    {
        lock (gate)
        {
            if (disposed || mutating)
            {
                result = new(RoamingNetworkReplicationOutcome.Unavailable, head, Error: "History is disposed or a mutation callback is reentrant.");
                return false;
            }
            mutating = true;
            try
            {
                ArgumentNullException.ThrowIfNull(pack);
                VerifyBoundary();
                if (maxCommits < 1 || maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxCommits), "Page limits must be positive.");
                if (pack.Checkpoint != checkpointId)
                {
                    result = new(RoamingNetworkReplicationOutcome.CheckpointMismatch, head,
                        Error: "The checkpoint differs; incremental import cannot replace the local history.");
                    return false;
                }
                if (pack.Commits.Length > maxCommits || PackSize(pack) > maxBytes)
                {
                    result = new(RoamingNetworkReplicationOutcome.CommitTooLarge, head, Error: "The incoming page exceeds the configured limits.");
                    return false;
                }
                if (!entries.TryGetValue(pack.AnchorCommit.Id, out var rootEntry))
                {
                    result = new(HasCompleteAncestry ? RoamingNetworkReplicationOutcome.MissingParents : RoamingNetworkReplicationOutcome.SnapshotRequired,
                        head, MissingCommits: [pack.AnchorCommit.Id],
                        RetentionReceipt: archivedCommits.GetValueOrDefault(pack.AnchorCommit.Id),
                        ProposedBoundary: pack.HasSnapshotAnchor && !pack.AnchorCommit.Signatures.IsEmpty ? new(checkpointId, pack.AnchorCommit) : AvailableBoundary(),
                        Error: "The page root is not retained. Import its original ancestry or explicitly bootstrap a separate authorized snapshot replica.");
                    return false;
                }
                var incomingIds = pack.Commits.Select(commit => commit.Id).ToHashSet();
                var missing = pack.Commits.SelectMany(commit => commit.Parents)
                    .Where(id => !entries.ContainsKey(id) && !incomingIds.Contains(id)).ToHashSet();
                if (pack.Complete && !entries.ContainsKey(pack.Tip) && !incomingIds.Contains(pack.Tip)) missing.Add(pack.Tip);
                if (missing.Count > 0)
                {
                    var ordered = RoamingNetworkRetentionPlan.Sort(missing);
                    var receipt = ordered.Select(id => archivedCommits.GetValueOrDefault(id)).FirstOrDefault(value => value is not null);
                    result = new(receipt is null ? RoamingNetworkReplicationOutcome.MissingParents : RoamingNetworkReplicationOutcome.HistoryRequired, head,
                        MissingCommits: ordered, RetentionReceipt: receipt, ProposedBoundary: receipt is null ? null : AvailableBoundary(),
                        Error: receipt is null ? "Fetch these missing commits and their ancestry before retrying the unchanged page." :
                            "Required commits were archived; retrieve cold history or explicitly bootstrap a compatible snapshot before retrying.");
                    return false;
                }
                if (!pack.HasSnapshotAnchor) VerifyCommit(pack.AnchorCommit);
                var root = MergeEnvelopes(rootEntry.Commit, pack.AnchorCommit);
                VerifyCommit(root);
                MatchState(root, rootEntry.Snapshot);
                var updatedEntries = entries.SetItem(root.Id, new(root, rootEntry.Snapshot));
                var updatedBatches = batchIds;

                // Stored ancestry is reauthorized using the receiver's current trust policy.
                var consumed = new HashSet<RoamingNetworkCommitId>();
                foreach (var parent in pack.Commits.SelectMany(commit => commit.Parents).Append(pack.Tip))
                    if (entries.ContainsKey(parent)) consumed.UnionWith(Ancestors(parent));
                foreach (var id in consumed.Where(id => id != root.Id && !incomingIds.Contains(id)))
                {
                    VerifyCommit(entries[id].Commit);
                    VerifyBatch(entries[id].Commit);
                }

                var stored = 0;
                var duplicates = 0;
                var preceding = new HashSet<RoamingNetworkCommitId>();
                foreach (var incoming in pack.Commits)
                {
                    if (incoming.Parents.Any(id => incomingIds.Contains(id) && !preceding.Contains(id)))
                        throw new ArgumentException("Parents included in a page must precede their children.");
                    VerifyCommit(incoming);
                    var batch = incoming.ChangeSet;
                    if (batch is not null && updatedBatches.TryGetValue(batch.Id, out var priorId) && priorId != incoming.Id)
                    {
                        result = new(RoamingNetworkReplicationOutcome.ChangeSetIdConflict, head,
                            Error: "A batch ID already identifies different commit content or ancestry.");
                        return false;
                    }
                    ValidateParents(incoming, updatedEntries);
                    var known = updatedEntries.TryGetValue(incoming.Id, out var retained);
                    var commit = known ? MergeEnvelopes(retained!.Commit, incoming) : incoming;
                    VerifyCommit(commit);
                    VerifyBatch(commit);
                    var snapshot = known ? retained!.Snapshot :
                        ApplyCommit(updatedEntries[commit.Parents[0]].Snapshot, commit);
                    MatchState(commit, snapshot);
                    updatedEntries = updatedEntries.SetItem(commit.Id, new(commit, snapshot));
                    if (batch is not null) updatedBatches = updatedBatches.SetItem(batch.Id, commit.Id);
                    preceding.Add(commit.Id);
                    if (known) duplicates++; else stored++;
                }
                if (pack.Complete && !updatedEntries.ContainsKey(pack.Tip)) throw new ArgumentException("The complete page did not retain its requested tip.");
                var updatedHead = new RoamingNetworkHead(updatedEntries[head.Id].Commit, head.Network);
                try { Persist(updatedEntries, updatedHead.Id); }
                catch (Exception exception)
                {
                    result = new(RoamingNetworkReplicationOutcome.PersistenceFailure, head, Error: exception.Message);
                    return false;
                }
                entries = updatedEntries;
                batchIds = updatedBatches;
                head = updatedHead;
                result = new(stored == 0 ? RoamingNetworkReplicationOutcome.AlreadyStored : RoamingNetworkReplicationOutcome.Imported,
                    head, stored, duplicates);
                return true;
            }
            catch (Exception exception)
            {
                result = new(RoamingNetworkReplicationOutcome.InvalidInput, head, Error: exception.Message);
                return false;
            }
            finally { mutating = false; }
        }
    }
}
