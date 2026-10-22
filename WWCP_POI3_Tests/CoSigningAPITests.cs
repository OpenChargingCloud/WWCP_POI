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
using System.Text;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

using cloud.charging.open.protocols.WWCP.POI.tests.Proofs;
using cloud.charging.open.protocols.WWCP.POI.tests.WebSockets;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Co-signing over HTTP (SPECIFICATION.md, section 4.2): ~/history/changeSets/pending.
    /// </summary>
    [TestFixture]
    public class CoSigningAPITests
    {

        #region Data

        private static readonly DateTimeOffset  T0       = DateTimeOffset.Parse("2026-10-22T12:00:00Z");
        private static readonly String[]        Holders  = [ "Alpha", "Beta", "Gamma" ];

        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private RoamingNetwork?         network;
        private RoamingNetworkHistory?  history;
        private HttpClient?             httpClient;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            httpServer  = new HTTPServer(
                              IPAddress:  IPv4Address.Localhost,
                              TCPPort:    IPPort.Zero,
                              AutoStart:  true
                          );

            poiAPI      = new POI_API(httpServer) {
                              AuthorizeChangeSetSubmission = (request, changeSet) => true
                          };

            var plain         = StationProofTests.NewNetwork();
            var json          = plain.ToJSON(IncludeETags: false);
            var operatorJSON  = (JObject) json[RoamingNetwork.PropertyKeys.Operator]!;

            // The signing keys of each holder, certified by its identity key.
            operatorJSON.Remove(POIPartyKeys.SigningKeysProperty);
            operatorJSON[RoamingNetworkSigningKeyCertificate.JSONProperty] = new JArray(Holders.Select(holder => RoamingNetworkSigningKeyCertificate.Create(TestParties.QuorumKey(plain.Id, holder, POIKeyKind.Identity).Private,
                                                                                                                             COSEAlgorithm.ES512,
                                                                                                                             TestParties.OperatorId(plain.Id),
                                                                                                                             TestParties.QuorumKey(plain.Id, holder, POIKeyKind.Signing).Public,
                                                                                                                             DateTimeOffset.Parse("2026-01-01T00:00:00Z")).ToJSON(Embedded: true)));
            operatorJSON[POIPartyKeys.IdentityKeysProperty]  = new JArray(Holders.Select(holder => TestParties.QuorumKey(plain.Id, holder, POIKeyKind.Identity).Public.ToJSON(Embedded: true)));
            operatorJSON[POIPartyKeys.MinConsensusProperty]  = 2;

            network     = RoamingNetwork.Parse(json);
            history     = RoamingNetworkHistory.Start(network);

            poiAPI.AddRoamingNetworkHistory(history);

            httpClient  = new HttpClient {
                              BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/RNs/{network.Id}/history/")
                          };

        }

        [TearDown]
        public async Task TearDown()
        {

            httpClient?.Dispose();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

        }

        #endregion


        #region (private) SignedBy(ChangeSet, Holders)

        private RoamingNetworkChangeSet SignedBy(RoamingNetworkChangeSet  ChangeSet,
                                                 params String[]          Holders)
        {

            foreach (var holder in Holders)
            {
                var (privateKey, publicKey) = TestParties.QuorumKey(network!.Id, holder, POIKeyKind.Signing);
                ChangeSet = ChangeSet.Sign(privateKey, publicKey.Id, COSEAlgorithm.ES256);
            }

            return ChangeSet;

        }

        #endregion

        #region (private) Send(Method, Path, JSON = null)

        private async Task<(HttpStatusCode StatusCode, JObject JSON)> Send(HttpMethod  Method,
                                                                           String      Path,
                                                                           JToken?     JSON   = null)
        {

            using var request   = new HttpRequestMessage(Method, Path) {
                                      Content = JSON is not null ? new StringContent(JSON.ToString(), Encoding.UTF8, "application/json") : null
                                  };

            using var response  = await httpClient!.SendAsync(request);

            return (response.StatusCode, JObject.Parse(await response.Content.ReadAsStringAsync()));

        }

        #endregion


        #region Holders_sign_a_change_set_waiting_over_HTTP()

        [Test]
        public async Task Holders_sign_a_change_set_waiting_over_HTTP()
        {

            var changeSet  = network!.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "quorum") ], CreatedAt: T0);

            var alpha      = await Send(HttpMethod.Post, "changeSets/pending", SignedBy(changeSet, "Alpha").ToJSON());
            var list       = await Send(HttpMethod.Get,  "changeSets/pending");
            var one        = await Send(HttpMethod.Get,  $"changeSets/pending/{changeSet.Id}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alpha.StatusCode,                                     Is.EqualTo(HttpStatusCode.Accepted), alpha.JSON.ToString());
                Assert.That(alpha.JSON["status"]?.Value<String>(),                Is.EqualTo("pending"));
                Assert.That(alpha.JSON["missing"]?.Count(),                       Is.EqualTo(1));
                Assert.That(list.JSON["pendingChangeSets"]?.Count(),              Is.EqualTo(1));
                Assert.That(one.StatusCode,                                       Is.EqualTo(HttpStatusCode.OK));
                Assert.That(one.JSON["changeSet"]?["@id"]?.Value<String>(),       Is.EqualTo(changeSet.Id.ToString()));
            }

            var beta       = await Send(HttpMethod.Post, $"changeSets/pending/{changeSet.Id}/signatures",
                                        new JObject(new JProperty("signatures", new JArray(SignedBy(changeSet, "Beta").Signatures.Select(signature => signature.ToJSON(Embedded: true))))));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(beta.StatusCode,                                      Is.EqualTo(HttpStatusCode.Created), beta.JSON.ToString());
                Assert.That(beta.JSON["status"]?.Value<String>(),                 Is.EqualTo("committed"));
                Assert.That(beta.JSON["commitId"]?.Value<String>(),               Is.EqualTo(history!.Head.Commit.Id.ToString()));
                Assert.That(history.Head.RoamingNetwork.DataSource,               Is.EqualTo("quorum"));
                Assert.That((await Send(HttpMethod.Get, $"changeSets/pending/{changeSet.Id}")).StatusCode,  Is.EqualTo(HttpStatusCode.NotFound));
            }

        }

        #endregion

        #region An_owner_withdraws_a_change_set_over_HTTP()

        [Test]
        public async Task An_owner_withdraws_a_change_set_over_HTTP()
        {

            var changeSet   = network!.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "quorum") ], CreatedAt: T0);
            var unsigned    = await Send(HttpMethod.Post, "changeSets/pending", changeSet.ToJSON());

            Assert.That((await Send(HttpMethod.Post, "changeSets/pending", SignedBy(changeSet, "Alpha").ToJSON())).StatusCode,  Is.EqualTo(HttpStatusCode.Accepted));

            var (privateKey, publicKey)  = TestParties.QuorumKey(network.Id, "Gamma", POIKeyKind.Signing);
            var withdrawal  = RoamingNetworkChangeSetWithdrawal.Create(changeSet, privateKey, publicKey.Id, COSEAlgorithm.ES256, T0.AddMinutes(1));

            var withdrawn   = await Send(HttpMethod.Delete, $"changeSets/pending/{changeSet.Id}", withdrawal.ToJSON());
            var again       = await Send(HttpMethod.Delete, $"changeSets/pending/{changeSet.Id}", withdrawal.ToJSON());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(unsigned.StatusCode,                        Is.EqualTo(HttpStatusCode.UnprocessableEntity), unsigned.JSON.ToString());
                Assert.That(withdrawn.StatusCode,                       Is.EqualTo(HttpStatusCode.OK), withdrawn.JSON.ToString());
                Assert.That(withdrawn.JSON["status"]?.Value<String>(),  Is.EqualTo("withdrawn"));
                Assert.That(again.StatusCode,                           Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(history!.PendingChangeSets,                 Is.Empty);
            }

        }

        #endregion

        #region Holders_sign_a_change_set_waiting_over_the_WebSocket()

        [Test]
        public async Task Holders_sign_a_change_set_waiting_over_the_WebSocket()
        {

            await using var client = await HistoryWebSocketTests.TestClient.Connect(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/RNs/{network!.Id}/history/ws"));

            await client.Request("1", "subscribe", new JObject(new JProperty("topics", new JArray(POI_API.PendingChangeSetsTopic))));

            var changeSet  = network.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "quorum") ], CreatedAt: T0);
            var alpha      = await client.Request("2", "coSignChangeSet", new JObject(new JProperty("changeSet", SignedBy(changeSet, "Alpha").ToJSON(Embedded: true))));
            var (waiting, _) = await client.NextPush();
            var list       = await client.Request("3", "pendingChangeSets");
            var beta       = await client.Request("4", "addPendingSignatures", new JObject(new JProperty("changeSetId", changeSet.Id.ToString()),
                                                                                           new JProperty("signatures",  new JArray(SignedBy(changeSet, "Beta").Signatures.Select(signature => signature.ToJSON(Embedded: true))))));
            var (committed, _) = await client.NextPush();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(alpha.Payload!["status"]!.Value<String>(),       Is.EqualTo("pending"), alpha.ToString());
                Assert.That(waiting.Action,                                  Is.EqualTo("pendingChangeSet"));
                Assert.That(waiting.Payload!["status"]!.Value<String>(),     Is.EqualTo("pending"));
                Assert.That(list.Payload!["pendingChangeSets"]!.Count(),     Is.EqualTo(1));
                Assert.That(beta.Payload!["status"]!.Value<String>(),        Is.EqualTo("committed"), beta.ToString());
                Assert.That(committed.Payload!["status"]!.Value<String>(),   Is.EqualTo("committed"));
                Assert.That(committed.Payload["commitId"]!.Value<String>(),  Is.EqualTo(history!.Head.Commit.Id.ToString()));
            }

        }

        #endregion

        #region Without_authorization_nothing_waits()

        [Test]
        public async Task Without_authorization_nothing_waits()
        {

            poiAPI!.AuthorizeChangeSetSubmission = null;

            var changeSet  = network!.CreateChangeSet([ RoamingNetworkChange.UpdateProperty(network, "dataSource", null, "quorum") ], CreatedAt: T0);
            var result     = await Send(HttpMethod.Post, "changeSets/pending", SignedBy(changeSet, "Alpha").ToJSON());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.StatusCode,           Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(history!.PendingChangeSets,  Is.Empty);
            }

        }

        #endregion

    }

}
