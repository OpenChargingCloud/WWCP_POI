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

using System.Net;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// EV Taler in the content (concepts/EVTaler.md, section 4): the payment option EVTaler of charging pools, the
    /// merchant of a charging station operator at a GNU Taler merchant backend with the exchanges it accepts, signed by a
    /// key that may sign "payments", and the EV Taler tariff and exchanges of an EVSE.
    /// </summary>
    [TestFixture]
    public class EVTalerTests
    {

        #region Data

        private static readonly DateTimeOffset              Created      = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        private static readonly DateTimeOffset              T0           = DateTimeOffset.Parse("2026-10-29T12:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF          = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1           = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingPool_Id             P2           = ChargingPool_Id.           Parse("DE*GEF*P2");
        private static readonly ChargingStation_Id          S1           = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly ChargingStation_Id          S2           = ChargingStation_Id.        Parse("DE*GEF*S2");
        private static readonly EVSE_Id                     E1           = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2           = EVSE_Id.                   Parse("DE*GEF*E2*1");
        private static readonly ChargingTariff_Id           AdHoc        = ChargingTariff_Id.         Parse("DE*GEF*T_ADHOC");
        private static readonly ChargingTariff_Id           Regular      = ChargingTariff_Id.         Parse("DE*GEF*T_REGULAR");
        private static readonly ChargingPoolTemplate_Id     EVTalerPool  = ChargingPoolTemplate_Id.   Parse("DE*GEF*PTEVTALER");

        /// <summary>
        /// The master public key of the exchange exchange.demo.taler.net, as it served it: a key as GNU Taler writes it.
        /// </summary>
        private const   String  EURExchangeKey  = "F80MFRG8HVH6R9CQ47KRFQSJP3T6DBJ4K1D9B703RJY3Z39TBMJ0";

        /// <summary>
        /// Keys made for the tests: the SHA-256 of a text, written as GNU Taler writes a key.
        /// </summary>
        private const   String  CHFExchangeKey  = "YJ7H5JVNVM5SG5FAE3BGQ35AEPHXBH27MWNRPFQ0SVW9BH0BN9B0";
        private const   String  MerchantKey     = "ESMBKNR5CMZPAYH9AD0QNKCPYSJSE0EN5MH4FMPKN683R63HMD3G";

        #endregion

        #region (static) Merchant()

        /// <summary>
        /// The merchant of DE*GEF, accepting an exchange in euros and one in Swiss francs.
        /// </summary>
        private static EVTalerMerchant Merchant()

            => new (URL.Parse("https://taler.gef.example/instances/de-gef/"),
                    MerchantKey,
                    [ new EVTalerExchange(CHFExchangeKey, URL.Parse("https://exchange.example.ch/"), Currency.CHF, new ImmutableI18NString([ new I18NPair(Languages.en, "Example Franc Exchange") ])),
                      new EVTalerExchange(EURExchangeKey, URL.Parse("https://exchange.example.eu/"), Currency.EUR, new ImmutableI18NString([ new I18NPair(Languages.en, "Example Exchange"), new I18NPair(Languages.de, "Beispiel-Exchange") ])) ]);

        #endregion

        #region (static) NewNetwork(Merchant, AdHocPaymentMeans)

        /// <summary>
        /// DE*GEF with the given merchant: the pool P1 takes the payment option EVTaler from its template (without a merchant,
        /// there is no template), its EVSE E1 names an ad hoc tariff in euros with the given payment means and a regular one;
        /// the pool P2 names no EVTaler.
        /// </summary>
        private static RoamingNetwork NewNetwork(EVTalerMerchant?  Merchant,
                                                 PaymentMeans[]?   AdHocPaymentMeans   = null)

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:                Created,
                                                                             EVTaler:                Merchant,
                                                                             ChargingPoolTemplates:  Merchant is not null
                                                                                                         ? [ new ChargingPoolTemplate(EVTalerPool,
                                                                                                                                      PaymentOptions:  [ PaymentOptions.EVTaler, PaymentOptions.Direct ],
                                                                                                                                      Created:         Created) ]
                                                                                                         : [],
                                                                             ChargingTariffs:        [ new ChargingTariff(AdHoc,
                                                                                                                          Currency.EUR,
                                                                                                                          [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.65m, StepSize: 1) ]) ],
                                                                                                                          TariffType:    ChargingTariffType.AD_HOC_PAYMENT,
                                                                                                                          PaymentMeans:  AdHocPaymentMeans,
                                                                                                                          Created:       Created),
                                                                                                       new ChargingTariff(Regular,
                                                                                                                          Currency.EUR,
                                                                                                                          [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.49m, StepSize: 1) ]) ],
                                                                                                                          TariffType:    ChargingTariffType.REGULAR,
                                                                                                                          Created:       Created) ],
                                                                             ChargingPools:          [ new ChargingPool(P1,
                                                                                                                        TemplateIds:       Merchant is not null ? [ EVTalerPool ] : [],
                                                                                                                        Created:           Created,
                                                                                                                        ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                                 Created:  Created,
                                                                                                                                                                 EVSEs:    [ new EVSE(E1,
                                                                                                                                                                                      ChargingTariffIds:  [ AdHoc, Regular ],
                                                                                                                                                                                      Created:            Created) ]) ]),
                                                                                                       new ChargingPool(P2,
                                                                                                                        PaymentOptions:    [ PaymentOptions.Contract ],
                                                                                                                        Created:           Created,
                                                                                                                        ChargingStations:  [ new ChargingStation(S2,
                                                                                                                                                                 Created:  Created,
                                                                                                                                                                 EVSEs:    [ new EVSE(E2,
                                                                                                                                                                                      ChargingTariffIds:  [ AdHoc ],
                                                                                                                                                                                      Created:            Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region (static) Operator(Network) / OperatorJSON(Network)

        private static ChargingStationOperator Operator(RoamingNetwork Network)
            => Network.GetChargingStationOperatorById(GEF)!;

        private static JObject OperatorJSON(JObject Network)
            => (JObject) Network["chargingStationOperators"]![0]!;

        #endregion


        #region Payment_options_are_an_open_set()

        /// <summary>
        /// The payment options are an open set of names: the predefined ones, EVTaler among them, and any other nonempty
        /// text, in their ordinal order; none twice, whatever its case, and none empty. A version giving them in another
        /// order is refused: it would be written in theirs.
        /// </summary>
        [Test]
        public void Payment_options_are_an_open_set()
        {

            var json  = NewNetwork(Merchant()).ToJSON();
            var pool  = (JObject) OperatorJSON(json)["chargingPools"]![1]!;

            pool["paymentOptions"] = new JArray("Bitcoin", "Contract", "SMS");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(PaymentOptions.Parse("EVTaler"),                                         Is.EqualTo(PaymentOptions.EVTaler));
                Assert.That(PaymentOptions.TryParse(" ", out _),                                     Is.False);

                Assert.That(RoamingNetwork.TryParse(json, out var network, out var error),           Is.True, error);
                Assert.That(network!.GetChargingStationOperatorById(GEF)!.ChargingPools.Single(chargingPool => chargingPool.Id == P2).PaymentOptions.Select(option => option.ToString()),
                            Is.EqualTo(new[] { "Bitcoin", "Contract", "SMS" }));

                pool["paymentOptions"] = new JArray("SMS", "Bitcoin", "Contract");
                Assert.That(RoamingNetwork.TryParse(json, out _, out error),                         Is.False);
                Assert.That(error,                                                                   Is.EqualTo("'chargingStationOperators[0].chargingPools[1].paymentOptions' is not written as the content holds it: its elements are in the order the content holds them in!"));

                pool["paymentOptions"] = new JArray("Cash", "cash");
                Assert.That(RoamingNetwork.TryParse(json, out _, out error),                         Is.False);
                Assert.That(error,                                                                   Does.Contain("must not be given twice"));

                pool["paymentOptions"] = new JArray(" ");
                Assert.That(RoamingNetwork.TryParse(json, out _, out error),                         Is.False);
                Assert.That(error,                                                                   Does.Contain("payment option ' '"));

            }

        }

        #endregion

        #region A_merchant_as_the_content_holds_it()

        /// <summary>
        /// A merchant and its exchanges name nothing else than what they have, their base URLs and currencies as they write
        /// them, their exchanges in the ordinal order of their master public keys: given otherwise, the version is refused,
        /// as written again it would be another text than the one hashed (SPECIFICATION.md, section 2, Written forms).
        /// </summary>
        [Test]
        public void A_merchant_as_the_content_holds_it()
        {

            String? Refusal(Action<JObject, JObject> Change)
            {
                var json      = NewNetwork(Merchant()).ToJSON(IncludeETags: false);
                var merchant  = (JObject) OperatorJSON(json)["evTaler"]!;
                Change(merchant, (JObject) merchant["exchanges"]![0]!);
                return RoamingNetwork.TryParse(json, out _, out var error) ? null : error;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Refusal((_, _) => { }),                                                     Is.Null);
                Assert.That(Refusal((merchant, _) => merchant["x"] = 1),                                Does.Contain("The EV Taler merchant must not name the property 'x'!"));
                Assert.That(Refusal((_, exchange) => exchange["x"] = 1),                                Does.Contain($"Invalid exchange of an EV Taler merchant: The exchange '{EURExchangeKey}' must not name the property 'x'!"));
                Assert.That(Refusal((merchant, _) => merchant["merchantBaseURL"] = "taler.gef.example/instances/de-gef/"),
                            Does.Contain("The merchant base URL 'taler.gef.example/instances/de-gef/' is written as 'https://taler.gef.example/instances/de-gef/'!"));
                Assert.That(Refusal((_, exchange) => exchange["currency"] = " EUR"),                    Does.Contain($"The currency ' EUR' of the exchange '{EURExchangeKey}' is written as 'EUR'!"));
                Assert.That(Refusal((merchant, _) => merchant["exchanges"] = new JArray(((JArray) merchant["exchanges"]!).Reverse())),
                            Is.EqualTo("'chargingStationOperators[0].evTaler.exchanges' is not written as the content holds it: its elements are in the order the content holds them in!"));

            }

        }

        #endregion

        #region An_operator_names_its_EV_Taler_merchant()

        /// <summary>
        /// The merchant of an operator, as JSON and CBOR: its exchanges in the ordinal order of their master public keys,
        /// read back as written; what drivers see as "EV Taler (provider)".
        /// </summary>
        [Test]
        public void An_operator_names_its_EV_Taler_merchant()
        {

            var network   = NewNetwork(Merchant());
            var gef       = Operator(network);
            var json      = gef.ToJSON(Embedded: true)["evTaler"] as JObject;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json,                                                                    Is.Not.Null);
                Assert.That(JToken.DeepEquals(json, JObject.Parse($$"""
                                {
                                  "merchantBaseURL":   "https://taler.gef.example/instances/de-gef/",
                                  "merchantPublicKey": "{{MerchantKey}}",
                                  "exchanges": [
                                    { "masterPublicKey": "{{EURExchangeKey}}", "baseURL": "https://exchange.example.eu/", "currency": "EUR",
                                      "name": { "en": "Example Exchange", "de": "Beispiel-Exchange" } },
                                    { "masterPublicKey": "{{CHFExchangeKey}}", "baseURL": "https://exchange.example.ch/", "currency": "CHF",
                                      "name": { "en": "Example Franc Exchange" } }
                                  ]
                                }
                                """)),                                                               Is.True, json?.ToString());

                Assert.That(gef.EVTaler!.ToJSON().Value<String>("@context"),                         Is.EqualTo(EVTalerMerchant.JSONLDContext));

                Assert.That(RoamingNetwork.TryParse(network.ToJSON(), out var fromJSON, out var error), Is.True, error);
                Assert.That(JToken.DeepEquals(Operator(fromJSON!).EVTaler!.ToJSON(), gef.EVTaler.ToJSON()), Is.True);

                Assert.That(ChargingStationOperator.TryParse(gef.ToCBOR(), out var fromCBOR, out error),    Is.True, error);
                Assert.That(JToken.DeepEquals(fromCBOR!.EVTaler!.ToJSON(), gef.EVTaler.ToJSON()),     Is.True);

                Assert.That(gef.EVTaler.Exchanges.Select(exchange => exchange.MasterPublicKey),      Is.EqualTo(new[] { EURExchangeKey, CHFExchangeKey }));
                Assert.That(gef.EVTaler.ExchangesFor(Currency.CHF).Single().MasterPublicKey,         Is.EqualTo(CHFExchangeKey));
                Assert.That(gef.EVTaler.Exchanges[0].PaymentOptionName[Languages.de],                Is.EqualTo("EV Taler (Beispiel-Exchange, EUR)"));
                Assert.That(gef.EVTaler.Exchanges[0].PaymentOptionName[Languages.en],                Is.EqualTo("EV Taler (Example Exchange, EUR)"));

            }

        }

        #endregion

        #region Invalid_EV_Taler_merchants_are_refused()

        /// <summary>
        /// A merchant needs HTTPS base URLs ending in '/', keys as GNU Taler writes them, at least one exchange, each once,
        /// with an ISO 4217 currency and a name.
        /// </summary>
        [Test]
        public void Invalid_EV_Taler_merchants_are_refused()
        {

            var json = NewNetwork(Merchant()).ToJSON();

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change((JObject) OperatorJSON(changed)["evTaler"]!);
                return changed;
            }

            static JObject Exchange(JObject Merchant)
                => (JObject) Merchant["exchanges"]![0]!;

            var cases = new (String Name, JObject JSON, String Error)[] {
                ("merchant over HTTP",        Changed(merchant => merchant["merchantBaseURL"]   = "http://taler.gef.example/instances/de-gef/"),          "must be an HTTPS URL"),
                ("merchant without '/'",      Changed(merchant => merchant["merchantBaseURL"]   = "https://taler.gef.example/instances/de-gef"),          "must end with '/'"),
                ("merchant with a query",     Changed(merchant => merchant["merchantBaseURL"]   = "https://taler.gef.example/instances/de-gef/?a=1"),     "neither a query nor a fragment"),
                ("merchant with a user",      Changed(merchant => merchant["merchantBaseURL"]   = "https://gef:secret@taler.gef.example/"),               "must not name a user"),
                ("no merchant base URL",      Changed(merchant => merchant.Remove("merchantBaseURL")),                                                    "needs its merchant base URL"),
                ("key in lower case",         Changed(merchant => merchant["merchantPublicKey"] = MerchantKey.ToLowerInvariant()),                        "merchant public key"),
                ("key too short",             Changed(merchant => merchant["merchantPublicKey"] = MerchantKey[1..]),                                      "merchant public key"),
                ("key with a U",              Changed(merchant => merchant["merchantPublicKey"] = "U" + MerchantKey[1..]),                                "merchant public key"),
                ("key with bits after it",    Changed(merchant => merchant["merchantPublicKey"] = MerchantKey[..^1] + "H"),                               "merchant public key"),
                ("no exchanges",              Changed(merchant => merchant["exchanges"]         = new JArray()),                                          "at least one exchange"),
                ("exchanges not an array",    Changed(merchant => merchant["exchanges"]         = "all"),                                                 "must be a JSON array"),
                ("exchange twice",            Changed(merchant => ((JArray) merchant["exchanges"]!).Add(Exchange(merchant).DeepClone())),                 "must not be given twice"),
                ("exchange over HTTP",        Changed(merchant => Exchange(merchant)["baseURL"]          = "http://exchange.example.eu/"),                "must be an HTTPS URL"),
                ("exchange without key",      Changed(merchant => Exchange(merchant).Remove("masterPublicKey")),                                          "needs its master public key"),
                ("unknown currency",          Changed(merchant => Exchange(merchant)["currency"]         = "XXX"),                                        "ISO 4217 code"),
                ("exchange without name",     Changed(merchant => Exchange(merchant).Remove("name")),                                                     "needs its name"),
                ("wrong context",             Changed(merchant => merchant["@context"]          = "https://example.org/merchant"),                        "JSON-LD context of an EV Taler merchant")
            };

            using (Assert.EnterMultipleScope())
            {
                foreach (var (name, invalid, error) in cases)
                {
                    Assert.That(RoamingNetwork.TryParse(invalid, out _, out var errorResponse),  Is.False, name);
                    Assert.That(errorResponse,                                                   Does.Contain(error), name);
                }
            }

        }

        #endregion

        #region The_payment_option_EVTaler_needs_the_merchant()

        /// <summary>
        /// A charging pool or charging pool template naming EVTaler needs its operator's merchant: checked when the operator
        /// is made, and by a change set after its last change, so that it may add both in either order.
        /// </summary>
        [Test]
        public void The_payment_option_EVTaler_needs_the_merchant()
        {

            var withoutMerchant  = NewNetwork(null);
            var withMerchant     = NewNetwork(Merchant());
            var gef              = Operator(withoutMerchant);
            var pool             = gef.ChargingPools.Single(chargingPool => chargingPool.Id == P2);

            var addOption        = RoamingNetworkChange.UpdateProperty(pool, "paymentOptions", new JArray("Contract"), new JArray("Contract", "EVTaler"));
            var addMerchant      = RoamingNetworkChange.UpdateProperty(gef,  "evTaler",        null,                   Merchant().ToJSON(Embedded: true));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(() => new ChargingStationOperator(GEF, ChargingPools: [ new ChargingPool(P1, PaymentOptions: [ PaymentOptions.EVTaler ]) ]),
                            Throws.ArgumentException.With.Message.Contains("names the payment option 'EVTaler', but its operator 'DE*GEF' has no EV Taler merchant"));

                Assert.That(() => new ChargingStationOperator(GEF, ChargingPoolTemplates: [ new ChargingPoolTemplate(EVTalerPool, PaymentOptions: [ PaymentOptions.EVTaler ]) ]),
                            Throws.ArgumentException.With.Message.Contains("charging pool template 'DE*GEF*PTEVTALER' names the payment option 'EVTaler'"));

                Assert.That(() => withoutMerchant.CreateSignedChangeSet([ addOption ], CreatedAt: T0),
                            Throws.ArgumentException.With.Message.Contains("has no EV Taler merchant"), "the option alone");

                Assert.That(withoutMerchant.TryApplyChangeSet(withoutMerchant.CreateSignedChangeSet([ addOption, addMerchant ], CreatedAt: T0), out var both, out var error2), Is.True, error2);
                Assert.That(Operator(both!).EVTaler!.MerchantPublicKey,                                                                               Is.EqualTo(MerchantKey));
                Assert.That(withoutMerchant.TryApplyChangeSet(withoutMerchant.CreateSignedChangeSet([ addMerchant, addOption ], CreatedAt: T0), out _, out var error3),   Is.True, error3);

                // The merchant of a pool naming EVTaler itself: its charging pools are kept as they are by a change of the
                // operator's own property, and checked all the same.
                Assert.That(() => both!.CreateSignedChangeSet([ RoamingNetworkChange.RemoveProperty(Operator(both!), "evTaler") ], CreatedAt: T0),
                            Throws.ArgumentException.With.Message.Contains("charging pool 'DE*GEF*P2' names the payment option 'EVTaler'"), "the merchant of a pool");

                var removeMerchant = RoamingNetworkChange.RemoveProperty(Operator(withMerchant), "evTaler");

                Assert.That(() => withMerchant.CreateSignedChangeSet([ removeMerchant ], CreatedAt: T0),
                            Throws.ArgumentException.With.Message.Contains("charging pool template 'DE*GEF*PTEVTALER' names the payment option 'EVTaler'"), "the merchant of the template's pools");

            }

        }

        #endregion

        #region The_merchant_is_signed_by_a_key_for_payments()

        /// <summary>
        /// Whoever changes the merchant redirects payments: a change of it needs a signing key that may sign "payments",
        /// which signs nothing else of the operator; its other properties still need one for the party.
        /// </summary>
        [Test]
        public void The_merchant_is_signed_by_a_key_for_payments()
        {

            var payments  = Key("payments", POIKeyUsages.Payments);
            var party     = Key("party",    POIKeyUsages.Party);
            var json      = NewNetwork(Merchant()).ToJSON(IncludeETags: false);

            OperatorJSON(json)[POIPartyKeys.SigningKeysProperty] = new JArray(new[] { payments.Public, party.Public }.OrderBy(key => key.Id, StringComparer.Ordinal).Select(key => key.ToJSON(Embedded: true)));

            Assert.That(RoamingNetwork.TryParse(json, out var network, out var parseError), Is.True, parseError);

            var gef       = Operator(network!);
            var merchant  = RoamingNetworkChange.UpdateProperty(gef, "evTaler", gef.EVTaler!.ToJSON(Embedded: true),
                                                                new EVTalerMerchant(URL.Parse("https://taler.gef.example/instances/gef2/"), MerchantKey, gef.EVTaler.Exchanges).ToJSON(Embedded: true));
            var name      = RoamingNetworkChange.UpdateProperty(gef, "name", null, new JObject(new JProperty("en", "Renamed")));

            RoamingNetworkChangeSet SignedBy((ECPrivateKeyParameters Private, POIPublicKey Public) Key, RoamingNetworkChange Change)

                => network!.CreateChangeSet([ Change ], CreatedAt: T0).Sign(Key.Private, Key.Public.Id, COSEAlgorithm.ES256);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(network!.TryApplyChangeSet(SignedBy(party,    merchant), out _,          out var error1),  Is.False, "the merchant by the key of the party");
                Assert.That(error1,                                                                                   Does.Contain("that may sign 'payments'"));
                Assert.That(network. TryApplyChangeSet(SignedBy(payments, merchant), out var result, out var error2),  Is.True,  error2);
                Assert.That(Operator(result!).EVTaler!.MerchantBaseURL.ToString(),                     Is.EqualTo("https://taler.gef.example/instances/gef2/"));

                Assert.That(network. TryApplyChangeSet(SignedBy(payments, name),     out _,          out var error3),  Is.False, "the name by the key of the payments");
                Assert.That(error3,                                                                                   Does.Contain("that may sign 'party'"));
                Assert.That(network. TryApplyChangeSet(SignedBy(party,    name),     out _,          out var error4),  Is.True,  error4);

            }

        }

        #endregion

        #region The_EV_Taler_tariff_and_exchanges_of_an_EVSE()

        /// <summary>
        /// The EV Taler tariff of an EVSE is its ad hoc tariff, else the one without a type, where its pool names EVTaler and
        /// the tariff names no payment means or EV_TALER; its exchanges are those of the merchant in the tariff's currency.
        /// </summary>
        [Test]
        public void The_EV_Taler_tariff_and_exchanges_of_an_EVSE()
        {

            var gef              = Operator(NewNetwork(Merchant()));
            var withEVTalerMeans = Operator(NewNetwork(Merchant(), [ PaymentMeans.CREDIT_CARD, PaymentMeans.EV_TALER ]));
            var cardsOnly        = Operator(NewNetwork(Merchant(), [ PaymentMeans.CREDIT_CARD ]));
            var noMerchant       = Operator(NewNetwork(null));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(gef.TryGetEVTalerTariff(E1, out var tariff),                                        Is.True);
                Assert.That(tariff!.Id,                                                                          Is.EqualTo(AdHoc), "the ad hoc tariff, not the regular one");
                Assert.That(gef.EVTalerExchangesAt(E1).Select(exchange => exchange.MasterPublicKey),            Is.EqualTo(new[] { EURExchangeKey }), "only the exchange in euros");
                Assert.That(gef.EVTalerExchangesAt(E1).Single().PaymentOptionName[Languages.en],               Is.EqualTo("EV Taler (Example Exchange, EUR)"));

                Assert.That(gef.TryGetEVTalerTariff(E2, out _),                                                 Is.False, "its pool names no EVTaler");
                Assert.That(gef.EVTalerExchangesAt(E2),                                                         Is.Empty);
                Assert.That(gef.TryGetEVTalerTariff(EVSE_Id.Parse("DE*GEF*E9*1"), out _),                        Is.False, "no EVSE of the operator");

                Assert.That(withEVTalerMeans.TryGetEVTalerTariff(E1, out _),                                    Is.True,  "its payment means name EV_TALER");
                Assert.That(cardsOnly.       TryGetEVTalerTariff(E1, out _),                                    Is.False, "its payment means are cards only");
                Assert.That(noMerchant.      TryGetEVTalerTariff(E1, out _),                                    Is.False, "no merchant");

            }

        }

        #endregion

        #region A_station_proof_gives_the_merchant()

        /// <summary>
        /// The merchant is one of the operator's own properties, no child array: every proof of a charging station gives it
        /// completely, with the operator's keys.
        /// </summary>
        [Test]
        public void A_station_proof_gives_the_merchant()
        {

            var history  = RoamingNetworkHistory.Start(NewNetwork(Merchant()));

            Assert.That(history.TryGetChargingStationProof(S1, out var proof, out var error), Is.True, error);

            var gef      = proof!.Given("chargingStationOperators").Single();

            Assert.That(JToken.DeepEquals(gef["evTaler"], Merchant().ToJSON(Embedded: true)), Is.True, gef["evTaler"]?.ToString());

        }

        #endregion

        #region An_app_asks_how_to_pay_with_EV_Taler_over_HTTP()

        /// <summary>
        /// POI_API tells how charging at an EVSE or a charging pool can be paid with EV Taler, by the rules of the operator:
        /// the EV Taler tariff, the merchant and the exchanges in its currency, each with what drivers see; 404 where none.
        /// </summary>
        [Test]
        public async Task An_app_asks_how_to_pay_with_EV_Taler_over_HTTP()
        {

            var history                = RoamingNetworkHistory.Start(NewNetwork(Merchant()));

            await using var httpServer = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
            await using var poiAPI     = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(history);

            using var httpClient       = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/") };

            var ofEVSE                 = JObject.Parse(await httpClient.GetStringAsync("EVSEs/DE*GEF*E1*1/evTaler"));
            var ofFullPath             = JObject.Parse(await httpClient.GetStringAsync("CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*1/evTaler"));
            var ofPool                 = JObject.Parse(await httpClient.GetStringAsync("CSOs/DE*GEF/CPs/DE*GEF*P1/evTaler"));
            var noneAtPool             = await httpClient.GetAsync("CSOs/DE*GEF/CPs/DE*GEF*P2/evTaler");
            var noneAtEVSE             = await httpClient.GetAsync("EVSEs/DE*GEF*E2*1/evTaler");
            var unknownOperator        = await httpClient.GetAsync("EVSEs/DE*XYZ*E1*1/evTaler");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(ofEVSE.Value<String>("EVSEId"),                                      Is.EqualTo(E1.ToString()));
                Assert.That(ofEVSE.Value<String>("chargingTariffId"),                            Is.EqualTo(AdHoc.ToString()));
                Assert.That(ofEVSE.Value<String>("currency"),                                    Is.EqualTo("EUR"));
                Assert.That(ofEVSE.Value<String>("merchantBaseURL"),                             Is.EqualTo("https://taler.gef.example/instances/de-gef/"));
                Assert.That(ofEVSE.Value<String>("merchantPublicKey"),                           Is.EqualTo(MerchantKey));
                Assert.That(ofEVSE["exchanges"]!.Select(exchange => exchange.Value<String>("masterPublicKey")), Is.EqualTo(new[] { EURExchangeKey }), "only the exchange in euros");
                Assert.That(ofEVSE["exchanges"]![0]!["paymentOptionName"]!.Value<String>("en"),  Is.EqualTo("EV Taler (Example Exchange, EUR)"));
                Assert.That(JToken.DeepEquals(ofFullPath, ofEVSE),                               Is.True, "the same by the full path");

                Assert.That(ofPool.Value<String>("chargingPoolId"),                              Is.EqualTo(P1.ToString()));
                Assert.That(ofPool["EVSEs"]!.Select(evse => evse.Value<String>("EVSEId")),        Is.EqualTo(new[] { E1.ToString() }));
                Assert.That(ofPool["exchanges"]!.Select(exchange => exchange.Value<String>("masterPublicKey")), Is.EqualTo(new[] { EURExchangeKey }));

                Assert.That(noneAtPool.StatusCode,                                               Is.EqualTo(HttpStatusCode.NotFound), "its pool names no EVTaler");
                Assert.That(await noneAtPool.Content.ReadAsStringAsync(),                        Does.Contain("offers no EV Taler"));
                Assert.That(noneAtEVSE.StatusCode,                                               Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(unknownOperator.StatusCode,                                          Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

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

    }

}
