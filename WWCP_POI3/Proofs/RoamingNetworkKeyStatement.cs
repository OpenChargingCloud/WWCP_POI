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

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;
using Org.BouncyCastle.X509;
using Org.BouncyCastle.Pkcs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// One signature of a key statement: by one identity key of the roaming network operator.
    /// </summary>
    /// <param name="Algorithm">The COSE name of the signature algorithm, e.g. ES512.</param>
    /// <param name="KeyId">The @id of the identity key that signed it.</param>
    /// <param name="Value">The signature value, canonical Base64.</param>
    public sealed record RoamingNetworkKeyStatementSignature(String  Algorithm,
                                                             String  KeyId,
                                                             String  Value)
    {

        /// <summary>
        /// Its JSON representation: {algorithm, keyId, value}.
        /// </summary>
        public JObject ToJSON()

            => new (new JProperty(RoamingNetworkKeyStatement.PropertyKeys.Algorithm,  Algorithm),
                    new JProperty(RoamingNetworkKeyStatement.PropertyKeys.KeyId,      KeyId),
                    new JProperty(RoamingNetworkKeyStatement.PropertyKeys.Value,      Value));

    }


    /// <summary>
    /// A key statement of a roaming network operator (SPECIFICATION.md, section 8.6): its identity keys and its
    /// minConsensus, signed with identity keys of as many holders. An app that pins identity keys of the operator, e.g. by
    /// scanning the QR code of a RoamingNetworkTrustAnchor, trusts the identity keys it names, and through them the
    /// signing keys they certify (RoamingNetworkSigningKeyCertificate), without trusting the network that names them. It
    /// changes only with the identity keys or the quorum, not with the signing keys, which each holder certifies on its
    /// own. It is part of the content: the roaming network operator embeds it as keyStatement, so that every proof of a
    /// charging station gives it, peers take it and witnesses see it.
    /// </summary>
    /// <remarks>
    /// It names the minConsensus of the operator and is signed by identity keys of as many holders, each signature in its
    /// signatures, so that the holders of a quorum sign it together (SPECIFICATION.md, section 4.1): each signs the same
    /// input with its own algorithm and keyId, and adds its signature to those of the others (WithSignature(…)). An
    /// operator without a quorum is one holder with a minConsensus of one, and signs it alone.
    /// </remarks>
    public sealed class RoamingNetworkKeyStatement
    {

        #region Data

        /// <summary>
        /// The name of the property of the roaming network operator embedding it.
        /// </summary>
        public const String JSONProperty    = "keyStatement";

        /// <summary>
        /// The JSON-LD context of the object, which names what its signatures sign and in which version: naming the
        /// minConsensus of the operator, signed by identity keys of as many holders. It is part of their signature input
        /// also where it is embedded, as in the roaming network operator.
        /// </summary>
        public const String JSONLDContext   = "https://open.charging.cloud/contexts/wwcp/roamingNetwork/keyStatement/v3";

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
            public const String  Context                   = "@context";

            /// <summary>
            /// The property of the identification of its roaming network.
            /// </summary>
            public const String  RoamingNetworkId          = "roamingNetworkId";

            /// <summary>
            /// The property of the identification of the roaming network operator stating it.
            /// </summary>
            public const String  RoamingNetworkOperatorId  = "roamingNetworkOperatorId";

            /// <summary>
            /// The property of the identity keys of the roaming network operator.
            /// </summary>
            public const String  IdentityKeys              = POIPartyKeys.IdentityKeysProperty;

            /// <summary>
            /// The property of how many holders of the keys of the operator of one kind sign.
            /// </summary>
            public const String  MinConsensus              = POIPartyKeys.MinConsensusProperty;

            /// <summary>
            /// The property of its signing timestamp.
            /// </summary>
            public const String  SignedAt                  = "signedAt";

            /// <summary>
            /// The property of the end of its validity.
            /// </summary>
            public const String  ValidUntil                = "validUntil";

            /// <summary>
            /// The property of its signatures.
            /// </summary>
            public const String  Signatures                = "signatures";

            /// <summary>
            /// The property of the algorithm of a signature.
            /// </summary>
            public const String  Algorithm                 = "algorithm";

            /// <summary>
            /// The property of the identification of the identity key of a signature.
            /// </summary>
            public const String  KeyId                     = "keyId";

            /// <summary>
            /// The property of the value of a signature.
            /// </summary>
            public const String  Value                     = "value";

        }

        #endregion


        /// <summary>
        /// The roaming network.
        /// </summary>
        public RoamingNetwork_Id                                    RoamingNetworkId            { get; }

        /// <summary>
        /// The roaming network operator stating it.
        /// </summary>
        public RoamingNetworkOperator_Id                            RoamingNetworkOperatorId    { get; }

        /// <summary>
        /// The identity keys of the roaming network operator, with their holders: they certify its signing keys and sign
        /// its next key statements, so a verifier trusting this statement may trust them as well, e.g. when the operator
        /// replaces an identity key.
        /// </summary>
        public ImmutableArray<POIPublicKey>                         IdentityKeys                { get; }

        /// <summary>
        /// The @ids of its identity keys per holder, as it names their holders, the keys naming none having one holder
        /// together: what a trust anchor of this statement pins, and what a verifier trusting it knows of the holders of
        /// the keys of a later statement (SPECIFICATION.md, section 8.6).
        /// </summary>
        public IEnumerable<IEnumerable<String>>                     Holders

            => IdentityKeys.GroupBy(identityKey => identityKey.HolderOrUnnamed, StringComparer.Ordinal).
                            Select (holder      => holder.Select(identityKey => identityKey.Id));

        /// <summary>
        /// How many holders of the identity keys sign it, and of the signing keys they certify attest a version: the
        /// minConsensus of the roaming network operator (SPECIFICATION.md, section 4.1).
        /// </summary>
        public Byte                                                 MinConsensus                { get; }

        /// <summary>
        /// When it was signed, signed as text: yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        public DateTimeOffset                                       SignedAt                    { get; }

        /// <summary>
        /// Until when it is valid: a verifier refuses it afterwards.
        /// </summary>
        public DateTimeOffset                                       ValidUntil                  { get; }

        /// <summary>
        /// Its signatures, each by another identity key.
        /// </summary>
        public ImmutableArray<RoamingNetworkKeyStatementSignature>  Signatures                  { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a key statement of its parts, naming the minConsensus of the operator, signed by identity keys of as many
        /// holders; Create(…) signs a new one.
        /// </summary>
        public RoamingNetworkKeyStatement(RoamingNetwork_Id                                 RoamingNetworkId,
                                          RoamingNetworkOperator_Id                         RoamingNetworkOperatorId,
                                          IEnumerable<POIPublicKey>                         IdentityKeys,
                                          Byte                                              MinConsensus,
                                          DateTimeOffset                                    SignedAt,
                                          DateTimeOffset                                    ValidUntil,
                                          IEnumerable<RoamingNetworkKeyStatementSignature>  Signatures)
        {

            this.IdentityKeys  = POIPublicKey.Ordered(IdentityKeys, "the identity keys of a key statement");
            this.Signatures    = [ .. Signatures ?? [] ];

            if (this.IdentityKeys.IsEmpty)
                throw new ArgumentException("A key statement names at least one identity key.", nameof(IdentityKeys));

            if (ValidUntil < SignedAt)
                throw new ArgumentException("A key statement is not valid until before it was signed.", nameof(ValidUntil));

            if (MinConsensus < 1)
                throw new ArgumentException("The minConsensus of a key statement is at least one.", nameof(MinConsensus));

            if (this.Signatures.IsEmpty ||
                this.Signatures.Any(signature => signature is null                               ||
                                                 String.IsNullOrWhiteSpace(signature.Algorithm)  ||
                                                 String.IsNullOrWhiteSpace(signature.KeyId)      ||
                                                 String.IsNullOrEmpty     (signature.Value)))
            {
                throw new ArgumentException("A key statement requires an algorithm, a key identification and a value of every signature.", nameof(Signatures));
            }

            if (this.Signatures.Select(signature => signature.KeyId).Distinct(StringComparer.Ordinal).Count() != this.Signatures.Length)
                throw new ArgumentException("An identity key signs a key statement once.", nameof(Signatures));

            this.RoamingNetworkId          = RoamingNetworkId;
            this.RoamingNetworkOperatorId  = RoamingNetworkOperatorId;
            this.MinConsensus              = MinConsensus;
            this.SignedAt                  = SignedAt;
            this.ValidUntil                = ValidUntil;

        }

        #endregion


        #region (static) Create(IdentityKey,      Algorithm,    MinConsensus, RoamingNetworkId, RoamingNetworkOperatorId, IdentityKeys, SignedAt, ValidUntil, Deterministic = true)

        /// <summary>
        /// Sign a new key statement with the given private identity key of a roaming network operator: with a minConsensus
        /// of one without a quorum, or as the first holder of its quorum, the others adding their signatures with
        /// WithSignature(…).
        /// </summary>
        /// <param name="IdentityKey">The private identity key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="MinConsensus">The minConsensus of the roaming network operator.</param>    
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
        /// <param name="IdentityKeys">The identity keys of the operator, which certify its signing keys and sign its next key statements.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="ValidUntil">Until when it is valid.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkKeyStatement Create(AsymmetricKeyParameter      IdentityKey,
                                                        COSEAlgorithm               Algorithm,
                                                        Byte                        MinConsensus,
                                                        RoamingNetwork_Id           RoamingNetworkId,
                                                        RoamingNetworkOperator_Id   RoamingNetworkOperatorId,
                                                        IEnumerable<POIPublicKey>   IdentityKeys,
                                                        DateTimeOffset              SignedAt,
                                                        DateTimeOffset              ValidUntil,
                                                        Boolean                     Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(IdentityKey);

            return Create([ (IdentityKey, Algorithm) ],
                          MinConsensus,
                          RoamingNetworkId,
                          RoamingNetworkOperatorId,
                          IdentityKeys,
                          SignedAt,
                          ValidUntil,
                          Deterministic);

        }

        #endregion

        #region (static) Create(SigningCOSEPair,                MinConsensus, RoamingNetworkId, RoamingNetworkOperatorId, IdentityKeys, SignedAt, ValidUntil, Deterministic = true)

        /// <summary>
        /// Sign a new key statement with the given private identity key of a roaming network operator: with a minConsensus
        /// of one without a quorum, or as the first holder of its quorum, the others adding their signatures with
        /// WithSignature(…).
        /// </summary>
        /// <param name="SigningCOSEPair">The COSE pair of the private identity key and its signature algorithm.</param>
        /// <param name="MinConsensus">The minConsensus of the roaming network operator.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
        /// <param name="IdentityKeys">The identity keys of the operator, which certify its signing keys and sign its next key statements.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="ValidUntil">Until when it is valid.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkKeyStatement Create(COSEKeyPair                 SigningCOSEPair,
                                                        Byte                        MinConsensus,
                                                        RoamingNetwork_Id           RoamingNetworkId,
                                                        RoamingNetworkOperator_Id   RoamingNetworkOperatorId,
                                                        IEnumerable<POIPublicKey>   IdentityKeys,
                                                        DateTimeOffset              SignedAt,
                                                        DateTimeOffset              ValidUntil,
                                                        Boolean                     Deterministic   = true)

            => Create([ SigningCOSEPair ],
                      MinConsensus,
                      RoamingNetworkId,
                      RoamingNetworkOperatorId,
                      IdentityKeys,
                      SignedAt,
                      ValidUntil,
                      Deterministic);

        #endregion

        #region (static) Create(IdentityKeys,     MinConsensus, RoamingNetworkId, RoamingNetworkOperatorId, IdentityKeys, SignedAt, ValidUntil, Deterministic = true)

        /// <summary>
        /// Sign a new key statement with the given private identity keys of the roaming network operator, e.g. of the
        /// holders of its quorum signing it at one place. Holders signing it each at their own place add their signatures
        /// with WithSignature(…).
        /// </summary>
        /// <param name="SigningIdentityKeys">The private identity keys signing it, each with its signature algorithm.</param>
        /// <param name="MinConsensus">The minConsensus of the roaming network operator.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
        /// <param name="IdentityKeys">The identity keys of the operator, which certify its signing keys and sign its next key statements.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="ValidUntil">Until when it is valid.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkKeyStatement Create(IEnumerable<(AsymmetricKeyParameter Key, COSEAlgorithm Algorithm)>  SigningIdentityKeys,
                                                        Byte                                                                MinConsensus,
                                                        RoamingNetwork_Id                                                   RoamingNetworkId,
                                                        RoamingNetworkOperator_Id                                           RoamingNetworkOperatorId,
                                                        IEnumerable<POIPublicKey>                                           IdentityKeys,
                                                        DateTimeOffset                                                      SignedAt,
                                                        DateTimeOffset                                                      ValidUntil,
                                                        Boolean                                                             Deterministic   = true)
        {

            var signingIdentityKeys = SigningIdentityKeys?.ToArray() ?? [];

            if (signingIdentityKeys.Length == 0)
                throw new ArgumentException("A key statement is signed by at least one identity key.", nameof(SigningIdentityKeys));

            // The first signature, then the others: each signs the same input with its own algorithm and keyId.
            var statement = new RoamingNetworkKeyStatement(
                                RoamingNetworkId,
                                RoamingNetworkOperatorId,
                                IdentityKeys,
                                MinConsensus,
                                Truncated(SignedAt),
                                Truncated(ValidUntil),
                                [ new RoamingNetworkKeyStatementSignature("-", "-", "-") ]
                            );

            statement = statement.Signed(signingIdentityKeys[0].Key, signingIdentityKeys[0].Algorithm, Deterministic, Replace: true);

            foreach (var (key, algorithm) in signingIdentityKeys.Skip(1))
                statement = statement.WithSignature(key, algorithm, Deterministic);

            return statement;

        }

        #endregion

        #region (static) Create(SigningCOSEPairs, MinConsensus, RoamingNetworkId, RoamingNetworkOperatorId, IdentityKeys, SignedAt, ValidUntil, Deterministic = true)

        /// <summary>
        /// Sign a new key statement with the given private identity keys of the roaming network operator, e.g. of the
        /// holders of its quorum signing it at one place. Holders signing it each at their own place add their signatures
        /// with WithSignature(…).
        /// </summary>
        /// <param name="SigningCOSEPairs">The private identity keys signing it, each with its signature algorithm.</param>
        /// <param name="MinConsensus">The minConsensus of the roaming network operator.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
        /// <param name="IdentityKeys">The identity keys of the operator, which certify its signing keys and sign its next key statements.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="ValidUntil">Until when it is valid.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkKeyStatement Create(IEnumerable<COSEKeyPair>    SigningCOSEPairs,
                                                        Byte                        MinConsensus,
                                                        RoamingNetwork_Id           RoamingNetworkId,
                                                        RoamingNetworkOperator_Id   RoamingNetworkOperatorId,
                                                        IEnumerable<POIPublicKey>   IdentityKeys,
                                                        DateTimeOffset              SignedAt,
                                                        DateTimeOffset              ValidUntil,
                                                        Boolean                     Deterministic   = true)
        {

            var signingCosePairs = SigningCOSEPairs?.ToArray() ?? [];

            if (signingCosePairs.Length == 0)
                throw new ArgumentException("A key statement is signed by at least one identity key.", nameof(SigningCOSEPairs));

            // The first signature, then the others: each signs the same input with its own algorithm and keyId.
            var statement = new RoamingNetworkKeyStatement(
                                RoamingNetworkId,
                                RoamingNetworkOperatorId,
                                IdentityKeys,
                                MinConsensus,
                                Truncated(SignedAt),
                                Truncated(ValidUntil),
                                [ new RoamingNetworkKeyStatementSignature("-", "-", "-") ]
                            );

            statement = statement.Signed(signingCosePairs[0].Private, signingCosePairs[0].Algorithm, Deterministic, Replace: true);

            foreach (var signingCOSEPair in signingCosePairs.Skip(1))
                statement = statement.WithSignature(signingCOSEPair, Deterministic);

            return statement;

        }

        #endregion


        #region WithSignature(IdentityKey, Algorithm, Deterministic = true)

        /// <summary>
        /// This key statement with the signature of one more identity key, e.g. of another holder of the quorum of the
        /// roaming network operator: the same input, signed with its own algorithm and keyId.
        /// </summary>
        /// <param name="IdentityKey">The private identity key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public RoamingNetworkKeyStatement WithSignature(AsymmetricKeyParameter  IdentityKey,
                                                        COSEAlgorithm           Algorithm,
                                                        Boolean                 Deterministic   = true)

            => Signed(
                   IdentityKey,
                   Algorithm,
                   Deterministic,
                   Replace: false
               );

        #endregion

        #region WithSignature(CosePair,               Deterministic = true)

        /// <summary>
        /// This key statement with the signature of one more identity key, e.g. of another holder of the quorum of the
        /// roaming network operator: the same input, signed with its own algorithm and keyId.
        /// </summary>
        /// <param name="CosePair">The COSE pair of the private identity key and its signature algorithm.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public RoamingNetworkKeyStatement WithSignature(COSEKeyPair  CosePair,
                                                        Boolean      Deterministic   = true)

            => Signed(
                   CosePair.Private,
                   CosePair.Algorithm,
                   Deterministic,
                   Replace: false
               );

        #endregion


        #region (private) Signed(IdentityKey, Algorithm, Deterministic, Replace)

        /// <summary>
        /// This key statement with the signature of the given identity key, in place of its signatures or in addition to
        /// them.
        /// </summary>
        private RoamingNetworkKeyStatement Signed(AsymmetricKeyParameter  IdentityKey,
                                                  COSEAlgorithm           Algorithm,
                                                  Boolean                 Deterministic,
                                                  Boolean                 Replace)
        {

            ArgumentNullException.ThrowIfNull(IdentityKey);

            if (!IdentityKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(IdentityKey));

            var keyId = POIPublicKey.From(IdentityKey).Id;

            if (!Replace && Signatures.Any(signature => signature.KeyId == keyId))
                throw new ArgumentException($"The identity key '{keyId}' signed the key statement already!", nameof(IdentityKey));

            var signature = new RoamingNetworkKeyStatementSignature(
                                Algorithm.Name,
                                keyId,
                                Convert.ToBase64String(Algorithm.Sign(GetSigningBytes(Algorithm.Name, keyId), IdentityKey, Deterministic: Deterministic))
                            );

            return new RoamingNetworkKeyStatement(RoamingNetworkId,
                                                  RoamingNetworkOperatorId,
                                                  IdentityKeys,
                                                  MinConsensus,
                                                  SignedAt,
                                                  ValidUntil,
                                                  Replace ? [ signature ] : [ .. Signatures, signature ]);

        }

        #endregion

        #region IsValidAt(Timestamp = null)

        /// <summary>
        /// Whether it is valid at the given time: not before it was signed, not after its validUntil.
        /// </summary>
        public Boolean IsValidAt(DateTimeOffset? Timestamp = null)
        {

            var timestamp = Timestamp ?? org.GraphDefined.Vanaheimr.Illias.Timestamp.Now;

            return SignedAt  <= timestamp &&
                   timestamp <= ValidUntil;

        }

        #endregion

        #region VerifySignature(IdentityKey, out ErrorResponse)

        /// <summary>
        /// Verify the signature of this key statement by the given public identity key, whose @id one of its signatures
        /// names. Unknown profiles, noncanonical Base64, other keys and malformed inputs return false.
        /// </summary>
        /// <param name="IdentityKey">The public identity key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(POIPublicKey                      IdentityKey,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (Signatures.FirstOrDefault(signature => signature.KeyId == IdentityKey.Id) is not RoamingNetworkKeyStatementSignature signature)
            {
                ErrorResponse = "The key statement is signed by another key.";
                return false;
            }

            return VerifySignature(signature, IdentityKey, out ErrorResponse);

        }

        #endregion

        #region (private) VerifySignature(Signature, IdentityKey, out ErrorResponse)

        private Boolean VerifySignature(RoamingNetworkKeyStatementSignature  Signature,
                                        POIPublicKey                         IdentityKey,
                                        [NotNullWhen(false)] out String?     ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                if (Signature.KeyId != IdentityKey.Id)
                    throw new ArgumentException("The key statement is signed by another key.");

                if (!COSEAlgorithm.TryParse(Signature.Algorithm, out var algorithm) || algorithm.Name != Signature.Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");

                if (IdentityKey.Key.Algorithm is { } restricted && restricted.Name != Signature.Algorithm)
                    throw new ArgumentException("The signature algorithm differs from the key's algorithm restriction.");

                var value = Convert.FromBase64String(Signature.Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Signature.Value)
                    throw new ArgumentException("The value must contain nonempty canonical standard Base64.");

                return algorithm.Verify(GetSigningBytes(Signature.Algorithm, Signature.KeyId), value, IdentityKey.Key.ToPublicKey(), out ErrorResponse);

            }
            catch (Exception exception)
            {
                ErrorResponse = exception.Message;
                return false;
            }

        }

        #endregion

        #region TryVerify(TrustedIdentityKeyIds, OtherIdentityKeys, out ErrorResponse, MinConsensus = 1, At = null)

        /// <summary>
        /// Verify this key statement against identity keys the verifier trusts by their @ids, each of a holder of its own:
        /// as TryVerify(TrustedHolders, …) does. A verifier knowing that one holder holds several of them, e.g. by a trust
        /// anchor, names them per holder there.
        /// </summary>
        /// <param name="TrustedIdentityKeyIds">The @ids of the identity keys the verifier trusts, each of a holder of its own.</param>
        /// <param name="OtherIdentityKeys">Other identity keys, whose @ids the verifier may trust.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="MinConsensus">How many holders of trusted identity keys must have signed it at least, e.g. as a trust anchor names it.</param>
        /// <param name="At">An optional time it must be valid at, e.g. now.</param>
        public Boolean TryVerify(IEnumerable<String>                 TrustedIdentityKeyIds,
                                 IEnumerable<POIPublicKey>?          OtherIdentityKeys,
                                 [NotNullWhen(false)] out String?    ErrorResponse,
                                 Byte                                MinConsensus   = 1,
                                 DateTimeOffset?                     At             = null)

            => TryVerify(TrustedIdentityKeyIds.Select(id => (IEnumerable<String>) [ id ]),
                         OtherIdentityKeys,
                         out _,
                         out ErrorResponse,
                         MinConsensus,
                         At);

        #endregion

        #region TryVerify(TrustedHolders, OtherIdentityKeys, out HolderOf, out ErrorResponse, MinConsensus = 1, At = null)

        /// <summary>
        /// Verify this key statement against identity keys the verifier trusts by their @ids, per holder, e.g. those of a
        /// trust anchor it scanned: it must be signed by trusted identity keys of as many holders as the given minConsensus
        /// and its own require, the keys given among its own identity keys or among the given other ones, e.g. those of the
        /// roaming network operator a proof gives, each found by its @id, the SHA-256 thumbprint of the key, and valid when
        /// it was signed. Every signature must be valid; those of keys not trusted are not counted. The holders are counted
        /// as the verifier knows them, never as this statement names them, and this statement must name the trusted keys it
        /// names as the verifier knows their holders: one holder for the keys of one, another for those of every other
        /// (SPECIFICATION.md, section 8.6). So no holder counts twice by naming a key of its own as another's. With a time
        /// given, it must also be valid then (IsValidAt); without, only its signatures are verified, e.g. as part of the
        /// content.
        /// </summary>
        /// <param name="TrustedHolders">The @ids of the identity keys the verifier trusts, per holder.</param>
        /// <param name="OtherIdentityKeys">Other identity keys, whose @ids the verifier may trust.</param>
        /// <param name="HolderOf">The holder of every identity key the verifier trusts then, by its @id: those of this statement, and the trusted ones, as this statement names their holders or, where it names none of a holder, as the verifier knows it.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="MinConsensus">How many holders of trusted identity keys must have signed it at least, e.g. as a trust anchor names it.</param>
        /// <param name="At">An optional time it must be valid at, e.g. now.</param>
        public Boolean TryVerify(IEnumerable<IEnumerable<String>>                                  TrustedHolders,
                                 IEnumerable<POIPublicKey>?                                        OtherIdentityKeys,
                                 [NotNullWhen(true)]  out IReadOnlyDictionary<String, String>?     HolderOf,
                                 [NotNullWhen(false)] out String?                                  ErrorResponse,
                                 Byte                                                              MinConsensus   = 1,
                                 DateTimeOffset?                                                   At             = null)
        {

            HolderOf = null;

            var trustedHolders  = TrustedHolders.Select(keys => keys.ToArray()).ToArray();
            var trusted         = new Dictionary<String, Int32>(StringComparer.Ordinal);

            for (var holder = 0; holder < trustedHolders.Length; holder++)
                foreach (var id in trustedHolders[holder])
                    if (!trusted.TryAdd(id, holder))
                    {
                        ErrorResponse = $"The identity key '{id}' is trusted as one of two holders!";
                        return false;
                    }

            // This statement names the trusted keys as the verifier knows their holders.
            var nameOf    = new Dictionary<Int32, String>();
            var holderOf  = new Dictionary<String, Int32>(StringComparer.Ordinal);

            foreach (var identityKey in IdentityKeys)
            {

                if (!trusted.TryGetValue(identityKey.Id, out var holder))
                    continue;

                var name = identityKey.HolderOrUnnamed;

                if ((nameOf.  TryGetValue(holder, out var named) && named != name) ||
                    (holderOf.TryGetValue(name,   out var other) && other != holder))
                {
                    ErrorResponse = $"The key statement names the holder of the trusted identity key '{identityKey.Id}' otherwise than it is trusted!";
                    return false;
                }

                nameOf  [holder] = name;
                holderOf[name]   = holder;

            }

            var keys      = IdentityKeys.Concat(OtherIdentityKeys ?? []).ToArray();
            var required  = Math.Max(MinConsensus, this.MinConsensus);
            var holders   = new HashSet<Int32>();

            if (Signatures.Length == 1 && required == 1 && !trusted.ContainsKey(Signatures[0].KeyId))
            {
                ErrorResponse = $"The key statement is signed by the key '{Signatures[0].KeyId}', which is not trusted!";
                return false;
            }

            foreach (var signature in Signatures)
            {

                if (keys.FirstOrDefault(key => key.Id == signature.KeyId) is not POIPublicKey identityKey)
                {
                    ErrorResponse = $"The identity key '{signature.KeyId}' that signed the key statement is not given!";
                    return false;
                }

                if (!identityKey.IsValidAt(SignedAt))
                {
                    ErrorResponse = $"The identity key '{signature.KeyId}' was not valid when the key statement was signed!";
                    return false;
                }

                if (!VerifySignature(signature, identityKey, out ErrorResponse))
                    return false;

                if (trusted.TryGetValue(signature.KeyId, out var holder))
                    holders.Add(holder);

            }

            if (holders.Count < required)
            {
                ErrorResponse = $"The key statement is signed by trusted identity keys of {holders.Count} holder{(holders.Count == 1 ? "" : "s")}, not of {required}!";
                return false;
            }

            if (At is DateTimeOffset at && !IsValidAt(at))
            {
                ErrorResponse = $"The key statement is not valid at {at.ToISO8601()}: it was signed at {SignedAt.ToISO8601()} and is valid until {ValidUntil.ToISO8601()}!";
                return false;
            }

            // Trusted then: its own identity keys, as it names their holders, and the trusted ones it does not name, as it
            // names the holder of another key of theirs, or else as a holder of its own the verifier knows.
            var result = IdentityKeys.ToDictionary(identityKey => identityKey.Id, identityKey => identityKey.HolderOrUnnamed, StringComparer.Ordinal);

            for (var holder = 0; holder < trustedHolders.Length; holder++)
                foreach (var id in trustedHolders[holder])
                    result.TryAdd(id, nameOf.TryGetValue(holder, out var name) ? name : $"\0{trustedHolders[holder][0]}");

            HolderOf       = result;
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region GetSigningBytes()

        /// <summary>
        /// The signature input of the first signature of this key statement, as Styx canonical JSON: its signed fields,
        /// without its value.
        /// </summary>
        public Byte[] GetSigningBytes()

            => GetSigningBytes(Signatures[0].Algorithm, Signatures[0].KeyId);

        #endregion

        #region GetSigningBytes(Algorithm, KeyId)

        /// <summary>
        /// The signature input of a signature of this key statement with the given algorithm and identity key, as Styx
        /// canonical JSON: its JSON-LD context, what it states, the algorithm and the keyId; no signature.
        /// </summary>
        /// <param name="Algorithm">The COSE name of the signature algorithm.</param>
        /// <param name="KeyId">The @id of the identity key.</param>
        public Byte[] GetSigningBytes(String  Algorithm,
                                      String  KeyId)
        {

            var json = Content();

            json.AddFirst(new JProperty(PropertyKeys.Context, JSONLDContext));
            json.Add(PropertyKeys.Algorithm,  Algorithm);
            json.Add(PropertyKeys.KeyId,      KeyId);

            return CanonicalJSON.ToUTF8Bytes(json);

        }

        #endregion

        private static String SerializeToBase64(AsymmetricKeyParameter key)
        {

            byte[] der;

            if (key.IsPrivate)
            {
                // PKCS#8 PrivateKeyInfo
                var privateKeyInfo = PrivateKeyInfoFactory.CreatePrivateKeyInfo(key);
                der = privateKeyInfo.GetDerEncoded();
            }
            else
            {
                // X.509 SubjectPublicKeyInfo
                var publicKeyInfo = SubjectPublicKeyInfoFactory.CreateSubjectPublicKeyInfo(key);
                der = publicKeyInfo.GetDerEncoded();
            }

            return Convert.ToBase64String(der);

        }



        #region (static) TryParse(JSON, out KeyStatement, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a key statement, read with strings as strings: with its
        /// minConsensus and its signatures. Its signatures are not verified here.
        /// </summary>
        public static Boolean TryParse(JObject                                               JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkKeyStatement?  KeyStatement,
                                       [NotNullWhen(false)] out String?                      ErrorResponse)
        {

            KeyStatement = null;

            try
            {

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the key statement must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatory(PropertyKeys.RoamingNetworkId,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse) ||
                    !JSON.ParseMandatory(PropertyKeys.RoamingNetworkOperatorId,
                                         "roaming network operator identification",
                                         RoamingNetworkOperator_Id.TryParse,
                                         out RoamingNetworkOperator_Id roamingNetworkOperatorId,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (!TryParseKeys(JSON, PropertyKeys.IdentityKeys, out var identityKeys, out ErrorResponse))
                    return false;

                if (identityKeys.Count == 0)
                {
                    ErrorResponse = $"A key statement requires its {PropertyKeys.IdentityKeys}!";
                    return false;
                }

                if (!TryParseTimestamp(JSON, PropertyKeys.SignedAt,   out var signedAt) ||
                    !TryParseTimestamp(JSON, PropertyKeys.ValidUntil, out var validUntil))
                {
                    ErrorResponse = $"The {PropertyKeys.SignedAt} and {PropertyKeys.ValidUntil} of a key statement must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                if (JSON[PropertyKeys.MinConsensus] is not JToken minConsensusJSON  ||
                    minConsensusJSON.Type != JTokenType.Integer                     ||
                    minConsensusJSON.Value<Int64>() is < 1 or > Byte.MaxValue)
                {
                    ErrorResponse = $"A key statement requires its {PropertyKeys.MinConsensus}, a whole number from 1 to {Byte.MaxValue}!";
                    return false;
                }

                if (JSON[PropertyKeys.Signatures] is not JArray signaturesJSON || signaturesJSON.Count == 0)
                {
                    ErrorResponse = $"A key statement requires its {PropertyKeys.Signatures}!";
                    return false;
                }

                var signatures = new List<RoamingNetworkKeyStatementSignature>();

                foreach (var signatureJSON in signaturesJSON)
                {

                    if (signatureJSON is not JObject signatureObject                                                       ||
                        Text(signatureObject, PropertyKeys.Algorithm) is not String signatureAlgorithm || signatureAlgorithm.Length == 0 ||
                        Text(signatureObject, PropertyKeys.KeyId)     is not String signatureKeyId     || signatureKeyId.    Length == 0 ||
                        Text(signatureObject, PropertyKeys.Value)     is not String signatureValue     || signatureValue.    Length == 0)
                    {
                        ErrorResponse = "Every signature of a key statement requires an algorithm, a key identification and a value!";
                        return false;
                    }

                    signatures.Add(new RoamingNetworkKeyStatementSignature(signatureAlgorithm, signatureKeyId, signatureValue));

                }

                KeyStatement   = new RoamingNetworkKeyStatement(roamingNetworkId,
                                                                roamingNetworkOperatorId,
                                                                identityKeys,
                                                                (Byte) minConsensusJSON.Value<Int64>(),
                                                                signedAt,
                                                                validUntil,
                                                                signatures);

                ErrorResponse  = null;
                return true;

            }
            catch (Exception e)
            {
                KeyStatement   = null;
                ErrorResponse  = "Invalid key statement: " + e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomKeyStatementSerializer = null)

        /// <summary>
        /// Return the JSON representation of this key statement: what it states and its signatures.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomKeyStatementSerializer">An optional custom serializer of key statements.</param>
        public JObject ToJSON(Boolean                                                       Embedded                       = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkKeyStatement>?  CustomKeyStatementSerializer   = null)
        {

            var json = Content();

            if (!Embedded)
                json.AddFirst(new JProperty(PropertyKeys.Context, JSONLDContext));

            json.Add(PropertyKeys.Signatures,      new JArray(Signatures.Select(signature => signature.ToJSON())));

            return CustomKeyStatementSerializer is not null
                       ? CustomKeyStatementSerializer(this, json)
                       : json;

        }

        #endregion


        #region (private) Content()

        /// <summary>
        /// What this key statement states, without its signatures: the identity keys and the minConsensus of the operator.
        /// </summary>
        private JObject Content()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.RoamingNetworkId,          RoamingNetworkId.        ToString()),
                           new JProperty(PropertyKeys.RoamingNetworkOperatorId,  RoamingNetworkOperatorId.ToString()),
                           new JProperty(PropertyKeys.IdentityKeys,              new JArray(IdentityKeys.Select(identityKey => identityKey.ToJSON(Embedded: true))))
                       );

            json.Add(PropertyKeys.MinConsensus,      MinConsensus);
            json.Add(PropertyKeys.SignedAt,          SignedAt.  ToISO8601());
            json.Add(PropertyKeys.ValidUntil,        ValidUntil.ToISO8601());

            return json;

        }

        #endregion

        #region (private static) TryParseKeys(JSON, PropertyName, out Keys, out ErrorResponse)

        private static Boolean TryParseKeys(JObject                           JSON,
                                            String                            PropertyName,
                                            out List<POIPublicKey>            Keys,
                                            [NotNullWhen(false)] out String?  ErrorResponse)
        {

            Keys = [];

            if (JSON[PropertyName] is null)
            {
                ErrorResponse = null;
                return true;
            }

            if (JSON[PropertyName] is not JArray keysJSON)
            {
                ErrorResponse = $"The {PropertyName} of a key statement must be a JSON array!";
                return false;
            }

            foreach (var keyJSON in keysJSON)
            {

                if (keyJSON is not JObject keyObject || !POIPublicKey.TryParse(keyObject, out var key, out var keyError))
                {
                    ErrorResponse = $"Invalid key in the {PropertyName} of a key statement!";
                    return false;
                }

                Keys.Add(key);

            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region (private static) Truncated(Timestamp)

        /// <summary>
        /// The given timestamp to the millisecond, as it is signed.
        /// </summary>
        private static DateTimeOffset Truncated(DateTimeOffset Timestamp)

            => DateTimeOffset.Parse(Timestamp.ToISO8601(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);

        #endregion

        #region (private static) Text(JSON, PropertyName)

        private static String? Text(JObject JSON, String PropertyName)

            => JSON[PropertyName]?.Type == JTokenType.String
                   ? JSON[PropertyName]!.Value<String>()
                   : null;

        #endregion

        #region (private static) TryParseTimestamp(JSON, PropertyName, out Timestamp)

        /// <summary>
        /// A mandatory timestamp, written exactly as yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        private static Boolean TryParseTimestamp(JObject             JSON,
                                                 String              PropertyName,
                                                 out DateTimeOffset  Timestamp)
        {

            Timestamp = default;

            return Text(JSON, PropertyName) is String text &&
                   DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out Timestamp) &&
                   Timestamp.ToISO8601() == text;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"Key statement of {RoamingNetworkOperatorId} for {RoamingNetworkId}: {IdentityKeys.Length} identity keys, minConsensus {MinConsensus}, valid until {ValidUntil.ToISO8601()}, signed by {String.Join(", ", Signatures.Select(signature => $"'{signature.KeyId}'"))}";

        #endregion

    }

}
