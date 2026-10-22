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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A slim notice of a new commit: there is now a commit for its parents, with its description.
    /// Whoever is interested fetches its metadata or the complete commit afterwards.
    /// </summary>
    public sealed record RoamingNetworkCommitAnnouncement
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/commitAnnouncement/v1";

        #endregion

        #region Properties

        #region (static) class PropertyKeys

        /// <summary>
        /// Property keys for JSON and CBOR (de-)serialization.
        /// </summary>
        public static class PropertyKeys
        {

            /// <summary>
            /// The property of its JSON-LD context, written unless it is embedded.
            /// </summary>
            public const String  Context      = "@context";

            /// <summary>
            /// The property of when the history took the commit.
            /// </summary>
            public const String  Timestamp    = "timestamp";

            /// <summary>
            /// The property of the identification of the commit.
            /// </summary>
            public const String  CommitId     = "commitId";

            /// <summary>
            /// The property of the identifications of its parents.
            /// </summary>
            public const String  ParentIds    = "parentIds";

            /// <summary>
            /// The property of the revision the commit leads to.
            /// </summary>
            public const String  Revision     = "revision";

            /// <summary>
            /// The property of the description of its change set.
            /// </summary>
            public const String  Description  = "description";

        }

        #endregion


        /// <summary>
        /// The complete commit, for whoever wants more than the notice. Not part of its JSON.
        /// </summary>
        public RoamingNetworkCommit                    Commit         { get; }

        /// <summary>
        /// When this history took the commit, to the millisecond.
        /// </summary>
        public DateTimeOffset                          Timestamp      { get; }

        /// <summary>
        /// The identification of the commit.
        /// </summary>
        public RoamingNetworkCommit_Id                 CommitId       { get; }

        /// <summary>
        /// The identifications of its parents: one, or more for a merge commit some day.
        /// </summary>
        public ImmutableArray<RoamingNetworkCommit_Id> ParentIds      { get; }

        /// <summary>
        /// The revision the commit leads to.
        /// </summary>
        public UInt64                                  Revision       { get; }

        /// <summary>
        /// The description of its change set.
        /// </summary>
        public ImmutableI18NString                     Description    { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Announce the given commit.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        /// <param name="Timestamp">When this history took it.</param>
        public RoamingNetworkCommitAnnouncement(RoamingNetworkCommit  Commit,
                                                DateTimeOffset        Timestamp)
        {

            this.Commit       = Commit;
            this.Timestamp    = DateTimeOffset.FromUnixTimeMilliseconds(Timestamp.ToUnixTimeMilliseconds());
            this.CommitId     = Commit.Id;
            this.ParentIds    = Commit.ParentIds;
            this.Revision     = Commit.Revision;
            this.Description  = Commit.ChangeSet?.Description ?? ImmutableI18NString.Empty;

        }

        #endregion


        #region ToJSON(Embedded = false, CustomCommitAnnouncementSerializer = null)

        /// <summary>
        /// Return a JSON representation of this announcement.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomCommitAnnouncementSerializer">An optional custom serializer of commit announcements.</param>
        public JObject ToJSON(Boolean                                                             Embedded                             = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkCommitAnnouncement>?  CustomCommitAnnouncementSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,    JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.Timestamp,  Timestamp.ToISO8601()),
                                 new JProperty(PropertyKeys.CommitId,   CommitId.ToString()),
                                 new JProperty(PropertyKeys.ParentIds,  new JArray(ParentIds.Select(parentId => parentId.ToString()))),
                                 new JProperty(PropertyKeys.Revision,   Revision)

                       );

            if (Description.IsNotNullOrEmpty())
                json.Add(PropertyKeys.Description, Description.ToJSON());

            return CustomCommitAnnouncementSerializer is not null
                       ? CustomCommitAnnouncementSerializer(this, json)
                       : json;

        }

        #endregion

    }

}
