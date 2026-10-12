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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
using org.GraphDefined.Vanaheimr.Hermod.WebSocket;

#endregion

namespace cloud.charging.open.protocols.WWCP.POI.tests.Sync
{

    /// <summary>
    /// A follower keeping its history in sync with its leader over the WebSocket.
    /// </summary>
    [TestFixture]
    public class FollowerTests
    {

        #region Data

        private const           String          Operator  = nameof(ChargingStationOperator);
        private static readonly DateTimeOffset  T0        = DateTimeOffset.Parse("2026-10-11T12:00:00Z");

        private HTTPServer?              httpServer;
        private POI_API?                 poiAPI;
        private RoamingNetworkHistory?   leader;
        private POIWebSocketServer?      webSocketServer;
        private RoamingNetworkFollower?  follower;

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
            leader      = NewHistory();

            poiAPI.AddRoamingNetworkHistory(leader);
            poiAPI.TryGetHistoryWebSocketServer(leader.RoamingNetworkId, out webSocketServer);

        }

        [TearDown]
        public async Task TearDown()
        {

            if (follower is not null)
                await follower.DisposeAsync();

            if (httpServer is not null)
                await httpServer.DisposeAsync();

            if (poiAPI is not null)
                await poiAPI.DisposeAsync();

            follower = null;

        }

        #endregion


        #region NewHistory(DataSource = "v0")

        private static RoamingNetworkHistory NewHistory(String DataSource = "v0")

            => RoamingNetworkHistory.Start(
                   new RoamingNetwork(
                       Id:                        RoamingNetwork_Id.Parse("rn1"),
                       DataSource:                DataSource,
                       Created:                   DateTimeOffset.Parse("2026-01-01T00:00:00Z"),
                       ChargingStationOperators:  [ new ChargingStationOperator(ChargingStationOperator_Id.Parse("DE*GEF"),
                                                                                I18NString.Create(Languages.en, "GraphDefined"),
                                                                                Created: DateTimeOffset.Parse("2026-01-01T00:00:00Z")) ]
                   )
               );

        #endregion

        #region Publish(Value)

        private RoamingNetworkPublishResult Publish(String Value)

            => leader!.Publish(
                   leader.Head.RoamingNetwork.CreateChangeSet(
                       [ RoamingNetworkChange.UpdateProperty(leader.Head.RoamingNetwork, "dataSource", null, Value) ],
                       Description:  I18NString.Create(Languages.en, "Data source " + Value),
                       CreatedAt:    T0.AddMinutes((Double) leader.Head.Commit.Revision + 1)
                   )
               );

        #endregion

        #region StartFollower(History = null, Options = null)

        private async Task<RoamingNetworkFollower> StartFollower(RoamingNetworkHistory?          History   = null,
                                                                 RoamingNetworkFollowerOptions?  Options   = null)
        {

            follower = new RoamingNetworkFollower(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/roamingNetworks/rn1/history/ws"),
                                                  History,
                                                  Options ?? new RoamingNetworkFollowerOptions {
                                                                 MinReconnectDelay  = TimeSpan.FromMilliseconds(50),
                                                                 MaxReconnectDelay  = TimeSpan.FromMilliseconds(200)
                                                             });

            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            await follower.StartAsync(timeout.Token);

            return follower;

        }

        #endregion

        #region InSync()

        private Boolean InSync()

            => follower?.History?.Head.Commit.Id == leader!.Head.Commit.Id;

        #endregion

        #region Eventually(Condition)

        private static async Task<Boolean> Eventually(Func<Boolean> Condition)
        {

            for (var i = 0; i < 200; i++)
            {

                if (Condition())
                    return true;

                await Task.Delay(25);

            }

            return Condition();

        }

        #endregion


        #region A_follower_starts_from_a_snapshot_and_follows()

        [Test]
        public async Task A_follower_starts_from_a_snapshot_and_follows()
        {

            Publish("v1");

            leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0, out _, out _);

