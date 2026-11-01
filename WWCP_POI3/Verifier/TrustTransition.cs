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
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A compact delegation of trust from one authorized policy to the next (WWCP_POI.TS TrustTransition, experimental):
    /// the exact policy and key statement it continues, the trust anchor and the roaming network operator it delegates to,
    /// its place in a chain of transitions, its interval, optionally the checkpoints of the old epoch and the start of the
    /// next one, signed by quorums of the holders of both, the old ones as "old", the new ones as "new". No replacement of
    /// the succession of a history (8.6) or a replay of the changes of its owners.
    /// </summary>
    public sealed partial class TrustTransition
    {

        #region Data

        /// <summary>
        /// The JSON-LD context of the object.
        /// </summary>
        public const String JSONLDContext  = "https://open.charging.cloud/contexts/wwcp/trustTransition/v1";

        /// <summary>
        /// The side of a signature of a holder of the policy continued.
        /// </summary>
        public const String Old            = "old";

        /// <summary>
        /// The side of a signature of a holder of the policy delegated to.
        /// </summary>
        public const String New            = "new";

        /// <summary>
        /// The largest transition read: 2 MiB.
        /// </summary>
        public const Int32  MaxSize        = 2 * 1024 * 1024;

        private static readonly String[] Properties = [ "@context", "sequence", "previous", "fromPolicyId", "fromStatementDigest", "toAnchor", "toOperator",
                                                        "signedAt", "validUntil", "checkpoints", "epochChange", "signatures" ];

        [GeneratedRegex("^[0-9a-f]{64}$")]
        private static partial Regex SHA256Hex();

        #endregion

        #region Properties

        /// <summary>
        /// The JSON as signed, with its signatures.
        /// </summary>
        public JObject                    JSON                   { get; }

        /// <summary>
        /// Its place in the chain of transitions, from 0 on.
        /// </summary>
        public Int32                      Sequence               { get; }

        /// <summary>
        /// The digest of the transition before, from sequence 1 on.
        /// </summary>
        public String?                    Previous               { get; }

        /// <summary>
        /// When it was signed: when the policy it delegates to takes over.
        /// </summary>
        public DateTimeOffset             SignedAt               { get; }

        /// <summary>
        /// Until when it may be accepted.
        /// </summary>
        public DateTimeOffset             ValidUntil             { get; }

        /// <summary>
        /// The policy it continues (PolicyIdOf).
        /// </summary>
        public String                     FromPolicyId           { get; }

        /// <summary>
        /// The digest of the key statement it continues.
        /// </summary>
        public String                     FromStatementDigest    { get; }

        /// <summary>
        /// The trust anchor it delegates to.
        /// </summary>
        public RoamingNetworkTrustAnchor  TrustAnchor            { get; }

        /// <summary>
        /// The roaming network operator it delegates to, with its key statement and certificates.
        /// </summary>
        public JObject                    Operator               { get; }

        #endregion

        #region (private) Constructor(s)

        private TrustTransition(JObject                    JSON,
                                Int32                      Sequence,
                                String?                    Previous,
                                DateTimeOffset             SignedAt,
                                DateTimeOffset             ValidUntil,
                                String                     FromPolicyId,
                                String                     FromStatementDigest,
                                RoamingNetworkTrustAnchor  TrustAnchor,
                                JObject                    Operator)
        {

            this.JSON                 = JSON;
            this.Sequence             = Sequence;
            this.Previous             = Previous;
            this.SignedAt             = SignedAt;
            this.ValidUntil           = ValidUntil;
            this.FromPolicyId         = FromPolicyId;
            this.FromStatementDigest  = FromStatementDigest;
            this.TrustAnchor          = TrustAnchor;
            this.Operator             = Operator;

        }

        #endregion


        #region (static) PolicyIdOf(TrustAnchor)

        /// <summary>
        /// The identity of the complete policy of a trust anchor, as Chargy calculates it (WWCP_POI.TS trustPolicyId): the
        /// SHA-256 of the compact JSON of its network, its holders, its minConsensus and its URLs, each normalized as a URL
        /// without trailing slashes, in their ordinal order, as lowercase hex.
        /// </summary>
        /// <param name="TrustAnchor">The trust anchor.</param>
        public static String PolicyIdOf(RoamingNetworkTrustAnchor TrustAnchor)
        {

            var json = new JObject(new JProperty("network",       TrustAnchor.Id.ToString()),
                                   new JProperty("holders",       new JArray(TrustAnchor.Holders.Select(holder => new JArray(holder)))),
                                   new JProperty("minConsensus",  TrustAnchor.MinConsensus),
                                   new JProperty("urls",          new JArray(TrustAnchor.URLs.Select(url => new Uri(url.ToString()).AbsoluteUri.TrimEnd('/')).
                                                                                              Order(StringComparer.Ordinal))));

            return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json.ToString(Formatting.None))));

        }

        #endregion

        #region (static) DigestOf(KeyStatement)

        /// <summary>
        /// The digest of a key statement a transition continues: the SHA-256 of its content (RoamingNetworkTrustContext).
        /// </summary>
        public static String DigestOf(RoamingNetworkKeyStatement KeyStatement)

            => Convert.ToHexStringLower(SHA256.HashData(KeyStatement.GetContentBytes()));

        #endregion


        #region (static) Create(Sequence, Previous, From, FromStatement, To, ToOperator, SignedAt, ValidUntil, Checkpoints = null, Genesis = null, FirstCheckpoint = null)

        /// <summary>
        /// Create an unsigned transition from the given authorized trust anchor and key statement to the given trust anchor
        /// and roaming network operator: sign it with WithSignature(…) by quorums of the holders of both.
        /// </summary>
        /// <param name="Sequence">Its place in the chain of transitions, from 0 on.</param>
        /// <param name="Previous">The digest of the transition before, from sequence 1 on.</param>
        /// <param name="From">The trust anchor of the policy it continues.</param>
        /// <param name="FromStatement">The key statement it continues.</param>
        /// <param name="To">The trust anchor it delegates to.</param>
        /// <param name="ToOperator">The roaming network operator it delegates to, as JSON.</param>
        /// <param name="SignedAt">When it is signed: when the policy takes over.</param>
        /// <param name="ValidUntil">Until when it may be accepted.</param>
        /// <param name="Checkpoints">The checkpoints of the old epoch, the final one last where it opens the next epoch.</param>
        /// <param name="Genesis">The genesis commit of the next epoch, where it opens one.</param>
        /// <param name="FirstCheckpoint">The first checkpoint of the next epoch, where it opens one.</param>
        public static TrustTransition Create(Int32                                   Sequence,
                                             String?                                 Previous,
                                             RoamingNetworkTrustAnchor               From,
                                             RoamingNetworkKeyStatement              FromStatement,
                                             RoamingNetworkTrustAnchor               To,
                                             JObject                                 ToOperator,
                                             DateTimeOffset                          SignedAt,
                                             DateTimeOffset                          ValidUntil,
                                             IEnumerable<RoamingNetworkCheckpoint>?  Checkpoints       = null,
                                             RoamingNetworkCommit?                   Genesis           = null,
                                             RoamingNetworkCheckpoint?               FirstCheckpoint   = null)
        {

            var json = new JObject(new JProperty("@context", JSONLDContext),
                                   new JProperty("sequence", Sequence));

            if (Previous is not null)
                json.Add("previous", Previous);

            json.Add("fromPolicyId",         PolicyIdOf(From));
            json.Add("fromStatementDigest",  DigestOf(FromStatement));
            json.Add("toAnchor",             To.ToQRCode());
            json.Add("toOperator",           ToOperator);
            json.Add("signedAt",             SignedAt.  ToISO8601());
            json.Add("validUntil",           ValidUntil.ToISO8601());

            if (Checkpoints is not null)
                json.Add("checkpoints",      new JArray(Checkpoints.Select(checkpoint => checkpoint.ToJSON())));

            if ((Genesis is null) != (FirstCheckpoint is null))
                throw new ArgumentException("An epoch change names its genesis commit and its first checkpoint.");

            if (Genesis is not null && FirstCheckpoint is not null)
                json.Add("epochChange",      new JObject(new JProperty("genesis",          Genesis.        ToJSON()),
                                                         new JProperty("firstCheckpoint",  FirstCheckpoint.ToJSON())));

            json.Add("signatures",           new JArray());

            return TryParse(json, out var transition, out var error)
                       ? transition
                       : throw new ArgumentException(error);

        }

        #endregion

        #region WithSignature(Side, PrivateKey, KeyId, Algorithm)

        /// <summary>
        /// This transition with the signature of the given identity key of a holder of the given side added.
        /// </summary>
        /// <param name="Side">Old or New.</param>
        /// <param name="PrivateKey">The private identity key.</param>
        /// <param name="KeyId">The @id of the identity key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        public TrustTransition WithSignature(String                  Side,
                                             AsymmetricKeyParameter  PrivateKey,
                                             String                  KeyId,
                                             COSEAlgorithm           Algorithm)
        {

            if (Side is not Old and not New)
                throw new ArgumentException("The side of a transition signature is old or new.", nameof(Side));

            var json = (JObject) JSON.DeepClone();

            ((JArray) json["signatures"]!).Add(new JObject(new JProperty("side",       Side),
                                                           new JProperty("algorithm",  Algorithm.Name),
                                                           new JProperty("keyId",      KeyId),
                                                           new JProperty("value",      Convert.ToBase64String(Algorithm.Sign(GetSigningBytes(Side, Algorithm.Name, KeyId), PrivateKey)))));

            return TryParse(json, out var transition, out var error)
                       ? transition
                       : throw new ArgumentException(error);

        }

        #endregion


        #region (static) TryParse(JSON, out Transition, out ErrorResponse)

        /// <summary>
        /// Try to read the given trust transition, naming nothing else, at most MaxSize as JSON.
        /// </summary>
        public static Boolean TryParse(JToken?                                    JSON,
                                       [NotNullWhen(true)]  out TrustTransition?  Transition,
                                       [NotNullWhen(false)] out String?           ErrorResponse)
        {

            Transition = null;

            if (JSON is not JObject json)
            {
                ErrorResponse = "A trust transition must be a JSON object!";
                return false;
            }

            if (!HistoricalMeterEvidence.Only(json, "A trust transition", out ErrorResponse, Properties))
                return false;

            if (json["@context"]?.Type != JTokenType.String || json["@context"]!.Value<String>() != JSONLDContext ||
                Encoding.UTF8.GetByteCount(json.ToString(Formatting.Indented)) > MaxSize)
            {
                ErrorResponse = "Unsupported or excessive trust transition!";
                return false;
            }

            if (json["sequence"] is not JValue { Type: JTokenType.Integer } sequenceJSON || sequenceJSON.Value<Int64>() is < 0 or > 31)
            {
                ErrorResponse = "The sequence of a trust transition is an integer from 0 to 31!";
                return false;
            }

            var sequence = (Int32) sequenceJSON.Value<Int64>();
            var previous = json["previous"];

            if ((previous is not null) != (sequence > 0) ||
                (previous is not null && (previous.Type != JTokenType.String || !SHA256Hex().IsMatch(previous.Value<String>()!))))
            {
                ErrorResponse = "Invalid predecessor of a trust transition!";
                return false;
            }

            if (!SessionPriceAgreement.TryTimestamp(json, "signedAt",   out var signedAt,   out ErrorResponse) ||
                !SessionPriceAgreement.TryTimestamp(json, "validUntil", out var validUntil, out ErrorResponse))
            {
                return false;
            }

            if (validUntil < signedAt)
            {
                ErrorResponse = "Invalid transition validity!";
                return false;
            }

            if (json["fromPolicyId"]?.Type != JTokenType.String || json["fromStatementDigest"]?.Type != JTokenType.String ||
                json["toAnchor"]?.    Type != JTokenType.String || json["toOperator"] is not JObject toOperator)
            {
                ErrorResponse = "A trust transition names the policy and statement it continues, and the anchor and operator it delegates to!";
                return false;
            }

            if (!RoamingNetworkTrustAnchor.TryParse(json["toAnchor"]!.Value<String>()!, out var anchor, out ErrorResponse))
                return false;

            if (json["signatures"] is not JArray)
            {
                ErrorResponse = "The signatures of a trust transition must be a JSON array!";
                return false;
            }

            Transition = new TrustTransition(json,
                                             sequence,
                                             previous?.Value<String>(),
                                             signedAt,
                                             validUntil,
                                             json["fromPolicyId"]!.Value<String>()!,
                                             json["fromStatementDigest"]!.Value<String>()!,
                                             anchor,
                                             toOperator);

            return true;

        }

        #endregion

        #region Content()

        /// <summary>
        /// What it names, but its signatures.
        /// </summary>
        public JObject Content()
        {
            var content = (JObject) JSON.DeepClone();
            content.Remove("signatures");
            return content;
        }

        #endregion

        #region Digest()

        /// <summary>
        /// The SHA-256 of the canonical JSON of its content, as lowercase hex: the "previous" of the next transition.
        /// </summary>
        public String Digest()

            => Convert.ToHexStringLower(SHA256.HashData(CanonicalJSON.ToUTF8Bytes(Content())));

        #endregion

        #region GetSigningBytes(Side, Algorithm, KeyId)

        /// <summary>
        /// The signature input of a signature: the canonical JSON of its content with the side, the algorithm and the @id of
        /// the key of the signature.
        /// </summary>
        public Byte[] GetSigningBytes(String  Side,
                                      String  Algorithm,
                                      String  KeyId)
        {

            var signed = Content();
            signed["side"]       = Side;
            signed["algorithm"]  = Algorithm;
            signed["keyId"]      = KeyId;

            return CanonicalJSON.ToUTF8Bytes(signed);

        }

        #endregion

    }


    /// <summary>
    /// A trust transition verified (WWCP_POI.TS VerifiedTrustTransition): only made once both holder quorums, the exact
    /// policies and the evidence of an epoch change are authenticated, never restored of a stored flag. Its context verifies
    /// in the policy and the epoch it delegates to.
    /// </summary>
    public sealed class VerifiedTrustTransition
    {

        #region Properties

        /// <summary>
        /// The transition.
        /// </summary>
        public TrustTransition                            Source              { get; }

        /// <summary>
        /// The trust context of the policy and the epoch it delegates to.
        /// </summary>
        public RoamingNetworkTrustContext                 Context             { get; }

        /// <summary>
        /// The epoch after the transition.
        /// </summary>
        public RoamingNetworkEpoch                        Epoch               { get; }

        /// <summary>
        /// When that epoch began, where the transition opens it.
        /// </summary>
        public DateTimeOffset?                            EpochStartedAt      { get; }

        /// <summary>
        /// The checkpoints of the old epoch it gives.
        /// </summary>
        public ImmutableArray<RoamingNetworkCheckpoint>   Checkpoints         { get; }

        /// <summary>
        /// The first checkpoint of the next epoch, where it opens one.
        /// </summary>
        public RoamingNetworkCheckpoint?                  FirstCheckpoint     { get; }

        /// <summary>
        /// The epoch before the transition.
        /// </summary>
        public RoamingNetworkEpoch                        OldEpoch            { get; }

        /// <summary>
        /// The holders of the old policy that signed, by their index in its trust anchor.
        /// </summary>
        public ImmutableArray<Int32>                      SignedByOld         { get; }

        /// <summary>
        /// The holders of the new policy that signed, by their index in its trust anchor.
        /// </summary>
        public ImmutableArray<Int32>                      SignedByNew         { get; }

        #endregion

        #region (private) Constructor(s)

        private VerifiedTrustTransition(TrustTransition                        Source,
                                        RoamingNetworkTrustContext             Context,
                                        RoamingNetworkEpoch                    Epoch,
                                        DateTimeOffset?                        EpochStartedAt,
                                        IEnumerable<RoamingNetworkCheckpoint>  Checkpoints,
                                        RoamingNetworkCheckpoint?              FirstCheckpoint,
                                        RoamingNetworkEpoch                    OldEpoch,
                                        IEnumerable<Int32>                     SignedByOld,
                                        IEnumerable<Int32>                     SignedByNew)
        {

            this.Source           = Source;
            this.Context          = Context;
            this.Epoch            = Epoch;
            this.EpochStartedAt   = EpochStartedAt;
            this.Checkpoints      = [.. Checkpoints];
            this.FirstCheckpoint  = FirstCheckpoint;
            this.OldEpoch         = OldEpoch;
            this.SignedByOld      = [.. SignedByOld];
            this.SignedByNew      = [.. SignedByNew];

        }

        #endregion


        #region (static) TryVerify(Basis, FromOperator, Transition, AcceptedAt, Epoch, Sequence, out Verified, out ErrorResponse, Previous = null, EpochStartedAt = null)

        /// <summary>
        /// Verify the given transition from the given trust context of the current local decisions and the roaming network
        /// operator it was evaluated with: accepted within its interval, in its place of the chain; continuing exactly the
        /// policy, key statement, network and operator of the context; the operator it delegates to verified under the trust
        /// anchor it delegates to at the time it was signed, requiring no fewer holders, splitting no holder known; signed by
        /// quorums of the holders of both, each by an identity key valid then; its checkpoints signed by the old operator at
        /// their times, of the current epoch; and an epoch change opened by the exact genesis of the final checkpoint and a
        /// first checkpoint following it, signed by the new operator under the profiles of both epochs.
        /// </summary>
        /// <param name="Basis">The trust context of the current local decisions.</param>
        /// <param name="FromOperator">The roaming network operator the context was evaluated with, as JSON.</param>
        /// <param name="Transition">The transition.</param>
        /// <param name="AcceptedAt">When it is accepted.</param>
        /// <param name="Epoch">The current epoch.</param>
        /// <param name="Sequence">The place in the chain expected.</param>
        /// <param name="Verified">The verified transition.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="Previous">The digest of the transition before expected, from sequence 1 on.</param>
        /// <param name="EpochStartedAt">When the current epoch began, where a transition before said so.</param>
        public static Boolean TryVerify(RoamingNetworkTrustContext                         Basis,
                                        JObject                                            FromOperator,
                                        TrustTransition                                    Transition,
                                        DateTimeOffset                                     AcceptedAt,
                                        RoamingNetworkEpoch                                Epoch,
                                        Int32                                              Sequence,
                                        [NotNullWhen(true)]  out VerifiedTrustTransition?  Verified,
                                        [NotNullWhen(false)] out String?                   ErrorResponse,
                                        String?                                            Previous         = null,
                                        DateTimeOffset?                                    EpochStartedAt   = null)
        {

            Verified = null;

            if (AcceptedAt < Transition.SignedAt || AcceptedAt > Transition.ValidUntil || Transition.Sequence != Sequence || Transition.Previous != Previous)
            {
                ErrorResponse = "Transition is expired, out of order or not accepted within its signed interval!";
                return false;
            }

            if (Transition.FromPolicyId              != TrustTransition.PolicyIdOf(Basis.TrustAnchor) ||
                Transition.FromStatementDigest       != Basis.KeyStatementDigest                      ||
                Transition.TrustAnchor.Id            != Basis.RoamingNetworkId                        ||
                VerifiedChargingStation.IdOf(Transition.Operator) != Basis.RoamingNetworkOperatorId.ToString())
            {
                ErrorResponse = "Transition does not continue the exact authorized policy/network/operator!";
                return false;
            }

            if (FromOperator[RoamingNetworkOperator.PropertyKeys.KeyStatement] is not JObject oldStatementJSON)
            {
                ErrorResponse = "The previous operator states no keys!";
                return false;
            }

            if (Transition.Operator[RoamingNetworkOperator.PropertyKeys.KeyStatement] is not JObject nextStatementJSON)
            {
                ErrorResponse = "The target operator states no keys!";
                return false;
            }

            if (!RoamingNetworkKeyStatement. TryParse (oldStatementJSON,  out var oldStatement,  out ErrorResponse) ||
                !RoamingNetworkOperatorTrust.TryCreate(Basis, FromOperator, out _,                out ErrorResponse) ||
                !RoamingNetworkKeyStatement. TryParse (nextStatementJSON, out var nextStatement, out ErrorResponse) ||
                !POIPartyKeys.               TryParse (Transition.Operator, out _, out var identities, out ErrorResponse))
            {
                return false;
            }

            if (!RoamingNetworkTrustContext.TryCreate(Transition.TrustAnchor, nextStatement, Transition.TrustAnchor.Keys, out var next, out ErrorResponse, identities, Transition.SignedAt) ||
                !RoamingNetworkOperatorTrust.TryCreate(next, Transition.Operator, out _, out ErrorResponse))
            {
                return false;
            }

            if (next.RoamingNetworkOperatorId != Basis.RoamingNetworkOperatorId || next.RequiredConsensus < Basis.RequiredConsensus)
            {
                ErrorResponse = "A trust transition must not silently reduce the required holder quorum!";
                return false;
            }

            // Holders known before are not split merely by relabeling the same keys.
            foreach (var holder in Basis.AuthorizedTrustAnchor.Holders)
                if (Transition.TrustAnchor.Holders.Count(group => group.Any(holder.Contains)) > 1)
                {
                    ErrorResponse = "A transition splits a previously known holder!";
                    return false;
                }

            if (!POIPartyKeys.TryParse(FromOperator, out _, out var oldIdentities, out ErrorResponse))
                return false;

            var oldPool   = oldStatement. IdentityKeys.Concat(oldIdentities).ToArray();
            var nextPool  = nextStatement.IdentityKeys.Concat(identities).   ToArray();
            var old       = new HashSet<Int32>();
            var target    = new HashSet<Int32>();

            foreach (var item in (JArray) Transition.JSON["signatures"]!)
            {

                if (item is not JObject signature)
                {
                    ErrorResponse = "A transition signature must be a JSON object!";
                    return false;
                }

                if (!HistoricalMeterEvidence.Only(signature, "A transition signature", out ErrorResponse, "side", "algorithm", "keyId", "value"))
                    return false;

                if (new[] { "side", "algorithm", "keyId", "value" }.Any(name => signature[name]?.Type != JTokenType.String))
                {
                    ErrorResponse = "A transition signature names its side, algorithm, key and value as texts!";
                    return false;
                }

                var side       = signature["side"]!.     Value<String>()!;
                var algorithm  = signature["algorithm"]!.Value<String>()!;
                var keyId      = signature["keyId"]!.    Value<String>()!;

                if (side is not TrustTransition.Old and not TrustTransition.New)
                {
                    ErrorResponse = "Invalid transition signature side!";
                    return false;
                }

                var groups  = side == TrustTransition.Old ? Basis.TrustedHolders : next.TrustedHolders;
                var pool    = side == TrustTransition.Old ? oldPool              : nextPool;
                var holder  = groups.Select((group, index) => (group, index)).FirstOrDefault(entry => entry.group.Contains(keyId), (group: [], index: -1)).index;

                if (pool.FirstOrDefault(key => key.Id == keyId) is not POIPublicKey key || !key.IsValidAt(Transition.SignedAt))
                {
                    ErrorResponse = "Transition signer is not an authorized valid identity key!";
                    return false;
                }

                if (!TryVerifySignature(algorithm, key, Transition.GetSigningBytes(side, algorithm, keyId), signature["value"]!.Value<String>()!, out ErrorResponse))
                    return false;

                if (holder >= 0)
                    (side == TrustTransition.Old ? old : target).Add(holder);

            }

            if (old.Count < Basis.RequiredConsensus || target.Count < next.RequiredConsensus)
            {
                ErrorResponse = "Both old and new holder quorums must sign the trust transition!";
                return false;
            }

            var checkpoints = new List<RoamingNetworkCheckpoint>();

            foreach (var checkpointJSON in Transition.JSON["checkpoints"] as JArray ?? [])
            {

                if (checkpointJSON is not JObject json)
                {
                    ErrorResponse = "A transition checkpoint must be a JSON object!";
                    return false;
                }

                if (!RoamingNetworkCheckpoint.TryParse(json, out var checkpoint, out ErrorResponse))
                    return false;

                if (!HistoricalTrustContext.TryGetOperatorContext(Basis, FromOperator, checkpoint.CreatedAt, out var context, out ErrorResponse) ||
                    !AuthorizedCheckpoint.TryVerify(context, checkpoint, FromOperator, null, out _, out ErrorResponse))
                {
                    return false;
                }

                if (checkpoint.Epoch != Epoch.Number)
                {
                    ErrorResponse = "The transition checkpoint line belongs to another epoch!";
                    return false;
                }

                checkpoints.Add(checkpoint);

            }

            var                        epoch           = Epoch;
            var                        epochStartedAt  = EpochStartedAt;
            RoamingNetworkCheckpoint?  first           = null;

            if (Transition.JSON["epochChange"] is JToken change)
            {
                if (!TryVerifyEpochChange(Basis, next, Transition, change, checkpoints, Epoch, out first, out var nextEpoch, out ErrorResponse))
                    return false;
                epoch           = nextEpoch;
                epochStartedAt  = first.CreatedAt;
            }

            Verified = new VerifiedTrustTransition(Transition,
                                                   RoamingNetworkTrustContext.WithEpoch(next,
                                                                                        epoch,
                                                                                        epochStartedAt,
                                                                                        [.. Basis.Epochs.Where(known => known.Number != epoch.Number), epoch]),
                                                   epoch,
                                                   epochStartedAt,
                                                   checkpoints,
                                                   first,
                                                   Epoch,
                                                   old,
                                                   target);

            ErrorResponse = null;
            return true;

        }

        #endregion


        #region (private static) TryVerifyEpochChange(…)

        private static Boolean TryVerifyEpochChange(RoamingNetworkTrustContext                          Basis,
                                                    RoamingNetworkTrustContext                          Next,
                                                    TrustTransition                                     Transition,
                                                    JToken                                              Change,
                                                    IReadOnlyList<RoamingNetworkCheckpoint>             Checkpoints,
                                                    RoamingNetworkEpoch                                 Epoch,
                                                    [NotNullWhen(true)]  out RoamingNetworkCheckpoint?  First,
                                                    [NotNullWhen(true)]  out RoamingNetworkEpoch?       NextEpoch,
                                                    [NotNullWhen(false)] out String?                    ErrorResponse)
        {

            First      = null;
            NextEpoch  = null;

            if (Change is not JObject change)
            {
                ErrorResponse = "An epoch change must be a JSON object!";
                return false;
            }

            if (!HistoricalMeterEvidence.Only(change, "An epoch change", out ErrorResponse, "genesis", "firstCheckpoint"))
                return false;

            if (Checkpoints.Count == 0 || Checkpoints[^1].Next is not RoamingNetworkEpoch nextEpoch)
            {
                ErrorResponse = "An epoch transition needs the signed final checkpoint!";
                return false;
            }

            var final = Checkpoints[^1];

            if (final.CreatedAt > Transition.SignedAt)
            {
                ErrorResponse = "The final checkpoint postdates the transition!";
                return false;
            }

            if (change["genesis"] is not JObject genesisJSON || change["firstCheckpoint"] is not JObject firstJSON)
            {
                ErrorResponse = "An epoch change gives the genesis commit and the first checkpoint of the next epoch!";
                return false;
            }

            if (!RoamingNetworkCommit.    TryParse(genesisJSON, out var genesis, out ErrorResponse) ||
                !RoamingNetworkCheckpoint.TryParse(firstJSON,   out var first,   out ErrorResponse))
            {
                return false;
            }

            RoamingNetworkCommit expected;

            try
            {
                expected = RoamingNetworkCommit.GenesisOf(Basis.RoamingNetworkId, final.ETags, nextEpoch);
            }
            catch (ArgumentException e)
            {
                ErrorResponse = $"The final checkpoint opens no next epoch: {e.Message}";
                return false;
            }

            if (genesis.Id              != expected.Id        ||
                first.CommitId          != expected.Id        ||
                first.Revision          != expected.Revision  ||
                !first.ETags.ToHashSet().SetEquals(expected.ETags) ||
                first.Sequence          != 0                  ||
                first.Epoch             != nextEpoch.Number   ||
                first.PreviousEpoch     is not RoamingNetworkEpochLink link ||
                link.CheckpointId       != final.Id           ||
                link.Epoch              != final.Epoch        ||
                !link.ETags.ToHashSet().SetEquals(final.ETags))
            {
                ErrorResponse = "The next epoch must start from the exact deterministic genesis and final checkpoint!";
                return false;
            }

            if ((TrustTransition.PolicyIdOf(Next.TrustAnchor) != TrustTransition.PolicyIdOf(Basis.TrustAnchor) || Next.KeyStatementDigest != Basis.KeyStatementDigest) &&
                first.CreatedAt != Transition.SignedAt)
            {
                ErrorResponse = "A combined policy/epoch handoff must activate at its first checkpoint time!";
                return false;
            }

            if (first.CreatedAt < final.CreatedAt || first.CreatedAt > Transition.SignedAt)
            {
                ErrorResponse = "Invalid epoch-transition checkpoint times!";
                return false;
            }

            if (!HistoricalTrustContext.TryGetOperatorContext(Next, Transition.Operator, first.CreatedAt, out var context, out ErrorResponse) ||
                !RoamingNetworkOperatorTrust.TryCreate(context, Transition.Operator, out var trust, out ErrorResponse))
            {
                return false;
            }

            foreach (var profiles in new[] { Epoch.Profiles, nextEpoch.Profiles })
            {

                var holders = new HashSet<String>(StringComparer.Ordinal);

                foreach (var signature in first.Signatures.Where(signature => signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator &&
                                                                              profiles.AllowsSignatureAlgorithm(signature.Algorithm)))
                {

                    if (trust.CertificateOf(signature.Role, signature.KeyId, signature.SignedAt) is not RoamingNetworkSigningKeyCertificate certificate ||
                        trust.HolderOf(certificate) is not String holder)
                    {
                        ErrorResponse = $"The key '{signature.KeyId}' is no signing key a trusted identity key of its holder certifies!";
                        return false;
                    }

                    if (!first.VerifySignature(signature, certificate.SigningKey.Key, certificate.SigningKey.Id, out ErrorResponse))
                        return false;

                    holders.Add(holder);

                }

                if (holders.Count < context.RequiredConsensus)
                {
                    ErrorResponse = $"The epoch-transition checkpoint is signed by {holders.Count} holder(s) of the roaming network operator only!";
                    return false;
                }

            }

            if (first.Signatures.Any(signature => !Epoch.Profiles.AllowsSignatureAlgorithm(signature.Algorithm) && !nextEpoch.Profiles.AllowsSignatureAlgorithm(signature.Algorithm)))
            {
                ErrorResponse = "An epoch-transition signature uses neither accepted profile!";
                return false;
            }

            First          = first;
            NextEpoch      = nextEpoch;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (internal static) TryVerifySignature(Algorithm, Key, Bytes, Value, out ErrorResponse)

        /// <summary>
        /// Whether the given signature of the given bytes is one of the given key, of a known algorithm named canonically,
        /// in canonical Base64.
        /// </summary>
        internal static Boolean TryVerifySignature(String                            Algorithm,
                                                   POIPublicKey                      Key,
                                                   Byte[]                            Bytes,
                                                   String                            Value,
                                                   [NotNullWhen(false)] out String?  ErrorResponse)
        {

            try
            {

                if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || algorithm.Name != Algorithm)
                {
                    ErrorResponse = $"Unknown or noncanonical signature algorithm '{Algorithm}'!";
                    return false;
                }

                var value = Convert.FromBase64String(Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Value)
                {
                    ErrorResponse = "A signature must be nonempty canonical Base64!";
                    return false;
                }

                if (!algorithm.Verify(Bytes, value, Key.Key.ToPublicKey(), out var signatureError))
                {
                    ErrorResponse = $"Invalid signature: {signatureError}";
                    return false;
                }

            }
            catch (Exception e)
            {
                ErrorResponse = $"Invalid signature: {e.Message}";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

    }

}
