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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What readers of a history take without its lock: its commits, its tips, the versions it keeps, the attestations
    /// it holds, its checkpoints and the change sets waiting for signatures, as immutable collections. A commit holds the
    /// lock for all of its time, a merge or a sync to the disk included; a reader of the view never waits for it, and
    /// reads one state of the history, the latest published.
    /// </summary>
    /// <remarks>
    /// Every change of these collections publishes a new view while the history is locked, the commits of a new head
    /// before the head itself: a commit a reader sees as the head is in the view it takes afterwards. The mutable
    /// collections stay for the writers, who change them under the lock as before; the view follows each change.
    /// A version a writer calculates again is kept only for the writers, until the retention drops it; a reader
    /// calculates one not in its view itself, from the versions of the view, and keeps the last one (TryGetVersion).
    /// </remarks>
    public sealed partial class RoamingNetworkHistory
    {

        #region (private) class ReadView

        /// <summary>
        /// One state of the commits, tips, kept versions, attestations, checkpoints and change sets waiting for
        /// signatures of a history.
        /// </summary>
        private sealed record ReadView(ImmutableList<RoamingNetworkCommit>                           Commits,
                                       ImmutableDictionary<RoamingNetworkCommit_Id, Int32>           CommitIndex,
                                       ImmutableArray<RoamingNetworkCommit_Id>                       Tips,
                                       ImmutableDictionary<RoamingNetworkCommit_Id, RoamingNetwork>  Versions,
                                       ImmutableArray<RoamingNetworkAttestation>                     Attestations,
                                       ImmutableList<RoamingNetworkCheckpoint>                       Checkpoints,
                                       ImmutableDictionary<RoamingNetworkCheckpoint_Id, Int32>       CheckpointIndex,
                                       ImmutableArray<RoamingNetworkChangeSet>                       PendingChangeSets,
                                       ImmutableDictionary<ChangeSet_Id, RoamingNetworkChangeSet>    PendingIndex)
        {

            public static ReadView Empty { get; }

                = new ([],
                       ImmutableDictionary<RoamingNetworkCommit_Id, Int32>.Empty,
                       [],
                       ImmutableDictionary<RoamingNetworkCommit_Id, RoamingNetwork>.Empty,
                       [],
                       [],
                       ImmutableDictionary<RoamingNetworkCheckpoint_Id, Int32>.Empty,
                       [],
                       ImmutableDictionary<ChangeSet_Id, RoamingNetworkChangeSet>.Empty);

            public Boolean TryGetCommit(RoamingNetworkCommit_Id                         CommitId,
                                        [NotNullWhen(true)] out RoamingNetworkCommit?   Commit)
            {

                if (CommitIndex.TryGetValue(CommitId, out var index))
                {
                    Commit = Commits[index];
                    return true;
                }

                Commit = null;
                return false;

            }

            /// <summary>
            /// The commit of this view, if it holds it.
            /// </summary>
            public RoamingNetworkCommit? Lookup(RoamingNetworkCommit_Id CommitId)

                => TryGetCommit(CommitId, out var commit)
                       ? commit
                       : null;

            /// <summary>
            /// The given commit and all its ancestors in this view.
            /// </summary>
            public HashSet<RoamingNetworkCommit_Id> Ancestors(RoamingNetworkCommit_Id CommitId)
            {

                var ancestors  = new HashSet<RoamingNetworkCommit_Id>();
                var pending    = new Stack<RoamingNetworkCommit_Id>([ CommitId ]);

                while (pending.TryPop(out var commitId))
                    if (CommitIndex.TryGetValue(commitId, out var index) && ancestors.Add(commitId))
                        foreach (var parentId in Commits[index].ParentIds)
                            pending.Push(parentId);

                return ancestors;

            }

            /// <summary>
            /// The commits of this view that neither are one of the given ones nor are followed by one, in their order,
            /// parents before their children; null when it holds none of the given ones.
            /// </summary>
            /// <remarks>
            /// The commits are walked from the tips and from the given ones at once, the highest revision first, as a
            /// parent has a lower revision than its children: a commit reached is one the given ones follow once one of
            /// them reached it, and the walk ends once only such commits are left. So it costs the commits missing and
            /// those of their revisions, not every commit for every given one.
            /// </remarks>
            /// <param name="Have">The commits held elsewhere, e.g. the tips and a sample of older commits.</param>
            public List<RoamingNetworkCommit>? Missing(IEnumerable<RoamingNetworkCommit_Id> Have)
            {

                var known    = new HashSet<RoamingNetworkCommit_Id>();
                var queue    = new PriorityQueue<(RoamingNetworkCommit_Id CommitId, Boolean Missing), UInt64>(Comparer<UInt64>.Create((revision1, revision2) => revision2.CompareTo(revision1)));
                var queued   = new HashSet<RoamingNetworkCommit_Id>();
                var missing  = new List<RoamingNetworkCommit>();
                var waiting  = 0;

                foreach (var commitId in Have)
                    if (CommitIndex.TryGetValue(commitId, out var index) && known.Add(commitId))
                        queue.Enqueue((commitId, false), Commits[index].Revision);

                if (known.Count == 0)
                    return null;

                foreach (var tip in Tips)
                    if (!known.Contains(tip) && queued.Add(tip))
                    {
                        queue.Enqueue((tip, true), Commits[CommitIndex[tip]].Revision);
                        waiting++;
                    }

                while (waiting > 0 && queue.TryDequeue(out var next, out _))
                {

                    var commit = Commits[CommitIndex[next.CommitId]];

                    if (next.Missing)
                        waiting--;

                    // Known: so are its parents. Reached as missing first, and known later, it is walked as known once.
                    if (known.Contains(next.CommitId))
                    {

                        if (!next.Missing)
                            foreach (var parentId in commit.ParentIds)
                                if (CommitIndex.TryGetValue(parentId, out var parentIndex) && known.Add(parentId))
                                    queue.Enqueue((parentId, false), Commits[parentIndex].Revision);

                        continue;

                    }

                    missing.Add(commit);

                    foreach (var parentId in commit.ParentIds)
                        if (CommitIndex.TryGetValue(parentId, out var parentIndex) && !known.Contains(parentId) && queued.Add(parentId))
                        {
                            queue.Enqueue((parentId, true), Commits[parentIndex].Revision);
                            waiting++;
                        }

                }

                missing.Sort((commit1, commit2) => CommitIndex[commit1.Id].CompareTo(CommitIndex[commit2.Id]));

                return missing;

            }

        }

        #endregion

        #region (private) class CalculatedVersion

        /// <summary>
        /// A version a reader calculated again.
        /// </summary>
        private sealed record CalculatedVersion(RoamingNetworkCommit_Id  CommitId,
                                                RoamingNetwork           Version);

        #endregion

        #region Data

        private ReadView readView = ReadView.Empty;

        /// <summary>
        /// The version a reader calculated last, as one calculated again is not kept in the view.
        /// </summary>
        private CalculatedVersion? lastCalculatedVersion;

        #endregion


        #region (private) View

        /// <summary>
        /// The latest view, without the lock.
        /// </summary>
        private ReadView View
            => Volatile.Read(ref readView);

        #endregion

        #region (private) TryGetVersion(View, CommitId, out RoamingNetwork, out ErrorResponse)

        /// <summary>
        /// The version of the given commit for a reader, without the lock: kept in the given view, the one a reader
        /// calculated last, or calculated again from the closest one kept on the line of its first parents, which is then
        /// the one calculated last. It is not kept in the view: the retention decides what the history keeps.
        /// </summary>
        /// <param name="View">The view of the reader.</param>
        /// <param name="CommitId">A commit of the view.</param>
        /// <param name="RoamingNetwork">Its version.</param>
        /// <param name="ErrorResponse">Why it can not be calculated, e.g. as the view does not hold the commit.</param>
        private Boolean TryGetVersion(ReadView                                  View,
                                      RoamingNetworkCommit_Id                   CommitId,
                                      [NotNullWhen(true)]  out RoamingNetwork?  RoamingNetwork,
                                      [NotNullWhen(false)] out String?          ErrorResponse)
        {

            if (View.Versions.TryGetValue(CommitId, out RoamingNetwork))
            {
                ErrorResponse = null;
                return true;
            }

            if (Volatile.Read(ref lastCalculatedVersion) is CalculatedVersion calculated &&
                calculated.CommitId == CommitId)
            {
                RoamingNetwork  = calculated.Version;
                ErrorResponse   = null;
                return true;
            }

            if (!TryCalculateVersion(CommitId, View.Versions, View.Lookup, null, out RoamingNetwork, out ErrorResponse))
                return false;

            Volatile.Write(ref lastCalculatedVersion, new CalculatedVersion(CommitId, RoamingNetwork));

            return true;

        }

        #endregion

        #region (private) Publish...(...)

        // All of them while the history is locked, right after the change of the mutable collections they follow.

        private void Publish(ReadView NewView)
            => Volatile.Write(ref readView, NewView);

        /// <summary>
        /// The given commit added at the end of the commits, with the version it leads to.
        /// </summary>
        private void PublishAddedCommit(RoamingNetworkCommit  Commit,
                                        RoamingNetwork        Version)
        {
            var view = View;
            Publish(view with { Commits      = view.Commits.    Add    (Commit),
                                CommitIndex  = view.CommitIndex.SetItem(Commit.Id, view.Commits.Count),
                                Versions     = view.Versions.   SetItem(Commit.Id, Version) });
        }

        /// <summary>
        /// The commit at the given index replaced, e.g. by itself with more signatures.
        /// </summary>
        private void PublishReplacedCommit(Int32                 Index,
                                           RoamingNetworkCommit  Commit)
        {
            var view = View;
            Publish(view with { Commits = view.Commits.SetItem(Index, Commit) });
        }

        /// <summary>
        /// All commits and kept versions anew, e.g. after compacting.
        /// </summary>
        private void PublishCommits()

            => Publish(View with { Commits      = [ .. commits ],
                                   CommitIndex  = commitIndex.ToImmutableDictionary(),
                                   Versions     = versions.   ToImmutableDictionary() });

        /// <summary>
        /// The tips anew, and the change sets waiting for signatures when the commit making them let go of some:
        /// a reader sees a change set waiting as long as the head does not include it.
        /// </summary>
        private void PublishTips()
        {

            if (!pendingChangeSetsChanged)
            {
                Publish(View with { Tips = [ .. tips ] });
                return;
            }

            pendingChangeSetsChanged = false;

            Publish(View with { Tips               = [ .. tips ],
                                PendingChangeSets  = [ .. pendingChangeSets.Values ],
                                PendingIndex       = pendingChangeSets.ToImmutableDictionary() });

        }

        /// <summary>
        /// The given versions no longer kept.
        /// </summary>
        private void PublishDroppedVersions(IEnumerable<RoamingNetworkCommit_Id> CommitIds)
        {
            var view = View;
            Publish(view with { Versions = view.Versions.RemoveRange(CommitIds) });
        }

        /// <summary>
        /// The change sets waiting for signatures anew, whenever one starts or keeps waiting, or waits no longer.
        /// </summary>
        private void PublishPendingChangeSets()

            => Publish(View with { PendingChangeSets  = [ .. pendingChangeSets.Values ],
                                   PendingIndex       = pendingChangeSets.ToImmutableDictionary() });

        private void PublishAttestations()
            => Publish(View with { Attestations = [ .. attestations ] });

        /// <summary>
        /// The given checkpoint added at the end of the checkpoints.
        /// </summary>
        private void PublishAddedCheckpoint(RoamingNetworkCheckpoint Checkpoint)
        {
            var view = View;
            Publish(view with { Checkpoints      = view.Checkpoints.    Add    (Checkpoint),
                                CheckpointIndex  = view.CheckpointIndex.SetItem(Checkpoint.Id, view.Checkpoints.Count) });
        }

        /// <summary>
        /// The checkpoint at the given index replaced, e.g. by itself with more signatures.
        /// </summary>
        private void PublishReplacedCheckpoint(Int32                     Index,
                                               RoamingNetworkCheckpoint  Checkpoint)
        {
            var view = View;
            Publish(view with { Checkpoints = view.Checkpoints.SetItem(Index, Checkpoint) });
        }

        #endregion

    }

}