            var replaced  = 0;
            var follower  = new RoamingNetworkFollower(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/roamingNetworks/rn1/history/ws"));
            this.follower = follower;

            follower.OnHistoryReplaced += (_, oldHistory, newHistory) => replaced++;

            await follower.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            var initiallyInSync  = InSync();
            var initialStatus    = follower.History!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status) ? status.Value : null;

            Publish("v2");
            leader.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.AdminStatus, Operator, "DE*GEF", "outOfService", T0, out _, out _);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(initiallyInSync,                Is.True);
                Assert.That(replaced,                       Is.EqualTo(1));
                Assert.That(initialStatus,                  Is.EqualTo("offline"));
                Assert.That(follower.IsConnected,           Is.True);

                Assert.That(await Eventually(InSync),       Is.True, "The follower did not take the pushed commit.");
                Assert.That(follower.History.Head.RoamingNetwork.DataSource,  Is.EqualTo("v2"));

                Assert.That(await Eventually(() => follower.History.RuntimeStatus.TryGet(POIRuntimeStatusKind.AdminStatus, Operator, "DE*GEF", out var adminStatus) &&
                                                   adminStatus.Value == "outOfService"),
                            Is.True,
                            "The follower did not take the pushed admin status.");

            }

        }

        #endregion

        #region Announced_and_large_commits_are_fetched()

        [TestCase(false, Description = "Subscribed to the announcements only")]
        [TestCase(true,  Description = "Subscribed to complete commits, but they are too large")]
        public async Task Announced_and_large_commits_are_fetched(Boolean PushCompleteCommits)
        {

            poiAPI!.MaxPushedCommitSize = 100;

            await StartFollower(Options: new RoamingNetworkFollowerOptions {
                                             PushCompleteCommits  = PushCompleteCommits,
                                             MinReconnectDelay    = TimeSpan.FromMilliseconds(50)
                                         });

            Publish("v1");
            Publish("v2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(InSync),                            Is.True);
                Assert.That(follower!.History!.Head.RoamingNetwork.DataSource,  Is.EqualTo("v2"));
                Assert.That(follower.Reconnects,                                 Is.Zero);
            }

        }

        #endregion

        #region A_follower_continues_its_own_history()

        [Test]
        public async Task A_follower_continues_its_own_history()
        {

            RoamingNetworkHistory.TryFromSnapshot(leader!.ToSnapshotJSON(), out var own, out _);

            Publish("v1");
            Publish("v2");
            Publish("v3");

            var follower = await StartFollower(own);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(follower.History,                Is.SameAs(own), "The follower started again instead of catching up.");
                Assert.That(InSync(),                        Is.True);
                Assert.That(own!.Head.Commit.Revision,       Is.EqualTo(3));
            }

        }

        #endregion

        #region A_follower_of_a_head_unknown_to_the_leader_starts_again()

        [Test]
        public async Task A_follower_of_a_head_unknown_to_the_leader_starts_again()
        {

            // The same roaming network, but another history: its head is unknown to the leader.
            var other     = NewHistory("other");
            var replaced  = new List<(RoamingNetworkHistory? Old, RoamingNetworkHistory New)>();

            follower = new RoamingNetworkFollower(new Uri($"ws://127.0.0.1:{httpServer!.TCPPort}/roamingNetworks/rn1/history/ws"), other);
            follower.OnHistoryReplaced += (_, oldHistory, newHistory) => replaced.Add((oldHistory, newHistory));

            await follower.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(replaced.Count,             Is.EqualTo(1));
                Assert.That(replaced[0].Old,            Is.SameAs(other));
                Assert.That(follower.History,           Is.Not.SameAs(other));
                Assert.That(InSync(),                   Is.True);
            }

        }

        #endregion

        #region A_follower_reconnects_and_catches_up()

        [Test]
        public async Task A_follower_reconnects_and_catches_up()
        {

            var follower = await StartFollower();

            // The leader throws everybody off...
            foreach (var session in webSocketServer!.Sessions.ToArray())
                await session.Connection.Close(WebSocketFrame.ClosingStatusCode.GoingAway);

            // ...and changes while the follower may still be away.
            Publish("v1");
            leader!.RuntimeStatus.TryUpdate(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0, out _, out _);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(await Eventually(() => follower.Reconnects > 0 && follower.IsConnected),  Is.True, "The follower did not connect again.");
                Assert.That(await Eventually(InSync),                                                 Is.True);
                Assert.That(await Eventually(() => follower.History!.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var status) &&
                                                   status.Value == "offline"),
                            Is.True);
            }

        }

        #endregion

        #region A_follower_submits_to_its_leader()

        [Test]
        public async Task A_follower_submits_to_its_leader()
        {

            var follower   = await StartFollower();

            var head       = follower.History!.Head;
            var changeSet  = head.RoamingNetwork.CreateChangeSet(
                                 [ RoamingNetworkChange.UpdateProperty(head.RoamingNetwork, "dataSource", null, "from the follower") ],
                                 CreatedAt: T0
                             );

            POIWebSocketRequestException? refused = null;

            try
            {
                await follower.CommitChangeSetAsync(changeSet);
            }
            catch (POIWebSocketRequestException e)
            {
                refused = e;
            }

            poiAPI!.AuthorizeChangeSetSubmission  = (request, changeSet)            => true;
            poiAPI.AuthorizeStatusUpdate          = (request, login, statusUpdate)  => true;

            var published  = await follower.CommitChangeSetAsync(changeSet, ExpectedHeadId: head.Commit.Id);
            var status     = await follower.SetStatusAsync(POIRuntimeStatusKind.Status, Operator, "DE*GEF", "offline", T0);

            using (Assert.EnterMultipleScope())
            {

                Assert.That(refused?.ErrorCode,                      Is.EqualTo(POIWebSocketErrorCodes.SecurityError));
                Assert.That(published["status"]!.ToString(),         Is.EqualTo("Published"));
                Assert.That(status["result"]!.ToString(),            Is.EqualTo("Updated"));

                // Back from the leader, as for every other follower.
                Assert.That(await Eventually(InSync),                Is.True);
                Assert.That(follower.History.Head.RoamingNetwork.DataSource,  Is.EqualTo("from the follower"));
                Assert.That(await Eventually(() => follower.History.RuntimeStatus.TryGet(POIRuntimeStatusKind.Status, Operator, "DE*GEF", out var operatorStatus) &&
                                                   operatorStatus.Value == "offline"),
                            Is.True);

            }

        }

        #endregion

        #region A_follower_sets_and_follows_energy_statuses()

        [Test]
        public async Task A_follower_sets_and_follows_energy_statuses()
        {

            var e1 = EVSE_Id.Parse("DE*GEF*E1*1");

            leader!.Publish(leader.Head.RoamingNetwork.CreateChangeSet([
                                RoamingNetworkChange.Add(new ChargingPool(ChargingPool_Id.Parse("DE*GEF*P1"),
                                                                          ChargingStations: [ new ChargingStation(ChargingStation_Id.Parse("DE*GEF*S1"),
                                                                                                                  EVSEs: [ new EVSE(e1) ]) ]))
                            ], CreatedAt: T0));

            poiAPI!.AuthorizeStatusUpdate = (request, login, statusUpdate) => true;

            var follower  = await StartFollower();

            var answer    = await follower.SetEnergyStatusAsync(nameof(EVSE),
                                                                e1.ToString(),
                                                                new EnergyInfo(Watt.FromW(11000), Watt.FromW(22000)),
                                                                T0.AddMinutes(1));

            using (Assert.EnterMultipleScope())
            {

                Assert.That(answer["result"]!.ToString(),  Is.EqualTo("Updated"));

                // Back from the leader as a pushed energy status update.
                Assert.That(await Eventually(() => follower.History!.RuntimeStatus.TryGet(POIRuntimeStatusKind.EnergyStatus, nameof(EVSE), e1.ToString(), out var energyStatus) &&
                                                   energyStatus.Value == POIEnergyStatus.ToText(new EnergyInfo(Watt.FromW(11000), Watt.FromW(22000)))),
                            Is.True);

            }

        }

        #endregion


    }

}
