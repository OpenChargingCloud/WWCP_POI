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
    /// The charging tariffs, the templates and the groups of a charging station operator: left out of it on request,
    /// listed below it, and served one by one, each the part of the operator it is.
    /// </summary>
    [TestFixture]
    public class OperatorChildrenAPITests
    {

        #region Data

        private static readonly DateTimeOffset          Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private static readonly ChargingTariff_Id       T1       = ChargingTariff_Id.     Parse("DE*GEF*T1");
        private static readonly ChargingTariffGroup_Id  TG1      = ChargingTariffGroup_Id.Parse("DE*GEF*TG1");
        private static readonly EVSEGroup_Id            EG1      = EVSEGroup_Id.          Parse("DE*GEF*EG1");
        private static readonly EVSETemplate_Id         ET1      = EVSETemplate_Id.       Parse("DE*GEF*ET1");
        private static readonly ChargingPoolGroup_Id    PG1      = ChargingPoolGroup_Id.  Parse("DE*GEF*PG1");
        private static readonly ChargingPool_Id         P1       = ChargingPool_Id.       Parse("DE*GEF*P1");
        private static readonly EVSE_Id                 E1       = EVSE_Id.               Parse("DE*GEF*E1*1");

        private const String CSO = "RNs/rn1/CSOs/DE*GEF";

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
        /// An operator with a charging tariff, a group of it, a template of EVSEs naming it, a group of EVSEs and a group
        /// of charging pools.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                   Created:                   Created,
                   ChargingStationOperators:  [ new ChargingStationOperator(
                                                    ChargingStationOperator_Id.Parse("DE*GEF"),
                                                    Created:               Created,
                                                    ChargingTariffs:       [ new ChargingTariff(T1,
                                                                                                Currency.EUR,
                                                                                                [ new ChargingTariffElement([ new ChargingPriceComponent(ChargingDimensionTypes.ENERGY, 0.39m, StepSize: 1) ]) ],
                                                                                                Created: Created) ],
                                                    ChargingPools:         [ new ChargingPool(P1,
                                                                                              Created:           Created,
                                                                                              ChargingStations:  [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                                       Created:  Created,
                                                                                                                                       EVSEs:    [ new EVSE(E1, TemplateIds: [ ET1 ], Created: Created) ]) ]) ],
                                                    EVSETemplates:         [ new EVSETemplate       (ET1, ChargingTariffId: T1,          Created: Created) ],
                                                    EVSEGroups:            [ new EVSEGroup          (EG1, EVSEIds:           [ E1 ], Created: Created) ],
                                                    ChargingPoolGroups:    [ new ChargingPoolGroup  (PG1, ChargingPoolIds:   [ P1 ], Created: Created) ],
                                                    ChargingTariffGroups:  [ new ChargingTariffGroup(TG1, ChargingTariffIds: [ T1 ],                        Created: Created) ]) ]).WithKeys();

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


        #region An_operator_leaves_out_its_tariffs_templates_and_groups_which_are_listed()

        [Test]
        public async Task An_operator_leaves_out_its_tariffs_templates_and_groups_which_are_listed()
        {

            var (_,       _, whole)     = await Get(CSO);
            var (slim,    _, slimJSON)  = await Get(CSO + "?without=chargingPools,chargingTariffs,EVSETemplates,chargingTariffGroups,chargingPoolGroups,chargingStationGroups,EVSEGroups");
            var (_,       _, templList) = await Get(CSO + "/EVSETs");
            var (tariffs, _, tariffList)= await Get(CSO + "/CTs");
            var (groups,  _, groupList) = await Get(CSO + "/EVSEGs?match=EG1");
            var (none,    _, noneList)  = await Get(CSO + "/CSGs");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(slim.StatusCode,                                   Is.EqualTo(HttpStatusCode.OK));

                foreach (var property in new[] { "chargingPools", "chargingTariffs", "EVSETemplates", "chargingTariffGroups", "chargingPoolGroups", "EVSEGroups" })
                {
                    Assert.That(whole![property],                              Is.Not.Null, property);
                    Assert.That(slimJSON![property],                           Is.Null,     property);
                }

                Assert.That(slimJSON!["@id"]!.Value<String>(),                 Is.EqualTo("DE*GEF"));

                // Each list is what the operator embeds.
                Assert.That(JToken.DeepEquals(tariffList, whole!["chargingTariffs"]),   Is.True);
                Assert.That(HeaderOf(tariffs, "X-Total-Count"),                Is.EqualTo("1"));
                Assert.That(JToken.DeepEquals(groupList,  whole["EVSEGroups"]),         Is.True);
                Assert.That(JToken.DeepEquals(templList,  whole["EVSETemplates"]),      Is.True);
                Assert.That(HeaderOf(groups,  "X-Filtered-Count"),             Is.EqualTo("1"));

                Assert.That(none.StatusCode,                                   Is.EqualTo(HttpStatusCode.OK));
                Assert.That(noneList,                                          Is.Empty);

            }

        }

        #endregion

        #region Tariffs_templates_and_groups_are_served_one_by_one()

        [Test]
        public async Task Tariffs_templates_and_groups_are_served_one_by_one()
        {

            var (_,          _,             whole)      = await Get(CSO);
            var (tariff,     tariffContent, tariffJSON) = await Get(CSO + "/CTs/DE*GEF*T1");
            var (tariffGroup, _,            groupJSON)  = await Get(CSO + "/CTGs/DE*GEF*TG1");
            var (poolGroup,  _,             poolJSON)   = await Get(CSO + "/CPGs/DE*GEF*PG1");
            var (evseGroup,  _,             evseJSON)   = await Get(CSO + "/EVSEGs/DE*GEF*EG1");
            var (template,   _,             templJSON)  = await Get(CSO + "/EVSETs/DE*GEF*ET1");
            var (noTemplate, _,             _)          = await Get(CSO + "/EVSETs/DE*GEF*ET9");
            var (unknown,    _,             _)          = await Get(CSO + "/CTs/DE*GEF*T9");
            var (nobody,     _,             _)          = await Get("RNs/rn1/CSOs/DE*XXX/CTs/DE*GEF*T1");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(tariff.StatusCode,                                 Is.EqualTo(HttpStatusCode.OK));
                Assert.That(JToken.DeepEquals(tariffJSON, whole!["chargingTariffs"]![0]),      Is.True);
                Assert.That(HeaderOf(tariff, "ETag"),                          Is.EqualTo(ETagOf(tariffContent)));

                Assert.That(JToken.DeepEquals(groupJSON, whole["chargingTariffGroups"]![0]),  Is.True);
                Assert.That(JToken.DeepEquals(poolJSON,  whole["chargingPoolGroups"]![0]),    Is.True);
                Assert.That(JToken.DeepEquals(evseJSON,  whole["EVSEGroups"]![0]),            Is.True);
                Assert.That(tariffGroup.StatusCode,                            Is.EqualTo(HttpStatusCode.OK));
                Assert.That(poolGroup.StatusCode,                              Is.EqualTo(HttpStatusCode.OK));
                Assert.That(evseGroup.StatusCode,                              Is.EqualTo(HttpStatusCode.OK));
                Assert.That(template.StatusCode,                               Is.EqualTo(HttpStatusCode.OK));
                Assert.That(JToken.DeepEquals(templJSON, whole["EVSETemplates"]![0]),         Is.True);
                Assert.That(noTemplate.StatusCode,                             Is.EqualTo(HttpStatusCode.NotFound));

                Assert.That(unknown.StatusCode,                                Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(nobody.StatusCode,                                 Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

    }

}
