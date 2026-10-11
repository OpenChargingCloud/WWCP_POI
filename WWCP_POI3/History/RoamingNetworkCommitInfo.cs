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
    /// The metadata of a commit, without its changes: small enough to list a history
    /// and to decide which commits to fetch completely.
    /// </summary>
    public sealed record RoamingNetworkCommitInfo
    {

        #region Properties

        /// <summary>
        /// The identification of the commit.
        /// </summary>
        public RoamingNetworkCommit_Id         Id                    { get; }

        /// <summary>
        /// The identification of the parent commit, or null for a genesis commit.
        /// </summary>
        public RoamingNetworkCommit_Id?        ParentId              { get; }

        /// <summary>
        /// The revision the commit leads to.
        /// </summary>
        public UInt64                          Revision              { get; }

        /// <summary>
        /// The ETags of the version the commit leads to.
        /// </summary>
        public ImmutableArray<ETag>            ETags                 { get; }

        /// <summary>
        /// The identification of the applied change set, or null for a genesis commit.
        /// </summary>
        public ChangeSet_Id?                   ChangeSetId           { get; }

        /// <summary>
        /// The commit timestamp of the applied change set, or null for a genesis commit.
        /// </summary>
        public DateTimeOffset?                 CreatedAt             { get; }

        /// <summary>
        /// The description of the applied change set.
        /// </summary>
        public ImmutableI18NString             Description           { get; }

        /// <summary>
        /// The number of changes of the applied change set.
        /// </summary>
        public Int32                           NumberOfChanges       { get; }

        /// <summary>
        /// The key identifications of the signatures of the applied change set.
        /// </summary>
        public ImmutableArray<String>          SignatureKeyIds       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// The metadata of the given commit.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        public RoamingNetworkCommitInfo(RoamingNetworkCommit Commit)
        {

            this.Id               = Commit.Id;
            this.ParentId         = Commit.ParentId;
            this.Revision         = Commit.Revision;
            this.ETags            = Commit.ETags;
            this.ChangeSetId      = Commit.ChangeSet?.Id;
            this.CreatedAt        = Commit.ChangeSet?.CreatedAt;
            this.Description      = Commit.ChangeSet?.Description ?? ImmutableI18NString.Empty;
            this.NumberOfChanges  = Commit.ChangeSet?.Changes.Length ?? 0;
            this.SignatureKeyIds  = Commit.ChangeSet?.Signatures.Select(signature => signature.KeyId).ToImmutableArray() ?? [];

        }

        #endregion


        #region ToJSON()

        /// <summary>
        /// Return a JSON representation of these commit metadata.
        /// </summary>
        public JObject ToJSON()
        {

            var json = new JObject(
                           new JProperty("@id",              Id.ToString()),
                           new JProperty("revision",         Revision),
                           new JProperty("eTags",            new JArray(ETags.Select(eTag => eTag.ToJSON()))),
                           new JProperty("numberOfChanges",  NumberOfChanges)
                       );

            if (ParentId.HasValue)
                json.Add("parentId",         ParentId.Value.ToString());

            if (ChangeSetId.HasValue)
                json.Add("changeSetId",      ChangeSetId.Value.ToString());

            if (CreatedAt.HasValue)
                json.Add("createdAt",        CreatedAt.Value.ToISO8601());

            if (Description.IsNotNullOrEmpty())
                json.Add("description",      Description.ToJSON());

            if (SignatureKeyIds.Length > 0)
                json.Add("signatureKeyIds",  new JArray(SignatureKeyIds));

            return json;

        }

        #endregion

        #region ToCBOR()

        /// <summary>
        /// Return a CBOR representation of these commit metadata: the keys and values of the JSON representation.
        /// </summary>
        public CBORValue ToCBOR()

            => CBORJSON.ToCBOR(ToJSON());

        #endregion

    }

}
