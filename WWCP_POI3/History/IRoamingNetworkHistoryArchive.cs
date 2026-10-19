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
using System.Globalization;

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Where a history keeps the commits it drops when compacting, so that they stay provable (SPECIFICATION.md, section 10.6):
    /// one segment per compaction, from the old root to the new one, in their order, never changed once written.
    /// </summary>
    public interface IRoamingNetworkHistoryArchive : IDisposable
    {

        /// <summary>
        /// What the segments hold, in their order.
        /// </summary>
        IReadOnlyList<RoamingNetworkArchiveSegmentInfo> Segments { get; }

        /// <summary>
        /// Keep the given segment durably, before the history drops its commits. A segment of the same number replaces
        /// the one written before, e.g. when a compaction failed after it was archived and is done again.
        /// </summary>
        /// <param name="Segment">The segment.</param>
        void Append(RoamingNetworkArchiveSegment Segment);

        /// <summary>
        /// The given segment completely, or null when there is none of this number.
        /// </summary>
        /// <param name="Number">The number of the segment.</param>
        RoamingNetworkArchiveSegment? Load(UInt32 Number);

    }


    /// <summary>
    /// What a segment of an archive holds: the commits from its root, the root of the history before the compaction,
    /// to its end, the new root, in their order.
    /// </summary>
    /// <param name="Number">The number of the segment: 1 for the first one.</param>
    /// <param name="RootId">The root of the history before the compaction; its version is the base of the segment.</param>
    /// <param name="EndId">The new root of the history, the last commit of the segment.</param>
    /// <param name="FromRevision">The revision of the root.</param>
    /// <param name="ToRevision">The revision of the end.</param>
    /// <param name="CommitIds">The commits of the segment after its root, in their order, the end last.</param>
    /// <param name="ArchivedAt">When the segment was archived.</param>
    public sealed record RoamingNetworkArchiveSegmentInfo(UInt32                                  Number,
                                                          RoamingNetworkCommit_Id                 RootId,
                                                          RoamingNetworkCommit_Id                 EndId,
                                                          UInt64                                  FromRevision,
                                                          UInt64                                  ToRevision,
                                                          ImmutableArray<RoamingNetworkCommit_Id> CommitIds,
                                                          DateTimeOffset                          ArchivedAt)
    {

        #region ToJSON(IncludeCommitIds = true)

        /// <summary>
        /// Return the JSON representation of this information, with or without the commit identifications.
        /// </summary>
        public JObject ToJSON(Boolean IncludeCommitIds = true)
        {

            var json = new JObject(
                           new JProperty("number",        Number),
                           new JProperty("rootId",        RootId.ToString()),
                           new JProperty("endId",         EndId. ToString()),
                           new JProperty("fromRevision",  FromRevision),
                           new JProperty("toRevision",    ToRevision),
                           new JProperty("size",          CommitIds.Length),
                           new JProperty("archivedAt",    ArchivedAt.ToISO8601())
                       );

            if (IncludeCommitIds)
                json.Add("commitIds", new JArray(CommitIds.Select(commitId => commitId.ToString())));

            return json;

        }

        #endregion

        #region (static) TryParse(JSON, out Info, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of the information of a segment, with its commit identifications.
        /// </summary>
        public static Boolean TryParse(JObject                                                     JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkArchiveSegmentInfo?  Info,
                                       [NotNullWhen(false)] out String?                            ErrorResponse)
        {

            Info = null;

            try
            {

                if (JSON["number"] is not JValue { Type: JTokenType.Integer } number ||
                    !RoamingNetworkCommit_Id.TryParse(JSON["rootId"]?.Value<String>() ?? "", out var rootId) ||
                    !RoamingNetworkCommit_Id.TryParse(JSON["endId"]?. Value<String>() ?? "", out var endId)  ||
                    JSON["fromRevision"] is not JValue { Type: JTokenType.Integer } fromRevision ||
                    JSON["toRevision"]   is not JValue { Type: JTokenType.Integer } toRevision   ||
                    JSON["commitIds"]    is not JArray commitIdsJSON ||
                    !DateTimeOffset.TryParse(JSON["archivedAt"]?.Value<String>(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var archivedAt))
                {
                    ErrorResponse = "Invalid information of an archive segment!";
                    return false;
                }

                var commitIds = ImmutableArray.CreateBuilder<RoamingNetworkCommit_Id>(commitIdsJSON.Count);

                foreach (var commitIdJSON in commitIdsJSON)
                {

                    if (!RoamingNetworkCommit_Id.TryParse(commitIdJSON.Value<String>() ?? "", out var commitId))
                    {
                        ErrorResponse = "Invalid commit identification in an archive segment!";
                        return false;
                    }

                    commitIds.Add(commitId);

                }

                Info = new RoamingNetworkArchiveSegmentInfo(number.Value<UInt32>(),
                                                            rootId,
                                                            endId,
                                                            fromRevision.Value<UInt64>(),
                                                            toRevision.Value<UInt64>(),
                                                            commitIds.MoveToImmutable(),
                                                            archivedAt);

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                Info           = null;
                ErrorResponse  = "Invalid information of an archive segment: " + e.Message;
                return false;
            }

        }

        #endregion

    }


    /// <summary>
    /// One segment of an archive: the snapshot of its root, without runtime status, and its commits after it,
    /// so that every version from its root to its end can be calculated again from the segment alone.
    /// </summary>
    /// <param name="Info">What it holds.</param>
    /// <param name="Base">The snapshot of its root: { commit, roamingNetwork, effectiveLastChanges? }.</param>
    /// <param name="Commits">Its commits after the root, in their order, the end last.</param>
    public sealed record RoamingNetworkArchiveSegment(RoamingNetworkArchiveSegmentInfo     Info,
                                                      JObject                              Base,
                                                      IReadOnlyList<RoamingNetworkCommit>  Commits)
    {

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this segment: { info, base, commits }.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty("info",     Info.ToJSON()),
                   new JProperty("base",     Base.DeepClone()),
                   new JProperty("commits",  new JArray(Commits.Select(commit => commit.ToJSON())))
               );

        #endregion

        #region Check(out ErrorResponse)

        /// <summary>
        /// Whether the commits of this segment are the ones its information names, in its order, and its base is of its root.
        /// Every commit checked its own identification when it was read.
        /// </summary>
        public Boolean Check([NotNullWhen(false)] out String? ErrorResponse)
        {

            if (Base["commit"]?["@id"]?.Value<String>() != Info.RootId.ToString())
            {
                ErrorResponse = $"The base of the archive segment {Info.Number} is not of its root '{Info.RootId}'!";
                return false;
            }

            if (!Commits.Select(commit => commit.Id).SequenceEqual(Info.CommitIds) ||
                Commits.Count == 0 ||
                Commits[^1].Id != Info.EndId)
            {
                ErrorResponse = $"The commits of the archive segment {Info.Number} are not the ones it names!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
