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


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Role}{(Signer is not null ? $" {Signer}" : "")} with '{KeyId}' ({Algorithm.Name})";

        #endregion

    }

}
