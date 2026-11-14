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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI
{

    /// <summary>
    /// Changes of who holds the keys of the roaming network operator: its shareholders, admitted and dismissed.
    /// </summary>
    public sealed partial class RoamingNetwork
    {

        #region Admit(IdentityKey, SigningKeyCertificate, KeyStatement, URL = null, Description = null, Metadata = null, CreatedAt = null)

        /// <summary>
        /// Prepare the unsigned change set by which one more shareholder holds the keys of the roaming network operator
        /// of this version (SPECIFICATION.md, section 4.1): its identity key next to those so far, the key statement
        /// naming all of them, the certificate of its signing key next to those so far, the minConsensus of the key
        /// statement unless it stays, and the URL of its copy next to those so far, if it serves one.
        /// It is one change set, as the quorum must fit the keys in every version, not between its changes.
        /// To be committed it is signed by identity keys of as many shareholders as this version requires, by the
        /// new identity key as the proof that its holder has it and asked for it, and by signing keys of as many
        /// shareholders, as the key statement and the URLs are content of the roaming network operator and the network.
        /// This version stays unchanged.
        /// </summary>
        /// <param name="IdentityKey">The public identity key of the new shareholder, naming it as its holder.</param>
        /// <param name="SigningKeyCertificate">The certificate of its signing key, by that identity key.</param>
        /// <param name="KeyStatement">The key statement of the identity keys so far and the new one, signed by the quorum so far and by its own.</param>
        /// <param name="URL">Where the new shareholder serves its copy of this roaming network, if it serves one.</param>
        /// <param name="Description">An optional multi-language description.</param>
        /// <param name="Metadata">Optional application metadata.</param>
        /// <param name="CreatedAt">An optional commit timestamp, now by default.</param>
        public RoamingNetworkChangeSet Admit(POIPublicKey                         IdentityKey,
                                             RoamingNetworkSigningKeyCertificate  SigningKeyCertificate,
                                             RoamingNetworkKeyStatement           KeyStatement,
                                             URL?                                 URL           = null,
                                             ImmutableI18NString?                 Description   = null,
                                             JObject?                             Metadata      = null,
                                             DateTimeOffset?                      CreatedAt     = null)
        {

            ArgumentNullException.ThrowIfNull(IdentityKey);
            ArgumentNullException.ThrowIfNull(SigningKeyCertificate);
            ArgumentNullException.ThrowIfNull(KeyStatement);

            if (Operator is null)
                throw new InvalidOperationException($"The roaming network '{Id}' has no roaming network operator a shareholder could be admitted to!");

            if (IdentityKey.Holder is null)
                throw new ArgumentException($"The identity key '{IdentityKey.Id}' names no holder: a shareholder is admitted by its name!", nameof(IdentityKey));

            if (POIPartyKeys.Holders(Operator.IdentityKeys).Contains(IdentityKey.Holder))
                throw new ArgumentException($"'{IdentityKey.Holder}' holds keys of the roaming network operator '{Operator.Id}' already!", nameof(IdentityKey));

            if (SigningKeyCertificate.Holder != IdentityKey.Holder)
                throw new ArgumentException($"The certificate of the signing key '{SigningKeyCertificate.SigningKey.Id}' is one of '{SigningKeyCertificate.Holder}', not of '{IdentityKey.Holder}'!", nameof(SigningKeyCertificate));

            var changes = new List<RoamingNetworkChange> {

                              RoamingNetworkChange.UpdateProperty(Operator,
                                                                  POIPartyKeys.IdentityKeysProperty,
                                                                  null,
                                                                  new JArray(Operator.IdentityKeys.
                                                                                 Append(IdentityKey).
                                                                                 Select(identityKey => identityKey.ToJSON(Embedded: true)))),

                              RoamingNetworkChange.UpdateProperty(Operator,
                                                                  RoamingNetworkKeyStatement.JSONProperty,
                                                                  null,
                                                                  KeyStatement.ToJSON(Embedded: true)),

                              RoamingNetworkChange.UpdateProperty(Operator,
                                                                  RoamingNetworkSigningKeyCertificate.JSONProperty,
                                                                  null,
                                                                  new JArray(Operator.SigningKeyCertificates.
                                                                                 Append(SigningKeyCertificate).
                                                                                 Select(certificate => certificate.ToJSON(Embedded: true))))

                          };

            // The quorum stays, e.g. two of three after two of two, or it is changed with the keys it fits.
            if (KeyStatement.MinConsensus != Operator.MinConsensus)
                changes.Add(RoamingNetworkChange.UpdateProperty(Operator,
                                                                POIPartyKeys.MinConsensusProperty,
                                                                null,
                                                                new JValue(KeyStatement.MinConsensus)));

            if (URL.HasValue && !URLs.Contains(URL.Value))
                changes.Add(RoamingNetworkChange.UpdateProperty(this,
                                                                POINetworkURLs.JSONProperty,
                                                                null,
                                                                POINetworkURLs.ToJSON(URLs.Append(URL.Value))));

            return CreateChangeSet(changes,
                                   Description,
                                   Metadata,
                                   CreatedAt);

        }

        #endregion

        #region Admit(Shareholder, KeyStatement, SigningKeyNotAfter = null, Description = null, Metadata = null, CreatedAt = null)

        /// <summary>
        /// Prepare the unsigned change set by which the given shareholder holds the keys of the roaming network operator
        /// of this version as well, as Admit(IdentityKey, …) does: with its public identity key, the certificate of its
        /// signing key, made now, and its URL. Where the new shareholder itself prepares the change set; the shareholders
        /// so far, knowing only what it publishes, call Admit(IdentityKey, …).
        /// </summary>
        /// <param name="Shareholder">The new shareholder.</param>
        /// <param name="KeyStatement">The key statement of the identity keys so far and the new one, signed by the quorum so far and by its own.</param>
        /// <param name="SigningKeyNotAfter">Until when the signing key of the new shareholder may be used, unless always.</param>
        /// <param name="Description">An optional multi-language description.</param>
        /// <param name="Metadata">Optional application metadata.</param>
        /// <param name="CreatedAt">An optional commit timestamp, now by default: when its signing key is certified as well.</param>
        public RoamingNetworkChangeSet Admit(RoamingShareholder          Shareholder,
                                             RoamingNetworkKeyStatement  KeyStatement,
                                             DateTimeOffset?             SigningKeyNotAfter   = null,
                                             ImmutableI18NString?        Description          = null,
                                             JObject?                    Metadata             = null,
                                             DateTimeOffset?             CreatedAt            = null)
        {

            ArgumentNullException.ThrowIfNull(Shareholder);

            if (Operator is null)
                throw new InvalidOperationException($"The roaming network '{Id}' has no roaming network operator a shareholder could be admitted to!");

            return Admit(Shareholder.IdentityPublicKey,
                         Shareholder.CertifySigningKey(Operator.Id,
                                                       CreatedAt ?? Timestamp.Now,
                                                       SigningKeyNotAfter),
                         KeyStatement,
                         Shareholder.URL,
                         Description,
                         Metadata,
                         CreatedAt);

        }

        #endregion


        #region Dismiss(Holder, KeyStatement, URL = null, Description = null, Metadata = null, CreatedAt = null)

        /// <summary>
        /// Prepare the unsigned change set by which the given shareholder holds the keys of the roaming network operator
        /// of this version no longer (SPECIFICATION.md, section 4.1): the identity keys without its ones, the key
        /// statement naming those that stay, the certificates without those of its signing keys, the minConsensus of the
        /// key statement unless it stays, not written when it is one, and the URLs without the one of its copy, if given.
        /// It is one change set, as the quorum must fit the keys in every version, not between its changes.
        /// To be committed it is signed as every change of the keys: by identity keys, and for the key statement and
        /// the URLs by signing keys, of as many shareholders as this version requires. So two of three dismiss the
        /// third without it, and of two deciding together the one leaving signs as well: it resigns, as nobody is
        /// dismissed by the other one alone. This version stays unchanged.
        /// </summary>
        /// <param name="Holder">The name of the shareholder, as its keys name it as their holder.</param>
        /// <param name="KeyStatement">The key statement of the identity keys that stay, signed by the quorum so far and by its own.</param>
        /// <param name="URL">Where the shareholder serves its copy of this roaming network, a URL this version names, if it serves one.</param>
        /// <param name="Description">An optional multi-language description.</param>
        /// <param name="Metadata">Optional application metadata.</param>
        /// <param name="CreatedAt">An optional commit timestamp, now by default.</param>
        public RoamingNetworkChangeSet Dismiss(String                      Holder,
                                               RoamingNetworkKeyStatement  KeyStatement,
                                               URL?                        URL           = null,
                                               ImmutableI18NString?        Description   = null,
                                               JObject?                    Metadata      = null,
                                               DateTimeOffset?             CreatedAt     = null)
        {

            ArgumentNullException.ThrowIfNull(Holder);
            ArgumentNullException.ThrowIfNull(KeyStatement);

            if (Operator is null)
                throw new InvalidOperationException($"The roaming network '{Id}' has no roaming network operator a shareholder could be dismissed from!");

            if (Holder.Length == 0)
                throw new ArgumentException("A shareholder is dismissed by its name: the holder its keys name!", nameof(Holder));

            if (!POIPartyKeys.Holders(Operator.IdentityKeys).Contains(Holder))
                throw new ArgumentException($"'{Holder}' holds no keys of the roaming network operator '{Operator.Id}'!", nameof(Holder));

            var identityKeys  = Operator.IdentityKeys.Where(identityKey => identityKey.HolderOrUnnamed != Holder).ToArray();

            if (identityKeys.Length == 0)
                throw new ArgumentException($"'{Holder}' is the last one holding keys of the roaming network operator '{Operator.Id}': it can not be dismissed!", nameof(Holder));

            if (URL.HasValue && !URLs.Contains(URL.Value))
                throw new ArgumentException($"The roaming network '{Id}' does not name the URL '{URL}'!", nameof(URL));

            var changes = new List<RoamingNetworkChange> {

                              RoamingNetworkChange.UpdateProperty(Operator,
                                                                  POIPartyKeys.IdentityKeysProperty,
                                                                  null,
                                                                  new JArray(identityKeys.Select(identityKey => identityKey.ToJSON(Embedded: true)))),

                              RoamingNetworkChange.UpdateProperty(Operator,
                                                                  RoamingNetworkKeyStatement.JSONProperty,
                                                                  null,
                                                                  KeyStatement.ToJSON(Embedded: true)),

                              RoamingNetworkChange.UpdateProperty(Operator,
                                                                  RoamingNetworkSigningKeyCertificate.JSONProperty,
                                                                  null,
                                                                  new JArray(Operator.SigningKeyCertificates.
                                                                                 Where (certificate => certificate.Holder != Holder).
                                                                                 Select(certificate => certificate.ToJSON(Embedded: true))))

                          };

            // The quorum stays, e.g. two of two after two of three, or it is changed with the keys it fits: a
            // minConsensus of one is not written.
            if (KeyStatement.MinConsensus != Operator.MinConsensus)
                changes.Add(KeyStatement.MinConsensus > 1
                                ? RoamingNetworkChange.UpdateProperty(Operator,
                                                                      POIPartyKeys.MinConsensusProperty,
                                                                      null,
                                                                      new JValue(KeyStatement.MinConsensus))
                                : RoamingNetworkChange.RemoveProperty(Operator,
                                                                      POIPartyKeys.MinConsensusProperty));

            if (URL.HasValue)
                changes.Add(URLs.Length > 1
                                ? RoamingNetworkChange.UpdateProperty(this,
                                                                      POINetworkURLs.JSONProperty,
                                                                      null,
                                                                      POINetworkURLs.ToJSON(URLs.Where(url => url != URL.Value)))
                                : RoamingNetworkChange.RemoveProperty(this,
                                                                      POINetworkURLs.JSONProperty));

            return CreateChangeSet(changes,
                                   Description,
                                   Metadata,
                                   CreatedAt);

        }

        #endregion

        #region Dismiss(Shareholder, KeyStatement, Description = null, Metadata = null, CreatedAt = null)

        /// <summary>
        /// Prepare the unsigned change set by which the given shareholder holds the keys of the roaming network operator
        /// of this version no longer, as Dismiss(Holder, …) does: by its name, and with the URL of its copy where this
        /// version names it.
        /// </summary>
        /// <param name="Shareholder">The shareholder leaving.</param>
        /// <param name="KeyStatement">The key statement of the identity keys that stay, signed by the quorum so far and by its own.</param>
        /// <param name="Description">An optional multi-language description.</param>
        /// <param name="Metadata">Optional application metadata.</param>
        /// <param name="CreatedAt">An optional commit timestamp, now by default.</param>
        public RoamingNetworkChangeSet Dismiss(RoamingShareholder          Shareholder,
                                               RoamingNetworkKeyStatement  KeyStatement,
                                               ImmutableI18NString?        Description   = null,
                                               JObject?                    Metadata      = null,
                                               DateTimeOffset?             CreatedAt     = null)
        {

            ArgumentNullException.ThrowIfNull(Shareholder);

            return Dismiss(Shareholder.Name,
                           KeyStatement,
                           Shareholder.URL.HasValue && URLs.Contains(Shareholder.URL.Value)
                               ? Shareholder.URL
                               : null,
                           Description,
                           Metadata,
                           CreatedAt);

        }

        #endregion

    }

}
