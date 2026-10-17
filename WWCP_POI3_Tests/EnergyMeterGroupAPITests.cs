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

namespace cloud.charging.open.protocols.WWCP.POI.tests
{

    /// <summary>
    /// Energy meter groups over HTTP, and the energy meters of charging pools, charging stations, EVSEs and grid
    /// connection points served with the properties of their groups, as they are given with ?view=stored.
    /// </summary>
    [TestFixture]
    public class EnergyMeterGroupAPITests
    {

        #region Data

        private const String CSO   = "RNs/rn1/CSOs/DE*GEF";
        private const String CP    = CSO + "/CPs/DE*GEF*P1";
        private const String EVSE  = CP  + "/CSs/DE*GEF*S1/EVSEs/DE*GEF*E1*2";
        private const String GO    = "RNs/rn1/GOs/DE*NET";
        private const String GCP   = GO  + "/GCPs/connection-42";

        private RoamingNetworkHistory?  history;
        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private HttpClient?             httpClient;

        #endregion

        #region SetUp / TearDown

        [SetUp]
        public void SetUp()
        {

            history     = RoamingNetworkHistory.Start(EnergyMeterGroupTests.NewNetwork());

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


        #region Energy_meter_groups_are_served_by_their_owners()

        [Test]
        public async Task Energy_meter_groups_are_served_by_their_owners()
        {

            var (_,         _,              operatorGroups)  = await Get(CSO + "/EMGs");
            var (group,     groupContent,   groupJSON)       = await Get(CSO + "/EMGs/gef-evse-meters");
            var (unknown,   _,              _)               = await Get(CSO + "/EMGs/net-grid-meters");
            var (_,         _,              gridGroups)      = await Get(GO  + "/EMGs");
            var (_,         _,              gridGroup)       = await Get(GO  + "/EMGs/NET-GRID-METERS");
            var (_,         _,              operatorWithout) = await Get(CSO + "?without=energyMeterGroups");
            var (_,         _,              operatorJSON)    = await Get(CSO);
            var (_,         _,              gridWithout)     = await Get(GO  + "?without=energyMeterGroups");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(operatorGroups!.Select(entry => entry["@id"]!.Value<String>()),       Is.EqualTo(new[] { "gef-evse-meters" }));
                Assert.That(group.StatusCode,                                                     Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(group, "ETag"),                                              Is.EqualTo(ETagOf(groupContent)));
                Assert.That(groupJSON!["modelId"]!.Value<String>(),                               Is.EqualTo("ACME-3000"));

                // A group of another owner is not one of this operator.
                Assert.That(unknown.StatusCode,                                                   Is.EqualTo(HttpStatusCode.NotFound));

                Assert.That(gridGroups!.Select(entry => entry["@id"]!.Value<String>()),           Is.EqualTo(new[] { "net-grid-meters" }));
                Assert.That(gridGroup!["manufacturerId"]!.Value<String>(),                        Is.EqualTo("ZAP"));

                Assert.That(operatorJSON!["energyMeterGroups"],                                   Is.Not.Null);
                Assert.That(operatorWithout!["energyMeterGroups"],                                Is.Null);
                Assert.That(gridWithout!["energyMeterGroups"],                                    Is.Null);

            }

        }

        #endregion

        #region Energy_meters_are_served_with_the_properties_of_their_groups()

        [Test]
        public async Task Energy_meters_are_served_with_the_properties_of_their_groups()
        {

            var (evse,     _,               evseJSON)        = await Get(EVSE);
            var (_,        _,               storedEVSE)      = await Get(EVSE + "?view=stored");
            var (point,    pointContent,    pointJSON)       = await Get(GCP);
            var (stored,   storedContent,   storedJSON)      = await Get(GCP  + "?view=stored");
            var (_,        _,               points)          = await Get(GO   + "/GCPs");
            var (_,        _,               storedPoints)    = await Get(GO   + "/GCPs?view=stored");
            var (_,        _,               expanded)        = await Get(CP   + "?expand=gridConnectionPoint");
            var (_,        _,               storedExpanded)  = await Get(CP   + "?expand=gridConnectionPoint&view=stored");
            var (_,        _,               storedOperator)  = await Get(GO);
            var (other,    _,               _)               = await Get(GCP  + "?view=effective");

            using (Assert.EnterMultipleScope())
            {

                // The meter of the EVSE: the model of its group, but its own serial number.
                Assert.That(evse.StatusCode,                                                             Is.EqualTo(HttpStatusCode.OK));
                Assert.That(evseJSON!["energyMeter"]!["modelId"]!.Value<String>(),                       Is.EqualTo("ACME-3000"));
                Assert.That(evseJSON["energyMeter"]!["serialNumber"]!.Value<String>(),                   Is.EqualTo("M-2"));
                Assert.That(storedEVSE!["energyMeter"]!["modelId"],                                      Is.Null);

                // The grid connection point: its meter as it is at runtime, with the ETag of its stored representation as well.
                Assert.That(point.StatusCode,                                                            Is.EqualTo(HttpStatusCode.OK));
                Assert.That(pointJSON!["energyMeter"]!["manufacturerId"]!.Value<String>(),               Is.EqualTo("ZAP"));
                Assert.That(HeaderOf(point, "ETag"),                                                     Is.EqualTo(ETagOf(pointContent)));
                Assert.That(HeaderOf(point, "X-Stored-ETag"),                                            Is.EqualTo(ETagOf(storedContent)));
                Assert.That(HeaderOf(point, "X-Stored-ETag"),                                            Is.Not.EqualTo(HeaderOf(point, "ETag")));
                Assert.That(stored.StatusCode,                                                           Is.EqualTo(HttpStatusCode.OK));
                Assert.That(storedJSON!["energyMeter"]!["manufacturerId"],                               Is.Null);
                Assert.That(HeaderOf(stored, "ETag"),                                                    Is.EqualTo(HeaderOf(stored, "X-Stored-ETag")));
                Assert.That(points!.Single()["energyMeter"]!["hardwareVersion"]!.Value<String>(),        Is.EqualTo("2.0"));
                Assert.That(storedPoints!.Single()["energyMeter"]!["hardwareVersion"],                   Is.Null);

                // Embedded in its charging pool as the pool is: at runtime, or as given.
                Assert.That(expanded!["gridConnectionPoint"]!["energyMeter"]!["manufacturerId"]!.Value<String>(),  Is.EqualTo("ZAP"));
                Assert.That(storedExpanded!["gridConnectionPoint"]!["energyMeter"]!["manufacturerId"],             Is.Null);

                // Its grid operator embeds it as it is given, with its groups.
                Assert.That(storedOperator!["gridConnectionPoints"]![0]!["energyMeter"]!["manufacturerId"],        Is.Null);
                Assert.That(storedOperator["energyMeterGroups"]![0]!["@id"]!.Value<String>(),                      Is.EqualTo("net-grid-meters"));

                Assert.That(other.StatusCode,                                                            Is.EqualTo(HttpStatusCode.BadRequest));

            }

        }

        #endregion

    }

}
