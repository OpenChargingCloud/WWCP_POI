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
    /// Read without the lock: the archive keeps its segments in order itself, and a change of it, by a compaction or
    /// another archive, makes the readers index it anew.
    /// </summary>
    public sealed partial class RoamingNetworkHistory
    {

        #region (private) class ArchiveIndex, LoadedArchiveSegment

        /// <summary>
        /// Which segment of an archive holds which commit, as long as the archive did not change.
        /// </summary>
        private sealed record ArchiveIndex(IRoamingNetworkHistoryArchive                      Archive,
                                           Int64                                              Generation,
                                           IReadOnlyDictionary<RoamingNetworkCommit_Id, UInt32>  Numbers);

        /// <summary>
        /// The commits of a segment of an archive loaded last, by their identifications.
        /// </summary>
        private sealed record LoadedArchiveSegment(IRoamingNetworkHistoryArchive                          Archive,
                                                   Int64                                                  Generation,
                                                   UInt32                                                 Number,
                                                   Func<RoamingNetworkCommit_Id, RoamingNetworkCommit?>   Lookup);

        #endregion

        #region Data

        private ArchiveIndex?          archiveIndex;
        private LoadedArchiveSegment?  lastArchiveSegment;

        /// <summary>
        /// Counts the changes of the archive: an index or a segment of an older one is not taken.
        /// </summary>
        private Int64                  archiveGeneration;

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
                    ForgetArchiveIndex();
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
        /// Read without the lock.
        /// </summary>
        /// <param name="CommitId">A commit identification.</param>
        /// <param name="Commit">The commit.</param>
        public Boolean TryGetArchivedCommit(RoamingNetworkCommit_Id                         CommitId,
                                            [NotNullWhen(true)] out RoamingNetworkCommit?   Commit)
        {

            Commit = null;

            if (!TryGetArchiveLookupOf(CommitId, out var lookup))
                return false;

            // Also the root of the first segment, which only its base holds: the genesis, or the commit of the snapshot
            // the history started from.
            Commit = lookup(CommitId);

            return Commit is not null;

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

            var baseJSON        = SnapshotJSON(oldRoot, rootVersion, StatusOf: null);

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

            ForgetArchiveIndex();

        }

        #endregion

        #region (private) ForgetArchiveIndex()

        /// <summary>
        /// Let go of the index of the archive and of the segment loaded last, while the history is locked, once the archive
        /// changed: a reader that took them before keeps them, but does not leave them for the next one.
        /// </summary>
        private void ForgetArchiveIndex()
        {
            Interlocked.Increment(ref archiveGeneration);
            Volatile.Write(ref archiveIndex,       null);
            Volatile.Write(ref lastArchiveSegment, null);
        }

        #endregion

        #region (private) TryGetArchiveLookupOf(CommitId, out Lookup)

        /// <summary>
        /// The commits of the segment of the archive holding the given commit, its root included, by their
        /// identifications; without the lock. The index of the archive and the segment loaded last are kept.
        /// </summary>
        private Boolean TryGetArchiveLookupOf(RoamingNetworkCommit_Id                                                       CommitId,
                                              [NotNullWhen(true)] out Func<RoamingNetworkCommit_Id, RoamingNetworkCommit?>?  Lookup)
        {

            Lookup = null;

            var generation      = Interlocked.Read(ref archiveGeneration);
            var currentArchive  = Archive;

            if (currentArchive is null)
                return false;

            var index = Volatile.Read(ref archiveIndex);

            if (index is null || index.Generation != generation || index.Archive != currentArchive)
            {

                var numbers = new Dictionary<RoamingNetworkCommit_Id, UInt32>();

                foreach (var info in currentArchive.Segments)
                {

                    foreach (var commitId in info.CommitIds)
                        numbers[commitId] = info.Number;

                    // The root of every later segment is the end of the one before.
                    numbers.TryAdd(info.RootId, info.Number);

                }

                index = new ArchiveIndex(currentArchive, generation, numbers);

                // Left for the next reader only while the archive did not change meanwhile.
                if (Interlocked.Read(ref archiveGeneration) == generation)
                    Volatile.Write(ref archiveIndex, index);

            }

            if (!index.Numbers.TryGetValue(CommitId, out var number))
                return false;

            var loaded = Volatile.Read(ref lastArchiveSegment);

            if (loaded is null || loaded.Generation != generation || loaded.Archive != currentArchive || loaded.Number != number)
            {

                if (currentArchive.Load(number) is not RoamingNetworkArchiveSegment segment)
                    return false;

                loaded = new LoadedArchiveSegment(currentArchive, generation, number, ArchiveLookup(segment));

                if (Interlocked.Read(ref archiveGeneration) == generation)
                    Volatile.Write(ref lastArchiveSegment, loaded);

            }

            Lookup = loaded.Lookup;
            return true;

        }

        #endregion

        #region (private) ArchiveLookup(Segment)

        /// <summary>
        /// The commits of the given segment of the archive, its root included, by their identifications.
        /// </summary>
        private static Func<RoamingNetworkCommit_Id, RoamingNetworkCommit?> ArchiveLookup(RoamingNetworkArchiveSegment Segment)
        {

            var lookup = Segment.Commits.ToDictionary(commit => commit.Id);

            if (Segment.Base[PropertyKeys.Commit] is Newtonsoft.Json.Linq.JObject rootJSON &&
                RoamingNetworkCommit.TryParse(rootJSON, out var root, out _))
            {
                lookup[root.Id] = root;
            }

            return commitId => lookup.TryGetValue(commitId, out var commit) ? commit : null;

        }

        #endregion

    }

}
