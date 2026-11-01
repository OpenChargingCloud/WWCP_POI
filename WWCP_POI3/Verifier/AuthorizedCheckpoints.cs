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
    /// A witness a verifier trusts, with the key it trusts for it (WWCP_POI.TS TrustedWitness): authorized by the verifier,
    /// never by a bundle it comes with.
    /// </summary>
    /// <param name="Witness">The witness, as its signatures name it.</param>
    /// <param name="Key">The key trusted for it.</param>
    public sealed record TrustedWitness(String        Witness,
                                        POIPublicKey  Key)
    {

        #region (static) TryParseAll(JSON, out Witnesses, out ErrorResponse)

        /// <summary>
        /// Try to read the given trusted witnesses: [ { "witness", "key" } ].
        /// </summary>
        /// <param name="JSON">The trusted witnesses.</param>
        /// <param name="Witnesses">The trusted witnesses.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryParseAll(JToken?                                                  JSON,
                                          [NotNullWhen(true)]  out IReadOnlyList<TrustedWitness>?  Witnesses,
                                          [NotNullWhen(false)] out String?                         ErrorResponse)
        {

            Witnesses = null;

            if (JSON is not JArray array)
            {
                ErrorResponse = "The trusted witnesses must be a JSON array!";
                return false;
            }

            var witnesses = new List<TrustedWitness>();

            foreach (var item in array)
            {

                if (item is not JObject json || json["witness"]?.Type != JTokenType.String || json["key"] is not JObject keyJSON)
                {
                    ErrorResponse = "A trusted witness names its witness and its key!";
                    return false;
                }

                if (!POIPublicKey.TryParse(keyJSON, out var key, out ErrorResponse))
                    return false;

                witnesses.Add(new TrustedWitness(json["witness"]!.Value<String>()!, key));

            }

            Witnesses      = witnesses;
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }


    /// <summary>
    /// A key compromised from the given time on, as the verifier was told by somebody it trusts, never by an evidence bundle:
    /// signatures of it and uses of it from then on do not count; ending normally does not end a valid historical signature.
    /// </summary>
    /// <param name="KeyId">The @id of the key, e.g. of a signing key, an identity key or a measurement key.</param>
    /// <param name="InvalidFrom">From when it is compromised.</param>
    public sealed record KeyRevocation(String          KeyId,
                                       DateTimeOffset  InvalidFrom);


    /// <summary>
    /// What a verifier requires of historical evidence (WWCP_POI.TS HistoricalMeterPolicy).
    /// </summary>
    /// <param name="Witnesses">The witnesses it trusts, if any.</param>
    /// <param name="RequiredWitnesses">How many of them must have signed a checkpoint.</param>
    /// <param name="RequirePublication">Whether a version must be proven included in a checkpoint.</param>
    /// <param name="Revocations">The keys compromised, as the verifier was told.</param>
    public sealed record HistoricalPolicy(IReadOnlyList<TrustedWitness>?  Witnesses            = null,
                                          UInt32                          RequiredWitnesses    = 0,
                                          Boolean                         RequirePublication   = false,
                                          IReadOnlyList<KeyRevocation>?   Revocations          = null)
    {

        /// <summary>
        /// Whether the key of the given @id is compromised at the given time.
        /// </summary>
        public Boolean IsRevoked(String          KeyId,
                                 DateTimeOffset  At)

            => Revocations?.Any(revocation => revocation.KeyId == KeyId && At >= revocation.InvalidFrom) == true;

    }


    /// <summary>
    /// The trust in the roaming network operator a historical document gives, under a trust context of it at that time
    /// (WWCP_POI.TS OperatorTrust.fromOperatorJSON): its key statement verified, and its signing keys where a trusted
    /// identity key of their holder certifies them, counted for that holder as the verifier knows it.
    /// </summary>
    internal sealed class RoamingNetworkOperatorTrust
    {

        #region Properties

        public RoamingNetworkTrustContext                               Context          { get; }
        public ImmutableArray<RoamingNetworkSigningKeyCertificate>      Certificates     { get; }

        private readonly ImmutableArray<POIPublicKey>                   identityKeys;
        private readonly IReadOnlyDictionary<String, String>            holderOf;
        private readonly String                                         operatorId;

        #endregion

        #region (private) Constructor(s)

        private RoamingNetworkOperatorTrust(RoamingNetworkTrustContext                       Context,
                                            ImmutableArray<RoamingNetworkSigningKeyCertificate>  Certificates,
                                            ImmutableArray<POIPublicKey>                     IdentityKeys,
                                            IReadOnlyDictionary<String, String>              HolderOf,
                                            String                                           OperatorId)
        {

            this.Context       = Context;
            this.Certificates  = Certificates;
            this.identityKeys  = IdentityKeys;
            this.holderOf      = HolderOf;
            this.operatorId    = OperatorId;

        }

        #endregion


        #region (static) TryCreate(Context, Operator, out Trust, out ErrorResponse)

        public static Boolean TryCreate(RoamingNetworkTrustContext                             Context,
                                        JObject                                                Operator,
                                        [NotNullWhen(true)]  out RoamingNetworkOperatorTrust?  Trust,
                                        [NotNullWhen(false)] out String?                       ErrorResponse)
        {

            Trust = null;

            // Read as WWCP_POI.TS RoamingNetworkOperator.parse reads it: its certificates and its key statement checked.
            if (!RoamingNetworkOperator.TryParse(Operator, out var parsed, out ErrorResponse))
                return false;

            if (parsed.KeyStatementError(Context.RoamingNetworkId) is String keysError)
            {
                ErrorResponse = keysError;
                return false;
            }

            if (parsed.KeyStatement is not RoamingNetworkKeyStatement statement)
            {
                ErrorResponse = "The roaming network operator states no keys!";
                return false;
            }

            if (!Context.TryVerify(statement, parsed.IdentityKeys, out var holderOf, out ErrorResponse))
                return false;

            Trust = new RoamingNetworkOperatorTrust(Context,
                                                    parsed.SigningKeyCertificates,
                                                    [.. statement.IdentityKeys, .. parsed.IdentityKeys],
                                                    holderOf,
                                                    parsed.Id.ToString());

            return true;

        }

        #endregion

        #region CertificateOf(Role, KeyId, SignedAt, Usage = Party)

        /// <summary>
        /// The certificate of the signing key a signature of the roaming network operator names: valid when it signed, one
        /// that may sign the given usage, certified by a trusted identity key of its holder; none unless all of that holds.
        /// </summary>
        public RoamingNetworkSigningKeyCertificate? CertificateOf(String          Role,
                                                                  String          KeyId,
                                                                  DateTimeOffset  SignedAt,
                                                                  String          Usage   = POIKeyUsages.Party)
        {

            if (Role != RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator)
                return null;

            var trustedIds = holderOf.Keys.ToHashSet(StringComparer.Ordinal);

            foreach (var certificate in Certificates)
                if (certificate.SigningKey.Id                         == KeyId      &&
                    certificate.RoamingNetworkOperatorId.ToString()   == operatorId &&
                    certificate.SigningKey.IsValidAt(SignedAt)                      &&
                    certificate.SigningKey.MaySign(Usage)                           &&
                    certificate.TryVerify(identityKeys, out _, out _, trustedIds))
                {
                    return certificate;
                }

            return null;

        }

        #endregion

        #region HolderOf(Certificate)

        /// <summary>
        /// The holder of the identity key certifying the given certificate, as the verifier knows it.
        /// </summary>
        public String? HolderOf(RoamingNetworkSigningKeyCertificate Certificate)

            => holderOf.TryGetValue(Certificate.KeyId, out var holder) ? holder : null;

        #endregion

    }


    /// <summary>
    /// A checkpoint verified under the local authorization of a verifier (WWCP_POI.TS verifyAuthorizedCheckpoint), shared by
    /// historical meter and price evidence: signed by as many holders of the roaming network operator as required, each
    /// signature checked with the operator and at the time it was signed, and by as many witnesses the verifier trusts as
    /// it requires. A witness is never authorized by its own bundle.
    /// </summary>
    /// <param name="Checkpoint">The checkpoint.</param>
    /// <param name="SignedBy">The holders of the roaming network operator that signed it.</param>
    /// <param name="WitnessedBy">The trusted witnesses that signed it.</param>
    public sealed record AuthorizedCheckpoint(RoamingNetworkCheckpoint  Checkpoint,
                                              ImmutableArray<String>    SignedBy,
                                              ImmutableArray<String>    WitnessedBy)
    {

        #region (static) TryVerify(Current, Checkpoint, Operator, Policy, out Verified, out ErrorResponse)

        /// <summary>
        /// Verify the given checkpoint under the given local authorization, with the given roaming network operator of its
        /// time, as the given policy requires: of its network and not later than the authorization; of an epoch verified;
        /// signed in the role of the operator by as many holders as required, each signature, of an algorithm of the epoch,
        /// signed when the checkpoint was made and not later than the authorization, of a key not compromised then, verified
        /// with a trust context of the operator at the time it was signed; and by as many trusted witnesses as required. An
        /// invalid or unauthorized extra signature grants no trust and spoils no quorum.
        /// </summary>
        /// <param name="Current">The current local authorization.</param>
        /// <param name="Checkpoint">The checkpoint.</param>
        /// <param name="Operator">The roaming network operator of its time, e.g. as an evidence gives it.</param>
        /// <param name="Policy">What the verifier requires.</param>
        /// <param name="Verified">The checkpoint and who signed it.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public static Boolean TryVerify(IRoamingNetworkTrustAuthority                    Current,
                                        RoamingNetworkCheckpoint                         Checkpoint,
                                        JToken?                                          Operator,
                                        HistoricalPolicy?                                Policy,
                                        [NotNullWhen(true)]  out AuthorizedCheckpoint?   Verified,
                                        [NotNullWhen(false)] out String?                 ErrorResponse)
        {

            Verified = null;

            var policy = Policy ?? new HistoricalPolicy();

            if (Checkpoint.RoamingNetworkId != Current.RoamingNetworkId || Checkpoint.CreatedAt > Current.EvaluatedAt)
            {
                ErrorResponse = "Foreign or future checkpoint!";
                return false;
            }

            if (Current.Epochs.FirstOrDefault(epoch => epoch.Number == Checkpoint.Epoch) is not RoamingNetworkEpoch epoch)
            {
                ErrorResponse = "The checkpoint epoch has no verified transition from the local policy!";
                return false;
            }

            if (!Current.TryGetAuthorityAt(Checkpoint.CreatedAt, out var authority, out ErrorResponse))
                return false;

            var required     = authority?.RequiredConsensus ?? Current.RequiredConsensus;
            var holders      = new HashSet<String>(StringComparer.Ordinal);
            var witnesses    = new HashSet<String>(StringComparer.Ordinal);
            var witnessKeys  = new HashSet<String>(StringComparer.Ordinal);

            foreach (var signature in Checkpoint.Signatures)
            {

                if (!epoch.Profiles.AllowsSignatureAlgorithm(signature.Algorithm) ||
                    signature.SignedAt < Checkpoint.CreatedAt                    ||
                    signature.SignedAt > Current.EvaluatedAt                     ||
                    policy.IsRevoked(signature.KeyId, signature.SignedAt))
                {
                    continue;
                }

                if (signature.Role == RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator)
                {

                    if (Operator is not JObject operatorJSON ||
                        !HistoricalTrustContext.TryGetOperatorContext(Current, operatorJSON, signature.SignedAt, out var context, out _))
                    {
                        continue;
                    }

                    required = Math.Max(required, context.RequiredConsensus);

                    if (RoamingNetworkOperatorTrust.TryCreate(context, operatorJSON, out var trust, out _) &&
                        trust.CertificateOf(signature.Role, signature.KeyId, signature.SignedAt) is RoamingNetworkSigningKeyCertificate certificate &&
                        !policy.IsRevoked(certificate.KeyId, signature.SignedAt) &&
                        trust.HolderOf(certificate) is String holder &&
                        Checkpoint.VerifySignature(signature, certificate.SigningKey.Key, certificate.SigningKey.Id, out _))
                    {
                        holders.Add(holder);
                    }

                }

                else if (signature.Role == RoamingNetworkCommitSignatureRoles.Witness)
                {

                    if (policy.Witnesses?.FirstOrDefault(witness => witness.Witness == signature.Signer && witness.Key.Id == signature.KeyId) is TrustedWitness witness &&
                        witness.Key.IsValidAt(signature.SignedAt)                &&
                        witness.Key.MaySign(POIKeyUsages.Party)                  &&
                        !witnessKeys.Contains(witness.Key.Id)                    &&
                        Checkpoint.VerifySignature(signature, witness.Key.Key, witness.Key.Id, out _))
                    {
                        witnesses.  Add(witness.Witness);
                        witnessKeys.Add(witness.Key.Id);
                    }

                }

            }

            if (holders.Count < required || witnesses.Count < policy.RequiredWitnesses)
            {
                ErrorResponse = "Historical checkpoint lacks the locally required RNO/witness quorum!";
                return false;
            }

            Verified       = new AuthorizedCheckpoint(Checkpoint, [.. holders], [.. witnesses]);
            ErrorResponse  = null;
            return true;

        }

        #endregion

    }

}
