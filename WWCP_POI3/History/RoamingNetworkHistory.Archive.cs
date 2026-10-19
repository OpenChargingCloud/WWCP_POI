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

using System.Diagnostics.CodeAnalysis;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The archive of a history: the commits it dropped when compacting, one segment per compaction,
    /// still readable and provable (SPECIFICATION.md, section 10.6).
    /// </summary>
    public sealed partial class RoamingNetworkHistory
    {

        #region Data

        private Dictionary<RoamingNetworkCommit_Id, UInt32>?  archiveIndex;
        private RoamingNetworkArchiveSegment?                 lastArchiveSegment;

        #endregion

        #region Properties

        /// <summary>
        /// Where this history keeps the commits it drops when compacting; without one, they are gone.
        /// </summary>
        public IRoamingNetworkHistoryArchive?  Archive
        {
            get => Volatile.Read(ref archive);
            set
            {
                lock (historyLock)
                {
                    Volatile.Write(ref archive, value);
                    archiveIndex        = null;
                    lastArchiveSegment  = null;
                }
            }
        }

        private IRoamingNetworkHistoryArchive? archive;

        /// <summary>
        /// What the segments of the archive hold, in their order; none without an archive.
        /// </summary>
        public IReadOnlyList<RoamingNetworkArchiveSegmentInfo>  ArchiveSegments
            => Archive?.Segments ?? [];

        #endregion


        #region TryGetArchivedCommit(CommitId, out Commit)

        /// <summary>
        /// Try to return the given commit from the archive: one this history dropped when compacting.
        /// </summary>
        /// <param name="CommitId">A commit identification.</param>
        /// <param name="Commit">The commit.</param>
        public Boolean TryGetArchivedCommit(RoamingNetworkCommit_Id                         CommitId,
                                            [NotNullWhen(true)] out RoamingNetworkCommit?   Commit)
        {

            lock (historyLock)
            {

                Commit = null;

                if (!TryLoadArchiveSegmentOf(CommitId, out var segment))
                    return false;

                Commit = segment.Commits.FirstOrDefault(commit => commit.Id == CommitId);

                return Commit is not null;

            }

        }

        #endregion

        #region TryGetArchiveSegment(Number, out Segment)

        /// <summary>
        /// Try to return the given segment of the archive completely.
        /// </summary>
        public Boolean TryGetArchiveSegment(UInt32                                                Number,
                                            [NotNullWhen(true)] out RoamingNetworkArchiveSegment? Segment)
        {

            Segment = Archive?.Load(Number);

            return Segment is not null;

        }

        #endregion


        #region (private) ArchiveDropped(NewRoot, Dropped)

        /// <summary>
        /// Keep the commits a compaction drops in a new segment of the archive, while the history is locked,
        /// before the store forgets them: from the root so far to the new root. Failing, it fails the compaction.
        /// </summary>
        private void ArchiveDropped(RoamingNetworkCommit              NewRoot,
                                    HashSet<RoamingNetworkCommit_Id>  Dropped)
        {

            var currentArchive = Archive;

            if (currentArchive is null || Dropped.Count == 0)
                return;

            var oldRoot = commits[0];

            if (!TryGetVersion(oldRoot.Id, out var rootVersion, out var versionError))
                throw new InvalidOperationException("The version of the root can not be archived: " + versionError);

            var segmentCommits  = commits.Where(commit => Dropped.Contains(commit.Id) && commit.Id != oldRoot.Id).
                                          Append(NewRoot).
                                          ToList();

            var segments        = currentArchive.Segments;

            // A compaction done again, after it failed once it was archived, writes the same segment again.
            var number          = segments.Count > 0 && segments[^1].RootId == oldRoot.Id
                                      ? segments[^1].Number
                                      : (UInt32) segments.Count + 1;

            var baseJSON        = SnapshotJSON(oldRoot, rootVersion, rootVersion);
            baseJSON.Remove("runtimeStatus");

            currentArchive.Append(new RoamingNetworkArchiveSegment(
                                      new RoamingNetworkArchiveSegmentInfo(number,
                                                                           oldRoot.Id,
                                                                           NewRoot.Id,
                                                                           oldRoot.Revision,
                                                                           NewRoot.Revision,
                                                                           [ .. segmentCommits.Select(commit => commit.Id) ],
                                                                           Timestamp.Now),
                                      baseJSON,
                                      segmentCommits
                                  ));

            archiveIndex        = null;
            lastArchiveSegment  = null;

        }

        #endregion

        #region (private) TryLoadArchiveSegmentOf(CommitId, out Segment)

        /// <summary>
        /// The segment of the archive holding the given commit, while the history is locked; the last one loaded is kept.
        /// </summary>
        private Boolean TryLoadArchiveSegmentOf(RoamingNetworkCommit_Id                                CommitId,
                                                [NotNullWhen(true)] out RoamingNetworkArchiveSegment?  Segment)
        {

            Segment = null;

            var currentArchive = Archive;

            if (currentArchive is null)
                return false;

            if (archiveIndex is null)
            {

                archiveIndex = [];

                foreach (var info in currentArchive.Segments)
                    foreach (var commitId in info.CommitIds)
                        archiveIndex[commitId] = info.Number;

            }

            if (!archiveIndex.TryGetValue(CommitId, out var number))
                return false;

            if (lastArchiveSegment?.Info.Number != number)
                lastArchiveSegment = currentArchive.Load(number);

            Segment = lastArchiveSegment;

            return Segment is not null;

        }

        #endregion

        #region (private) ArchiveLookup(Segment)

        /// <summary>
        /// The commits of the given segment of the archive, its root included, by their identifications.
        /// </summary>
        private static Func<RoamingNetworkCommit_Id, RoamingNetworkCommit?> ArchiveLookup(RoamingNetworkArchiveSegment Segment)
        {

            var lookup = Segment.Commits.ToDictionary(commit => commit.Id);

            if (Segment.Base["commit"] is Newtonsoft.Json.Linq.JObject rootJSON &&
                RoamingNetworkCommit.TryParse(rootJSON, out var root, out _))
            {
                lookup[root.Id] = root;
            }

            return commitId => lookup.TryGetValue(commitId, out var commit) ? commit : null;

        }

        #endregion

    }

}
