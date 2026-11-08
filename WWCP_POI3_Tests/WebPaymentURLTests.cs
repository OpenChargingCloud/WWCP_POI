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

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// The URL of the web payment page of a charging station operator, a charging pool and a charging station - not the
    /// URL in the dynamic QR code of a station - written and read, given by templates, changed by change sets and signed
    /// by a key that may sign "payments".
    /// </summary>
    [TestFixture]
    public class WebPaymentURLTests
    {

        #region Data

        private static readonly DateTimeOffset              Created     = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0          = DateTimeOffset.Parse("2026-11-08T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF         = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1          = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingPool_Id             P2          = ChargingPool_Id.           Parse("DE*GEF*P2");
        private static readonly ChargingStation_Id          S1          = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly ChargingStation_Id          S2          = ChargingStation_Id.        Parse("DE*GEF*S2");
        private static readonly ChargingPoolTemplate_Id     Sites       = ChargingPoolTemplate_Id.   Parse("DE*GEF*PT1");
        private static readonly ChargingStationTemplate_Id  Wallbox     = ChargingStationTemplate_Id.Parse("DE*GEF*STWALLBOX");

        private static readonly URL                         OfGEF       = URL.Parse("https://pay.gef.example/");
        private static readonly URL                         OfP1        = URL.Parse("https://pay.gef.example/pools/P1");
        private static readonly URL                         OfSites     = URL.Parse("https://pay.gef.example/sites");
        private static readonly URL                         OfS1        = URL.Parse("https://pay.gef.example/stations/S1");
        private static readonly URL                         OfWallbox   = URL.Parse("https://pay.gef.example/wallbox");

        #endregion

        #region NewNetwork()

        /// <summary>
        /// An operator with a web payment page; the first pool with one of its own and a station with one of its own, the
        /// second pool taking one from its template and its station one from its own template.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    GEF,
                                                    WebPaymentURL:             OfGEF,
                                                    Created:                   Created,
                                                    ChargingPools:             [ new ChargingPool(P1,
                                                                                                  WebPaymentURL:     OfP1,
                                                                                                  Created:           Created,
                                                                                                  ChargingStations:  [ new ChargingStation(S1, WebPaymentURL: OfS1, Created: Created) ]),
                                                                                 new ChargingPool(P2,
                                                                                                  TemplateIds:       [ Sites ],
                                                                                                  Created:           Created,
                                                                                                  ChargingStations:  [ new ChargingStation(S2, TemplateIds: [ Wallbox ], Created: Created) ]) ],
                                                    ChargingPoolTemplates:     [ new ChargingPoolTemplate   (Sites,   WebPaymentURL: OfSites,   Created: Created) ],
                                                    ChargingStationTemplates:  [ new ChargingStationTemplate(Wallbox, WebPaymentURL: OfWallbox, Created: Created) ]) ]).WithKeys();

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

        #region (static) GEFOf(Network)

        private static ChargingStationOperator GEFOf(RoamingNetwork Network)

            => Network.GetChargingStationOperatorById(GEF)!;

        #endregion


        #region A_web_payment_URL_is_written_and_read_in_JSON_and_CBOR()

        [Test]
        public void A_web_payment_URL_is_written_and_read_in_JSON_and_CBOR()
        {

            var rn1   = NewNetwork();
            var json  = rn1.ToJSON();

            Assert.That(RoamingNetwork.TryParse(json,         out var fromJSON, out var errorJSON),  Is.True, errorJSON);
            Assert.That(RoamingNetwork.TryParse(rn1.ToCBOR(), out var fromCBOR, out var errorCBOR),  Is.True, errorCBOR);

            var gef   = json["chargingStationOperators"]![0]!;
            var p1    = gef["chargingPools"]![0]!;
            var p2    = gef["chargingPools"]![1]!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(fromJSON!.ToJSON().ToString(),                                        Is.EqualTo(json.ToString()));
                Assert.That(fromCBOR!.ToJSON().ToString(),                                        Is.EqualTo(json.ToString()));
                Assert.That(GEFOf(fromCBOR).WebPaymentURL,                                        Is.EqualTo(OfGEF));

                Assert.That(gef["webPaymentURL"]?.Value<String>(),                                Is.EqualTo(OfGEF.    ToString()));
                Assert.That(p1 ["webPaymentURL"]?.Value<String>(),                                Is.EqualTo(OfP1.     ToString()));
                Assert.That(p1 ["chargingStations"]![0]!["webPaymentURL"]?.Value<String>(),       Is.EqualTo(OfS1.     ToString()));
                Assert.That(gef["chargingPoolTemplates"]!   [0]!["webPaymentURL"]?.Value<String>(), Is.EqualTo(OfSites.  ToString()));
                Assert.That(gef["chargingStationTemplates"]![0]!["webPaymentURL"]?.Value<String>(), Is.EqualTo(OfWallbox.ToString()));

                // Kept where it is given: not written into the members of a template.
                Assert.That(p2["webPaymentURL"],                                                  Is.Null);
                Assert.That(p2["chargingStations"]![0]!["webPaymentURL"],                         Is.Null);

            }

        }

        #endregion

        #region Members_take_the_web_payment_URL_of_their_templates_unless_they_have_their_own()

        [Test]
        public void Members_take_the_web_payment_URL_of_their_templates_unless_they_have_their_own()
        {

            var gef = GEFOf(NewNetwork());

            Assert.That(gef.TryGetEffectiveChargingPool(P1, out var effectiveP1),  Is.True);
            Assert.That(gef.TryGetEffectiveChargingPool(P2, out var effectiveP2),  Is.True);
            Assert.That(gef.TryGetChargingPool         (P2, out var p2),           Is.True);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(effectiveP1!.WebPaymentURL,                                      Is.EqualTo(OfP1));
                Assert.That(effectiveP1.ChargingStations.Single().WebPaymentURL,             Is.EqualTo(OfS1));
                Assert.That(effectiveP2!.WebPaymentURL,                                      Is.EqualTo(OfSites));
                Assert.That(effectiveP2.ChargingStations.Single().WebPaymentURL,             Is.EqualTo(OfWallbox));
                Assert.That(p2!.WebPaymentURL,                                               Is.Null);
                Assert.That(p2.ChargingStations.Single().WebPaymentURL,                      Is.Null);
            }

        }

        #endregion

        #region Change_sets_update_and_remove_a_web_payment_URL()

        [Test]
        public void Change_sets_update_and_remove_a_web_payment_URL()
        {

            var history = RoamingNetworkHistory.Start(NewNetwork());
            var rn1     = history.Head.RoamingNetwork;
            var gef1    = GEFOf(rn1);

            gef1.TryGetChargingPool   (P2, out var p2);
            gef1.TryGetChargingStation(S1, out var s1);

            var moved   = URL.Parse("https://payments.gef.example/");
            var ofP2    = URL.Parse("https://payments.gef.example/pools/P2");

            Assert.That(history.Commit(rn1.CreateSignedChangeSet([
                            RoamingNetworkChange.UpdateProperty(gef1, "webPaymentURL", OfGEF.ToString(), moved.ToString()),
                            RoamingNetworkChange.UpdateProperty(p2!,  "webPaymentURL", null,             ofP2. ToString()),
                            RoamingNetworkChange.RemoveProperty(s1!,  "webPaymentURL", OfS1. ToString())
                        ], CreatedAt: T0)).IsCommitted);

            var gef2 = GEFOf(history.Head.RoamingNetwork);

            gef2.TryGetEffectiveChargingPool(P1, out var effectiveP1);
            gef2.TryGetEffectiveChargingPool(P2, out var effectiveP2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(gef2.WebPaymentURL,                                              Is.EqualTo(moved));
                Assert.That(effectiveP2!.WebPaymentURL,                                      Is.EqualTo(ofP2));
                Assert.That(effectiveP1!.ChargingStations.Single().WebPaymentURL,            Is.Null);
                Assert.That(effectiveP1.WebPaymentURL,                                       Is.EqualTo(OfP1));
            }

        }

        #endregion

        #region A_web_payment_URL_is_signed_by_a_key_for_payments()

        /// <summary>
        /// Whoever names a web payment page redirects payments: a change of it - of the operator, a pool, a station or their
        /// templates - needs a signing key that may sign "payments"; adding a pool with one, its stations' included, a key
        /// that may sign "infrastructure" and "payments"; adding a pool without one, "infrastructure" as before.
        /// </summary>
        [Test]
        public void A_web_payment_URL_is_signed_by_a_key_for_payments()
        {

            var payments        = Key("payments",        POIKeyUsages.Payments);
            var infrastructure  = Key("infrastructure",  POIKeyUsages.Infrastructure);
            var party           = Key("party",           POIKeyUsages.Party);
            var both            = Key("both",            POIKeyUsages.Infrastructure, POIKeyUsages.Payments);
            var json            = NewNetwork().ToJSON(IncludeETags: false);

            json["chargingStationOperators"]![0]![POIPartyKeys.SigningKeysProperty] = new JArray(new[] { payments.Public, infrastructure.Public, party.Public, both.Public }.
                                                                                                    OrderBy(key => key.Id, StringComparer.Ordinal).
                                                                                                    Select (key => key.ToJSON(Embedded: true)));

            Assert.That(RoamingNetwork.TryParse(json, out var network, out var parseError), Is.True, parseError);

            var gef = GEFOf(network!);

            Assert.That(gef.TryGetChargingPool           (P2,      out var p2),       Is.True);
            Assert.That(gef.TryGetChargingStation        (S1,      out var s1),       Is.True);
            Assert.That(gef.TryGetChargingStationTemplate(Wallbox, out var wallbox),  Is.True);

            var ofOperator   = RoamingNetworkChange.UpdateProperty(gef,      "webPaymentURL", OfGEF.    ToString(), "https://payments.gef.example/");
            var ofPool       = RoamingNetworkChange.UpdateProperty(p2!,      "webPaymentURL", null,                 "https://payments.gef.example/pools/P2");
            var ofStation    = RoamingNetworkChange.RemoveProperty(s1!,      "webPaymentURL", OfS1.     ToString());
            var ofTemplate   = RoamingNetworkChange.UpdateProperty(wallbox!, "webPaymentURL", OfWallbox.ToString(), "https://payments.gef.example/wallbox");
            var name         = RoamingNetworkChange.UpdateProperty(gef,      "name",          null,                 new JObject(new JProperty("en", "Renamed")));

            // A new pool naming no web payment page, but one of its stations does.
            var withPage     = RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P3"),
                                                                         Created:           Created,
                                                                         ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S3"),
                                                                                                                  WebPaymentURL:  URL.Parse("https://pay.gef.example/stations/S3"),
                                                                                                                  Created:        Created) ]));

            var withoutPage  = RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P4"),
                                                                         Created:           Created,
                                                                         ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S4"), Created: Created) ]));

            (Boolean Applied, String? Error) SignedBy((ECPrivateKeyParameters Private, POIPublicKey Public) Key, RoamingNetworkChange Change)
            {
                var applied = network!.TryApplyChangeSet(network.CreateChangeSet([ Change ], CreatedAt: T0).Sign(Key.Private, Key.Public.Id, COSEAlgorithm.ES256),
                                                         out _,
                                                         out var error);
                return (applied, error);
            }

            using (Assert.EnterMultipleScope())
            {

                foreach (var (change, of, without) in new[] { (ofOperator, "of the operator", party),
                                                              (ofPool,     "of a pool",       infrastructure),
                                                              (ofStation,  "of a station",    infrastructure),
                                                              (ofTemplate, "of a template",   infrastructure) })
                {
                    var refused = SignedBy(without, change);
                    Assert.That(refused.Applied,                                             Is.False, $"{of} by a key that may not sign 'payments'");
                    Assert.That(refused.Error,                                               Does.Contain("that may sign 'payments'"), of);
                    Assert.That(SignedBy(payments, change).Applied,                          Is.True,  $"{of} by the key for payments");
                }

                var byInfrastructure = SignedBy(infrastructure, withPage);
                Assert.That(byInfrastructure.Applied,                                        Is.False, "a pool with a page by the key for infrastructure");
                Assert.That(byInfrastructure.Error,                                          Does.Contain("that may sign 'infrastructure' and 'payments'"));
                Assert.That(SignedBy(payments, withPage).Applied,                            Is.False, "a pool with a page by the key for payments");
                Assert.That(SignedBy(both,     withPage).Applied,                            Is.True,  "a pool with a page by the key for both");

                Assert.That(SignedBy(infrastructure, withoutPage).Applied,                   Is.True,  "a pool without a page by the key for infrastructure");
                Assert.That(SignedBy(payments,       withoutPage).Applied,                   Is.False, "a pool without a page by the key for payments");

                Assert.That(SignedBy(payments, name).Applied,                                Is.False, "the name of the operator by the key for payments");
                Assert.That(SignedBy(party,    name).Applied,                                Is.True,  "the name of the operator by the key of the party");

            }

        }

        #endregion

        #region A_web_payment_URL_that_is_no_text_is_refused()

        [Test]
        public void A_web_payment_URL_that_is_no_text_is_refused()
        {

            var json = new JObject(
                           new JProperty("@id",            P1.ToString()),
                           new JProperty("webPaymentURL",  42)
                       );

            using (Assert.EnterMultipleScope())
            {
                Assert.That(ChargingPool.TryParse(json, out var pool, out var error),        Is.False);
                Assert.That(pool,                                                            Is.Null);
                Assert.That(error,                                                           Does.Contain("web payment URL"));
            }

        }

        #endregion

    }

}
