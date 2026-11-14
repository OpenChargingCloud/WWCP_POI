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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// A shareholder of a roaming network operator: an organization that holds, or is about to hold, one voice among
    /// those deciding as the roaming network operator, and so in every roaming network it operates
    /// (SPECIFICATION.md, section 4.1). It is the "holder" its public keys name, with what only itself has: the
    /// private halves of its identity key and of its signing key.
    /// </summary>
    /// <remarks>
    /// One shareholder, one voice: a quorum counts holders, not keys, and so far not shares either. An organization
    /// is a shareholder on its own machine only; of the others it knows what they publish, their public identity key,
    /// the certificate of their signing key and the URL of their copy.
    /// </remarks>
    public sealed class RoamingShareholder
    {

        #region Properties

        /// <summary>
        /// Its name: the holder its public keys name, as a quorum counts it.
        /// </summary>
        public String        Name                 { get; }

        /// <summary>
        /// Its identity key pair, the long-term one: it states who holds the keys of the roaming network operator,
        /// certifies its own signing key, and signs changes of the keys.
        /// </summary>
        public COSEKeyPair   IdentityKey          { get; }

        /// <summary>
        /// Its signing key pair, the everyday one: it signs commits, change sets and attestations as the roaming
        /// network operator.
        /// </summary>
        public COSEKeyPair   SigningKey           { get; }

        /// <summary>
        /// Where it serves its copy of a roaming network, ~/RNs/{RN_Id}, if it serves one.
        /// </summary>
        public URL?          URL                  { get; }

        /// <summary>
        /// The public half of its identity key, naming it as its holder: what a key statement and a trust anchor name.
        /// </summary>
        public POIPublicKey  IdentityPublicKey    { get; }

        /// <summary>
        /// The identification of its signing key, as its signatures name it.
        /// </summary>
        public String        SigningKeyId         { get; }

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new shareholder of its two key pairs.
        /// </summary>
        /// <param name="Name">Its name: a nonempty text without leading or trailing white space.</param>
        /// <param name="IdentityKey">Its identity key pair.</param>
        /// <param name="SigningKey">Its signing key pair, another one than its identity key pair.</param>
        /// <param name="URL">Where it serves its copy of a roaming network, if it serves one.</param>
        public RoamingShareholder(String       Name,
                                  COSEKeyPair  IdentityKey,
                                  COSEKeyPair  SigningKey,
                                  URL?         URL   = null)
        {

            ArgumentNullException.ThrowIfNull(Name);

            if (Name.Length == 0 || Name.Trim() != Name)
                throw new ArgumentException("The name of a shareholder must be a nonempty text without leading or trailing white space!", nameof(Name));

            if (IdentityKey.KeyPair is null)
                throw new ArgumentException($"The shareholder '{Name}' needs its identity key pair!", nameof(IdentityKey));

            if (SigningKey.KeyPair is null)
                throw new ArgumentException($"The shareholder '{Name}' needs its signing key pair!",  nameof(SigningKey));

            this.Name               = Name;
            this.IdentityKey        = IdentityKey;
            this.SigningKey         = SigningKey;
            this.URL                = URL;

            this.IdentityPublicKey  = POIPublicKey.From(IdentityKey.Private, Holder: Name);
            this.SigningKeyId       = POIPublicKey.From(SigningKey. Private).Id;

            if (IdentityPublicKey.Id == SigningKeyId)
                throw new ArgumentException($"The identity key of the shareholder '{Name}' must not be its signing key as well!", nameof(SigningKey));

        }

        #endregion


        #region (static) Create(Name, IdentityKey, SigningKey = null, URL = null)

        /// <summary>
        /// A new shareholder of the given identity key pair, with a new signing key pair unless one is given: ECDSA
        /// P-256 (ES256).
        /// </summary>
        /// <param name="Name">Its name.</param>
        /// <param name="IdentityKey">Its identity key pair.</param>
        /// <param name="SigningKey">Its signing key pair, a new ES256 one otherwise.</param>
        /// <param name="URL">Where it serves its copy of a roaming network, if it serves one.</param>
        public static RoamingShareholder Create(String        Name,
                                                COSEKeyPair   IdentityKey,
                                                COSEKeyPair?  SigningKey   = null,
                                                URL?          URL          = null)

            => new (Name,
                    IdentityKey,
                    SigningKey ?? COSEKeyPair.NewES256(),
                    URL);

        #endregion

        #region WithKeys(IdentityKey, SigningKey)

        /// <summary>
        /// This shareholder with other keys, e.g. of another algorithm: the same holder, and so the same voice.
        /// </summary>
        /// <param name="IdentityKey">Its new identity key pair.</param>
        /// <param name="SigningKey">Its new signing key pair.</param>
        public RoamingShareholder WithKeys(COSEKeyPair  IdentityKey,
                                           COSEKeyPair  SigningKey)

            => new (Name,
                    IdentityKey,
                    SigningKey,
                    URL);

        #endregion


        #region SigningPublicKey(NotAfter = null, NotBefore = null)

        /// <summary>
        /// The public half of its signing key, naming it as its holder, for the party only (POIKeyUsages.Party): it
        /// signs what the roaming network operator owns.
        /// </summary>
        /// <param name="NotAfter">Until when it may be used, unless always.</param>
        /// <param name="NotBefore">Since when it may be used, unless always.</param>
        public POIPublicKey SigningPublicKey(DateTimeOffset?  NotAfter    = null,
                                             DateTimeOffset?  NotBefore   = null)

            => POIPublicKey.From(SigningKey.Private,
                                 NotBefore,
                                 NotAfter,
                                 Name,
                                 [ POIKeyUsages.Party ]);

        #endregion

        #region CertifySigningKey(RoamingNetworkOperatorId, SignedAt, NotAfter = null, NotBefore = null)

        /// <summary>
        /// It certifies its own signing key with its identity key, alone: renewing a signing key needs no one else, as
        /// it gives its holder one voice only (SPECIFICATION.md, section 8.6).
        /// </summary>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator it is a shareholder of.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="NotAfter">Until when the signing key may be used, unless always.</param>
        /// <param name="NotBefore">Since when the signing key may be used, unless always.</param>
        public RoamingNetworkSigningKeyCertificate CertifySigningKey(RoamingNetworkOperator_Id  RoamingNetworkOperatorId,
                                                                     DateTimeOffset             SignedAt,
                                                                     DateTimeOffset?            NotAfter    = null,
                                                                     DateTimeOffset?            NotBefore   = null)

            => RoamingNetworkSigningKeyCertificate.Create(IdentityKey,
                                                          RoamingNetworkOperatorId,
                                                          SigningPublicKey(NotAfter, NotBefore),
                                                          SignedAt);

        #endregion

        #region StateKeys(RoamingNetworkId, RoamingNetworkOperatorId, IdentityKeys, MinConsensus, SignedAt, ValidUntil)

        /// <summary>
        /// It states who holds the identity keys of the roaming network operator in the given roaming network, and how
        /// many of the holders decide: a key statement signed with its identity key, as the first of its quorum. The
        /// others add their signatures with CoSign(…) (SPECIFICATION.md, section 8.6).
        /// </summary>
        /// <param name="RoamingNetworkId">The roaming network.</param>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator.</param>
        /// <param name="IdentityKeys">The identity keys of all shareholders, its own among them.</param>
        /// <param name="MinConsensus">How many of the shareholders decide.</param>
        /// <param name="SignedAt">When it is signed.</param>
        /// <param name="ValidUntil">Until when it is valid.</param>
        public RoamingNetworkKeyStatement StateKeys(RoamingNetwork_Id          RoamingNetworkId,
                                                    RoamingNetworkOperator_Id  RoamingNetworkOperatorId,
                                                    IEnumerable<POIPublicKey>  IdentityKeys,
                                                    Byte                       MinConsensus,
                                                    DateTimeOffset             SignedAt,
                                                    DateTimeOffset             ValidUntil)

            => RoamingNetworkKeyStatement.Create(IdentityKey,
                                                 MinConsensus,
                                                 RoamingNetworkId,
                                                 RoamingNetworkOperatorId,
                                                 IdentityKeys,
                                                 SignedAt,
                                                 ValidUntil);

        #endregion

        #region CoSign(KeyStatement)

        /// <summary>
        /// The given key statement with its signature as well, by its identity key: one more holder of the quorum.
        /// </summary>
        /// <param name="KeyStatement">A key statement another shareholder stated.</param>
        public RoamingNetworkKeyStatement CoSign(RoamingNetworkKeyStatement KeyStatement)
        {

            ArgumentNullException.ThrowIfNull(KeyStatement);

            return KeyStatement.WithSignature(IdentityKey);

        }

        #endregion

        #region SignWithIdentityKey(ChangeSet)

        /// <summary>
        /// The given change set with its signature by its identity key: a change of the keys of the roaming network
        /// operator, or the proof that it holds an identity key the change set enters anew.
        /// </summary>
        /// <param name="ChangeSet">A change set.</param>
        public RoamingNetworkChangeSet SignWithIdentityKey(RoamingNetworkChangeSet ChangeSet)
        {

            ArgumentNullException.ThrowIfNull(ChangeSet);

            return ChangeSet.Sign(IdentityKey.Private,
                                  IdentityPublicKey.Id,
                                  IdentityKey.Algorithm);

        }

        #endregion

        #region SignWithSigningKey(ChangeSet)

        /// <summary>
        /// The given change set with its signature by its signing key: a change of what the roaming network operator
        /// owns.
        /// </summary>
        /// <param name="ChangeSet">A change set.</param>
        public RoamingNetworkChangeSet SignWithSigningKey(RoamingNetworkChangeSet ChangeSet)
        {

            ArgumentNullException.ThrowIfNull(ChangeSet);

            return ChangeSet.Sign(SigningKey.Private,
                                  SigningKeyId,
                                  SigningKey.Algorithm);

        }

        #endregion

        #region CommitSignerFor(RoamingNetworkOperatorId)

        /// <summary>
        /// A signer of commits, checkpoints and attestations with its signing key, in the role of the given roaming
        /// network operator.
        /// </summary>
        /// <param name="RoamingNetworkOperatorId">The roaming network operator it is a shareholder of.</param>
        public RoamingNetworkCommitSigner CommitSignerFor(RoamingNetworkOperator_Id RoamingNetworkOperatorId)

            => new (SigningKey.Private,
                    SigningKeyId,
                    SigningKey.Algorithm,
                    RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                    RoamingNetworkOperatorId.ToString());

        #endregion


        #region (override) ToString()

        /// <summary>
        /// Return a text representation of this object.
        /// </summary>
        public override String ToString()

            => $"{Name} with the identity key '{IdentityPublicKey.Id}' ({IdentityKey.Algorithm.Name}) and the signing key '{SigningKeyId}' ({SigningKey.Algorithm.Name}){(URL.HasValue ? $", at {URL}" : "")}";

        #endregion

    }

}
