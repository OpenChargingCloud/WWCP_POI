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
    /// A proof that a price statement is one of the price statements a checkpoint names (SPECIFICATION.md, section 12):
    /// the statement with the index and the audit path of its digest in the tree of the checkpoint. With the signatures of
    /// the checkpoint, e.g. of witnesses, it shows that the statement was made before the checkpoint.
    /// </summary>
    public sealed class RoamingNetworkPriceStatementProof
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/priceStatementProof/v1";

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
            public const String  Context         = "@context";

            /// <summary>
            /// The property of the identification of its checkpoint.
            /// </summary>
            public const String  CheckpointId    = "checkpointId";

            /// <summary>
            /// The property of its price statement.
            /// </summary>
            public const String  PriceStatement  = "priceStatement";

            /// <summary>
            /// The property of the index of the leaf.
            /// </summary>
            public const String  Index           = "@index";

            /// <summary>
            /// The property of the number of price statements of the checkpoint.
            /// </summary>
            public const String  Size            = "@size";

            /// <summary>
            /// The property of the audit path of the leaf.
            /// </summary>
            public const String  AuditPath       = "@auditPath";

        }

        #endregion


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

            if (!POIMerkleTree.TryCalculateRootOfLeafHash(Checkpoint.IdentificationHash,
                                                          POIMerkleTree.LeafHash(Checkpoint.IdentificationHash, PriceStatement.Leaf),
                                                          Index,
                                                          Size,
                                                          AuditPath,
                                                          out var root))
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

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the proof of a price statement must be '{JSONLDContext}'!";
                    return false;
                }

                if (!RoamingNetworkCheckpoint_Id.TryParse(JSON[PropertyKeys.CheckpointId]?.Value<String>() ?? "", out var checkpointId))
                {
                    ErrorResponse = "Missing or invalid checkpoint identification!";
                    return false;
                }

                if (JSON[PropertyKeys.PriceStatement] is not JObject priceStatementJSON)
                {
                    ErrorResponse = "A proof of a price statement needs its price statement!";
                    return false;
                }

                if (!RoamingNetworkPriceStatement.TryParse(priceStatementJSON, out var priceStatement, out var priceStatementError))
                {
                    ErrorResponse = "Invalid price statement of the proof: " + priceStatementError;
                    return false;
                }

                if (JSON[PropertyKeys.Index]     is not JValue { Type: JTokenType.Integer } index ||
                    JSON[PropertyKeys.Size]      is not JValue { Type: JTokenType.Integer } size  ||
                    JSON[PropertyKeys.AuditPath] is not JArray auditPath)
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

        #region ToJSON(Embedded = false, CustomPriceStatementProofSerializer = null)

        /// <summary>
        /// Return the JSON representation of this proof.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomPriceStatementProofSerializer">An optional custom serializer of proofs of price statements.</param>
        public JObject ToJSON(Boolean                                                              Embedded                              = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkPriceStatementProof>?  CustomPriceStatementProofSerializer   = null)
        {

            var json = JSONObject.Create(

                           !Embedded
                               ? new JProperty(PropertyKeys.Context,         JSONLDContext)
                               : null,

                                 new JProperty(PropertyKeys.CheckpointId,    CheckpointId.ToString()),
                                 new JProperty(PropertyKeys.PriceStatement,  PriceStatement.ToJSON(Embedded: true)),
                                 new JProperty(PropertyKeys.Index,           Index),
                                 new JProperty(PropertyKeys.Size,            Size),
                                 new JProperty(PropertyKeys.AuditPath,       new JArray(AuditPath.Select(hash => Convert.ToHexStringLower(hash))))

                       );

            return CustomPriceStatementProofSerializer is not null
                       ? CustomPriceStatementProofSerializer(this, json)
                       : json;

        }

        #endregion

    }

}
