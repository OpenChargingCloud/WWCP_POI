/*
 * Copyright (c) 2014-2026 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP POI <https://github.com/OpenChargingCloud/WWCP_POI>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A store of a history failed, e.g. to keep a status update; the history goes on.
    /// </summary>
    /// <param name="History">The history.</param>
    /// <param name="Description">What failed.</param>
    /// <param name="Exception">The exception, if any.</param>
    public delegate void OnRoamingNetworkHistoryStoreErrorDelegate(RoamingNetworkHistory  History,
                                                                   String                 Description,
                                                                   Exception?             Exception);


    /// <summary>
    /// The persistence and the retention of a history.
    /// </summary>
    /// <remarks>
    /// A history with a store writes every commit and every signature added later before it shows them, and every
    /// change of the runtime status; loading reads the base, a snapshot of the root, and applies the log again,
    /// checking every commit. Compacting drops the commits before a new root, see RoamingNetworkHistoryRetention,
    /// and replaces the base and the log of the store: the runtime status is folded into the new base.
    /// </remarks>
    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        private IRoamingNetworkHistoryStore?  store;
        private Boolean                       loading;
        private Int64                         logEntries;
        private Int32                         compacting;

        #endregion

        #region Properties

        /// <summary>
        /// The store of this history, if any.
        /// </summary>
        public IRoamingNetworkHistoryStore?     Store
            => Volatile.Read(ref store);

        /// <summary>
        /// What this history keeps; everything, unless given.
        /// </summary>
        public RoamingNetworkHistoryRetention   Retention    { get; set; } = RoamingNetworkHistoryRetention.KeepEverything;

        /// <summary>
        /// The number of entries of the log of the store since its base.
        /// </summary>
        public Int64                            LogEntries
            => Interlocked.Read(ref logEntries);

        #endregion

        #region Events

        /// <summary>
        /// The store failed to keep something the history did not have to refuse, e.g. a status update.
        /// </summary>
        public event OnRoamingNetworkHistoryStoreErrorDelegate? OnStoreError;

        #endregion


        #region AttachStore(Store, Retention = null)

        /// <summary>
        /// Keep this history in the given store from now on: everything it holds is written, as a new base
        /// when the store is empty, otherwise replacing its content.
        /// </summary>
        /// <param name="Store">The store.</param>
        /// <param name="Retention">What to keep, unless the one so far.</param>
        public void AttachStore(IRoamingNetworkHistoryStore      Store,
                                RoamingNetworkHistoryRetention?  Retention   = null)
        {

            ArgumentNullException.ThrowIfNull(Store);

            lock (historyLock)
            {

                if (Retention is not null)
                    this.Retention = Retention;

                var snapshot  = RootSnapshotJSON();
                var entries   = commits.Skip(1).Select(RoamingNetworkHistoryLogEntry.ForCommit).Concat(CheckpointLogEntries()).ToArray();

                if (Store.Load() is null)
                {
                    Store.WriteBase(snapshot);
                    foreach (var entry in entries)
                        Store.Append(entry);
                }
                else
                    Store.Compact(snapshot, entries);

                Interlocked.Exchange(ref logEntries, entries.Length);
                Volatile.Write(ref store, Store);

            }

        }

        #endregion

        #region DetachStore()

        /// <summary>
        /// Stop keeping this history in its store, e.g. when another history replaces it there.
        /// </summary>
        /// <returns>The store, if there was one.</returns>
        public IRoamingNetworkHistoryStore? DetachStore()
        {
            lock (historyLock)
            {
                return Interlocked.Exchange(ref store, null);
            }
        }

        #endregion

        #region (static) TryLoad(Store, out History, out ErrorResponse, Retention = null, Signers = null, Limits = null, Archive = null)

        /// <summary>
        /// Load a history from the given store: its base, and every log entry again, in their order.
        /// Every commit is checked again: signed by its owners and leading to its ETags, or the merge of its parents;
        /// its own signatures and the ones added later count as checked. Tips that do not conflict are merged afterwards,
        /// e.g. when a crash came before a merge commit was written. The history keeps the store.
        /// </summary>
        /// <param name="Store">The store.</param>
        /// <param name="History">The history.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Retention">What to keep; everything, unless given.</param>
        /// <param name="Signers">The keys that sign every commit made here.</param>
        /// <param name="Limits">Optional limits for reading change sets.</param>
        /// <param name="Archive">Where to keep the commits dropped when compacting.</param>
        public static Boolean TryLoad(IRoamingNetworkHistoryStore                       Store,
                                      [NotNullWhen(true)]  out RoamingNetworkHistory?   History,
                                      [NotNullWhen(false)] out String?                  ErrorResponse,
                                      RoamingNetworkHistoryRetention?                   Retention   = null,
                                      IEnumerable<RoamingNetworkCommitSigner>?          Signers     = null,
                                      RoamingNetworkChangeSetLimits?                    Limits      = null,
                                      IRoamingNetworkHistoryArchive?                    Archive     = null)
        {

            History = null;

            RoamingNetworkHistoryStoreContent? content;

            try
            {
                content = Store.Load();
            }
            catch (Exception e)
            {
                ErrorResponse = "The store can not be read: " + e.Message;
                return false;
            }

            if (content is null)
            {
                ErrorResponse = "The store is empty!";
                return false;
            }

            if (!TryFromSnapshot(content.Base, out var history, out ErrorResponse, Limits))
            {
                ErrorResponse = "Invalid base of the store: " + ErrorResponse;
                return false;
            }

            history.Retention  = Retention ?? RoamingNetworkHistoryRetention.KeepEverything;
            history.Signers    = Signers?.ToImmutableArray() ?? [];
            history.Archive    = Archive;

            lock (history.historyLock)
            {

                history.loading = true;

                try
                {

                    for (var index = 0; index < content.Entries.Count; index++)
                    {

                        var entry = content.Entries[index];

                        switch (entry.Type)
                        {

                            case RoamingNetworkHistoryLogEntryType.Commit:
                                if (!history.TryAppend(entry.Commit!, false, true, out var appendError, null, null, null))
                                {
                                    ErrorResponse = $"Log entry {index}: {appendError}";
                                    return false;
                                }
                                break;

                            case RoamingNetworkHistoryLogEntryType.Signatures:
                                history.TakeKnownSignatures(entry.CommitId!.Value, entry.Signatures);
                                break;

                            case RoamingNetworkHistoryLogEntryType.Checkpoint:
                                history.TakeStoredCheckpoint(entry.Checkpoint!);
                                break;

                            case RoamingNetworkHistoryLogEntryType.CheckpointSignatures:
                                history.TakeStoredCheckpointSignatures(entry.CheckpointId!.Value, entry.Signatures);
                                break;

                            case RoamingNetworkHistoryLogEntryType.PriceStatement:
                                lock (history.priceStatementLock)
                                {
                                    history.TakeStoredPriceStatement(entry.PriceStatement!);
                                }
                                break;

                            case RoamingNetworkHistoryLogEntryType.CheckpointPriceStatements:
                                lock (history.priceStatementLock)
                                {
                                    history.TakeCheckpointPriceStatements(entry.CheckpointId!.Value, entry.PriceStatements);
                                }
                                break;

                            default:
                                history.RuntimeStatus.TryUpdate(entry.StatusUpdate!, out _);
                                break;

                        }

                    }

                    // The status of the base, for the entities added since; not authoritative,
                    // so that a later status of the log stays the current one.
                    if (content.Base["runtimeStatus"] is JArray baseStatus)
                        foreach (var statusJSON in baseStatus.OfType<JObject>())
                            if (POIStatusUpdate.TryParse(statusJSON, out var statusUpdate, out _))
                                history.RuntimeStatus.TryUpdate(statusUpdate.Kind,
                                                                statusUpdate.EntityType,
                                                                statusUpdate.EntityId,
                                                                statusUpdate.Value,
                                                                statusUpdate.Timestamp,
                                                                out _,
                                                                out _,
                                                                statusUpdate.DataSource);

                }
                finally
                {
                    history.loading = false;
                }

                Interlocked.Exchange(ref history.logEntries, content.Entries.Count);
                Volatile.Write(ref history.store, Store);

                history.RetainVersions();
                history.MergeTips();

            }

            History        = history;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region Compact(out ErrorResponse)

        /// <summary>
        /// Drop the commits before a new root, as the retention allows, and replace the content of the store:
        /// the base becomes a snapshot of the new root with the current runtime status, the log the commits since.
        /// Without commits to drop, the log is still replaced, without its status updates.
        /// </summary>
        /// <param name="ErrorResponse">Why it failed, if it did.</param>
        /// <returns>Whether the history was compacted.</returns>
        public Boolean Compact([NotNullWhen(false)] out String? ErrorResponse)
        {

            lock (historyLock)
            {

                try
                {

                    var newRoot  = FindNewRoot();
                    var dropped  = newRoot.Id == commits[0].Id ? new HashSet<RoamingNetworkCommit_Id>() : Ancestors(newRoot.Id);
                    dropped.Remove(newRoot.Id);

                    if (!TryGetVersion(newRoot.Id, out var rootVersion, out ErrorResponse))
                        return false;

                    var kept     = commits.Where(commit => !dropped.Contains(commit.Id)).ToList();

                    // Archived before the store forgets them: an archive failing fails the compaction, and nothing is dropped.
                    ArchiveDropped(newRoot, dropped);

                    // The checkpoints are all kept, also those of the commits dropped: they are the evidence of what was.
                    Store?.Compact(SnapshotJSON(newRoot, rootVersion, Head.RoamingNetwork),
                                   kept.Skip(1).Select(RoamingNetworkHistoryLogEntry.ForCommit).Concat(CheckpointLogEntries()));

                    #region Drop them from memory, too

                    if (dropped.Count > 0)
                    {

                        commits.Clear();
                        commitIndex.Clear();
                        committedChangeSets.Clear();

                        foreach (var commitId in dropped)
                            versions.Remove(commitId);

                        foreach (var commit in kept)
                        {
                            commitIndex[commit.Id] = commits.Count;
                            commits.Add(commit);
                            if (commit.ChangeSet is RoamingNetworkChangeSet changeSet && !commit.IsMerge)
                                foreach (var changeSetId in ChangeSetIds(changeSet))
                                    committedChangeSets.TryAdd(changeSetId, commit.Id);
                        }

                        versions[newRoot.Id] = rootVersion;

                    }

                    #endregion

                    Interlocked.Exchange(ref logEntries, kept.Count - 1 + checkpoints.Count);

                    ErrorResponse = null;
                    return true;

                }
                catch (Exception e)
                {
                    ErrorResponse = "The history can not be compacted: " + e.Message;
                    return false;
                }

            }

        }

        #endregion

        #region (private) FindNewRoot()

        /// <summary>
        /// The latest commit all tips follow, at least RetainCommits revisions behind the head, that every commit kept follows;
        /// the root so far, when there is none.
        /// </summary>
        private RoamingNetworkCommit FindNewRoot()
        {

            var head = Head.Commit;

            if (Retention.RetainCommits >= head.Revision)
                return commits[0];

            var maxRevision  = head.Revision - Retention.RetainCommits;
            var checkpointCommits = CheckpointCommitIds();
            HashSet<RoamingNetworkCommit_Id>? common = null;

            foreach (var tip in tips)
            {
                var ancestors = Ancestors(tip);
                if (common is null) common = ancestors;
                else                common.IntersectWith(ancestors);
            }

            var candidates = (common ?? []).
                                 Select (commitId => commits[commitIndex[commitId]]).
                                 Where  (commit   => commit.Revision <= maxRevision).
                                 // A history with checkpoints compacts only to the commit of one of them (SPECIFICATION.md, section 10.6).
                                 Where  (commit   => checkpointCommits.Count == 0 || checkpointCommits.Contains(commit.Id)).
                                 ToList();

            candidates.Sort((commit1, commit2) => IsCanonicalOrder(commit1, commit2) ? (commit1.Id == commit2.Id ? 0 : -1) : 1);

            foreach (var candidate in candidates)
            {

                if (candidate.Id == commits[0].Id)
                    break;

                var dropped  = Ancestors(candidate.Id);
                dropped.Remove(candidate.Id);

                var keptIds  = commits.Where(commit => !dropped.Contains(commit.Id)).Select(commit => commit.Id).ToHashSet();

                // Every commit kept must follow the new root: none may name a parent dropped.
                if (commits.Where (commit => keptIds.Contains(commit.Id) && commit.Id != candidate.Id).
                            All   (commit => commit.ParentIds.All(keptIds.Contains)))
                {
                    return candidate;
                }

            }

            return commits[0];

        }

        #endregion

        #region (private) CompactIfDue()

        /// <summary>
        /// Compact this history in the background once the log of its store is long enough.
        /// </summary>
        private void CompactIfDue()
        {

            if (Store is null || loading || Interlocked.Read(ref logEntries) < Retention.CompactAfterLogEntries)
                return;

            if (Interlocked.CompareExchange(ref compacting, 1, 0) != 0)
                return;

            Task.Run(() => {
                try
                {
                    if (!Compact(out var error))
                        RaiseStoreError(error, null);
                }
                finally
                {
                    Interlocked.Exchange(ref compacting, 0);
                }
            });

        }

        #endregion


        #region (private) StoreAppend(Entry)

        /// <summary>
        /// Write the given log entry to the store, unless loading or there is none; failures are thrown.
        /// </summary>
        private void StoreAppend(RoamingNetworkHistoryLogEntry Entry)
        {

            var currentStore = Store;

            if (currentStore is null || loading)
                return;

            currentStore.Append(Entry);
            Interlocked.Increment(ref logEntries);

        }

        #endregion

        #region (private) AppendStatusUpdate(StatusUpdate)

        /// <summary>
        /// Write a change of the runtime status to the store; a failure does not keep the status from changing.
        /// </summary>
        private void AppendStatusUpdate(POIStatusUpdate StatusUpdate)
        {

            try
            {
                StoreAppend(RoamingNetworkHistoryLogEntry.ForStatus(StatusUpdate));
                CompactIfDue();
            }
            catch (Exception e)
            {
                RaiseStoreError("A status update can not be stored: " + e.Message, e);
            }

        }

        #endregion

        #region (private) RaiseStoreError(Description, Exception)

        private void RaiseStoreError(String      Description,
                                     Exception?  Exception)
        {
            try
            {
                OnStoreError?.Invoke(this, Description, Exception);
            }
            catch
            { }
        }

        #endregion

        #region (private) TakeKnownSignatures(CommitId, Signatures)

        /// <summary>
        /// Take signatures of a commit checked before, e.g. from the log of the store.
        /// </summary>
        private void TakeKnownSignatures(RoamingNetworkCommit_Id                     CommitId,
                                         IEnumerable<RoamingNetworkCommitSignature>  Signatures)
        {

            if (!commitIndex.TryGetValue(CommitId, out var index))
                return;

            var commit = commits[index].WithSignatures(Signatures);

            commits[index] = commit;

            if (Head.Commit.Id == CommitId)
                Volatile.Write(ref head, Head with { Commit = commit });

        }

        #endregion


        #region (private) TryGetVersion(CommitId, out RoamingNetwork, out ErrorResponse)

        /// <summary>
        /// The version of the given commit: kept, or calculated again from the closest one kept on the line of its first parents.
        /// </summary>
        private Boolean TryGetVersion(RoamingNetworkCommit_Id                   CommitId,
                                      [NotNullWhen(true)]  out RoamingNetwork?  RoamingNetwork,
                                      [NotNullWhen(false)] out String?          ErrorResponse)
        {

            if (versions.TryGetValue(CommitId, out RoamingNetwork))
            {
                ErrorResponse = null;
                return true;
            }

            var line = new Stack<RoamingNetworkCommit>();
            var next = CommitId;

            while (!versions.TryGetValue(next, out RoamingNetwork))
            {

                if (!commitIndex.TryGetValue(next, out var index) || commits[index].ParentId is not RoamingNetworkCommit_Id parentId)
                {
                    ErrorResponse = $"The version of the commit '{CommitId}' can not be calculated here!";
                    return false;
                }

                line.Push(commits[index]);
                next = parentId;

            }

            while (line.TryPop(out var commit))
            {

                if (!RoamingNetwork.TryApplyKnown(commit.ChangeSet!, out var nextVersion, out ErrorResponse))
                {
                    ErrorResponse = $"The version of the commit '{commit.Id}' can not be calculated again: {ErrorResponse}";
                    RoamingNetwork = null;
                    return false;
                }

                // A merge compared its effective members with both of its parents, and took the runtime status of both.
                if (commit.IsMerge)
                {

                    if (!TryGetVersion(commit.ParentIds[1], out var right, out ErrorResponse))
                    {
                        RoamingNetwork = null;
                        return false;
                    }

                    nextVersion.ShareRuntimeOfMergedParents(RoamingNetwork, right);

                }

                RoamingNetwork = nextVersion;

            }

            versions[CommitId] = RoamingNetwork;

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private) VersionOf(CommitId)

        /// <summary>
        /// The version of the given commit, which must be one this history can calculate.
        /// </summary>
        private RoamingNetwork VersionOf(RoamingNetworkCommit_Id CommitId)

            => TryGetVersion(CommitId, out var roamingNetwork, out var errorResponse)
                   ? roamingNetwork
                   : throw new InvalidOperationException(errorResponse);

        #endregion

        #region (private) RetainVersions()

        /// <summary>
        /// Drop the versions the retention does not keep: all but the root, the tips,
        /// every VersionCheckpointInterval-th revision and the RecentVersions latest commits.
        /// </summary>
        private void RetainVersions()
        {

            if (versions.Count <= (Int64) Retention.RecentVersions + tips.Count + 1)
                return;

            var keep = new HashSet<RoamingNetworkCommit_Id>(tips) { commits[0].Id };

            for (var index = Math.Max(0, commits.Count - Retention.RecentVersions); index < commits.Count; index++)
                keep.Add(commits[index].Id);

            foreach (var commitId in versions.Keys.ToArray())
                if (!keep.Contains(commitId) &&
                    (Retention.VersionCheckpointInterval == 0 || commits[commitIndex[commitId]].Revision % Retention.VersionCheckpointInterval != 0))
                {
                    versions.Remove(commitId);
                }

        }

        #endregion

        #region (private) FindVersionFor(ChangeSet)

        /// <summary>
        /// The commit whose version the given change set was made for: a tip, the head first,
        /// or else the latest commit having it, also one whose version is calculated again.
        /// </summary>
        private (RoamingNetworkCommit Commit, RoamingNetwork Version)? FindVersionFor(RoamingNetworkChangeSet ChangeSet)
        {

            foreach (var tip in tips)
                if (versions[tip].Fits(ChangeSet))
                    return (commits[commitIndex[tip]], versions[tip]);

            for (var index = commits.Count - 1; index >= 0; index--)
            {

                var commit = commits[index];

                if (versions.TryGetValue(commit.Id, out var kept))
                {
                    if (kept.Fits(ChangeSet))
                        return (commit, kept);
                    continue;
                }

                // Not kept: only when the ETags of the commit name the same version, it is calculated again.
                var comparable = ChangeSet.BeforeETags.Where(before => commit.ETags.Any(eTag => eTag.Format == before.Format && eTag.HashAlgorithm == before.HashAlgorithm)).ToArray();

                if (comparable.Length > 0 &&
                    comparable.All(before => commit.ETags.Contains(before)) &&
                    TryGetVersion(commit.Id, out var calculated, out _) &&
                    calculated.Fits(ChangeSet))
                {
                    return (commit, calculated);
                }

            }

            return null;

        }

        #endregion

        #region (private) RootSnapshotJSON()

        /// <summary>
        /// A snapshot of the root, with the current runtime status: the base of a store.
        /// </summary>
        private JObject RootSnapshotJSON()

            => SnapshotJSON(commits[0], VersionOf(commits[0].Id), Head.RoamingNetwork);

        #endregion

        #region (private static) SnapshotJSON(Commit, RoamingNetwork, StatusOf)

        /// <summary>
        /// A snapshot of the given commit and its version, with the floors of the last changes of its effective members,
        /// and with the runtime status of the given version.
        /// </summary>
        private static JObject SnapshotJSON(RoamingNetworkCommit  Commit,
                                            RoamingNetwork        RoamingNetwork,
                                            RoamingNetwork        StatusOf)
        {

            var json = new JObject(
                           new JProperty("timestamp",       Timestamp.Now.ToISO8601()),
                           new JProperty("commit",          Commit.ToJSON()),
                           new JProperty("roamingNetwork",  RoamingNetwork.ToJSON())
                       );

            if (RoamingNetwork.EffectiveFloorsJSON() is JArray effectiveLastChanges)
                json.Add("effectiveLastChanges", effectiveLastChanges);

            json.Add("runtimeStatus", new JArray(RoamingNetworkRuntimeStatus.Of(StatusOf).Select(status => status.ToJSON())));

            return json;

        }

        #endregion

    }

}
