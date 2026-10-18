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
    /// Charging stations, EVSEs, charging tariffs and parking spaces served with the properties their templates give them,
    /// as they are given with ?view=stored, and the templates and groups of a member below it.
    /// </summary>
    [TestFixture]
    public class EffectiveChargingStationAPITests
    {

        #region Data

        private static readonly DateTimeOffset           Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private static readonly ChargingStation_Id          S1       = ChargingStation_Id.        Parse("DE*GEF*S1");
        private static readonly ChargingStationTemplate_Id  ST1      = ChargingStationTemplate_Id.Parse("DE*GEF*ST1");
        private static readonly ChargingStationGroup_Id     SG1      = ChargingStationGroup_Id.   Parse("DE*GEF*SG1");

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
        /// A charging station with a hotline and a brand of its template only, in a group that gives it nothing; its EVSE
        /// with the maximum power of its template; a charging tariff with the prices of its template only; a parking space
        /// roofed by its template.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    ChargingStationOperator_Id.Parse("DE*GEF"),
                                                    Created:                   Created,
                                                    ChargingTariffTemplates:   [ new ChargingTariffTemplate(ChargingTariffTemplate_Id.Parse("DE*GEF*TT1"),
                                                                                                            Currency:  Currency.EUR,
                                                                                                            Elements:  [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                            Created:   Created) ],
                                                    ChargingTariffs:           [ new ChargingTariff(ChargingTariff_Id.Parse("DE*GEF*T1"), null, null, TemplateIds: [ ChargingTariffTemplate_Id.Parse("DE*GEF*TT1") ], Created: Created) ],
                                                    ChargingPools:             [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                  Created:           Created,
                                                                                                  ChargingStations:  [ new ChargingStation(S1,
                                                                                                                                           TemplateIds:  [ ST1 ],
                                                                                                                                           Created:      Created,
                                                                                                                                           EVSEs:        [ new EVSE(EVSE_Id.Parse("DE*GEF*E1*1"), TemplateIds: [ EVSETemplate_Id.Parse("DE*GEF*ET1") ], Created: Created) ]) ]) ],
                                                    ChargingStationTemplates:  [ new ChargingStationTemplate(ST1,
                                                                                                             HotlinePhoneNumber:  PhoneNumber.Parse("+49 800 424242"),
                                                                                                             Brands:              [ new Brand(Brand_Id.Parse("blue-charge"), new ImmutableI18NString(I18NString.Create(Languages.en, "Blue Charge"))) ],
                                                                                                             Created:             Created) ],
                                                    EVSETemplates:             [ new EVSETemplate(EVSETemplate_Id.Parse("DE*GEF*ET1"),
                                                                                                  MaxPower:  Watt.FromW(22000),
                                                                                                  Created:   Created) ],
                                                    ChargingStationGroups:     [ new ChargingStationGroup(SG1, ChargingStationIds: [ S1 ], Created: Created) ]) ],
                   ParkingOperators:          [ new ParkingOperator(ParkingOperator_Id.Parse("DE*PAR"),
                                                                    Created:                Created,
                                                                    ParkingSpaces:          [ new ParkingSpace(ParkingSpace_Id.Parse("space-1"), TemplateIds: [ ParkingSpaceTemplate_Id.Parse("level-1") ], Created: Created) ],
                                                                    ParkingSpaceTemplates:  [ new ParkingSpaceTemplate(ParkingSpaceTemplate_Id.Parse("level-1"),
                                                                                                                       Roofed:   true,
                                                                                                                       Created:  Created) ]) ]).WithKeys();

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

        #region (static) HeaderOf(Response, Name)

        private static String? HeaderOf(HttpResponseMessage Response, String Name)

            => Response.Headers.TryGetValues(Name, out var values)
                   ? String.Join(", ", values)
                   : null;

        #endregion

        #region (static) ETagOf(Content)

        private static String ETagOf(Byte[] Content)

            => $"\"json:sha256:hex:{Convert.ToHexStringLower(SHA256.HashData(Content))}\"";

        #endregion


        #region Charging_stations_are_served_with_the_properties_of_their_templates()

        [Test]
        public async Task Charging_stations_are_served_with_the_properties_of_their_templates()
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
            var (evse,       _,              evseJSON)      = await Get(CS + "/EVSEs/DE*GEF*E1*1");
            var (storedE,    _,              storedEVSE)    = await Get(CS + "/EVSEs/DE*GEF*E1*1?view=stored");
            var (space,      _,              spaceJSON)     = await Get("RNs/rn1/POs/DE*PAR/PSs/space-1");
            var (storedS,    _,              storedSpace)   = await Get("RNs/rn1/POs/DE*PAR/PSs/space-1?view=stored");
            var (_,          _,              spaces)        = await Get("RNs/rn1/POs/DE*PAR/PSs");
            var (pagedS,     _,              _)             = await Get("RNs/rn1/POs/DE*PAR/PSs?view=stored&limit=1&offset=1");
            var (pools,      _,              _)             = await Get("RNs/rn1/CPs?view=stored");
            var (whiteLabel, _,              _)             = await Get(CSO + "/Brands?view=nonsense");
            var (_,          _,              stationTempl)  = await Get(CS + "/CSTs");
            var (_,          _,              evseTempl)     = await Get(CS + "/EVSEs/DE*GEF*E1*1/EVSETs");
            var (_,          _,              poolTempl)     = await Get(CP + "/CPTs");
            var (_,          _,              spaceTempl)    = await Get("RNs/rn1/POs/DE*PAR/PSs/space-1/PSTs");
            var (noSpace,    _,              _)             = await Get("RNs/rn1/POs/DE*PAR/PSs/space-9/PSTs");
            var (_,          _,              stationGroups) = await Get(CS + "/CSGs");
            var (_,          _,              evseGroups)    = await Get(CS + "/EVSEs/DE*GEF*E1*1/EVSEGs");
            var (tariff,     _,              tariffJSON)    = await Get(CSO + "/CTs/DE*GEF*T1");
            var (storedT,    _,              storedTariff)  = await Get(CSO + "/CTs/DE*GEF*T1?view=stored");
            var (_,          _,              tariffs)       = await Get(CSO + "/CTs");
            var (_,          _,              tariffTempl)   = await Get(CSO + "/CTs/DE*GEF*T1/CTTs");
            var (_,          _,              templates)     = await Get(CSO + "/EVSETs");
            var (_,          _,              template)      = await Get(CSO + "/EVSETs/DE*GEF*ET1");
            var (_,          _,              spaceTemplates) = await Get("RNs/rn1/POs/DE*PAR/PSTs");

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

                // An EVSE as well.
                Assert.That(evseJSON!["maxPower"]!.Value<Int32>(),                             Is.EqualTo(22000));
                Assert.That(stationJSON["EVSEs"]![0]!["maxPower"]!.Value<Int32>(),             Is.EqualTo(22000));
                Assert.That(storedEVSE!["maxPower"],                                           Is.Null);

                // The ETag a precondition names, also when served as it is at runtime.
                Assert.That(station.Headers.ETag!.ToString(),                                  Is.Not.EqualTo(stored.Headers.ETag!.ToString()));
                Assert.That(HeaderOf(station, "X-Stored-ETag"),                                Is.EqualTo(stored.Headers.ETag!.ToString()));
                Assert.That(HeaderOf(stored,  "X-Stored-ETag"),                                Is.EqualTo(stored.Headers.ETag!.ToString()));
                Assert.That(HeaderOf(station, "Access-Control-Expose-Headers"),                Is.EqualTo("ETag, X-Stored-ETag"));
                Assert.That(HeaderOf(evse,    "X-Stored-ETag"),                                Is.EqualTo(storedE.Headers.ETag!.ToString()));

                // Parking spaces as well, of their parking operator and their lists.
                Assert.That(spaceJSON!["roofed"]!.Value<Boolean>(),                            Is.True);
                Assert.That(storedSpace!["roofed"],                                            Is.Null);
                Assert.That(spaces!.Single()["roofed"]!.Value<Boolean>(),                      Is.True);
                Assert.That(HeaderOf(space,   "X-Stored-ETag"),                                Is.EqualTo(storedS.Headers.ETag!.ToString()));
                Assert.That(HeaderOf(pagedS,  "Link"),                                         Is.EqualTo("</RNs/rn1/POs/DE*PAR/PSs?view=stored&limit=1>; rel=\"prev\""));

                // Found by what its template gives it.
                Assert.That(found!.Single()["@id"]!.Value<String>(),                           Is.EqualTo("DE*GEF*P1"));

                // The charging pools of the roaming network only as they are at runtime; a list of what is not at
                // runtime other than given has no view.
                Assert.That(pools.StatusCode,                                                  Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(whiteLabel.StatusCode,                                             Is.EqualTo(HttpStatusCode.OK));

                // The templates of a member, which give it properties: so a client tells where its values come from.
                Assert.That(stationTempl!.Single()["@id"]!.Value<String>(),                    Is.EqualTo(ST1.ToString()));
                Assert.That(stationTempl!.Single()["hotline"]!.Value<String>(),                Is.EqualTo("+49 800 424242"));
                Assert.That(evseTempl!.Single()["@id"]!.Value<String>(),                       Is.EqualTo("DE*GEF*ET1"));
                Assert.That(poolTempl,                                                         Is.Empty);
                Assert.That(spaceTempl!.Single()["@id"]!.Value<String>(),                      Is.EqualTo("level-1"));
                Assert.That(noSpace.StatusCode,                                                Is.EqualTo(HttpStatusCode.NotFound));

                // The groups it is in, which give it nothing.
                Assert.That(stationGroups!.Single()["@id"]!.Value<String>(),                   Is.EqualTo(SG1.ToString()));
                Assert.That(stationGroups!.Single()["hotline"],                                Is.Null);
                Assert.That(evseGroups,                                                        Is.Empty);

                // A charging tariff with the prices of its template, as it is given with ?view=stored.
                Assert.That(tariffJSON!["currency"]!.Value<String>(),                          Is.EqualTo("EUR"));
                Assert.That(storedTariff!["currency"],                                         Is.Null);
                Assert.That(tariffs!.Single()["currency"]!.Value<String>(),                    Is.EqualTo("EUR"));
                Assert.That(HeaderOf(tariff, "X-Stored-ETag"),                                 Is.EqualTo(storedT.Headers.ETag!.ToString()));
                Assert.That(tariffTempl!.Single()["@id"]!.Value<String>(),                     Is.EqualTo("DE*GEF*TT1"));

                // The templates of their owners, as they embed them.
                Assert.That(templates!.Single()["maxPower"]!.Value<Int32>(),                   Is.EqualTo(22000));
                Assert.That(template!["@id"]!.Value<String>(),                                 Is.EqualTo("DE*GEF*ET1"));
                Assert.That(spaceTemplates!.Single()["roofed"]!.Value<Boolean>(),              Is.True);

            }

        }

        #endregion

    }

}
