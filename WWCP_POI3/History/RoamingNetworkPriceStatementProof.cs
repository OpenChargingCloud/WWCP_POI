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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A proof that a price statement is one of the price statements a checkpoint names (SPECIFICATION.md, section 12):
    /// the statement with the index and the audit path of its digest in the tree of the checkpoint. With the signatures of
    /// the checkpoint, e.g. of witnesses, it shows that the statement was made before the checkpoint.
    /// </summary>
    public sealed class RoamingNetworkPriceStatementProof
    {

        #region Properties

        /// <summary>
        /// The checkpoint the price statement is proved to be named by.
        /// </summary>
        public RoamingNetworkCheckpoint_Id   CheckpointId      { get; }

        /// <summary>
        /// The price statement.
        /// </summary>
        public RoamingNetworkPriceStatement  PriceStatement    { get; }

        /// <summary>
        /// The index of the leaf of the price statement.
        /// </summary>
        public UInt64                        Index             { get; }

        /// <summary>
        /// The number of price statements of the checkpoint.
        /// </summary>
        public UInt64                        Size              { get; }

        /// <summary>
        /// The audit path of the leaf, from the bottom up.
        /// </summary>
        public ImmutableArray<Byte[]>        AuditPath         { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a proof of a price statement.
        /// </summary>
        public RoamingNetworkPriceStatementProof(RoamingNetworkCheckpoint_Id   CheckpointId,
                                                 RoamingNetworkPriceStatement  PriceStatement,
                                                 UInt64                        Index,
                                                 UInt64                        Size,
                                                 IEnumerable<Byte[]>           AuditPath)
        {

            this.CheckpointId    = CheckpointId;
            this.PriceStatement  = PriceStatement;
            this.Index           = Index;
            this.Size            = Size;
            this.AuditPath       = [ .. AuditPath ];

        }

        #endregion


        #region Verify(Checkpoint, out ErrorResponse)

        /// <summary>
        /// Whether this proof proves its price statement to be named by the given checkpoint. Whether the checkpoint carries
        /// the signatures a verifier requires is to be checked as well, and the statement against the proof of its charging
        /// station.
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

            if (Checkpoint.RoamingNetworkId != PriceStatement.RoamingNetworkId)
            {
                ErrorResponse = $"The price statement is one of the roaming network '{PriceStatement.RoamingNetworkId}', the checkpoint of '{Checkpoint.RoamingNetworkId}'!";
                return false;
            }

            if (Checkpoint.PriceStatementsSize != Size || Checkpoint.PriceStatementsRoot is null)
            {
                ErrorResponse = $"The checkpoint names {Checkpoint.PriceStatementsSize} price statements, not {Size}!";
                return false;
            }

            if (!POIMerkleTree.TryCalculateRoot(PriceStatement.Leaf, Index, Size, AuditPath, out var root))
            {
                ErrorResponse = "The audit path does not fit the index and the number of price statements!";
                return false;
            }

            if (Convert.ToHexStringLower(root) != Checkpoint.PriceStatementsRoot)
            {
                ErrorResponse = "The proof does not lead to the price statements of the checkpoint!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (static) TryParse(JSON, out Proof, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a proof of a price statement, read with numbers with a fraction as
        /// decimals and strings as strings.
        /// </summary>
        public static Boolean TryParse(JObject                                                      JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkPriceStatementProof?  Proof,
                                       [NotNullWhen(false)] out String?                             ErrorResponse)
        {

            Proof = null;

            try
            {

                if (!RoamingNetworkCheckpoint_Id.TryParse(JSON["checkpointId"]?.Value<String>() ?? "", out var checkpointId))
                {
                    ErrorResponse = "Missing or invalid checkpoint identification!";
                    return false;
                }

                if (JSON["priceStatement"] is not JObject priceStatementJSON)
                {
                    ErrorResponse = "A proof of a price statement needs its price statement!";
                    return false;
                }

                if (!RoamingNetworkPriceStatement.TryParse(priceStatementJSON, out var priceStatement, out var priceStatementError))
                {
                    ErrorResponse = "Invalid price statement of the proof: " + priceStatementError;
                    return false;
                }

                if (JSON["@index"]     is not JValue { Type: JTokenType.Integer } index ||
                    JSON["@size"]      is not JValue { Type: JTokenType.Integer } size  ||
                    JSON["@auditPath"] is not JArray auditPath)
                {
                    ErrorResponse = "A proof needs @index, @size and @auditPath!";
                    return false;
                }

                Proof = new RoamingNetworkPriceStatementProof(checkpointId,
                                                              priceStatement,
                                                              index.Value<UInt64>(),
                                                              size.Value<UInt64>(),
                                                              auditPath.Select(hash => Convert.FromHexString(hash.Value<String>() ?? "")));

                ErrorResponse = null;
                return true;

            }
            catch (Exception e)
            {
                Proof          = null;
                ErrorResponse  = "Invalid proof of a price statement: " + e.Message;
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
                   new JProperty("checkpointId",    CheckpointId.ToString()),
                   new JProperty("priceStatement",  PriceStatement.ToJSON()),
                   new JProperty("@index",          Index),
                   new JProperty("@size",           Size),
                   new JProperty("@auditPath",      new JArray(AuditPath.Select(hash => Convert.ToHexStringLower(hash))))
               );

        #endregion

    }

}
