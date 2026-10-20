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
    /// A proof that a commit is one of the commits of a checkpoint (SPECIFICATION.md, section 10.4):
    /// its leaf { commitId, eTags, revision } with its index and audit path in the tree of the checkpoint.
    /// </summary>
    public sealed class RoamingNetworkCheckpointProof
    {

        #region Properties

        /// <summary>
        /// The checkpoint the commit is proved to be part of.
        /// </summary>
        public RoamingNetworkCheckpoint_Id  CheckpointId    { get; }

        /// <summary>
        /// The commit.
        /// </summary>
        public RoamingNetworkCommit_Id      CommitId        { get; }

        /// <summary>
        /// The ETags of the version of the commit.
        /// </summary>
        public ImmutableArray<ETag>         ETags           { get; }

        /// <summary>
        /// The revision of the commit.
        /// </summary>
        public UInt64                       Revision        { get; }

        /// <summary>
        /// The index of the leaf of the commit.
        /// </summary>
        public UInt64                       Index           { get; }

        /// <summary>
        /// The number of commits of the checkpoint.
        /// </summary>
        public UInt64                       Size            { get; }

        /// <summary>
        /// The audit path of the leaf, from the bottom up.
        /// </summary>
        public ImmutableArray<Byte[]>       AuditPath       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a proof of a commit.
        /// </summary>
        public RoamingNetworkCheckpointProof(RoamingNetworkCheckpoint_Id  CheckpointId,
                                             RoamingNetworkCommit_Id      CommitId,
                                             ImmutableArray<ETag>         ETags,
                                             UInt64                       Revision,
                                             UInt64                       Index,
                                             UInt64                       Size,
                                             IEnumerable<Byte[]>          AuditPath)
        {

            this.CheckpointId  = CheckpointId;
            this.CommitId      = CommitId;
            this.ETags         = ETags.IsDefault ? [] : ETags;
            this.Revision      = Revision;
            this.Index         = Index;
            this.Size          = Size;
            this.AuditPath     = [ .. AuditPath ];

        }

        #endregion


        #region Verify(Checkpoint, out ErrorResponse)

        /// <summary>
        /// Whether this proof proves its commit to be one of the commits of the given checkpoint.
        /// Whether the checkpoint carries the signatures a verifier requires is to be checked as well.
        /// </summary>
        /// <param name="Checkpoint">The checkpoint.</param>
        /// <param name="ErrorResponse">Why not, if not.</param>
        public Boolean Verify(RoamingNetworkCheckpoint          Checkpoint,
                              [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (Checkpoint.Id != CheckpointId)
            {
                ErrorResponse = $"The proof is for the checkpoint '{CheckpointId}', not for '{Checkpoint.Id}'!";
                return false;
            }

            if (Checkpoint.CommitsSize != Size)
            {
                ErrorResponse = $"The checkpoint has {Checkpoint.CommitsSize} commits, not {Size}!";
                return false;
            }

            if (!POIMerkleTree.TryCalculateRootOfLeafHash(Checkpoint.IdentificationHash,
                                                          POIMerkleTree.LeafHash(Checkpoint.IdentificationHash, RoamingNetworkCheckpoint.Leaf(CommitId, ETags, Revision)),
                                                          Index,
                                                          Size,
                                                          AuditPath,
                                                          out var root))
            {
                ErrorResponse = "The audit path does not fit the index and the number of commits!";
                return false;
            }

            if (Convert.ToHexStringLower(root) != Checkpoint.CommitsRoot)
            {
                ErrorResponse = "The proof does not lead to the commits of the checkpoint!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (static) TryParse(JSON, out Proof, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a proof of a commit.
        /// </summary>
        public static Boolean TryParse(JObject                                                  JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkCheckpointProof?  Proof,
                                       [NotNullWhen(false)] out String?                         ErrorResponse)
        {

            Proof = null;

            try
            {

                if (!RoamingNetworkCheckpoint_Id.TryParse(JSON["checkpointId"]?.Value<String>() ?? "", out var checkpointId))
                {
                    ErrorResponse = "Missing or invalid checkpoint identification!";
                    return false;
                }

                if (JSON["leaf"] is not JObject leaf ||
                    !RoamingNetworkCommit_Id.TryParse(leaf["commitId"]?.Value<String>() ?? "", out var commitId) ||
                    leaf["eTags"] is not JArray eTagsJSON ||
                    leaf["revision"] is not JValue { Type: JTokenType.Integer } revision)
                {
                    ErrorResponse = "The leaf must be { commitId, eTags, revision }!";
                    return false;
                }

                if (JSON["@index"]     is not JValue { Type: JTokenType.Integer } index ||
                    JSON["@size"]      is not JValue { Type: JTokenType.Integer } size  ||
                    JSON["@auditPath"] is not JArray auditPath)
                {
                    ErrorResponse = "A proof needs @index, @size and @auditPath!";
                    return false;
                }

                Proof = new RoamingNetworkCheckpointProof(checkpointId,
                                                          commitId,
                                                          [ .. eTagsJSON.Select(ETag.ParseHex) ],
                                                          revision.Value<UInt64>(),
                                                          index.Value<UInt64>(),
                                                          size.Value<UInt64>(),
                                                          auditPath.Select(hash => Convert.FromHexString(hash.Value<String>() ?? "")));

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                Proof          = null;
                ErrorResponse  = "Invalid proof of a commit: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON()

        /// <summary>
        /// Return the JSON representation of this proof.
        /// </summary>
        public JObject ToJSON()

            => new (
                   new JProperty("checkpointId",  CheckpointId.ToString()),
                   new JProperty("leaf",          RoamingNetworkCheckpoint.LeafJSON(CommitId, ETags, Revision)),
                   new JProperty("@index",        Index),
                   new JProperty("@size",         Size),
                   new JProperty("@auditPath",    new JArray(AuditPath.Select(hash => Convert.ToHexStringLower(hash))))
               );

        #endregion

    }

}
