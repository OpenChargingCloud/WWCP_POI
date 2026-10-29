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
    /// that is neither the server's own nor allowed. Programs, which send no Origin, are not asked. What is served to
    /// everyone such a page may read: every read of a history, as the entities (CORS *), but no answer to a change.
    /// </summary>
    [TestFixture]
    public class CrossSiteTests
    {

        #region Data

        private static readonly HttpMethod COMMIT = new ("COMMIT");

        private const String Zeros = "0000000000000000000000000000000000000000000000000000000000000000";
        private const String Site  = "https://map.example.org";

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

        #region (private) FromSite(Method, Path, Content = null, Headers)

        /// <summary>
        /// Send what a page of another site makes a browser send, with its Origin and the given header fields: the answer,
        /// to read its header fields.
        /// </summary>
        private async Task<HttpResponseMessage> FromSite(HttpMethod                  Method,
                                                         String                      Path,
                                                         HttpContent?                Content = null,
                                                         params (String, String)[]   Headers)
        {

            using var request = new HttpRequestMessage(Method, Path) { Content = Content };

            request.Headers.Add("Origin", Site);

            foreach (var (name, value) in Headers)
                request.Headers.Add(name, value);

            return await httpClient!.SendAsync(request);

        }

        #endregion

        #region (static)  HeaderOf(Response, Name)

        private static String? HeaderOf(HttpResponseMessage Response, String Name)

            => Response.Headers.TryGetValues(Name, out var values)
                   ? String.Join(", ", values)
                   : Response.Content.Headers.TryGetValues(Name, out var contentValues)
                         ? String.Join(", ", contentValues)
                         : null;

        #endregion

        #region (static)  MethodsOf(Response, Name)

        private static String[] MethodsOf(HttpResponseMessage Response, String Name)

            => (HeaderOf(Response, Name) ?? "").Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

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


        #region Every_read_of_a_history_is_readable_from_every_origin(Path)

        /// <summary>
        /// Every read of a history is readable by a page of another site, as the entities are: the head, the snapshot,
        /// commits, checkpoints, epochs, the archive, price statements, the tips and the change sets waiting; an answer
        /// that something is not there or was asked for wrongly as well.
        /// </summary>
        [TestCase("head")]
        [TestCase("snapshot")]
        [TestCase("commitInfos")]
        [TestCase("commits")]
        [TestCase("commits/" + Zeros)]
        [TestCase("commits/" + Zeros + "/proof")]
        [TestCase("commits/not-a-commit-id")]
        [TestCase("checkpoints")]
        [TestCase("checkpoints/" + Zeros)]
        [TestCase("checkpoints/" + Zeros + "/snapshot")]
        [TestCase("checkpoints/" + Zeros + "/commits/" + Zeros + "/proof")]
        [TestCase("checkpoints/" + Zeros + "/priceStatements")]
        [TestCase("checkpoints/" + Zeros + "/priceStatements/" + Zeros + "/proof")]
        [TestCase("epoch")]
        [TestCase("epochs")]
        [TestCase("epochs/1/checkpoints")]
        [TestCase("epochs/1/checkpoints/" + Zeros)]
        [TestCase("epochs/1/commits/" + Zeros)]
        [TestCase("epochs/1/commits/" + Zeros + "/proof")]
        [TestCase("archive")]
        [TestCase("archive/0")]
        [TestCase("priceStatements")]
        [TestCase("tips")]
        [TestCase("haveIds")]
        [TestCase("changeSets/pending")]
        [TestCase("changeSets/pending/" + Zeros)]
        public async Task Every_read_of_a_history_is_readable_from_every_origin(String Path)
        {

            using var get   = await FromSite(HttpMethod.Get,  Path);
            using var head  = await FromSite(HttpMethod.Head, Path);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(HeaderOf(get,  "Access-Control-Allow-Origin"),  Is.EqualTo("*"), $"GET {Path}: {(Int32) get.StatusCode}");
                Assert.That(HeaderOf(head, "Access-Control-Allow-Origin"),  Is.EqualTo("*"), $"HEAD {Path}: {(Int32) head.StatusCode}");
            }

        }

        #endregion

        #region The_preflight_of_a_read_allows_every_origin_to_read_but_not_to_change(Path, Change)

        /// <summary>
        /// The preflight of a read allows every origin GET and HEAD with Accept and If-None-Match, as for the entities.
        /// Where the same path takes a change as well, Allow names it, Access-Control-Allow-Methods does not: a browser
        /// does not send it for a page of another site.
        /// </summary>
        [TestCase("head",                              null)]
        [TestCase("snapshot",                          null)]
        [TestCase("checkpoints/" + Zeros,              null)]
        [TestCase("archive/0",                         null)]
        [TestCase("commits",                           "POST")]
        [TestCase("checkpoints",                       "POST")]
        [TestCase("priceStatements",                   "POST")]
        [TestCase("changeSets/pending",                "POST")]
        [TestCase("changeSets/pending/" + Zeros,       "DELETE")]
        public async Task The_preflight_of_a_read_allows_every_origin_to_read_but_not_to_change(String Path, String? Change)
        {

            using var read    = await FromSite(HttpMethod.Options, Path, null, ("Access-Control-Request-Method", "GET"), ("Access-Control-Request-Headers", "if-none-match"));
            using var change  = await FromSite(HttpMethod.Options, Path, null, ("Access-Control-Request-Method", Change ?? "POST"), ("Access-Control-Request-Headers", "content-type"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(read.StatusCode,                                   Is.EqualTo(HttpStatusCode.NoContent), Path);
                Assert.That(HeaderOf(read, "Access-Control-Allow-Origin"),     Is.EqualTo("*"), Path);
                Assert.That(MethodsOf(read, "Access-Control-Allow-Methods"),   Is.EquivalentTo(new[] { "OPTIONS", "GET", "HEAD" }), Path);
                Assert.That(HeaderOf(read, "Access-Control-Allow-Headers"),    Does.Contain("If-None-Match").And.Not.Contain("Content-Type"), Path);

                Assert.That(MethodsOf(change, "Access-Control-Allow-Methods"), Does.Not.Contain(Change ?? "POST"), Path);
                Assert.That(MethodsOf(change, "Allow"),                        Is.EquivalentTo(Change is null
                                                                                                   ? new[] { "OPTIONS", "GET", "HEAD" }
                                                                                                   : new[] { "OPTIONS", "GET", "HEAD", Change }), Path);

            }

        }

        #endregion

        #region The_answer_to_a_change_is_not_readable_from_another_origin()

        /// <summary>
        /// A change set a program commits is committed as ever; with an Origin, as a page of another site would send it
        /// if a browser let it, its answer is not readable by that page: neither the 201, nor the 415 of text/plain, a
        /// form's own type, which a browser sends without a preflight. Nor is the preflight of the change set's path.
        /// </summary>
        [Test]
        public async Task The_answer_to_a_change_is_not_readable_from_another_origin()
        {

            using var preflight  = await FromSite(HttpMethod.Options, "changeSets", null, ("Access-Control-Request-Method", "COMMIT"), ("Access-Control-Request-Headers", "content-type"));
            using var committed  = await FromSite(COMMIT, "changeSets", new StringContent(ChangeSet("v1").ToJSON().ToString(), Encoding.UTF8, "application/json"));
            using var plain      = await FromSite(COMMIT, "changeSets", new StringContent(ChangeSet("v2").ToJSON().ToString(), Encoding.UTF8, "text/plain"));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(HeaderOf(preflight, "Access-Control-Allow-Origin"),  Is.Null);

                Assert.That(committed.StatusCode,                                Is.EqualTo(HttpStatusCode.Created));
                Assert.That(HeaderOf(committed, "Access-Control-Allow-Origin"),  Is.Null);

                Assert.That(plain.StatusCode,                                    Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
                Assert.That(HeaderOf(plain,     "Access-Control-Allow-Origin"),  Is.Null);

                Assert.That(leader!.Head.RoamingNetwork.DataSource,              Is.EqualTo("v1"));

            }

        }

        #endregion

        #region A_heavy_read_refused_is_readable_from_every_origin_with_its_Retry_After()

        /// <summary>
        /// A heavy read beyond the limit is refused with 429 as ever, readable by a page of another site as the read is,
        /// and with Retry-After exposed to it: not a CORS-safelisted response header, a script sees it only so.
        /// </summary>
        [Test]
        public async Task A_heavy_read_refused_is_readable_from_every_origin_with_its_Retry_After()
        {

            poiAPI!.MaxHeavyReadsPerMinute = 1;

            using var taken    = await FromSite(HttpMethod.Get, "snapshot");
            using var refused  = await FromSite(HttpMethod.Get, "snapshot");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(taken.StatusCode,                                      Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(taken,   "Access-Control-Allow-Origin"),      Is.EqualTo("*"));

                Assert.That(refused.StatusCode,                                    Is.EqualTo(HttpStatusCode.TooManyRequests));
                Assert.That(refused.Headers.RetryAfter?.Delta,                     Is.GreaterThanOrEqualTo(TimeSpan.FromSeconds(1)));
                Assert.That(HeaderOf(refused, "Access-Control-Allow-Origin"),      Is.EqualTo("*"));
                Assert.That(HeaderOf(refused, "Access-Control-Expose-Headers"),    Is.EqualTo("Retry-After"));

            }

        }

        #endregion

    }

}
