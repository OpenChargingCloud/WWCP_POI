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
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// EV Taler, phase 2 (concepts/EVTaler.md, 5 and 7): the steps of a session in their contract terms, checked against a
    /// station proof, and the settlement statement, refused without both signed meter values.
    /// </summary>
    [TestFixture]
    public class EVTalerEvidenceTests
    {

        #region Data

        private static readonly DateTimeOffset              Created      = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private static readonly RoamingNetwork_Id           RN           = RoamingNetwork_Id.         Parse("rn1");
        private static readonly ChargingStationOperator_Id  GEF          = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1           = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1           = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1           = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly ChargingTariff_Id           AdHoc        = ChargingTariff_Id.         Parse("DE*GEF*T_ADHOC");

        private const   String  MeterSerial      = "M-1";
        private const   String  SessionId        = "kq3Vd0sY0f9hJ2bW8Qm1tA";
        private const   String  Encoding_        = "WWCP-TEST";

        private const   String  MerchantURL      = "https://taler.gef.example/instances/de-gef/";
        private const   String  MerchantKey      = "ESMBKNR5CMZPAYH9AD0QNKCPYSJSE0EN5MH4FMPKN683R63HMD3G";
        private const   String  EURExchangeKey   = "F80MFRG8HVH6R9CQ47KRFQSJP3T6DBJ4K1D9B703RJY3Z39TBMJ0";
        private const   String  CHFExchangeKey   = "YJ7H5JVNVM5SG5FAE3BGQ35AEPHXBH27MWNRPFQ0SVW9BH0BN9B0";

        private static readonly ECDsa  Meter        = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        private static readonly ECDsa  OtherMeter   = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        #endregion

        #region OneTimeTearDown()

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Meter.     Dispose();
            OtherMeter.Dispose();
        }

        #endregion

        #region (static) Now()

        /// <summary>
        /// Now, in whole seconds, as GNU Taler writes times: the history attests with its own clock.
        /// </summary>
        private static DateTimeOffset Now()
            => DateTimeOffset.FromUnixTimeSeconds(Timestamp.Now.ToUnixTimeSeconds());

        #endregion

        #region (private) Session

        private sealed record Session(RoamingNetworkHistory    History,
                                      VerifiedChargingStation  Station,
                                      DateTimeOffset           Now);

        #endregion


        #region (static) NewNetwork()

        /// <summary>
        /// DE*GEF accepting EV Taler at Jena Paradies: its merchant, an ad hoc tariff of a flat fee of 0.50 € and 0.65 €/kWh,
        /// and the energy meter of DE*GEF*E1*1 with its measurement key.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RN,
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:          Created,
                                                                             EVTaler:          new EVTalerMerchant(URL.Parse(MerchantURL),
                                                                                                                   MerchantKey,
                                                                                                                   [ new EVTalerExchange(CHFExchangeKey, URL.Parse("https://exchange.example.ch/"), Currency.CHF, new ImmutableI18NString([ new I18NPair(Languages.en, "Example Franc Exchange") ])),
                                                                                                                     new EVTalerExchange(EURExchangeKey, URL.Parse("https://exchange.example.eu/"), Currency.EUR, new ImmutableI18NString([ new I18NPair(Languages.en, "Example Exchange") ])) ]),
                                                                             ChargingTariffs:  [ new ChargingTariff(AdHoc,
                                                                                                                    Currency.EUR,
                                                                                                                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.FLAT,   0.50m),
                                                                                                                                                  new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.65m, StepSize: 1) ]) ],
                                                                                                                    TariffType:  ChargingTariffType.AD_HOC_PAYMENT,
                                                                                                                    Created:     Created) ],
                                                                             ChargingPools:    [ new ChargingPool(P1,
                                                                                                                  PaymentOptions:    [ PaymentOptions.EVTaler ],
                                                                                                                  AuthenticationModes:  [ AuthenticationMode.SecureDynamicQRCode([ SecureDynamicQRCodeChannels.Display ]) ],
                                                                                                                  Created:           Created,
                                                                                                                  ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                           Created:  Created,
                                                                                                                                                           EVSEs:    [ new EVSE(E1,
                                                                                                                                                                                ChargingTariffIds:  [ AdHoc ],
                                                                                                                                                                                EnergyMeter:        new EnergyMeter(EnergyMeter_Id.Parse("meter-1"),
                                                                                                                                                                                                                    SerialNumber:  SerialNumber.Parse(MeterSerial),
                                                                                                                                                                                                                    PublicKeys:    [ new MeasurementPublicKey(Meter.ExportSubjectPublicKeyInfo()) ],
                                                                                                                                                                                                                    Created:       Created),
                                                                                                                                                                                Created:            Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region (static) NewSession(Now)

        /// <summary>
        /// The history of the network, its operator stating its keys and attesting its head now, and the verified station.
        /// </summary>
        private static Session NewSession(DateTimeOffset Now)
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var network   = history.Head.RoamingNetwork;
            var signing   = TestParties.Key(TestParties.Party(network), POIKeyKind.Signing);
            var identity  = TestParties.Key(TestParties.Party(network), POIKeyKind.Identity);

            var gef       = TestParties.Key(TestParties.Party(GEF));

            history.PriceStatementSigners = [ new RoamingNetworkCommitSigner(gef.Private, gef.Public.Id, COSEAlgorithm.ES256, RoamingNetworkCommitSignatureRoles.ChargingStationOperator, GEF.ToString()) ];

            var keyStatement = RoamingNetworkKeyStatement.Create(identity.Private,
                                                                 COSEAlgorithm.ES512,
                                                                 1,
                                                                 network.Id,
                                                                 network.Operator!.Id,
                                                                 [ identity.Public ],
                                                                 Now.AddHours(-2),
                                                                 Now.AddDays(30));

            var certificate  = RoamingNetworkSigningKeyCertificate.Create(identity.Private,
                                                                          COSEAlgorithm.ES512,
                                                                          network.Operator!.Id,
                                                                          signing.Public,
                                                                          Now.AddHours(-2));

            var result       = history.Commit(network.CreateSignedChangeSet([ RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkKeyStatement.JSONProperty,          null, keyStatement.ToJSON()),
                                                                              RoamingNetworkChange.UpdateProperty(network.Operator!, RoamingNetworkSigningKeyCertificate.JSONProperty, null, new JArray(certificate.ToJSON(Embedded: true))) ],
                                                                            CreatedAt: Now.AddHours(-2)));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);

            history.AttestationSigners = [ new RoamingNetworkCommitSigner(signing.Private, signing.Public.Id, COSEAlgorithm.ES256, RoamingNetworkCommitSignatureRoles.RoamingNetworkOperator) ];

            Assert.That(history.TryAttest(out _, out var attestError, Now),  Is.True, attestError);

            return new Session(history, Verified(history, Now), Now);

        }

        #endregion

        #region (static) Verified(History, Now)

        private static VerifiedChargingStation Verified(RoamingNetworkHistory  History,
                                                        DateTimeOffset         Now)
        {

            Assert.That(History.TryGetChargingStationProof(S1, out var proof, out var proofError),  Is.True, proofError);
            Assert.That(VerifiedChargingStation.TryVerify(History.Head.RoamingNetwork.TrustAnchor(), proof!, out var station, out var verifyError, ChargingStationId: S1.ToString(), At: Now),  Is.True, verifyError);

            return station!;

        }

        #endregion


        #region (static) MeterValue(Signer, Energy, At, Serial = MeterSerial)

        /// <summary>
        /// A signed meter value of the test format: the payload, a JSON text naming the meter, the time and the energy
        /// register, and its ECDSA signature, both in a JSON object, Base64 encoded as OCPP 2.1 sends it.
        /// </summary>
        private static EVTalerSignedMeterValue MeterValue(ECDsa           Signer,
                                                          Decimal         Energy,
                                                          DateTimeOffset  At,
                                                          String          Serial = MeterSerial)
        {

            var payload    = new JObject(new JProperty("serial", Serial),
                                         new JProperty("time",   At.ToISO8601()),
                                         new JProperty("kWh",    Energy.ToString(CultureInfo.InvariantCulture))).ToString(Newtonsoft.Json.Formatting.None);

            var signature  = Signer.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256);

            return new EVTalerSignedMeterValue(Convert.ToBase64String(Encoding.UTF8.GetBytes(new JObject(new JProperty("payload",   payload),
                                                                                                          new JProperty("signature", Convert.ToBase64String(signature))).ToString(Newtonsoft.Json.Formatting.None))),
                                               Encoding_,
                                               "ECDSA-secp256r1-SHA256");

        }

        #endregion

        #region (static) VerifyMeterValue(Value, Key, out Reading, out ErrorResponse)

        /// <summary>
        /// The transparency software of the test format.
        /// </summary>
        private static Boolean VerifyMeterValue(EVTalerSignedMeterValue                        Value,
                                                MeasurementPublicKey                           Key,
                                                [NotNullWhen(true)]  out EVTalerMeterReading?  Reading,
                                                [NotNullWhen(false)] out String?               ErrorResponse)
        {

            Reading = null;

            if (Value.EncodingMethod != Encoding_)
            {
                ErrorResponse = $"Unknown format '{Value.EncodingMethod}'!";
                return false;
            }

            var data      = (JObject) POIJSON.Read(Encoding.UTF8.GetString(Convert.FromBase64String(Value.SignedMeterData)));
            var payload   = data["payload"]!.Value<String>()!;

            using var key = ECDsa.Create();
            key.ImportSubjectPublicKeyInfo(Key.Key.AsSpan(), out _);

            if (!key.VerifyData(Encoding.UTF8.GetBytes(payload), Convert.FromBase64String(data["signature"]!.Value<String>()!), HashAlgorithmName.SHA256))
            {
                ErrorResponse = "Invalid signature!";
                return false;
            }

            var reading    = (JObject) POIJSON.Read(payload);

            Reading        = new EVTalerMeterReading(reading["serial"]!.Value<String>()!,
                                                     DateTimeOffset.Parse(reading["time"]!.Value<String>()!, CultureInfo.InvariantCulture),
                                                     Decimal.Parse(reading["kWh"]!.Value<String>()!, CultureInfo.InvariantCulture));
            ErrorResponse  = null;
            return true;

        }

        #endregion

        #region (static) Terms(Step, OrderId, Amount, At, …)

        /// <summary>
        /// The contract terms of a step, as a merchant backend makes them (version 0).
        /// </summary>
        private static EVTalerContractTerms Terms(EVTalerStep     Step,
                                                  String          OrderId,
                                                  String          Amount,
                                                  DateTimeOffset  At,
                                                  String          MerchantPublicKey   = MerchantKey,
                                                  String          ExchangeURL         = "https://exchange.example.eu/",
                                                  String          ExchangeKey         = EURExchangeKey)
        {

            var seconds  = At.ToUnixTimeSeconds();
            var currency = Amount.Split(':')[0];

            var json     = new JObject(new JProperty("order_id",                OrderId),
                                       new JProperty("summary",                 $"Charging at {Step.EVSEId}, step {Step.Step}: {Step.Covers}"),
                                       new JProperty("amount",                  Amount),
                                       new JProperty("max_fee",                 $"{currency}:0.10"),
                                       new JProperty("products",                new JArray(new JObject(new JProperty("description", "Charging"),
                                                                                                       new JProperty("quantity",    5),
                                                                                                       new JProperty("unit",        "kWh")))),
                                       new JProperty("timestamp",               new JObject(new JProperty("t_s", seconds))),
                                       new JProperty("pay_deadline",            new JObject(new JProperty("t_s", seconds + 120))),
                                       new JProperty("refund_deadline",         new JObject(new JProperty("t_s", seconds + 86400))),
                                       new JProperty("wire_transfer_deadline",  new JObject(new JProperty("t_s", seconds + 2 * 86400))),
                                       new JProperty("auto_refund",             new JObject(new JProperty("d_us", 86400L * 1000000))),
                                       new JProperty("merchant_pub",            MerchantPublicKey),
                                       new JProperty("merchant_base_url",       MerchantURL),
                                       new JProperty("merchant",                new JObject(new JProperty("name", "GraphDefined"))),
                                       new JProperty("h_wire",                  "ABCD"),
                                       new JProperty("wire_method",             "iban"),
                                       new JProperty("exchanges",               new JArray(new JObject(new JProperty("url",        ExchangeURL),
                                                                                                       new JProperty("priority",   1024),
                                                                                                       new JProperty("master_pub", ExchangeKey)))),
                                       new JProperty("nonce",                   "NONCE"),
                                       new JProperty("extra",                   new JObject(new JProperty("wwcp", Step.ToJSON()))));

            Assert.That(EVTalerContractTerms.TryParse(json, out var terms, out var error),  Is.True, error);

            return terms!;

        }

        #endregion

        #region (static) Step(Number, From, To, Previous = null, Start = null, PriceStatement = null)

        private static EVTalerStep Step(UInt32                    Number,
                                        Decimal                   From,
                                        Decimal                   To,
                                        String?                   Previous         = null,
                                        EVTalerSignedMeterValue?  Start            = null,
                                        String?                   PriceStatement   = null)

            => new (RN, E1.ToString(), SessionId, Number, Previous, AdHoc.ToString(), PriceStatement, new EVTalerCovers(From, To, EVTalerCovers.KWh), Start);

        #endregion

        #region (static) Settle(Session, Steps, Start, End, Price = null, Key = null, …)

        private static EVTalerSettlement Settle(Session                                        Session,
                                                IReadOnlyList<EVTalerVerifiedStep>             Steps,
                                                EVTalerSignedMeterValue                        Start,
                                                EVTalerSignedMeterValue                        End,
                                                DateTimeOffset                                 StartAt,
                                                DateTimeOffset                                 EndAt,
                                                Decimal                                        Energy,
                                                Decimal                                        Price,
                                                (Org.BouncyCastle.Crypto.AsymmetricKeyParameter Private, String Id)?  Key  = null)
        {

            var (privateKey, publicKey) = TestParties.Key(TestParties.Party(GEF));

            return EVTalerSettlement.Create(Key?.Private ?? privateKey,
                                            Key?.Id      ?? publicKey.Id,
                                            COSEAlgorithm.ES256,
                                            GEF,
                                            RN,
                                            E1.ToString(),
                                            SessionId,
                                            MerchantKey,
                                            AdHoc.ToString(),
                                            Steps[0].Step.PriceStatement,
                                            StartAt,
                                            EndAt,
                                            Start,
                                            End,
                                            Energy,
                                            "EUR",
                                            Steps.Select(step => (step.Terms.OrderId, step.Terms.Amount)),
                                            Price,
                                            SignedAt: EndAt.AddMinutes(1));

        }

        #endregion


        #region A_step_is_written_as_read_and_refused_otherwise()

        [Test]
        public void A_step_is_written_as_read_and_refused_otherwise()
        {

            var start  = MeterValue(Meter, 1234.5m, DateTimeOffset.Parse("2026-10-28T09:12:04Z"));
            var step   = Step(2, 5, 10, "2026.301-01", start);
            var json   = step.ToJSON();

            Assert.That(EVTalerStep.TryParse(json, out var read, out var error),  Is.True, error);

            JObject Changed(Action<JObject> Change)
            {
                var changed = (JObject) json.DeepClone();
                Change(changed);
                return changed;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(read!.ToJSON().ToString(),                                                                         Is.EqualTo(json.ToString()));
                Assert.That(read.StartMeterValue,                                                                              Is.EqualTo(start));
                Assert.That(json["covers"]!.ToString(Newtonsoft.Json.Formatting.None),                                        Is.EqualTo("{\"from\":\"5\",\"to\":\"10\",\"unit\":\"kWh\"}"));

                Assert.That(EVTalerStep.TryParse(Changed(j => j.Remove("startMeterValue")),                         out _, out _),  Is.False, "step 2 without the start value");
                Assert.That(EVTalerStep.TryParse(Changed(j => j.Remove("previousOrderId")),                        out _, out _),  Is.False, "step 2 without the order before");
                Assert.That(EVTalerStep.TryParse(Changed(j => j["unknown"]   = 1),                                  out _, out _),  Is.False, "an unknown property");
                Assert.That(EVTalerStep.TryParse(Changed(j => j["@context"]  = "https://example.org/step"),         out _, out _),  Is.False, "another context");
                Assert.That(EVTalerStep.TryParse(Changed(j => j["sessionId"] = "kq3Vd0sY0f9hJ2bW8Qm1tB"),           out _, out _),  Is.False, "a session not canonical base64url");
                Assert.That(EVTalerStep.TryParse(Changed(j => j["step"]      = 0),                                  out _, out _),  Is.False, "step 0");
                Assert.That(EVTalerStep.TryParse(Changed(j => j["covers"]    = new JObject(new JProperty("from", "5"), new JProperty("to", 10), new JProperty("unit", "kWh"))),  out _, out _),  Is.False, "a number not written as a text");
                Assert.That(EVTalerStep.TryParse(Changed(j => j["covers"]!["unit"] = "Wh"),                         out _, out _),  Is.False, "a unit not kWh or min");

                Assert.That(() => Step(1, 2, 5),                                                                               Throws.ArgumentException, "step 1 covers from 0");
                Assert.That(() => Step(1, 0, 5, Start: start),                                                                 Throws.ArgumentException, "step 1 names no start value");

            }

        }

        #endregion

        #region A_step_costs_what_it_covers_rounded_up_to_the_cent()

        /// <summary>
        /// What a step covers at the price of its dimension, rounded up to the cent, and not a cent more: 0.40 €/h is no
        /// rate of a minute a decimal holds exactly (found by the TS port's probe).
        /// </summary>
        [Test]
        public void A_step_costs_what_it_covers_rounded_up_to_the_cent()
        {

            var rates = new EVTalerRates("EUR", 0.59m, 1m, 0.40m, 60m, null);

            Decimal PriceOf(Decimal From, Decimal To, String Unit)
            {
                Assert.That(rates.TryGetStepPrice(new EVTalerCovers(From, To, Unit), out var price, out var error),  Is.True, error);
                return price;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(PriceOf(0,  60, EVTalerCovers.Minutes),  Is.EqualTo(0.40m));
                Assert.That(PriceOf(0,  30, EVTalerCovers.Minutes),  Is.EqualTo(0.20m));
                Assert.That(PriceOf(30, 120, EVTalerCovers.Minutes), Is.EqualTo(0.60m));
                Assert.That(PriceOf(0,  7,  EVTalerCovers.Minutes),  Is.EqualTo(0.05m), "0.0467 €, rounded up");
                Assert.That(PriceOf(0,  5,  EVTalerCovers.KWh),      Is.EqualTo(2.95m));
                Assert.That(PriceOf(5,  5.5m, EVTalerCovers.KWh),    Is.EqualTo(0.30m), "0.295 €, rounded up");
            }

        }

        #endregion

        #region A_pay_URI_names_the_merchant_of_the_operator()

        [Test]
        public void A_pay_URI_names_the_merchant_of_the_operator()
        {

            var merchant = NewNetwork().ChargingStationOperators.First().EVTaler!;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(EVTalerPayURI.TryParse("taler://pay/taler.gef.example/instances/de-gef/2026.301-01ABCDEF1/?c=CLAIMTOKEN", out var uri, out var error),  Is.True, error);
                Assert.That(uri!.MerchantBaseURL,                                                       Is.EqualTo(MerchantURL));
                Assert.That(uri.OrderId,                                                                Is.EqualTo("2026.301-01ABCDEF1"));
                Assert.That(uri.SessionId,                                                              Is.Null);
                Assert.That(uri.ClaimToken,                                                             Is.EqualTo("CLAIMTOKEN"));
                Assert.That(uri.TryVerify(merchant, out error),                                         Is.True, error);

                Assert.That(EVTalerPayURI.TryParse("TALER://PAY/TALER.GEF.EXAMPLE/instances/de-gef/O1/S1", out var upper, out error),  Is.True, error);
                Assert.That(upper!.TryVerify(merchant, out error),                                      Is.True, "the scheme and the host in any case: " + error);
                Assert.That(upper.SessionId,                                                            Is.EqualTo("S1"));

                Assert.That(EVTalerPayURI.TryParse("taler://pay/taler.evil.example/instances/de-gef/O1/", out var foreign, out _),        Is.True);
                Assert.That(foreign!.TryVerify(merchant, out _),                                        Is.False, "another merchant");
                Assert.That(EVTalerPayURI.TryParse("taler://pay/taler.gef.example/instances/de-gef-2/O1/", out var otherInstance, out _), Is.True);
                Assert.That(otherInstance!.TryVerify(merchant, out _),                                  Is.False, "another instance");

                Assert.That(EVTalerPayURI.TryParse("taler+http://pay/taler.gef.example/instances/de-gef/O1/", out _, out _),            Is.False, "without HTTPS");
                Assert.That(EVTalerPayURI.TryParse("taler://withdraw/exchange.example.eu/W1",                 out _, out _),            Is.False);
                Assert.That(EVTalerPayURI.TryParse("taler://pay/taler.gef.example",                            out _, out _),            Is.False);

            }

        }

        #endregion

        #region A_session_in_steps_is_checked_and_settled()

        /// <summary>
        /// Step 1 pays the flat fee, 5 kWh and a reserve of 1 €; step 2 the next 5 kWh, naming the signed start value; the
        /// session takes 7.412 kWh, priced 0.50 € + 7.412 kWh at 0.65 € = 5.32 €, and 2.68 € of the 8.00 € paid is refunded.
        /// </summary>
        [Test]
        public void A_session_in_steps_is_checked_and_settled()
        {

            var now      = Now();
            var session  = NewSession(now);
            var startAt  = now.AddMinutes(-39);
            var endAt    = now.AddMinutes(-5);
            var start    = MeterValue(Meter, 1234.5m,   startAt);
            var end      = MeterValue(Meter, 1241.912m, endAt);

            var terms1   = Terms(Step(1, 0,  5),                       "2026.309-01", "EUR:4.75", now.AddMinutes(-40));
            var terms2   = Terms(Step(2, 5, 10, terms1.OrderId, start), "2026.309-02", "EUR:3.25", now.AddMinutes(-20));

            Assert.That(terms1.TryVerify(session.Station, [], null,  null,             out var step1, out var error),  Is.True, error);
            Assert.That(terms2.TryVerify(session.Station, [], step1, VerifyMeterValue, out var step2, out error),      Is.True, error);

            var settlement = Settle(session, [ step1!, step2! ], start, end, startAt, endAt, 7.412m, 5.32m);

            Assert.That(EVTalerSettlement.TryParse(POIJSON.Read(settlement.JSON.ToString()), out var read, out error),  Is.True, error);
            Assert.That(read!.TryVerify(session.Station, [ step1!, step2! ], VerifyMeterValue, out var verification, out error),  Is.True, error);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(step1!.AmountCheck,                        Is.EqualTo(EVTalerAmountChecks.Exact));
                Assert.That(step1.StepPrice,                           Is.EqualTo(3.25m));
                Assert.That(step1.FlatFee,                             Is.EqualTo(0.50m));
                Assert.That(step1.Reserve,                             Is.EqualTo(1.00m));
                Assert.That(step1.Start,                               Is.Null);
                Assert.That(step2!.AmountCheck,                        Is.EqualTo(EVTalerAmountChecks.Exact));
                Assert.That(step2.Start!.Reading.Energy,               Is.EqualTo(1234.5m));
                Assert.That(step2.Start.SignedBy,                      Is.EqualTo("meter-1"));

                Assert.That(read.JSON["price"]!.Value<String>(),       Is.EqualTo("5.32"));
                Assert.That(read.JSON["paid"]!.Value<String>(),        Is.EqualTo("8.00"));
                Assert.That(read.JSON["refunded"]!.Value<String>(),    Is.EqualTo("2.68"));
                Assert.That(read.JSON["energy"]!.Value<String>(),      Is.EqualTo("7.412"));
                Assert.That(verification!.Energy,                      Is.EqualTo(7.412m));
                Assert.That(verification.ComputedPrice,                Is.EqualTo(5.32m));
                Assert.That(verification.PriceCheck,                   Is.EqualTo(EVTalerAmountChecks.Exact));

            }

        }

        #endregion

        #region Offers_of_another_merchant_or_exchange_or_at_another_price_are_refused()

        [Test]
        public void Offers_of_another_merchant_or_exchange_or_at_another_price_are_refused()
        {

            var now      = Now();
            var session  = NewSession(now);
            var at       = now.AddMinutes(-40);
            var start    = MeterValue(Meter, 1234.5m, now.AddMinutes(-39));

            Assert.That(Terms(Step(1, 0, 5), "O1", "EUR:4.75", at).TryVerify(session.Station, [], null, null, out var step1, out var error),  Is.True, error);

            Boolean Refused(EVTalerContractTerms Terms, EVTalerVerifiedStep? Previous = null, EVTalerMeterValueVerifier? Verifier = null)
                => !Terms.TryVerify(session.Station, [], Previous, Verifier, out _, out _);

            var foreignStep = new EVTalerStep(RN, E1.ToString(), SessionId, 1, null, "DE*GEF*T_OTHER", null, new EVTalerCovers(0, 5, EVTalerCovers.KWh), null);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Refused(Terms(Step(1, 0, 5), "O1", "EUR:4.75", at, MerchantPublicKey: CHFExchangeKey)),                                       Is.True, "another merchant key");
                Assert.That(Refused(Terms(Step(1, 0, 5), "O1", "EUR:4.75", at, ExchangeURL: "https://exchange.evil.example/")),                         Is.True, "an exchange the operator does not name");
                Assert.That(Refused(Terms(Step(1, 0, 5), "O1", "EUR:4.75", at, ExchangeURL: "https://exchange.example.ch/", ExchangeKey: CHFExchangeKey)), Is.True, "an exchange of another currency");
                Assert.That(Refused(Terms(Step(1, 0, 5), "O1", "CHF:4.75", at, ExchangeURL: "https://exchange.example.ch/", ExchangeKey: CHFExchangeKey)), Is.True, "another currency than the tariff's");
                Assert.That(Refused(Terms(foreignStep,   "O1", "EUR:4.75", at)),                                                                        Is.True, "another tariff than the EV Taler tariff");
                Assert.That(Refused(Terms(Step(1, 0, 5), "O1", "EUR:3.70", at)),                                                                        Is.True, "less than 5 kWh and the flat fee");
                Assert.That(Refused(Terms(Step(1, 0, 5, PriceStatement: new String('a', 64)), "O1", "EUR:4.75", at)),                                    Is.True, "a price statement not current");

                Assert.That(Refused(Terms(Step(2, 5, 10, "O1", start), "O2", "EUR:3.30", at), step1, VerifyMeterValue),                                  Is.True, "step 2 is 5 kWh at the price, no more");
                Assert.That(Refused(Terms(Step(2, 5, 10, "O0", start), "O2", "EUR:3.25", at), step1, VerifyMeterValue),                                  Is.True, "another order before");
                Assert.That(Refused(Terms(Step(2, 6, 11, "O1", start), "O2", "EUR:3.25", at), step1, VerifyMeterValue),                                  Is.True, "not following on");
                Assert.That(Refused(Terms(Step(3, 5, 10, "O1", start), "O2", "EUR:3.25", at), step1, VerifyMeterValue),                                  Is.True, "step 3 after step 1");
                Assert.That(Refused(Terms(Step(2, 5, 10, "O1", start), "O2", "EUR:3.25", at)),                                                          Is.True, "step 2 without step 1");
                Assert.That(Refused(Terms(Step(2, 5, 10, "O1", start), "O2", "EUR:3.25", at), step1),                                                   Is.True, "no transparency software for the start value");

            }

        }

        #endregion

        #region The_start_value_is_verified_with_the_key_of_the_proof_and_stays()

        [Test]
        public void The_start_value_is_verified_with_the_key_of_the_proof_and_stays()
        {

            var now      = Now();
            var session  = NewSession(now);
            var at       = now.AddMinutes(-40);
            var start    = MeterValue(Meter, 1234.5m, now.AddMinutes(-39));

            Assert.That(Terms(Step(1, 0, 5), "O1", "EUR:4.75", at).TryVerify(session.Station, [], null, null, out var step1, out var error),  Is.True, error);
            Assert.That(Terms(Step(2, 5, 10, "O1", start), "O2", "EUR:3.25", at).TryVerify(session.Station, [], step1, VerifyMeterValue, out var step2, out error),  Is.True, error);

            Boolean Refused(EVTalerSignedMeterValue Start, EVTalerVerifiedStep Previous, UInt32 Number = 2, String Before = "O1")
                => !Terms(Step(Number, 5 * (Number - 1), 5 * Number, Before, Start), "O" + Number, "EUR:3.25", at).TryVerify(session.Station, [], Previous, VerifyMeterValue, out _, out _);

            var withForeignKey = new EVTalerSignedMeterValue(start.SignedMeterData, start.EncodingMethod, start.SigningMethod, Convert.ToBase64String(OtherMeter.ExportSubjectPublicKeyInfo()));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Refused(MeterValue(OtherMeter, 1234.5m, now.AddMinutes(-39)),                 step1!),            Is.True, "signed by another meter");
                Assert.That(Refused(MeterValue(Meter,      1234.5m, now.AddMinutes(-39), Serial: "M-2"), step1!),            Is.True, "naming another serial number");
                Assert.That(Refused(withForeignKey,                                                        step1!),            Is.True, "naming a key the proof does not give");
                Assert.That(Refused(MeterValue(Meter,      1234.6m, now.AddMinutes(-39)),                 step2!, 3, "O2"),  Is.True, "step 3 naming another start value than step 2");
                Assert.That(Refused(start,                                                                 step2!, 3, "O2"),  Is.False, "step 3 naming the start value of step 2");
            }

        }

        #endregion

        #region No_settlement_without_both_signed_meter_values()

        [Test]
        public void No_settlement_without_both_signed_meter_values()
        {

            var now      = Now();
            var session  = NewSession(now);
            var startAt  = now.AddMinutes(-39);
            var endAt    = now.AddMinutes(-5);
            var start    = MeterValue(Meter, 1234.5m,   startAt);
            var end      = MeterValue(Meter, 1241.912m, endAt);

            Assert.That(Terms(Step(1, 0, 5), "O1", "EUR:4.75", now.AddMinutes(-40)).TryVerify(session.Station, [], null, null, out var step1, out var error),  Is.True, error);
            Assert.That(Terms(Step(2, 5, 10, "O1", start), "O2", "EUR:3.25", now.AddMinutes(-20)).TryVerify(session.Station, [], step1, VerifyMeterValue, out var step2, out error),  Is.True, error);

            EVTalerVerifiedStep[] steps = [ step1!, step2! ];

            Boolean Refused(EVTalerSettlement Settlement, IReadOnlyList<EVTalerVerifiedStep>? Steps = null)
            {
                var taken = Settlement.TryVerify(session.Station, Steps ?? steps, VerifyMeterValue, out _, out _);
                return !taken;
            }

            var good        = Settle(session, steps, start, end, startAt, endAt, 7.412m, 5.32m);

            var oneValue    = (JObject) good.JSON.DeepClone();
            ((JArray) oneValue["meterValues"]!).RemoveAt(1);

            var paymentsOnly = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF/payments"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Refused(good),                                                                                          Is.False);
                Assert.That(EVTalerSettlement.TryParse(oneValue, out _, out _),                                                     Is.False, "one signed meter value");

                Assert.That(Refused(Settle(session, steps, end,   start, endAt,   startAt, 7.412m, 5.32m)),                         Is.True,  "the end before the start");
                Assert.That(Refused(Settle(session, steps, MeterValue(Meter, 1234.4m, startAt), end, startAt, endAt, 7.512m, 5.38m)),  Is.True,  "another start value than the steps name");
                Assert.That(Refused(Settle(session, steps, start, MeterValue(OtherMeter, 1241.912m, endAt), startAt, endAt, 7.412m, 5.32m)),  Is.True,  "the end value of another meter");
                Assert.That(Refused(Settle(session, steps, start, MeterValue(Meter, 1230m, endAt), startAt, endAt, 0m, 0.50m)),     Is.True,  "the end value below the start value");
                Assert.That(Refused(Settle(session, steps, start, end, startAt, endAt, 7.5m,   5.32m)),                             Is.True,  "another energy than the values'");
                Assert.That(Refused(Settle(session, steps, start, end, startAt, endAt.AddSeconds(1), 7.412m, 5.32m)),               Is.True,  "another end than the value's");
                Assert.That(Refused(Settle(session, steps, start, end, startAt, endAt, 7.412m, 5.33m)),                             Is.True,  "another price than by the tariff");
                Assert.That(Refused(good, [ step1! ]),                                                                              Is.True,  "orders of other steps");
                Assert.That(Refused(Settle(session, steps, start, end, startAt, endAt, 7.412m, 5.32m, (paymentsOnly.Private, paymentsOnly.Public.Id))),  Is.True,  "a key the proof does not give");

            }

        }

        #endregion

        #region A_settlement_needs_a_key_for_payments()

        [Test]
        public void A_settlement_needs_a_key_for_payments()
        {

            var now        = Now();
            var session    = NewSession(now);
            var startAt    = now.AddMinutes(-39);
            var endAt      = now.AddMinutes(-5);
            var start      = MeterValue(Meter, 1234.5m,   startAt);
            var end        = MeterValue(Meter, 1236.912m, endAt);

            Assert.That(Terms(Step(1, 0, 5), "O1", "EUR:4.75", now.AddMinutes(-40)).TryVerify(session.Station, [], null, null, out var step1, out var error),  Is.True, error);

            // DE*GEF signs with keys for tariffs and for payments apart.
            var tariffs    = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF/tariffs"));
            var payments   = TestParties.Key(new POIPartyId(nameof(ChargingStationOperator), "DE*GEF/payments"));
            var history    = session.History;
            var gef        = history.Head.RoamingNetwork.ChargingStationOperators.First();
            var result     = history.Commit(history.Head.RoamingNetwork.CreateSignedChangeSet(
                                 [ RoamingNetworkChange.UpdateProperty(gef, POIPartyKeys.SigningKeysProperty, null,
                                                                       new JArray(gef.SigningKeys.Concat([ POIPublicKey.From(tariffs. Private, Usages: [ POIKeyUsages.PriceStatements ]),
                                                                                                           POIPublicKey.From(payments.Private, Usages: [ POIKeyUsages.Payments ]) ]).
                                                                                      OrderBy(key => key.Id.ToString(), StringComparer.Ordinal).
                                                                                      Select (key => key.ToJSON(Embedded: true)))) ],
                                 CreatedAt: now.AddMinutes(-50)));

            Assert.That(result.Status, Is.EqualTo(RoamingNetworkCommitStatus.Committed), result.ErrorResponse);
            Assert.That(history.TryAttest(out _, out var attestError, now),  Is.True, attestError);

            var station    = Verified(history, now);

            Assert.That(Terms(Step(1, 0, 5), "O1", "EUR:4.75", now.AddMinutes(-40)).TryVerify(station, [], null, null, out step1, out error),  Is.True, error);

            Boolean Taken((Org.BouncyCastle.Crypto.Parameters.ECPrivateKeyParameters Private, POIPublicKey Public) Key)
            {
                var taken = Settle(session, [ step1! ], start, end, startAt, endAt, 2.412m, 2.07m, (Key.Private, Key.Public.Id)).
                                TryVerify(station, [ step1! ], VerifyMeterValue, out _, out _);
                return taken;
            }

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Taken(payments),  Is.True,  "the key for payments");
                Assert.That(Taken(tariffs),   Is.False, "a key for price statements alone");
            }

        }

        #endregion

        #region A_price_statement_prices_the_session_and_every_step_names_it()

        [Test]
        public void A_price_statement_prices_the_session_and_every_step_names_it()
        {

            var now        = Now();
            var session    = NewSession(now);
            var prices     = new ChargingPrices([ new ChargingTariffPrices(AdHoc, [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.59m, StepSize: 1) ]) ]) ]);

            Assert.That(session.History.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.PriceStatus, nameof(EVSE), E1.ToString(), prices.Text, now.AddMinutes(-50), out _, out var updateError),  Is.EqualTo(POIStatusUpdateResult.Updated), updateError);
            Assert.That(session.History.TryGetPriceStatement(nameof(EVSE), E1.ToString(), out var statement),  Is.True);

            // A price statement is valid from when it is signed: the session begins after it.
            var at         = DateTimeOffset.FromUnixTimeSeconds(statement!.ValidFrom.ToUnixTimeSeconds() + 2);

            Boolean Taken(EVTalerStep Step, String Amount, out EVTalerVerifiedStep? Verified)
            {
                var taken = Terms(Step, "O1", Amount, at).TryVerify(session.Station, [ statement! ], null, null, out Verified, out _);
                return taken;
            }

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Taken(Step(1, 0, 5, PriceStatement: statement!.Digest), "EUR:2.95", out var step1),  Is.True, "5 kWh at 0.59 €, no flat fee");
                Assert.That(step1!.PriceStatement!.Digest,                                                       Is.EqualTo(statement.Digest));
                Assert.That(step1.Reserve,                                                                       Is.EqualTo(0m));
                Assert.That(Taken(Step(1, 0, 5),                                     "EUR:4.75", out _),          Is.False, "a step not naming the price statement of the session");

            }

        }

        #endregion

    }

}
