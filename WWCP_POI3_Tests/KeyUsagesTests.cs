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

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The usages of signing keys (SPECIFICATION.md, section 4.1): a signing key naming none signs everything its party
    /// owns, one naming usages only what they name, e.g. a charging station operator signing its pools with one key and
    /// its tariffs with another; price statements and attestations need their usages as well.
    /// </summary>
    [TestFixture]
    public class KeyUsagesTests
    {

        #region Data

        private static readonly DateTimeOffset              T0    = DateTimeOffset.Parse("2026-10-21T12:00:00Z");
        private static readonly ChargingStationOperator_Id  GEF   = ChargingStationOperator_Id.Parse("DE*GEF");

        #endregion

        #region (static) Key(Name, Usages)

        /// <summary>
        /// A signing key of the charging station operator DE*GEF, named for the test, with the given usages.
        /// </summary>
        private static (ECPrivateKeyParameters Private, POIPublicKey Public) Key(String           Name,
                                                                                 params String[]  Usages)
        {

            var (privateKey, _) = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), $"DE*GEF/{Name}"), POIKeyKind.Signing);

            return (privateKey, POIPublicKey.From(privateKey, Usages: Usages));

        }

        #endregion

        #region (static) Network(SigningKeys)

        /// <summary>
        /// The network of the station proof tests, the charging station operator DE*GEF with the given signing keys.
        /// </summary>
        private static RoamingNetwork Network(params POIPublicKey[] SigningKeys)
        {

            var json  = StationProofTests.NewNetwork().ToJSON(IncludeETags: false);
            var gef   = json[RoamingNetwork.PropertyKeys.ChargingStationOperators]!.Children<JObject>().Single(cso => cso["@id"]!.Value<String>() == GEF.ToString());

            gef[POIPartyKeys.SigningKeysProperty] = new JArray(SigningKeys.Select(key => key.ToJSON(Embedded: true)));

            return RoamingNetwork.TryParse(json, out var network, out var error)
                       ? network
                       : throw new ArgumentException(error);

        }

        #endregion


        #region Usages_are_part_of_a_signing_key()

        /// <summary>
        /// Usages are written in their ordinal order, read back, and are no part of the @id; unknown usages, usages written
        /// twice or out of order, and usages of identity keys are refused.
        /// </summary>
        [Test]
        public void Usages_are_part_of_a_signing_key()
        {

            var (privateKey, unrestricted)  = Key("all");
            var restricted                  = POIPublicKey.From(privateKey, Usages: [ POIKeyUsages.Tariffs, POIKeyUsages.Infrastructure ]);
            var json                        = restricted.ToJSON(Embedded: true);

            JObject With(JArray Usages)
            {
                var copy = (JObject) json.DeepClone();
                copy[POIPublicKey.PropertyKeys.Usages] = Usages;
                return copy;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(restricted.Id,                                                   Is.EqualTo(unrestricted.Id), "no part of the @id");
                Assert.That(json[POIPublicKey.PropertyKeys.Usages]!.ToObject<String[]>(),   Is.EqualTo(new[] { "infrastructure", "tariffs" }));
                Assert.That(unrestricted.ToJSON(Embedded: true).ContainsKey("usages"),       Is.False, "written unless none");
                Assert.That(POIPublicKey.TryParse(json, out var read, out var error),        Is.True, error);
                Assert.That(read,                                                            Is.EqualTo(restricted));

                Assert.That(restricted.  MaySign(POIKeyUsages.Tariffs),                      Is.True);
                Assert.That(restricted.  MaySign(POIKeyUsages.Party),                        Is.False);
                Assert.That(unrestricted.MaySign(POIKeyUsages.Party),                        Is.True, "a key naming no usages signs everything");

                Assert.That(POIPublicKey.TryParse(With([ "everything" ]),                 out _, out error),  Is.False);
                Assert.That(error,  Does.Contain("is unknown"));
                Assert.That(POIPublicKey.TryParse(With([ "tariffs", "tariffs" ]),         out _, out error),  Is.False);
                Assert.That(POIPublicKey.TryParse(With([ "tariffs", "infrastructure" ]),  out _, out error),  Is.False);
                Assert.That(error,  Does.Contain("ordinal order"));
                Assert.That(POIPublicKey.TryParse(With([]),                               out _, out error),  Is.False);

                Assert.That(() => new ChargingStationOperator(GEF,
                                                              SigningKeys:   [ unrestricted ],
                                                              IdentityKeys:  [ POIPublicKey.From(TestParties.Key(TestParties.Party(GEF), POIKeyKind.Identity).Private, Usages: [ POIKeyUsages.Party ]) ]),
                            Throws.ArgumentException.With.Message.Contains("names no usages"));

            }

        }

        #endregion

        #region Pools_and_tariffs_by_keys_of_their_own()

        /// <summary>
        /// A charging station operator signing its pools with one key and its tariffs with another: each key signs what it
        /// may, the other one is refused, and its own properties need a key for the party.
        /// </summary>
        [Test]
        public void Pools_and_tariffs_by_keys_of_their_own()
        {

            var infrastructure  = Key("infrastructure", POIKeyUsages.Infrastructure);
            var tariffs         = Key("tariffs",        POIKeyUsages.Tariffs);
            var network         = Network(infrastructure.Public, tariffs.Public);
            var gef             = network.GetChargingStationOperatorById(GEF)!;

            RoamingNetworkChangeSet SignedBy((ECPrivateKeyParameters Private, POIPublicKey Public) Key, RoamingNetworkChange Change)

                => network.CreateChangeSet([ Change ], CreatedAt: T0).Sign(Key.Private, Key.Public.Id, COSEAlgorithm.ES256);

            var pool    = RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P9"), Created: T0));
            var tariff  = RoamingNetworkChange.Add(new ChargingTariff(ChargingTariff_Id.Parse("DE*GEF*T9"),
                                                                      Currency.EUR,
                                                                      [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                      Created: T0));
            var name    = RoamingNetworkChange.UpdateProperty(gef, "name", null, new JObject(new JProperty("en", "Renamed")));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network.TryApplyChangeSet(SignedBy(tariffs,        pool),    out _, out var error1),  Is.False, "a pool by the key of the tariffs");
                Assert.That(error1,  Does.Contain("that may sign 'infrastructure'"));
                Assert.That(network.TryApplyChangeSet(SignedBy(infrastructure, pool),    out _, out var error2),  Is.True,  error2);

                Assert.That(network.TryApplyChangeSet(SignedBy(infrastructure, tariff),  out _, out var error3),  Is.False, "a tariff by the key of the pools");
                Assert.That(error3,  Does.Contain("that may sign 'tariffs'"));
                Assert.That(network.TryApplyChangeSet(SignedBy(tariffs,        tariff),  out _, out var error4),  Is.True,  error4);

                Assert.That(network.TryApplyChangeSet(SignedBy(infrastructure, name),    out _, out var error5),  Is.False, "its name by neither of them");
                Assert.That(error5,  Does.Contain("that may sign 'party'"));
                Assert.That(network.TryApplyChangeSet(SignedBy(tariffs,        name),    out _, out _),           Is.False);

            }

        }

        #endregion

        #region A_key_of_tariffs_does_not_replace_the_keys()

        /// <summary>
        /// Signing keys are rotated by a signing key for the party: a key restricted to tariffs can not give itself more.
        /// </summary>
        [Test]
        public void A_key_of_tariffs_does_not_replace_the_keys()
        {

            var tariffs   = Key("tariffs", POIKeyUsages.Tariffs);
            var party     = Key("party",   POIKeyUsages.Party);
            var network   = Network(tariffs.Public, party.Public);
            var gef       = network.GetChargingStationOperatorById(GEF)!;
            var unlimited = Key("unlimited");

            var rotate    = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.SigningKeysProperty, null, new JArray(unlimited.Public.ToJSON(Embedded: true))) ], CreatedAt: T0);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(network.TryApplyChangeSet(rotate.Sign(tariffs.Private, tariffs.Public.Id, COSEAlgorithm.ES256), out _, out var error1),  Is.False);
                Assert.That(error1,  Does.Contain("that may sign 'party'"));
                Assert.That(network.TryApplyChangeSet(rotate.Sign(party.  Private, party.  Public.Id, COSEAlgorithm.ES256), out var rotated, out var error2),  Is.True, error2);
                Assert.That(rotated!.GetChargingStationOperatorById(GEF)!.SigningKeys,  Is.EqualTo(new[] { unlimited.Public }));
            }

        }

        #endregion

        #region An_attestation_needs_a_key_for_the_party()

        /// <summary>
        /// An app counts an attestation of the roaming network operator only by a certified signing key that may sign for
        /// the party: one restricted to infrastructure does not attest the network.
        /// </summary>
        [Test]
        public void An_attestation_needs_a_key_for_the_party()
        {

            var now = Timestamp.Now;

            Boolean Verifies(String Usage, out String? Error)
            {

                var network     = StationProofTests.NewNetwork();
                var party       = TestParties.Party(network);
                var (signing, _)   = TestParties.Key(party, POIKeyKind.Signing);
                var identity    = TestParties.Key(party, POIKeyKind.Identity);
                var signingKey  = POIPublicKey.From(signing, Usages: [ Usage ]);

                var json        = network.ToJSON(IncludeETags: false);
                var op          = (JObject) json[RoamingNetwork.PropertyKeys.Operator]!;

                op.Remove(POIPartyKeys.SigningKeysProperty);
                op[RoamingNetworkSigningKeyCertificate.JSONProperty] = new JArray(RoamingNetworkSigningKeyCertificate.Create(identity.Private, COSEAlgorithm.ES512, network.Operator!.Id, signingKey, now.AddMinutes(-1)).ToJSON(Embedded: true));
                op[RoamingNetworkKeyStatement.JSONProperty]          = RoamingNetworkKeyStatement.Create(identity.Private, COSEAlgorithm.ES512, 1, network.Id, network.Operator!.Id, [ identity.Public ], now.AddMinutes(-1), now.AddDays(30)).ToJSON(Embedded: true);

                Assert.That(RoamingNetwork.TryParse(json, out var withKeys, out var parseError), Is.True, parseError);

                var history = RoamingNetworkHistory.Start(withKeys!);
                history.AttestationSigners = [ new RoamingNetworkCommitSigner(signing, signingKey.Id, COSEAlgorithm.ES256, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator) ];

                Assert.That(history.TryAttest(out _, out var attestError, now), Is.True, attestError);
                Assert.That(history.TryGetChargingStationProof(ChargingStation_Id.Parse("DE*GEF*S1"), out var proof, out var proofError, At: now.AddMinutes(1)), Is.True, proofError);

                return proof!.Verify([ identity.Public.Id ], out _, out Error, now.AddMinutes(1));

            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Verifies(POIKeyUsages.Party,          out var error1),  Is.True,  error1);
                Assert.That(Verifies(POIKeyUsages.Infrastructure, out var error2),  Is.False, "a key for the infrastructure only");
            }

        }

        #endregion

        #region A_price_statement_needs_a_key_for_price_statements()

        /// <summary>
        /// A price statement is verified only with a signing key that may sign price statements.
        /// </summary>
        [Test]
        public void A_price_statement_needs_a_key_for_price_statements()
        {

            var tariffs  = Key("tariffs", POIKeyUsages.Tariffs);
            var prices   = Key("prices",  POIKeyUsages.PriceStatements);
            var status   = new ChargingPrices([ new ChargingTariffPrices(ChargingTariff_Id.Parse("DE*GEF*T1"),
                                                                         [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ]) ]);

            RoamingNetworkPriceStatement SignedBy((ECPrivateKeyParameters Private, POIPublicKey Public) Key)

                => RoamingNetworkPriceStatement.Create(Key.Private, Key.Public.Id, COSEAlgorithm.ES256, RoamingNetwork_Id.Parse("rn1"),
                                                       nameof(EVSE), "DE*GEF*E1*1", status, 0, null, T0, T0, T0.AddMinutes(15));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(SignedBy(tariffs).TryVerify([ tariffs.Public, prices.Public ], out var error1),  Is.False);
                Assert.That(error1,  Does.Contain("may not sign 'priceStatements'"));
                Assert.That(SignedBy(prices). TryVerify([ tariffs.Public, prices.Public ], out var error2),  Is.True, error2);
            }

        }

        #endregion

    }

}
