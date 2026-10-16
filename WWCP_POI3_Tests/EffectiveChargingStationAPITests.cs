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
    /// Charging stations served with the properties their groups give them, as they are given with ?view=stored.
    /// </summary>
    [TestFixture]
    public class EffectiveChargingStationAPITests
    {

        #region Data

        private static readonly DateTimeOffset           Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private static readonly ChargingStation_Id       S1       = ChargingStation_Id.     Parse("DE*GEF*S1");
        private static readonly ChargingStationGroup_Id  SG1      = ChargingStationGroup_Id.Parse("DE*GEF*SG1");

        private const String CSO = "RNs/rn1/CSOs/DE*GEF";
        private const String CP  = CSO + "/CPs/DE*GEF*P1";
        private const String CS  = CP  + "/CSs/DE*GEF*S1";

        private RoamingNetworkHistory?  history;
        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private HttpClient?             httpClient;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            history     = RoamingNetworkHistory.Start(NewNetwork());

            httpServer  = new HTTPServer(
                              IPAddress:  IPv4Address.Localhost,
                              TCPPort:    IPPort.Zero,
                              AutoStart:  true
                          );

            poiAPI      = new POI_API(httpServer);

            poiAPI.AddRoamingNetworkHistory(history);

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
        /// A charging station with a hotline and a brand of its group only.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    ChargingStationOperator_Id.Parse("DE*GEF"),
                                                    Created:                Created,
                                                    ChargingPools:          [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                               Created:           Created,
                                                                                               ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                        Created:  Created,
                                                                                                                                        EVSEs:    [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"), Created: Created) ]) ]) ],
                                                    ChargingStationGroups:  [ new ChargingStationGroup(SG1,
                                                                                                       ChargingStationIds:  [ S1 ],
                                                                                                       HotlinePhoneNumber:  PhoneNumber.Parse("+49 800 424242"),
                                                                                                       Brands:              [ new Brand(Brand_Id.Parse("blue-charge"), new ImmutableI18NString(I18NString.Create(Languages.en, "Blue Charge"))) ],
                                                                                                       Created:             Created) ]) ]).WithKeys();

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

        #region (static) ETagOf(Content)

        private static String ETagOf(Byte[] Content)

            => $"\"json:sha256:hex:{Convert.ToHexStringLower(SHA256.HashData(Content))}\"";

        #endregion


        #region Charging_stations_are_served_with_the_properties_of_their_groups()

        [Test]
        public async Task Charging_stations_are_served_with_the_properties_of_their_groups()
        {

            var (_,          _,              operatorJSON)  = await Get(CSO);
            var (station,    stationContent, stationJSON)   = await Get(CS);
            var (stored,     storedContent,  storedJSON)    = await Get(CS + "?view=stored");
            var (_,          _,              poolJSON)      = await Get(CP);
            var (_,          _,              storedPool)    = await Get(CP + "?view=stored");
            var (_,          _,              brands)        = await Get(CS + "/Brands");
            var (_,          _,              storedBrands)  = await Get(CS + "/Brands?view=stored");
            var (unknown,    _,              _)             = await Get(CP + "?view=effective");
            var (_,          _,              found)         = await Get("RNs/rn1/CPs?match=424242");

            history!.Head.RoamingNetwork.TryGetEntityETag(nameof(ChargingStation), S1.ToString(), out var eTag);

            using (Assert.EnterMultipleScope())
            {

                // As it is at runtime, with its own ETag.
                Assert.That(station.StatusCode,                                                Is.EqualTo(HttpStatusCode.OK));
                Assert.That(stationJSON!["hotline"]!.Value<String>(),                          Is.EqualTo("+49 800 424242"));
                Assert.That(poolJSON!["chargingStations"]![0]!["hotline"]!.Value<String>(),    Is.EqualTo("+49 800 424242"));
                Assert.That(brands!.Single()["@id"]!.Value<String>(),                          Is.EqualTo("blue-charge"));
                Assert.That(station.Headers.ETag!.ToString(),                                  Is.EqualTo(ETagOf(stationContent)));

                // As its operator embeds it, with the ETag a precondition names.
                Assert.That(stored.StatusCode,                                                 Is.EqualTo(HttpStatusCode.OK));
                Assert.That(storedJSON!["hotline"],                                            Is.Null);
                Assert.That(JToken.DeepEquals(storedJSON, operatorJSON!["chargingPools"]![0]!["chargingStations"]![0]),  Is.True);
                Assert.That(JToken.DeepEquals(storedPool, operatorJSON["chargingPools"]![0]),  Is.True);
                Assert.That(storedBrands,                                                      Is.Empty);
                Assert.That(stored.Headers.ETag!.ToString(),                                   Is.EqualTo(ETagOf(storedContent)));
                Assert.That(stored.Headers.ETag!.ToString(),                                   Is.EqualTo($"\"{eTag}\""));

                Assert.That(unknown.StatusCode,                                                Is.EqualTo(HttpStatusCode.BadRequest));

                // Found by what its group gives it.
                Assert.That(found!.Single()["@id"]!.Value<String>(),                           Is.EqualTo("DE*GEF*P1"));

            }

        }

        #endregion

    }

}
