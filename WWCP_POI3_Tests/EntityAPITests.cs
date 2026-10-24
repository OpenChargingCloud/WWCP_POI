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
using System.Net.Http.Headers;
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
    /// The roaming network and what it embeds over HTTP, each with the ETag of its representation.
    /// </summary>
    [TestFixture]
    public class EntityAPITests
    {

        #region Data

        private static readonly DateTimeOffset          Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

        private static readonly ChargingStationOperator_Id  GEF  = ChargingStationOperator_Id.Parse("DE*GEF");
        private static readonly ChargingPool_Id         P1       = ChargingPool_Id.        Parse("DE*GEF*P1");
        private static readonly ChargingStation_Id      S1       = ChargingStation_Id.     Parse("DE*GEF*S1");
        private static readonly EVSE_Id                 E1       = EVSE_Id.                Parse("DE*GEF*E1*1");
        private static readonly EMobilityProvider_Id    EMP      = EMobilityProvider_Id.   Parse("DE*EMP");
        private static readonly GridOperator_Id         NET      = GridOperator_Id.        Parse("DE*NET");
        private static readonly GridOperator_Id         NET2     = GridOperator_Id.        Parse("DE*NE2");
        private static readonly GridConnectionPoint_Id  GCP      = GridConnectionPoint_Id. Parse("connection-42");

        private const String RN   = "RNs/rn1";
        private const String CSO  = RN  + "/CSOs/DE*GEF";
        private const String CP   = CSO + "/CPs/DE*GEF*P1";
        private const String CS   = CP  + "/CSs/DE*GEF*S1";
        private const String EV   = CS  + "/EVSEs/DE*GEF*E1*1";
        private const String GO   = RN  + "/GOs/DE*NET";
        private const String GC   = GO  + "/GCPs/connection-42";
        private const String EM   = RN  + "/EMPs/DE*EMP";
        private const String CM   = RN  + "/CSMs/ACME";
        private const String MM   = RN  + "/EMMs/METERS";
        private const String PO   = RN  + "/POs/DE*PAR";

        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private RoamingNetworkHistory?  history;
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

            poiAPI      = new POI_API(httpServer);

            history     = RoamingNetworkHistory.Start(NewNetwork());

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


        #region NewNetwork(Id = "rn1")

        private static RoamingNetwork NewNetwork(String Id = "rn1")

            => new RoamingNetwork(RoamingNetwork_Id.Parse(Id),
                    Created:                   Created,
                    ChargingStationManufacturers:  [ new ChargingStationManufacturer(ChargingStationManufacturer_Id.Parse("ACME"),
                                                                                     I18NString.Create(Languages.en, "ACME Chargers"),
                                                                                     Models:   [ new ChargingStationModel(ChargingStationModel_Id.Parse("Wallbox-22"), Versions: [ "1.0" ]),
                                                                                                 new ChargingStationModel(ChargingStationModel_Id.Parse("Wallbox-11")) ],
                                                                                     Created:  Created) ],
                    EnergyMeterManufacturers:  [ new EnergyMeterManufacturer(EnergyMeterManufacturer_Id.Parse("METERS"),
                                                                             ImmutableI18NString.Create(Languages.en, "Example Meters"),
                                                                             Models:   [ new EnergyMeterModel(EnergyMeterModel_Id.Parse("M-500")) ],
                                                                             Created:  Created) ],
                    ParkingOperators:          [ new ParkingOperator(ParkingOperator_Id.Parse("DE*PAR"),
                                                                     Created:        Created,
                                                                     ParkingSpaces:  [ new ParkingSpace(ParkingSpace_Id.Parse("space-1"), Created: Created) ]) ],
                    EMobilityProviders:        [ new EMobilityProvider(EMP,
                                                                       I18NString.Create(Languages.en, "Example Mobility"),
                                                                       Created: Created) ],
                    GridOperators:             [ new GridOperator(NET,
                                                                  I18NString.Create(Languages.en, "Example Netz"),
                                                                  GridConnectionPoints:  [ new GridConnectionPoint(GCP, VoltageLevel: GridVoltageLevel.LowVoltage) ],
                                                                  Created:               Created),
                                                 new GridOperator(NET2,
                                                                  I18NString.Create(Languages.en, "Other Netz"),
                                                                  Created: Created) ],
                    ChargingStationOperators:  [ new ChargingStationOperator(GEF,
                                                                             I18NString.Create(Languages.en, "GraphDefined"),
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(P1,
                                                                                                                Created:                Created,
                                                                                                                GridConnectionPointId:  GCP,
                                                                                                                ChargingStations:       [ new ChargingStation(S1,
                                                                                                                                                            Created:  Created,
                                                                                                                                                            EVSEs:    [ new EVSE(E1, Created: Created) ]) ]) ]) ]).WithKeys();

        #endregion

        #region Send(Method, Path, Accept = null, IfNoneMatch = null)

        private async Task<(HttpResponseMessage Response, Byte[] Content)> Send(HttpMethod  Method,
                                                                               String      Path,
                                                                               String?     Accept        = null,
                                                                               String?     IfNoneMatch   = null)
        {

            using var request = new HttpRequestMessage(Method, Path);

            if (Accept is not null)
                request.Headers.Accept.ParseAdd(Accept);

            if (IfNoneMatch is not null)
                request.Headers.TryAddWithoutValidation("If-None-Match", IfNoneMatch);

            var response = await httpClient!.SendAsync(request);

            return (response, await response.Content.ReadAsByteArrayAsync());

        }

        #endregion

        #region (static) HeaderOf(Response, Name)

        private static String? HeaderOf(HttpResponseMessage Response, String Name)

            => Response.Headers.TryGetValues(Name, out var values)
                   ? String.Join(", ", values)
                   : Response.Content.Headers.TryGetValues(Name, out var contentValues)
                         ? String.Join(", ", contentValues)
                         : null;

        #endregion

        #region (static) ParseJSON(Content)

        /// <summary>
        /// The JSON as it was sent: timestamps stay text, numbers decimals.
        /// </summary>
        private static JObject ParseJSON(Byte[] Content)

            => (JObject) ParseToken(Content);

        #endregion

        #region (static) ParseList(Content)

        /// <summary>
        /// The JSON array as it was sent: timestamps stay text, numbers decimals.
        /// </summary>
        private static JArray ParseList(Byte[] Content)

            => (JArray) ParseToken(Content);

        #endregion

        #region (static) ParseToken(Content)

        private static JToken ParseToken(Byte[] Content)

            => JToken.ReadFrom(new JsonTextReader(new StringReader(System.Text.Encoding.UTF8.GetString(Content))) {
                                   DateParseHandling   = DateParseHandling.None,
                                   FloatParseHandling  = FloatParseHandling.Decimal
                               });

        #endregion

        #region (static) ETagOf(Format, Content)

        private static String ETagOf(String Format, Byte[] Content)

            => $"\"{Format}:sha256:hex:{Convert.ToHexStringLower(SHA256.HashData(Content))}\"";

        #endregion


        #region Every_entity_is_served_in_JSON_with_the_ETag_of_its_content()

        [Test]
        public async Task Every_entity_is_served_in_JSON_with_the_ETag_of_its_content()
        {

            foreach (var (path, id) in new[] {
                                           (CSO, "DE*GEF"),
                                           (CP,  "DE*GEF*P1"),
                                           (CS,  "DE*GEF*S1"),
                                           (EV,  "DE*GEF*E1*1"),
                                           (GO,  "DE*NET"),
                                           (GC,  "connection-42"),
                                           (EM,  "DE*EMP"),
                                           (CM,  "ACME"),
                                           (MM,  "METERS"),
                                           (PO,  "DE*PAR")
                                       })
            {

                var (response, content) = await Send(HttpMethod.Get, path);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(response.StatusCode,                                            Is.EqualTo(HttpStatusCode.OK), path);
                    Assert.That(response.Content.Headers.ContentType?.MediaType,                Is.EqualTo("application/json"), path);
                    Assert.That(HeaderOf(response, "ETag"),                                     Is.EqualTo(ETagOf("json", content)), path);
                    Assert.That(HeaderOf(response, "Access-Control-Allow-Origin"),              Is.EqualTo("*"), path);
                    Assert.That(ParseJSON((content))["@id"]!.Value<String>(),  Is.EqualTo(id), path);

                    // A charging pool, station, EVSE or grid connection point, served as it is at runtime, tells the ETag of its
                    // stored representation as well: without groups giving it something, the same.
                    if (path is CP or CS or EV or GC)
                    {
                        Assert.That(HeaderOf(response, "Access-Control-Expose-Headers"),        Is.EqualTo("ETag, X-Stored-ETag"), path);
                        Assert.That(HeaderOf(response, "X-Stored-ETag"),                        Is.EqualTo(HeaderOf(response, "ETag")), path);
                    }
                    else
                    {
                        Assert.That(HeaderOf(response, "Access-Control-Expose-Headers"),        Is.EqualTo("ETag"), path);
                        Assert.That(HeaderOf(response, "X-Stored-ETag"),                        Is.Null, path);
                    }
                }

            }

        }

        #endregion

        #region The_lists_name_the_roaming_networks_and_their_operators()

        [Test]
        public async Task The_lists_name_the_roaming_networks_and_their_operators()
        {

            poiAPI!.AddRoamingNetwork(NewNetwork("rn0"), EventTracking_Id.New);

            var (roamingNetworks,          roamingNetworksContent)           = await Send(HttpMethod.Get, "RNs");
            var (chargingStationOperators, chargingStationOperatorsContent)  = await Send(HttpMethod.Get, RN + "/CSOs");
            var (unknown,                  _)                                = await Send(HttpMethod.Get, "RNs/rn2/CSOs");
            var (options,                  _)                                = await Send(HttpMethod.Options, "RNs");

            var roamingNetworksJSON           = JArray.Parse(System.Text.Encoding.UTF8.GetString(roamingNetworksContent));
            var chargingStationOperatorsJSON  = JArray.Parse(System.Text.Encoding.UTF8.GetString(chargingStationOperatorsContent));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(roamingNetworks.StatusCode,                                         Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(roamingNetworks, "ETag"),                                  Is.EqualTo(ETagOf("json", roamingNetworksContent)));
                Assert.That(HeaderOf(roamingNetworks, "Access-Control-Allow-Origin"),           Is.EqualTo("*"));
                Assert.That(roamingNetworksJSON.Select(entry => entry["@id"]!.Value<String>()), Is.EqualTo(new[] { "rn0", "rn1" }));
                Assert.That(roamingNetworksJSON[1]["chargingStationOperators"],                 Is.Null);

                Assert.That(chargingStationOperators.StatusCode,                                Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(chargingStationOperators, "ETag"),                         Is.EqualTo(ETagOf("json", chargingStationOperatorsContent)));
                Assert.That(chargingStationOperatorsJSON.Count,                                 Is.EqualTo(1));
                Assert.That(chargingStationOperatorsJSON[0]["@id"]!.Value<String>(),            Is.EqualTo("DE*GEF"));
                Assert.That(chargingStationOperatorsJSON[0]["name"]!["en"]!.Value<String>(),    Is.EqualTo("GraphDefined"));
                Assert.That(chargingStationOperatorsJSON[0]["chargingPools"],                   Is.Null);

                Assert.That(unknown.StatusCode,                                                 Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(options.StatusCode,                                                 Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(HeaderOf(options, "Access-Control-Allow-Origin"),                   Is.EqualTo("*"));

            }

        }

        #endregion

        #region Every_collection_of_the_roaming_network_is_listed()

        [Test]
        public async Task Every_collection_of_the_roaming_network_is_listed()
        {

            foreach (var (path, ids) in new[] {
                                            (RN + "/CSOs",  new[] { "DE*GEF" }),
                                            (RN + "/GOs",   new[] { "DE*NE2", "DE*NET" }),
                                            (RN + "/EMPs",  new[] { "DE*EMP" }),
                                            (RN + "/CSMs",  new[] { "ACME" }),
                                            (RN + "/EMMs",  new[] { "METERS" }),
                                            (RN + "/POs",   new[] { "DE*PAR" })
                                        })
            {

                var (response, content) = await Send(HttpMethod.Get, path);
                var (unknown,  _)       = await Send(HttpMethod.Get, path.Replace("rn1", "rn2"));

                var list = JArray.Parse(System.Text.Encoding.UTF8.GetString(content));

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(response.StatusCode,                                 Is.EqualTo(HttpStatusCode.OK), path);
                    Assert.That(HeaderOf(response, "ETag"),                          Is.EqualTo(ETagOf("json", content)), path);
                    Assert.That(HeaderOf(response, "Access-Control-Allow-Origin"),   Is.EqualTo("*"), path);
                    Assert.That(list.Select(entry => entry["@id"]!.Value<String>()), Is.EqualTo(ids), path);
                    Assert.That(list.All(entry => ((JObject) entry).Properties().All(property => property.Name is "@id" or "name" or "description")),  Is.True, path);
                    Assert.That(unknown.StatusCode,                                  Is.EqualTo(HttpStatusCode.NotFound), path);
                }

            }

        }

        #endregion

        #region Kept_representations_follow_the_versions()

        [Test]
        public async Task Kept_representations_follow_the_versions()
        {

            var paths   = new[] { RN, RN + "/EMPs", EM, CSO, CSO + "?without=chargingPools", CP, CP + "?expand=gridConnectionPoint", GO, GO + "?without=gridConnectionPoints", GO + "/GCPs", GC, "RNs" };

            async Task<Dictionary<String, (String ETag, Byte[] Content)>> GetAll()
            {
                var all = new Dictionary<String, (String, Byte[])>();
                foreach (var path in paths)
                {
                    var (response, content) = await Send(HttpMethod.Get, path);
                    Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), path);
                    all[path] = (HeaderOf(response, "ETag")!, content);
                }
                return all;
            }

            var before  = await GetAll();
            var again   = await GetAll();

            var rn1     = history!.Head.RoamingNetwork;
            rn1.TryGetEMobilityProviderById(EMP, out var emp);
            rn1.TryGetGridConnectionPoint  (GCP, out _, out var gcp);

            // A new name of the e-mobility provider, and a new rating of the grid connection point.
            history.Commit(
                rn1.CreateSignedChangeSet(
                    [
                        RoamingNetworkChange.UpdateProperty(emp!, "name",                  null, new JObject(new JProperty("en", "Another Mobility"))),
                        RoamingNetworkChange.UpdateProperty(gcp!, "contractedImportPower", null, 200000)
                    ],
                    CreatedAt: DateTimeOffset.Parse("2026-10-14T12:00:00Z")
                )
            );

            var after   = await GetAll();

            poiAPI!.AddRoamingNetwork(NewNetwork("rn0"), EventTracking_Id.New);

            var (roamingNetworks, roamingNetworksContent) = await Send(HttpMethod.Get, "RNs");

            using (Assert.EnterMultipleScope())
            {

                // Asked again: the very same content and ETags.
                foreach (var path in paths)
                {
                    Assert.That(again[path].ETag,     Is.EqualTo(before[path].ETag),     path);
                    Assert.That(again[path].Content,  Is.EqualTo(before[path].Content),  path);
                }

                // Changed: the roaming network, the e-mobility provider and their list, the grid connection point,
                // its grid operator - also without them, as its last change is the one of its grid connection point -
                // and their list, and the view of the pool embedding it.
                foreach (var path in new[] { RN, RN + "/EMPs", EM, GC, GO, GO + "?without=gridConnectionPoints", GO + "/GCPs", CP + "?expand=gridConnectionPoint" })
                    Assert.That(after[path].ETag,     Is.Not.EqualTo(before[path].ETag), path);

                // Unchanged: the operator, also without its pools, and its pool, and the names of the roaming networks.
                foreach (var path in new[] { CSO, CSO + "?without=chargingPools", CP, "RNs" })
                    Assert.That(after[path].ETag,     Is.EqualTo(before[path].ETag),     path);

                // Every ETag is still the one of the content.
                foreach (var path in paths.Where(path => path != RN))
                    Assert.That(after[path].ETag,     Is.EqualTo(ETagOf("json", after[path].Content)), path);

                Assert.That(ParseJSON(after[EM].Content)["name"]!["en"]!.Value<String>(),                                    Is.EqualTo("Another Mobility"));
                Assert.That(ParseJSON(after[CP + "?expand=gridConnectionPoint"].Content)["gridConnectionPoint"]!["contractedImportPower"]!.Value<Decimal>(),
                            Is.EqualTo(200000m));

                // Another roaming network served: another list.
                Assert.That(HeaderOf(roamingNetworks, "ETag"),                                                                Is.Not.EqualTo(after["RNs"].ETag));
                Assert.That(JArray.Parse(System.Text.Encoding.UTF8.GetString(roamingNetworksContent)).Count,                  Is.EqualTo(2));

            }

        }

        #endregion

        #region JSON_and_CBOR_are_kept_apart()

        [Test]
        public async Task JSON_and_CBOR_are_kept_apart()
        {

            var (json, jsonContent)  = await Send(HttpMethod.Get, EV);
            var (cbor, cborContent)  = await Send(HttpMethod.Get, EV, Accept: "application/cbor");
            var (list, listContent)  = await Send(HttpMethod.Get, RN + "/GOs", Accept: "application/cbor");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(HeaderOf(json, "ETag"),                        Is.EqualTo(ETagOf("json", jsonContent)));
                Assert.That(HeaderOf(cbor, "ETag"),                        Is.EqualTo(ETagOf("cbor", cborContent)));
                Assert.That(cbor.Content.Headers.ContentType?.MediaType,   Is.EqualTo("application/cbor"));
                Assert.That(HeaderOf(list, "ETag"),                        Is.EqualTo(ETagOf("cbor", listContent)));
            }

        }

        #endregion

        #region A_charging_pool_embeds_its_grid_connection_point_on_request()

        [Test]
        public async Task A_charging_pool_embeds_its_grid_connection_point_on_request()
        {

            var (named,    namedContent)     = await Send(HttpMethod.Get, CP);
            var (expanded, expandedContent)  = await Send(HttpMethod.Get, CP + "?expand=gridConnectionPoint");
            var (unknown,  _)                = await Send(HttpMethod.Get, CP + "?expand=chargingStations");
            var (unknownFirst, _)            = await Send(HttpMethod.Get, CP + "?expand=chargingStations,gridConnectionPoint");

            var namedJSON     = ParseJSON(namedContent);
            var expandedJSON  = ParseJSON(expandedContent);
            var point         = expandedJSON["gridConnectionPoint"] as JObject;

            using (Assert.EnterMultipleScope())
            {

                Assert.That(namedJSON["gridConnectionPointId"]!.Value<String>(),      Is.EqualTo("connection-42"));
                Assert.That(namedJSON["gridConnectionPoint"],                          Is.Null);

                Assert.That(expanded.StatusCode,                                       Is.EqualTo(HttpStatusCode.OK));
                Assert.That(expandedJSON["gridConnectionPointId"],                     Is.Null);
                Assert.That(point?["@id"]?.Value<String>(),                            Is.EqualTo("connection-42"));
                Assert.That(point?["gridOperatorId"]?.Value<String>(),                 Is.EqualTo("DE*NET"));
                Assert.That(point?["voltageLevel"]?.Value<String>(),                   Is.EqualTo("LowVoltage"));

                // Another representation: its own ETag, of its own content.
                Assert.That(HeaderOf(expanded, "ETag"),                                Is.EqualTo(ETagOf("json", expandedContent)));
                Assert.That(HeaderOf(expanded, "ETag"),                                Is.Not.EqualTo(HeaderOf(named, "ETag")));

                Assert.That(unknown.StatusCode,                                        Is.EqualTo(HttpStatusCode.BadRequest));

                // Also before a comma: Hermod splits the value there.
                Assert.That(unknownFirst.StatusCode,                                   Is.EqualTo(HttpStatusCode.BadRequest));

            }

        }

        #endregion

        #region An_operator_leaves_out_its_charging_pools_on_request()

        [Test]
        public async Task An_operator_leaves_out_its_charging_pools_on_request()
        {

            var (whole,   wholeContent)  = await Send(HttpMethod.Get, CSO);
            var (slim,    slimContent)   = await Send(HttpMethod.Get, CSO + "?without=chargingPools");
            var (again,   _)             = await Send(HttpMethod.Get, CSO + "?without=chargingPools");
            var (unknown, _)             = await Send(HttpMethod.Get, CSO + "?without=gridConnectionPoints");

            var wholeJSON  = ParseJSON(wholeContent);
            var slimJSON   = ParseJSON(slimContent);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(wholeJSON["chargingPools"],                                Is.Not.Null);

                Assert.That(slim.StatusCode,                                           Is.EqualTo(HttpStatusCode.OK));
                Assert.That(slimJSON["chargingPools"],                                 Is.Null);
                Assert.That(slimJSON["@id"]!.Value<String>(),                          Is.EqualTo("DE*GEF"));

                // Everything else as the whole operator has it.
                wholeJSON.Remove("chargingPools");
                Assert.That(JToken.DeepEquals(slimJSON, wholeJSON),                    Is.True);

                // Another representation: its own ETag, of its own content, kept with the version.
                Assert.That(HeaderOf(slim, "ETag"),                                    Is.EqualTo(ETagOf("json", slimContent)));
                Assert.That(HeaderOf(slim, "ETag"),                                    Is.Not.EqualTo(HeaderOf(whole, "ETag")));
                Assert.That(HeaderOf(again, "ETag"),                                   Is.EqualTo(HeaderOf(slim, "ETag")));

                Assert.That(unknown.StatusCode,                                        Is.EqualTo(HttpStatusCode.BadRequest));

            }

        }

        #endregion

        #region A_grid_operator_leaves_out_its_grid_connection_points_which_are_listed()

        [Test]
        public async Task A_grid_operator_leaves_out_its_grid_connection_points_which_are_listed()
        {

            var (whole,    wholeContent)  = await Send(HttpMethod.Get, GO);
            var (slim,     slimContent)   = await Send(HttpMethod.Get, GO + "?without=gridConnectionPoints");
            var (unknown,  _)             = await Send(HttpMethod.Get, GO + "?without=chargingPools");
            var (list,     listContent)   = await Send(HttpMethod.Get, GO + "/GCPs");
            var (found,    foundContent)  = await Send(HttpMethod.Get, GO + "/GCPs?match=connection-42&limit=1");
            var (none,     noneContent)   = await Send(HttpMethod.Get, GO + "/GCPs?match=nowhere");
            var (nobody,   _)             = await Send(HttpMethod.Get, RN + "/GOs/DE*XXX/GCPs");

            var wholeJSON  = ParseJSON(wholeContent);
            var slimJSON   = ParseJSON(slimContent);
            var listJSON   = JArray.Parse(System.Text.Encoding.UTF8.GetString(listContent));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(slim.StatusCode,                                           Is.EqualTo(HttpStatusCode.OK));
                Assert.That(slimJSON["gridConnectionPoints"],                          Is.Null);
                Assert.That(HeaderOf(slim, "ETag"),                                    Is.EqualTo(ETagOf("json", slimContent)));
                Assert.That(HeaderOf(slim, "ETag"),                                    Is.Not.EqualTo(HeaderOf(whole, "ETag")));
                Assert.That(unknown.StatusCode,                                        Is.EqualTo(HttpStatusCode.BadRequest));

                // The list is what the grid operator embeds, and the rest of it is the slim grid operator.
                Assert.That(JToken.DeepEquals(listJSON, wholeJSON["gridConnectionPoints"]),  Is.True);
                wholeJSON.Remove("gridConnectionPoints");
                Assert.That(JToken.DeepEquals(slimJSON, wholeJSON),                    Is.True);

                Assert.That(list.StatusCode,                                           Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(list, "X-Total-Count"),                           Is.EqualTo("1"));
                Assert.That(HeaderOf(list, "ETag"),                                    Is.EqualTo(ETagOf("json", listContent)));

                Assert.That(JArray.Parse(System.Text.Encoding.UTF8.GetString(foundContent))[0]["@id"]!.Value<String>(),  Is.EqualTo("connection-42"));
                Assert.That(HeaderOf(found, "X-Filtered-Count"),                       Is.EqualTo("1"));
                Assert.That(JArray.Parse(System.Text.Encoding.UTF8.GetString(noneContent)),                               Is.Empty);
                Assert.That(HeaderOf(none, "X-Filtered-Count"),                        Is.EqualTo("0"));

                Assert.That(nobody.StatusCode,                                         Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

        #region A_manufacturer_leaves_out_its_models_which_are_listed()

        [Test]
        public async Task A_manufacturer_leaves_out_its_models_which_are_listed()
        {

            var (whole,    wholeContent)  = await Send(HttpMethod.Get, CM);
            var (slim,     slimContent)   = await Send(HttpMethod.Get, CM + "?without=chargingStationModels");
            var (unknown,  _)             = await Send(HttpMethod.Get, CM + "?without=energyMeterModels");
            var (list,     listContent)   = await Send(HttpMethod.Get, CM + "/models");
            var (model,    modelContent)  = await Send(HttpMethod.Get, CM + "/models/Wallbox-22");
            var (missing,  _)             = await Send(HttpMethod.Get, CM + "/models/Wallbox-99");
            var (meters,   metersContent) = await Send(HttpMethod.Get, MM + "/models");
            var (nobody,   _)             = await Send(HttpMethod.Get, RN + "/CSMs/NOBODY/models");

            var wholeJSON  = ParseJSON(wholeContent);
            var slimJSON   = ParseJSON(slimContent);
            var listJSON   = JArray.Parse(System.Text.Encoding.UTF8.GetString(listContent));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(slim.StatusCode,                                           Is.EqualTo(HttpStatusCode.OK));
                Assert.That(slimJSON["chargingStationModels"],                         Is.Null);
                Assert.That(HeaderOf(slim, "ETag"),                                    Is.Not.EqualTo(HeaderOf(whole, "ETag")));
                Assert.That(unknown.StatusCode,                                        Is.EqualTo(HttpStatusCode.BadRequest));

                // The list is what the manufacturer embeds, in the order of their identifications.
                Assert.That(JToken.DeepEquals(listJSON, wholeJSON["chargingStationModels"]),  Is.True);
                Assert.That(listJSON.Select(entry => entry["@id"]!.Value<String>()),   Is.EqualTo(new[] { "Wallbox-11", "Wallbox-22" }));
                Assert.That(HeaderOf(list, "X-Total-Count"),                           Is.EqualTo("2"));

                Assert.That(model.StatusCode,                                          Is.EqualTo(HttpStatusCode.OK));
                Assert.That(ParseJSON(modelContent)["versions"]![0]!.Value<String>(), Is.EqualTo("1.0"));
                Assert.That(missing.StatusCode,                                        Is.EqualTo(HttpStatusCode.NotFound));

                Assert.That(JArray.Parse(System.Text.Encoding.UTF8.GetString(metersContent)).Single()["@id"]!.Value<String>(),  Is.EqualTo("M-500"));
                Assert.That(nobody.StatusCode,                                         Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

        #region A_parking_operator_leaves_out_what_it_embeds_which_is_listed()

        [Test]
        public async Task A_parking_operator_leaves_out_what_it_embeds_which_is_listed()
        {

            var (whole,     wholeContent)  = await Send(HttpMethod.Get, PO);
            var (slim,      slimContent)   = await Send(HttpMethod.Get, PO + "?without=parkingSpaces,parkingGarages");
            var (reversed,  _)             = await Send(HttpMethod.Get, PO + "?without=parkingGarages&without=parkingSpaces");
            var (encoded,   _)             = await Send(HttpMethod.Get, PO + "?without=parkingGarages%2CparkingSpaces");
            var (unknown,   _)             = await Send(HttpMethod.Get, PO + "?without=chargingPools");
            var (spaces,    spacesContent) = await Send(HttpMethod.Get, PO + "/PSs");
            var (garages,   garagesContent)= await Send(HttpMethod.Get, PO + "/PGs?limit=10");
            var (nobody,    _)             = await Send(HttpMethod.Get, RN + "/POs/DE*XXX/PSs");

            var wholeJSON  = ParseJSON(wholeContent);
            var slimJSON   = ParseJSON(slimContent);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(slim.StatusCode,                                           Is.EqualTo(HttpStatusCode.OK));
                Assert.That(slimJSON["parkingSpaces"],                                 Is.Null);
                Assert.That(slimJSON["parkingGarages"],                                Is.Null);
                Assert.That(HeaderOf(slim, "ETag"),                                    Is.EqualTo(ETagOf("json", slimContent)));
                Assert.That(HeaderOf(slim, "ETag"),                                    Is.Not.EqualTo(HeaderOf(whole, "ETag")));
                Assert.That(HeaderOf(reversed, "ETag"),                                Is.EqualTo(HeaderOf(slim, "ETag")), "the same view, whatever the order");
                Assert.That(HeaderOf(encoded,  "ETag"),                                Is.EqualTo(HeaderOf(slim, "ETag")), "the same view, its comma encoded");
                Assert.That(unknown.StatusCode,                                        Is.EqualTo(HttpStatusCode.BadRequest));

                // The list is what the parking operator embeds, and the rest of it is the slim parking operator.
                Assert.That(JToken.DeepEquals(ParseList(spacesContent), wholeJSON["parkingSpaces"]),  Is.True);
                wholeJSON.Remove("parkingSpaces");
                Assert.That(JToken.DeepEquals(slimJSON, wholeJSON),                    Is.True);

                Assert.That(HeaderOf(spaces, "X-Total-Count"),                         Is.EqualTo("1"));
                Assert.That(garages.StatusCode,                                        Is.EqualTo(HttpStatusCode.OK));
                Assert.That(ParseList(garagesContent),                                 Is.Empty);
                Assert.That(HeaderOf(garages, "X-Total-Count"),                        Is.EqualTo("0"));

                Assert.That(nobody.StatusCode,                                         Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

        #region What_a_parking_operator_embeds_is_served_one_by_one()

        [Test]
        public async Task What_a_parking_operator_embeds_is_served_one_by_one()
        {

            var (parkingOperator, operatorContent)  = await Send(HttpMethod.Get, PO);
            var (space,           spaceContent)     = await Send(HttpMethod.Get, PO + "/PSs/space-1");
            var (again,           _)                = await Send(HttpMethod.Get, PO + "/PSs/space-1");
            var (unknown,         _)                = await Send(HttpMethod.Get, PO + "/PSs/space-2");
            var (noGarage,        _)                = await Send(HttpMethod.Get, PO + "/PGs/garage-1");
            var (nobody,          _)                = await Send(HttpMethod.Get, RN + "/POs/DE*XXX/PSs/space-1");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(space.StatusCode,                                          Is.EqualTo(HttpStatusCode.OK));

                // The part of the parking operator it is, with the ETag of its content, kept with its version.
                Assert.That(JToken.DeepEquals(ParseJSON(spaceContent), ParseJSON(operatorContent)["parkingSpaces"]![0]),  Is.True);
                Assert.That(HeaderOf(space, "ETag"),                                   Is.EqualTo(ETagOf("json", spaceContent)));
                Assert.That(HeaderOf(again, "ETag"),                                   Is.EqualTo(HeaderOf(space, "ETag")));

                Assert.That(unknown.StatusCode,                                        Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(noGarage.StatusCode,                                       Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(nobody.StatusCode,                                         Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

        #region An_entity_is_the_part_its_roaming_network_embeds()

        [Test]
        public async Task An_entity_is_the_part_its_roaming_network_embeds()
        {

            var json         = history!.Head.RoamingNetwork.ToJSON();
            var embeddedPool = (JObject) json["chargingStationOperators"]![0]!["chargingPools"]![0]!;

            var (response, content) = await Send(HttpMethod.Get, CP);

            Assert.That(JToken.DeepEquals(ParseJSON((content)), embeddedPool),  Is.True);
            Assert.That(HeaderOf(response, "ETag"),
                        Is.EqualTo(ETagOf("json", CanonicalJSON.ToUTF8Bytes(embeddedPool))));

        }

        #endregion

        #region The_roaming_network_has_its_own_ETags_in_JSON_and_in_CBOR()

        [Test]
        public async Task The_roaming_network_has_its_own_ETags_in_JSON_and_in_CBOR()
        {

            var roamingNetwork        = history!.Head.RoamingNetwork;

            var (json, jsonContent)   = await Send(HttpMethod.Get, RN);
            var (cbor, cborContent)   = await Send(HttpMethod.Get, RN, Accept: "application/cbor");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(json.StatusCode,                                  Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(json, "ETag"),                           Is.EqualTo($"\"{roamingNetwork.ETags.Single(eTag => eTag.Format == ETagFormat.JSON).ToString(ETagDigestEncoding.HEX)}\""));
                Assert.That(roamingNetwork.ValidateETags(ParseJSON((jsonContent)), out var jsonError),  Is.True, jsonError);

                Assert.That(cbor.StatusCode,                                  Is.EqualTo(HttpStatusCode.OK));
                Assert.That(cbor.Content.Headers.ContentType?.MediaType,      Is.EqualTo("application/cbor"));
                Assert.That(HeaderOf(cbor, "ETag"),                           Is.EqualTo($"\"{roamingNetwork.ETags.Single(eTag => eTag.Format == ETagFormat.CBOR).ToString(ETagDigestEncoding.HEX)}\""));
                Assert.That(roamingNetwork.ValidateETags(CBORValue.Parse(cborContent), out var cborError),  Is.True, cborError);
                Assert.That(HeaderOf(cbor, "Vary"),                           Does.Contain("Accept"));

            }

        }

        #endregion

        #region An_entity_in_CBOR_has_the_ETag_of_its_CBOR()

        [Test]
        public async Task An_entity_in_CBOR_has_the_ETag_of_its_CBOR()
        {

            var (response, content) = await Send(HttpMethod.Get, EV, Accept: "application/cbor");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode,                              Is.EqualTo(HttpStatusCode.OK));
                Assert.That(response.Content.Headers.ContentType?.MediaType,  Is.EqualTo("application/cbor"));
                Assert.That(HeaderOf(response, "ETag"),                       Is.EqualTo(ETagOf("cbor", content)));
                Assert.That(CBORJSON.ToJSON(CBORValue.Parse(content))!["@id"]!.Value<String>(),  Is.EqualTo("DE*GEF*E1*1"));
            }

        }

        #endregion

        #region A_known_ETag_is_not_modified_and_a_new_version_is()

        [Test]
        public async Task A_known_ETag_is_not_modified_and_a_new_version_is()
        {

            var (first, _)        = await Send(HttpMethod.Get, RN);
            var eTag              = HeaderOf(first, "ETag")!;

            var (same, sameBody)  = await Send(HttpMethod.Get, RN, IfNoneMatch: eTag);

            history!.Commit(
                history.Head.RoamingNetwork.CreateSignedChangeSet(
                    [ RoamingNetworkChange.UpdateProperty(history.Head.RoamingNetwork, "dataSource", null, "v1") ],
                    CreatedAt: DateTimeOffset.Parse("2026-10-11T12:00:00Z")
                )
            );

            var (changed, _)      = await Send(HttpMethod.Get, RN, IfNoneMatch: eTag);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(same.StatusCode,             Is.EqualTo(HttpStatusCode.NotModified));
                Assert.That(sameBody,                    Is.Empty);
                Assert.That(HeaderOf(same, "ETag"),      Is.EqualTo(eTag));
                Assert.That(changed.StatusCode,          Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(changed, "ETag"),   Is.Not.EqualTo(eTag));
            }

        }

        #endregion

        #region HEAD_has_the_ETag_without_content()

        [Test]
        public async Task HEAD_has_the_ETag_without_content()
        {

            var (get,  _)        = await Send(HttpMethod.Get,  CS);
            var (head, content)  = await Send(HttpMethod.Head, CS);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(head.StatusCode,          Is.EqualTo(HttpStatusCode.OK));
                Assert.That(content,                  Is.Empty);
                Assert.That(HeaderOf(head, "ETag"),   Is.EqualTo(HeaderOf(get, "ETag")));
            }

        }

        #endregion

        #region OPTIONS_permits_every_origin()

        [Test]
        public async Task OPTIONS_permits_every_origin()
        {

            using var request = new HttpRequestMessage(HttpMethod.Options, EV);
            request.Headers.Add("Origin",                          "https://example.org");
            request.Headers.Add("Access-Control-Request-Method",   "GET");
            request.Headers.Add("Access-Control-Request-Headers",  "if-none-match");

            using var response = await httpClient!.SendAsync(request);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode,                                         Is.EqualTo(HttpStatusCode.NoContent));
                Assert.That(HeaderOf(response, "Access-Control-Allow-Origin"),           Is.EqualTo("*"));
                Assert.That(HeaderOf(response, "Access-Control-Allow-Methods"),          Does.Contain("GET").And.Contain("HEAD").And.Contain("OPTIONS"));
                Assert.That(HeaderOf(response, "Access-Control-Allow-Headers"),          Does.Contain("If-None-Match"));
                Assert.That(HeaderOf(response, "Allow"),                                 Does.Contain("GET").And.Contain("HEAD"));
            }

        }

        #endregion

        #region Unknown_entities_are_not_found()

        [Test]
        public async Task Unknown_entities_are_not_found()
        {

            foreach (var path in new[] {
                                     "RNs/rn2",
                                     RN  + "/CSOs/DE*XYZ",
                                     CSO + "/CPs/DE*GEF*P2",
                                     CP  + "/CSs/DE*GEF*S2",
                                     CS  + "/EVSEs/DE*GEF*E1*2",
                                     RN  + "/GOs/DE*XYZ",
                                     RN  + "/GOs/DE*NE2/GCPs/connection-42",   // another grid operator's connection point
                                     GO  + "/GCPs/connection-43",
                                     RN  + "/EMPs/DE*XYZ",
                                     RN  + "/CSMs/ZAP",
                                     RN  + "/EMMs/ZAP",
                                     RN  + "/POs/DE*XYZ"
                                 })
            {

                var (response, content) = await Send(HttpMethod.Get, path);

                using (Assert.EnterMultipleScope())
                {
                    Assert.That(response.StatusCode,                                Is.EqualTo(HttpStatusCode.NotFound), path);
                    Assert.That(HeaderOf(response, "Access-Control-Allow-Origin"),  Is.EqualTo("*"), path);
                    Assert.That(ParseJSON((content))["description"],  Is.Not.Null, path);
                }

            }

        }

        #endregion

        #region A_roaming_network_without_history_is_served_too()

        [Test]
        public async Task A_roaming_network_without_history_is_served_too()
        {

            var roamingNetwork = NewNetwork("rn3");

            poiAPI!.AddRoamingNetwork(roamingNetwork, EventTracking_Id.New);

            var (response, content) = await Send(HttpMethod.Get, "RNs/rn3");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode,        Is.EqualTo(HttpStatusCode.OK));
                Assert.That(HeaderOf(response, "ETag"),
                            Is.EqualTo(ETagOf("json", CanonicalJSON.ToUTF8Bytes(roamingNetwork.ToJSON(IncludeETags: false)))));
                Assert.That(ParseJSON((content))["@id"]!.Value<String>(),  Is.EqualTo("rn3"));
            }

        }

        #endregion

    }

}
