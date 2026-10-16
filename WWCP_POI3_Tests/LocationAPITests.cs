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

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.API
{

    /// <summary>
    /// A charging pool, charging station or EVSE found by its identification alone, redirected to where it is; the
    /// roaming network without what it holds; and lists in the natural order of the identifications.
    /// </summary>
    [TestFixture]
    public class LocationAPITests
    {

        #region Data

        private const String BrowserAccept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
        private const String Page          = "<!DOCTYPE html><title>POI</title>";

        private static readonly DateTimeOffset Created = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private const String CSO = "poi/RNs/rn1/CSOs/DE*GEF";

        private HTTPServer?   httpServer;
        private POI_API?      poiAPI;
        private HttpClient?   httpClient;
        private HttpClient?   following;

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

            poiAPI.AddRoamingNetworkHistory(RoamingNetworkHistory.Start(NewNetwork()));

            httpClient  = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) {
                              BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/")
                          };

            following   = new HttpClient {
                              BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/")
                          };

        }

        [TearDown]
        public async Task TearDown()
        {

            httpClient?.Dispose();
            following?. Dispose();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

        }

        #endregion


        #region NewNetwork()

        /// <summary>
        /// An operator with the charging pools P1, P2 and P10, each with a station and an EVSE, and the tariffs
        /// T1, T2 and T10; a grid operator.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    ChargingStationOperator_Id.Parse("DE*GEF"),
                                                    Created:          Created,
                                                    ChargingTariffs:  [ .. new[] { "1", "2", "10" }.Select(number => new ChargingTariff(ChargingTariff_Id.Parse($"DE*GEF*T{number}"),
                                                                                                                                      Currency.EUR,
                                                                                                                                      [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                                                      Created: Created)) ],
                                                    ChargingPools:    [ .. new[] { "1", "2", "10" }.Select(number => new ChargingPool(ChargingPool_Id.Parse($"DE*GEF*P{number}"),
                                                                                                                                    Created:           Created,
                                                                                                                                    ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse($"DE*GEF*S{number}"),
                                                                                                                                                                             Created:  Created,
                                                                                                                                                                             EVSEs:    [ new EVSE(EVSE_Id.Parse($"DE*GEF*E{number}*1"), Created: Created) ]) ])) ]) ],
                   GridOperators:             [ new GridOperator(GridOperator_Id.Parse("DE*GO1"), Created: Created) ]).WithKeys();

        #endregion

        #region Get(Path, Accept = null, Follow = false)

        private async Task<(HttpResponseMessage Response, String Text, JToken? JSON)> Get(String   Path,
                                                                                          String?  Accept   = null,
                                                                                          Boolean  Follow   = false)
        {

            using var request = new HttpRequestMessage(HttpMethod.Get, Path);

            if (Accept is not null)
                request.Headers.TryAddWithoutValidation("Accept", Accept);

            var response  = await (Follow ? following! : httpClient!).SendAsync(request);
            var text      = await response.Content.ReadAsStringAsync();

            return (response,
                    text,
                    response.IsSuccessStatusCode && response.Content.Headers.ContentType?.MediaType == "application/json"
                        ? JToken.ReadFrom(new JsonTextReader(new StringReader(text)) {
                                              DateParseHandling   = DateParseHandling.None,
                                              FloatParseHandling  = FloatParseHandling.Decimal
                                          })
                        : null);

        }

        #endregion

        #region (static) IdsOf(JSON)

        private static String[] IdsOf(JToken? JSON)

            => [.. (JSON as JArray ?? []).Select(entry => entry["@id"]!.Value<String>()!)];

        #endregion


        #region A_station_EVSE_and_pool_are_redirected_to_where_they_are()

        [Test]
        public async Task A_station_EVSE_and_pool_are_redirected_to_where_they_are()
        {

            var (station, _, _) = await Get("poi/RNs/rn1/CSs/DE*GEF*S2");
            var (evse,    _, _) = await Get("poi/RNs/rn1/EVSEs/DE*GEF*E10*1");
            var (pool,    _, _) = await Get("poi/RNs/rn1/CPs/DE*GEF*P1");
            var (stored,  _, _) = await Get("poi/RNs/rn1/CSs/DE*GEF*S2?view=stored");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(station.StatusCode,                          Is.EqualTo(HttpStatusCode.TemporaryRedirect));
                Assert.That(station.Headers.Location?.ToString(),       Is.EqualTo("/poi/RNs/rn1/CSOs/DE*GEF/CPs/DE*GEF*P2/CSs/DE*GEF*S2"));
                Assert.That(station.Headers.GetValues("Access-Control-Allow-Origin"), Is.EqualTo(new[] { "*" }));

                Assert.That(evse.StatusCode,                             Is.EqualTo(HttpStatusCode.TemporaryRedirect));
                Assert.That(evse.Headers.Location?.ToString(),          Is.EqualTo("/poi/RNs/rn1/CSOs/DE*GEF/CPs/DE*GEF*P10/CSs/DE*GEF*S10/EVSEs/DE*GEF*E10*1"));

                Assert.That(pool.StatusCode,                             Is.EqualTo(HttpStatusCode.TemporaryRedirect));
                Assert.That(pool.Headers.Location?.ToString(),          Is.EqualTo("/poi/RNs/rn1/CSOs/DE*GEF/CPs/DE*GEF*P1"));

                // The query kept: a view, a format, whatever it asks.
                Assert.That(stored.Headers.Location?.ToString(),        Is.EqualTo("/poi/RNs/rn1/CSOs/DE*GEF/CPs/DE*GEF*P2/CSs/DE*GEF*S2?view=stored"));

            }

        }

        #endregion

        #region Following_the_redirect_is_the_entity_where_it_is()

        [Test]
        public async Task Following_the_redirect_is_the_entity_where_it_is()
        {

            var (_, _, byId)   = await Get("poi/RNs/rn1/EVSEs/DE*GEF*E2*1",                          "application/json", Follow: true);
            var (_, _, where)  = await Get(CSO + "/CPs/DE*GEF*P2/CSs/DE*GEF*S2/EVSEs/DE*GEF*E2*1",   "application/json");

            Assert.That(byId,                         Is.Not.Null);
            Assert.That(JToken.DeepEquals(byId, where), Is.True);

        }

        #endregion

        #region A_browser_is_redirected_as_well_and_gets_the_HTML_view_of_what_is_not_there()

        [Test]
        public async Task A_browser_is_redirected_as_well_and_gets_the_HTML_view_of_what_is_not_there()
        {

            var (known,   _,    _) = await Get("poi/RNs/rn1/CSs/DE*GEF*S1", BrowserAccept);
            var (unknown, page, _) = await Get("poi/RNs/rn1/CSs/DE*GEF*S9", BrowserAccept);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(known.StatusCode,                     Is.EqualTo(HttpStatusCode.TemporaryRedirect));
                Assert.That(known.Headers.Location?.ToString(),   Is.EqualTo("/poi/RNs/rn1/CSOs/DE*GEF/CPs/DE*GEF*P1/CSs/DE*GEF*S1"));
                Assert.That(page,                                 Is.EqualTo(Page));
            }

        }

        #endregion

        #region What_is_not_there_or_no_identification_is_refused()

        [Test]
        public async Task What_is_not_there_or_no_identification_is_refused()
        {

            var (station, _, _) = await Get("poi/RNs/rn1/CSs/DE*GEF*S9",    "application/json");
            var (evse,    _, _) = await Get("poi/RNs/rn1/EVSEs/DE*XYZ*E1*1", "application/json");
            var (invalid, _, _) = await Get("poi/RNs/rn1/CSs/nonsense",      "application/json");
            var (network, _, _) = await Get("poi/RNs/rn9/CSs/DE*GEF*S1",    "application/json");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(station.StatusCode,  Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(evse.StatusCode,     Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(invalid.StatusCode,  Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(network.StatusCode,  Is.EqualTo(HttpStatusCode.NotFound));
            }

        }

        #endregion


        #region The_roaming_network_leaves_out_what_it_holds()

        [Test]
        public async Task The_roaming_network_leaves_out_what_it_holds()
        {

            var (whole,   _, wholeJSON) = await Get("poi/RNs/rn1",                                                 "application/json");
            var (slim,    _, slimJSON)  = await Get("poi/RNs/rn1?without=chargingStationOperators,gridOperators",   "application/json");
            var (again,   _, _)         = await Get("poi/RNs/rn1?without=gridOperators&without=chargingStationOperators", "application/json");
            var (unknown, _, _)         = await Get("poi/RNs/rn1?without=eTags",                                   "application/json");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(slim.StatusCode,                             Is.EqualTo(HttpStatusCode.OK));
                Assert.That(wholeJSON!["chargingStationOperators"],      Is.Not.Null);
                Assert.That(wholeJSON["gridOperators"],                  Is.Not.Null);
                Assert.That(slimJSON!["chargingStationOperators"],       Is.Null);
                Assert.That(slimJSON["gridOperators"],                   Is.Null);
                Assert.That(slimJSON["@id"]!.Value<String>(),            Is.EqualTo("rn1"));

                // Its ETags, of all of it: what tells whether anything of it changed.
                Assert.That(JToken.DeepEquals(slimJSON["eTags"], wholeJSON["eTags"]), Is.True);
                Assert.That(slimJSON["eTags"],                           Is.Not.Null);

                // Another representation, another ETag; the same, whatever the order the parts are named in.
                Assert.That(slim.Headers.ETag,                           Is.Not.EqualTo(whole.Headers.ETag));
                Assert.That(again.Headers.ETag,                          Is.EqualTo(slim.Headers.ETag));

                Assert.That(unknown.StatusCode,                          Is.EqualTo(HttpStatusCode.BadRequest));

            }

        }

        #endregion


        #region Lists_are_in_the_natural_order_of_the_identifications()

        [Test]
        public async Task Lists_are_in_the_natural_order_of_the_identifications()
        {

            var (_, _, pools)     = await Get("poi/RNs/rn1/CPs",   "application/json");
            var (_, _, tariffs)   = await Get(CSO + "/CTs",        "application/json");
            var (_, _, paged)     = await Get(CSO + "/CTs?limit=2", "application/json");
            var (_, _, operator_) = await Get(CSO,                 "application/json");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(IdsOf(pools),    Is.EqualTo(new[] { "DE*GEF*P1", "DE*GEF*P2", "DE*GEF*P10" }));
                Assert.That(IdsOf(tariffs),  Is.EqualTo(new[] { "DE*GEF*T1", "DE*GEF*T2", "DE*GEF*T10" }));
                Assert.That(IdsOf(paged),    Is.EqualTo(new[] { "DE*GEF*T1", "DE*GEF*T2" }));

                // What the operator embeds keeps the ordinal order: its canonical JSON, its ETag.
                Assert.That(IdsOf(operator_!["chargingTariffs"]), Is.EqualTo(new[] { "DE*GEF*T1", "DE*GEF*T10", "DE*GEF*T2" }));

            }

        }

        #endregion

        #region The_natural_order_reads_numbers_by_their_value()

        [Test]
        public void The_natural_order_reads_numbers_by_their_value()
        {

            String[] ids = [ "P10", "P2", "P1", "P01", "Q1", "P1a", "P", "P1*10", "P1*9" ];

            Assert.That(ids.Order(POINaturalOrder.Instance).ToArray(),
                        Is.EqualTo(new[] { "P", "P01", "P1", "P1*9", "P1*10", "P1a", "P2", "P10", "Q1" }));

        }

        #endregion

    }

}
