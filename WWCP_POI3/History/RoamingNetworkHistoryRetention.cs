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

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What a history keeps.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Versions:</b> a history keeps the version of its root, of its tips, of every VersionCheckpointInterval-th revision
    /// and of its RecentVersions latest commits; any other one is calculated again when needed, from the closest one kept
    /// on the line of its first parents, e.g. to commit a change set made for it, or as a merge base.
    /// </para>
    /// <para>
    /// <b>Commits:</b> a history drops the commits before a new root when compacted: the latest commit all tips follow
    /// that is at least RetainCommits revisions behind the head, and which every commit kept follows.
    /// A server knowing only older commits starts again from a snapshot, a branch older than the root can no longer
    /// be appended, and a change set made for an older version no longer fits.
    /// </para>
    /// </remarks>
    public sealed record RoamingNetworkHistoryRetention
    {

        /// <summary>
        /// Keep everything: all versions, all commits.
        /// </summary>
        public static RoamingNetworkHistoryRetention  KeepEverything  { get; } = new () {
                                                                                   VersionCheckpointInterval  = 1,
                                                                                   RecentVersions             = Int32.MaxValue,
                                                                                   RetainCommits              = UInt64.MaxValue,
                                                                                   CompactAfterLogEntries     = Int64.MaxValue
                                                                               };

        /// <summary>
        /// The versions of every this many revisions are kept. Default: 100.
        /// </summary>
        public UInt64  VersionCheckpointInterval  { get; init; } = 100;

        /// <summary>
        /// The versions of this many latest commits are kept. Default: 100.
        /// </summary>
        public Int32   RecentVersions             { get; init; } = 100;

        /// <summary>
        /// The commits of at least this many revisions behind the head are kept when compacting. Default: 1000.
        /// </summary>
        public UInt64  RetainCommits              { get; init; } = 1000;

        /// <summary>
        /// A history with a store compacts itself once its log has this many entries. Default: 100 000.
        /// </summary>
        public Int64   CompactAfterLogEntries     { get; init; } = 100_000;

    }

}
