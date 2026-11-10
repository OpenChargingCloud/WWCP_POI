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

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// EV Taler, phase 3 (concepts/EVTaler.md, sections 6 and 9): the offers of a POI server for the operator its backend
    /// serves, over HTTP, against a merchant backend and a charging station management system played by the test; and the
    /// offersURL of the operator's evTaler.
    /// </summary>
    [TestFixture]
    public class EVTalerOffersTests
    {

        #region Data

        private static readonly DateTimeOffset              Created         = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private static readonly RoamingNetwork_Id           RN              = RoamingNetwork_Id.         Parse("rn1");
        private static readonly ChargingStationOperator_Id  GEF             = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id             P1              = ChargingPool_Id.           Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id          S1              = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly EVSE_Id                     E1              = EVSE_Id.                   Parse("DE*GEF*E1*1");
        private static readonly EVSE_Id                     E2              = EVSE_Id.                   Parse("DE*GEF*E1*2");
        private static readonly ChargingTariff_Id           AdHoc           = ChargingTariff_Id.         Parse("DE*GEF*T_ADHOC");

        private const   String  MeterSerial      = "M-1";
        private const   String  Encoding_        = "WWCP-TEST";
        private const   String  AccessToken      = "secret-token:test";

        private const   String  MerchantURL      = "https://taler.gef.example/instances/de-gef/";
        private const   String  MerchantKey      = "ESMBKNR5CMZPAYH9AD0QNKCPYSJSE0EN5MH4FMPKN683R63HMD3G";
        private const   String  OtherKey         = "YJ7H5JVNVM5SG5FAE3BGQ35AEPHXBH27MWNRPFQ0SVW9BH0BN9B0";
        private const   String  EURExchangeKey   = "F80MFRG8HVH6R9CQ47KRFQSJP3T6DBJ4K1D9B703RJY3Z39TBMJ0";
        private const   String  EURExchangeURL   = "https://exchange.example.eu/";

        private static readonly ECDsa  Meter = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        #endregion

        #region OneTimeTearDown()

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Meter.Dispose();
        }

        #endregion


        #region (static) NewNetwork()

        /// <summary>
        /// DE*GEF accepting EV Taler at two EVSEs: its merchant, an ad hoc tariff of a flat fee of 0.50 € and 0.65 €/kWh,
        /// billed per Wh, and the energy meter of DE*GEF*E1*1 with its measurement key.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RN,
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             Created:          Created,
                                                                             EVTaler:          new EVTalerMerchant(URL.Parse(MerchantURL),
                                                                                                                   MerchantKey,
                                                                                                                   [ new EVTalerExchange(EURExchangeKey, URL.Parse(EURExchangeURL), Currency.EUR, new ImmutableI18NString([ new I18NPair(Languages.en, "Example Exchange") ])) ],
                                                                                                                   URL.Parse("https://poi.gef.example/RNs/rn1/")),
                                                                             ChargingTariffs:  [ new ChargingTariff(AdHoc,
                                                                                                                    Currency.EUR,
                                                                                                                    [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.FLAT,   0.50m),
                                                                                                                                                  new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.65m, StepSize: 1) ]) ],
                                                                                                                    TariffType:  ChargingTariffType.AD_HOC_PAYMENT,
                                                                                                                    Created:     Created) ],
                                                                             ChargingPools:    [ new ChargingPool(P1,
                                                                                                                  PaymentOptions:    [ PaymentOptions.EVTaler ],
                                                                                                                  Created:           Created,
                                                                                                                  ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                                           Created:  Created,
                                                                                                                                                           EVSEs:    [ new EVSE(E1,
                                                                                                                                                                                ChargingTariffIds:  [ AdHoc ],
                                                                                                                                                                                EnergyMeter:        new EnergyMeter(EnergyMeter_Id.Parse("meter-1"),
                                                                                                                                                                                                                    SerialNumber:  SerialNumber.Parse(MeterSerial),
                                                                                                                                                                                                                    PublicKeys:    [ new MeasurementPublicKey(Meter.ExportSubjectPublicKeyInfo()) ],
                                                                                                                                                                                                                    Created:       Created),
                                                                                                                                                                                Created:            Created),
                                                                                                                                                                       new EVSE(E2,
                                                                                                                                                                                ChargingTariffIds:  [ AdHoc ],
                                                                                                                                                                                Created:            Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region (static) NewHistory(Now)

        /// <summary>
        /// The history of the network, its operator stating its keys and attesting its head now.
        /// </summary>
        private static RoamingNetworkHistory NewHistory(DateTimeOffset Now)
        {

            var history   = RoamingNetworkHistory.Start(NewNetwork());
            var network   = history.Head.RoamingNetwork;
            var signing   = TestParties.Key(TestParties.Party(network), POIKeyKind.Signing);
            var identity  = TestParties.Key(TestParties.Party(network), POIKeyKind.Identity);

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

            return history;

        }

        #endregion


        #region (static) MeterValue(Energy, At)

        /// <summary>
        /// A signed meter value of the test format: the payload, a JSON text naming the meter, the time and the energy
        /// register, and its ECDSA signature, both in a JSON object, Base64 encoded as OCPP 2.1 sends it.
        /// </summary>
        private static EVTalerSignedMeterValue MeterValue(Decimal         Energy,
                                                          DateTimeOffset  At)
        {

            var payload    = new JObject(new JProperty("serial", MeterSerial),
                                         new JProperty("time",   At.ToISO8601()),
                                         new JProperty("kWh",    Energy.ToString(CultureInfo.InvariantCulture))).ToString(Newtonsoft.Json.Formatting.None);

            var signature  = Meter.SignData(Encoding.UTF8.GetBytes(payload), HashAlgorithmName.SHA256);

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


        #region (class) FakeMerchant

        /// <summary>
        /// The instance of DE*GEF at a GNU Taler merchant backend, as far as the backend of EV Taler sessions asks it: orders
        /// with their contract terms as the backend completes them, their status with long polling, payments made by the
        /// test, and refunds.
        /// </summary>
        private sealed class FakeMerchant(String MerchantPublicKey = MerchantKey) : HttpMessageHandler
        {

            public sealed class Order(String OrderId, String Token, JObject ContractTerms)
            {
                public String                ID             { get; } = OrderId;
                public String                Token          { get; } = Token;
                public JObject               ContractTerms  { get; } = ContractTerms;
                public TaskCompletionSource  Paid           { get; } = new (TaskCreationOptions.RunContinuationsAsynchronously);
                public Decimal               Refunded       { get; set; }
            }

            public ConcurrentDictionary<String, Order>  Orders         { get; } = new ();
            public ConcurrentQueue<String>              Requests       { get; } = new ();
            public Boolean                              Down           { get; set; }

            /// <summary>
            /// Changes the contract terms of the orders it makes from now on, as a merchant backend configured otherwise
            /// than the operator's evTaler, or a relay changing them, would.
            /// </summary>
            public Action<JObject>?                     Tamper         { get; set; }

            public void Pay(String OrderId)
                => Orders[OrderId].Paid.TrySetResult();

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage  Request,
                                                                         CancellationToken   CancellationToken)
            {

                var path = Request.RequestUri!.AbsolutePath;

                Requests.Enqueue($"{Request.Method} {Request.RequestUri.PathAndQuery}");

                if (Down)
                    return Answer(HttpStatusCode.BadGateway, new JObject(new JProperty("code", 1), new JProperty("hint", "down")));

                if (Request.Headers.Authorization?.ToString() != "Bearer " + AccessToken)
                    return Answer(HttpStatusCode.Unauthorized, new JObject(new JProperty("code", 2), new JProperty("hint", "token")));

                const String prefix = "/instances/de-gef/private/orders";

                if (!path.StartsWith(prefix, StringComparison.Ordinal))
                    return Answer(HttpStatusCode.NotFound, new JObject(new JProperty("code", 3)));

                var rest = path[prefix.Length..].Trim('/');

                #region POST private/orders

                if (Request.Method == HttpMethod.Post && rest.Length == 0)
                {

                    var body      = JObject.Parse(await Request.Content!.ReadAsStringAsync(CancellationToken));
                    var order     = (JObject) body["order"]!;
                    var orderId   = order["order_id"]!.Value<String>()!;
                    var now       = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                    var payBy     = order["pay_deadline"]!["t_s"]!.Value<Int64>();
                    var delay     = body["refund_delay"]!["d_us"]!.Value<Int64>() / 1_000_000L;
                    var currency  = order["amount"]!.Value<String>()!.Split(':')[0];

                    var terms     = (JObject) order.DeepClone();
                    terms["timestamp"]               = new JObject(new JProperty("t_s", now));
                    terms["refund_deadline"]         = new JObject(new JProperty("t_s", payBy + delay));
                    terms["wire_transfer_deadline"]  = new JObject(new JProperty("t_s", payBy + delay + 86400));
                    terms["max_fee"]                 = $"{currency}:0.10";
                    terms["merchant_pub"]            = MerchantPublicKey;
                    terms["merchant_base_url"]       = MerchantURL;
                    terms["merchant"]                = new JObject(new JProperty("name", "GraphDefined"));
                    terms["h_wire"]                  = "ABCD";
                    terms["wire_method"]             = "iban";
                    terms["exchanges"]               = new JArray(new JObject(new JProperty("url",        EURExchangeURL),
                                                                              new JProperty("priority",   1024),
                                                                              new JProperty("master_pub", EURExchangeKey)));

                    Tamper?.Invoke(terms);

                    Orders[orderId] = new Order(orderId, "CLAIM" + orderId.Length, terms);

                    return Answer(HttpStatusCode.OK, new JObject(new JProperty("order_id", orderId),
                                                                 new JProperty("token",    Orders[orderId].Token)));

                }

                #endregion

                var parts = rest.Split('/');

                if (!Orders.TryGetValue(Uri.UnescapeDataString(parts[0]), out var known))
                    return Answer(HttpStatusCode.NotFound, new JObject(new JProperty("code", 2000)));

                #region GET private/orders/{id}?timeout_ms

                if (Request.Method == HttpMethod.Get && parts.Length == 1)
                {

                    var query = System.Web.HttpUtility.ParseQueryString(Request.RequestUri.Query);

                    if (!known.Paid.Task.IsCompleted && Int64.TryParse(query["timeout_ms"], out var ms) && ms > 0)
                        await Task.WhenAny(known.Paid.Task, Task.Delay(TimeSpan.FromMilliseconds(Math.Min(ms, 5000)), CancellationToken));

                    if (known.Paid.Task.IsCompleted)
                    {
                        var terms = (JObject) known.ContractTerms.DeepClone();
                        terms["nonce"] = "NONCE";
                        return Answer(HttpStatusCode.OK, new JObject(new JProperty("order_status",   "paid"),
                                                                     new JProperty("refunded",       known.Refunded > 0),
                                                                     new JProperty("refund_pending", false),
                                                                     new JProperty("wired",          false),
                                                                     new JProperty("refund_amount",  $"EUR:{known.Refunded.ToString(CultureInfo.InvariantCulture)}"),
                                                                     new JProperty("contract_terms", terms)));
                    }

                    return Answer(HttpStatusCode.OK, new JObject(new JProperty("order_status",          "unpaid"),
                                                                 new JProperty("taler_pay_uri",         $"taler://pay/taler.gef.example/instances/de-gef/{known.ID}/?c={known.Token}"),
                                                                 new JProperty("proto_contract_terms",  known.ContractTerms.DeepClone()),
                                                                 new JProperty("order_status_url",      $"{MerchantURL}orders/{known.ID}")));

                }

                #endregion

                #region POST private/orders/{id}/refund

                if (Request.Method == HttpMethod.Post && parts.Length == 2 && parts[1] == "refund")
                {

                    if (!known.Paid.Task.IsCompleted)
                        return Answer(HttpStatusCode.Conflict, new JObject(new JProperty("code", 2201), new JProperty("hint", "unpaid")));

                    var body   = JObject.Parse(await Request.Content!.ReadAsStringAsync(CancellationToken));
                    var amount = Decimal.Parse(body["refund"]!.Value<String>()!.Split(':')[1], CultureInfo.InvariantCulture);
                    var total  = Decimal.Parse(known.ContractTerms["amount"]!.Value<String>()!.Split(':')[1], CultureInfo.InvariantCulture);

                    if (amount > total)
                        return Answer(HttpStatusCode.Conflict, new JObject(new JProperty("code", 2202), new JProperty("hint", "too much")));

                    known.Refunded = Math.Max(known.Refunded, amount);

                    return Answer(HttpStatusCode.OK, new JObject(new JProperty("taler_refund_uri", $"taler://refund/taler.gef.example/instances/de-gef/{known.ID}/"),
                                                                 new JProperty("h_contract",       "HC")));

                }

                #endregion

                return Answer(HttpStatusCode.MethodNotAllowed, new JObject(new JProperty("code", 4)));

            }

            private static HttpResponseMessage Answer(HttpStatusCode  StatusCode,
                                                      JObject         JSON)

                => new (StatusCode) {
                       Content = new StringContent(JSON.ToString(), Encoding.UTF8, "application/json")
                   };

        }

        #endregion

        #region (class) FakeChargingControl

        /// <summary>
        /// The charging station management system of DE*GEF, as the test plays it.
        /// </summary>
        private sealed class FakeChargingControl : IEVTalerChargingControl
        {

            public String?                                    Busy        { get; set; }
            public ConcurrentQueue<String>                    Calls       { get; } = new ();
            public ConcurrentDictionary<String, IEVTalerChargingEvents> Events { get; } = new ();
            public EVTalerSignedMeterValue?                   EndValue    { get; set; }

            public Task<String?> WhyNotStart(EVSE_Id EVSEId, CancellationToken CancellationToken)
                => Task.FromResult(Busy);

            public Task<String?> Start(EVSE_Id EVSEId, String SessionId, EVTalerChargingLimit Limit, IEVTalerChargingEvents Events, CancellationToken CancellationToken)
            {
                Calls.Enqueue($"start {EVSEId} {Limit}");
                this.Events[SessionId] = Events;
                return Task.FromResult<String?>(null);
            }

            public Task RaiseLimit(String SessionId, EVTalerChargingLimit Limit, CancellationToken CancellationToken)
            {
                Calls.Enqueue($"limit {Limit}");
                return Task.CompletedTask;
            }

            public Task Stop(String SessionId, CancellationToken CancellationToken)
            {
                Calls.Enqueue("stop");
                return Events[SessionId].Ended(SessionId, EndValue, EVTalerStopReasons.Driver);
            }

        }

        #endregion

        #region (class) Hub

        /// <summary>
        /// A POI server serving the history over HTTP with the offers of the backend of DE*GEF.
        /// </summary>
        private sealed class Hub : IAsyncDisposable
        {

            public RoamingNetworkHistory  History    { get; }
            public HTTPServer             Server     { get; }
            public POI_API                POIAPI     { get; }
            public FakeMerchant           Merchant   { get; }
            public FakeChargingControl    Control    { get; } = new ();
            public EVTalerStepBackend     Backend    { get; }
            public EVTalerOffersAPI       Offers     { get; }
            public HttpClient             Client     { get; }
            public ConcurrentQueue<String> Errors    { get; } = new ();

            private Hub(RoamingNetworkHistory History, HTTPServer Server, POI_API POIAPI, FakeMerchant Merchant)
            {

                this.History   = History;
                this.Server    = Server;
                this.POIAPI    = POIAPI;
                this.Merchant  = Merchant;

                var (privateKey, publicKey) = TestParties.Key(TestParties.Party(GEF));

                this.Backend   = new EVTalerStepBackend(RN,
                                                        GEF,
                                                        new TalerMerchantClient(URL.Parse(MerchantURL), AccessToken, Merchant),
                                                        Control,
                                                        VerifyMeterValue,
                                                        privateKey,
                                                        publicKey.Id,
                                                        COSEAlgorithm.ES256);

                this.Backend.OnError += (session, description) => Errors.Enqueue(description);

                this.Offers    = new EVTalerOffersAPI(POIAPI,
                                                      Backend,
                                                      [ History.Head.RoamingNetwork.TrustAnchor() ],
                                                      VerifyMeterValue);

                this.Client    = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{Server.TCPPort}/RNs/rn1/") };

            }

            public static async Task<Hub> Start(FakeMerchant? Merchant = null)
            {

                var history = NewHistory(DateTimeOffset.UtcNow);
                var server  = new HTTPServer(IPAddress: IPv4Address.Localhost, TCPPort: IPPort.Zero, AutoStart: true);
                var api     = new POI_API(server);

                api.AddRoamingNetworkHistory(history);

                await Task.CompletedTask;

                return new Hub(history, server, api, Merchant ?? new FakeMerchant());

            }

            public async Task<(HttpStatusCode Status, JObject JSON)> Ask(EVSE_Id EVSEId, String Body, String ContentType = "application/json")
            {
                using var response = await Client.PostAsync($"EVSEs/{EVSEId}/evTaler/sessions", new StringContent(Body, Encoding.UTF8, ContentType));
                return (response.StatusCode, (JObject) POIJSON.Read(await response.Content.ReadAsStringAsync()));
            }

            public Task<(HttpStatusCode Status, JObject JSON)> Ask(EVSE_Id EVSEId, Decimal Quantity = 5, String Unit = "kWh", String Exchange = EURExchangeKey, String? Cap = null)
            {

                var json = new JObject(new JProperty("exchange", Exchange),
                                       new JProperty("step",     new JObject(new JProperty("quantity", Quantity.ToString(CultureInfo.InvariantCulture)),
                                                                             new JProperty("unit",     Unit))));

                if (Cap is not null)
                    json.Add("cap", Cap);

                return Ask(EVSEId, json.ToString());

            }

            /// <summary>
            /// The events after the given number, waiting up to the given time for one.
            /// </summary>
            public async Task<(HttpStatusCode Status, JObject JSON)> Events(String SessionId, String? Secret, UInt64 After = 0, Int32 TimeoutMs = 0)
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, $"evTaler/sessions/{SessionId}/events?after={After}&timeout_ms={TimeoutMs}");
                if (Secret is not null)
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Secret);
                using var response = await Client.SendAsync(request);
                return (response.StatusCode, (JObject) POIJSON.Read(await response.Content.ReadAsStringAsync()));
            }

            /// <summary>
            /// Read the events after those seen until one of the given type comes, at most a few seconds; all events read
            /// so far, added to those seen.
            /// </summary>
            public async Task<List<JObject>> Until(String SessionId, String Secret, String Type, List<JObject> Seen)
            {

                var from      = Seen.Count;
                var deadline  = DateTimeOffset.UtcNow.AddSeconds(15);

                while (DateTimeOffset.UtcNow < deadline)
                {

                    if (Seen.Skip(from).Any(json => json["type"]!.Value<String>() == Type))
                        return Seen;

                    var (status, json) = await Events(SessionId, Secret, (UInt64) Seen.Count, 2000);

                    Assert.That(status, Is.EqualTo(HttpStatusCode.OK), json.ToString());

                    Seen.AddRange(((JArray) json["events"]!).OfType<JObject>());

                }

                Assert.Fail($"No '{Type}' event came: {String.Join(", ", Seen.Select(json => json["type"]))}; errors: {String.Join(" | ", Errors)}");
                return Seen;

            }

            public async Task Stop(String SessionId, String Secret)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, $"evTaler/sessions/{SessionId}/stop");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Secret);
                using var response = await Client.SendAsync(request);
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Accepted));
            }

            public VerifiedChargingStation Station()
            {
                Assert.That(History.TryGetChargingStationProof(E1, out var proof, out var proofError), Is.True, proofError);
                Assert.That(VerifiedChargingStation.TryVerify(History.Head.RoamingNetwork.TrustAnchor(), proof!, out var station, out var verifyError, EVSEId: E1.ToString()), Is.True, verifyError);
                return station!;
            }

            public async ValueTask DisposeAsync()
            {
                Client.Dispose();
                await POIAPI.DisposeAsync();
                await Server.DisposeAsync();
            }

        }

        #endregion


        #region OffersURL_is_written_as_read_and_left_out_when_none()

        /// <summary>
        /// The offersURL of a merchant: written as read, HTTPS and ending in '/', left out when none is given, so that a
        /// merchant without it is written as before, byte for byte.
        /// </summary>
        [Test]
        public void OffersURL_is_written_as_read_and_left_out_when_none()
        {

            var exchange  = new EVTalerExchange(EURExchangeKey, URL.Parse(EURExchangeURL), Currency.EUR, new ImmutableI18NString([ new I18NPair(Languages.en, "Example Exchange") ]));
            var without   = new EVTalerMerchant(URL.Parse(MerchantURL), MerchantKey, [ exchange ]);
            var with      = new EVTalerMerchant(URL.Parse(MerchantURL), MerchantKey, [ exchange ], URL.Parse("https://poi.gef.example/RNs/rn1/"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(without.ToJSON().ToString(Newtonsoft.Json.Formatting.None),
                            Is.EqualTo("{\"@context\":\"https://open.charging.cloud/contexts/wwcp/roamingNetwork/evTalerMerchant/v1\"," +
                                       "\"merchantBaseURL\":\"https://taler.gef.example/instances/de-gef/\"," +
                                       "\"merchantPublicKey\":\"" + MerchantKey + "\"," +
                                       "\"exchanges\":[{\"masterPublicKey\":\"" + EURExchangeKey + "\",\"baseURL\":\"https://exchange.example.eu/\",\"currency\":\"EUR\",\"name\":{\"en\":\"Example Exchange\"}}]}"));

                Assert.That(with.ToJSON()["offersURL"]?.Value<String>(),  Is.EqualTo("https://poi.gef.example/RNs/rn1/"));

                Assert.That(EVTalerMerchant.TryParse(with.ToJSON(), out var read, out var error),  Is.True, error);
                Assert.That(read!.OffersURL?.ToString(),                                         Is.EqualTo("https://poi.gef.example/RNs/rn1/"));
                Assert.That(read.ToJSON().ToString(),                                             Is.EqualTo(with.ToJSON().ToString()));

                Assert.That(EVTalerMerchant.TryParse(without.ToJSON(), out var readWithout, out error),  Is.True, error);
                Assert.That(readWithout!.OffersURL,                                                     Is.Null);

                foreach (var invalid in new JToken[] { "http://poi.gef.example/RNs/rn1/", "https://poi.gef.example/RNs/rn1", JValue.CreateNull(), 42 })
                {
                    var json = with.ToJSON();
                    json["offersURL"] = invalid;
                    Assert.That(EVTalerMerchant.TryParse(json, out _, out _), Is.False, $"offersURL {invalid.ToString(Newtonsoft.Json.Formatting.None)} is refused");
                }

                Assert.That(() => new EVTalerMerchant(URL.Parse(MerchantURL), MerchantKey, [ exchange ], URL.Parse("https://poi.gef.example/RNs/rn1")),
                            Throws.ArgumentException);

            }

        }

        #endregion

        #region A_session_is_paid_in_steps_charged_and_settled()

        /// <summary>
        /// A driver asks for a session in steps of 5 kWh. Step 1 is offered, 0.50 € flat fee and 5 kWh at 0.65 €/kWh; paid,
        /// charging starts limited to 5 kWh; the station gives its signed start value; at 4 kWh step 2 is offered, naming
        /// the start value; paid, the limit is raised to 10 kWh; the driver stops at 7.5 kWh, the station gives its signed
        /// end value; the session costs 5.38 €, 7.00 € were paid, 1.62 € are refunded from step 2. The app checks every
        /// step and the settlement statement as phase 2 does.
        /// </summary>
        [Test]
        public async Task A_session_is_paid_in_steps_charged_and_settled()
        {

            await using var hub = await Hub.Start();

            var (status, started) = await hub.Ask(E1);

            Assert.That(status, Is.EqualTo(HttpStatusCode.Created), started.ToString());

            var sessionId  = started["sessionId"]!.Value<String>()!;
            var secret     = started["secret"]!.Value<String>()!;
            var offer1     = (JObject) started["offer"]!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(started["events"]!.Value<String>(),            Is.EqualTo($"/RNs/rn1/evTaler/sessions/{sessionId}/events"));
                Assert.That(offer1["step"]!.Value<Int32>(),                Is.EqualTo(1));
                Assert.That(offer1["amount"]!.Value<String>(),             Is.EqualTo("EUR:3.75"));
                Assert.That(offer1["talerPayURI"]!.Value<String>(),        Does.StartWith("taler://pay/taler.gef.example/instances/de-gef/"));
                Assert.That(Convert.FromBase64String(Base64Of(sessionId)), Has.Length.EqualTo(16));
                Assert.That(Convert.FromBase64String(Base64Of(secret)),    Has.Length.EqualTo(32));
            }

            var seen = new List<JObject>();

            hub.Merchant.Pay(offer1["orderId"]!.Value<String>()!);
            await hub.Until(sessionId, secret, "paid", seen);

            await WaitFor(() => hub.Control.Events.ContainsKey(sessionId));

            var events = hub.Control.Events[sessionId];
            var t0     = DateTimeOffset.UtcNow.AddMinutes(-30);

            // Delivered before the station gave its signed start value: step 2 waits for it.
            await events.Delivered(sessionId, 4m);
            await Task.Delay(300);
            Assert.That((await hub.Events(sessionId, secret, 2)).JSON["events"],  Is.Empty);

            await events.Started  (sessionId, MeterValue(1000m, t0));

            await hub.Until(sessionId, secret, "offer", seen);

            var offer2 = seen.Last(json => json["type"]!.Value<String>() == "offer");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seen.Select(json => json["type"]!.Value<String>()),  Is.EqualTo(new[] { "offer", "paid", "meterValue", "offer" }));
                Assert.That(seen.Select(json => json["seq"]!.Value<Int32>()),     Is.EqualTo(new[] { 1, 2, 3, 4 }));
                Assert.That(offer2["step"]!.Value<Int32>(),                       Is.EqualTo(2));
                Assert.That(offer2["amount"]!.Value<String>(),                    Is.EqualTo("EUR:3.25"));
                Assert.That(offer2["covers"]!.ToString(Newtonsoft.Json.Formatting.None), Does.Contain("\"from\":\"5\"").And.Contain("\"to\":\"10\""));
            }

            hub.Merchant.Pay(offer2["orderId"]!.Value<String>()!);
            await hub.Until(sessionId, secret, "paid", seen);
            await WaitFor(() => hub.Control.Calls.Contains("limit 10 kWh"));

            hub.Control.EndValue = MeterValue(1007.5m, t0.AddMinutes(25));
            await hub.Stop(sessionId, secret);

            await hub.Until(sessionId, secret, "settlement", seen);

            var settlement  = seen.Last();
            var statement   = (JObject) settlement["statement"]!;
            var refunds     = (JArray)  settlement["refunds"]!;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seen.Select(json => json["type"]!.Value<String>()),  Is.EqualTo(new[] { "offer", "paid", "meterValue", "offer", "paid", "meterValue", "stopped", "settlement" }));
                Assert.That(hub.Control.Calls,                                    Is.EqualTo(new[] { "start DE*GEF*E1*1 5 kWh", "limit 10 kWh", "stop" }));
                Assert.That(seen[6]["reason"]!.Value<String>(),                   Is.EqualTo(EVTalerStopReasons.Driver));
                Assert.That(statement["price"]!.Value<String>(),                  Is.EqualTo("5.38"));
                Assert.That(statement["paid"]!.Value<String>(),                   Is.EqualTo("7.00"));
                Assert.That(statement["refunded"]!.Value<String>(),               Is.EqualTo("1.62"));
                Assert.That(refunds,                                              Has.Count.EqualTo(1));
                Assert.That(refunds[0]["orderId"]!.Value<String>(),               Is.EqualTo(offer2["orderId"]!.Value<String>()));
                Assert.That(refunds[0]["amount"]!.Value<String>(),                Is.EqualTo("EUR:1.62"));
                Assert.That(refunds[0]["talerRefundURI"]!.Value<String>(),        Does.StartWith("taler://refund/"));
                Assert.That(hub.Merchant.Orders[offer2["orderId"]!.Value<String>()!].Refunded,  Is.EqualTo(1.62m));
                Assert.That(hub.Errors,                                           Is.Empty);
            }

            #region The app checks every step and the settlement, as phase 2 does

            var station = hub.Station();
            var steps   = new List<EVTalerVerifiedStep>();

            foreach (var offer in seen.Where(json => json["type"]!.Value<String>() == "offer"))
            {
                Assert.That(EVTalerContractTerms.TryParse(offer["contractTerms"], out var terms, out var termsError),  Is.True, termsError);
                Assert.That(EVTalerPayURI.TryParse(offer["talerPayURI"]!.Value<String>(), out var payURI, out var uriError),  Is.True, uriError);
                Assert.That(terms!.TryVerify(station, hub.History.PriceStatementsOf(E1), steps.LastOrDefault(), VerifyMeterValue, out var verified, out var verifyError, payURI),  Is.True, verifyError);
                steps.Add(verified!);
            }

            Assert.That(EVTalerSettlement.TryParse(statement, out var parsed, out var parseError),                       Is.True, parseError);
            Assert.That(parsed!.TryVerify(station, steps, VerifyMeterValue, out var verification, out var settleError),  Is.True, settleError);

            #endregion

            Assert.That((await hub.Events(sessionId, secret, 8)).JSON["ended"]!.Value<Boolean>(),  Is.True);

        }

        #endregion

        #region Sessions_are_refused_where_they_are_not_made()

        /// <summary>
        /// 415 without JSON, 400 for a request it does not read, 404 where the EVSE offers no EV Taler, 422 for a step or an
        /// exchange not accepted, with the steps that are, 409 where the EVSE takes no session now or has one already; and a
        /// session is read with its secret only.
        /// </summary>
        [Test]
        public async Task Sessions_are_refused_where_they_are_not_made()
        {

            await using var hub = await Hub.Start();

            using (Assert.EnterMultipleScope())
            {

                Assert.That((await hub.Ask(E1, "{}", "text/plain")).Status,                                     Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
                Assert.That((await hub.Ask(E1, "{\"exchange\":\"x\"}")).Status,                                 Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That((await hub.Ask(E1, Quantity: 0)).Status,                                            Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That((await hub.Ask(EVSE_Id.Parse("DE*GEF*E9*9"))).Status,                               Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That((await hub.Ask(E1, Exchange: OtherKey)).Status,                                     Is.EqualTo(HttpStatusCode.UnprocessableEntity));

                var (status, json) = await hub.Ask(E1, Quantity: 3);

                Assert.That(status,                                                                              Is.EqualTo(HttpStatusCode.UnprocessableEntity));
                Assert.That(((JArray) json["acceptedSteps"]!).Select(step => step["unit"]!.Value<String>()),    Is.All.EqualTo("kWh"));

                Assert.That((await hub.Ask(E1, Quantity: 15, Unit: "min")).Status,                              Is.EqualTo(HttpStatusCode.UnprocessableEntity));
                Assert.That((await hub.Ask(E1, Cap: "2.00")).Status,                                            Is.EqualTo(HttpStatusCode.UnprocessableEntity));

                hub.Control.Busy = "No vehicle is plugged in.";
                Assert.That((await hub.Ask(E1)).Status,                                                         Is.EqualTo(HttpStatusCode.Conflict));
                hub.Control.Busy = null;

                hub.Merchant.Down = true;
                Assert.That((await hub.Ask(E1)).Status,                                                         Is.EqualTo(HttpStatusCode.BadGateway));
                Assert.That(hub.Offers.Sessions,                                                                Is.Empty);
                hub.Merchant.Down = false;

                (status, json) = await hub.Ask(E1);
                Assert.That(status,                                                                              Is.EqualTo(HttpStatusCode.Created), json.ToString());
                Assert.That((await hub.Ask(E1)).Status,                                                         Is.EqualTo(HttpStatusCode.Conflict));

                hub.Offers.MaxOpenSessionsPerClient = 1;
                Assert.That((await hub.Ask(E2)).Status,                                                         Is.EqualTo(HttpStatusCode.TooManyRequests));

                var sessionId = json["sessionId"]!.Value<String>()!;
                Assert.That((await hub.Events(sessionId, null)).Status,                                         Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That((await hub.Events(sessionId, "wrong")).Status,                                      Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That((await hub.Events("unknown", "wrong")).Status,                                      Is.EqualTo(HttpStatusCode.Unauthorized));
                Assert.That((await hub.Events(sessionId, json["secret"]!.Value<String>())).Status,              Is.EqualTo(HttpStatusCode.OK));

            }

        }

        #endregion

        #region An_offer_of_another_merchant_is_not_relayed()

        /// <summary>
        /// A backend whose merchant instance signs with another key than the operator's evTaler names: its offer of step 1
        /// fails the check of the server, is not relayed, and the session is not started (502).
        /// </summary>
        [Test]
        public async Task An_offer_of_another_merchant_is_not_relayed()
        {

            await using var hub = await Hub.Start(new FakeMerchant(OtherKey));

            var (status, json) = await hub.Ask(E1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(status,                                    Is.EqualTo(HttpStatusCode.BadGateway), json.ToString());
                Assert.That(json["description"]!.Value<String>(),      Does.Contain("refused").And.Contain(OtherKey));
                Assert.That(json["sessionId"],                         Is.Null);
                Assert.That(hub.Offers.Sessions,                       Is.Empty);
            }

        }

        #endregion

        #region A_step_not_paid_ends_the_session()

        /// <summary>
        /// Step 1 not paid by its deadline: the session ends unpaid, nothing is charged and nothing refunded, and the EVSE
        /// takes the next session.
        /// </summary>
        [Test]
        public async Task A_step_not_paid_ends_the_session()
        {

            await using var hub = await Hub.Start();

            hub.Backend.FirstPayDeadline = TimeSpan.FromSeconds(2);

            var (status, started) = await hub.Ask(E1);

            Assert.That(status, Is.EqualTo(HttpStatusCode.Created), started.ToString());

            var seen = await hub.Until(started["sessionId"]!.Value<String>()!, started["secret"]!.Value<String>()!, "settlement", []);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seen.Select(json => json["type"]!.Value<String>()),   Is.EqualTo(new[] { "offer", "unpaid", "stopped", "settlement" }));
                Assert.That(seen[2]["reason"]!.Value<String>(),                    Is.EqualTo(EVTalerStopReasons.Unpaid));
                Assert.That(seen[3]["statement"],                                  Is.Null);
                Assert.That((JArray) seen[3]["refunds"]!,                          Is.Empty);
                Assert.That(hub.Control.Calls,                                     Is.Empty);
                Assert.That((await hub.Ask(E1)).Status,                            Is.EqualTo(HttpStatusCode.Created));
            }

        }

        #endregion

        #region Without_the_end_value_every_step_is_refunded_in_full()

        /// <summary>
        /// The station ends the session without a signed meter value at the end: the session has no price by the tariff,
        /// there is no settlement statement, and every step paid is refunded in full (concepts/EVTaler.md, 5.4).
        /// </summary>
        [Test]
        public async Task Without_the_end_value_every_step_is_refunded_in_full()
        {

            await using var hub = await Hub.Start();

            var (_, started)  = await hub.Ask(E1);
            var sessionId     = started["sessionId"]!.Value<String>()!;
            var secret        = started["secret"]!.Value<String>()!;
            var orderId       = started["offer"]!["orderId"]!.Value<String>()!;

            hub.Merchant.Pay(orderId);
            var seen = await hub.Until(sessionId, secret, "paid", []);

            await WaitFor(() => hub.Control.Events.ContainsKey(sessionId));

            await hub.Control.Events[sessionId].Started(sessionId, MeterValue(2000m, DateTimeOffset.UtcNow.AddMinutes(-5)));
            await hub.Control.Events[sessionId].Ended  (sessionId, null, EVTalerStopReasons.Limit);

            await hub.Until(sessionId, secret, "settlement", seen);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seen.Select(json => json["type"]!.Value<String>()),   Is.EqualTo(new[] { "offer", "paid", "meterValue", "stopped", "settlement" }));
                Assert.That(seen[3]["reason"]!.Value<String>(),                    Is.EqualTo(EVTalerStopReasons.Limit));
                Assert.That(seen[4]["statement"],                                  Is.Null);
                Assert.That(seen[4]["refunds"]![0]!["amount"]!.Value<String>(),    Is.EqualTo("EUR:3.75"));
                Assert.That(hub.Merchant.Orders[orderId].Refunded,                 Is.EqualTo(3.75m));
                Assert.That(hub.Errors,                                            Has.Some.Contains("refunded in full"));
            }

        }

        #endregion


        #region A_later_offer_failing_a_check_is_not_relayed_and_ends_the_session(What)

        /// <summary>
        /// The merchant backend makes step 2 at another price than the tariff's, or naming another signed meter value at
        /// the start than the station gave: the server does not relay it, tells "refused", and stops charging; the session
        /// is settled by what step 1 paid for.
        /// </summary>
        [TestCase("amount",          "9.99")]
        [TestCase("startMeterValue", "another signed meter value at the start")]
        public async Task A_later_offer_failing_a_check_is_not_relayed_and_ends_the_session(String What, String Refusal)
        {

            await using var hub = await Hub.Start();

            var (_, started)  = await hub.Ask(E1);
            var sessionId     = started["sessionId"]!.Value<String>()!;
            var secret        = started["secret"]!.Value<String>()!;
            var orderId       = started["offer"]!["orderId"]!.Value<String>()!;
            var seen          = new List<JObject>();
            var t0            = DateTimeOffset.UtcNow.AddMinutes(-20);

            hub.Merchant.Pay(orderId);
            await hub.Until(sessionId, secret, "paid", seen);
            await WaitFor(() => hub.Control.Events.ContainsKey(sessionId));

            hub.Merchant.Tamper   = What == "amount"
                                        ? terms => terms["amount"] = "EUR:9.99"
                                        : terms => terms["extra"]!["wwcp"]!["startMeterValue"] = MeterValue(3000.5m, t0).ToJSON();
            hub.Control.EndValue  = MeterValue(3004.2m, t0.AddMinutes(10));

            await hub.Control.Events[sessionId].Started  (sessionId, MeterValue(3000m, t0));
            await hub.Control.Events[sessionId].Delivered(sessionId, 4.2m);

            await hub.Until(sessionId, secret, "settlement", seen);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seen.Select(json => json["type"]!.Value<String>()),   Is.EqualTo(new[] { "offer", "paid", "meterValue", "refused", "meterValue", "stopped", "settlement" }));
                Assert.That(seen[3]["step"]!.Value<Int32>(),                       Is.EqualTo(2));
                Assert.That(seen[3]["description"]!.Value<String>(),               Does.Contain(Refusal));
                Assert.That(hub.Control.Calls,                                     Is.EqualTo(new[] { "start DE*GEF*E1*1 5 kWh", "stop" }));
                Assert.That(seen[6]["statement"]!["price"]!.Value<String>(),       Is.EqualTo("3.23"));
                Assert.That(seen[6]["refunds"]![0]!["amount"]!.Value<String>(),    Is.EqualTo("EUR:0.52"));
                Assert.That(hub.Merchant.Orders[orderId].Refunded,                 Is.EqualTo(0.52m));
            }

        }

        #endregion

        #region The_cap_of_the_driver_ends_the_steps()

        /// <summary>
        /// A cap of 5.00 EUR: step 1 costs 3.75 EUR, step 2 would bring it to 7.00 EUR, and is not offered; the station ends
        /// at the limit of step 1, which is told as the cap.
        /// </summary>
        [Test]
        public async Task The_cap_of_the_driver_ends_the_steps()
        {

            await using var hub = await Hub.Start();

            var (status, started) = await hub.Ask(E1, Cap: "5.00");

            Assert.That(status, Is.EqualTo(HttpStatusCode.Created), started.ToString());

            var sessionId     = started["sessionId"]!.Value<String>()!;
            var secret        = started["secret"]!.Value<String>()!;
            var seen          = new List<JObject>();
            var t0            = DateTimeOffset.UtcNow.AddMinutes(-20);

            hub.Merchant.Pay(started["offer"]!["orderId"]!.Value<String>()!);
            await hub.Until(sessionId, secret, "paid", seen);
            await WaitFor(() => hub.Control.Events.ContainsKey(sessionId));

            await hub.Control.Events[sessionId].Started  (sessionId, MeterValue(500m, t0));
            await hub.Control.Events[sessionId].Delivered(sessionId, 4.5m);
            await hub.Control.Events[sessionId].Ended    (sessionId, MeterValue(505m, t0.AddMinutes(12)), EVTalerStopReasons.Limit);

            await hub.Until(sessionId, secret, "settlement", seen);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(seen.Select(json => json["type"]!.Value<String>()),   Is.EqualTo(new[] { "offer", "paid", "meterValue", "meterValue", "stopped", "settlement" }));
                Assert.That(seen[4]["reason"]!.Value<String>(),                    Is.EqualTo(EVTalerStopReasons.Cap));
                Assert.That(seen[5]["statement"]!["price"]!.Value<String>(),       Is.EqualTo("3.75"));
                Assert.That((JArray) seen[5]["refunds"]!,                          Is.Empty);
                Assert.That(hub.Merchant.Orders,                                   Has.Count.EqualTo(1));
            }

        }

        #endregion


        #region (static) WaitFor(Condition)

        private static async Task WaitFor(Func<Boolean> Condition)
        {

            var deadline = DateTimeOffset.UtcNow.AddSeconds(10);

            while (!Condition())
            {
                if (DateTimeOffset.UtcNow > deadline)
                    Assert.Fail("A condition did not come true in time.");
                await Task.Delay(20);
            }

        }

        #endregion

        #region (static) Base64Of(Base64URL)

        private static String Base64Of(String Base64URL)
        {
            var text = Base64URL.Replace('-', '+').Replace('_', '/');
            return text + new String('=', (4 - text.Length % 4) % 4);
        }

        #endregion

    }

}
