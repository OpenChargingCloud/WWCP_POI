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

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// What RoamingNetworkTrustContext.Evaluate(…) finds of a key statement against the identity keys of a trust anchor an
    /// app trusts locally: its signatures, its signers, its quorum and its validity apart, so that the app shows each; and
    /// the trust context, only where all of them hold.
    /// </summary>
    public sealed class RoamingNetworkTrustEvaluation
    {

        #region Properties

        /// <summary>
        /// Whether every signature of the key statement is valid, by an identity key it names or one given besides.
        /// </summary>
        public Boolean                      SignaturesValid       { get; }

        /// <summary>
        /// Whether a trusted identity key of at least one holder of the trust anchor signed the key statement.
        /// </summary>
        public Boolean                      SignersTrusted        { get; }

        /// <summary>
        /// Whether trusted identity keys of as many holders as the trust anchor and the key statement require signed it, and
        /// it names their holders as the trust anchor does (RoamingNetworkKeyStatement.TryVerify(TrustedHolders, …)).
        /// </summary>
        public Boolean                      QuorumReached         { get; }

        /// <summary>
        /// Whether the identity keys that signed the key statement were valid when it was signed, and it is valid at the time
        /// of the evaluation.
        /// </summary>
        public Boolean                      WithinValidity        { get; }

        /// <summary>
        /// How many holders of the trust anchor signed the key statement with an identity key trusted locally.
        /// </summary>
        public Int32                        TrustedHolderCount    { get; }

        /// <summary>
        /// How many holders must sign: the larger of the minConsensus of the trust anchor and the one of the key statement.
        /// </summary>
        public Byte                         RequiredConsensus     { get; }

        /// <summary>
        /// How many signatures the key statement has.
        /// </summary>
        public Int32                        SignatureCount        { get; }

        /// <summary>
        /// Whether the app may import what the key statement names and let its user trust its keys by hand: validly signed
        /// and valid, also without a quorum trusted locally.
        /// </summary>
        public Boolean                      CanTrustKeys
            => SignaturesValid && WithinValidity;

        /// <summary>
        /// Why the signatures, the quorum or the validity do not hold, or null.
        /// </summary>
        public String?                      VerificationError     { get; }

        /// <summary>
        /// The trust context, where the signatures, the quorum and the validity hold; null otherwise.
        /// </summary>
        public RoamingNetworkTrustContext?  Context               { get; }

        #endregion

        #region (internal) Constructor(s)

        internal RoamingNetworkTrustEvaluation(Boolean                      SignaturesValid,
                                               Boolean                      SignersTrusted,
                                               Boolean                      QuorumReached,
                                               Boolean                      WithinValidity,
                                               Int32                        TrustedHolderCount,
                                               Byte                         RequiredConsensus,
                                               Int32                        SignatureCount,
                                               String?                      VerificationError,
                                               RoamingNetworkTrustContext?  Context)
        {

            this.SignaturesValid     = SignaturesValid;
            this.SignersTrusted      = SignersTrusted;
            this.QuorumReached       = QuorumReached;
            this.WithinValidity      = WithinValidity;
            this.TrustedHolderCount  = TrustedHolderCount;
            this.RequiredConsensus   = RequiredConsensus;
            this.SignatureCount      = SignatureCount;
            this.VerificationError   = VerificationError;
            this.Context             = Context;

        }

        #endregion

    }


    /// <summary>
    /// A trust anchor authorized locally and a key statement verified with it, at one time, as WWCP_POI.TS 0.1.1 has it
    /// (TrustContext): an app that lets its users import a trust anchor before they trust its keys does not verify against
    /// that anchor, which trusts every identity key it pins, but against a context it evaluates of the identity keys it
    /// trusts, e.g. those it brings along and those its user chose, without those excluded. Of the holders of the trust
    /// anchor it keeps those keys, and it is bound to the roaming network, its operator, the content of the key statement
    /// and the time it was evaluated at. Importing a trust anchor authorizes nothing.
    /// </summary>
    /// <remarks>
    /// No revocation: the app evaluates a context of its current decisions for every verification, and stores its
    /// decisions, not the context, which sees nothing that changes after it was evaluated. A key excluded locally is no
    /// longer trusted from the start, but a key statement signed by the quorum may still name it among its identity keys.
    /// </remarks>
    public sealed class RoamingNetworkTrustContext : IRoamingNetworkTrustAuthority
    {

        #region Properties

        /// <summary>
        /// The trust anchor as the app imported it, e.g. to show its texts (RoamingNetworkProof.TrustAnchorTextMismatch(…)).
        /// </summary>
        public RoamingNetworkTrustAnchor             TrustAnchor                 { get; }

        /// <summary>
        /// The trust anchor a verifier checks against: of the holders of the imported one, those with an identity key trusted
        /// locally, each with only those keys, and its minConsensus.
        /// </summary>
        public RoamingNetworkTrustAnchor             AuthorizedTrustAnchor       { get; }

        /// <summary>
        /// The @ids of the identity keys trusted locally per holder of the imported trust anchor, in its order: a holder of no
        /// such key with none, so that no key changes its holder.
        /// </summary>
        public IReadOnlyList<IReadOnlyList<String>>  TrustedHolders              { get; }

        /// <summary>
        /// The roaming network of the key statement and the trust anchor.
        /// </summary>
        public RoamingNetwork_Id                     RoamingNetworkId            { get; }

        /// <summary>
        /// The roaming network operator of the key statement.
        /// </summary>
        public RoamingNetworkOperator_Id             RoamingNetworkOperatorId    { get; }

        /// <summary>
        /// The SHA-256 of the content of the key statement, as lowercase hex (RoamingNetworkKeyStatement.GetContentBytes()).
        /// </summary>
        public String                                KeyStatementDigest          { get; }

        /// <summary>
        /// How many holders must sign: the larger of the minConsensus of the trust anchor and the one of the key statement.
        /// </summary>
        public Byte                                  RequiredConsensus           { get; }

        /// <summary>
        /// Until when the key statement is valid.
        /// </summary>
        public DateTimeOffset                        ValidUntil                  { get; }

        /// <summary>
        /// The time it was evaluated at, and the only one it verifies at.
        /// </summary>
        public DateTimeOffset                        EvaluatedAt                 { get; }

        /// <summary>
        /// The epoch a proof verified against this context must belong to (SPECIFICATION.md, section 11): the first one, unless
        /// a verified trust transition established another (RoamingNetworkVerifiedTrustTransition).
        /// </summary>
        public RoamingNetworkEpoch                   Epoch                       { get; }

        /// <summary>
        /// When the epoch began, as the first checkpoint of it a verified trust transition gives says, and no attestation signed
        /// earlier counts; none for the first epoch. Known locally, no part of what is signed.
        /// </summary>
        public DateTimeOffset?                       EpochStartedAt              { get; }

        /// <summary>
        /// The epochs verified so far, e.g. the one before the last trust transition, by which checkpoints are checked.
        /// </summary>
        public IReadOnlyList<RoamingNetworkEpoch>    Epochs                      { get; }

        /// <summary>
        /// The key statement it was evaluated of.
        /// </summary>
        internal RoamingNetworkKeyStatement          KeyStatement                { get; }

        #endregion

        #region (private) Constructor(s)

        private RoamingNetworkTrustContext(RoamingNetworkTrustAnchor             TrustAnchor,
                                           IReadOnlyList<IReadOnlyList<String>>  TrustedHolders,
                                           RoamingNetworkKeyStatement            KeyStatement,
                                           DateTimeOffset                        EvaluatedAt,
                                           RoamingNetworkEpoch?                  Epoch            = null,
                                           DateTimeOffset?                       EpochStartedAt   = null,
                                           IEnumerable<RoamingNetworkEpoch>?     Epochs           = null)
        {

            this.TrustAnchor               = TrustAnchor;
            this.TrustedHolders            = TrustedHolders;

            // A trust anchor names every holder with a key: those without a key trusted locally are left out, which splits no
            // holder and lowers no quorum.
            this.AuthorizedTrustAnchor     = new RoamingNetworkTrustAnchor(TrustAnchor.Id,
                                                                           TrustAnchor.URLs,
                                                                           TrustedHolders.Where(keys => keys.Count > 0),
                                                                           TrustAnchor.MinConsensus,
                                                                           TrustAnchor.Name,
                                                                           TrustAnchor.Description);

            this.RoamingNetworkId          = KeyStatement.RoamingNetworkId;
            this.RoamingNetworkOperatorId  = KeyStatement.RoamingNetworkOperatorId;
            this.KeyStatementDigest        = DigestOf(KeyStatement);
            this.RequiredConsensus         = Math.Max(TrustAnchor.MinConsensus, KeyStatement.MinConsensus);
            this.ValidUntil                = KeyStatement.ValidUntil;
            this.EvaluatedAt               = EvaluatedAt;
            this.Epoch                     = Epoch ?? RoamingNetworkEpoch.First;
            this.EpochStartedAt            = EpochStartedAt;
            this.Epochs                    = (Epochs ?? [ this.Epoch ]).ToImmutableArray();
            this.KeyStatement              = KeyStatement;

        }

        #endregion


        #region (static) Evaluate (TrustAnchor, KeyStatement, TrustedIdentityKeyIds, OtherIdentityKeys = null, At = null)

        /// <summary>
        /// Evaluate the given key statement against the given trust anchor of its roaming network, of which the app trusts
        /// the identity keys of the given @ids locally: every signature and that its key was valid when the statement was
        /// signed, that the statement is valid at the given time, now by default, that it names the holders of the trusted
        /// keys as the trust anchor does, and that trusted keys of as many holders as the larger of both minConsensus signed
        /// it; two trusted keys of one holder count once. Each is reported apart, the context only where all of them hold.
        /// </summary>
        /// <param name="TrustAnchor">The trust anchor, e.g. as imported.</param>
        /// <param name="KeyStatement">The key statement of the roaming network operator, e.g. as a proof gives it.</param>
        /// <param name="TrustedIdentityKeyIds">The @ids of the identity keys the app trusts locally.</param>
        /// <param name="OtherIdentityKeys">Other identity keys, e.g. those of the roaming network operator a proof gives.</param>
        /// <param name="At">The time of the evaluation, now by default.</param>
        /// <exception cref="ArgumentException">The key statement is one of another roaming network than the trust anchor.</exception>
        public static RoamingNetworkTrustEvaluation Evaluate(RoamingNetworkTrustAnchor   TrustAnchor,
                                                             RoamingNetworkKeyStatement  KeyStatement,
                                                             IEnumerable<String>         TrustedIdentityKeyIds,
                                                             IEnumerable<POIPublicKey>?  OtherIdentityKeys   = null,
                                                             DateTimeOffset?             At                  = null)
        {

            if (KeyStatement.RoamingNetworkId != TrustAnchor.Id)
                throw new ArgumentException($"The key statement is one of the roaming network '{KeyStatement.RoamingNetworkId}', the trust anchor of '{TrustAnchor.Id}'!",
                                            nameof(KeyStatement));

            var at                  = At ?? Timestamp.Now;
            var trusted             = TrustedIdentityKeyIds.ToHashSet(StringComparer.Ordinal);
            var holders             = TrustAnchor.Holders.Select(keys => (IReadOnlyList<String>) keys.Where(trusted.Contains).ToImmutableArray()).ToImmutableArray();
            var otherIdentityKeys   = OtherIdentityKeys?.ToArray() ?? [];
            var identityKeys        = KeyStatement.IdentityKeys.Concat(otherIdentityKeys).ToArray();
            var signedBy            = KeyStatement.Signatures.Select(signature => signature.KeyId).ToHashSet(StringComparer.Ordinal);
            var trustedHolderCount  = holders.Count(keys => keys.Any(signedBy.Contains));

            var signaturesValid     = true;
            var keysValid           = true;
            String? error           = null;

            foreach (var signature in KeyStatement.Signatures)
            {

                if (identityKeys.FirstOrDefault(key => key.Id == signature.KeyId) is not POIPublicKey identityKey)
                {
                    signaturesValid = false;
                    error           = $"The identity key '{signature.KeyId}' that signed the key statement is not given!";
                    break;
                }

                if (!KeyStatement.VerifySignature(identityKey, out var signatureError))
                {
                    signaturesValid = false;
                    error           = signatureError;
                    break;
                }

                if (!identityKey.IsValidAt(KeyStatement.SignedAt))
                    keysValid = false;

            }

            var quorumReached       = KeyStatement.TryVerify(holders, otherIdentityKeys, out _, out var quorumError, TrustAnchor.MinConsensus, at);
            var withinValidity      = keysValid && KeyStatement.IsValidAt(at);

            return new RoamingNetworkTrustEvaluation(signaturesValid,
                                                     trustedHolderCount > 0,
                                                     quorumReached,
                                                     withinValidity,
                                                     trustedHolderCount,
                                                     Math.Max(TrustAnchor.MinConsensus, KeyStatement.MinConsensus),
                                                     KeyStatement.Signatures.Length,
                                                     error ?? quorumError,
                                                     signaturesValid && quorumReached && withinValidity
                                                         ? new RoamingNetworkTrustContext(TrustAnchor, holders, KeyStatement, at)
                                                         : null);

        }

        #endregion

        #region (static) TryCreate(TrustAnchor, KeyStatement, TrustedIdentityKeyIds, out TrustContext, out ErrorResponse, OtherIdentityKeys = null, At = null)

        /// <summary>
        /// Evaluate the given key statement against the given trust anchor of its roaming network as Evaluate(…) does, and
        /// give the trust context where the signatures, the quorum and the validity hold.
        /// </summary>
        /// <param name="TrustAnchor">The trust anchor, e.g. as imported.</param>
        /// <param name="KeyStatement">The key statement of the roaming network operator, e.g. as a proof gives it.</param>
        /// <param name="TrustedIdentityKeyIds">The @ids of the identity keys the app trusts locally.</param>
        /// <param name="TrustContext">The trust context.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="OtherIdentityKeys">Other identity keys, e.g. those of the roaming network operator a proof gives.</param>
        /// <param name="At">The time of the evaluation, now by default.</param>
        public static Boolean TryCreate(RoamingNetworkTrustAnchor                              TrustAnchor,
                                        RoamingNetworkKeyStatement                             KeyStatement,
                                        IEnumerable<String>                                    TrustedIdentityKeyIds,
                                        [NotNullWhen(true)]  out RoamingNetworkTrustContext?   TrustContext,
                                        [NotNullWhen(false)] out String?                       ErrorResponse,
                                        IEnumerable<POIPublicKey>?                             OtherIdentityKeys   = null,
                                        DateTimeOffset?                                        At                  = null)
        {

            TrustContext = null;

            if (KeyStatement.RoamingNetworkId != TrustAnchor.Id)
            {
                ErrorResponse = $"The key statement is one of the roaming network '{KeyStatement.RoamingNetworkId}', the trust anchor of '{TrustAnchor.Id}'!";
                return false;
            }

            var evaluation  = Evaluate(TrustAnchor, KeyStatement, TrustedIdentityKeyIds, OtherIdentityKeys, At);

            TrustContext    = evaluation.Context;
            ErrorResponse   = TrustContext is null
                                  ? evaluation.VerificationError ?? "No quorum of the trust anchor is trusted locally!"
                                  : null;

            return TrustContext is not null;

        }

        #endregion


        #region (static) WithEpoch(TrustContext, Epoch, EpochStartedAt, Epochs = null)

        /// <summary>
        /// The given trust context in the given epoch: only of an epoch a verified trust transition established
        /// (RoamingNetworkVerifiedTrustTransition), as a context evaluated of the decisions of an app starts in the first one.
        /// </summary>
        /// <param name="TrustContext">The trust context.</param>
        /// <param name="Epoch">The epoch.</param>
        /// <param name="EpochStartedAt">When it began, as its first checkpoint says.</param>
        /// <param name="Epochs">The epochs verified so far, the given one by default.</param>
        public static RoamingNetworkTrustContext WithEpoch(RoamingNetworkTrustContext         TrustContext,
                                                           RoamingNetworkEpoch                Epoch,
                                                           DateTimeOffset?                    EpochStartedAt,
                                                           IEnumerable<RoamingNetworkEpoch>?  Epochs   = null)

            => new (TrustContext.TrustAnchor,
                    TrustContext.TrustedHolders,
                    TrustContext.KeyStatement,
                    TrustContext.EvaluatedAt,
                    Epoch,
                    EpochStartedAt,
                    Epochs ?? [ Epoch ]);

        #endregion


        #region TryGetAuthorityAt(At, out Authority, out ErrorResponse)

        /// <summary>
        /// None: a trust context of the current decisions of an app has no timeline of authorities, it is the authority.
        /// </summary>
        /// <param name="At">The historical time.</param>
        /// <param name="Authority">None.</param>
        /// <param name="ErrorResponse">None.</param>
        public Boolean TryGetAuthorityAt(DateTimeOffset                    At,
                                         out RoamingNetworkTrustContext?   Authority,
                                         [NotNullWhen(false)] out String?  ErrorResponse)
        {
            Authority      = null;
            ErrorResponse  = null;
            return true;
        }

        #endregion

        #region TryGetEvaluationTime(At, out EvaluationTime, out ErrorResponse)

        /// <summary>
        /// The time this context was evaluated at, unless another time is given: another time needs another context, of the
        /// decisions of the app then.
        /// </summary>
        /// <param name="At">The time to verify at, or null.</param>
        /// <param name="EvaluationTime">The time this context was evaluated at.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryGetEvaluationTime(DateTimeOffset?                   At,
                                            out DateTimeOffset                EvaluationTime,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            EvaluationTime = EvaluatedAt;

            if (At is DateTimeOffset at && at != EvaluatedAt)
            {
                ErrorResponse = $"The trust context was evaluated at {EvaluatedAt.ToISO8601()}: recreate it to verify at {at.ToISO8601()}!";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryVerify(KeyStatement, OtherIdentityKeys, out HolderOf, out ErrorResponse, At = null)

        /// <summary>
        /// Verify the given key statement against this context: the one it was evaluated of, of its roaming network and its
        /// operator and with the same content, embedded or not and its signatures in any order, and every signature given and
        /// the quorum of the holders trusted locally checked again, at the time it was evaluated at. Another key statement,
        /// even one signed genuinely, needs another context: a context follows the keys of the operator to no successor.
        /// </summary>
        /// <param name="KeyStatement">The key statement, e.g. as a proof gives it.</param>
        /// <param name="OtherIdentityKeys">Other identity keys, e.g. those of the roaming network operator a proof gives.</param>
        /// <param name="HolderOf">The holder of every identity key trusted then, by its @id (RoamingNetworkKeyStatement.TryVerify(TrustedHolders, …)).</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">The time to verify at: the time it was evaluated at, which it is by default.</param>
        public Boolean TryVerify(RoamingNetworkKeyStatement                                        KeyStatement,
                                 IEnumerable<POIPublicKey>?                                        OtherIdentityKeys,
                                 [NotNullWhen(true)]  out IReadOnlyDictionary<String, String>?     HolderOf,
                                 [NotNullWhen(false)] out String?                                  ErrorResponse,
                                 DateTimeOffset?                                                   At   = null)
        {

            HolderOf = null;

            if (!TryGetEvaluationTime(At, out var at, out ErrorResponse))
                return false;

            if (KeyStatement.RoamingNetworkId         != RoamingNetworkId         ||
                KeyStatement.RoamingNetworkOperatorId != RoamingNetworkOperatorId ||
                DigestOf(KeyStatement)                != KeyStatementDigest)
            {
                ErrorResponse = "The roaming network operator or its key statement differs from the one of the trust context!";
                return false;
            }

            return KeyStatement.TryVerify(TrustedHolders,
                                          OtherIdentityKeys,
                                          out HolderOf,
                                          out ErrorResponse,
                                          TrustAnchor.MinConsensus,
                                          at);

        }

        #endregion


        #region (private static) DigestOf(KeyStatement)

        /// <summary>
        /// The SHA-256 of the content of the given key statement, as lowercase hex.
        /// </summary>
        private static String DigestOf(RoamingNetworkKeyStatement KeyStatement)

            => Convert.ToHexStringLower(SHA256.HashData(KeyStatement.GetContentBytes()));

        #endregion

    }

}
