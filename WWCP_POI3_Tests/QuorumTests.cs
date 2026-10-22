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

using NUnit.Framework;

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The quorum of a roaming network operator (SPECIFICATION.md, sections 4.1, 8.5 and 8.6): its keys held by several
    /// holders, e.g. companies, of which minConsensus must sign what it owns, its key statement included, and attest a
    /// version, so that none of them changes the network or vouches for it alone; more than half of them, so that two
    /// quorums always share a holder. Holders are counted, not keys.
    /// </summary>
    [TestFixture]
    public class QuorumTests
    {

        #region Data

        private static readonly DateTimeOffset      T0       = DateTimeOffset.Parse("2026-10-21T12:00:00Z");
        private static readonly ChargingStation_Id  S1       = ChargingStation_Id.Parse("DE*GEF*S1");
        private static readonly String[]            Holders  = [ "Alpha", "Beta", "Gamma" ];

        #endregion

        #region (static) Key(Network, Holder, Kind, Number = 1)

        private static (ECPrivateKeyParameters Private, POIPublicKey Public) Key(RoamingNetwork  Network,
                                                                                 String          Holder,
                                                                                 POIKeyKind      Kind,
                                                                                 Int32           Number   = 1)

            => TestParties.QuorumKey(Network.Id, Holder, Kind, Number);

        #endregion

        #region (static) Network(MinConsensus = 2, ExtraSigningKeyOf = null)

        /// <summary>
        /// The network of the station proof tests, its operator with the signing and identity keys of three holders and
        /// the given minConsensus; optionally one holder with a second signing key, e.g. while it renews its key.
        /// </summary>
        private static RoamingNetwork Network(Byte     MinConsensus        = 2,
                                              String?  ExtraSigningKeyOf   = null)
        {

            var network       = StationProofTests.NewNetwork();
            var json          = network.ToJSON(IncludeETags: false);
            var operatorJSON  = (JObject) json[RoamingNetwork.PropertyKeys.Operator]!;

            var signingKeys   = Holders.Select(holder => Key(network, holder, POIKeyKind.Signing).Public).ToList();

            if (ExtraSigningKeyOf is not null)
                signingKeys.Add(Key(network, ExtraSigningKeyOf, POIKeyKind.Signing, 2).Public);

            operatorJSON[POIPartyKeys.SigningKeysProperty]   = new JArray(signingKeys.Select(key => key.ToJSON(Embedded: true)));
            operatorJSON[POIPartyKeys.IdentityKeysProperty]  = new JArray(Holders.Select(holder => Key(network, holder, POIKeyKind.Identity).Public.ToJSON(Embedded: true)));

            if (MinConsensus > 1)
                operatorJSON[POIPartyKeys.MinConsensusProperty] = MinConsensus;

            return RoamingNetwork.TryParse(json, out var quorumNetwork, out var error)
                       ? quorumNetwork
                       : throw new ArgumentException(error);

        }

        #endregion

        #region (static) Signed(ChangeSet, Network, Kind, Holders)

        /// <summary>
        /// The given change set signed by the keys of the given kind of the given holders; "Alpha#2" names the second key
        /// of Alpha.
        /// </summary>
        private static RoamingNetworkChangeSet Signed(RoamingNetworkChangeSet  ChangeSet,
                                                      RoamingNetwork           Network,
                                                      POIKeyKind               Kind,
                                                      params String[]          Holders)
        {

            foreach (var holder in Holders)
            {

                var parts                    = holder.Split('#');
                var (privateKey, publicKey)  = Key(Network, parts[0], Kind, parts.Length > 1 ? Int32.Parse(parts[1]) : 1);

                ChangeSet = ChangeSet.Sign(privateKey, publicKey.Id, Kind == POIKeyKind.Signing ? COSEAlgorithm.ES256 : COSEAlgorithm.ES512);

            }

            return ChangeSet;

        }

        #endregion

        #region (static) Statement(Network, Holders)

        /// <summary>
        /// A key statement of the operator of the given network, signed by the identity keys of the given holders.
        /// </summary>
        private static RoamingNetworkKeyStatement Statement(RoamingNetwork   Network,
                                                            params String[]  Holders)

            => RoamingNetworkKeyStatement.Create([ .. Holders.Select(holder => ((Org.BouncyCastle.Crypto.AsymmetricKeyParameter) Key(Network, holder, POIKeyKind.Identity).Private, COSEAlgorithm.ES512)) ],
                                                 Network.Operator!.MinConsensus,
                                                 Network.Id,
                                                 Network.Operator!.Id,
                                                 Network.Operator!.SigningKeys,
                                                 Network.Operator!.IdentityKeys,
                                                 T0,
                                                 T0.AddDays(30));

        #endregion


        #region The_quorum_needs_more_than_half_of_the_holders()

        /// <summary>
        /// minConsensus is more than half of the holders of each kind of keys of the operator, and at most as many: so 2 of
        /// 3, 3 of 4 or 3 of 5; an odd number of holders is not needed. Keys naming no holder have one.
        /// </summary>
        [Test]
        public void The_quorum_needs_more_than_half_of_the_holders()
        {

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Network(2).Operator!.MinConsensus,  Is.EqualTo(2));
                Assert.That(Network(3).Operator!.MinConsensus,  Is.EqualTo(3));
                Assert.That(() => Network(1),                   Throws.ArgumentException.With.Message.Contains("more than half of the 3 holders"));
                Assert.That(() => Network(4),                   Throws.ArgumentException.With.Message.Contains("as many holders, not of 3"));
                Assert.That(StationProofTests.NewNetwork().Operator!.MinConsensus,  Is.EqualTo(1), "the keys of one holder, naming none");
                Assert.That(Network(2).Operator!.ToJSON()[POIPartyKeys.MinConsensusProperty]!.Value<Int32>(),             Is.EqualTo(2));
                Assert.That(StationProofTests.NewNetwork().Operator!.ToJSON().ContainsKey(POIPartyKeys.MinConsensusProperty),  Is.False, "written unless one");
            }

        }

        #endregion

        #region A_change_of_the_operator_needs_its_quorum()

        /// <summary>
        /// What the operator owns, e.g. the roaming network itself, needs signing keys of two of its three holders: one is
        /// not enough, nor one holder signing with two keys of its own.
        /// </summary>
        [Test]
        public void A_change_of_the_operator_needs_its_quorum()
        {

            var network    = Network(2, ExtraSigningKeyOf: "Alpha");
            var changeSet  = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "quorum") ], CreatedAt: T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Signing, "Alpha"),            out _, out var error1),  Is.False);
                Assert.That(error1,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Signing, "Alpha", "Alpha#2"), out _, out var error2),  Is.False, "one holder, two keys");
                Assert.That(error2,  Does.Contain("needs signatures by signing keys of 2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Signing, "Alpha", "Gamma"),   out var changed, out var error3),  Is.True, error3);
                Assert.That(changed!.DataSource,  Is.EqualTo("quorum"));

            }

        }

        #endregion

        #region The_quorum_is_changed_by_identity_keys_of_its_holders()

        /// <summary>
        /// The minConsensus of the operator is changed as its keys: by its identity keys, of as many holders as the version
        /// before needs; and only to a quorum the keys allow.
        /// </summary>
        [Test]
        public void The_quorum_is_changed_by_identity_keys_of_its_holders()
        {

            var network    = Network(2);
            var toThree    = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, POIPartyKeys.MinConsensusProperty, 2, 3) ], CreatedAt: T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(Signed(toThree, network, POIKeyKind.Signing,  "Alpha", "Beta"),  out _, out var error1),  Is.False, "signing keys");
                Assert.That(error1,  Does.Contain("identity keys of 2 holders"));

                Assert.That(network.TryApplyChangeSet(Signed(toThree, network, POIKeyKind.Identity, "Alpha"),          out _, out var error2),  Is.False, "one holder");
                Assert.That(error2,  Does.Contain("identity keys of 2 holders"));

                Assert.That(network.TryApplyChangeSet(Signed(toThree, network, POIKeyKind.Identity, "Alpha", "Beta"),  out var changed, out var error3),  Is.True, error3);
                Assert.That(changed!.Operator!.MinConsensus,  Is.EqualTo(3));

                Assert.That(() => network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, POIPartyKeys.MinConsensusProperty, 2, 1) ], CreatedAt: T0),
                            Throws.ArgumentException.With.Message.Contains("more than half"));

            }

        }

        #endregion

        #region The_operator_recovers_keys_of_a_party_by_its_quorum()

        /// <summary>
        /// The identity keys of the operator recover the keys of other parties: by as many holders as its quorum needs.
        /// </summary>
        [Test]
        public void The_operator_recovers_keys_of_a_party_by_its_quorum()
        {

            var network    = Network(2);
            var gef        = network.GetChargingStationOperatorById(ChargingStationOperator_Id.Parse("DE*GEF"))!;
            var newKey     = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF/recovered"), POIKeyKind.Signing).Public;
            var changeSet  = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.SigningKeysProperty, null, new JArray(newKey.ToJSON(Embedded: true))) ], CreatedAt: T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Identity, "Beta"),           out _, out var error1),  Is.False);
                Assert.That(error1,  Does.Contain("identity keys of 2 holders of"));

                Assert.That(network.TryApplyChangeSet(Signed(changeSet, network, POIKeyKind.Identity, "Beta", "Gamma"),  out var changed, out var error2),  Is.True, error2);
                Assert.That(changed!.GetChargingStationOperatorById(gef.Id)!.SigningKeys,  Is.EqualTo(new[] { newKey }));

            }

        }

        #endregion

        #region A_key_statement_is_signed_by_the_quorum()

        /// <summary>
        /// The key statement of an operator with a quorum names its minConsensus and is signed by identity keys of as many
        /// holders (profile wwcp-poi-key-statement-v2): signed by one of them, the version is refused; the others add their
        /// signatures each on its own, and it reads back as written.
        /// </summary>
        [Test]
        public void A_key_statement_is_signed_by_the_quorum()
        {

            var network    = Network(2);
            var byAlpha    = Statement(network, "Alpha");
            var byTwo      = byAlpha.WithSignature(Key(network, "Beta", POIKeyKind.Identity).Private, COSEAlgorithm.ES512);

            RoamingNetworkChangeSet Stating(RoamingNetworkKeyStatement KeyStatement)

                => Signed(network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkKeyStatement.JSONProperty, null, KeyStatement.ToJSON(Embedded: true)) ], CreatedAt: T0),
                          network, POIKeyKind.Signing, "Alpha", "Beta");

            Assert.That(RoamingNetworkKeyStatement.TryParse(byTwo.ToJSON(), out var read, out var parseError), Is.True, parseError);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(byTwo.ToJSON()["@context"]!.Value<String>(),       Is.EqualTo(RoamingNetworkKeyStatement.JSONLDContext));
                Assert.That(byTwo.ToJSON()["minConsensus"]!.Value<Int32>(),    Is.EqualTo(2));
                Assert.That(byTwo.Signatures.Select(signature => signature.KeyId),  Is.EqualTo(new[] { Key(network, "Alpha", POIKeyKind.Identity).Public.Id, Key(network, "Beta", POIKeyKind.Identity).Public.Id }));
                Assert.That(JToken.DeepEquals(read!.ToJSON(), byTwo.ToJSON()), Is.True);

                Assert.That(byTwo.  TryVerify(network.Operator!.IdentityKeys.Select(key => key.Id), null, out var error1),  Is.True, error1);
                Assert.That(byAlpha.TryVerify(network.Operator!.IdentityKeys.Select(key => key.Id), null, out var error2),  Is.False);
                Assert.That(error2,  Does.Contain("of 1 holder, not of 2"));

                Assert.That(() => Stating(byAlpha),  Throws.ArgumentException.With.Message.Contains("identity keys of 2 of its holders"));
                Assert.That(network.TryApplyChangeSet(Stating(byTwo), out var stated, out var error3),  Is.True, error3);
                Assert.That(stated!.Operator!.KeyStatement!.Signatures,  Has.Length.EqualTo(2));

            }

        }

        #endregion

        #region A_proof_needs_attestations_of_the_quorum()

        /// <summary>
        /// An app pinning the trust anchor of a roaming network with a quorum trusts a proof only when the key statement is
        /// signed by identity keys of minConsensus holders and the root is attested by signing keys of as many.
        /// </summary>
        [Test]
        public void A_proof_needs_attestations_of_the_quorum()
        {

            RoamingNetworkHistory AttestedBy(params String[] Holders)
            {

                var history  = RoamingNetworkHistory.Start(Network(2));
                var network  = history.Head.RoamingNetwork;
                var result   = history.Commit(Signed(network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkKeyStatement.JSONProperty, null, Statement(network, "Alpha", "Beta").ToJSON(Embedded: true)) ], CreatedAt: T0),
                                                     network, POIKeyKind.Signing, "Alpha", "Beta"));

                Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

                history.AttestationSigners = [ .. Holders.Select(holder => new RoamingNetworkCommitSigner(Key(network, holder, POIKeyKind.Signing).Private,
                                                                                                          Key(network, holder, POIKeyKind.Signing).Public.Id,
                                                                                                          COSEAlgorithm.ES256,
                                                                                                          RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator,
                                                                                                          holder)) ];

                Assert.That(history.TryAttest(out _, out var error, T0.AddDays(1)), Is.True, error);

                return history;

            }

            var at        = T0.AddDays(1).AddMinutes(1);
            var byOne     = AttestedBy("Alpha");
            var byTwo     = AttestedBy("Alpha", "Gamma");
            var network   = byTwo.Head.RoamingNetwork;
            var anchor    = new RoamingNetworkTrustAnchor(network.Id,
                                                          URL.Parse($"https://hub.example/poi/RNs/{network.Id}"),
                                                          [ .. network.Operator!.IdentityKeys.Select(key => key.Id) ],
                                                          network.Operator!.MinConsensus);

            Assert.That(byOne.TryGetChargingStationProof(S1, out var proofOfOne, out var error, At: at), Is.True, error);
            Assert.That(byTwo.TryGetChargingStationProof(S1, out var proofOfTwo, out error,     At: at), Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(proofOfTwo!.Verify(anchor, out var attestation, out var error1, at),  Is.True, error1);
                Assert.That(attestation,                                                         Is.Not.Null);

                Assert.That(proofOfOne!.Verify(anchor, out _, out var error2, at),  Is.False);
                Assert.That(error2,  Does.Contain("of 1 holder"));

                // A verifier pinning the keys without the minConsensus still needs the one the key statement names.
                Assert.That(proofOfOne.Verify(anchor.IdentityKeyIds, out _, out _, at),  Is.False);

            }

        }

        #endregion

        #region A_trust_anchor_is_CBOR_in_a_QR_code()

        /// <summary>
        /// The trust anchor of a roaming network with a quorum, with its name, description and further information: the
        /// text of its QR code is WWCPPOI1: and the Base45 of its CBOR, all of the alphanumeric mode of QR codes, and reads
        /// back as it was; the identity keys are their 32 bytes in CBOR.
        /// </summary>
        [Test]
        public void A_trust_anchor_is_CBOR_in_a_QR_code()
        {

            var network  = Network(2);
            var anchor   = new RoamingNetworkTrustAnchor(network.Id,
                                                         URL.Parse($"https://hub.example/poi/RNs/{network.Id}"),
                                                         [ .. network.Operator!.IdentityKeys.Select(key => key.Id) ],
                                                         2,
                                                         new ImmutableI18NString(I18NString.Create(Languages.de, "Beispielnetz").Set(Languages.en, "Example network")),
                                                         new ImmutableI18NString(I18NString.Create(Languages.en, "Charging stations of the example operators")));

            var qrCode   = anchor.ToQRCode();

            Assert.That(RoamingNetworkTrustAnchor.TryParse(qrCode, out var read, out var error), Is.True, error);
            Assert.That(anchor.ToCBOR().TryGetValue(CBORValue.FromText("identityKeys"), out var identityKeysCBOR), Is.True);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(qrCode,                    Does.StartWith("WWCPPOI1:"));
                Assert.That(qrCode.All(character => "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:".Contains(character)),  Is.True, "the alphanumeric mode");
                Assert.That(qrCode.Length,             Is.LessThan(1000));

                Assert.That(read!.RoamingNetworkId,    Is.EqualTo(network.Id));
                Assert.That(read.RoamingNetworkURL,    Is.EqualTo(anchor.RoamingNetworkURL));
                Assert.That(read.IdentityKeyIds,       Is.EqualTo(anchor.IdentityKeyIds));
                Assert.That(read.MinConsensus,         Is.EqualTo((Byte) 2));
                Assert.That(read.Name,                 Is.EqualTo(anchor.Name));
                Assert.That(read.Description,          Is.EqualTo(anchor.Description));
                Assert.That(JToken.DeepEquals(read.ToJSON(), anchor.ToJSON()),  Is.True);

                Assert.That(identityKeysCBOR.AsArray().All(id => id.TryGetBytes(out var bytes) && bytes.Length == 32),  Is.True);

                Assert.That(anchor.ToURI(),            Does.EndWith("&minConsensus=2"));
                Assert.That(RoamingNetworkTrustAnchor.TryParse(anchor.ToURI(), out var fromURI, out error),  Is.True, error);
                Assert.That(fromURI!.MinConsensus,     Is.EqualTo((Byte) 2));

                Assert.That(() => new RoamingNetworkTrustAnchor(anchor.RoamingNetworkId, anchor.RoamingNetworkURL, anchor.IdentityKeyIds, 4),
                            Throws.ArgumentException.With.Message.Contains("needs as many identity keys"));

                var otherNetwork = anchor.ToJSON();
                otherNetwork["roamingNetworkId"] = "rn9";
                Assert.That(RoamingNetworkTrustAnchor.TryParse(otherNetwork, out _, out var error2),  Is.False);
                Assert.That(error2,  Does.Contain("names the URL of"));

            }

        }

        #endregion

    }

}
