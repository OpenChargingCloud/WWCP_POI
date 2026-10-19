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

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Proofs
{

    /// <summary>
    /// Key statements of the roaming network operator and trust anchors: an app pins the identity keys of the operator,
    /// e.g. by scanning a QR code, and trusts the signing keys a key statement names, and so the attestations of proofs
    /// (SPECIFICATION.md, section 8.6).
    /// </summary>
    [TestFixture]
    public class KeyStatementTests
    {

        #region Data

        private static readonly DateTimeOffset    T0         = DateTimeOffset.Parse("2026-10-19T12:00:00Z");
        private static readonly ChargingStation_Id S1         = ChargingStation_Id.Parse("DE*GEF*S1");

        #endregion

        #region (static) Keys(Network)

        /// <summary>
        /// The test keys of the roaming network operator: its signing key and its identity key.
        /// </summary>
        private static ((Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters Private, POIPublicKey Public) Signing,
                        (Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters Private, POIPublicKey Public) Identity) Keys(RoamingNetwork Network)

            => (TestParties.Key(TestParties.Party(Network), POIKeyKind.Signing),
                TestParties.Key(TestParties.Party(Network), POIKeyKind.Identity));

        #endregion

        #region (static) Statement(Network, SignedAt, ValidUntil)

        /// <summary>
        /// A key statement of the operator of the given network, signed with its identity key.
        /// </summary>
        private static RoamingNetworkKeyStatement Statement(RoamingNetwork  Network,
                                                            DateTimeOffset  SignedAt,
                                                            DateTimeOffset  ValidUntil)
        {

            var (signing, identity) = Keys(Network);

            return RoamingNetworkKeyStatement.Create(identity.Private,
                                                     COSEAlgorithm.ES512,
                                                     Network.Id,
                                                     Network.Operator!.Id,
                                                     [ signing.Public ],
                                                     [ identity.Public ],
                                                     SignedAt,
                                                     ValidUntil);

        }

        #endregion

        #region (static) Commit(History, Statement)

        private static RoamingNetworkCommitResult Commit(RoamingNetworkHistory  History,
                                                         JToken                 Statement,
                                                         DateTimeOffset?        CreatedAt   = null)

            => History.Commit(History.Head.RoamingNetwork.CreateSignedChangeSet(
                                  [ RoamingNetworkChange.UpdateProperty(History.Head.RoamingNetwork.Operator!, RoamingNetworkKeyStatement.JSONProperty, null, Statement) ],
                                  CreatedAt: CreatedAt ?? T0));

        #endregion

        #region (static) Attested(SignedAt, ValidUntil, out History)

        /// <summary>
        /// A history whose operator states its signing key, and attests its head with it at the given time.
        /// </summary>
        private static RoamingNetworkHistory Attested(DateTimeOffset  SignedAt,
                                                      DateTimeOffset  ValidUntil,
                                                      DateTimeOffset  AttestedAt)
        {

            var history = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());
            var result  = Commit(history, Statement(history.Head.RoamingNetwork, SignedAt, ValidUntil).ToJSON());

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            var (signing, _) = Keys(history.Head.RoamingNetwork);

            history.AttestationSigners = [ new RoamingNetworkCommitSigner(signing.Private,
                                                                          signing.Public.Id,
                                                                          COSEAlgorithm.ES256,
                                                                          RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator) ];

            Assert.That(history.TryAttest(out _, out var error, AttestedAt), Is.True, error);

            return history;

        }

        #endregion


        #region A_key_statement_is_part_of_the_content()

        [Test]
        public void A_key_statement_is_part_of_the_content()
        {

            var history    = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());
            var statement  = Statement(history.Head.RoamingNetwork, T0, T0.AddDays(30));
            var result     = Commit(history, statement.ToJSON());

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            var stated     = history.Head.RoamingNetwork.Operator!.KeyStatement;
            var (signing, identity) = Keys(history.Head.RoamingNetwork);

            Assert.That(RoamingNetwork.TryParse(history.Head.RoamingNetwork.ToJSON(), out var parsed, out var error), Is.True, error);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(stated,                                                                    Is.Not.Null);
                Assert.That(JToken.DeepEquals(stated!.ToJSON(), statement.ToJSON()),                   Is.True);
                Assert.That(stated.KeyId,                                                              Is.EqualTo(identity.Public.Id));
                Assert.That(stated.SigningKeys,                                                        Is.EqualTo(new[] { signing.Public }));
                Assert.That(stated.TryVerify([ identity.Public.Id ], null, out error),                 Is.True, error);
                Assert.That(stated.ToJSON()["profile"]!.Value<String>(),                               Is.EqualTo("wwcp-poi-key-statement-v1"));
                Assert.That(JToken.DeepEquals(parsed!.Operator!.KeyStatement!.ToJSON(), statement.ToJSON()),  Is.True);
            }

        }

        #endregion

        #region A_key_statement_not_of_the_operator_is_refused()

        [Test]
        public void A_key_statement_not_of_the_operator_is_refused()
        {

            var history              = RoamingNetworkHistory.Start(StationProofTests.NewNetwork());
            var network              = history.Head.RoamingNetwork;
            var (signing, identity)  = Keys(network);
            var other                = TestParties.Key(TestParties.Party(ChargingStationOperator_Id.Parse("DE*GEF")), POIKeyKind.Identity);

            RoamingNetworkKeyStatement Signed(Org.BouncyCastle.Crypto.AsymmetricKeyParameter  Key,
                                              COSEAlgorithm                                  Algorithm,
                                              RoamingNetwork_Id?                             NetworkId     = null,
                                              POIPublicKey[]?                                SigningKeys   = null)

                => RoamingNetworkKeyStatement.Create(Key, Algorithm, NetworkId ?? network.Id, network.Operator!.Id, SigningKeys ?? [ signing.Public ], [ identity.Public ], T0, T0.AddDays(30));

            var bySigningKey   = Signed(signing.Private,  COSEAlgorithm.ES256);
            var byOtherParty   = Signed(other.Private,    COSEAlgorithm.ES512);
            var otherNetwork   = Signed(identity.Private, COSEAlgorithm.ES512, RoamingNetwork_Id.Parse("rn2"));
            var foreignKey     = Signed(identity.Private, COSEAlgorithm.ES512, SigningKeys: [ other.Public ]);

            var tampered       = (JObject) Statement(network, T0, T0.AddDays(30)).ToJSON();
            tampered["validUntil"] = T0.AddYears(10).ToISO8601();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => Commit(history, bySigningKey.ToJSON()),  Throws.ArgumentException.With.Message.Contains("identity keys"));
                Assert.That(() => Commit(history, byOtherParty.ToJSON()),  Throws.ArgumentException.With.Message.Contains("identity keys"));
                Assert.That(() => Commit(history, otherNetwork.ToJSON()),  Throws.ArgumentException.With.Message.Contains("rn2"));
                Assert.That(() => Commit(history, foreignKey.ToJSON()),    Throws.ArgumentException.With.Message.Contains("not one of its signing keys"));
                Assert.That(() => Commit(history, tampered),               Throws.ArgumentException.With.Message.Contains("identity keys"));
                Assert.That(history.Head.RoamingNetwork.Operator!.KeyStatement,  Is.Null);
            }

        }

        #endregion

        #region A_proof_verifies_against_pinned_identity_keys()

        [Test]
        public void A_proof_verifies_against_pinned_identity_keys()
        {

            var history              = Attested(T0, T0.AddDays(30), T0.AddDays(1));
            var (signing, identity)  = Keys(history.Head.RoamingNetwork);
            var at                   = T0.AddDays(1).AddMinutes(1);

            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out var error, At: at), Is.True, error);
            Assert.That(RoamingNetworkProof.TryParse(proof!.ToJSON().ToString(), out var read, out error), Is.True, error);

            // An attestation of a key the statement does not name.
            var byOther   = StationProofTests.OtherSigner.Attest(history.RoamingNetworkId, history.Head.Commit.Id, history.Head.Commit.Revision,
                                                                 history.Head.RoamingNetwork.EffectiveTreeETag(ETagHashAlgorithm.SHA256), T0.AddDays(1), T0.AddDays(1).AddMinutes(15));
            var unstated  = new RoamingNetworkProof(read!.HashAlgorithm, read.RoamingNetwork, read.CommitId, read.Revision, [ byOther ]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(read.TryGetKeyStatement(out var keyStatement, out _, out error),                Is.True, error);
                Assert.That(keyStatement!.SigningKeyOf(signing.Public.Id, at),                              Is.EqualTo(signing.Public));

                Assert.That(read.Verify([ identity.Public.Id ], out var attestation, out error, at),        Is.True, error);
                Assert.That(attestation!.KeyId,                                                             Is.EqualTo(signing.Public.Id));

                Assert.That(read.Verify([ signing.Public.Id ], out _, out error, at),                       Is.False, "a signing key is no identity key");
                Assert.That(read.Verify([ new String('0', 64) ], out _, out error, at),                     Is.False, "an identity key not pinned");
                Assert.That(read.Verify([ identity.Public.Id ], out _, out error, T0.AddDays(31)),          Is.False, "after the key statement");
                Assert.That(unstated.Verify([ identity.Public.Id ], out _, out error, at),                  Is.False, "a key the statement does not name");
            }

        }

        #endregion

        #region A_trust_anchor_is_a_URI_for_a_QR_code()

        [Test]
        public void A_trust_anchor_is_a_URI_for_a_QR_code()
        {

            var id      = new String('a', 64);
            var anchor  = new RoamingNetworkTrustAnchor(new Uri("https://hub.example/poi/RNs/rn1"), [ id, new String('b', 64) ]);
            var uri     = anchor.ToURI();

            using (Assert.EnterMultipleScope())
            {

                Assert.That(uri,                                                                     Is.EqualTo($"https://hub.example/poi/RNs/rn1?identityKeys={id},{new String('b', 64)}"));
                Assert.That(RoamingNetworkTrustAnchor.TryParse(uri, out var parsed, out var error),  Is.True, error);
                Assert.That(parsed!.RoamingNetworkId.ToString(),                                     Is.EqualTo("rn1"));
                Assert.That(parsed.IdentityKeyIds,                                                   Is.EqualTo(anchor.IdentityKeyIds));
                Assert.That(parsed.RoamingNetworkURI,                                                Is.EqualTo(new Uri("https://hub.example/poi/RNs/rn1")));

                Assert.That(RoamingNetworkTrustAnchor.TryParse("https://hub.example/poi/RNs/rn1",                                 out _, out _),  Is.False, "no identity keys");
                Assert.That(RoamingNetworkTrustAnchor.TryParse("https://hub.example/poi/RNs/rn1?identityKeys=ABC",                out _, out _),  Is.False, "not a thumbprint");
                Assert.That(RoamingNetworkTrustAnchor.TryParse($"https://hub.example/poi/CSOs/rn1?identityKeys={id}",             out _, out _),  Is.False, "not a roaming network");
                Assert.That(RoamingNetworkTrustAnchor.TryParse($"ftp://hub.example/poi/RNs/rn1?identityKeys={id}",               out _, out _),  Is.False, "neither http nor https");

            }

        }

        #endregion

        #region An_app_scans_the_trust_anchor_and_verifies_a_proof_over_HTTP()

        [Test]
        public async Task An_app_scans_the_trust_anchor_and_verifies_a_proof_over_HTTP()
        {

            var now      = Timestamp.Now;
            var history  = Attested(now.AddHours(-1), now.AddDays(30), now);

            await using var httpServer = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI     = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(history);

            using var httpClient    = new HttpClient();

            // The operator prints the URI of its trust anchor as a QR code; the app scans it.
            var trustAnchorJSON     = JObject.Parse(await httpClient.GetStringAsync($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/trustAnchor"));
            var keyStatementJSON    = JObject.Parse(await httpClient.GetStringAsync($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/keyStatement"));

            Assert.That(RoamingNetworkTrustAnchor.TryParse(trustAnchorJSON["uri"]!.Value<String>()!, out var anchor, out var error), Is.True, error);

            // Where the app finds the proofs: below the roaming network the anchor names.
            var proofText           = await httpClient.GetStringAsync($"{anchor!.RoamingNetworkURI}/CSs/{S1}/proof");

            Assert.That(RoamingNetworkProof.TryParse(proofText, out var proof, out error), Is.True, error);

            var (_, identity)       = Keys(history.Head.RoamingNetwork);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(anchor.RoamingNetworkURI.ToString(),                         Is.EqualTo($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1"));
                Assert.That(anchor.IdentityKeyIds,                                       Is.EqualTo(new[] { identity.Public.Id }));
                Assert.That(keyStatementJSON["keyId"]!.Value<String>(),                  Is.EqualTo(identity.Public.Id));
                Assert.That(proof!.Verify(anchor.IdentityKeyIds, out _, out error),      Is.True, error);
            }

        }

        #endregion

    }

}
