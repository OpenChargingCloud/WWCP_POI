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
    /// Announce the shared checkpoint, published head and all retained branch tips.
    /// Each acknowledged tip includes its complete ancestry through every parent.
    /// </summary>
    public RoamingNetworkReplicationState GetReplicationState()
    {
        lock (gate)
        {
            var parents = entries.Values.SelectMany(entry => entry.Commit.Parents).ToHashSet();
            return new(checkpointId, head.Id, entries.Keys.Where(id => !parents.Contains(id)).ToImmutableArray());
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
                    result = new(RoamingNetworkReplicationOutcome.UnknownTip, head, Error: "The requested tip is not retained.");
                    return false;
                }
                var known = new HashSet<RoamingNetworkCommitId> { checkpointId };
                foreach (var tip in receiver.KnownTips)
                    if (entries.ContainsKey(tip)) known.UnionWith(Ancestors(tip));
                var missing = Ancestors(targetTip);
                missing.ExceptWith(known);
                var selected = ImmutableArray<RoamingNetworkCommit>.Empty;
                var anchor = entries[checkpointId].Commit;
                var candidate = new RoamingNetworkCommitPack(anchor, targetTip, true, selected);
                if (PackSize(candidate) > maxBytes)
                {
                    result = new(RoamingNetworkReplicationOutcome.CommitTooLarge, head,
                        Error: "The checkpoint envelope and page header exceed maxBytes.");
                    return false;
                }
                foreach (var entry in OrderedEntries(entries).Where(entry => missing.Contains(entry.Commit.Id)))
                {
                    if (selected.Length == maxCommits) break;
                    var next = selected.Add(entry.Commit);
                    candidate = new(anchor, targetTip, next.Length == missing.Count, next);
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
                pack = new(anchor, targetTip, selected.Length == missing.Count, selected);
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
                if (maxCommits < 1 || maxBytes < 1) throw new ArgumentOutOfRangeException(nameof(maxCommits), "Page limits must be positive.");
                if (pack.CheckpointCommit.Id != checkpointId)
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
                var incomingIds = pack.Commits.Select(commit => commit.Id).ToHashSet();
                var missing = pack.Commits.SelectMany(commit => commit.Parents)
                    .Where(id => !entries.ContainsKey(id) && !incomingIds.Contains(id)).ToHashSet();
                if (pack.Complete && !entries.ContainsKey(pack.Tip) && !incomingIds.Contains(pack.Tip)) missing.Add(pack.Tip);
                if (missing.Count > 0)
                {
                    result = new(RoamingNetworkReplicationOutcome.MissingParents, head,
                        MissingCommits: missing.OrderBy(id => id.ToString(), StringComparer.Ordinal).ToImmutableArray(),
                        Error: "Fetch these missing commits and their ancestry before retrying the unchanged page.");
                    return false;
                }
                VerifyCommit(pack.CheckpointCommit);
                var checkpoint = MergeEnvelopes(entries[checkpointId].Commit, pack.CheckpointCommit);
                VerifyCommit(checkpoint);
                MatchState(checkpoint, entries[checkpointId].Snapshot);
                var updatedEntries = entries.SetItem(checkpointId, new(checkpoint, entries[checkpointId].Snapshot));
                var updatedBatches = batchIds;

                // Stored ancestry is reauthorized using the receiver's current trust policy.
                var consumed = new HashSet<RoamingNetworkCommitId>();
                foreach (var parent in pack.Commits.SelectMany(commit => commit.Parents).Append(pack.Tip))
                    if (entries.ContainsKey(parent)) consumed.UnionWith(Ancestors(parent));
                foreach (var id in consumed.Where(id => id != checkpointId && !incomingIds.Contains(id)))
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
                    var batch = incoming.ChangeSet!;
                    if (updatedBatches.TryGetValue(batch.Id, out var priorId) && priorId != incoming.Id)
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
                        updatedEntries[commit.Parents[0]].Snapshot.ApplyChangeSet(commit.ChangeSet!, verifyBatchSignature);
                    MatchState(commit, snapshot);
                    updatedEntries = updatedEntries.SetItem(commit.Id, new(commit, snapshot));
                    updatedBatches = updatedBatches.SetItem(batch.Id, commit.Id);
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
