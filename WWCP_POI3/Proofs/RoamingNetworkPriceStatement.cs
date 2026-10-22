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
using System.Globalization;
using System.Security.Cryptography;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A delegate called for every price statement a history takes.
    /// </summary>
    /// <param name="PriceStatement">The price statement.</param>
    public delegate void OnRoamingNetworkPriceStatementDelegate(RoamingNetworkPriceStatement PriceStatement);


    /// <summary>
    /// A price statement (SPECIFICATION.md, section 12): the price status of a charging pool, a charging station or an EVSE,
    /// signed by its charging station operator with one of its signing keys at the moment it sets the price. Prices at
    /// runtime change by the minute and have no versions; their evidence is this statement, chained per entity by its
    /// sequence and the digest of the statement before, so that statements are neither reordered nor withdrawn unnoticed.
    /// A driver's app checks it against the proof of the charging station (RoamingNetworkProof), which gives the signing
    /// keys of the operator, and later against a checkpoint naming it (RoamingNetworkPriceStatementProof).
    /// </summary>
    /// <remarks>
    /// Its price status is kept as it was given, so that its signature and its digest are calculated over the canonical JSON
    /// of exactly that; read it from text with TryParse(Text, …), numbers with a fraction as decimals.
    /// </remarks>
    public sealed class RoamingNetworkPriceStatement
    {

        #region Data

        /// <summary>
        /// The role a price statement is signed in: by the charging station operator.
        /// </summary>
        public const String Role            = RoamingNetworkCommitSignatureRoles.ChargingStationOperator;

        /// <summary>
        /// The JSON-LD context of the object, which names what its signature signs and in which version: it is part of its
        /// signature input and its digest also where it is embedded.
        /// </summary>
        public const String JSONLDContext   = "https://open.charging.cloud/contexts/wwcp+json/roamingNetwork/priceStatement/v1";

        private String? digest;

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
            public const String  Context           = "@context";

            /// <summary>
            /// The property of the identification of its roaming network.
            /// </summary>
            public const String  RoamingNetworkId  = "roamingNetworkId";

            /// <summary>
            /// The property of the type of its entity.
            /// </summary>
            public const String  EntityType        = "entityType";

            /// <summary>
            /// The property of the identification of its entity.
            /// </summary>
            public const String  EntityId          = "entityId";

            /// <summary>
            /// The property of the price status it states.
            /// </summary>
            public const String  PriceStatus       = "priceStatus";

            /// <summary>
            /// The property of the beginning of its validity.
            /// </summary>
            public const String  ValidFrom         = "validFrom";

            /// <summary>
            /// The property of the end of its validity.
            /// </summary>
            public const String  ValidUntil        = "validUntil";

            /// <summary>
            /// The property of its position in the chain of the statements of its entity.
            /// </summary>
            public const String  Sequence          = "sequence";

            /// <summary>
            /// The property of the digest of the statement before.
            /// </summary>
            public const String  Previous          = "previous";

            /// <summary>
            /// The property of the latest checkpoint known when it was signed.
            /// </summary>
            public const String  After             = "after";

            /// <summary>
            /// The property of the role it is signed in.
            /// </summary>
            public const String  Role              = "role";

            /// <summary>
            /// The property of the charging station operator signing it.
            /// </summary>
            public const String  Signer            = "signer";

            /// <summary>
            /// The property of its signing timestamp.
            /// </summary>
            public const String  SignedAt          = "signedAt";

            /// <summary>
            /// The property of its signature algorithm.
            /// </summary>
            public const String  Algorithm         = "algorithm";

            /// <summary>
            /// The property of the identification of its key.
            /// </summary>
            public const String  KeyId             = "keyId";

            /// <summary>
            /// The property of its signature value.
            /// </summary>
            public const String  Value             = "value";

        }

        #endregion


        /// <summary>
        /// The roaming network.
        /// </summary>
        public RoamingNetwork_Id             RoamingNetworkId    { get; }

        /// <summary>
        /// The type of the entity: ChargingPool, ChargingStation or EVSE.
        /// </summary>
        public String                        EntityType          { get; }

        /// <summary>
        /// The identification of the entity.
        /// </summary>
        public String                        EntityId            { get; }

        /// <summary>
        /// The price status it states.
        /// </summary>
        public ChargingPrices                PriceStatus         { get; }

        /// <summary>
        /// The price status as it was given and signed.
        /// </summary>
        public JObject                       PriceStatusJSON     { get; }

        /// <summary>
        /// Since when it is valid, not before it was signed.
        /// </summary>
        public DateTimeOffset                ValidFrom           { get; }

        /// <summary>
        /// Until when it is valid, if it says: until the next statement of the entity otherwise.
        /// </summary>
        public DateTimeOffset?               ValidUntil          { get; }

        /// <summary>
        /// The position of this statement in the chain of the statements of the entity: 0 for the first one.
        /// </summary>
        public UInt64                        Sequence            { get; }

        /// <summary>
        /// The digest of the statement before, lowercase hex: absent for sequence 0.
        /// </summary>
        public String?                       Previous            { get; }

        /// <summary>
        /// The latest checkpoint the operator knew when it signed: the statement was made after it.
        /// </summary>
        public RoamingNetworkCheckpoint_Id?  After               { get; }

        /// <summary>
        /// The charging station operator signing it, the operator of the entity.
        /// </summary>
        public ChargingStationOperator_Id    Signer              { get; }

        /// <summary>
        /// When it was signed, signed as text: yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        public DateTimeOffset                SignedAt            { get; }

        /// <summary>
        /// The COSE name of the signature algorithm, e.g. ES256.
        /// </summary>
        public String                        Algorithm           { get; }

        /// <summary>
        /// The identification of the key: the @id of a signing key of the operator.
        /// </summary>
        public String                        KeyId               { get; }

        /// <summary>
        /// The signature value, canonical Base64.
        /// </summary>
        public String                        Value               { get; }

        /// <summary>
        /// The digest of this statement: the SHA-256 of its canonical JSON with its JSON-LD context, signature included, as
        /// 64 lowercase hexadecimal digits. The next statement of the entity names it as its previous one, and a checkpoint
        /// takes it as a leaf.
        /// </summary>
        public String                        Digest
            => digest ??= Convert.ToHexStringLower(SHA256.HashData(CanonicalJSON.ToUTF8Bytes(ToJSON(Embedded: false))));

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a price statement of its parts; Create(…) signs a new one.
        /// </summary>
        public RoamingNetworkPriceStatement(RoamingNetwork_Id             RoamingNetworkId,
                                            String                        EntityType,
                                            String                        EntityId,
                                            JObject                       PriceStatusJSON,
                                            DateTimeOffset                ValidFrom,
                                            DateTimeOffset?               ValidUntil,
                                            UInt64                        Sequence,
                                            String?                       Previous,
                                            RoamingNetworkCheckpoint_Id?  After,
                                            ChargingStationOperator_Id    Signer,
                                            DateTimeOffset                SignedAt,
                                            String                        Algorithm,
                                            String                        KeyId,
                                            String                        Value)
        {

            if (!IsEntityType(EntityType))
                throw new ArgumentException("A price statement is one of a ChargingPool, a ChargingStation or an EVSE.", nameof(EntityType));

            if (OperatorOf(EntityType, EntityId) is not ChargingStationOperator_Id operatorId)
                throw new ArgumentException($"Invalid identification of a {EntityType}.", nameof(EntityId));

            if (operatorId != Signer)
                throw new ArgumentException($"A price statement of the {EntityType} '{EntityId}' is signed by its operator '{operatorId}'.", nameof(Signer));

            ArgumentNullException.ThrowIfNull(PriceStatusJSON);

            if (!ChargingPrices.TryParse(PriceStatusJSON, out var priceStatus, out var priceStatusError))
                throw new ArgumentException(priceStatusError, nameof(PriceStatusJSON));

            if (ValidFrom < SignedAt)
                throw new ArgumentException("A price statement is not valid before it was signed.", nameof(ValidFrom));

            if (ValidUntil.HasValue && ValidUntil.Value <= ValidFrom)
                throw new ArgumentException("A price statement must be valid until after it is valid from.", nameof(ValidUntil));

            if ((Sequence == 0) != (Previous is null))
                throw new ArgumentException("A price statement names the digest of the one before it, unless it is the first one of its entity.", nameof(Previous));

            if (Previous is not null && !IsDigest(Previous))
                throw new ArgumentException("The digest of the statement before must be 64 lowercase hexadecimal digits.", nameof(Previous));

            if (String.IsNullOrWhiteSpace(Algorithm) || String.IsNullOrWhiteSpace(KeyId) || String.IsNullOrEmpty(Value))
                throw new ArgumentException("A price statement requires an algorithm, a key identification and a value.");

            this.RoamingNetworkId  = RoamingNetworkId;
            this.EntityType        = EntityType;
            this.EntityId          = EntityId;
            this.PriceStatus       = priceStatus;
            this.PriceStatusJSON   = (JObject) PriceStatusJSON.DeepClone();
            this.ValidFrom         = ValidFrom;
            this.ValidUntil        = ValidUntil;
            this.Sequence          = Sequence;
            this.Previous          = Previous;
            this.After             = After;
            this.Signer            = Signer;
            this.SignedAt          = SignedAt;
            this.Algorithm         = Algorithm;
            this.KeyId             = KeyId;
            this.Value             = Value;

        }

        #endregion


        #region (static) Create(PrivateKey, KeyId, Algorithm, RoamingNetworkId, EntityType, EntityId, PriceStatus, Sequence, Previous, SignedAt = null, ValidFrom = null, ValidUntil = null, After = null, Deterministic = true)

        /// <summary>
        /// Sign a new price statement of the given entity with a signing key of its charging station operator.
        /// </summary>
        /// <param name="PrivateKey">The private signing key.</param>
        /// <param name="KeyId">The @id of the signing key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="EntityType">ChargingPool, ChargingStation or EVSE.</param>
        /// <param name="EntityId">The identification of the entity.</param>
        /// <param name="PriceStatus">The price status.</param>
        /// <param name="Sequence">The position in the chain of the statements of the entity.</param>
        /// <param name="Previous">The digest of the statement before, absent for sequence 0.</param>
        /// <param name="SignedAt">When it is signed, now otherwise.</param>
        /// <param name="ValidFrom">Since when it is valid, when it is signed otherwise.</param>
        /// <param name="ValidUntil">Until when it is valid, if it says.</param>
        /// <param name="After">The latest checkpoint known, if any.</param>
        /// <param name="Deterministic">Whether to sign deterministically.</param>
        public static RoamingNetworkPriceStatement Create(AsymmetricKeyParameter        PrivateKey,
                                                          String                        KeyId,
                                                          COSEAlgorithm                 Algorithm,
                                                          RoamingNetwork_Id             RoamingNetworkId,
                                                          String                        EntityType,
                                                          String                        EntityId,
                                                          ChargingPrices                PriceStatus,
                                                          UInt64                        Sequence,
                                                          String?                       Previous,
                                                          DateTimeOffset?               SignedAt        = null,
                                                          DateTimeOffset?               ValidFrom       = null,
                                                          DateTimeOffset?               ValidUntil      = null,
                                                          RoamingNetworkCheckpoint_Id?  After           = null,
                                                          Boolean                       Deterministic   = true)
        {

            ArgumentNullException.ThrowIfNull(PrivateKey);
            ArgumentNullException.ThrowIfNull(PriceStatus);

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            if (OperatorOf(EntityType, EntityId) is not ChargingStationOperator_Id operatorId)
                throw new ArgumentException($"Invalid identification of a {EntityType}.", nameof(EntityId));

            var signedAt   = Truncated(SignedAt ?? Timestamp.Now);

            // Without a value yet, only to get the signed fields exactly as they will be written.
            var unsigned   = new RoamingNetworkPriceStatement(RoamingNetworkId,
                                                              EntityType,
                                                              EntityId,
                                                              PriceStatus.ToJSON(Embedded: true),
                                                              ValidFrom.HasValue ? Truncated(ValidFrom.Value) : signedAt,
                                                              ValidUntil.HasValue ? Truncated(ValidUntil.Value) : null,
                                                              Sequence,
                                                              Previous,
                                                              After,
                                                              operatorId,
                                                              signedAt,
                                                              Algorithm.Name,
                                                              KeyId,
                                                              "-");

            var value      = Algorithm.Sign(unsigned.GetSigningBytes(), PrivateKey, Deterministic: Deterministic);

            return new RoamingNetworkPriceStatement(unsigned.RoamingNetworkId,
                                                    unsigned.EntityType,
                                                    unsigned.EntityId,
                                                    unsigned.PriceStatusJSON,
                                                    unsigned.ValidFrom,
                                                    unsigned.ValidUntil,
                                                    unsigned.Sequence,
                                                    unsigned.Previous,
                                                    unsigned.After,
                                                    unsigned.Signer,
                                                    unsigned.SignedAt,
                                                    unsigned.Algorithm,
                                                    unsigned.KeyId,
                                                    Convert.ToBase64String(value));

        }

        #endregion

        #region IsValidAt(Timestamp)

        /// <summary>
        /// Whether it is valid at the given time: not before its validFrom, and not after its validUntil, when it has one.
        /// </summary>
        public Boolean IsValidAt(DateTimeOffset Timestamp)

            => Timestamp >= ValidFrom &&
               (!ValidUntil.HasValue || Timestamp <= ValidUntil.Value);

        #endregion

        #region IsOf(EntityType, EntityId)

        /// <summary>
        /// Whether it is a statement of the given entity.
        /// </summary>
        public Boolean IsOf(String  EntityType,
                            String  EntityId)

            => this.EntityType == EntityType &&
               this.EntityId   == EntityId;

        #endregion

        #region VerifySignature(PublicKey, ExpectedKeyId, out ErrorResponse)

        /// <summary>
        /// Verify the signature of this price statement with the given public key and its expected key identification, e.g.
        /// a signing key of the operator. Unknown profiles, noncanonical Base64, mismatched keys and malformed inputs return false.
        /// </summary>
        /// <param name="PublicKey">The public key.</param>
        /// <param name="ExpectedKeyId">The key identification of the public key.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean VerifySignature(AsymmetricKeyParameter            PublicKey,
                                       String                            ExpectedKeyId,
                                       [NotNullWhen(false)] out String?  ErrorResponse)
        {

            ErrorResponse = null;

            try
            {

                ArgumentNullException.ThrowIfNull(PublicKey);

                if (PublicKey.IsPrivate)
                    throw new ArgumentException("Verification requires a public asymmetric key.", nameof(PublicKey));

                if (KeyId != ExpectedKeyId)
                    throw new ArgumentException("The key ID of the price statement does not match the expected key ID.");

                if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || algorithm.Name != Algorithm)
                    throw new ArgumentException("Unknown or noncanonical signature algorithm.");

                var value = Convert.FromBase64String(Value);

                if (value.Length == 0 || Convert.ToBase64String(value) != Value)
                    throw new ArgumentException("The value must contain nonempty canonical standard Base64.");

                return algorithm.Verify(GetSigningBytes(), value, PublicKey, out ErrorResponse);

            }
            catch (Exception exception)
            {
                ErrorResponse = exception.Message;
                return false;
            }

        }

        #endregion

        #region TryVerify(SigningKeys, out ErrorResponse)

        /// <summary>
        /// Verify the signature of this price statement with the one of the given signing keys of its operator it names,
        /// which must be valid when it was signed.
        /// </summary>
        /// <param name="SigningKeys">The signing keys of the charging station operator of the entity.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        public Boolean TryVerify(IEnumerable<POIPublicKey>         SigningKeys,
                                 [NotNullWhen(false)] out String?  ErrorResponse)
        {

            if (SigningKeys.FirstOrDefault(signingKey => signingKey.Id == KeyId) is not POIPublicKey signingKey)
            {
                ErrorResponse = $"The key '{KeyId}' of the price statement is no signing key of the charging station operator '{Signer}'!";
                return false;
            }

            if (!signingKey.IsValidAt(SignedAt))
            {
                ErrorResponse = $"The signing key '{KeyId}' of the charging station operator '{Signer}' is not valid at {SignedAt.ToISO8601()}, when the price statement was signed!";
                return false;
            }

            if (!VerifySignature(signingKey.Key.ToPublicKey(), signingKey.Id, out var signatureError))
            {
                ErrorResponse = $"The signature of the price statement is invalid: {signatureError}";
                return false;
            }

            ErrorResponse = null;
            return true;

        }

        #endregion

        #region TryVerify(Proof, out ErrorResponse, At = null)

        /// <summary>
        /// Verify this price statement against what the given proof of a charging station gives (SPECIFICATION.md, section 12):
        /// its entity, below its charging station operator, and the signing key of the operator it is signed with, valid when
        /// it was signed; and it must be valid at the given time, now by default. Whether the proof itself is attested, its
        /// Verify(…) tells, or TryVerify(Proof, TrustedIdentityKeyIds, …) checks both. Its prices apply to the charging
        /// tariffs the proof gives, those of the EVSEs of the station.
        /// </summary>
        /// <param name="Proof">The proof of the charging station.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When it must be valid, now by default.</param>
        public Boolean TryVerify(RoamingNetworkProof               Proof,
                                 [NotNullWhen(false)] out String?  ErrorResponse,
                                 DateTimeOffset?                   At   = null)
        {

            var at = At ?? Timestamp.Now;

            if (RoamingNetworkId.ToString() != Proof.RoamingNetwork[POI.RoamingNetwork.BasePropertyKeys.Id]?.Value<String>())
            {
                ErrorResponse = $"The price statement is one of the roaming network '{RoamingNetworkId}'!";
                return false;
            }

            if (!IsValidAt(at))
            {
                ErrorResponse = $"The price statement is not valid at {at.ToISO8601()}: it is valid from {ValidFrom.ToISO8601()}{(ValidUntil.HasValue ? $" until {ValidUntil.Value.ToISO8601()}" : "")}!";
                return false;
            }

            if (Proof.Given(POI.RoamingNetwork.PropertyKeys.ChargingStationOperators).FirstOrDefault(chargingStationOperator => Id(chargingStationOperator) == Signer.ToString()) is not JObject operatorJSON)
            {
                ErrorResponse = $"The proof gives no charging station operator '{Signer}'!";
                return false;
            }

            var given = EntityType switch {
                            nameof(ChargingPool)     => Proof.GivenBelow(operatorJSON, ChargingStationOperator.PropertyKeys.ChargingPools),
                            nameof(ChargingStation)  => Proof.GivenBelow(operatorJSON, ChargingStationOperator.PropertyKeys.ChargingPools, ChargingPool.PropertyKeys.ChargingStations),
                            _                        => Proof.GivenBelow(operatorJSON, ChargingStationOperator.PropertyKeys.ChargingPools, ChargingPool.PropertyKeys.ChargingStations, ChargingStation.PropertyKeys.EVSEs)
                        };

            if (!given.Any(entity => Id(entity) == EntityId))
            {
                ErrorResponse = $"The proof gives no {EntityType} '{EntityId}' of the charging station operator '{Signer}'!";
                return false;
            }

            if (!POIPartyKeys.TryParse(operatorJSON, out var signingKeys, out _, out var keysError))
            {
                ErrorResponse = $"The keys of the charging station operator '{Signer}' in the proof are invalid: {keysError}";
                return false;
            }

            return TryVerify(signingKeys, out ErrorResponse);

        }

        #endregion

        #region TryVerify(Proof, TrustedIdentityKeyIds, out Attestation, out ErrorResponse, At = null, MinConsensus = 1)

        /// <summary>
        /// Verify this price statement as a driver's app does, offline: the given proof of the charging station against the
        /// identity keys of the roaming network operator it trusts by their @ids, e.g. those of a QR code it scanned
        /// (RoamingNetworkProof.Verify), and this statement against what the proof gives, both valid at the given time,
        /// now by default.
        /// </summary>
        /// <param name="Proof">The proof of the charging station.</param>
        /// <param name="TrustedIdentityKeyIds">The @ids of the identity keys of the roaming network operator the verifier trusts.</param>
        /// <param name="Attestation">The attestation the proof was verified against.</param>
        /// <param name="ErrorResponse">An optional error response.</param>
        /// <param name="At">When they must be valid, now by default.</param>
        /// <param name="MinConsensus">How many holders of the keys of the roaming network operator must have signed its key statement and attested the root, e.g. as a trust anchor names it.</param>
        public Boolean TryVerify(RoamingNetworkProof                                   Proof,
                                 IEnumerable<String>                                   TrustedIdentityKeyIds,
                                 [NotNullWhen(true)]  out RoamingNetworkAttestation?   Attestation,
                                 [NotNullWhen(false)] out String?                      ErrorResponse,
                                 DateTimeOffset?                                       At             = null,
                                 Byte                                                  MinConsensus   = 1)
        {

            var at = At ?? Timestamp.Now;

            if (!Proof.Verify(TrustedIdentityKeyIds, out Attestation, out ErrorResponse, at, MinConsensus))
                return false;

            if (!TryVerify(Proof, out ErrorResponse, at))
            {
                Attestation = null;
                return false;
            }

            return true;

        }

        #endregion

        #region Follows(Previous)

        /// <summary>
        /// Whether this statement directly follows the given one of the same entity: one more in sequence, naming its digest.
        /// </summary>
        /// <param name="Previous">The statement before.</param>
        public Boolean Follows(RoamingNetworkPriceStatement Previous)

            => IsOf(Previous.EntityType, Previous.EntityId) &&
               Sequence == Previous.Sequence + 1             &&
               this.Previous == Previous.Digest;

        #endregion

        #region GetSigningBytes()

        /// <summary>
        /// The signature input of this price statement, as Styx canonical JSON: its JSON-LD context and its signed fields,
        /// without its value.
        /// </summary>
        public Byte[] GetSigningBytes()
        {

            if (!COSEAlgorithm.TryParse(Algorithm, out var algorithm) || !algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.");

            var json = Content();

            json.AddFirst(new JProperty(PropertyKeys.KeyId,      KeyId));
            json.AddFirst(new JProperty(PropertyKeys.Algorithm,  Algorithm));
            json.AddFirst(new JProperty(PropertyKeys.Context,    JSONLDContext));

            return CanonicalJSON.ToUTF8Bytes(json);

        }

        #endregion


        #region (static) IsEntityType(EntityType)

        /// <summary>
        /// Whether the given entity type has a price status: ChargingPool, ChargingStation or EVSE.
        /// </summary>
        public static Boolean IsEntityType(String? EntityType)

            => EntityType is nameof(ChargingPool) or nameof(ChargingStation) or nameof(EVSE);

        #endregion

        #region (static) OperatorOf(EntityType, EntityId)

        /// <summary>
        /// The charging station operator of the given charging pool, charging station or EVSE, by its identification,
        /// or null when it is none.
        /// </summary>
        public static ChargingStationOperator_Id? OperatorOf(String  EntityType,
                                                             String  EntityId)

            => EntityType switch {
                   nameof(ChargingPool)     => ChargingPool_Id.   TryParse(EntityId, out var chargingPoolId)    ? chargingPoolId.   OperatorId : null,
                   nameof(ChargingStation)  => ChargingStation_Id.TryParse(EntityId, out var chargingStationId) ? chargingStationId.OperatorId : null,
                   nameof(EVSE)             => EVSE_Id.           TryParse(EntityId, out var evseId)            ? evseId.           OperatorId : null,
                   _                        => null
               };

        #endregion

        #region (static) InOrder(PriceStatements)

        /// <summary>
        /// The given price statements in the order of the leaves of a checkpoint: by their entity types and identifications
        /// in ordinal order, then by their sequences; each one once.
        /// </summary>
        /// <param name="PriceStatements">Price statements.</param>
        public static IReadOnlyList<RoamingNetworkPriceStatement> InOrder(IEnumerable<RoamingNetworkPriceStatement> PriceStatements)

            => [ .. PriceStatements.DistinctBy(priceStatement => priceStatement.Digest).
                                    OrderBy   (priceStatement => priceStatement.EntityType, StringComparer.Ordinal).
                                    ThenBy    (priceStatement => priceStatement.EntityId,   StringComparer.Ordinal).
                                    ThenBy    (priceStatement => priceStatement.Sequence).
                                    ThenBy    (priceStatement => priceStatement.Digest,     StringComparer.Ordinal) ];

        #endregion

        #region (static) RootOf(OrderedPriceStatements, IdentificationHash = SHA256)

        /// <summary>
        /// The Merkle tree hash of the given price statements, already in the order of a checkpoint, with the identification
        /// hash of its epoch: each leaf the 32 bytes of the digest of a statement, in lowercase hexadecimal digits.
        /// </summary>
        public static String RootOf(IReadOnlyList<RoamingNetworkPriceStatement>  OrderedPriceStatements,
                                    ETagHashAlgorithm                            IdentificationHash   = ETagHashAlgorithm.SHA256)

            => Convert.ToHexStringLower(POIMerkleTree.RootOfLeafHashes(IdentificationHash,
                                                                       [ .. OrderedPriceStatements.Select(priceStatement => POIMerkleTree.LeafHash(IdentificationHash, priceStatement.Leaf)) ]));

        #endregion

        #region Leaf

        /// <summary>
        /// The leaf of this statement in the tree of a checkpoint: the 32 bytes of its digest.
        /// </summary>
        public Byte[] Leaf
            => Convert.FromHexString(Digest);

        #endregion


        #region (static) TryParse(Text, out PriceStatement, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON text of a price statement, numbers with a fraction as decimals. Dates stay text, so
        /// signed values keep their exact representation.
        /// </summary>
        public static Boolean TryParse(String                                                  Text,
                                       [NotNullWhen(true)]  out RoamingNetworkPriceStatement?  PriceStatement,
                                       [NotNullWhen(false)] out String?                        ErrorResponse)
        {

            PriceStatement = null;

            try
            {

                if (POIJSON.Read(Text) is not JObject json)
                {
                    ErrorResponse = "The given JSON text must be a JSON object!";
                    return false;
                }

                return TryParse(json, out PriceStatement, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(JSON, out PriceStatement, out ErrorResponse)

        /// <summary>
        /// Try to parse the given JSON representation of a price statement. Its signature is not verified here.
        /// </summary>
        public static Boolean TryParse(JObject                                                 JSON,
                                       [NotNullWhen(true)]  out RoamingNetworkPriceStatement?  PriceStatement,
                                       [NotNullWhen(false)] out String?                        ErrorResponse)
        {

            PriceStatement = null;

            try
            {

                foreach (var property in JSON.Properties())
                    if (property.Name is not (PropertyKeys.Context or
                                              PropertyKeys.RoamingNetworkId or PropertyKeys.EntityType or PropertyKeys.EntityId or PropertyKeys.PriceStatus or
                                              PropertyKeys.ValidFrom or PropertyKeys.ValidUntil or PropertyKeys.Sequence or PropertyKeys.Previous or
                                              PropertyKeys.After or PropertyKeys.Role or PropertyKeys.Signer or PropertyKeys.SignedAt or
                                              PropertyKeys.Algorithm or PropertyKeys.KeyId or PropertyKeys.Value))
                    {
                        ErrorResponse = $"Unknown property '{property.Name}' of a price statement!";
                        return false;
                    }

                if (JSON[PropertyKeys.Context] is JToken context &&
                    !(context.Type == JTokenType.String && context.Value<String>() == JSONLDContext))
                {
                    ErrorResponse = $"The JSON-LD context of the price statement must be '{JSONLDContext}'!";
                    return false;
                }

                if (!JSON.ParseMandatory(PropertyKeys.RoamingNetworkId,
                                         "roaming network identification",
                                         RoamingNetwork_Id.TryParse,
                                         out RoamingNetwork_Id roamingNetworkId,
                                         out ErrorResponse))
                {
                    return false;
                }

                if (Text(JSON, PropertyKeys.EntityType) is not String entityType || !IsEntityType(entityType))
                {
                    ErrorResponse = "The entity type of a price statement must be ChargingPool, ChargingStation or EVSE!";
                    return false;
                }

                if (Text(JSON, PropertyKeys.EntityId) is not String entityId || OperatorOf(entityType, entityId) is not ChargingStationOperator_Id operatorId)
                {
                    ErrorResponse = $"Missing or invalid identification of the {entityType} of the price statement!";
                    return false;
                }

                if (JSON[PropertyKeys.PriceStatus] is not JObject priceStatusJSON)
                {
                    ErrorResponse = "A price statement needs its price status!";
                    return false;
                }

                if (!ChargingPrices.TryParse(priceStatusJSON, out _, out var priceStatusError))
                {
                    ErrorResponse = "Invalid price status of the price statement: " + priceStatusError;
                    return false;
                }

                if (!TryParseTimestamp(JSON, PropertyKeys.ValidFrom, out var validFrom) || !validFrom.HasValue)
                {
                    ErrorResponse = "The beginning of the validity must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                if (!TryParseTimestamp(JSON, PropertyKeys.ValidUntil, out var validUntil))
                {
                    ErrorResponse = "The end of the validity must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                if (JSON[PropertyKeys.Sequence] is not JValue { Type: JTokenType.Integer } sequenceJSON ||
                    sequenceJSON.Value<Decimal>() < 0 || sequenceJSON.Value<Decimal>() > UInt64.MaxValue)
                {
                    ErrorResponse = "Missing or invalid sequence!";
                    return false;
                }

                var sequence = sequenceJSON.Value<UInt64>();

                String? previous = null;

                if (JSON[PropertyKeys.Previous] is JToken previousJSON)
                {

                    if (previousJSON.Type != JTokenType.String || !IsDigest(previousJSON.Value<String>()))
                    {
                        ErrorResponse = "The digest of the statement before must be 64 lowercase hexadecimal digits!";
                        return false;
                    }

                    previous = previousJSON.Value<String>();

                }

                if ((sequence == 0) != (previous is null))
                {
                    ErrorResponse = "A price statement names the digest of the one before it, unless it is the first one of its entity!";
                    return false;
                }

                RoamingNetworkCheckpoint_Id? after = null;

                if (JSON[PropertyKeys.After] is JToken afterJSON)
                {

                    if (afterJSON.Type != JTokenType.String || !RoamingNetworkCheckpoint_Id.TryParse(afterJSON.Value<String>(), out var afterId))
                    {
                        ErrorResponse = "The checkpoint a price statement was made after must be a checkpoint identification!";
                        return false;
                    }

                    after = afterId;

                }

                if (Text(JSON, PropertyKeys.Role) != Role)
                {
                    ErrorResponse = $"A price statement is signed in the role '{Role}'!";
                    return false;
                }

                if (Text(JSON, PropertyKeys.Signer) != operatorId.ToString())
                {
                    ErrorResponse = $"A price statement of the {entityType} '{entityId}' is signed by its operator '{operatorId}'!";
                    return false;
                }

                if (!TryParseTimestamp(JSON, PropertyKeys.SignedAt, out var signedAt) || !signedAt.HasValue)
                {
                    ErrorResponse = "The signing timestamp must be written as yyyy-MM-ddTHH:mm:ss.fffZ!";
                    return false;
                }

                if (Text(JSON, PropertyKeys.Algorithm) is not String algorithm || algorithm.Length == 0 ||
                    Text(JSON, PropertyKeys.KeyId)     is not String keyId     || keyId.    Length == 0 ||
                    Text(JSON, PropertyKeys.Value)     is not String value     || value.    Length == 0)
                {
                    ErrorResponse = "A price statement requires an algorithm, a key identification and a value!";
                    return false;
                }

                if (validFrom.Value < signedAt.Value)
                {
                    ErrorResponse = "A price statement is not valid before it was signed!";
                    return false;
                }

                if (validUntil.HasValue && validUntil.Value <= validFrom.Value)
                {
                    ErrorResponse = "A price statement must be valid until after it is valid from!";
                    return false;
                }

                PriceStatement  = new RoamingNetworkPriceStatement(roamingNetworkId,
                                                                   entityType,
                                                                   entityId,
                                                                   priceStatusJSON,
                                                                   validFrom.Value,
                                                                   validUntil,
                                                                   sequence,
                                                                   previous,
                                                                   after,
                                                                   operatorId,
                                                                   signedAt.Value,
                                                                   algorithm,
                                                                   keyId,
                                                                   value);

                ErrorResponse   = null;
                return true;

            }
            catch (Exception e)
            {
                PriceStatement  = null;
                ErrorResponse   = e.Message;
                return false;
            }

        }

        #endregion

        #region (static) TryParse(CBOR, out PriceStatement, out ErrorResponse)

        /// <summary>
        /// Try to parse the given CBOR representation of a price statement: the keys and values of its JSON representation.
        /// </summary>
        public static Boolean TryParse(CBORValue                                               CBOR,
                                       [NotNullWhen(true)]  out RoamingNetworkPriceStatement?  PriceStatement,
                                       [NotNullWhen(false)] out String?                        ErrorResponse)
        {

            PriceStatement = null;

            try
            {

                if (CBOR.Kind != CBORValueKind.Map || CBORJSON.ToJSON(CBOR) is not JObject json)
                {
                    ErrorResponse = "The CBOR representation of a price statement must be a map with text keys!";
                    return false;
                }

                return TryParse(json, out PriceStatement, out ErrorResponse);

            }
            catch (Exception e)
            {
                ErrorResponse = e.Message;
                return false;
            }

        }

        #endregion

        #region ToJSON(Embedded = false, CustomPriceStatementSerializer = null)

        /// <summary>
        /// Return the JSON representation of this price statement.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomPriceStatementSerializer">An optional custom serializer of price statements.</param>
        public JObject ToJSON(Boolean                                                         Embedded                         = false,
                              CustomJObjectSerializerDelegate<RoamingNetworkPriceStatement>?  CustomPriceStatementSerializer   = null)
        {

            var json = Content();

            if (!Embedded)
                json.AddFirst(new JProperty(PropertyKeys.Context, JSONLDContext));

            json.Add(PropertyKeys.Algorithm,  Algorithm);
            json.Add(PropertyKeys.KeyId,      KeyId);
            json.Add(PropertyKeys.Value,      Value);

            return CustomPriceStatementSerializer is not null
                       ? CustomPriceStatementSerializer(this, json)
                       : json;

        }

        #endregion

        #region ToCBOR(Embedded = false, CustomPriceStatementSerializer = null)

        /// <summary>
        /// Return the CBOR representation of this price statement: the keys and values of its JSON representation.
        /// </summary>
        /// <param name="Embedded">Whether this data structure is embedded into another data structure.</param>
        /// <param name="CustomPriceStatementSerializer">An optional custom serializer of price statements.</param>
        public CBORValue ToCBOR(Boolean                                                      Embedded                         = false,
                                CustomCBORSerializerDelegate<RoamingNetworkPriceStatement>?  CustomPriceStatementSerializer   = null)
        {

            var cbor = new CBORMap();

            if (!Embedded)
                cbor.Add(CBORValue.FromText(PropertyKeys.Context),           CBORValue.FromText(JSONLDContext));

            cbor.Add(CBORValue.FromText(PropertyKeys.RoamingNetworkId),      CBORValue.FromText(RoamingNetworkId.ToString()));
            cbor.Add(CBORValue.FromText(PropertyKeys.EntityType),            CBORValue.FromText(EntityType));
            cbor.Add(CBORValue.FromText(PropertyKeys.EntityId),              CBORValue.FromText(EntityId));

            // The price status as it was given and signed: the keys and values of its JSON representation.
            cbor.Add(CBORValue.FromText(PropertyKeys.PriceStatus),           CBORJSON.ToCBOR(PriceStatusJSON));
            cbor.Add(CBORValue.FromText(PropertyKeys.ValidFrom),             CBORValue.FromText(ValidFrom.ToISO8601()));

            if (ValidUntil.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.ValidUntil),        CBORValue.FromText(ValidUntil.Value.ToISO8601()));

            cbor.Add(CBORValue.FromText(PropertyKeys.Sequence),              CBORValue.FromUInt64(Sequence));

            if (Previous is not null)
                cbor.Add(CBORValue.FromText(PropertyKeys.Previous),          CBORValue.FromText(Previous));

            if (After.HasValue)
                cbor.Add(CBORValue.FromText(PropertyKeys.After),             CBORValue.FromText(After.Value.ToString()));

            cbor.Add(CBORValue.FromText(PropertyKeys.Role),                  CBORValue.FromText(Role));
            cbor.Add(CBORValue.FromText(PropertyKeys.Signer),                CBORValue.FromText(Signer.ToString()));
            cbor.Add(CBORValue.FromText(PropertyKeys.SignedAt),              CBORValue.FromText(SignedAt.ToISO8601()));
            cbor.Add(CBORValue.FromText(PropertyKeys.Algorithm),             CBORValue.FromText(Algorithm));
            cbor.Add(CBORValue.FromText(PropertyKeys.KeyId),                 CBORValue.FromText(KeyId));
            cbor.Add(CBORValue.FromText(PropertyKeys.Value),                 CBORValue.FromText(Value));

            return CustomPriceStatementSerializer is not null
                       ? CustomPriceStatementSerializer(this, cbor)
                       : cbor;

        }

        #endregion


        #region (private) Content()

        /// <summary>
        /// What this price statement states and who states it, without its signature.
        /// </summary>
        private JObject Content()
        {

            var json = new JObject(
                           new JProperty(PropertyKeys.RoamingNetworkId,  RoamingNetworkId.ToString()),
                           new JProperty(PropertyKeys.EntityType,        EntityType),
                           new JProperty(PropertyKeys.EntityId,          EntityId),
                           new JProperty(PropertyKeys.PriceStatus,       PriceStatusJSON.DeepClone()),
                           new JProperty(PropertyKeys.ValidFrom,         ValidFrom.ToISO8601())
                       );

            if (ValidUntil.HasValue)
                json.Add(PropertyKeys.ValidUntil,  ValidUntil.Value.ToISO8601());

            json.Add(PropertyKeys.Sequence,        Sequence);

            if (Previous is not null)
                json.Add(PropertyKeys.Previous,    Previous);

            if (After.HasValue)
                json.Add(PropertyKeys.After,       After.Value.ToString());

            json.Add(PropertyKeys.Role,            Role);
            json.Add(PropertyKeys.Signer,          Signer.ToString());
            json.Add(PropertyKeys.SignedAt,        SignedAt.ToISO8601());

            return json;

        }

        #endregion

        #region (private static) IsDigest(Text)

        private static Boolean IsDigest(String? Text)

            => Text is not null &&
               Text.Length == 64 &&
               Text.AsSpan().IndexOfAnyExcept("0123456789abcdef") < 0;

        #endregion

        #region (private static) Id(Entity)

        private static String? Id(JObject Entity)

            => Entity[POI.RoamingNetwork.BasePropertyKeys.Id]?.Type == JTokenType.String
                   ? Entity[POI.RoamingNetwork.BasePropertyKeys.Id]!.Value<String>()
                   : null;

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
        /// An optional timestamp, written exactly as yyyy-MM-ddTHH:mm:ss.fffZ.
        /// </summary>
        private static Boolean TryParseTimestamp(JObject              JSON,
                                                 String               PropertyName,
                                                 out DateTimeOffset?  Timestamp)
        {

            Timestamp = null;

            if (JSON[PropertyName] is null)
                return true;

            if (Text(JSON, PropertyName) is not String text ||
                !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var timestamp) ||
                timestamp.ToISO8601() != text)
            {
                return false;
            }

            Timestamp = timestamp;
            return true;

        }

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"Price statement {Sequence} of the {EntityType} '{EntityId}' by '{Signer}', valid from {ValidFrom.ToISO8601()}{(ValidUntil.HasValue ? $" until {ValidUntil.Value.ToISO8601()}" : "")}";

        #endregion

    }

}
