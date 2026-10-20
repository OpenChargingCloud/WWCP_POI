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

using Org.BouncyCastle.Crypto;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A key that signs every commit of a history, e.g. the one of the leading server.
    /// </summary>
    public sealed class RoamingNetworkCommitSigner
    {

        #region Data

        private readonly AsymmetricKeyParameter privateKey;

        #endregion

        #region Properties

        /// <summary>
        /// The application identifier of the key.
        /// </summary>
        public String         KeyId        { get; }

        /// <summary>
        /// The signature algorithm.
        /// </summary>
        public COSEAlgorithm  Algorithm    { get; }

        /// <summary>
        /// The role of the signer, e.g. leader.
        /// </summary>
        public String         Role         { get; }

        /// <summary>
        /// The optional name of the signer.
        /// </summary>
        public String?        Signer       { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new signer of commits.
        /// </summary>
        /// <param name="PrivateKey">The private key.</param>
        /// <param name="KeyId">The application identifier of the key.</param>
        /// <param name="Algorithm">The signature algorithm.</param>
        /// <param name="Role">The role of the signer, the leader by default.</param>
        /// <param name="Signer">The optional name of the signer.</param>
        public RoamingNetworkCommitSigner(AsymmetricKeyParameter  PrivateKey,
                                          String                  KeyId,
                                          COSEAlgorithm           Algorithm,
                                          String                  Role     = RoamingNetworkCommitSignatureRoles.Leader,
                                          String?                 Signer   = null)
        {

            ArgumentNullException.ThrowIfNull(PrivateKey);

            if (!PrivateKey.IsPrivate)
                throw new ArgumentException("Signing requires a private asymmetric key.", nameof(PrivateKey));

            if (!Algorithm.IsSupportedForSigning)
                throw new ArgumentException("The algorithm is not supported for asymmetric signing.", nameof(Algorithm));

            if (String.IsNullOrWhiteSpace(KeyId))
                throw new ArgumentException("A key identification is required.", nameof(KeyId));

            if (String.IsNullOrWhiteSpace(Role))
                throw new ArgumentException("A role is required.", nameof(Role));

            this.privateKey  = PrivateKey;
            this.KeyId       = KeyId;
            this.Algorithm   = Algorithm;
            this.Role        = Role;
            this.Signer      = Signer;

        }

        #endregion


        #region Sign(Commit, SignedAt = null)

        /// <summary>
        /// Create a signature of the given commit.
        /// </summary>
        /// <param name="Commit">A commit.</param>
        /// <param name="SignedAt">When the commit is signed, now otherwise.</param>
        public RoamingNetworkCommitSignature Sign(RoamingNetworkCommit  Commit,
                                                  DateTimeOffset?       SignedAt   = null)

            => Commit.CreateSignature(privateKey,
                                      KeyId,
                                      Algorithm,
                                      Role,
                                      Signer,
                                      SignedAt);

        #endregion

        #region Sign(Checkpoint, SignedAt = null)

        /// <summary>
        /// Create a signature of the given checkpoint.
        /// </summary>
        /// <param name="Checkpoint">A checkpoint.</param>
        /// <param name="SignedAt">When the checkpoint is signed, now otherwise.</param>
        public RoamingNetworkCommitSignature Sign(RoamingNetworkCheckpoint  Checkpoint,
                                                  DateTimeOffset?           SignedAt   = null)

            => Checkpoint.CreateSignature(privateKey,
                                          KeyId,
                                          Algorithm,
                                          Role,
                                          Signer,
                                          SignedAt);

        #endregion

        #region Attest(RoamingNetworkId, CommitId, Revision, ETag, SignedAt = null, ValidUntil = null, Subject = null)

        /// <summary>
        /// Attest the version of the given commit with its given effectiveTree ETag (SPECIFICATION.md, section 8.5).
        /// </summary>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="CommitId">The commit of the attested version.</param>
        /// <param name="Revision">The revision of the commit.</param>
        /// <param name="ETag">The effectiveTree ETag of the attested version.</param>
        /// <param name="SignedAt">When it is signed, now otherwise.</param>
        /// <param name="ValidUntil">Until when it is valid, if it says.</param>
        /// <param name="Subject">The digest of the one entity it attests instead of the root, if any.</param>
        public RoamingNetworkAttestation Attest(RoamingNetwork_Id                  RoamingNetworkId,
                                                RoamingNetworkCommit_Id            CommitId,
                                                UInt64                             Revision,
                                                ETag                               ETag,
                                                DateTimeOffset?                    SignedAt     = null,
                                                DateTimeOffset?                    ValidUntil   = null,
                                                RoamingNetworkAttestationSubject?  Subject      = null)

            => RoamingNetworkAttestation.Create(privateKey,
                                                KeyId,
                                                Algorithm,
                                                Role,
                                                RoamingNetworkId,
                                                CommitId,
                                                Revision,
                                                ETag,
                                                Signer,
                                                SignedAt,
                                                ValidUntil,
                                                Subject);

        #endregion

        #region StatePrices(RoamingNetworkId, EntityType, EntityId, PriceStatus, Sequence, Previous, SignedAt = null, ValidFrom = null, ValidUntil = null, After = null)

        /// <summary>
        /// Sign a price statement of the given charging pool, charging station or EVSE (SPECIFICATION.md, section 12):
        /// a signing key of its charging station operator.
        /// </summary>
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
        public RoamingNetworkPriceStatement StatePrices(RoamingNetwork_Id             RoamingNetworkId,
                                                        String                        EntityType,
                                                        String                        EntityId,
                                                        ChargingPrices                PriceStatus,
                                                        UInt64                        Sequence,
                                                        String?                       Previous,
                                                        DateTimeOffset?               SignedAt     = null,
                                                        DateTimeOffset?               ValidFrom    = null,
                                                        DateTimeOffset?               ValidUntil   = null,
                                                        RoamingNetworkCheckpoint_Id?  After        = null)

            => RoamingNetworkPriceStatement.Create(privateKey,
                                                   KeyId,
                                                   Algorithm,
                                                   RoamingNetworkId,
                                                   EntityType,
                                                   EntityId,
                                                   PriceStatus,
                                                   Sequence,
                                                   Previous,
                                                   SignedAt,
                                                   ValidFrom,
                                                   ValidUntil,
                                                   After);

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Role}{(Signer is not null ? $" {Signer}" : "")} with '{KeyId}' ({Algorithm.Name})";

        #endregion

    }

}
