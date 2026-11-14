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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// The trust anchor of a roaming network changed, as a version it checked states the keys of its operator anew.
    /// </summary>
    /// <param name="Trust">Who trusts it.</param>
    /// <param name="TrustAnchor">The trust anchor from now on.</param>
    public delegate void OnRoamingNetworkFollowerTrustFollowedDelegate(RoamingNetworkFollowerTrust  Trust,
                                                                       RoamingNetworkTrustAnchor    TrustAnchor);


    /// <summary>
    /// Whom a server following a roaming network of another server trusts to sign as its operator: the signing keys
    /// the trust anchor of the roaming network leads to (SPECIFICATION.md, section 8.6). A signature in the role of the
    /// roaming network operator is taken when its key is certified in the version it signs by an identity key of its
    /// holder, valid when it signed and one that may sign for the party, and the key statement of that version is
    /// signed by identity keys of as many holders as the trust anchor names and requires. No signature in another role
    /// is taken: who leads or witnesses is no business of the operator's keys.
    /// </summary>
    /// <remarks>
    /// The trust anchor is followed, never replaced from outside: Follow(Version) takes the keys a version states once
    /// its key statement is verified against the ones trusted so far, as a verifier follows the key statements one
    /// after the other, only through versions it checked. A key statement is taken as the trust anchor takes it, or
    /// as the one following the one taken last: signed by identity keys that one names, of as many of its holders as
    /// it requires, and by its own quorum - so a holder joins, and one leaves. A version whose key statement the
    /// trusted keys did not sign, e.g. after an identity key was replaced at once, leads nowhere: its signatures are
    /// not taken, and whoever follows needs the new trust anchor.
    /// </remarks>
    public sealed class RoamingNetworkFollowerTrust
    {

        #region Data

        private readonly Lock                trustLock = new ();
        private RoamingNetworkTrustAnchor    trustAnchor;

        // The key statement followed last: the next one is signed by the quorum it names.
        private RoamingNetworkKeyStatement?  keyStatement;

        #endregion

        #region Properties

        /// <summary>
        /// The trust anchor as far as it was followed.
        /// </summary>
        public RoamingNetworkTrustAnchor TrustAnchor
        {
            get
            {
                lock (trustLock)
                    return trustAnchor;
            }
        }

        #endregion

        #region Events

        /// <summary>
        /// The trust anchor changed, as a version checked states the keys of its operator anew.
        /// </summary>
        public event OnRoamingNetworkFollowerTrustFollowedDelegate? OnFollowed;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Trust the signing keys the given trust anchor leads to.
        /// </summary>
        /// <param name="TrustAnchor">The trust anchor of the roaming network, e.g. as scanned from the QR code of its operator.</param>
        public RoamingNetworkFollowerTrust(RoamingNetworkTrustAnchor TrustAnchor)
        {

            ArgumentNullException.ThrowIfNull(TrustAnchor);

            this.trustAnchor = TrustAnchor;

        }

        #endregion


        #region SigningKeyOf(Version, Role, KeyId, SignedAt)

        /// <summary>
        /// The signing key of the roaming network operator of the given identification, as the given version certifies
        /// it and the trust anchor lets it be trusted; null otherwise.
        /// </summary>
        /// <param name="Version">The version of the roaming network the signature is checked in.</param>
        /// <param name="Role">The role it signed in.</param>
        /// <param name="KeyId">The identification of the key.</param>
        /// <param name="SignedAt">When it signed.</param>
        public POIPublicKey? SigningKeyOf(RoamingNetwork  Version,
                                          String          Role,
                                          String          KeyId,
                                          DateTimeOffset  SignedAt)
        {

            if (Role != RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator    ||
                Version.Operator is not RoamingNetworkOperator networkOperator       ||
                networkOperator.KeyStatement is not RoamingNetworkKeyStatement statement)
            {
                return null;
            }

            RoamingNetworkTrustAnchor    anchor;
            RoamingNetworkKeyStatement?  last;

            lock (trustLock)
            {
                anchor  = trustAnchor;
                last    = keyStatement;
            }

            if (!TryTrust(Version, anchor, last, out var holderOf))
                return null;

            var trustedIds    = holderOf.Keys.ToHashSet();
            var identityPool  = statement.IdentityKeys.Concat(networkOperator.IdentityKeys).ToArray();

            return networkOperator.SigningKeyCertificates.
                       FirstOrDefault(certificate => certificate.SigningKey.Id == KeyId               &&
                                                     certificate.SigningKey.IsValidAt(SignedAt)       &&
                                                     certificate.SigningKey.MaySign(POIKeyUsages.Party) &&
                                                     certificate.TryVerify(identityPool, out _, out _, trustedIds))?.
                       SigningKey;

        }

        #endregion

        #region (private static) TryTrust(Version, TrustAnchor, Last, out HolderOf)

        /// <summary>
        /// Whether the key statement of the given version is one to trust: as the given trust anchor takes it, or as the
        /// one following the given one taken last (RoamingNetworkKeyStatement.TryVerifyChain).
        /// </summary>
        private static Boolean TryTrust(RoamingNetwork                                                Version,
                                        RoamingNetworkTrustAnchor                                     TrustAnchor,
                                        RoamingNetworkKeyStatement?                                   Last,
                                        [NotNullWhen(true)] out IReadOnlyDictionary<String, String>?  HolderOf)
        {

            HolderOf = null;

            if (Version.Id != TrustAnchor.Id                              ||
                Version.Operator is not RoamingNetworkOperator networkOperator ||
                networkOperator.KeyStatement is not RoamingNetworkKeyStatement statement)
            {
                return false;
            }

            if (statement.TryVerify(TrustAnchor.Holders, networkOperator.IdentityKeys, out HolderOf, out _, TrustAnchor.MinConsensus))
                return true;

            return Last is not null &&
                   RoamingNetworkKeyStatement.TryVerifyChain([ Last, statement ],
                                                             TrustAnchor.Holders,
                                                             out _,
                                                             out HolderOf,
                                                             out _,
                                                             TrustAnchor.MinConsensus,
                                                             OtherIdentityKeys: networkOperator.IdentityKeys);

        }

        #endregion

        #region Follow(Version)

        /// <summary>
        /// Take the keys the given version states of its operator as the ones trusted from now on, once its key
        /// statement is verified against the ones trusted so far. Called for every version checked, in their order.
        /// </summary>
        /// <param name="Version">A version of the roaming network that was checked.</param>
        /// <returns>Whether the trust anchor changed.</returns>
        public Boolean Follow(RoamingNetwork Version)
        {

            RoamingNetworkTrustAnchor followed;

            lock (trustLock)
            {

                if (Version.Operator?.KeyStatement is not RoamingNetworkKeyStatement statement ||
                    !TryTrust(Version, trustAnchor, keyStatement, out _))
                {
                    return false;
                }

                // The one the next one follows, also where the trust anchor stays as it is.
                keyStatement  = statement;

                followed      = Version.CreateTrustAnchor(statement, Version.URLs.IsDefaultOrEmpty ? trustAnchor.URLs : Version.URLs);

                if (followed.ToQRCode() == trustAnchor.ToQRCode())
                    return false;

                trustAnchor   = followed;

            }

            OnFollowed?.Invoke(this, followed);

            return true;

        }

        #endregion


        #region VerifyCommitSignature

        /// <summary>
        /// Verifies a signature of a commit in the version it leads to.
        /// </summary>
        public VerifyCommitSignatureDelegate VerifyCommitSignature

            => (commit, version, signature) => SigningKeyOf(version, signature.Role, signature.KeyId, signature.SignedAt) is POIPublicKey key &&
                                               commit.VerifySignature(signature, key.Key, key.Id, out _);

        #endregion

        #region VerifyCheckpointSignature(Version)

        /// <summary>
        /// Verifies a signature of a checkpoint in the version the given function gives, e.g. the head of the history
        /// following: a checkpoint names no version to check it in. Nothing is verified while there is none.
        /// </summary>
        /// <param name="Version">The version to check signatures in, null while there is none.</param>
        public VerifyCheckpointSignatureDelegate VerifyCheckpointSignature(Func<RoamingNetwork?> Version)

            => (checkpoint, signature) => Version() is RoamingNetwork version &&
                                          SigningKeyOf(version, signature.Role, signature.KeyId, signature.SignedAt) is POIPublicKey key &&
                                          checkpoint.VerifySignature(signature, key.Key, key.Id, out _);

        #endregion

        #region VerifyAttestation(Version)

        /// <summary>
        /// Verifies an attestation in the version the given function gives, e.g. the head of the history following.
        /// Nothing is verified while there is none.
        /// </summary>
        /// <param name="Version">The version to check signatures in, null while there is none.</param>
        public VerifyAttestationDelegate VerifyAttestation(Func<RoamingNetwork?> Version)

            => attestation => Version() is RoamingNetwork version &&
                              SigningKeyOf(version, attestation.Role, attestation.KeyId, attestation.SignedAt) is POIPublicKey key &&
                              attestation.VerifySignature(key.Key.ToPublicKey(), key.Id, out _);

        #endregion

    }

}
