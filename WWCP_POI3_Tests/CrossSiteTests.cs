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
using System.Net.WebSockets;
using System.Text;

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// What a page of another site can make a browser send does not reach what changes something (POI3-ISSUES.md S23):
    /// a body neither JSON nor CBOR is refused with 415, and the WebSocket of a history is refused with 403 to an Origin
    /// that is neither the server's own nor allowed. Programs, which send no Origin, are not asked.
    /// </summary>
    [TestFixture]
    public class CrossSiteTests
    {

        #region Data

        private static readonly HttpMethod COMMIT = new ("COMMIT");

        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private RoamingNetworkHistory?  leader;
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

            leader      = RoamingNetworkHistory.Start(
                              new RoamingNetwork(
                                  Id:          RoamingNetwork_Id.Parse("rn1"),
                                  Name:        I18NString.Create(Languages.en, "Roaming Network 1"),
                                  DataSource:  "v0",
                                  Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")
                              ).WithKeys()
                          );

            poiAPI.AddRoamingNetworkHistory(leader);

            httpClient  = new HttpClient {
                              BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/RNs/rn1/history/")
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


        #region (private) ChangeSet(Value)

        private RoamingNetworkChangeSet ChangeSet(String Value)

            => leader!.Head.RoamingNetwork.CreateSignedChangeSet(
                   [ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, Value) ],
                   CreatedAt: DateTimeOffset.Parse("2026-10-11T13:00:00Z")
               );

        #endregion

        #region (private) Send(Method, Path, Content)

        private async Task<(HttpStatusCode StatusCode, String Text)> Send(HttpMethod    Method,
                                                                         String        Path,
                                                                         HttpContent?  Content)
        {

            using var request   = new HttpRequestMessage(Method, Path) { Content = Content };
            using var response  = await httpClient!.SendAsync(request);

            return (response.StatusCode, await response.Content.ReadAsStringAsync());

        }

        #endregion

        #region (private) Upgrade(Origin = null)

        /// <summary>
        /// Open the WebSocket of the history with the given Origin, or none: the status of the answer to the upgrade
        /// request, 101 where it was opened.
        /// </summary>
        private async Task<HttpStatusCode> Upgrade(String? Origin = null)
        {

            using var webSocket = new ClientWebSocket();

            webSocket.Options.CollectHttpResponseDetails = true;

            if (Origin is not null)
                webSocket.Options.SetRequestHeader("Origin", Origin);

            try
            {

                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));

                await webSocket.ConnectAsync(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/RNs/rn1/history/ws"), timeout.Token);

                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "done", timeout.Token);

                return HttpStatusCode.SwitchingProtocols;

            }
            catch (WebSocketException)
            {
                return webSocket.HttpStatusCode;
            }

        }

        #endregion


        #region A_change_set_neither_JSON_nor_CBOR_is_refused()

        /// <summary>
        /// A change set sent as text/plain, as a form of another site's page sends it without asking first, is refused
        /// with 415 and committed not - and so is one that says no type at all.
        /// </summary>
        [Test]
        public async Task A_change_set_neither_JSON_nor_CBOR_is_refused()
        {

            var json       = ChangeSet("v1").ToJSON().ToString();

            var plain      = await Send(COMMIT, "changeSets", new StringContent(json, Encoding.UTF8, "text/plain"));
            var form       = await Send(COMMIT, "changeSets", new StringContent(json, Encoding.UTF8, "application/x-www-form-urlencoded"));

            var untyped    = new ByteArrayContent(Encoding.UTF8.GetBytes(json));
            var none       = await Send(COMMIT, "changeSets", untyped);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(plain.StatusCode,                                         Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
                Assert.That(JObject.Parse(plain.Text)["description"]?.Value<String>(), Is.EqualTo("The request body must be application/json or application/cbor!"));
                Assert.That(form.StatusCode,                                          Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
                Assert.That(none.StatusCode,                                          Is.EqualTo(HttpStatusCode.UnsupportedMediaType));

                Assert.That(leader!.Head.Commit.Revision,                             Is.Zero, "a change set came through");

            }

        }

        #endregion

        #region Every_route_that_changes_something_refuses_text_plain()

        /// <summary>
        /// Every route that changes something - change sets, commits and their signatures, checkpoints and theirs, price
        /// statements, the change sets that wait for signatures, their signatures and their withdrawal - answers 415 to a
        /// body of text/plain, before it reads it.
        /// </summary>
        [TestCase("COMMIT",  "changeSets")]
        [TestCase("POST",    "commits")]
        [TestCase("POST",    "commits/0000000000000000000000000000000000000000000000000000000000000000/signatures")]
        [TestCase("POST",    "checkpoints")]
        [TestCase("POST",    "checkpoints/0000000000000000000000000000000000000000000000000000000000000000/signatures")]
        [TestCase("POST",    "priceStatements")]
        [TestCase("POST",    "changeSets/pending")]
        [TestCase("POST",    "changeSets/pending/0000000000000000000000000000000000000000000000000000000000000000/signatures")]
        [TestCase("DELETE",  "changeSets/pending/0000000000000000000000000000000000000000000000000000000000000000")]
        public async Task Every_route_that_changes_something_refuses_text_plain(String Method, String Path)
        {

            var answer = await Send(new HttpMethod(Method), Path, new StringContent("{}", Encoding.UTF8, "text/plain"));

            Assert.That(answer.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType), answer.Text);

        }

        #endregion

        #region JSON_with_its_parameters_and_CBOR_are_taken()

        /// <summary>
        /// JSON with a charset, a media type in capitals, and CBOR are taken as before.
        /// </summary>
        [Test]
        public async Task JSON_with_its_parameters_and_CBOR_are_taken()
        {

            var charset     = await Send(COMMIT, "changeSets", new StringContent(ChangeSet("v1").ToJSON().ToString(), Encoding.UTF8, "application/json"));

            var capitals    = new StringContent(ChangeSet("v2").ToJSON().ToString(), Encoding.UTF8);
            capitals.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("Application/JSON");
            var upper       = await Send(COMMIT, "changeSets", capitals);

            var cborContent = new ByteArrayContent(ChangeSet("v3").ToCBOR().ToByteArray(CBORWriterOptions.Canonical));
            cborContent.Headers.ContentType = System.Net.Http.Headers.MediaTypeHeaderValue.Parse("APPLICATION/CBOR");
            var cbor        = await Send(COMMIT, "changeSets", cborContent);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(charset.StatusCode,                       Is.EqualTo(HttpStatusCode.Created), charset.Text);
                Assert.That(upper.  StatusCode,                       Is.EqualTo(HttpStatusCode.Created), upper.  Text);
                Assert.That(cbor.   StatusCode,                       Is.EqualTo(HttpStatusCode.Created), cbor.   Text);
                Assert.That(leader!.Head.RoamingNetwork.DataSource,   Is.EqualTo("v3"));
            }

        }

        #endregion

        #region The_WebSocket_refuses_the_pages_of_other_sites()

        /// <summary>
        /// The WebSocket of a history opens to a program, which sends no Origin, and to a page of the server's own; a page
        /// of another site, and a sandboxed page ("null"), are refused with 403.
        /// </summary>
        [Test]
        public async Task The_WebSocket_refuses_the_pages_of_other_sites()
        {

            var program   = await Upgrade();
            var own       = await Upgrade($"http://127.0.0.1:{httpServer!.TCPPort}");
            var otherPort = await Upgrade($"http://127.0.0.1:{httpServer!.TCPPort.ToUInt16() + 1}");
            var other     = await Upgrade("https://evil.example");
            var sandboxed = await Upgrade("null");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(program,    Is.EqualTo(HttpStatusCode.SwitchingProtocols), "a program");
                Assert.That(own,        Is.EqualTo(HttpStatusCode.SwitchingProtocols), "a page of the server's own");
                Assert.That(otherPort,  Is.EqualTo(HttpStatusCode.Forbidden),          "a page of another port");
                Assert.That(other,      Is.EqualTo(HttpStatusCode.Forbidden),          "a page of another site");
                Assert.That(sandboxed,  Is.EqualTo(HttpStatusCode.Forbidden),          "a sandboxed page");
            }

        }

        #endregion

        #region The_WebSocket_opens_to_the_origins_allowed_and_the_host_a_proxy_names()

        /// <summary>
        /// The origins allowed open the WebSocket too, and so does a page of the name a proxy in front says the browser
        /// asked for - by its host alone, where the proxy names no port.
        /// </summary>
        [Test]
        public async Task The_WebSocket_opens_to_the_origins_allowed_and_the_host_a_proxy_names()
        {

            poiAPI!.AllowedOrigins  = [ "https://map.example.org" ];
            poiAPI!.HostAskedFor    = request => "hub.example.org";

            var allowed   = await Upgrade("https://map.example.org");
            var behind    = await Upgrade("https://hub.example.org");
            var other     = await Upgrade("https://evil.example");

            poiAPI!.HostAskedFor    = null;

            var noProxy   = await Upgrade("https://hub.example.org");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(allowed,  Is.EqualTo(HttpStatusCode.SwitchingProtocols), "an origin allowed");
                Assert.That(behind,   Is.EqualTo(HttpStatusCode.SwitchingProtocols), "the name the proxy says");
                Assert.That(other,    Is.EqualTo(HttpStatusCode.Forbidden),          "another site");
                Assert.That(noProxy,  Is.EqualTo(HttpStatusCode.Forbidden),          "the name with no proxy to say it");
            }

        }

        #endregion

    }

}
