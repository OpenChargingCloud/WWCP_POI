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

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/archiveSegmentInfo/v1";

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context       = "@context";

            /// <summary>
            /// The property of the number of the segment.
            /// </summary>
            public const String  Number        = "number";

            /// <summary>
            /// The property of the root of the segment.
            /// </summary>
            public const String  RootId        = "rootId";

            /// <summary>
            /// The property of the end of the segment.
            /// </summary>
            public const String  EndId         = "endId";

            /// <summary>
            /// The property of the revision of the root.
            /// </summary>
            public const String  FromRevision  = "fromRevision";

            /// <summary>
            /// The property of the revision of the end.
            /// </summary>
            public const String  ToRevision    = "toRevision";

            /// <summary>
            /// The property of the number of commits of the segment after its root.
            /// </summary>
            public const String  Size          = "size";

            /// <summary>
            /// The property of when the segment was archived.
            /// </summary>
            public const String  ArchivedAt    = "archivedAt";

            /// <summary>
            /// The property of the commits of the segment after its root.
            /// </summary>
            public const String  CommitIds     = "commitIds";

        }

        #endregion

        #region ToJSON(Embedded = false, IncludeCommitIds = true, CustomArchiveSegmentInfoSerializer = null)

        /// <summary>
        /// Return the JSON representation of this information, with or without the commit identifications.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure, or stored.</param>
        /// <param name="IncludeCommitIds">Whether to include the commit identifications.</param>
        /// <param name="CustomArchiveSegmentInfoSerializer">An optional custom serializer of the information of archive segments.</param>
        public JObject ToJSON(Boolean                                                             Embedded                             = false,
                              Boolean                                                             IncludeCommitIds                     = true,
                              CustomJObjectSerializerDelegate<RoamingNetworkArchiveSegmentInfo>?  CustomArchiveSegmentInfoSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,       JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Number,        Number),
                                 new JProperty(PropertyKeys.RootId,        RootId.ToString()),
                                 new JProperty(PropertyKeys.EndId,         EndId. ToString()),
                                 new JProperty(PropertyKeys.FromRevision,  FromRevision),
                                 new JProperty(PropertyKeys.ToRevision,    ToRevision),
                                 new JProperty(PropertyKeys.Size,          CommitIds.Length),
                                 new JProperty(PropertyKeys.ArchivedAt,    ArchivedAt.ToISO8601())

                       );

            if (IncludeCommitIds)
                json.Add(PropertyKeys.CommitIds, new JArray(CommitIds.Select(commitId => commitId.ToString())));

            return CustomArchiveSegmentInfoSerializer is not null
                       ? CustomArchiveSegmentInfoSerializer(this, json)
                       : json;

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

                if (JSON[PropertyKeys.Number] is not JValue { Type: JTokenType.Integer } number ||
                    !RoamingNetworkCommit_Id.TryParse(JSON[PropertyKeys.RootId]?.Value<String>() ?? "", out var rootId) ||
                    !RoamingNetworkCommit_Id.TryParse(JSON[PropertyKeys.EndId]?. Value<String>() ?? "", out var endId)  ||
                    JSON[PropertyKeys.FromRevision] is not JValue { Type: JTokenType.Integer } fromRevision ||
                    JSON[PropertyKeys.ToRevision]   is not JValue { Type: JTokenType.Integer } toRevision   ||
                    JSON[PropertyKeys.CommitIds]    is not JArray commitIdsJSON ||
                    !DateTimeOffset.TryParse(JSON[PropertyKeys.ArchivedAt]?.Value<String>(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var archivedAt))
                {
                    ErrorResponse = "Invalid information of an archive segment!";
                    return false;
                }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the information of an archive segment must be '{JSONLDContext}'!";
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

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/archiveSegment/v1";

        #endregion

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context  = "@context";

            /// <summary>
            /// The property of what it holds.
            /// </summary>
            public const String  Info     = "info";

            /// <summary>
            /// The property of the snapshot of its root.
            /// </summary>
            public const String  Base     = "base";

            /// <summary>
            /// The property of its commits after the root.
            /// </summary>
            public const String  Commits  = "commits";

        }

        #endregion

        #region ToJSON(Embedded = false, CustomArchiveSegmentSerializer = null)

        /// <summary>
        /// Return the JSON representation of this segment: { info, base, commits }.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure, or stored.</param>
        /// <param name="CustomArchiveSegmentSerializer">An optional custom serializer of archive segments.</param>
        public JObject ToJSON(Boolean                                                         Embedded                         = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkArchiveSegment>?  CustomArchiveSegmentSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,  JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Info,     Info.ToJSON(Embedded: true)),
                                 new JProperty(PropertyKeys.Base,     Base.DeepClone()),
                                 new JProperty(PropertyKeys.Commits,  new JArray(Commits.Select(commit => commit.ToJSON(Embedded: true))))

                       );

            return CustomArchiveSegmentSerializer is not null
                       ? CustomArchiveSegmentSerializer(this, json)
                       : json;

        }

        #endregion

        #region Check(out ErrorResponse)

        /// <summary>
        /// Whether the commits of this segment are the ones its information names, in its order, and its base is of its root.
        /// Every commit checked its own identification when it was read.
        /// </summary>
        public Boolean Check([NotNullWhen(false)] out String? ErrorResponse)
        {

            if (Base[RoamingNetworkHistory.PropertyKeys.Commit]?[RoamingNetworkCommit.PropertyKeys.Id]?.Value<String>() != Info.RootId.ToString())
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
