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
    /// The QUERY interface: the charging pools of all operators, and every list, searched and paged.
    /// </summary>
    [TestFixture]
    public class QueryAPITests
    {

        #region Data

        private static readonly DateTimeOffset  Created  = DateTimeOffset.Parse("2026-01-01T00:00:00Z");

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
        /// Two operators with three charging pools: two in Jena, one in Berlin.
        /// </summary>
        private static RoamingNetwork NewNetwork()

            => new RoamingNetwork(RoamingNetwork_Id.Parse("rn1"),
                    Created:                   Created,
                    ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                             I18NString.Create(Languages.en, "GraphDefined"),
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P2"),
                                                                                                                I18NString.Create(Languages.de, "Berlin Mitte"),
                                                                                                                Created: Created),
                                                                                               new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                                                                I18NString.Create(Languages.de, "Jena Paradies"),
                                                                                                                Created: Created) ]),
                                                 new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*EXA"),
                                                                             I18NString.Create(Languages.en, "Example Charging"),
                                                                             Created:        Created,
                                                                             ChargingPools:  [ new ChargingPool(ChargingPool_Id.Parse("DE*EXA*P1"),
                                                                                                                I18NString.Create(Languages.de, "Jena Nord"),
                                                                                                                Created: Created) ]) ]).WithKeys();

        #endregion

        #region Get(Path)

        private async Task<(HttpResponseMessage Response, Byte[] Content, JArray? List)> Get(String Path)
        {

            var response  = await httpClient!.GetAsync(Path);
            var content   = await response.Content.ReadAsByteArrayAsync();

            return (response,
                    content,
                    response.IsSuccessStatusCode
                        ? (JArray) JToken.ReadFrom(new JsonTextReader(new StringReader(System.Text.Encoding.UTF8.GetString(content))) { DateParseHandling = DateParseHandling.None })
                        : null);

        }

        #endregion

        #region (static) HeaderOf(Response, Name)

        private static String? HeaderOf(HttpResponseMessage Response, String Name)

            => Response.Headers.TryGetValues(Name, out var values)
                   ? String.Join(", ", values)
                   : null;

        #endregion

        #region (static) Ids(List)

        private static String[] Ids(JArray? List)
            => [.. (List ?? []).Select(entry => entry["@id"]!.Value<String>()!)];

        #endregion


        #region The_charging_pools_of_all_operators()

        [Test]
        public async Task The_charging_pools_of_all_operators()
        {

            var (response, content, list) = await Get("RNs/rn1/CPs");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(response.StatusCode,                                    Is.EqualTo(HttpStatusCode.OK));
                Assert.That(Ids(list),                                              Is.EqualTo(new[] { "DE*EXA*P1", "DE*GEF*P1", "DE*GEF*P2" }));
                Assert.That(list![0]["chargingStationOperator"]!["@id"]!.Value<String>(),          Is.EqualTo("DE*EXA"));
                Assert.That(list[0]["chargingStationOperator"]!["name"]!["en"]!.Value<String>(),   Is.EqualTo("Example Charging"));
                Assert.That(list[1]["name"]!["de"]!.Value<String>(),                Is.EqualTo("Jena Paradies"));

                Assert.That(HeaderOf(response, "X-Total-Count"),                    Is.EqualTo("3"));
                Assert.That(HeaderOf(response, "X-Filtered-Count"),                 Is.EqualTo("3"));
                Assert.That(HeaderOf(response, "Link"),                             Is.Null);
                Assert.That(HeaderOf(response, "Access-Control-Expose-Headers"),    Does.Contain("X-Total-Count").And.Contain("Link"));
                Assert.That(HeaderOf(response, "ETag"),
                            Is.EqualTo($"\"json:sha256:hex:{Convert.ToHexStringLower(SHA256.HashData(content))}\""));

            }

        }

        #endregion

        #region Charging_pools_are_searched_by_their_texts_and_their_operators()

        [Test]
        public async Task Charging_pools_are_searched_by_their_texts_and_their_operators()
        {

            var (jena,     _, jenaList)      = await Get("RNs/rn1/CPs?match=jena");
            var (both,     _, bothList)      = await Get("RNs/rn1/CPs?match=JENA%20graphdefined");
            var (notGEF,   _, notGEFList)    = await Get("RNs/rn1/CPs?match=jena%20!graphdefined");
            var (exa,      _, exaList)       = await Get("RNs/rn1/CPs?operatorId=DE*EXA");
            var (byId,     _, byIdList)      = await Get("RNs/rn1/CPs?match=GEF*P2");
            var (none,     _, noneList)      = await Get("RNs/rn1/CPs?match=hamburg");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Ids(jenaList),                              Is.EqualTo(new[] { "DE*EXA*P1", "DE*GEF*P1" }));
                Assert.That(HeaderOf(jena, "X-Total-Count"),            Is.EqualTo("3"));
                Assert.That(HeaderOf(jena, "X-Filtered-Count"),         Is.EqualTo("2"));

                Assert.That(Ids(bothList),                              Is.EqualTo(new[] { "DE*GEF*P1" }));
                Assert.That(Ids(notGEFList),                            Is.EqualTo(new[] { "DE*EXA*P1" }));
                Assert.That(Ids(exaList),                               Is.EqualTo(new[] { "DE*EXA*P1" }));
                Assert.That(Ids(byIdList),                              Is.EqualTo(new[] { "DE*GEF*P2" }));

                Assert.That(none.StatusCode,                            Is.EqualTo(HttpStatusCode.OK));
                Assert.That(noneList,                                   Is.Empty);
                Assert.That(HeaderOf(none, "X-Filtered-Count"),         Is.EqualTo("0"));

            }

        }

        #endregion

        #region A_search_takes_every_word_also_after_a_comma()

        /// <summary>
        /// Words separated by commas, as people write them, or in parameters of their own: every one of them searched for.
        /// Hermod splits a value at its commas, so the words before the last comma were lost.
        /// </summary>
        [Test]
        public async Task A_search_takes_every_word_also_after_a_comma()
        {

            var (comma,     _, commaList)      = await Get("RNs/rn1/CPs?match=paradies,jena");
            var (encoded,   _, encodedList)    = await Get("RNs/rn1/CPs?match=paradies%2C%20jena");
            var (form,      _, formList)       = await Get("RNs/rn1/CPs?match=paradies%2C+jena");
            var (repeated,  _, repeatedList)   = await Get("RNs/rn1/CPs?match=nord&match=jena");
            var (negated,   _, negatedList)    = await Get("RNs/rn1/CPs?match=jena,!nord&limit=1");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Ids(commaList),                             Is.EqualTo(new[] { "DE*GEF*P1" }));
                Assert.That(Ids(encodedList),                           Is.EqualTo(new[] { "DE*GEF*P1" }));
                Assert.That(Ids(formList),                              Is.EqualTo(new[] { "DE*GEF*P1" }));
                Assert.That(Ids(repeatedList),                          Is.EqualTo(new[] { "DE*EXA*P1" }));
                Assert.That(Ids(negatedList),                           Is.EqualTo(new[] { "DE*GEF*P1" }));
                Assert.That(HeaderOf(comma, "X-Filtered-Count"),        Is.EqualTo("1"));

                // The next page keeps every word.
                Assert.That(HeaderOf(negated, "X-Filtered-Count"),      Is.EqualTo("1"));
                Assert.That(HeaderOf(repeated, "Link"),                 Is.Null);

            }

        }

        #endregion

        #region Charging_pools_are_paged()

        [Test]
        public async Task Charging_pools_are_paged()
        {

            var (first,  _, firstList)   = await Get("RNs/rn1/CPs?limit=2");
            var (second, _, secondList)  = await Get("RNs/rn1/CPs?offset=2&limit=2");
            var (beyond, _, beyondList)  = await Get("RNs/rn1/CPs?offset=5&limit=2");
            var (invalid, _, _)          = await Get("RNs/rn1/CPs?offset=minus");
            var (none,    _, _)          = await Get("RNs/rn1/CPs?limit=0");
            var (largest, _, _)          = await Get($"RNs/rn1/CPs?offset=2&limit={POI_API.DefaultMaxListLimit}");
            var (huge,    _, _)          = await Get("RNs/rn1/CPs?offset=2&limit=18446744073709551615");
            var (unknown, _, _)          = await Get("RNs/rn2/CPs");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Ids(firstList),                             Is.EqualTo(new[] { "DE*EXA*P1", "DE*GEF*P1" }));
                Assert.That(HeaderOf(first, "Link"),                    Is.EqualTo("</RNs/rn1/CPs?offset=2&limit=2>; rel=\"next\""));

                Assert.That(Ids(secondList),                            Is.EqualTo(new[] { "DE*GEF*P2" }));
                Assert.That(HeaderOf(second, "Link"),                   Is.EqualTo("</RNs/rn1/CPs?limit=2>; rel=\"prev\""));

                Assert.That(beyondList,                                 Is.Empty);

                Assert.That(invalid.StatusCode,                         Is.EqualTo(HttpStatusCode.BadRequest));

                // A page of none would name itself as the next one; a limit beyond the offset names the first page as the
                // previous one; one above the maximum is refused.
                Assert.That(none.StatusCode,                            Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(HeaderOf(largest, "Link"),                  Is.EqualTo($"</RNs/rn1/CPs?limit={POI_API.DefaultMaxListLimit}>; rel=\"prev\""));
                Assert.That(huge.StatusCode,                            Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(unknown.StatusCode,                         Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

        #region Limits_and_searches_are_bounded()

        /// <summary>
        /// Every word of a search is looked for in the texts of every entry, and a page is calculated for its request:
        /// a limit above MaxListLimit, a match longer than MaxMatchLength or of more than MaxMatchWords words is refused,
        /// in every list; without a limit, a search or a page returns at most MaxListLimit entries, and the whole list,
        /// kept with its version, all of them (POI3-ISSUES.md S12).
        /// </summary>
        [Test]
        public async Task Limits_and_searches_are_bounded()
        {

            var (aboveMax,   _, _)          = await Get($"RNs/rn1/CPs?limit={POI_API.DefaultMaxListLimit + 1}");
            var (listAbove,  _, _)          = await Get($"RNs/rn1/CSOs?limit={POI_API.DefaultMaxListLimit + 1}");
            var (longMatch,  _, _)          = await Get($"RNs/rn1/CPs?match={new String('j', POI_API.DefaultMaxMatchLength + 1)}");
            var (longest,    _, _)          = await Get($"RNs/rn1/CPs?match={new String('j', POI_API.DefaultMaxMatchLength)}");
            var (manyWords,  _, _)          = await Get($"RNs/rn1/CPs?match={String.Join(",", Enumerable.Repeat("jena", POI_API.DefaultMaxMatchWords + 1))}");
            var (manyParts,  _, _)          = await Get($"RNs/rn1/CPs?{String.Join("&", Enumerable.Repeat("match=jena", POI_API.DefaultMaxMatchWords + 1))}");
            var (mostWords,  _, mostList)   = await Get($"RNs/rn1/CPs?match={String.Join(",", Enumerable.Repeat("jena", POI_API.DefaultMaxMatchWords))}");

            poiAPI!.MaxListLimit = 1;

            var (searched,   _, searchedList)  = await Get("RNs/rn1/CPs?match=jena");
            var (whole,      _, wholeList)     = await Get("RNs/rn1/CPs");
            var (aboveOne,   _, _)             = await Get("RNs/rn1/CPs?limit=2");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(aboveMax.StatusCode,                        Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(listAbove.StatusCode,                       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(longMatch.StatusCode,                       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(longest.StatusCode,                         Is.EqualTo(HttpStatusCode.OK));
                Assert.That(manyWords.StatusCode,                       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(manyParts.StatusCode,                       Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(Ids(mostList),                              Is.EqualTo(new[] { "DE*EXA*P1", "DE*GEF*P1" }));

                Assert.That(Ids(searchedList),                          Is.EqualTo(new[] { "DE*EXA*P1" }));
                Assert.That(HeaderOf(searched, "X-Filtered-Count"),     Is.EqualTo("2"));
                Assert.That(HeaderOf(searched, "Link"),                 Is.EqualTo("</RNs/rn1/CPs?match=jena&offset=1>; rel=\"next\""));

                Assert.That(Ids(wholeList),                             Is.EqualTo(new[] { "DE*EXA*P1", "DE*GEF*P1", "DE*GEF*P2" }));
                Assert.That(HeaderOf(whole, "Link"),                    Is.Null);

                Assert.That(aboveOne.StatusCode,                        Is.EqualTo(HttpStatusCode.BadRequest));

            }

        }

        #endregion

        #region Every_list_is_searched_and_paged()

        [Test]
        public async Task Every_list_is_searched_and_paged()
        {

            var (all,     _, allList)      = await Get("RNs/rn1/CSOs");
            var (example, _, exampleList)  = await Get("RNs/rn1/CSOs?match=example");
            var (paged,   _, pagedList)    = await Get("RNs/rn1/CSOs?offset=1&limit=1");
            var (networks, _, networkList) = await Get("RNs?match=rn1");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(Ids(allList),                               Is.EqualTo(new[] { "DE*EXA", "DE*GEF" }));
                Assert.That(HeaderOf(all, "X-Total-Count"),             Is.EqualTo("2"));
                Assert.That(HeaderOf(all, "X-Filtered-Count"),          Is.EqualTo("2"));

                Assert.That(Ids(exampleList),                           Is.EqualTo(new[] { "DE*EXA" }));
                Assert.That(HeaderOf(example, "X-Filtered-Count"),      Is.EqualTo("1"));

                Assert.That(Ids(pagedList),                             Is.EqualTo(new[] { "DE*GEF" }));
                Assert.That(HeaderOf(paged, "Link"),                    Is.EqualTo("</RNs/rn1/CSOs?limit=1>; rel=\"prev\""));

                Assert.That(Ids(networkList),                           Is.EqualTo(new[] { "rn1" }));
                Assert.That(HeaderOf(networks, "X-Total-Count"),        Is.EqualTo("1"));

            }

        }

        #endregion

    }

}
