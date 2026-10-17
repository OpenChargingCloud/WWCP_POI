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
using System.Security.Cryptography;

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
    /// The parking spaces a charging station names, listed and served below it: those of parking operators
    /// and those its charging station operator defines itself, each as its operator embeds it.
    /// </summary>
    [TestFixture]
    public class StationParkingSpacesAPITests
    {

        #region Data

        private static readonly DateTimeOffset  Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private const String CSO      = "RNs/rn1/CSOs/DE*GEF";
        private const String PO       = "RNs/rn1/POs/DE*PAR";
        private const String Station  = CSO + "/CPs/DE*GEF*P1/CSs/DE*GEF*S1";

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

            poiAPI      = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(RoamingNetworkHistory.Start(NewNetwork()));

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


        #region NewNetwork()

        /// <summary>
        /// A charging station naming a parking space its operator defines and one of a parking operator,
        /// which has another one the station does not name.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    ChargingStationOperator_Id.Parse("DE*GEF"),
                                                    Created:        Created,
                                                    ParkingSpaces:  [ new ParkingSpace(ParkingSpace_Id.Parse("GEF-1"), PhysicalReference: "1", Roofed: true, Created: Created) ],
                                                    ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                       Created:           Created,
                                                                                       ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                ParkingSpaceIds:  [ ParkingSpace_Id.Parse("PAR-1"), ParkingSpace_Id.Parse("GEF-1") ],
                                                                                                                                Created:          Created) ]) ]) ],
                   ParkingOperators:          [ new ParkingOperator(
                                                    ParkingOperator_Id.Parse("DE*PAR"),
                                                    Created:        Created,
                                                    ParkingSpaces:  [ new ParkingSpace(ParkingSpace_Id.Parse("PAR-1"), Created: Created),
                                                                      new ParkingSpace(ParkingSpace_Id.Parse("PAR-2"), Created: Created) ]) ]).WithKeys();

        #endregion

        #region Get(Path)

        private async Task<(HttpResponseMessage Response, Byte[] Content, JToken? JSON)> Get(String Path)
        {

            var response  = await httpClient!.GetAsync(Path);
            var content   = await response.Content.ReadAsByteArrayAsync();

            return (response,
                    content,
                    response.IsSuccessStatusCode
                        ? JToken.ReadFrom(new JsonTextReader(new StringReader(System.Text.Encoding.UTF8.GetString(content))) {
                                              DateParseHandling   = DateParseHandling.None,
                                              FloatParseHandling  = FloatParseHandling.Decimal
                                          })
                        : null);

        }

        #endregion

        #region (static) HeaderOf(Response, Name) / ETagOf(Content)

        private static String? HeaderOf(HttpResponseMessage Response, String Name)

            => Response.Headers.TryGetValues(Name, out var values)
                   ? String.Join(", ", values)
                   : null;

        private static String ETagOf(Byte[] Content)

            => $"\"json:sha256:hex:{Convert.ToHexStringLower(SHA256.HashData(Content))}\"";

        #endregion


        #region A_charging_station_lists_the_parking_spaces_it_names()

        [Test]
        public async Task A_charging_station_lists_the_parking_spaces_it_names()
        {

            var (_,      _, cso)       = await Get(CSO);
            var (_,      _, po)        = await Get(PO);
            var (list,   _, listJSON)  = await Get(Station + "/PSs");
            var (found,  _, foundJSON) = await Get(Station + "/PSs?match=PAR");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(list.StatusCode,                                                   Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(list, "X-Total-Count"),                                   Is.EqualTo("2"));

                // In the order of their identifications, each as its operator embeds it.
                Assert.That(listJSON!.Select(space => space["@id"]!.Value<String>()),           Is.EqualTo(new[] { "GEF-1", "PAR-1" }));
                Assert.That(JToken.DeepEquals(listJSON![0], cso!["parkingSpaces"]![0]),         Is.True);
                Assert.That(JToken.DeepEquals(listJSON![1], po! ["parkingSpaces"]![0]),         Is.True);

                Assert.That(found.StatusCode,                                                  Is.EqualTo(HttpStatusCode.OK));
                Assert.That(foundJSON!.Select(space => space["@id"]!.Value<String>()),          Is.EqualTo(new[] { "PAR-1" }));

            }

        }

        #endregion

        #region A_parking_space_a_charging_station_names_is_served_below_it()

        [Test]
        public async Task A_parking_space_a_charging_station_names_is_served_below_it()
        {

            var (_,        _,              cso)        = await Get(CSO);
            var (own,      ownContent,     ownJSON)    = await Get(Station + "/PSs/GEF-1");
            var (parking,  _,              parkingJSON) = await Get(Station + "/PSs/par-1");
            var (unnamed,  _,              _)          = await Get(Station + "/PSs/PAR-2");
            var (unknown,  _,              _)          = await Get(Station + "/PSs/PAR-9");
            var (nowhere,  _,              _)          = await Get(CSO + "/CPs/DE*GEF*P1/CSs/DE*GEF*S9/PSs");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(own.StatusCode,                                                    Is.EqualTo(HttpStatusCode.OK));
                Assert.That(JToken.DeepEquals(ownJSON, cso!["parkingSpaces"]![0]),               Is.True);
                Assert.That(HeaderOf(own, "ETag"),                                             Is.EqualTo(ETagOf(ownContent)));

                // In every spelling its identification considers equal.
                Assert.That(parking.StatusCode,                                                Is.EqualTo(HttpStatusCode.OK));
                Assert.That(parkingJSON!["@id"]!.Value<String>(),                              Is.EqualTo("PAR-1"));

                // A parking space the charging station does not name is none of it, even if it exists.
                Assert.That(unnamed.StatusCode,                                                Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(unknown.StatusCode,                                                Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(nowhere.StatusCode,                                                Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

        #region An_operator_leaves_out_its_parking_spaces_which_its_stations_list()

        [Test]
        public async Task An_operator_leaves_out_its_parking_spaces_which_its_stations_list()
        {

            var (_,     _, whole)     = await Get(CSO);
            var (slim,  _, slimJSON)  = await Get(CSO + "?without=parkingSpaces");
            var (_,     _, listJSON)  = await Get(Station + "/PSs");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(slim.StatusCode,                                                   Is.EqualTo(HttpStatusCode.OK));
                Assert.That(whole!   ["parkingSpaces"],                                        Is.Not.Null);
                Assert.That(slimJSON!["parkingSpaces"],                                        Is.Null);
                Assert.That(slimJSON!["chargingPools"],                                        Is.Not.Null);

                // What it leaves out, its station lists.
                Assert.That(JToken.DeepEquals(listJSON![0], whole!["parkingSpaces"]![0]),       Is.True);

            }

        }

        #endregion

    }

}
