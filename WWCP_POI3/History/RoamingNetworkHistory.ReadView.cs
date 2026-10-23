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
    /// What readers of a history take without its lock: its commits, its tips, the attestations it holds and its
    /// checkpoints, as immutable collections. A commit holds the lock for all of its time, a merge or a sync to the disk
    /// included; a reader of the view never waits for it, and reads one state of the history, the latest published.
    /// </summary>
    /// <remarks>
    /// Every change of these collections publishes a new view while the history is locked, the commits of a new head
    /// before the head itself: a commit a reader sees as the head is in the view it takes afterwards. The mutable
    /// collections stay for the writers, who change them under the lock as before; the view follows each change.
    /// </remarks>
    public sealed partial class RoamingNetworkHistory
    {

        #region (private) class ReadView

        /// <summary>
        /// One state of the commits, tips, attestations and checkpoints of a history.
        /// </summary>
        private sealed record ReadView(ImmutableList<RoamingNetworkCommit>                       Commits,
                                       ImmutableDictionary<RoamingNetworkCommit_Id, Int32>       CommitIndex,
                                       ImmutableArray<RoamingNetworkCommit_Id>                   Tips,
                                       ImmutableArray<RoamingNetworkAttestation>                 Attestations,
                                       ImmutableList<RoamingNetworkCheckpoint>                   Checkpoints,
                                       ImmutableDictionary<RoamingNetworkCheckpoint_Id, Int32>   CheckpointIndex)
        {

            public static ReadView Empty { get; }

                = new ([],
                       ImmutableDictionary<RoamingNetworkCommit_Id, Int32>.Empty,
                       [],
                       [],
                       [],
                       ImmutableDictionary<RoamingNetworkCheckpoint_Id, Int32>.Empty);

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

        }

        #endregion

        #region Data

        private ReadView readView = ReadView.Empty;

        #endregion


        #region (private) View

        /// <summary>
        /// The latest view, without the lock.
        /// </summary>
        private ReadView View
            => Volatile.Read(ref readView);

        #endregion

        #region (private) Publish...(...)

        // All of them while the history is locked, right after the change of the mutable collections they follow.

        private void Publish(ReadView NewView)
            => Volatile.Write(ref readView, NewView);

        /// <summary>
        /// The given commit added at the end of the commits.
        /// </summary>
        private void PublishAddedCommit(RoamingNetworkCommit Commit)
        {
            var view = View;
            Publish(view with { Commits      = view.Commits.    Add    (Commit),
                                CommitIndex  = view.CommitIndex.SetItem(Commit.Id, view.Commits.Count) });
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
        /// All commits anew, e.g. after compacting.
        /// </summary>
        private void PublishCommits()

            => Publish(View with { Commits      = [ .. commits ],
                                   CommitIndex  = commitIndex.ToImmutableDictionary() });

        private void PublishTips()
            => Publish(View with { Tips = [ .. tips ] });

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
