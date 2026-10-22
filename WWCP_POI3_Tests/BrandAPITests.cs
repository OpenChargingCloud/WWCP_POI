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
    /// The brands of a charging station operator, a charging pool, a charging station and an EVSE: each level its own,
    /// listed below it and served one by one; the same identification on two levels two brands.
    /// </summary>
    [TestFixture]
    public class BrandAPITests
    {

        #region Data

        private static readonly DateTimeOffset  Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private const String CSO   = "RNs/rn1/CSOs/DE*GEF";
        private const String CP    = CSO + "/CPs/DE*GEF*P1";
        private const String CS    = CP  + "/CSs/DE*GEF*S1";
        private const String EV    = CS  + "/EVSEs/DE*GEF*E1*1";

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


        #region (static) BrandOf(Id, Name)

        private static Brand BrandOf(String Id, String Name)

            => new (Brand_Id.Parse(Id), ImmutableI18NString.Create(Languages.en, Name));

        #endregion

        #region NewNetwork()

        /// <summary>
        /// An operator charging under a white label, a pool and its station both also appearing under "city-charge",
        /// each with its own idea of it, and an EVSE under a brand of its own.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    ChargingStationOperator_Id.Parse("DE*GEF"),
                                                    Created:        Created,
                                                    Brands:         [ BrandOf("white-charge", "White Charge") ],
                                                    ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                       Created:           Created,
                                                                                       Brands:            [ BrandOf("city-charge", "City Charge, the pool") ],
                                                                                       ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                Created:  Created,
                                                                                                                                Brands:   [ BrandOf("city-charge", "City Charge, the station") ],
                                                                                                                                EVSEs:    [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"),
                                                                                                                                                     Brands:   [ BrandOf("fast-charge", "Fast Charge") ],
                                                                                                                                                     Created:  Created) ]) ]) ]) ]).WithKeys();

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

        #region (static) Ids(List)

        private static String[] Ids(JToken? List)
            => [.. (List as JArray ?? []).Select(entry => entry["@id"]!.Value<String>()!)];

        #endregion


        #region Every_level_has_its_own_brands()

        [Test]
        public async Task Every_level_has_its_own_brands()
        {

            var (_,          _,            operatorBrands)  = await Get(CSO + "/Brands");
            var (_,          _,            poolBrands)      = await Get(CP  + "/Brands");
            var (_,          _,            stationBrands)   = await Get(CS  + "/Brands");
            var (_,          _,            evseBrands)      = await Get(EV  + "/Brands");
            var (ofPool,     poolContent,  poolCity)        = await Get(CP  + "/Brands/city-charge");
            var (_,          _,            stationCity)     = await Get(CS  + "/Brands/city-charge");
            var (_,          _,            pool)            = await Get(CP);
            var (notOfPool,  _,            _)               = await Get(CP  + "/Brands/white-charge");
            var (nobody,     _,            _)               = await Get(CSO + "/CPs/DE*GEF*P9/Brands");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Ids(operatorBrands),                                   Is.EqualTo(new[] { "white-charge" }));
                Assert.That(Ids(poolBrands),                                       Is.EqualTo(new[] { "city-charge" }));
                Assert.That(Ids(stationBrands),                                    Is.EqualTo(new[] { "city-charge" }));
                Assert.That(Ids(evseBrands),                                       Is.EqualTo(new[] { "fast-charge" }));

                // The same identification on two levels: two brands, each the one its level embeds.
                Assert.That(poolCity!   ["name"]!["en"]!.Value<String>(),          Is.EqualTo("City Charge, the pool"));
                Assert.That(stationCity!["name"]!["en"]!.Value<String>(),          Is.EqualTo("City Charge, the station"));
                Assert.That(JToken.DeepEquals(poolCity, pool!["brands"]![0]),      Is.True);
                Assert.That(ofPool.Headers.ETag?.Tag,
                            Is.EqualTo($"\"json:sha256:hex:{Convert.ToHexStringLower(SHA256.HashData(poolContent))}\""));

                // The white label of the operator is no brand of its pool.
                Assert.That(notOfPool.StatusCode,                                  Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(nobody.StatusCode,                                     Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

    }

}
