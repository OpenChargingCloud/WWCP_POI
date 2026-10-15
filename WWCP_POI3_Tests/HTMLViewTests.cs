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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.API
{

    /// <summary>
    /// An API under a path of its own, whose lists and entities a browser gets as the page of a web frontend.
    /// </summary>
    [TestFixture]
    public class HTMLViewTests
    {

        #region Data

        private const String BrowserAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        private const String Page          = "<!DOCTYPE html><title>POI</title>";

        private HTTPServer?   httpServer;
        private POI_API?      poiAPI;
        private HttpClient?   httpClient;

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

            poiAPI      = new POI_API(httpServer,
                                      RootPath:  HTTPPath.Parse("/poi"),
                                      HTMLView:  request => Task.FromResult(
                                                                new HTTPResponse.Builder(request) {
                                                                    HTTPStatusCode  = HTTPStatusCode.OK,
                                                                    ContentType     = HTTPContentType.Text.HTML_UTF8,
                                                                    Content         = Page.ToUTF8Bytes()
                                                                }.AsImmutable));

            poiAPI.AddRoamingNetworkHistory(
                RoamingNetworkHistory.Start(
                    new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                                       Created:                   DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                                       ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                                                Created: DateTimeOffset.Parse("2026-01-01T00:00:00Z")) ]).WithKeys()));

            httpClient  = new HttpClient {
                              BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/")
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

        #region Get(Path, Accept = null)

        private async Task<(HttpStatusCode StatusCode, String? ContentType, String Text)> Get(String   Path,
                                                                                              String?  Accept   = null)
        {

            using var request = new HttpRequestMessage(HttpMethod.Get, Path);

            if (Accept is not null)
                request.Headers.TryAddWithoutValidation("Accept", Accept);

            using var response = await httpClient!.SendAsync(request);

            return (response.StatusCode,
                    response.Content.Headers.ContentType?.MediaType,
                    await response.Content.ReadAsStringAsync());

        }

        #endregion


        #region A_browser_gets_the_HTML_view_of_a_list_and_every_client_its_data()

        [Test]
        public async Task A_browser_gets_the_HTML_view_of_a_list_and_every_client_its_data()
        {

            var browser   = await Get("poi/RNs/rn1/CPs",  BrowserAccept);
            var networks  = await Get("poi/RNs",          BrowserAccept);
            var json      = await Get("poi/RNs/rn1/CPs",  "application/json");
            var cbor      = await Get("poi/RNs/rn1/CPs",  "application/cbor");
            var anything  = await Get("poi/RNs/rn1/CPs",  "*/*");
            var none      = await Get("poi/RNs/rn1/CPs");
            var entity    = await Get("poi/RNs/rn1/CSOs/DE*GEF", BrowserAccept);
            var entityJSON = await Get("poi/RNs/rn1/CSOs/DE*GEF", "application/json");
            var network   = await Get("poi/RNs/rn1",      BrowserAccept);
            var unprefixed = await Get("RNs/rn1/CPs",     "application/json");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(browser.StatusCode,     Is.EqualTo(HttpStatusCode.OK));
                Assert.That(browser.ContentType,    Is.EqualTo("text/html"));
                Assert.That(browser.Text,           Is.EqualTo(Page));
                Assert.That(networks.Text,          Is.EqualTo(Page));

                Assert.That(json.ContentType,       Is.EqualTo("application/json"));
                Assert.That(json.Text,              Does.StartWith("["));
                Assert.That(cbor.ContentType,       Is.EqualTo("application/cbor"));
                Assert.That(anything.ContentType,   Is.EqualTo("application/json"));
                Assert.That(none.ContentType,       Is.EqualTo("application/json"));

                // A single entity as well, and its data for everybody else.
                Assert.That(entity.ContentType,     Is.EqualTo("text/html"));
                Assert.That(entity.Text,            Is.EqualTo(Page));
                Assert.That(network.Text,           Is.EqualTo(Page));
                Assert.That(entityJSON.ContentType, Is.EqualTo("application/json"));
                Assert.That(entityJSON.Text,        Does.Contain("\"@id\":\"DE*GEF\""));

                Assert.That(unprefixed.StatusCode,  Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

    }

}
