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

using System.Diagnostics.CodeAnalysis;

using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate verifying one signature of a commit with a trusted key,
    /// e.g. by resolving the key of its key identification and calling VerifySignature.
    /// </summary>
    /// <param name="Commit">The signed commit.</param>
    /// <param name="Signature">One of its signatures.</param>
    public delegate Boolean VerifyCommitSignatureDelegate(RoamingNetworkCommit           Commit,
                                                          RoamingNetworkCommitSignature  Signature);


    public sealed partial class RoamingNetworkCommit
    {

        #region Data

        /// <summary>
        /// The versioned profile of the signature input of a commit signature.
        /// </summary>
        public const String SigningProfile = "wwcp-poi-commit-signature-v1";

        #endregion


        #region WithSignatures(Signatures)

        /// <summary>
        /// Return this commit with the given signatures added, unless it has them already.
        /// The identification stays the same.
        /// </summary>
        /// <param name="Signatures">Signatures of this commit.</param>
        public RoamingNetworkCommit WithSignatures(IEnumerable<RoamingNetworkCommitSignature> Signatures)
        {

            var newSignatures = Signatures.Where(signature => !this.Signatures.Contains(signature)).Distinct().ToArray();

            return newSignatures.Length == 0
                       ? this
                       : new RoamingNetworkCommit(ParentIds,
                                                  RoamingNetworkId,
                                                  Revision,
                                                  ETags,
                                                  ChangeSet,
                                                  Epoch,
                                                  IdentificationHash,
                                                  Profiles,
                                                  this.Signatures.AddRange(newSignatures));

        }

        #endregion

        #region CreateSignature(PrivateKey, KeyId, Algorithm, Role, Signer = null, SignedAt = null, Deterministic = true)

        /// <summary>
        /// Create a signature of this commit, e.g. to send it to the leading server; the commit stays unchanged.
        /// Deterministic ECDSA uses RFC 6979; private keys are never retained.
        /// </summary>
        /// <param name="PrivateKey">The private key.</param>
        /// <param name="KeyId">The application identifier of the key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="Role">The role of the signer, e.g. leader or roamingNetworkOperator.</param>
        /// <param name="Signer">The optional name of the signer.</param>
        /// <param name="SignedAt">When the commit is signed, now otherwise.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public RoamingNetworkCommitSignature CreateSignature(AsymmetricKeyParameter  PrivateKey,
                                                             String                  KeyId,
                                                             COSEAlgorithm           Algorithm,
                                                             String                  Role,
                                                             String?                 Signer          = null,
                                                             DateTimeOffset?         SignedAt        = null,
                                                             Boolean                 Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(PrivateKey);

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            // Without a value yet, only to get the signed fields exactly as they will be written.
            var unsigned  = new RoamingNetworkCommitSignature(Role, Signer, SignedAt ?? Timestamp.Now, Algorithm.Name, KeyId, "-");
            var value     = Algorithm.Sign(GetSigningBytes(unsigned), PrivateKey, Deterministic: Deterministic);

            return new RoamingNetworkCommitSignature(unsigned.Role,
                                                     unsigned.Signer,
                                                     unsigned.SignedAt,
                                                     unsigned.Algorithm,
                                                     unsigned.KeyId,
                                                     Convert.ToBase64String(value));

        }

        #endregion

        #region CreateSignature(PrivateKey, KeyId, Role, Signer = null, SignedAt = null, Algorithm = null, Deterministic = true)

        /// <summary>
        /// Create a signature of this commit with a Styx COSE key, honoring its algorithm restriction.
        /// </summary>
        /// <param name="PrivateKey">The private COSE key.</param>
        /// <param name="KeyId">The application identifier of the key.</param>
        /// <param name="Role">The role of the signer, e.g. leader or roamingNetworkOperator.</param>
        /// <param name="Signer">The optional name of the signer.</param>
        /// <param name="SignedAt">When the commit is signed, now otherwise.</param>
        /// <param name="Algorithm">The signature algorithm, the one of the key otherwise.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public RoamingNetworkCommitSignature CreateSignature(COSEKey          PrivateKey,
                                                             String           KeyId,
                                                             String           Role,
                                                             String?          Signer          = null,
                                                             DateTimeOffset?  SignedAt        = null,
                                                             COSEAlgorithm?   Algorithm       = null,
                                                             Boolean          Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(PrivateKey);

            var selected = Algorithm ?? PrivateKey.Algorithm ??
                           throw new ArgumentException("An explicit algorithm or a key algorithm is required.", nameof(Algorithm));

            if (PrivateKey.Algorithm is { } restricted && restricted != selected)
                throw new ArgumentException("The selected algorithm differs from the key's algorithm restriction.", nameof(Algorithm));

            return CreateSignature(PrivateKey.ToPrivateKey(), KeyId, selected, Role, Signer, SignedAt, Deterministic);

        }

        #endregion

        #region Sign           (PrivateKey, KeyId, Algorithm, Role, Signer = null, SignedAt = null, Deterministic = true)

        /// <summary>
        /// Return this commit with a new signature added. The identification stays the same.
        /// </summary>
        /// <param name="PrivateKey">The private key.</param>
        /// <param name="KeyId">The application identifier of the key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="Role">The role of the signer, e.g. leader or roamingNetworkOperator.</param>
        /// <param name="Signer">The optional name of the signer.</param>
        /// <param name="SignedAt">When the commit is signed, now otherwise.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public RoamingNetworkCommit Sign(AsymmetricKeyParameter  PrivateKey,
                                         String                  KeyId,
                                         COSEAlgorithm           Algorithm,
                                         String                  Role,
                                         String?                 Signer          = null,
                                         DateTimeOffset?         SignedAt        = null,
                                         Boolean                 Deterministic   = true)

            => WithSignatures([ CreateSignature(PrivateKey, KeyId, Algorithm, Role, Signer, SignedAt, Deterministic) ]);

        #endregion


        #region VerifySignature(Signature, PublicKey, ExpectedKeyId, out ErrorResponse)

        /// <summary>
        /// Verify the given signature of this commit with a trusted public key and its expected key identification.
        /// Unknown profiles, noncanonical Base64, mismatched keys and malformed inputs return false.
        /// </summary>
        /// <param name="Signature">A signature of this commit.</param>
        /// <param name="PublicKey">The trusted public key.</param>
        /// <param name="ExpectedKeyId">The key identification of the trusted public key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(RoamingNetworkCommitSignature     Signature,
                                       AsymmetricKeyParameter            PublicKey,
                                       String                            ExpectedKeyId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                ArgumentNullException.ThrowIfNull(Signature);
                ArgumentNullException.ThrowIfNull(PublicKey);

                if (PublicKey.IsPrivate)
                    throw new ArgumentException("Verification requires a public asymmetric key.", nameof(PublicKey));

                if (Signature.Profile != SigningProfile || Signature.Encoding != "base64")
                    throw new ArgumentException("Unsupported commit signature profile or encoding.");

                if (Signature.KeyId != ExpectedKeyId)
                    throw new ArgumentException("The signature key ID does not match the trusted key ID.");

                if (!COSEAlgorithm.TryParse(Signature.Algorithm, out var algorithm) || algorithm.Name != Signature.Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");

                var value = Convert.FromBase64String(Signature.Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Signature.Value)
                    throw new ArgumentException("Signature Value must contain nonempty canonical standard Base64.");

                return algorithm.Verify(GetSigningBytes(Signature), value, PublicKey, out ErrorResponse);

            }
            catch (Exception exception)
            {
                ErrorResponse = exception.Message;
                return false;
            }

        }

        #endregion

        #region VerifySignature(Signature, PublicKey, ExpectedKeyId, out ErrorResponse)

        /// <summary>
        /// Verify the given signature of this commit with a trusted Styx COSE public key, honoring its algorithm restriction.
        /// </summary>
        /// <param name="Signature">A signature of this commit.</param>
        /// <param name="PublicKey">The trusted public COSE key.</param>
        /// <param name="ExpectedKeyId">The key identification of the trusted public key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(RoamingNetworkCommitSignature     Signature,
                                       COSEKey                           PublicKey,
                                       String                            ExpectedKeyId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                ArgumentNullException.ThrowIfNull(Signature);
                ArgumentNullException.ThrowIfNull(PublicKey);

                if (PublicKey.IsPrivate)
                    throw new ArgumentException("Supply the public COSE key for verification.", nameof(PublicKey));

                if (PublicKey.Algorithm is { } restricted && Signature.Algorithm != restricted.Name)
                    throw new ArgumentException("The signature algorithm differs from the key's algorithm restriction.");

                return VerifySignature(Signature, PublicKey.ToPublicKey(), ExpectedKeyId, out ErrorResponse);

            }
            catch (Exception exception)
            {
                ErrorResponse = exception.Message;
                return false;
            }

        }

        #endregion


        #region GetSigningBytes(Signature)

        /// <summary>
        /// The signature input of the given signature of this commit, as Styx canonical JSON:
        /// the signed fields of the signature and the JSON representation of this commit
        /// with the signatures of its change set, but without the signatures of this commit.
        /// </summary>
        /// <param name="Signature">A signature of this commit; its value is not part of the input.</param>
        public Byte[] GetSigningBytes(RoamingNetworkCommitSignature Signature)
        {

            if (!COSEAlgorithm.TryParse(Signature.Algorithm, out var algorithm) || !algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.", nameof(Signature));

            var json = new JObject(
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Profile,    SigningProfile),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Algorithm,  Signature.Algorithm),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.KeyId,      Signature.KeyId),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Encoding,   "base64"),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.Role,       Signature.Role),
                           new JProperty(RoamingNetworkCommitSignature.PropertyKeys.SignedAt,   Signature.SignedAt.ToISO8601())
                       );

            if (Signature.Signer is not null)
                json.Add(RoamingNetworkCommitSignature.PropertyKeys.Signer, Signature.Signer);

            json.Add(PropertyKeys.Commit, ToJSON(Embedded: true, IncludeSignatures: false));

            return CanonicalJSON.ToUTF8Bytes(json);

        }

        #endregion

    }

}
