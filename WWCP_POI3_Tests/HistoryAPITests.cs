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

using Newtonsoft.Json.Linq;

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.History
{

    /// <summary>
    /// A follower catching up with a leader over HTTP.
    /// </summary>
    [TestFixture]
    public class HistoryAPITests
    {

        #region Data

        private HTTPServer?             httpServer;
        private POI_API?                poiAPI;
        private RoamingNetworkHistory?  leader;
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

            leader      = RoamingNetworkHistory.Start(
                              new RoamingNetwork(
                                  Id:          RoamingNetwork_Id.Parse("rn1"),
                                  Name:        I18NString.Create(Languages.en, "Roaming Network 1"),
                                  DataSource:  "v0",
                                  Created:     DateTimeOffset.Parse("2026-01-01T00:00:00Z")
                              )
                          );

            poiAPI.AddRoamingNetworkHistory(leader);

            httpClient  = new HttpClient {
                              BaseAddress = new Uri($"http://127.0.0.1:{httpServer.TCPPort}/roamingNetworks/rn1/history/")
                          };

        }

        [TearDown]
        public async Task TearDown()
        {

            httpClient?.Dispose();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

        }

        #endregion


        #region Publish(Value, Minute)

        private RoamingNetworkPublishResult Publish(String Value, Int32 Minute)

            => leader!.Publish(
                   leader.Head.RoamingNetwork.CreateChangeSet(
                       [ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, Value) ],
                       Description:  I18NString.Create(Languages.en, "Data source " + Value),
                       CreatedAt:    DateTimeOffset.Parse("2026-10-11T12:00:00Z").AddMinutes(Minute)
                   )
               );

        #endregion

        #region Get(Path)

        private async Task<(HttpStatusCode StatusCode, String Text)> Get(String Path)
        {

            using var response = await httpClient!.GetAsync(Path);

            return (response.StatusCode, await response.Content.ReadAsStringAsync());

        }

        #endregion


        #region A_follower_catches_up_over_HTTP()

        [Test]
        public async Task A_follower_catches_up_over_HTTP()
        {

            Publish("v1", 1);

            // 1. Start from a snapshot of the leader's head.
            var snapshot = await Get("snapshot");
            Assert.That(snapshot.StatusCode,  Is.EqualTo(HttpStatusCode.OK), snapshot.Text);
            Assert.That(RoamingNetworkHistory.TryFromSnapshot(snapshot.Text, out var follower, out var error1),  Is.True, error1);

            Publish("v2", 2);
            Publish("v3", 3);

            // 2. Look at what is new: the metadata only.
            var head   = await Get("head");
            var infos  = await Get($"commitInfos?since={follower!.Head.Commit.Id}");

            // 3. Fetch the commits completely, one page of one commit, and one commit by its identification.
            var page   = await Get($"commits?since={follower.Head.Commit.Id}&max=1");
            var single = await Get($"commits/{leader!.Head.Commit.Id}");

            var infosJSON   = JArray.Parse(infos.Text);
            var pageJSON    = JArray.Parse(page.Text);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(head.StatusCode,                                       Is.EqualTo(HttpStatusCode.OK));
                Assert.That(JObject.Parse(head.Text)["revision"]!.Value<Int32>(),  Is.EqualTo(3));

                Assert.That(infosJSON.Count,                                       Is.EqualTo(2));
                Assert.That(infosJSON[0]["description"]!["en"]!.Value<String>(),   Is.EqualTo("Data source v2"));
                Assert.That(infosJSON[0]["changeSet"],                             Is.Null);

                Assert.That(pageJSON.Count,                                        Is.EqualTo(1));
                Assert.That(single.StatusCode,                                     Is.EqualTo(HttpStatusCode.OK));

            }

            var commits = new[] {
                              RoamingNetworkCommit.Parse(pageJSON[0].ToString()),
                              RoamingNetworkCommit.Parse(single.Text)
                          };

            Assert.That(follower.TryAppend(commits, out var appended, out var error2),  Is.True, error2);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(appended,                                  Is.EqualTo(2));
                Assert.That(follower.Head.Commit.Id,                   Is.EqualTo(leader.Head.Commit.Id));
                Assert.That(follower.Head.RoamingNetwork.DataSource,   Is.EqualTo("v3"));
            }

        }

        #endregion

        #region Errors_are_answered_in_JSON()

        [Test]
        public async Task Errors_are_answered_in_JSON()
        {

            var unknownCommit   = await Get($"commits?since={new String('0', 64)}");
            var invalidCommit   = await Get("commits?since=nope");
            var invalidMax      = await Get("commits?max=0");
            var missingCommit   = await Get($"commits/{new String('0', 64)}");

            using var unknownNetworkResponse = await httpClient!.GetAsync("/roamingNetworks/rn2/history/head");

            using (Assert.EnterMultipleScope())
            {

                Assert.That(unknownCommit.StatusCode,                   Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(JObject.Parse(unknownCommit.Text)["description"]!.Value<String>(),  Does.Contain("start from a new snapshot"));

                Assert.That(invalidCommit.StatusCode,                   Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(invalidMax.StatusCode,                      Is.EqualTo(HttpStatusCode.BadRequest));
                Assert.That(missingCommit.StatusCode,                   Is.EqualTo(HttpStatusCode.NotFound));
                Assert.That(unknownNetworkResponse.StatusCode,          Is.EqualTo(HttpStatusCode.NotFound));

            }

        }

        #endregion

    }

}
